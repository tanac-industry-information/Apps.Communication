using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;

namespace Apps.Communication.WebSocket
{
	/// <summary>
	/// websocket协议的客户端实现，支持从服务器订阅，发布数据内容信息，详细参考api文档信息<br />
	/// Client implementation of the websocket protocol. It supports subscribing from the server and publishing data content information.
	/// </summary>
	/// <example>
	/// 本客户端使用起来非常的方便，基本就是实例化，绑定一个数据接收的事件即可，如下所示
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample1" title="简单的实例化" />
	/// 假设我们需要发数据给服务端，那么可以参考如下的方式
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample2" title="发送数据" />
	/// 如果我们需要搭配服务器来做订阅推送的功能的话，写法上会稍微有点区别，按照下面的代码来写。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample3" title="订阅操作" />
	/// 当网络发生异常的时候，我们需要这么来进行重新连接。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\WebSocket\WebSocketClientSample.cs" region="Sample4" title="异常重连" />
	/// </example>
	public class WebSocketClient : NetworkXBase, IDisposable
	{
		/// <summary>
		/// websocket的消息收到委托<br />
		/// websocket message received delegate
		/// </summary>
		/// <param name="message">websocket的消息</param>
		public delegate void OnClientApplicationMessageReceiveDelegate(WebSocketMessage message);

		/// <summary>
		/// 连接服务器成功的委托<br />
		/// Connection server successfully delegated
		/// </summary>
		public delegate void OnClientConnectedDelegate();

		private int isReConnectServer = 0;

		private string[] subcribeTopics;

		private bool closed = false;

		private string ipAddress = string.Empty;

		private int port = 1883;

		private int connectTimeOut = 10000;

		private Timer timerCheck;

		private string url = string.Empty;

		private bool disposedValue;

		/// <summary>
		/// Mqtt服务器的ip地址<br />
		/// IP address of Mqtt server
		/// </summary>
		public string IpAddress
		{
			get
			{
				return ipAddress;
			}
			set
			{
				ipAddress = HslHelper.GetIpAddressFromInput(value);
			}
		}

		/// <summary>
		/// 端口号。默认1883<br />
		/// The port number. Default 1883
		/// </summary>
		public int Port
		{
			get
			{
				return port;
			}
			set
			{
				port = value;
			}
		}

		/// <summary>
		/// 获取或设置当前客户端的连接超时时间，默认10,000毫秒，单位ms<br />
		/// Gets or sets the connection timeout of the current client. The default is 10,000 milliseconds. The unit is ms.
		/// </summary>
		public int ConnectTimeOut
		{
			get
			{
				return connectTimeOut;
			}
			set
			{
				connectTimeOut = value;
			}
		}

		/// <summary>
		/// 获取当前的客户端状态是否关闭了连接，当自己手动处理网络异常事件的时候，在重连之前就需要判断是否关闭了连接。<br />
		/// Obtain whether the current client status has closed the connection. When manually handling network abnormal events, you need to determine whether the connection is closed before reconnecting.
		/// </summary>
		public bool IsClosed => closed;

		/// <summary>
		/// websocket的消息收到时触发<br />
		/// Triggered when a websocket message is received
		///             </summary>
		public event OnClientApplicationMessageReceiveDelegate OnClientApplicationMessageReceive;

		/// <summary>
		/// 当客户端连接成功触发事件，就算是重新连接服务器后，也是会触发的<br />
		/// The event is triggered when the client is connected successfully, even after reconnecting to the server.
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected;

		/// <summary>
		/// 当网络发生异常的时候触发的事件，用户应该在事件里进行重连服务器
		/// </summary>
		public event EventHandler OnNetworkError;

		/// <summary>
		/// 使用指定的ip，端口来实例化一个默认的对象<br />
		/// Use the specified ip and port to instantiate a default objects
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public WebSocketClient(string ipAddress, int port)
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <summary>
		/// 使用指定的ip，端口，额外的url信息来实例化一个默认的对象<br />
		/// Use the specified ip, port, and additional url information to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="url">额外的信息，比如 /A/B?C=123456</param>
		public WebSocketClient(string ipAddress, int port, string url)
		{
			IpAddress = ipAddress;
			Port = port;
			this.url = url;
		}

		/// <summary>
		/// 使用指定的url来实例化一个默认的对象，例如 ws://127.0.0.1:1883/A/B?C=123456 或是 ws://www.hslcommunication.cn:1883<br />
		/// Use the specified url to instantiate a default object, such as ws://127.0.0.1:1883/A/B?C=123456 or ws://www.hslcommunication.cn:1883s
		/// </summary>
		/// <param name="url">完整的ws地址</param>
		public WebSocketClient(string url)
		{
			if (url.StartsWith("ws://"))
			{
				url = url.Substring(5);
				IpAddress = url.Substring(0, url.IndexOf(':'));
				url = url.Substring(url.IndexOf(':') + 1);
				if (url.IndexOf('/') < 0)
				{
					Port = int.Parse(url);
					return;
				}
				Port = int.Parse(url.Substring(0, url.IndexOf('/')));
				this.url = url.Substring(url.IndexOf('/'));
				return;
			}
			throw new Exception("Url Must start with ws://");
		}

		/// <summary>
		/// 连接服务器，实例化客户端之后，至少要调用成功一次，如果返回失败，那些请过一段时间后重新调用本方法连接。<br />
		/// After connecting to the server, the client must be called at least once after instantiating the client.
		/// If the return fails, please call this method to connect again after a period of time.
		/// </summary>
		/// <returns>连接是否成功</returns>
		public OperateResult ConnectServer()
		{
			return ConnectServer(subcribeTopics);
		}

		/// <summary>
		/// 连接服务器，实例化客户端之后，至少要调用成功一次，如果返回失败，那些请过一段时间后重新调用本方法连接。<br />
		/// After connecting to the server, the client must be called at least once after instantiating the client.
		/// If the return fails, please call this method to connect again after a period of time.
		/// </summary>
		/// <param name="subscribes">订阅的消息</param>
		/// <returns>连接是否成功</returns>
		public OperateResult ConnectServer(string[] subscribes)
		{
			subcribeTopics = subscribes;
			CoreSocket?.Close();
			OperateResult<Socket> operateResult = CreateSocketAndConnect(ipAddress, port, connectTimeOut);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			CoreSocket = operateResult.Content;
			byte[] data = WebSocketHelper.BuildWsSubRequest(ipAddress, port, url, subcribeTopics);
			OperateResult operateResult2 = Send(CoreSocket, data);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = Receive(CoreSocket, -1, 10000);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			try
			{
				CoreSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, ReceiveAsyncCallback, CoreSocket);
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
			closed = false;
			this.OnClientConnected?.Invoke();
			timerCheck?.Dispose();
			timerCheck = new Timer(TimerCheckServer, null, 2000, 30000);
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 关闭Mqtt服务器的连接。<br />
		/// Close the connection to the Mqtt server.
		/// </summary>
		public void ConnectClose()
		{
			if (!closed)
			{
				Send(CoreSocket, WebSocketHelper.WebScoketPackData(8, isMask: true, "Closed"));
				closed = true;
				Thread.Sleep(20);
				CoreSocket?.Close();
			}
		}

		/// <inheritdoc cref="M:Communication.WebSocket.WebSocketClient.ConnectServer" />
		public async Task<OperateResult> ConnectServerAsync()
		{
			return await ConnectServerAsync(subcribeTopics);
		}

		/// <inheritdoc cref="M:Communication.WebSocket.WebSocketClient.ConnectServer(System.String[])" />
		public async Task<OperateResult> ConnectServerAsync(string[] subscribes)
		{
			subcribeTopics = subscribes;
			CoreSocket?.Close();
			OperateResult<Socket> connect = await CreateSocketAndConnectAsync(ipAddress, port, connectTimeOut);
			if (!connect.IsSuccess)
			{
				return connect;
			}
			CoreSocket = connect.Content;
			OperateResult send = await SendAsync(data: WebSocketHelper.BuildWsSubRequest(ipAddress, port, url, subcribeTopics), socket: CoreSocket);
			if (!send.IsSuccess)
			{
				return send;
			}
			OperateResult<byte[]> rece = await ReceiveAsync(CoreSocket, -1, 10000);
			if (!rece.IsSuccess)
			{
				return rece;
			}
			try
			{
				CoreSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, ReceiveAsyncCallback, CoreSocket);
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
			closed = false;
			this.OnClientConnected?.Invoke();
			timerCheck?.Dispose();
			timerCheck = new Timer(TimerCheckServer, null, 2000, 30000);
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.WebSocket.WebSocketClient.ConnectClose" />
		public async Task ConnectCloseAsync()
		{
			if (!closed)
			{
				await SendAsync(CoreSocket, WebSocketHelper.WebScoketPackData(8, isMask: true, "Closed"));
				closed = true;
				Thread.Sleep(20);
				CoreSocket?.Close();
			}
		}

		private void OnWebsocketNetworkError()
		{
			if (closed || Interlocked.CompareExchange(ref isReConnectServer, 1, 0) != 0)
			{
				return;
			}
			try
			{
				if (this.OnNetworkError == null)
				{
					base.LogNet?.WriteInfo(ToString(), "The network is abnormal, and the system is ready to automatically reconnect after 10 seconds.");
					while (true)
					{
						for (int i = 0; i < 10; i++)
						{
							if (closed)
							{
								Interlocked.Exchange(ref isReConnectServer, 0);
								return;
							}
							Thread.Sleep(1000);
							base.LogNet?.WriteInfo(ToString(), $"Wait for {10 - i} second to connect to the server ...");
						}
						if (closed)
						{
							Interlocked.Exchange(ref isReConnectServer, 0);
							return;
						}
						OperateResult operateResult = ConnectServer();
						if (operateResult.IsSuccess)
						{
							break;
						}
						base.LogNet?.WriteInfo(ToString(), "The connection failed. Prepare to reconnect after 10 seconds.");
					}
					base.LogNet?.WriteInfo(ToString(), "Successfully connected to the server!");
				}
				else
				{
					this.OnNetworkError?.Invoke(this, new EventArgs());
				}
				Interlocked.Exchange(ref isReConnectServer, 0);
			}
			catch
			{
				Interlocked.Exchange(ref isReConnectServer, 0);
				throw;
			}
		}

		private async void ReceiveAsyncCallback(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			Socket socket = asyncState as Socket;
			if (socket == null)
			{
				return;
			}
			try
			{
				socket.EndReceive(ar);
			}
			catch (ObjectDisposedException)
			{
				socket?.Close();
				base.LogNet?.WriteDebug(ToString(), "Closed");
				return;
			}
			catch (Exception ex4)
			{
				Exception ex2 = ex4;
				socket?.Close();
				base.LogNet?.WriteDebug(ToString(), "ReceiveCallback Failed:" + ex2.Message);
				OnWebsocketNetworkError();
				return;
			}
			if (closed)
			{
				base.LogNet?.WriteDebug(ToString(), "Closed");
				return;
			}
			OperateResult<WebSocketMessage> read = await ReceiveWebSocketPayloadAsync(socket);
			if (!read.IsSuccess)
			{
				OnWebsocketNetworkError();
				return;
			}
			if (read.Content.OpCode == 9)
			{
				Send(socket, WebSocketHelper.WebScoketPackData(10, isMask: true, read.Content.Payload));
				base.LogNet?.WriteDebug(ToString(), read.Content.ToString());
			}
			else if (read.Content.OpCode == 10)
			{
				base.LogNet?.WriteDebug(ToString(), read.Content.ToString());
			}
			else
			{
				this.OnClientApplicationMessageReceive?.Invoke(read.Content);
			}
			try
			{
				socket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, ReceiveAsyncCallback, socket);
			}
			catch (Exception ex)
			{
				socket?.Close();
				base.LogNet?.WriteDebug(ToString(), "BeginReceive Failed:" + ex.Message);
				OnWebsocketNetworkError();
			}
		}

		private void TimerCheckServer(object obj)
		{
			if (CoreSocket == null)
			{
			}
		}

		/// <summary>
		/// 发送数据到WebSocket的服务器<br />
		/// Send data to WebSocket server
		/// </summary>
		/// <param name="message">消息</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer(string message)
		{
			return Send(CoreSocket, WebSocketHelper.WebScoketPackData(1, isMask: true, message));
		}

		/// <summary>
		/// 发送数据到WebSocket的服务器，可以指定是否进行掩码操作<br />
		/// Send data to the WebSocket server, you can specify whether to perform a mask operation
		/// </summary>
		/// <param name="mask">是否进行掩码操作</param>
		/// <param name="message">消息</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer(bool mask, string message)
		{
			return Send(CoreSocket, WebSocketHelper.WebScoketPackData(1, mask, message));
		}

		/// <summary>
		/// 发送自定义的命令到WebSocket服务器，可以指定操作码，是否掩码操作，原始字节数据<br />
		/// Send custom commands to the WebSocket server, you can specify the operation code, whether to mask operation, raw byte data
		/// </summary>
		/// <param name="opCode">操作码</param>
		/// <param name="mask">是否进行掩码操作</param>
		/// <param name="payload">原始字节数据</param>
		/// <returns>是否发送成功</returns>
		public OperateResult SendServer(int opCode, bool mask, byte[] payload)
		{
			return Send(CoreSocket, WebSocketHelper.WebScoketPackData(opCode, mask, payload));
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.OnClientApplicationMessageReceive = null;
					this.OnClientConnected = null;
					this.OnNetworkError = null;
				}
				disposedValue = true;
			}
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"WebSocketClient[{ipAddress}:{port}]";
		}
	}
}

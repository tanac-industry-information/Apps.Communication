using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Security;
using Apps.Communication.Enthernet.Redis;
using Apps.Communication.LogNet;
using Apps.Communication.MQTT;
using Apps.Communication.WebSocket;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 本系统所有网络类的基类，该类为抽象类，无法进行实例化，如果想使用里面的方法来实现自定义的网络通信，请通过继承使用。<br />
	/// The base class of all network classes in this system. This class is an abstract class and cannot be instantiated. 
	/// If you want to use the methods inside to implement custom network communication, please use it through inheritance.
	/// </summary>
	/// <remarks>
	/// 本类提供了丰富的底层数据的收发支持，包含<see cref="T:Communication.Core.IMessage.INetMessage" />消息的接收，<c>MQTT</c>以及<c>Redis</c>,<c>websocket</c>协议的实现
	/// </remarks>
	public abstract class NetworkBase
	{
		/// <summary>
		/// 文件传输的时候的缓存大小，直接影响传输的速度，值越大，传输速度越快，越占内存，默认为100K大小<br />
		/// The size of the cache during file transfer directly affects the speed of the transfer. The larger the value, the faster the transfer speed and the more memory it takes. The default size is 100K.
		/// </summary>
		protected int fileCacheSize = 102400;

		private int connectErrorCount = 0;

		/// <summary>
		/// 组件的日志工具，支持日志记录，只要实例化后，当前网络的基本信息，就以<see cref="F:Communication.LogNet.HslMessageDegree.DEBUG" />等级进行输出<br />
		/// The component's logging tool supports logging. As long as the instantiation of the basic network information, the output will be output at <see cref="F:Communication.LogNet.HslMessageDegree.DEBUG" />
		/// </summary>
		/// <remarks>
		/// 只要实例化即可以记录日志，实例化的对象需要实现接口 <see cref="T:Communication.LogNet.ILogNet" /> ，本组件提供了三个日志记录类，你可以实现基于 <see cref="T:Communication.LogNet.ILogNet" />  的对象。</remarks>
		/// <example>
		/// 如下的实例化适用于所有的Network及其派生类，以下举两个例子，三菱的设备类及服务器类
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample1" title="LogNet示例" />
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="LogNetExample2" title="LogNet示例" />
		/// </example>
		public ILogNet LogNet { get; set; }

		/// <summary>
		/// 网络类的身份令牌，在hsl协议的模式下会有效，在和设备进行通信的时候是无效的<br />
		/// Network-type identity tokens will be valid in the hsl protocol mode and will not be valid when communicating with the device
		/// </summary>
		/// <remarks>
		/// 适用于Hsl协议相关的网络通信类，不适用于设备交互类。
		/// </remarks>
		/// <example>
		/// 此处以 <see cref="T:Communication.Enthernet.NetSimplifyServer" /> 服务器类及 <see cref="T:Communication.Enthernet.NetSimplifyClient" /> 客户端类的令牌设置举例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenClientExample" title="Client示例" />
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="TokenServerExample" title="Server示例" />
		/// </example>
		public Guid Token { get; set; }

		/// <summary>
		/// 实例化一个NetworkBase对象，令牌的默认值为空，都是0x00<br />
		/// Instantiate a NetworkBase object, the default value of the token is empty, both are 0x00
		/// </summary>
		public NetworkBase()
		{
			Token = Guid.Empty;

		}

		/// <summary>
		/// 接收固定长度的字节数组，允许指定超时时间，默认为60秒，当length大于0时，接收固定长度的数据内容，当length小于0时，buffer长度的缓存数据<br />
		/// Receiving a fixed-length byte array, allowing a specified timeout time. The default is 60 seconds. When length is greater than 0, 
		/// fixed-length data content is received. When length is less than 0, random data information of a length not greater than 2048 is received.
		/// </summary>
		/// <param name="socket">网络通讯的套接字<br />Network communication socket</param>
		/// <param name="buffer">等待接收的数据缓存信息</param>
		/// <param name="offset">开始接收数据的偏移地址</param>
		/// <param name="length">准备接收的数据长度，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于1024长度的随机数据信息</param>
		/// <param name="timeOut">单位：毫秒，超时时间，默认为60秒，如果设置小于0，则不检查超时时间</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>包含了字节数据的结果类</returns>
		protected OperateResult<int> Receive(Socket socket, byte[] buffer, int offset, int length, int timeOut = 60000, Action<long, long> reportProgress = null)
		{
			if (length == 0)
			{
				return OperateResult.CreateSuccessResult(0);
			}
			try
			{
				socket.ReceiveTimeout = timeOut;
				if (length > 0)
				{
					NetSupport.ReceiveBytesFromSocket(socket, buffer, offset, length, reportProgress);
					return OperateResult.CreateSuccessResult(length);
				}
				int num = socket.Receive(buffer, offset, buffer.Length - offset, SocketFlags.None);
				if (num == 0)
				{
					throw new RemoteCloseException();
				}
				return OperateResult.CreateSuccessResult(num);
			}
			catch (RemoteCloseException)
			{
				socket?.Close();
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				return new OperateResult<int>(-connectErrorCount, "Socket Exception -> " + StringResources.Language.RemoteClosedConnection);
			}
			catch (Exception ex2)
			{
				socket?.Close();
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				return new OperateResult<int>(-connectErrorCount, "Socket Exception -> " + ex2.Message);
			}
		}

		/// <summary>
		/// 接收固定长度的字节数组，允许指定超时时间，默认为60秒，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于2048长度的随机数据信息<br />
		/// Receiving a fixed-length byte array, allowing a specified timeout time. The default is 60 seconds. When length is greater than 0, 
		/// fixed-length data content is received. When length is less than 0, random data information of a length not greater than 2048 is received.
		/// </summary>
		/// <param name="socket">网络通讯的套接字<br />Network communication socket</param>
		/// <param name="length">准备接收的数据长度，当length大于0时，接收固定长度的数据内容，当length小于0时，接收不大于1024长度的随机数据信息</param>
		/// <param name="timeOut">单位：毫秒，超时时间，默认为60秒，如果设置小于0，则不检查超时时间</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>包含了字节数据的结果类</returns>
		protected OperateResult<byte[]> Receive(Socket socket, int length, int timeOut = 60000, Action<long, long> reportProgress = null)
		{
			if (length == 0)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}

			int num = ((length > 0) ? length : 2048);
			byte[] array;
			try
			{
				array = new byte[num];
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<byte[]>($"Create byte[{num}] buffer failed: " + ex.Message);
			}
			OperateResult<int> operateResult = Receive(socket, array, 0, length, timeOut, reportProgress);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult((length > 0) ? array : array.SelectBegin(operateResult.Content));
		}

		/// <summary>
		/// 接收一行命令数据，需要自己指定这个结束符，默认超时时间为60秒，也即是60000，单位是毫秒<br />
		/// To receive a line of command data, you need to specify the terminator yourself. The default timeout is 60 seconds, which is 60,000, in milliseconds.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="endCode">结束符信息</param>
		/// <param name="timeout">超时时间，默认为60000，单位为毫秒，也就是60秒</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveCommandLineFromSocket(Socket socket, byte endCode, int timeout = 60000)
		{
			List<byte> list = new List<byte>(128);
			try
			{
				DateTime now = DateTime.Now;
				bool flag = false;
				while ((DateTime.Now - now).TotalMilliseconds < (double)timeout)
				{
					if (socket.Poll(timeout, SelectMode.SelectRead))
					{
						OperateResult<byte[]> operateResult = Receive(socket, 1, timeout);
						if (!operateResult.IsSuccess)
						{
							return operateResult;
						}
						list.AddRange(operateResult.Content);
						if (operateResult.Content[0] == endCode)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					return new OperateResult<byte[]>(StringResources.Language.ReceiveDataTimeout);
				}
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <summary>
		/// 接收一行命令数据，需要自己指定这个结束符，默认超时时间为60秒，也即是60000，单位是毫秒<br />
		/// To receive a line of command data, you need to specify the terminator yourself. The default timeout is 60 seconds, which is 60,000, in milliseconds.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="endCode1">结束符1信息</param>
		/// <param name="endCode2">结束符2信息</param>
		/// /// <param name="timeout">超时时间，默认无穷大，单位毫秒</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveCommandLineFromSocket(Socket socket, byte endCode1, byte endCode2, int timeout = 60000)
		{
			List<byte> list = new List<byte>(128);
			try
			{
				DateTime now = DateTime.Now;
				bool flag = false;
				while ((DateTime.Now - now).TotalMilliseconds < (double)timeout)
				{
					if (socket.Poll(timeout, SelectMode.SelectRead))
					{
						OperateResult<byte[]> operateResult = Receive(socket, 1, timeout);
						if (!operateResult.IsSuccess)
						{
							return operateResult;
						}
						list.AddRange(operateResult.Content);
						if (operateResult.Content[0] == endCode2 && list.Count > 1 && list[list.Count - 2] == endCode1)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					return new OperateResult<byte[]>(StringResources.Language.ReceiveDataTimeout);
				}
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <summary>
		/// 接收一条完整的 <seealso cref="T:Communication.Core.IMessage.INetMessage" /> 数据内容，需要指定超时时间，单位为毫秒。 <br />
		/// Receive a complete <seealso cref="T:Communication.Core.IMessage.INetMessage" /> data content, Need to specify a timeout period in milliseconds
		/// </summary>
		/// <param name="socket">网络的套接字</param>
		/// <param name="timeOut">超时时间，单位：毫秒</param>
		/// <param name="netMessage">消息的格式定义</param>
		/// <param name="reportProgress">接收消息的时候的进度报告</param>
		/// <returns>带有是否成功的byte数组对象</returns>
		protected virtual OperateResult<byte[]> ReceiveByMessage(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			if (netMessage == null)
			{
				return Receive(socket, -1, timeOut);
			}
			OperateResult<byte[]> operateResult = Receive(socket, netMessage.ProtocolHeadBytesLength, timeOut);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			netMessage.HeadBytes = operateResult.Content;
			int contentLengthByHeadBytes = netMessage.GetContentLengthByHeadBytes();
			if (contentLengthByHeadBytes <= 0)
			{
				return OperateResult.CreateSuccessResult(operateResult.Content);
			}
			byte[] array = new byte[netMessage.ProtocolHeadBytesLength + contentLengthByHeadBytes];
			operateResult.Content.CopyTo(array, 0);
			OperateResult operateResult2 = Receive(socket, array, netMessage.ProtocolHeadBytesLength, contentLengthByHeadBytes, timeOut, reportProgress);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 发送消息给套接字，直到完成的时候返回，经过测试，本方法是线程安全的。<br />
		/// Send a message to the socket until it returns when completed. After testing, this method is thread-safe.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="data">字节数据</param>
		/// <returns>发送是否成功的结果</returns>
		protected OperateResult Send(Socket socket, byte[] data)
		{
			if (data == null)
			{
				return OperateResult.CreateSuccessResult();
			}
			return Send(socket, data, 0, data.Length);
		}

		/// <summary>
		/// 发送消息给套接字，直到完成的时候返回，经过测试，本方法是线程安全的。<br />
		/// Send a message to the socket until it returns when completed. After testing, this method is thread-safe.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="data">字节数据</param>
		/// <param name="offset">偏移的位置信息</param>
		/// <param name="size">发送的数据总数</param>
		/// <returns>发送是否成功的结果</returns>
		protected OperateResult Send(Socket socket, byte[] data, int offset, int size)
		{
			if (data == null)
			{
				return OperateResult.CreateSuccessResult();
			}

			try
			{
				int num = 0;
				do
				{
					int num2 = socket.Send(data, offset, size - num, SocketFlags.None);
					num += num2;
					offset += num2;
				}
				while (num < size);
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				socket?.Close();
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				return new OperateResult<byte[]>(-connectErrorCount, ex.Message);
			}
		}

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，默认超时时间为10秒钟，需要指定ip地址以及端口号信息<br />
		/// Create a new socket object and connect to the remote address. The default timeout is 10 seconds. You need to specify the IP address and port number.
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect(string ipAddress, int port)
		{
			return CreateSocketAndConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), 10000);
		}

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，需要指定ip地址以及端口号信息，还有超时时间，单位是毫秒<br />
		/// To create a new socket object and connect to a remote address, you need to specify the IP address and port number information, and the timeout period in milliseconds
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="timeOut">连接的超时时间</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect(string ipAddress, int port, int timeOut)
		{
			return CreateSocketAndConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), timeOut);
		}

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，需要指定远程终结点，超时时间（单位是毫秒），如果需要绑定本地的IP或是端口，传入 local对象<br />
		/// To create a new socket object and connect to the remote address, you need to specify the remote endpoint, 
		/// the timeout period (in milliseconds), if you need to bind the local IP or port, pass in the local object
		/// </summary>
		/// <param name="endPoint">连接的目标终结点</param>
		/// <param name="timeOut">连接的超时时间</param>
		/// <param name="local">如果需要绑定本地的IP地址，就需要设置当前的对象</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		protected OperateResult<Socket> CreateSocketAndConnect(IPEndPoint endPoint, int timeOut, IPEndPoint local = null)
		{
			OperateResult<Socket> operateResult = NetSupport.CreateSocketAndConnect(endPoint, timeOut, local);
			if (operateResult.IsSuccess)
			{
				connectErrorCount = 0;
				return operateResult;
			}
			if (connectErrorCount < 1000000000)
			{
				connectErrorCount++;
			}
			return new OperateResult<Socket>(-connectErrorCount, operateResult.Message);
		}

		/// <summary>
		/// 读取流中的数据到缓存区，读取的长度需要按照实际的情况来判断<br />
		/// Read the data in the stream to the buffer area. The length of the read needs to be determined according to the actual situation.
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="buffer">缓冲区</param>
		/// <returns>带有成功标志的读取数据长度</returns>
		protected OperateResult<int> ReadStream(Stream stream, byte[] buffer)
		{
			ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
			FileStateObject fileStateObject = new FileStateObject
			{
				WaitDone = manualResetEvent,
				Stream = stream,
				DataLength = buffer.Length,
				Buffer = buffer
			};
			try
			{
				stream.BeginRead(buffer, 0, fileStateObject.DataLength, ReadStreamCallBack, fileStateObject);
			}
			catch (Exception ex)
			{
				fileStateObject = null;
				manualResetEvent.Close();
				return new OperateResult<int>("stream.BeginRead Exception -> " + ex.Message);
			}
			manualResetEvent.WaitOne();
			manualResetEvent.Close();
			return fileStateObject.IsError ? new OperateResult<int>(fileStateObject.ErrerMsg) : OperateResult.CreateSuccessResult(fileStateObject.AlreadyDealLength);
		}

		private void ReadStreamCallBack(IAsyncResult ar)
		{
			FileStateObject fileStateObject = ar.AsyncState as FileStateObject;
			if (fileStateObject != null)
			{
				try
				{
					fileStateObject.AlreadyDealLength += fileStateObject.Stream.EndRead(ar);
					fileStateObject.WaitDone.Set();
				}
				catch (Exception ex)
				{
					fileStateObject.IsError = true;
					fileStateObject.ErrerMsg = ex.Message;
					fileStateObject.WaitDone.Set();
				}
			}
		}

		/// <summary>
		/// 将缓冲区的数据写入到流里面去<br />
		/// Write the buffer data to the stream
		/// </summary>
		/// <param name="stream">数据流</param>
		/// <param name="buffer">缓冲区</param>
		/// <returns>是否写入成功</returns>
		protected OperateResult WriteStream(Stream stream, byte[] buffer)
		{
			ManualResetEvent manualResetEvent = new ManualResetEvent(initialState: false);
			FileStateObject fileStateObject = new FileStateObject
			{
				WaitDone = manualResetEvent,
				Stream = stream
			};
			try
			{
				stream.BeginWrite(buffer, 0, buffer.Length, WriteStreamCallBack, fileStateObject);
			}
			catch (Exception ex)
			{
				fileStateObject = null;
				manualResetEvent.Close();
				return new OperateResult("stream.BeginWrite Exception -> " + ex.Message);
			}
			manualResetEvent.WaitOne();
			manualResetEvent.Close();
			if (fileStateObject.IsError)
			{
				return new OperateResult
				{
					Message = fileStateObject.ErrerMsg
				};
			}
			return OperateResult.CreateSuccessResult();
		}

		private void WriteStreamCallBack(IAsyncResult ar)
		{
			FileStateObject fileStateObject = ar.AsyncState as FileStateObject;
			if (fileStateObject == null)
			{
				return;
			}
			try
			{
				fileStateObject.Stream.EndWrite(ar);
			}
			catch (Exception ex)
			{
				fileStateObject.IsError = true;
				fileStateObject.ErrerMsg = ex.Message;
			}
			finally
			{
				fileStateObject.WaitDone.Set();
			}
		}

		/// <summary>
		/// 检查当前的头子节信息的令牌是否是正确的，仅用于某些特殊的协议实现<br />
		/// Check whether the token of the current header subsection information is correct, only for some special protocol implementations
		/// </summary>
		/// <param name="headBytes">头子节数据</param>
		/// <returns>令牌是验证成功</returns>
		protected bool CheckRemoteToken(byte[] headBytes)
		{
			return SoftBasic.IsByteTokenEquel(headBytes, Token);
		}

		/// <summary>
		/// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send the byte data and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="headCode">头指令</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendBaseAndCheckReceive(Socket socket, int headCode, int customer, byte[] send)
		{
			send = HslProtocol.CommandBytes(headCode, customer, Token, send);
			OperateResult operateResult = Send(socket, send);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<long> operateResult2 = ReceiveLong(socket);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content != send.Length)
			{
				socket?.Close();
				return new OperateResult(StringResources.Language.CommandLengthCheckFailed);
			}
			return operateResult2;
		}

		/// <summary>
		/// [自校验] 发送字节数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send the byte data and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendBytesAndCheckReceive(Socket socket, int customer, byte[] send)
		{
			return SendBaseAndCheckReceive(socket, 1002, customer, send);
		}

		/// <summary>
		/// [自校验] 直接发送字符串数据并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Send string data directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="send">发送的数据</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendStringAndCheckReceive(Socket socket, int customer, string send)
		{
			byte[] send2 = (string.IsNullOrEmpty(send) ? null : Encoding.Unicode.GetBytes(send));
			return SendBaseAndCheckReceive(socket, 1001, customer, send2);
		}

		/// <summary>
		/// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send string array directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="sends">发送的字符串数组</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendStringAndCheckReceive(Socket socket, int customer, string[] sends)
		{
			return SendBaseAndCheckReceive(socket, 1005, customer, HslProtocol.PackStringArrayToByte(sends));
		}

		/// <summary>
		/// [自校验] 直接发送字符串数组并确认对方接收完成数据，如果结果异常，则结束通讯<br />
		/// [Self-check] Send string array directly and confirm that the other party has received the completed data. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="customer">用户指令</param>
		/// <param name="name">用户名</param>
		/// <param name="pwd">密码</param>
		/// <returns>是否发送成功</returns>
		protected OperateResult SendAccountAndCheckReceive(Socket socket, int customer, string name, string pwd)
		{
			return SendBaseAndCheckReceive(socket, 5, customer, HslProtocol.PackStringArrayToByte(new string[2] { name, pwd }));
		}

		/// <summary>
		/// [自校验] 接收一条完整的同步数据，包含头子节和内容字节，基础的数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a complete synchronization data, including header subsection and content bytes, basic data, if the result is abnormal, the communication ends
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">超时时间设置，如果为负数，则不检查超时</param>
		/// <returns>包含是否成功的结果对象</returns>
		/// <exception cref="T:System.ArgumentNullException">result</exception>
		protected OperateResult<byte[], byte[]> ReceiveAndCheckBytes(Socket socket, int timeOut)
		{
			OperateResult<byte[]> operateResult = Receive(socket, 32, timeOut);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[], byte[]>();
			}
			if (!CheckRemoteToken(operateResult.Content))
			{
				socket?.Close();
				return new OperateResult<byte[], byte[]>(StringResources.Language.TokenCheckFailed);
			}
			int num = BitConverter.ToInt32(operateResult.Content, 28);
			OperateResult<byte[]> operateResult2 = Receive(socket, num, timeOut);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2.ConvertFailed<byte[], byte[]>();
			}
			OperateResult operateResult3 = SendLong(socket, 32 + num);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3.ConvertFailed<byte[], byte[]>();
			}
			byte[] content = operateResult.Content;
			byte[] content2 = operateResult2.Content;
			content2 = HslProtocol.CommandAnalysis(content, content2);
			return OperateResult.CreateSuccessResult(content, content2);
		}

		/// <summary>
		/// [自校验] 从网络中接收一个字符串数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a string of data from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">接收数据的超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, string> ReceiveStringContentFromSocket(Socket socket, int timeOut = 30000)
		{
			OperateResult<byte[], byte[]> operateResult = ReceiveAndCheckBytes(socket, timeOut);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(operateResult);
			}
			if (BitConverter.ToInt32(operateResult.Content1, 0) != 1001)
			{
				socket?.Close();
				return new OperateResult<int, string>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			if (operateResult.Content2 == null)
			{
				operateResult.Content2 = new byte[0];
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(operateResult.Content1, 4), Encoding.Unicode.GetString(operateResult.Content2));
		}

		/// <summary>
		/// [自校验] 从网络中接收一个字符串数组，如果结果异常，则结束通讯<br />
		/// [Self-check] Receive an array of strings from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="timeOut">接收数据的超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, string[]> ReceiveStringArrayContentFromSocket(Socket socket, int timeOut = 30000)
		{
			OperateResult<byte[], byte[]> operateResult = ReceiveAndCheckBytes(socket, timeOut);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string[]>(operateResult);
			}
			if (BitConverter.ToInt32(operateResult.Content1, 0) != 1005)
			{
				socket?.Close();
				return new OperateResult<int, string[]>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			if (operateResult.Content2 == null)
			{
				operateResult.Content2 = new byte[4];
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(operateResult.Content1, 4), HslProtocol.UnPackStringArrayFromByte(operateResult.Content2));
		}

		/// <summary>
		/// [自校验] 从网络中接收一串字节数据，如果结果异常，则结束通讯<br />
		/// [Self-checking] Receive a string of byte data from the network. If the result is abnormal, the communication ends.
		/// </summary>
		/// <param name="socket">套接字的网络</param>
		/// <param name="timeout">超时时间</param>
		/// <returns>包含是否成功的结果对象</returns>
		protected OperateResult<int, byte[]> ReceiveBytesContentFromSocket(Socket socket, int timeout = 30000)
		{
			OperateResult<byte[], byte[]> operateResult = ReceiveAndCheckBytes(socket, timeout);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, byte[]>(operateResult);
			}
			if (BitConverter.ToInt32(operateResult.Content1, 0) != 1002)
			{
				socket?.Close();
				return new OperateResult<int, byte[]>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(operateResult.Content1, 4), operateResult.Content2);
		}

		/// <summary>
		/// 从网络中接收Long数据<br />
		/// Receive Long data from the network
		/// </summary>
		/// <param name="socket">套接字网络</param>
		/// <returns>long数据结果</returns>
		private OperateResult<long> ReceiveLong(Socket socket)
		{
			OperateResult<byte[]> operateResult = Receive(socket, 8, -1);
			if (operateResult.IsSuccess)
			{
				return OperateResult.CreateSuccessResult(BitConverter.ToInt64(operateResult.Content, 0));
			}
			return OperateResult.CreateFailedResult<long>(operateResult);
		}

		/// <summary>
		/// 将long数据发送到套接字<br />
		/// Send long data to the socket
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="value">long数据</param>
		/// <returns>是否发送成功</returns>
		private OperateResult SendLong(Socket socket, long value)
		{
			return Send(socket, BitConverter.GetBytes(value));
		}

		/// <summary>
		/// 发送一个流的所有数据到指定的网络套接字，需要指定发送的数据长度，支持按照百分比的进度报告<br />
		/// Send all the data of a stream to the specified network socket. You need to specify the length of the data to be sent. It supports the progress report in percentage.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="stream">内存流</param>
		/// <param name="receive">发送的数据长度</param>
		/// <param name="report">进度报告的委托</param>
		/// <param name="reportByPercent">进度报告是否按照百分比报告</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult SendStreamToSocket(Socket socket, Stream stream, long receive, Action<long, long> report, bool reportByPercent)
		{
			byte[] array = new byte[fileCacheSize];
			long num = 0L;
			long num2 = 0L;
			stream.Position = 0L;
			while (num < receive)
			{
				OperateResult<int> operateResult = ReadStream(stream, array);
				if (!operateResult.IsSuccess)
				{
					socket?.Close();
					return operateResult;
				}
				num += operateResult.Content;
				byte[] array2 = new byte[operateResult.Content];
				Array.Copy(array, 0, array2, 0, array2.Length);
				OperateResult operateResult2 = SendBytesAndCheckReceive(socket, operateResult.Content, array2);
				if (!operateResult2.IsSuccess)
				{
					socket?.Close();
					return operateResult2;
				}
				if (reportByPercent)
				{
					long num3 = num * 100 / receive;
					if (num2 != num3)
					{
						num2 = num3;
						report?.Invoke(num, receive);
					}
				}
				else
				{
					report?.Invoke(num, receive);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 从套接字中接收所有的数据然后写入到指定的流当中去，需要指定数据的长度，支持按照百分比进行进度报告<br />
		/// Receives all data from the socket and writes it to the specified stream. The length of the data needs to be specified, and progress reporting is supported in percentage.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="totalLength">所有数据的长度</param>
		/// <param name="report">进度报告</param>
		/// <param name="reportByPercent">进度报告是否按照百分比</param>
		/// <returns>是否成功的结果对象</returns>
		protected OperateResult WriteStreamFromSocket(Socket socket, Stream stream, long totalLength, Action<long, long> report, bool reportByPercent)
		{
			long num = 0L;
			long num2 = 0L;
			while (num < totalLength)
			{
				OperateResult<int, byte[]> operateResult = ReceiveBytesContentFromSocket(socket, 60000);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				num += operateResult.Content1;
				OperateResult operateResult2 = WriteStream(stream, operateResult.Content2);
				if (!operateResult2.IsSuccess)
				{
					socket?.Close();
					return operateResult2;
				}
				if (reportByPercent)
				{
					long num3 = num * 100 / totalLength;
					if (num2 != num3)
					{
						num2 = num3;
						report?.Invoke(num, totalLength);
					}
				}
				else
				{
					report?.Invoke(num, totalLength);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.CreateSocketAndConnect(System.Net.IPEndPoint,System.Int32,System.Net.IPEndPoint)" />
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync(IPEndPoint endPoint, int timeOut, IPEndPoint local = null)
		{
			OperateResult<Socket> connect = await NetSupport.CreateSocketAndConnectAsync(endPoint, timeOut, local);
			if (connect.IsSuccess)
			{
				connectErrorCount = 0;
				return connect;
			}
			if (connectErrorCount < 1000000000)
			{
				connectErrorCount++;
			}
			return new OperateResult<Socket>(-connectErrorCount, connect.Message);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.CreateSocketAndConnect(System.String,System.Int32)" />
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync(string ipAddress, int port)
		{
			return await CreateSocketAndConnectAsync(new IPEndPoint(IPAddress.Parse(ipAddress), port), 10000);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.CreateSocketAndConnect(System.String,System.Int32,System.Int32)" />
		protected async Task<OperateResult<Socket>> CreateSocketAndConnectAsync(string ipAddress, int port, int timeOut)
		{
			return await CreateSocketAndConnectAsync(new IPEndPoint(IPAddress.Parse(ipAddress), port), timeOut);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.Receive(System.Net.Sockets.Socket,System.Int32,System.Int32,System.Action{System.Int64,System.Int64})" />
		protected async Task<OperateResult<byte[]>> ReceiveAsync(Socket socket, int length, int timeOut = 60000, Action<long, long> reportProgress = null)
		{
			if (length == 0)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}

			int bufferLength = ((length > 0) ? length : 2048);
			byte[] buffer;
			try
			{
				buffer = new byte[bufferLength];
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<byte[]>($"Create byte[{bufferLength}] buffer failed: " + ex.Message);
			}
			OperateResult<int> receive = await ReceiveAsync(socket, buffer, 0, length, timeOut, reportProgress);
			if (!receive.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(receive);
			}
			return OperateResult.CreateSuccessResult((length > 0) ? buffer : buffer.SelectBegin(receive.Content));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.Receive(System.Net.Sockets.Socket,System.Byte[],System.Int32,System.Int32,System.Int32,System.Action{System.Int64,System.Int64})" />
		protected async Task<OperateResult<int>> ReceiveAsync(Socket socket, byte[] buffer, int offset, int length, int timeOut = 60000, Action<long, long> reportProgress = null)
		{
			if (length == 0)
			{
				return OperateResult.CreateSuccessResult(length);
			}

			HslTimeOut hslTimeOut = HslTimeOut.HandleTimeOutCheck(socket, timeOut);
			try
			{
				if (length > 0)
				{
					int alreadyCount = 0;
					do
					{
						int currentReceiveLength = ((length - alreadyCount > 16384) ? 16384 : (length - alreadyCount));
						int count = await Task.Factory.FromAsync(socket.BeginReceive(buffer, alreadyCount + offset, currentReceiveLength, SocketFlags.None, null, socket), (Func<IAsyncResult, int>)socket.EndReceive);
						alreadyCount += count;
						if (count > 0)
						{
							hslTimeOut.StartTime = DateTime.Now;
							reportProgress?.Invoke(alreadyCount, length);
							continue;
						}
						throw new RemoteCloseException();
					}
					while (alreadyCount < length);
					hslTimeOut.IsSuccessful = true;
					return OperateResult.CreateSuccessResult(length);
				}
				int count2 = await Task.Factory.FromAsync(socket.BeginReceive(buffer, offset, buffer.Length - offset, SocketFlags.None, null, socket), (Func<IAsyncResult, int>)socket.EndReceive);
				if (count2 == 0)
				{
					throw new RemoteCloseException();
				}
				hslTimeOut.IsSuccessful = true;
				return OperateResult.CreateSuccessResult(count2);
			}
			catch (RemoteCloseException)
			{
				socket?.Close();
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				hslTimeOut.IsSuccessful = true;
				return new OperateResult<int>(-connectErrorCount, StringResources.Language.RemoteClosedConnection);
			}
			catch (Exception ex)
			{
				socket?.Close();
				hslTimeOut.IsSuccessful = true;
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				if (hslTimeOut.IsTimeout)
				{
					return new OperateResult<int>(-connectErrorCount, StringResources.Language.ReceiveDataTimeout + hslTimeOut.DelayTime);
				}
				return new OperateResult<int>(-connectErrorCount, "Socket Exception -> " + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveCommandLineFromSocket(System.Net.Sockets.Socket,System.Byte,System.Int32)" />
		protected async Task<OperateResult<byte[]>> ReceiveCommandLineFromSocketAsync(Socket socket, byte endCode, int timeout = int.MaxValue)
		{
			List<byte> bufferArray = new List<byte>(128);
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;
				while ((DateTime.Now - st).TotalMilliseconds < (double)timeout)
				{
					if (socket.Poll(timeout, SelectMode.SelectRead))
					{
						OperateResult<byte[]> headResult = await ReceiveAsync(socket, 1, timeout);
						if (!headResult.IsSuccess)
						{
							return headResult;
						}
						bufferArray.AddRange(headResult.Content);
						if (headResult.Content[0] == endCode)
						{
							bOK = true;
							break;
						}
					}
				}
				if (!bOK)
				{
					return new OperateResult<byte[]>(StringResources.Language.ReceiveDataTimeout);
				}
				return OperateResult.CreateSuccessResult(bufferArray.ToArray());
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				socket?.Close();
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveCommandLineFromSocket(System.Net.Sockets.Socket,System.Byte,System.Byte,System.Int32)" />
		protected async Task<OperateResult<byte[]>> ReceiveCommandLineFromSocketAsync(Socket socket, byte endCode1, byte endCode2, int timeout = 60000)
		{
			List<byte> bufferArray = new List<byte>(128);
			try
			{
				DateTime st = DateTime.Now;
				bool bOK = false;
				while ((DateTime.Now - st).TotalMilliseconds < (double)timeout)
				{
					if (socket.Poll(timeout, SelectMode.SelectRead))
					{
						OperateResult<byte[]> headResult = await ReceiveAsync(socket, 1, timeout);
						if (!headResult.IsSuccess)
						{
							return headResult;
						}
						bufferArray.AddRange(headResult.Content);
						if (headResult.Content[0] == endCode2 && bufferArray.Count > 1 && bufferArray[bufferArray.Count - 2] == endCode1)
						{
							bOK = true;
							break;
						}
					}
				}
				if (!bOK)
				{
					return new OperateResult<byte[]>(StringResources.Language.ReceiveDataTimeout);
				}
				return OperateResult.CreateSuccessResult(bufferArray.ToArray());
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				socket?.Close();
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.Send(System.Net.Sockets.Socket,System.Byte[])" />
		protected async Task<OperateResult> SendAsync(Socket socket, byte[] data)
		{
			if (data == null)
			{
				return OperateResult.CreateSuccessResult();
			}
			return await SendAsync(socket, data, 0, data.Length);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.Send(System.Net.Sockets.Socket,System.Byte[],System.Int32,System.Int32)" />
		protected async Task<OperateResult> SendAsync(Socket socket, byte[] data, int offset, int size)
		{
			if (data == null)
			{
				return OperateResult.CreateSuccessResult();
			}

			int alreadyCount = 0;
			try
			{
				do
				{
					int count = await Task.Factory.FromAsync(socket.BeginSend(data, offset, size - alreadyCount, SocketFlags.None, null, socket), (Func<IAsyncResult, int>)socket.EndSend);
					alreadyCount += count;
					offset += count;
				}
				while (alreadyCount < size);
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				socket?.Close();
				if (connectErrorCount < 1000000000)
				{
					connectErrorCount++;
				}
				return new OperateResult<byte[]>(-connectErrorCount, ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveByMessage(System.Net.Sockets.Socket,System.Int32,Communication.Core.IMessage.INetMessage,System.Action{System.Int64,System.Int64})" />
		protected virtual async Task<OperateResult<byte[]>> ReceiveByMessageAsync(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			if (netMessage == null)
			{
				return await ReceiveAsync(socket, -1, timeOut);
			}
			OperateResult<byte[]> headResult = await ReceiveAsync(socket, netMessage.ProtocolHeadBytesLength, timeOut);
			if (!headResult.IsSuccess)
			{
				return headResult;
			}
			netMessage.HeadBytes = headResult.Content;
			int contentLength = netMessage.GetContentLengthByHeadBytes();
			if (contentLength <= 0)
			{
				return OperateResult.CreateSuccessResult(headResult.Content);
			}
			byte[] buffer = new byte[netMessage.ProtocolHeadBytesLength + contentLength];
			headResult.Content.CopyTo(buffer, 0);
			OperateResult<int> contentResult = await ReceiveAsync(socket, buffer, netMessage.ProtocolHeadBytesLength, contentLength, timeOut, reportProgress);
			if (!contentResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(contentResult);
			}
			return OperateResult.CreateSuccessResult(buffer);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReadStream(System.IO.Stream,System.Byte[])" />
		protected async Task<OperateResult<int>> ReadStreamAsync(Stream stream, byte[] buffer)
		{
			try
			{
				return OperateResult.CreateSuccessResult(await stream.ReadAsync(buffer, 0, buffer.Length));
			}
			catch (Exception ex)
			{
				stream?.Close();
				return new OperateResult<int>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.WriteStream(System.IO.Stream,System.Byte[])" />
		protected async Task<OperateResult> WriteStreamAsync(Stream stream, byte[] buffer)
		{

			int alreadyCount = 0;
			try
			{
				await stream.WriteAsync(buffer, alreadyCount, buffer.Length - alreadyCount);
				return OperateResult.CreateSuccessResult(alreadyCount);
			}
			catch (Exception ex)
			{
				stream?.Close();
				return new OperateResult<int>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveLong(System.Net.Sockets.Socket)" />
		private async Task<OperateResult<long>> ReceiveLongAsync(Socket socket)
		{
			OperateResult<byte[]> read = await ReceiveAsync(socket, 8, -1);
			if (read.IsSuccess)
			{
				return OperateResult.CreateSuccessResult(BitConverter.ToInt64(read.Content, 0));
			}
			return OperateResult.CreateFailedResult<long>(read);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendLong(System.Net.Sockets.Socket,System.Int64)" />
		private async Task<OperateResult> SendLongAsync(Socket socket, long value)
		{
			return await SendAsync(socket, BitConverter.GetBytes(value));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendBaseAndCheckReceive(System.Net.Sockets.Socket,System.Int32,System.Int32,System.Byte[])" />
		protected async Task<OperateResult> SendBaseAndCheckReceiveAsync(Socket socket, int headCode, int customer, byte[] send)
		{
			send = HslProtocol.CommandBytes(headCode, customer, Token, send);
			OperateResult sendResult = await SendAsync(socket, send);
			if (!sendResult.IsSuccess)
			{
				return sendResult;
			}
			OperateResult<long> checkResult = await ReceiveLongAsync(socket);
			if (!checkResult.IsSuccess)
			{
				return checkResult;
			}
			if (checkResult.Content != send.Length)
			{
				socket?.Close();
				return new OperateResult(StringResources.Language.CommandLengthCheckFailed);
			}
			return checkResult;
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendBytesAndCheckReceive(System.Net.Sockets.Socket,System.Int32,System.Byte[])" />
		protected async Task<OperateResult> SendBytesAndCheckReceiveAsync(Socket socket, int customer, byte[] send)
		{
			return await SendBaseAndCheckReceiveAsync(socket, 1002, customer, send);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendStringAndCheckReceive(System.Net.Sockets.Socket,System.Int32,System.String)" />
		protected async Task<OperateResult> SendStringAndCheckReceiveAsync(Socket socket, int customer, string send)
		{
			byte[] data = (string.IsNullOrEmpty(send) ? null : Encoding.Unicode.GetBytes(send));
			return await SendBaseAndCheckReceiveAsync(socket, 1001, customer, data);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendStringAndCheckReceive(System.Net.Sockets.Socket,System.Int32,System.String[])" />
		protected async Task<OperateResult> SendStringAndCheckReceiveAsync(Socket socket, int customer, string[] sends)
		{
			return await SendBaseAndCheckReceiveAsync(socket, 1005, customer, HslProtocol.PackStringArrayToByte(sends));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendAccountAndCheckReceive(System.Net.Sockets.Socket,System.Int32,System.String,System.String)" />
		protected async Task<OperateResult> SendAccountAndCheckReceiveAsync(Socket socket, int customer, string name, string pwd)
		{
			return await SendBaseAndCheckReceiveAsync(socket, 5, customer, HslProtocol.PackStringArrayToByte(new string[2] { name, pwd }));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveAndCheckBytes(System.Net.Sockets.Socket,System.Int32)" />
		protected async Task<OperateResult<byte[], byte[]>> ReceiveAndCheckBytesAsync(Socket socket, int timeout)
		{
			OperateResult<byte[]> headResult = await ReceiveAsync(socket, 32, timeout);
			if (!headResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], byte[]>(headResult);
			}
			if (!CheckRemoteToken(headResult.Content))
			{
				socket?.Close();
				return new OperateResult<byte[], byte[]>(StringResources.Language.TokenCheckFailed);
			}
			int contentLength = BitConverter.ToInt32(headResult.Content, 28);
			OperateResult<byte[]> contentResult = await ReceiveAsync(socket, contentLength, timeout);
			if (!contentResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], byte[]>(contentResult);
			}
			OperateResult checkResult = await SendLongAsync(socket, 32 + contentLength);
			if (!checkResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], byte[]>(checkResult);
			}
			byte[] head = headResult.Content;
			byte[] content2 = contentResult.Content;
			content2 = HslProtocol.CommandAnalysis(head, content2);
			return OperateResult.CreateSuccessResult(head, content2);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveStringContentFromSocket(System.Net.Sockets.Socket,System.Int32)" />
		protected async Task<OperateResult<int, string>> ReceiveStringContentFromSocketAsync(Socket socket, int timeOut = 30000)
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync(socket, timeOut);
			if (!receive.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(receive);
			}
			if (BitConverter.ToInt32(receive.Content1, 0) != 1001)
			{
				socket?.Close();
				return new OperateResult<int, string>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			if (receive.Content2 == null)
			{
				receive.Content2 = new byte[0];
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), Encoding.Unicode.GetString(receive.Content2));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveStringArrayContentFromSocket(System.Net.Sockets.Socket,System.Int32)" />
		protected async Task<OperateResult<int, string[]>> ReceiveStringArrayContentFromSocketAsync(Socket socket, int timeOut = 30000)
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync(socket, timeOut);
			if (!receive.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string[]>(receive);
			}
			if (BitConverter.ToInt32(receive.Content1, 0) != 1005)
			{
				socket?.Close();
				return new OperateResult<int, string[]>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			if (receive.Content2 == null)
			{
				receive.Content2 = new byte[4];
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), HslProtocol.UnPackStringArrayFromByte(receive.Content2));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveBytesContentFromSocket(System.Net.Sockets.Socket,System.Int32)" />
		protected async Task<OperateResult<int, byte[]>> ReceiveBytesContentFromSocketAsync(Socket socket, int timeout = 30000)
		{
			OperateResult<byte[], byte[]> receive = await ReceiveAndCheckBytesAsync(socket, timeout);
			if (!receive.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, byte[]>(receive);
			}
			if (BitConverter.ToInt32(receive.Content1, 0) != 1002)
			{
				socket?.Close();
				return new OperateResult<int, byte[]>(StringResources.Language.CommandHeadCodeCheckFailed);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToInt32(receive.Content1, 4), receive.Content2);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendStreamToSocket(System.Net.Sockets.Socket,System.IO.Stream,System.Int64,System.Action{System.Int64,System.Int64},System.Boolean)" />
		protected async Task<OperateResult> SendStreamToSocketAsync(Socket socket, Stream stream, long receive, Action<long, long> report, bool reportByPercent)
		{
			byte[] buffer = new byte[fileCacheSize];
			long SendTotal = 0L;
			long percent = 0L;
			stream.Position = 0L;
			while (SendTotal < receive)
			{
				OperateResult<int> read = await ReadStreamAsync(stream, buffer);
				if (!read.IsSuccess)
				{
					socket?.Close();
					return read;
				}
				SendTotal += read.Content;
				byte[] newBuffer = new byte[read.Content];
				Array.Copy(buffer, 0, newBuffer, 0, newBuffer.Length);
				OperateResult write = await SendBytesAndCheckReceiveAsync(socket, read.Content, newBuffer);
				if (!write.IsSuccess)
				{
					socket?.Close();
					return write;
				}
				if (reportByPercent)
				{
					long percentCurrent = SendTotal * 100 / receive;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke(SendTotal, receive);
					}
				}
				else
				{
					report?.Invoke(SendTotal, receive);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.WriteStreamFromSocket(System.Net.Sockets.Socket,System.IO.Stream,System.Int64,System.Action{System.Int64,System.Int64},System.Boolean)" />
		protected async Task<OperateResult> WriteStreamFromSocketAsync(Socket socket, Stream stream, long totalLength, Action<long, long> report, bool reportByPercent)
		{
			long count_receive = 0L;
			long percent = 0L;
			while (count_receive < totalLength)
			{
				OperateResult<int, byte[]> read = await ReceiveBytesContentFromSocketAsync(socket, 60000);
				if (!read.IsSuccess)
				{
					return read;
				}
				count_receive += read.Content1;
				OperateResult write = await WriteStreamAsync(stream, read.Content2);
				if (!write.IsSuccess)
				{
					socket?.Close();
					return write;
				}
				if (reportByPercent)
				{
					long percentCurrent = count_receive * 100 / totalLength;
					if (percent != percentCurrent)
					{
						percent = percentCurrent;
						report?.Invoke(count_receive, totalLength);
					}
				}
				else
				{
					report?.Invoke(count_receive, totalLength);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 从socket接收一条完整的websocket数据，返回<see cref="T:Communication.WebSocket.WebSocketMessage" />的数据信息<br />
		/// Receive a complete websocket data from the socket, return the data information of the <see cref="T:Communication.WebSocket.WebSocketMessage" />
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>包含websocket消息的结果内容</returns>
		protected OperateResult<WebSocketMessage> ReceiveWebSocketPayload(Socket socket)
		{
			List<byte> list = new List<byte>();
			OperateResult<WebSocketMessage, bool> operateResult;
			do
			{
				operateResult = ReceiveFrameWebSocketPayload(socket);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage>(operateResult);
				}
				list.AddRange(operateResult.Content1.Payload);
			}
			while (!operateResult.Content2);
			return OperateResult.CreateSuccessResult(new WebSocketMessage
			{
				HasMask = operateResult.Content1.HasMask,
				OpCode = operateResult.Content1.OpCode,
				Payload = list.ToArray()
			});
		}

		/// <summary>
		/// 从socket接收一条<see cref="T:Communication.WebSocket.WebSocketMessage" />片段数据，返回<see cref="T:Communication.WebSocket.WebSocketMessage" />的数据信息和是否最后一条数据内容<br />
		/// Receive a piece of <see cref="T:Communication.WebSocket.WebSocketMessage" /> fragment data from the socket, return the data information of <see cref="T:Communication.WebSocket.WebSocketMessage" /> and whether the last data content
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>包含websocket消息的结果内容</returns>
		protected OperateResult<WebSocketMessage, bool> ReceiveFrameWebSocketPayload(Socket socket)
		{
			OperateResult<byte[]> operateResult = Receive(socket, 2, 5000);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<WebSocketMessage, bool>(operateResult);
			}
			bool value = (operateResult.Content[0] & 0x80) == 128;
			bool flag = (operateResult.Content[1] & 0x80) == 128;
			int opCode = operateResult.Content[0] & 0xF;
			byte[] array = null;
			int num = operateResult.Content[1] & 0x7F;
			switch (num)
			{
			case 126:
			{
				OperateResult<byte[]> operateResult3 = Receive(socket, 2, 5000);
				if (!operateResult3.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(operateResult3);
				}
				Array.Reverse(operateResult3.Content);
				num = BitConverter.ToUInt16(operateResult3.Content, 0);
				break;
			}
			case 127:
			{
				OperateResult<byte[]> operateResult2 = Receive(socket, 8, 5000);
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(operateResult2);
				}
				Array.Reverse(operateResult2.Content);
				num = (int)BitConverter.ToUInt64(operateResult2.Content, 0);
				break;
			}
			}
			if (flag)
			{
				OperateResult<byte[]> operateResult4 = Receive(socket, 4, 5000);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(operateResult4);
				}
				array = operateResult4.Content;
			}
			OperateResult<byte[]> operateResult5 = Receive(socket, num);
			if (!operateResult5.IsSuccess)
			{
				return OperateResult.CreateFailedResult<WebSocketMessage, bool>(operateResult5);
			}
			if (flag)
			{
				for (int i = 0; i < operateResult5.Content.Length; i++)
				{
					operateResult5.Content[i] = (byte)(operateResult5.Content[i] ^ array[i % 4]);
				}
			}
			return OperateResult.CreateSuccessResult(new WebSocketMessage
			{
				HasMask = flag,
				OpCode = opCode,
				Payload = operateResult5.Content
			}, value);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveWebSocketPayload(System.Net.Sockets.Socket)" />
		protected async Task<OperateResult<WebSocketMessage>> ReceiveWebSocketPayloadAsync(Socket socket)
		{
			List<byte> data = new List<byte>();
			OperateResult<WebSocketMessage, bool> read;
			do
			{
				read = await ReceiveFrameWebSocketPayloadAsync(socket);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage>(read);
				}
				data.AddRange(read.Content1.Payload);
			}
			while (!read.Content2);
			return OperateResult.CreateSuccessResult(new WebSocketMessage
			{
				HasMask = read.Content1.HasMask,
				OpCode = read.Content1.OpCode,
				Payload = data.ToArray()
			});
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveFrameWebSocketPayload(System.Net.Sockets.Socket)" />
		protected async Task<OperateResult<WebSocketMessage, bool>> ReceiveFrameWebSocketPayloadAsync(Socket socket)
		{
			OperateResult<byte[]> head = await ReceiveAsync(socket, 2, 10000);
			if (!head.IsSuccess)
			{
				return OperateResult.CreateFailedResult<WebSocketMessage, bool>(head);
			}
			bool isEof = (head.Content[0] & 0x80) == 128;
			bool hasMask = (head.Content[1] & 0x80) == 128;
			int opCode = head.Content[0] & 0xF;
			byte[] mask = null;
			int length = head.Content[1] & 0x7F;
			switch (length)
			{
			case 126:
			{
				OperateResult<byte[]> extended2 = await ReceiveAsync(socket, 2, 30000);
				if (!extended2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(extended2);
				}
				Array.Reverse(extended2.Content);
				length = BitConverter.ToUInt16(extended2.Content, 0);
				break;
			}
			case 127:
			{
				OperateResult<byte[]> extended = await ReceiveAsync(socket, 8, 30000);
				if (!extended.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(extended);
				}
				Array.Reverse(extended.Content);
				length = (int)BitConverter.ToUInt64(extended.Content, 0);
				break;
			}
			}
			if (hasMask)
			{
				OperateResult<byte[]> maskResult = await ReceiveAsync(socket, 4, 30000);
				if (!maskResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<WebSocketMessage, bool>(maskResult);
				}
				mask = maskResult.Content;
			}
			OperateResult<byte[]> payload = await ReceiveAsync(socket, length);
			if (!payload.IsSuccess)
			{
				return OperateResult.CreateFailedResult<WebSocketMessage, bool>(payload);
			}
			if (hasMask)
			{
				for (int i = 0; i < payload.Content.Length; i++)
				{
					payload.Content[i] = (byte)(payload.Content[i] ^ mask[i % 4]);
				}
			}
			return OperateResult.CreateSuccessResult(new WebSocketMessage
			{
				HasMask = hasMask,
				OpCode = opCode,
				Payload = payload.Content
			}, isEof);
		}

		/// <summary>
		/// 基于MQTT协议，从网络套接字中接收剩余的数据长度<br />
		/// Receives the remaining data length from the network socket based on the MQTT protocol
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>网络中剩余的长度数据</returns>
		private OperateResult<int> ReceiveMqttRemainingLength(Socket socket)
		{
			List<byte> list = new List<byte>();
			OperateResult<byte[]> operateResult;
			do
			{
				operateResult = Receive(socket, 1, 5000);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<int>(operateResult);
				}
				list.Add(operateResult.Content[0]);
			}
			while (operateResult.Content[0] >= 128 && list.Count < 4);
			if (list.Count > 4)
			{
				return new OperateResult<int>("Receive Length is too long!");
			}
			if (list.Count == 1)
			{
				return OperateResult.CreateSuccessResult((int)list[0]);
			}
			if (list.Count == 2)
			{
				return OperateResult.CreateSuccessResult(list[0] - 128 + list[1] * 128);
			}
			if (list.Count == 3)
			{
				return OperateResult.CreateSuccessResult(list[0] - 128 + (list[1] - 128) * 128 + list[2] * 128 * 128);
			}
			return OperateResult.CreateSuccessResult(list[0] - 128 + (list[1] - 128) * 128 + (list[2] - 128) * 128 * 128 + list[3] * 128 * 128 * 128);
		}

		/// <summary>
		/// 接收一条完整的MQTT协议的报文信息，包含控制码和负载数据<br />
		/// Receive a message of a completed MQTT protocol, including control code and payload data
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="timeOut">超时时间</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <returns>结果数据内容</returns>
		protected OperateResult<byte, byte[]> ReceiveMqttMessage(Socket socket, int timeOut, Action<long, long> reportProgress = null)
		{
			OperateResult<byte[]> operateResult = Receive(socket, 1, timeOut);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(operateResult);
			}
			OperateResult<int> operateResult2 = ReceiveMqttRemainingLength(socket);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(operateResult2);
			}
			if (operateResult.Content[0] >> 4 == 15)
			{
				reportProgress = null;
			}
			if (operateResult.Content[0] >> 4 == 0)
			{
				reportProgress = null;
			}
			OperateResult<byte[]> operateResult3 = Receive(socket, operateResult2.Content, 60000, reportProgress);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content[0], operateResult3.Content);
		}

		/// <summary>
		/// 使用MQTT协议从socket接收指定长度的字节数组，然后全部写入到流中，可以指定进度报告<br />
		/// Use the MQTT protocol to receive a byte array of specified length from the socket, and then write all of them to the stream, and you can specify a progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="fileSize">数据大小</param>
		/// <param name="timeOut">超时时间</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult ReceiveMqttStream(Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			long num = 0L;
			while (num < fileSize)
			{
				OperateResult<byte, byte[]> operateResult = ReceiveMqttMessage(socket, timeOut);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				if (operateResult.Content1 == 0)
				{
					socket?.Close();
					return new OperateResult(Encoding.UTF8.GetString(operateResult.Content2));
				}
				if (aesCryptography != null)
				{
					try
					{
						operateResult.Content2 = aesCryptography.Decrypt(operateResult.Content2);
					}
					catch (Exception ex)
					{
						socket?.Close();
						return new OperateResult("AES Decrypt file stream failed: " + ex.Message);
					}
				}
				OperateResult operateResult2 = WriteStream(stream, operateResult.Content2);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				num += operateResult.Content2.Length;
				byte[] array = new byte[16];
				BitConverter.GetBytes(num).CopyTo(array, 0);
				BitConverter.GetBytes(fileSize).CopyTo(array, 8);
				OperateResult operateResult3 = Send(socket, MqttHelper.BuildMqttCommand(100, null, array).Content);
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				reportProgress?.Invoke(num, fileSize);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 使用MQTT协议将流中的数据读取到字节数组，然后都写入到socket里面，可以指定进度报告，主要用于将文件发送到网络。<br />
		/// Use the MQTT protocol to read the data in the stream into a byte array, and then write them all into the socket. 
		/// You can specify a progress report, which is mainly used to send files to the network.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">流</param>
		/// <param name="fileSize">总的数据大小</param>
		/// <param name="timeOut">超时信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttStream(Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			byte[] array = new byte[fileCacheSize];
			long num = 0L;
			stream.Position = 0L;
			while (num < fileSize)
			{
				OperateResult<int> operateResult = ReadStream(stream, array);
				if (!operateResult.IsSuccess)
				{
					socket?.Close();
					return operateResult;
				}
				num += operateResult.Content;
				OperateResult operateResult2 = Send(socket, MqttHelper.BuildMqttCommand(100, null, array.SelectBegin(operateResult.Content), aesCryptography).Content);
				if (!operateResult2.IsSuccess)
				{
					socket?.Close();
					return operateResult2;
				}
				OperateResult<byte, byte[]> operateResult3 = ReceiveMqttMessage(socket, timeOut);
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				reportProgress?.Invoke(num, fileSize);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 使用MQTT协议将一个文件发送到网络上去，需要指定文件名，保存的文件名，可选指定文件描述信息，进度报告<br />
		/// To send a file to the network using the MQTT protocol, you need to specify the file name, the saved file name, 
		/// optionally specify the file description information, and the progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="filename">文件名称</param>
		/// <param name="servername">对方接收后保存的文件名</param>
		/// <param name="filetag">文件的描述信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttFile(Socket socket, string filename, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			FileInfo fileInfo = new FileInfo(filename);
			if (!File.Exists(filename))
			{
				OperateResult operateResult = Send(socket, MqttHelper.BuildMqttCommand(0, null, Encoding.UTF8.GetBytes(StringResources.Language.FileNotExist)).Content);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				socket?.Close();
				return new OperateResult(StringResources.Language.FileNotExist);
			}
			string[] data = new string[3]
			{
				servername,
				fileInfo.Length.ToString(),
				filetag
			};
			OperateResult operateResult2 = Send(socket, MqttHelper.BuildMqttCommand(100, null, HslProtocol.PackStringArrayToByte(data)).Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte, byte[]> operateResult3 = ReceiveMqttMessage(socket, 60000);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			if (operateResult3.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult(Encoding.UTF8.GetString(operateResult3.Content2));
			}
			try
			{
				OperateResult result = new OperateResult();
				using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					result = SendMqttStream(socket, stream, fileInfo.Length, 60000, reportProgress, aesCryptography);
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult("SendMqttStream Exception -> " + ex.Message);
			}
		}

		/// <summary>
		/// 使用MQTT协议将一个数据流发送到网络上去，需要保存的文件名，可选指定文件描述信息，进度报告<br />
		/// Use the MQTT protocol to send a data stream to the network, the file name that needs to be saved, optional file description information, progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="stream">数据流</param>
		/// <param name="servername">对方接收后保存的文件名</param>
		/// <param name="filetag">文件的描述信息</param>
		/// <param name="reportProgress">进度报告，第一个参数是已完成的字节数量，第二个参数是总字节数量。</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功</returns>
		protected OperateResult SendMqttFile(Socket socket, Stream stream, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			string[] data = new string[3]
			{
				servername,
				stream.Length.ToString(),
				filetag
			};
			OperateResult operateResult = Send(socket, MqttHelper.BuildMqttCommand(100, null, HslProtocol.PackStringArrayToByte(data)).Content);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte, byte[]> operateResult2 = ReceiveMqttMessage(socket, 60000);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult(Encoding.UTF8.GetString(operateResult2.Content2));
			}
			try
			{
				return SendMqttStream(socket, stream, stream.Length, 60000, reportProgress, aesCryptography);
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult("SendMqttStream Exception -> " + ex.Message);
			}
		}

		/// <summary>
		/// 使用MQTT协议从网络接收字节数组，然后写入文件或流中，支持进度报告<br />
		/// Use MQTT protocol to receive byte array from the network, and then write it to file or stream, support progress report
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="source">文件名或是流</param>
		/// <param name="reportProgress">进度报告</param>
		/// <param name="aesCryptography">AES数据加密对象，如果为空，则不进行加密</param>
		/// <returns>是否操作成功，如果成功，携带文件基本信息</returns>
		protected OperateResult<FileBaseInfo> ReceiveMqttFile(Socket socket, object source, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			OperateResult<byte, byte[]> operateResult = ReceiveMqttMessage(socket, 60000);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<FileBaseInfo>(operateResult);
			}
			if (operateResult.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult<FileBaseInfo>(Encoding.UTF8.GetString(operateResult.Content2));
			}
			FileBaseInfo fileBaseInfo = new FileBaseInfo();
			string[] array = HslProtocol.UnPackStringArrayFromByte(operateResult.Content2);
			fileBaseInfo.Name = array[0];
			fileBaseInfo.Size = long.Parse(array[1]);
			fileBaseInfo.Tag = array[2];
			Send(socket, MqttHelper.BuildMqttCommand(100, null, null).Content);
			try
			{
				OperateResult operateResult2 = null;
				string text = source as string;
				if (text != null)
				{
					using (FileStream stream = new FileStream(text, FileMode.Create, FileAccess.Write))
					{
						operateResult2 = ReceiveMqttStream(socket, stream, fileBaseInfo.Size, 60000, reportProgress, aesCryptography);
					}
					if (!operateResult2.IsSuccess)
					{
						if (File.Exists(text))
						{
							File.Delete(text);
						}
						return OperateResult.CreateFailedResult<FileBaseInfo>(operateResult2);
					}
				}
				else
				{
					Stream stream2 = source as Stream;
					if (stream2 == null)
					{
						throw new Exception("Not Supported Type");
					}
					operateResult2 = ReceiveMqttStream(socket, stream2, fileBaseInfo.Size, 60000, reportProgress, aesCryptography);
				}
				return OperateResult.CreateSuccessResult(fileBaseInfo);
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<FileBaseInfo>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveMqttRemainingLength(System.Net.Sockets.Socket)" />
		private async Task<OperateResult<int>> ReceiveMqttRemainingLengthAsync(Socket socket)
		{
			List<byte> buffer = new List<byte>();
			OperateResult<byte[]> rece;
			do
			{
				rece = await ReceiveAsync(socket, 1, 5000);
				if (!rece.IsSuccess)
				{
					return OperateResult.CreateFailedResult<int>(rece);
				}
				buffer.Add(rece.Content[0]);
			}
			while (rece.Content[0] >= 128 && buffer.Count < 4);
			if (buffer.Count > 4)
			{
				return new OperateResult<int>("Receive Length is too long!");
			}
			if (buffer.Count == 1)
			{
				return OperateResult.CreateSuccessResult((int)buffer[0]);
			}
			if (buffer.Count == 2)
			{
				return OperateResult.CreateSuccessResult(buffer[0] - 128 + buffer[1] * 128);
			}
			if (buffer.Count == 3)
			{
				return OperateResult.CreateSuccessResult(buffer[0] - 128 + (buffer[1] - 128) * 128 + buffer[2] * 128 * 128);
			}
			return OperateResult.CreateSuccessResult(buffer[0] - 128 + (buffer[1] - 128) * 128 + (buffer[2] - 128) * 128 * 128 + buffer[3] * 128 * 128 * 128);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveMqttMessage(System.Net.Sockets.Socket,System.Int32,System.Action{System.Int64,System.Int64})" />
		protected async Task<OperateResult<byte, byte[]>> ReceiveMqttMessageAsync(Socket socket, int timeOut, Action<long, long> reportProgress = null)
		{
			OperateResult<byte[]> readCode = await ReceiveAsync(socket, 1, timeOut);
			if (!readCode.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(readCode);
			}
			OperateResult<int> readContentLength = await ReceiveMqttRemainingLengthAsync(socket);
			if (!readContentLength.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(readContentLength);
			}
			if (readCode.Content[0] >> 4 == 15)
			{
				reportProgress = null;
			}
			if (readCode.Content[0] >> 4 == 0)
			{
				reportProgress = null;
			}
			OperateResult<byte[]> readContent = await ReceiveAsync(socket, readContentLength.Content, timeOut, reportProgress);
			if (!readContent.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte, byte[]>(readContent);
			}
			return OperateResult.CreateSuccessResult(readCode.Content[0], readContent.Content);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveMqttStream(System.Net.Sockets.Socket,System.IO.Stream,System.Int64,System.Int32,System.Action{System.Int64,System.Int64},Communication.Core.Security.AesCryptography)" />
		protected async Task<OperateResult> ReceiveMqttStreamAsync(Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			long already = 0L;
			while (already < fileSize)
			{
				OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync(socket, timeOut);
				if (!receive.IsSuccess)
				{
					return receive;
				}
				if (receive.Content1 == 0)
				{
					socket?.Close();
					return new OperateResult(Encoding.UTF8.GetString(receive.Content2));
				}
				if (aesCryptography != null)
				{
					try
					{
						receive.Content2 = aesCryptography.Decrypt(receive.Content2);
					}
					catch (Exception ex2)
					{
						Exception ex = ex2;
						socket?.Close();
						return new OperateResult("AES Decrypt file stream failed: " + ex.Message);
					}
				}
				OperateResult write = await WriteStreamAsync(stream, receive.Content2);
				if (!write.IsSuccess)
				{
					return write;
				}
				already += receive.Content2.Length;
				byte[] ack = new byte[16];
				BitConverter.GetBytes(already).CopyTo(ack, 0);
				BitConverter.GetBytes(fileSize).CopyTo(ack, 8);
				OperateResult send = await SendAsync(socket, MqttHelper.BuildMqttCommand(100, null, ack).Content);
				if (!send.IsSuccess)
				{
					return send;
				}
				reportProgress?.Invoke(already, fileSize);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendMqttStream(System.Net.Sockets.Socket,System.IO.Stream,System.Int64,System.Int32,System.Action{System.Int64,System.Int64},Communication.Core.Security.AesCryptography)" />
		protected async Task<OperateResult> SendMqttStreamAsync(Socket socket, Stream stream, long fileSize, int timeOut, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			byte[] buffer = new byte[fileCacheSize];
			long already = 0L;
			stream.Position = 0L;
			while (already < fileSize)
			{
				OperateResult<int> read = await ReadStreamAsync(stream, buffer);
				if (!read.IsSuccess)
				{
					socket?.Close();
					return read;
				}
				already += read.Content;
				OperateResult write = await SendAsync(socket, MqttHelper.BuildMqttCommand(100, null, buffer.SelectBegin(read.Content), aesCryptography).Content);
				if (!write.IsSuccess)
				{
					socket?.Close();
					return write;
				}
				OperateResult<byte, byte[]> receive = await ReceiveMqttMessageAsync(socket, timeOut);
				if (!receive.IsSuccess)
				{
					return receive;
				}
				reportProgress?.Invoke(already, fileSize);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendMqttFile(System.Net.Sockets.Socket,System.String,System.String,System.String,System.Action{System.Int64,System.Int64},Communication.Core.Security.AesCryptography)" />
		protected async Task<OperateResult> SendMqttFileAsync(Socket socket, string filename, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			FileInfo info = new FileInfo(filename);
			if (!File.Exists(filename))
			{
				OperateResult notFoundResult = await SendAsync(socket, MqttHelper.BuildMqttCommand(0, null, Encoding.UTF8.GetBytes(StringResources.Language.FileNotExist)).Content);
				if (!notFoundResult.IsSuccess)
				{
					return notFoundResult;
				}
				socket?.Close();
				return new OperateResult(StringResources.Language.FileNotExist);
			}
			string[] array = new string[3]
			{
				servername,
				info.Length.ToString(),
				filetag
			};
			OperateResult sendResult = await SendAsync(socket, MqttHelper.BuildMqttCommand(100, null, HslProtocol.PackStringArrayToByte(array)).Content);
			if (!sendResult.IsSuccess)
			{
				return sendResult;
			}
			OperateResult<byte, byte[]> check = await ReceiveMqttMessageAsync(socket, 60000);
			if (!check.IsSuccess)
			{
				return check;
			}
			if (check.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult(Encoding.UTF8.GetString(check.Content2));
			}
			try
			{
				OperateResult result = new OperateResult();
				using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					result = await SendMqttStreamAsync(socket, fs, info.Length, 60000, reportProgress, aesCryptography);
				}
				return result;
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult("SendMqttStreamAsync Exception -> " + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.SendMqttFile(System.Net.Sockets.Socket,System.IO.Stream,System.String,System.String,System.Action{System.Int64,System.Int64},Communication.Core.Security.AesCryptography)" />
		protected async Task<OperateResult> SendMqttFileAsync(Socket socket, Stream stream, string servername, string filetag, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			string[] array = new string[3]
			{
				servername,
				stream.Length.ToString(),
				filetag
			};
			OperateResult sendResult = await SendAsync(socket, MqttHelper.BuildMqttCommand(100, null, HslProtocol.PackStringArrayToByte(array)).Content);
			if (!sendResult.IsSuccess)
			{
				return sendResult;
			}
			OperateResult<byte, byte[]> check = await ReceiveMqttMessageAsync(socket, 60000);
			if (!check.IsSuccess)
			{
				return check;
			}
			if (check.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult(Encoding.UTF8.GetString(check.Content2));
			}
			try
			{
				return await SendMqttStreamAsync(socket, stream, stream.Length, 60000, reportProgress, aesCryptography);
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult("SendMqttStreamAsync Exception -> " + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveMqttFile(System.Net.Sockets.Socket,System.Object,System.Action{System.Int64,System.Int64},Communication.Core.Security.AesCryptography)" />
		protected async Task<OperateResult<FileBaseInfo>> ReceiveMqttFileAsync(Socket socket, object source, Action<long, long> reportProgress = null, AesCryptography aesCryptography = null)
		{
			OperateResult<byte, byte[]> receiveFileInfo = await ReceiveMqttMessageAsync(socket, 60000);
			if (!receiveFileInfo.IsSuccess)
			{
				return OperateResult.CreateFailedResult<FileBaseInfo>(receiveFileInfo);
			}
			if (receiveFileInfo.Content1 == 0)
			{
				socket?.Close();
				return new OperateResult<FileBaseInfo>(Encoding.UTF8.GetString(receiveFileInfo.Content2));
			}
			FileBaseInfo fileBaseInfo = new FileBaseInfo();
			string[] array = HslProtocol.UnPackStringArrayFromByte(receiveFileInfo.Content2);
			if (array.Length < 3)
			{
				socket?.Close();
				return new OperateResult<FileBaseInfo>("FileBaseInfo Check failed: " + array.ToArrayString());
			}
			fileBaseInfo.Name = array[0];
			fileBaseInfo.Size = long.Parse(array[1]);
			fileBaseInfo.Tag = array[2];
			await SendAsync(socket, MqttHelper.BuildMqttCommand(100, null, null).Content);
			try
			{
				OperateResult write = null;
				string savename = source as string;
				if (savename != null)
				{
					using (FileStream fs = new FileStream(savename, FileMode.Create, FileAccess.Write))
					{
						write = await ReceiveMqttStreamAsync(socket, fs, fileBaseInfo.Size, 60000, reportProgress, aesCryptography);
					}
					if (!write.IsSuccess)
					{
						if (File.Exists(savename))
						{
							File.Delete(savename);
						}
						return OperateResult.CreateFailedResult<FileBaseInfo>(write);
					}
				}
				else
				{
					Stream stream = source as Stream;
					if (stream == null)
					{
						throw new Exception("Not Supported Type");
					}
					await ReceiveMqttStreamAsync(socket, stream, fileBaseInfo.Size, 60000, reportProgress, aesCryptography);
				}
				return OperateResult.CreateSuccessResult(fileBaseInfo);
			}
			catch (Exception ex)
			{
				socket?.Close();
				return new OperateResult<FileBaseInfo>(ex.Message);
			}
		}

		/// <summary>
		/// 接收一行基于redis协议的字符串的信息，需要指定固定的长度<br />
		/// Receive a line of information based on the redis protocol string, you need to specify a fixed length
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="length">字符串的长度</param>
		/// <returns>带有结果对象的数据信息</returns>
		protected OperateResult<byte[]> ReceiveRedisCommandString(Socket socket, int length)
		{
			List<byte> list = new List<byte>();
			OperateResult<byte[]> operateResult = Receive(socket, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			list.AddRange(operateResult.Content);
			OperateResult<byte[]> operateResult2 = ReceiveCommandLineFromSocket(socket, 10);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			list.AddRange(operateResult2.Content);
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// 从网络接收一条完整的redis报文的消息<br />
		/// Receive a complete redis message from the network
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>接收的结果对象</returns>
		protected OperateResult<byte[]> ReceiveRedisCommand(Socket socket)
		{
			List<byte> list = new List<byte>();
			OperateResult<byte[]> operateResult = ReceiveCommandLineFromSocket(socket, 10);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			list.AddRange(operateResult.Content);
			if (operateResult.Content[0] == 43 || operateResult.Content[0] == 45 || operateResult.Content[0] == 58)
			{
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			if (operateResult.Content[0] == 36)
			{
				OperateResult<int> numberFromCommandLine = RedisHelper.GetNumberFromCommandLine(operateResult.Content);
				if (!numberFromCommandLine.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(numberFromCommandLine);
				}
				if (numberFromCommandLine.Content < 0)
				{
					return OperateResult.CreateSuccessResult(list.ToArray());
				}
				OperateResult<byte[]> operateResult2 = ReceiveRedisCommandString(socket, numberFromCommandLine.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				list.AddRange(operateResult2.Content);
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			if (operateResult.Content[0] == 42)
			{
				OperateResult<int> numberFromCommandLine2 = RedisHelper.GetNumberFromCommandLine(operateResult.Content);
				if (!numberFromCommandLine2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(numberFromCommandLine2);
				}
				for (int i = 0; i < numberFromCommandLine2.Content; i++)
				{
					OperateResult<byte[]> operateResult3 = ReceiveRedisCommand(socket);
					if (!operateResult3.IsSuccess)
					{
						return operateResult3;
					}
					list.AddRange(operateResult3.Content);
				}
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			return new OperateResult<byte[]>("Not Supported HeadCode: " + operateResult.Content[0]);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveRedisCommandString(System.Net.Sockets.Socket,System.Int32)" />
		protected async Task<OperateResult<byte[]>> ReceiveRedisCommandStringAsync(Socket socket, int length)
		{
			List<byte> bufferArray = new List<byte>();
			OperateResult<byte[]> receive = await ReceiveAsync(socket, length);
			if (!receive.IsSuccess)
			{
				return receive;
			}
			bufferArray.AddRange(receive.Content);
			OperateResult<byte[]> commandTail = await ReceiveCommandLineFromSocketAsync(socket, 10);
			if (!commandTail.IsSuccess)
			{
				return commandTail;
			}
			bufferArray.AddRange(commandTail.Content);
			return OperateResult.CreateSuccessResult(bufferArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveRedisCommand(System.Net.Sockets.Socket)" />
		protected async Task<OperateResult<byte[]>> ReceiveRedisCommandAsync(Socket socket)
		{
			List<byte> bufferArray = new List<byte>();
			OperateResult<byte[]> readCommandLine = await ReceiveCommandLineFromSocketAsync(socket, 10);
			if (!readCommandLine.IsSuccess)
			{
				return readCommandLine;
			}
			bufferArray.AddRange(readCommandLine.Content);
			if (readCommandLine.Content[0] == 43 || readCommandLine.Content[0] == 45 || readCommandLine.Content[0] == 58)
			{
				return OperateResult.CreateSuccessResult(bufferArray.ToArray());
			}
			if (readCommandLine.Content[0] == 36)
			{
				OperateResult<int> lengthResult2 = RedisHelper.GetNumberFromCommandLine(readCommandLine.Content);
				if (!lengthResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(lengthResult2);
				}
				if (lengthResult2.Content < 0)
				{
					return OperateResult.CreateSuccessResult(bufferArray.ToArray());
				}
				OperateResult<byte[]> receiveContent = await ReceiveRedisCommandStringAsync(socket, lengthResult2.Content);
				if (!receiveContent.IsSuccess)
				{
					return receiveContent;
				}
				bufferArray.AddRange(receiveContent.Content);
				return OperateResult.CreateSuccessResult(bufferArray.ToArray());
			}
			if (readCommandLine.Content[0] == 42)
			{
				OperateResult<int> lengthResult = RedisHelper.GetNumberFromCommandLine(readCommandLine.Content);
				if (!lengthResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(lengthResult);
				}
				for (int i = 0; i < lengthResult.Content; i++)
				{
					OperateResult<byte[]> receiveCommand = await ReceiveRedisCommandAsync(socket);
					if (!receiveCommand.IsSuccess)
					{
						return receiveCommand;
					}
					bufferArray.AddRange(receiveCommand.Content);
				}
				return OperateResult.CreateSuccessResult(bufferArray.ToArray());
			}
			return new OperateResult<byte[]>("Not Supported HeadCode: " + readCommandLine.Content[0]);
		}

		/// <summary>
		/// 接收一条hsl协议的数据信息，自动解析，解压，解码操作，获取最后的实际的数据，接收结果依次为暗号，用户码，负载数据<br />
		/// Receive a piece of hsl protocol data information, automatically parse, decompress, and decode operations to obtain the last actual data. 
		/// The result is a opCode, user code, and payload data in order.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <returns>接收结果，依次为暗号，用户码，负载数据</returns>
		protected OperateResult<int, int, byte[]> ReceiveHslMessage(Socket socket)
		{
			OperateResult<byte[]> operateResult = Receive(socket, 32, 10000);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, int, byte[]>(operateResult);
			}
			int length = BitConverter.ToInt32(operateResult.Content, operateResult.Content.Length - 4);
			OperateResult<byte[]> operateResult2 = Receive(socket, length);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, int, byte[]>(operateResult2);
			}
			byte[] value = HslProtocol.CommandAnalysis(operateResult.Content, operateResult2.Content);
			int value2 = BitConverter.ToInt32(operateResult.Content, 0);
			int value3 = BitConverter.ToInt32(operateResult.Content, 4);
			return OperateResult.CreateSuccessResult(value2, value3, value);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveHslMessage(System.Net.Sockets.Socket)" />
		protected async Task<OperateResult<int, int, byte[]>> ReceiveHslMessageAsync(Socket socket)
		{
			OperateResult<byte[]> receiveHead = await ReceiveAsync(socket, 32, 10000);
			if (!receiveHead.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, int, byte[]>(receiveHead);
			}
			int receive_length = BitConverter.ToInt32(receiveHead.Content, receiveHead.Content.Length - 4);
			OperateResult<byte[]> receiveContent = await ReceiveAsync(socket, receive_length);
			if (!receiveContent.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, int, byte[]>(receiveContent);
			}
			byte[] Content = HslProtocol.CommandAnalysis(receiveHead.Content, receiveContent.Content);
			int protocol = BitConverter.ToInt32(receiveHead.Content, 0);
			int customer = BitConverter.ToInt32(receiveHead.Content, 4);
			return OperateResult.CreateSuccessResult(protocol, customer, Content);
		}

		/// <summary>
		/// 从Socket接收一条VigorPLC的消息数据信息，指定套接字对象及超时时间
		/// </summary>
		/// <param name="socket">套接字对象</param>
		/// <param name="timeOut">超时时间</param>
		/// <returns>接收的结果内容</returns>
		protected internal OperateResult<byte[]> ReceiveVigorMessage(Socket socket, int timeOut)
		{
			MemoryStream memoryStream = new MemoryStream();
			while (true)
			{
				OperateResult<byte[]> operateResult = Receive(socket, 1, timeOut);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				memoryStream.WriteByte(operateResult.Content[0]);
				if (operateResult.Content[0] == 16)
				{
					OperateResult<byte[]> operateResult2 = Receive(socket, 1, timeOut);
					if (!operateResult2.IsSuccess)
					{
						return operateResult2;
					}
					memoryStream.WriteByte(operateResult2.Content[0]);
					if (operateResult2.Content[0] == 3)
					{
						break;
					}
				}
			}
			OperateResult<byte[]> operateResult3 = Receive(socket, 2, timeOut);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			memoryStream.Write(operateResult3.Content, 0, operateResult3.Content.Length);
			return OperateResult.CreateSuccessResult(memoryStream.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkBase.ReceiveVigorMessage(System.Net.Sockets.Socket,System.Int32)" />
		protected internal async Task<OperateResult<byte[]>> ReceiveVigorMessageAsync(Socket socket, int timeOut)
		{
			MemoryStream ms = new MemoryStream();
			while (true)
			{
				OperateResult<byte[]> read1 = await ReceiveAsync(socket, 1, timeOut);
				if (!read1.IsSuccess)
				{
					return read1;
				}
				ms.WriteByte(read1.Content[0]);
				if (read1.Content[0] == 16)
				{
					OperateResult<byte[]> read2 = await ReceiveAsync(socket, 1, timeOut);
					if (!read2.IsSuccess)
					{
						return read2;
					}
					ms.WriteByte(read2.Content[0]);
					if (read2.Content[0] == 3)
					{
						break;
					}
				}
			}
			OperateResult<byte[]> read3 = await ReceiveAsync(socket, 2, timeOut);
			if (!read3.IsSuccess)
			{
				return read3;
			}
			ms.Write(read3.Content, 0, read3.Content.Length);
			return OperateResult.CreateSuccessResult(ms.ToArray());
		}

		/// <summary>
		/// 删除文件的操作<br />
		/// Delete file operation
		/// </summary>
		/// <param name="filename">完整的真实的文件路径</param>
		/// <returns>是否删除成功</returns>
		protected bool DeleteFileByName(string filename)
		{
			try
			{
				if (!File.Exists(filename))
				{
					return true;
				}
				File.Delete(filename);
				return true;
			}
			catch (Exception ex)
			{
				LogNet?.WriteException(ToString(), "delete file failed:" + filename, ex);
				return false;
			}
		}

		/// <summary>
		/// 预处理文件夹的名称，除去文件夹名称最后一个'\'或'/'，如果有的话<br />
		/// Preprocess the name of the folder, removing the last '\' or '/' in the folder name
		/// </summary>
		/// <param name="folder">文件夹名称</param>
		/// <returns>返回处理之后的名称</returns>
		protected string PreprocessFolderName(string folder)
		{
			if (folder.EndsWith("\\") || folder.EndsWith("/"))
			{
				return folder.Substring(0, folder.Length - 1);
			}
			return folder;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "NetworkBase";
		}

		/// <summary>
		/// 通过主机名或是IP地址信息，获取到真实的IP地址信息<br />
		/// Obtain the real IP address information through the host name or IP address information
		/// </summary>
		/// <param name="hostName">主机名或是IP地址</param>
		/// <returns>IP地址信息</returns>
		public static string GetIpAddressHostName(string hostName)
		{
			IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
			IPAddress iPAddress = hostEntry.AddressList[0];
			return iPAddress.ToString();
		}
	}
}

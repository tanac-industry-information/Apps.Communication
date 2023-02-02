using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// Udp服务器程序的基础类，提供了启动服务器的基本实现，方便后续的扩展操作。<br />
	/// The basic class of the udp server program provides the basic implementation of starting the server to facilitate subsequent expansion operations.
	/// </summary>
	public class NetworkUdpServerBase : NetworkXBase
	{
		private Thread threadReceive;

		/// <summary>
		/// 服务器引擎是否启动<br />
		/// Whether the server engine is started
		/// </summary>
		public bool IsStarted { get; protected set; }

		/// <summary>
		/// 获取或设置服务器的端口号，如果是设置，需要在服务器启动前设置完成，才能生效。<br />
		/// Gets or sets the port number of the server. If it is set, it needs to be set before the server starts to take effect.
		/// </summary>
		/// <remarks>需要在服务器启动之前设置为有效</remarks>
		public int Port { get; set; }

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public NetworkUdpServerBase()
		{
			IsStarted = false;
			Port = 0;
		}

		/// <summary>
		/// 后台接收数据的线程
		/// </summary>
		protected virtual void ThreadReceiveCycle()
		{
			IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, 0);
			EndPoint remoteEP = iPEndPoint;
			while (IsStarted)
			{
				byte[] buffer = new byte[1024];
				int num = 0;
				try
				{
					num = CoreSocket.ReceiveFrom(buffer, ref remoteEP);
				}
				catch (Exception ex)
				{
					base.LogNet?.WriteException("ThreadReceiveCycle", ex);
				}
				Console.WriteLine(DateTime.Now.ToString() + " :ReceiveData");
			}
		}

		/// <summary>
		/// 当客户端的socket登录的时候额外检查的操作，并返回操作的结果信息。<br />
		/// The operation is additionally checked when the client's socket logs in, and the result information of the operation is returned.
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="endPoint">终结点</param>
		/// <returns>验证的结果</returns>
		protected virtual OperateResult SocketAcceptExtraCheck(Socket socket, IPEndPoint endPoint)
		{
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 服务器启动时额外的初始化信息，可以用于启动一些额外的服务的操作。<br />
		/// The extra initialization information when the server starts can be used to start some additional service operations.
		/// </summary>
		/// <remarks>需要在派生类中重写</remarks>
		protected virtual void StartInitialization()
		{
		}

		/// <summary>
		/// 指定端口号来启动服务器的引擎<br />
		/// Specify the port number to start the server's engine
		/// </summary>
		/// <param name="port">指定一个端口号</param>
		public virtual void ServerStart(int port)
		{
			if (!IsStarted)
			{
				StartInitialization();
				CoreSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				CoreSocket.Bind(new IPEndPoint(IPAddress.Any, port));
				threadReceive = new Thread(ThreadReceiveCycle)
				{
					IsBackground = true
				};
				threadReceive.Start();
				IsStarted = true;
				Port = port;
				base.LogNet?.WriteNewLine();
				base.LogNet?.WriteInfo(ToString(), StringResources.Language.NetEngineStart);
			}
		}

		/// <summary>
		/// 使用已经配置好的端口启动服务器的引擎<br />
		/// Use the configured port to start the server's engine
		/// </summary>
		public void ServerStart()
		{
			ServerStart(Port);
		}

		/// <summary>
		/// 服务器关闭的时候需要做的事情<br />
		/// Things to do when the server is down
		/// </summary>
		protected virtual void CloseAction()
		{
		}

		/// <summary>
		/// 关闭服务器的引擎<br />
		/// Shut down the server's engine
		/// </summary>
		public virtual void ServerClose()
		{
			if (IsStarted)
			{
				CloseAction();
				CoreSocket?.Close();
				IsStarted = false;
				base.LogNet?.WriteInfo(ToString(), StringResources.Language.NetEngineClose);
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"NetworkUdpServerBase[{Port}]";
		}
	}
}

using System;
using System.Net;
using System.Net.Sockets;

namespace Apps.Communication.Core.Pipe
{
	/// <summary>
	/// 基于网络通信的管道信息，可以设置额外的一些参数信息，例如连接超时时间，读取超时时间等等。<br />
	/// Based on the pipe information of network communication, some additional parameter information can be set, such as connection timeout time, read timeout time and so on.
	/// </summary>
	public class PipeSocket : PipeBase, IDisposable
	{
		private string ipAddress = "127.0.0.1";

		private int port = 2000;

		private Socket socket;

		private int receiveTimeOut = 5000;

		private int connectTimeOut = 10000;

		private int sleepTime = 0;

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.LocalBinding" />
		public IPEndPoint LocalBinding { get; set; }

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.IpAddress" />
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

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.Port" />
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
		/// 指示长连接的套接字是否处于错误的状态<br />
		/// Indicates if the long-connected socket is in the wrong state
		/// </summary>
		public bool IsSocketError { get; set; }

		/// <summary>
		/// 获取或设置当前的客户端用于服务器连接的套接字。<br />
		/// Gets or sets the socket currently used by the client for server connection.
		/// </summary>
		public Socket Socket
		{
			get
			{
				return socket;
			}
			set
			{
				socket = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ReceiveTimeOut" />
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

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ReceiveTimeOut" />
		public int ReceiveTimeOut
		{
			get
			{
				return receiveTimeOut;
			}
			set
			{
				receiveTimeOut = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.SleepTime" />
		public int SleepTime
		{
			get
			{
				return sleepTime;
			}
			set
			{
				sleepTime = value;
			}
		}

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public PipeSocket()
		{
		}

		/// <summary>
		/// 通过指定的IP地址和端口号来实例化一个对象<br />
		/// Instantiate an object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号</param>
		public PipeSocket(string ipAddress, int port)
		{
			this.ipAddress = ipAddress;
			this.port = port;
		}

		/// <summary>
		/// 获取当前的连接状态是否有效<br />
		/// Get the current connection status is valid
		/// </summary>
		/// <returns>如果有效，返回 True, 否则返回 False</returns>
		public bool IsConnectitonError()
		{
			return IsSocketError || socket == null;
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public override void Dispose()
		{
			base.Dispose();
			socket?.Close();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"PipeSocket[{ipAddress}:{port}]";
		}
	}
}

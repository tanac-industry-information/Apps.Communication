using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 基于Udp的应答式通信类<br />
	/// Udp - based responsive communication class
	/// </summary>
	public class NetworkUdpBase : NetworkBase
	{
		/// <inheritdoc cref="F:Communication.Core.Net.NetworkDoubleBase.LogMsgFormatBinary" />
		protected bool LogMsgFormatBinary = true;

		private SimpleHybirdLock hybirdLock = null;

		private int connectErrorCount = 0;

		private string ipAddress = "127.0.0.1";

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.IpAddress" />
		public virtual string IpAddress
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
		public virtual int Port { get; set; }

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ReceiveTimeOut" />
		public int ReceiveTimeout { get; set; }

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ConnectionId" />
		public string ConnectionId { get; set; }

		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度，特殊情况的时候需要调整<br />
		/// Gets or sets the length of data received at a time. The default length is 2KB
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;


		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.LocalBinding" />
		public IPEndPoint LocalBinding { get; set; }

		/// <summary>
		/// 实例化一个默认的方法<br />
		/// Instantiate a default method
		/// </summary>
		public NetworkUdpBase()
		{
			hybirdLock = new SimpleHybirdLock();
			ReceiveTimeout = 5000;
			ConnectionId = SoftBasic.GetUniqueStringByGuidAndRandom();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.PackCommandWithHeader(System.Byte[])" />
		protected virtual byte[] PackCommandWithHeader(byte[] command)
		{
			return command;
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.UnpackResponseContent(System.Byte[],System.Byte[])" />
		protected virtual OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return OperateResult.CreateSuccessResult(response);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkUdpBase.ReadFromCoreServer(System.Byte[],System.Boolean,System.Boolean)" />
		public virtual OperateResult<byte[]> ReadFromCoreServer(byte[] send)
		{
			return ReadFromCoreServer(send, hasResponseData: true, usePackAndUnpack: true);
		}

		/// <summary>
		/// 核心的数据交互读取，发数据发送到通道上去，然后从通道上接收返回的数据<br />
		/// The core data is read interactively, the data is sent to the serial port, and the returned data is received from the serial port
		/// </summary>
		/// <param name="send">完整的报文内容</param>
		/// <param name="hasResponseData">是否有等待的数据返回，默认为 true</param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="M:Communication.Core.Net.NetworkUdpBase.PackCommandWithHeader(System.Byte[])" />方法后才会有影响</param>
		/// <returns>是否成功的结果对象</returns>
		public virtual OperateResult<byte[]> ReadFromCoreServer(byte[] send, bool hasResponseData, bool usePackAndUnpack)
		{

			byte[] array = (usePackAndUnpack ? PackCommandWithHeader(send) : send);
			base.LogNet?.WriteDebug(ToString(), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? SoftBasic.ByteToHexString(array) : Encoding.ASCII.GetString(array)));
			hybirdLock.Enter();
			try
			{
				IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
				Socket socket = new Socket(iPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
				if (LocalBinding != null)
				{
					socket.Bind(LocalBinding);
				}
				socket.SendTo(array, array.Length, SocketFlags.None, iPEndPoint);
				if (ReceiveTimeout < 0)
				{
					hybirdLock.Leave();
					return OperateResult.CreateSuccessResult(new byte[0]);
				}
				if (!hasResponseData)
				{
					hybirdLock.Leave();
					return OperateResult.CreateSuccessResult(new byte[0]);
				}
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, ReceiveTimeout);
				IPEndPoint iPEndPoint2 = new IPEndPoint((iPEndPoint.AddressFamily == AddressFamily.InterNetworkV6) ? IPAddress.IPv6Any : IPAddress.Any, 0);
				EndPoint remoteEP = iPEndPoint2;
				byte[] array2 = new byte[ReceiveCacheLength];
				int length = socket.ReceiveFrom(array2, ref remoteEP);
				byte[] array3 = array2.SelectBegin(length);
				hybirdLock.Leave();
				base.LogNet?.WriteDebug(ToString(), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? SoftBasic.ByteToHexString(array3) : Encoding.ASCII.GetString(array3)));
				connectErrorCount = 0;
				return usePackAndUnpack ? UnpackResponseContent(array, array3) : OperateResult.CreateSuccessResult(array3);
			}
			catch (Exception ex)
			{
				hybirdLock.Leave();
				if (connectErrorCount < 100000000)
				{
					connectErrorCount++;
				}
				return new OperateResult<byte[]>(-connectErrorCount, ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkUdpBase.ReadFromCoreServer(System.Byte[],System.Boolean,System.Boolean)" />
		public async Task<OperateResult<byte[]>> ReadFromCoreServerAsync(byte[] value)
		{
			return await Task.Run(() => ReadFromCoreServer(value));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.IpAddressPing" />
		public IPStatus IpAddressPing()
		{
			Ping ping = new Ping();
			return ping.Send(IpAddress).Status;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"NetworkUdpBase[{IpAddress}:{Port}]";
		}
	}
}

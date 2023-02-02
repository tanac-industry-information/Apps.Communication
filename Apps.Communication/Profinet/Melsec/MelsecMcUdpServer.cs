using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Melsec
{
	/// <inheritdoc />
	public class MelsecMcUdpServer : MelsecMcServer
	{
		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;


		/// <summary>
		/// 实例化一个默认参数的mc协议的服务器<br />
		/// Instantiate a mc protocol server with default parameters
		/// </summary>
		/// <param name="isBinary">是否是二进制，默认是二进制，否则是ASCII格式</param>
		public MelsecMcUdpServer(bool isBinary = true)
			: base(isBinary)
		{
		}

		/// <inheritdoc />
		public override void ServerStart(int port)
		{
			if (!base.IsStarted)
			{
				StartInitialization();
				CoreSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				CoreSocket.Bind(new IPEndPoint(IPAddress.Any, port));
				RefreshReceive();
				base.IsStarted = true;
				base.Port = port;
				base.LogNet?.WriteInfo(ToString(), StringResources.Language.NetEngineStart);
			}
		}

		/// <summary>
		/// 重新开始接收数据
		/// </summary>
		private void RefreshReceive()
		{
			AppSession appSession = new AppSession(CoreSocket);
			appSession.UdpEndPoint = new IPEndPoint(IPAddress.Any, 0);
			appSession.BytesBuffer = new byte[ReceiveCacheLength];
			CoreSocket.BeginReceiveFrom(appSession.BytesBuffer, 0, ReceiveCacheLength, SocketFlags.None, ref appSession.UdpEndPoint, AsyncCallback, appSession);
		}

		private void AsyncCallback(IAsyncResult ar)
		{
			AppSession appSession = ar.AsyncState as AppSession;
			if (appSession == null)
			{
				return;
			}
			try
			{
				int num = appSession.WorkSocket.EndReceiveFrom(ar, ref appSession.UdpEndPoint);
				RefreshReceive();
				byte[] array = new byte[num];
				Array.Copy(appSession.BytesBuffer, 0, array, 0, num);
				byte[] array2 = null;
				array2 = ((!base.IsBinary) ? ReadFromMcAsciiCore(array.RemoveBegin(22)) : ReadFromMcCore(array.RemoveBegin(11)));
				base.LogNet?.WriteDebug(ToString(), "Udp " + StringResources.Language.Receive + "：" + (base.IsBinary ? array.ToHexString(' ') : Encoding.ASCII.GetString(array)));
				if (array2 != null)
				{
					appSession.WorkSocket.SendTo(array2, array2.Length, SocketFlags.None, appSession.UdpEndPoint);
					base.LogNet?.WriteDebug(ToString(), "Udp " + StringResources.Language.Send + "：" + (base.IsBinary ? array2.ToHexString(' ') : Encoding.ASCII.GetString(array2)));
					RaiseDataReceived(appSession, array);
				}
				else
				{
					RemoveClient(appSession);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex2)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.SocketEndReceiveException, ex2);
				RefreshReceive();
			}
			finally
			{
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecMcUdpServer[{base.Port}]";
		}
	}
}

using System;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Omron
{
	/// <inheritdoc cref="T:Communication.Profinet.Omron.OmronFinsServer" />
	public class OmronFinsUdpServer : OmronFinsServer
	{
		/// <summary>
		/// 获取或设置一次接收时的数据长度，默认2KB数据长度
		/// </summary>
		public int ReceiveCacheLength { get; set; } = 2048;


		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public OmronFinsUdpServer()
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

		/// <inheritdoc />
		protected override byte[] PackCommand(int status, byte[] finsCore, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[14 + data.Length];
			SoftBasic.HexStringToBytes("00 00 00 00 00 00 00 00 00 00 00 00 00 00").CopyTo(array, 0);
			if (data.Length != 0)
			{
				data.CopyTo(array, 14);
			}
			array[10] = finsCore[0];
			array[11] = finsCore[1];
			array[12] = BitConverter.GetBytes(status)[1];
			array[13] = BitConverter.GetBytes(status)[0];
			return array;
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
				base.LogNet?.WriteDebug(ToString(), "Udp " + StringResources.Language.Receive + "：" + array.ToHexString(' '));
				byte[] array2 = ReadFromFinsCore(array.RemoveBegin(10));
				if (array2 != null)
				{
					appSession.WorkSocket.SendTo(array2, array2.Length, SocketFlags.None, appSession.UdpEndPoint);
					base.LogNet?.WriteDebug(ToString(), "Udp " + StringResources.Language.Send + "：" + array2.ToHexString(' '));
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
			return $"OmronFinsUdpServer[{base.Port}]";
		}
	}
}

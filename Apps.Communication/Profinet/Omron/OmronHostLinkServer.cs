using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// <b>[商业授权]</b> 欧姆龙的HostLink虚拟服务器，支持DM区，CIO区，Work区，Hold区，Auxiliary区，可以方便的进行测试<br />
	/// <b>[Authorization]</b> Omron's HostLink virtual server supports DM area, CIO area, Work area, Hold area, and Auxiliary area, which can be easily tested
	/// </summary>
	/// <remarks>
	/// 支持TCP的接口以及串口，方便客户端进行测试，或是开发用于教学的虚拟服务器对象
	/// </remarks>
	public class OmronHostLinkServer : OmronFinsServer
	{
		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLink.UnitNumber" />
		public byte UnitNumber { get; set; }

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsServer.#ctor" />
		public OmronHostLinkServer()
		{
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			AppSession appSession = new AppSession(socket);
			try
			{
				socket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, appSession);
				AddClient(appSession);
			}
			catch
			{
				socket.Close();
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
			}
		}

		private async void SocketAsyncCallBack(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			AppSession session = asyncState as AppSession;
			if (session == null)
			{
				return;
			}
			try
			{
				session.WorkSocket.EndReceive(ar);
				OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync(session.WorkSocket, 13, 5000);
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender(read1.Content)}");
				string hexFinsCore = Encoding.ASCII.GetString(read1.Content, 14, read1.Content.Length - 18);
				byte[] back = ReadFromFinsCore(SoftBasic.HexStringToBytes(hexFinsCore));
				if (back != null)
				{
					session.WorkSocket.Send(back);
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender(back)}");
					session.UpdateHeartTime();
					RaiseDataReceived(session, read1.Content);
					session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				}
				else
				{
					RemoveClient(session);
				}
			}
			catch
			{
				RemoveClient(session);
			}
		}

		/// <inheritdoc />
		protected override byte[] PackCommand(int status, byte[] finsCore, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			data = SoftBasic.BytesToAsciiBytes(data);
			byte[] array = new byte[27 + data.Length];
			Encoding.ASCII.GetBytes("@00FA0040000000").CopyTo(array, 0);
			Encoding.ASCII.GetBytes(UnitNumber.ToString("X2")).CopyTo(array, 1);
			if (data.Length != 0)
			{
				data.CopyTo(array, 23);
			}
			Encoding.ASCII.GetBytes(finsCore.SelectBegin(2).ToHexString()).CopyTo(array, 15);
			Encoding.ASCII.GetBytes(status.ToString("X4")).CopyTo(array, 19);
			int num = array[0];
			for (int i = 1; i < array.Length - 4; i++)
			{
				num ^= array[i];
			}
			SoftBasic.BuildAsciiBytesFrom((byte)num).CopyTo(array, array.Length - 4);
			array[array.Length - 2] = 42;
			array[array.Length - 1] = 13;
			return array;
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			if (receivedLength > 1)
			{
				return buffer[receivedLength - 1] == 13;
			}
			return false;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			if (data.Length < 22)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Uknown Data：" + data.ToHexString(' '));
			}
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			string @string = Encoding.ASCII.GetString(data, 14, data.Length - 18);
			byte[] array = ReadFromFinsCore(SoftBasic.HexStringToBytes(@string));
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(array));
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronHostLinkServer[{base.Port}]";
		}
	}
}

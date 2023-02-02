using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLinkCMode协议的虚拟服务器
	/// </summary>
	public class OmronHostLinkCModeServer : OmronFinsServer
	{
		private byte operationMode = 1;

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLink.UnitNumber" />
		public byte UnitNumber { get; set; }

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsServer.#ctor" />
		public OmronHostLinkCModeServer()
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
				byte[] back = ReadFromFinsCore(read1.Content);
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
		protected override byte[] ReadFromFinsCore(byte[] finsCore)
		{
			string @string = Encoding.ASCII.GetString(finsCore, 3, 2);
			int num;
			switch (@string)
			{
			default:
				num = ((@string == "RJ") ? 1 : 0);
				break;
			case "RD":
			case "RR":
			case "RL":
			case "RH":
				num = 1;
				break;
			}
			if (num != 0)
			{
				SoftBuffer softBuffer = null;
				switch (@string)
				{
				case "RD":
					softBuffer = dBuffer;
					break;
				case "RR":
					softBuffer = cioBuffer;
					break;
				case "RL":
					softBuffer = cioBuffer;
					break;
				case "RH":
					softBuffer = hBuffer;
					break;
				case "RJ":
					softBuffer = arBuffer;
					break;
				case "RE":
					softBuffer = emBuffer;
					break;
				default:
					return PackCommand(22, finsCore, null);
				}
				if (@string == "RE")
				{
					int num2 = Convert.ToInt32(Encoding.ASCII.GetString(finsCore, 7, 4));
					ushort num3 = Convert.ToUInt16(Encoding.ASCII.GetString(finsCore, 11, 4));
					byte[] bytes = softBuffer.GetBytes(num2 * 2, num3 * 2);
					return PackCommand(0, finsCore, bytes);
				}
				int num4 = Convert.ToInt32(Encoding.ASCII.GetString(finsCore, 5, 4));
				ushort num5 = Convert.ToUInt16(Encoding.ASCII.GetString(finsCore, 9, 4));
				byte[] bytes2 = softBuffer.GetBytes(num4 * 2, num5 * 2);
				return PackCommand(0, finsCore, bytes2);
			}
			int num6;
			switch (@string)
			{
			default:
				num6 = ((@string == "WE") ? 1 : 0);
				break;
			case "WD":
			case "WR":
			case "WL":
			case "WH":
			case "WJ":
				num6 = 1;
				break;
			}
			if (num6 != 0)
			{
				SoftBuffer softBuffer2 = null;
				switch (@string)
				{
				case "WD":
					softBuffer2 = dBuffer;
					break;
				case "WR":
					softBuffer2 = cioBuffer;
					break;
				case "WL":
					softBuffer2 = cioBuffer;
					break;
				case "WH":
					softBuffer2 = hBuffer;
					break;
				case "WJ":
					softBuffer2 = arBuffer;
					break;
				case "WE":
					softBuffer2 = emBuffer;
					break;
				default:
					return PackCommand(22, finsCore, null);
				}
				if (@string == "WE")
				{
					int num7 = Convert.ToInt32(Encoding.ASCII.GetString(finsCore, 7, 4));
					byte[] data = Encoding.ASCII.GetString(finsCore, 11, finsCore.Length - 11).ToHexBytes();
					softBuffer2.SetBytes(data, num7 * 2);
					return PackCommand(0, finsCore, null);
				}
				int num8 = Convert.ToInt32(Encoding.ASCII.GetString(finsCore, 5, 4));
				byte[] data2 = Encoding.ASCII.GetString(finsCore, 9, finsCore.Length - 9).ToHexBytes();
				softBuffer2.SetBytes(data2, num8 * 2);
				return PackCommand(0, finsCore, null);
			}
			switch (@string)
			{
			case "MM":
				return PackCommand(0, finsCore, new byte[1] { 48 });
			case "MS":
				return PackCommand(0, finsCore, new byte[2] { operationMode, 48 });
			case "SC":
			{
				byte b = Convert.ToByte(Encoding.ASCII.GetString(finsCore, 5, 2), 16);
				if (b >= 0 && b <= 2)
				{
					operationMode = b;
				}
				return PackCommand(0, finsCore, null);
			}
			default:
				return PackCommand(22, finsCore, null);
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
			byte[] array = new byte[11 + data.Length];
			Encoding.ASCII.GetBytes("@0000").CopyTo(array, 0);
			Encoding.ASCII.GetBytes(UnitNumber.ToString("X2")).CopyTo(array, 1);
			Array.Copy(finsCore, 3, array, 3, 2);
			Encoding.ASCII.GetBytes(status.ToString("X2")).CopyTo(array, 5);
			if (data.Length != 0)
			{
				data.CopyTo(array, 7);
			}
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
			if (data.Length < 9)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Uknown Data：" + SoftBasic.GetAsciiStringRender(data));
			}
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			byte[] array = ReadFromFinsCore(data);
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

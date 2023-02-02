using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Yamatake.Helper;

namespace Apps.Communication.Profinet.Yamatake
{
	/// <summary>
	/// <b>[商业授权]</b> 山武的数字指示调节器的虚拟设备，支持和HSL本身进行数据通信测试<br />
	/// <b>[Authorization]</b> Yamatake’s digital indicating regulator is a virtual device that supports data communication testing with HSL itself
	/// </summary>
	public class DigitronCPLServer : NetworkDataServerBase
	{
		private SoftBuffer softBuffer;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 获取或设置当前虚拟仪表的站号信息，如果站号不一致，将不予访问<br />
		/// Get or set the station number information of the current virtual instrument. If the station number is inconsistent, it will not be accessed
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public DigitronCPLServer()
		{
			softBuffer = new SoftBuffer(131072);
			base.ByteTransform = new RegularByteTransform();
			Station = 1;
		}

		/// <inheritdoc />
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			try
			{
				ushort num = ushort.Parse(address);
				return OperateResult.CreateSuccessResult(softBuffer.GetBytes(num * 2, length * 2));
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("Read Failed: " + ex.Message);
			}
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, byte[] value)
		{
			try
			{
				ushort num = ushort.Parse(address);
				softBuffer.SetBytes(value, num * 2);
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				return new OperateResult("Write Failed: " + ex.Message);
			}
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
				OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync(session.WorkSocket, 10, 5000);
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}

				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender(read1.Content)}");
				byte[] back = ReadFromCore(read1.Content);
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

		private byte[] ReadFromCore(byte[] command)
		{
			try
			{
				int num = 9;
				for (int i = 9; i < command.Length; i++)
				{
					if (command[i] == 3)
					{
						num = i;
						break;
					}
				}
				byte b = Convert.ToByte(Encoding.ASCII.GetString(command, 1, 2), 16);
				if (b != Station)
				{
					return DigitronCPLHelper.PackResponseContent(Station, 40, null, 87);
				}
				string[] array = Encoding.ASCII.GetString(command, 9, num - 9).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				string @string = Encoding.ASCII.GetString(command, 6, 2);
				int num2 = int.Parse(array[0].Substring(0, array[0].Length - 1));
				byte b2 = (byte)(array[0].EndsWith("W") ? 87 : 83);
				if (num2 >= 65536 || num2 < 0)
				{
					return DigitronCPLHelper.PackResponseContent(Station, 42, null, b2);
				}
				if (@string == "RS")
				{
					int num3 = int.Parse(array[1]);
					if (num2 + num3 > 65535)
					{
						return DigitronCPLHelper.PackResponseContent(Station, 42, null, b2);
					}
					if (num3 > 16)
					{
						return DigitronCPLHelper.PackResponseContent(Station, 41, null, b2);
					}
					return DigitronCPLHelper.PackResponseContent(Station, 0, softBuffer.GetBytes(num2 * 2, num3 * 2), b2);
				}
				if (@string == "WS")
				{
					if (!base.EnableWrite)
					{
						return DigitronCPLHelper.PackResponseContent(Station, 46, null, b2);
					}
					if (array.Length > 17)
					{
						return DigitronCPLHelper.PackResponseContent(Station, 41, null, b2);
					}
					byte[] array2 = new byte[(array.Length - 1) * 2];
					for (int j = 1; j < array.Length; j++)
					{
						if (b2 == 87)
						{
							BitConverter.GetBytes(short.Parse(array[j])).CopyTo(array2, j * 2 - 2);
						}
						else
						{
							BitConverter.GetBytes(ushort.Parse(array[j])).CopyTo(array2, j * 2 - 2);
						}
					}
					softBuffer.SetBytes(array2, num2 * 2);
					return DigitronCPLHelper.PackResponseContent(Station, 0, null, b2);
				}
				return DigitronCPLHelper.PackResponseContent(Station, 40, null, b2);
			}
			catch
			{
				return null;
			}
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			if (receivedLength > 5)
			{
				return buffer[receivedLength - 2] == 13 && buffer[receivedLength - 1] == 10;
			}
			return base.CheckSerialReceiveDataComplete(buffer, receivedLength);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			byte[] array = ReadFromCore(data);
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(array));
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DigitronCPLServer[{base.Port}]";
		}
	}
}

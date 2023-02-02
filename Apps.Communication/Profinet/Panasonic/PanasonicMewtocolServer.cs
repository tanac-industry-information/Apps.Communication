using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Panasonic
{
	/// <summary>
	/// <b>[商业授权]</b> 松下Mewtocol协议的虚拟服务器，支持串口和网口的操作<br />
	/// <b>[Authorization]</b> Panasonic Mewtocol protocol virtual server, supports serial and network port operations
	/// </summary>
	public class PanasonicMewtocolServer : NetworkDataServerBase
	{
		private SoftBuffer xBuffer;

		private SoftBuffer rBuffer;

		private SoftBuffer dBuffer;

		private SoftBuffer lBuffer;

		private SoftBuffer fBuffer;

		private const int DataPoolLength = 65536;

		private byte station = 1;

		/// <inheritdoc cref="P:Communication.Profinet.Panasonic.PanasonicMewtocol.Station" />
		public byte Station
		{
			get
			{
				return station;
			}
			set
			{
				station = value;
			}
		}

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public PanasonicMewtocolServer()
		{
			rBuffer = new SoftBuffer(131072);
			dBuffer = new SoftBuffer(131072);
			xBuffer = new SoftBuffer(131072);
			lBuffer = new SoftBuffer(131072);
			fBuffer = new SoftBuffer(131072);
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[262144];
			Array.Copy(rBuffer.GetBytes(), 0, array, 0, 131072);
			Array.Copy(dBuffer.GetBytes(), 0, array, 131072, 131072);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 262144)
			{
				throw new Exception("File is not correct");
			}
			rBuffer.SetBytes(content, 0, 0, 131072);
			dBuffer.SetBytes(content, 131072, 0, 131072);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			AppSession appSession = new AppSession(socket);
			if (socket.BeginReceiveResult(SocketAsyncCallBack, appSession).IsSuccess)
			{
				AddClient(appSession);
			}
			else
			{
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
			if (!session.WorkSocket.EndReceiveResult(ar).IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync(session.WorkSocket, 13, 2000);
			if (!read1.IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{Encoding.ASCII.GetString(read1.Content.RemoveLast(1))}");
			byte[] back = PanasonicHelper.PackPanasonicCommand(station, ReadFromCommand(read1.Content)).Content;
			if (back == null)
			{
				RemoveClient(session);
				return;
			}
			if (!Send(session.WorkSocket, back).IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{Encoding.ASCII.GetString(back.RemoveLast(1))}");
			session.UpdateHeartTime();
			RaiseDataReceived(session, read1.Content);
			if (!session.WorkSocket.BeginReceiveResult(SocketAsyncCallBack, session).IsSuccess)
			{
				RemoveClient(session);
			}
		}

		/// <summary>
		/// 创建一个失败的返回消息，指定错误码即可，会自动计算出来BCC校验和
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>原始字节报文，用于反馈消息</returns>
		protected string CreateFailedResponse(byte code)
		{
			return "!" + code.ToString("D2");
		}

		/// <summary>
		/// 根据命令来获取相关的数据内容
		/// </summary>
		/// <param name="cmd">原始的命令码</param>
		/// <returns>返回的数据信息</returns>
		public virtual string ReadFromCommand(byte[] cmd)
		{
			try
			{
				string @string = Encoding.ASCII.GetString(cmd);
				if (@string[0] != '%')
				{
					return CreateFailedResponse(41);
				}
				byte b = Convert.ToByte(@string.Substring(1, 2), 16);
				if (b != station)
				{
					base.LogNet?.WriteError(ToString(), $"Station not match, need:{station}, but now: {b}");
					return CreateFailedResponse(50);
				}
				if (@string[3] != '#')
				{
					return CreateFailedResponse(41);
				}
				if (@string.Substring(4, 3) == "RCS")
				{
					if (@string[7] == 'R')
					{
						int destIndex = Convert.ToInt32(@string.Substring(8, 3)) * 16 + Convert.ToInt32(@string.Substring(11, 1), 16);
						bool @bool = rBuffer.GetBool(destIndex);
						return "$RC" + (@bool ? "1" : "0");
					}
				}
				else if (@string.Substring(4, 3) == "WCS" && @string[7] == 'R')
				{
					int destIndex2 = Convert.ToInt32(@string.Substring(8, 3)) * 16 + Convert.ToInt32(@string.Substring(11, 1), 16);
					rBuffer.SetBool(@string[12] == '1', destIndex2);
					return "$WC";
				}
				if (@string.Substring(4, 3) == "RCC")
				{
					if (@string[7] == 'R')
					{
						int num = Convert.ToInt32(@string.Substring(8, 4));
						int num2 = Convert.ToInt32(@string.Substring(12, 4));
						int num3 = num2 - num + 1;
						byte[] bytes = rBuffer.GetBytes(num * 2, num3 * 2);
						return "$RC" + bytes.ToHexString();
					}
				}
				else if (@string.Substring(4, 3) == "WCC" && @string[7] == 'R')
				{
					int num4 = Convert.ToInt32(@string.Substring(8, 4));
					int num5 = Convert.ToInt32(@string.Substring(12, 4));
					int num6 = num5 - num4 + 1;
					byte[] data = @string.Substring(16, num6 * 2).ToHexBytes();
					rBuffer.SetBytes(data, num4 * 2);
					return "$WC";
				}
				if (@string.Substring(4, 2) == "RD")
				{
					if (@string[6] == 'D')
					{
						int num7 = Convert.ToInt32(@string.Substring(7, 5));
						int num8 = Convert.ToInt32(@string.Substring(12, 5));
						int num9 = num8 - num7 + 1;
						byte[] bytes2 = dBuffer.GetBytes(num7 * 2, num9 * 2);
						return "$RD" + bytes2.ToHexString();
					}
				}
				else if (@string.Substring(4, 2) == "WD" && @string[6] == 'D')
				{
					int num10 = Convert.ToInt32(@string.Substring(7, 5));
					int num11 = Convert.ToInt32(@string.Substring(12, 5));
					int num12 = num11 - num10 + 1;
					byte[] data2 = @string.Substring(17, num12 * 2).ToHexBytes();
					dBuffer.SetBytes(data2, num10 * 2);
					return "$WD";
				}
				return CreateFailedResponse(41);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), ex);
				return CreateFailedResponse(41);
			}
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			if (receivedLength > 5)
			{
				return buffer[receivedLength - 1] == 13;
			}
			return base.CheckSerialReceiveDataComplete(buffer, receivedLength);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			if (data.Length < 5)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Uknown Data：" + data.ToHexString(' '));
			}
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			byte[] content = PanasonicHelper.PackPanasonicCommand(station, ReadFromCommand(data)).Content;
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(content));
			return OperateResult.CreateSuccessResult(content);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				xBuffer?.Dispose();
				rBuffer?.Dispose();
				dBuffer?.Dispose();
				lBuffer?.Dispose();
				fBuffer?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"PanasonicMewtocolServer[{base.Port}]";
		}
	}
}

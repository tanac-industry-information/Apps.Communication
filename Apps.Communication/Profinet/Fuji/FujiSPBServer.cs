using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.ModBus;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Fuji
{
	/// <summary>
	/// <b>[商业授权]</b> 富士的SPB虚拟的PLC，线圈支持X,Y,M的读写，其中X只能远程读，寄存器支持D,R,W的读写操作。<br />
	/// <b>[Authorization]</b> Fuji's SPB virtual PLC, the coil supports X, Y, M read and write, 
	/// X can only be read remotely, and the register supports D, R, W read and write operations.
	/// </summary>
	public class FujiSPBServer : NetworkDataServerBase
	{
		private SoftBuffer xBuffer;

		private SoftBuffer yBuffer;

		private SoftBuffer mBuffer;

		private SoftBuffer dBuffer;

		private SoftBuffer rBuffer;

		private SoftBuffer wBuffer;

		private const int DataPoolLength = 65536;

		private int station = 1;

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.DataFormat" />
		public DataFormat DataFormat
		{
			get
			{
				return base.ByteTransform.DataFormat;
			}
			set
			{
				base.ByteTransform.DataFormat = value;
			}
		}

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.IsStringReverse" />
		public bool IsStringReverse
		{
			get
			{
				return base.ByteTransform.IsStringReverseByteWord;
			}
			set
			{
				base.ByteTransform.IsStringReverseByteWord = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Profinet.Fuji.FujiSPBOverTcp.Station" />
		public int Station
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
		/// 实例化一个富士SPB的网口和串口服务器，支持数据读写操作
		/// </summary>
		public FujiSPBServer()
		{
			xBuffer = new SoftBuffer(65536);
			yBuffer = new SoftBuffer(65536);
			mBuffer = new SoftBuffer(65536);
			dBuffer = new SoftBuffer(131072);
			rBuffer = new SoftBuffer(131072);
			wBuffer = new SoftBuffer(131072);
			base.ByteTransform = new RegularByteTransform();
			base.ByteTransform.DataFormat = DataFormat.CDAB;
			base.WordLength = 1;
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[589824];
			xBuffer.GetBytes().CopyTo(array, 0);
			yBuffer.GetBytes().CopyTo(array, 65536);
			mBuffer.GetBytes().CopyTo(array, 131072);
			dBuffer.GetBytes().CopyTo(array, 196608);
			rBuffer.GetBytes().CopyTo(array, 327680);
			wBuffer.GetBytes().CopyTo(array, 458752);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 589824)
			{
				throw new Exception("File is not correct");
			}
			xBuffer.SetBytes(content, 0, 65536);
			yBuffer.SetBytes(content, 65536, 65536);
			mBuffer.SetBytes(content, 131072, 65536);
			dBuffer.SetBytes(content, 196608, 131072);
			rBuffer.SetBytes(content, 327680, 131072);
			wBuffer.SetBytes(content, 458752, 131072);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = new OperateResult<byte[]>();
			try
			{
				switch (address[0])
				{
				case 'X':
				case 'x':
					return OperateResult.CreateSuccessResult(xBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				case 'Y':
				case 'y':
					return OperateResult.CreateSuccessResult(yBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				case 'M':
				case 'm':
					return OperateResult.CreateSuccessResult(mBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				case 'D':
				case 'd':
					return OperateResult.CreateSuccessResult(dBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				case 'R':
				case 'r':
					return OperateResult.CreateSuccessResult(rBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				case 'W':
				case 'w':
					return OperateResult.CreateSuccessResult(wBuffer.GetBytes(Convert.ToInt32(address.Substring(1)) * 2, length * 2));
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				operateResult.Message = ex.Message;
				return operateResult;
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = new OperateResult<byte[]>();
			try
			{
				switch (address[0])
				{
				case 'X':
				case 'x':
					xBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				case 'Y':
				case 'y':
					yBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				case 'M':
				case 'm':
					mBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				case 'D':
				case 'd':
					dBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				case 'R':
				case 'r':
					rBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				case 'W':
				case 'w':
					wBuffer.SetBytes(value, Convert.ToInt32(address.Substring(1)) * 2);
					return OperateResult.CreateSuccessResult();
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				operateResult.Message = ex.Message;
				return operateResult;
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			try
			{
				int destIndex = 0;
				if (address.LastIndexOf('.') > 0)
				{
					destIndex = HslHelper.GetBitIndexInformation(ref address);
					destIndex = Convert.ToInt32(address.Substring(1)) * 16 + destIndex;
				}
				else if (address[0] == 'X' || address[0] == 'x' || address[0] == 'Y' || address[0] == 'y' || address[0] == 'M' || address[0] == 'm')
				{
					destIndex = Convert.ToInt32(address.Substring(1));
				}
				switch (address[0])
				{
				case 'X':
				case 'x':
					return OperateResult.CreateSuccessResult(xBuffer.GetBool(destIndex, length));
				case 'Y':
				case 'y':
					return OperateResult.CreateSuccessResult(yBuffer.GetBool(destIndex, length));
				case 'M':
				case 'm':
					return OperateResult.CreateSuccessResult(mBuffer.GetBool(destIndex, length));
				case 'D':
				case 'd':
					return OperateResult.CreateSuccessResult(dBuffer.GetBool(destIndex, length));
				case 'R':
				case 'r':
					return OperateResult.CreateSuccessResult(rBuffer.GetBool(destIndex, length));
				case 'W':
				case 'w':
					return OperateResult.CreateSuccessResult(wBuffer.GetBool(destIndex, length));
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>(ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDeviceBase.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			try
			{
				int num = 0;
				if (address.LastIndexOf('.') > 0)
				{
					HslHelper.GetBitIndexInformation(ref address);
					num = Convert.ToInt32(address.Substring(1)) * 16 + num;
				}
				else if (address[0] == 'X' || address[0] == 'x' || address[0] == 'Y' || address[0] == 'y' || address[0] == 'M' || address[0] == 'm')
				{
					num = Convert.ToInt32(address.Substring(1));
				}
				switch (address[0])
				{
				case 'X':
				case 'x':
					xBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				case 'Y':
				case 'y':
					yBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				case 'M':
				case 'm':
					mBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				case 'D':
				case 'd':
					dBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				case 'R':
				case 'r':
					rBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				case 'W':
				case 'w':
					wBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>(ex.Message);
			}
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
			OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 2000, new FujiSPBMessage());
			if (!read1.IsSuccess)
			{
				RemoveClient(session);
				return;
			}

			if (read1.Content[0] != 58)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{Encoding.ASCII.GetString(read1.Content.RemoveLast(2))}");
			byte[] back = ReadFromSPBCore(read1.Content);
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
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{Encoding.ASCII.GetString(back.RemoveLast(2))}");
			session.UpdateHeartTime();
			RaiseDataReceived(session, read1.Content);
			if (!session.WorkSocket.BeginReceiveResult(SocketAsyncCallBack, session).IsSuccess)
			{
				RemoveClient(session);
			}
		}

		private byte[] CreateResponseBack(byte err, string command, byte[] data, bool addLength = true)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(':');
			stringBuilder.Append(Station.ToString("X2"));
			stringBuilder.Append("00");
			stringBuilder.Append(command.Substring(9, 4));
			stringBuilder.Append(err.ToString("X2"));
			if (err == 0 && data != null)
			{
				if (addLength)
				{
					stringBuilder.Append(FujiSPBHelper.AnalysisIntegerAddress(data.Length / 2));
				}
				stringBuilder.Append(data.ToHexString());
			}
			stringBuilder[3] = ((stringBuilder.Length - 5) / 2).ToString("X2")[0];
			stringBuilder[4] = ((stringBuilder.Length - 5) / 2).ToString("X2")[1];
			stringBuilder.Append("\r\n");
			return Encoding.ASCII.GetBytes(stringBuilder.ToString());
		}

		private int AnalysisAddress(string address)
		{
			string value = address.Substring(2) + address.Substring(0, 2);
			return Convert.ToInt32(value);
		}

		private byte[] ReadFromSPBCore(byte[] receive)
		{
			if (receive.Length < 15)
			{
				return null;
			}
			if (receive[receive.Length - 2] == 13 && receive[receive.Length - 1] == 10)
			{
				receive = receive.RemoveLast(2);
			}
			string @string = Encoding.ASCII.GetString(receive);
			int num = Convert.ToInt32(@string.Substring(3, 2), 16);
			if (num != (@string.Length - 5) / 2)
			{
				return CreateResponseBack(3, @string, null);
			}
			if (@string.Substring(9, 4) == "0000")
			{
				return ReadByCommand(@string);
			}
			if (@string.Substring(9, 4) == "0100")
			{
				return WriteByCommand(@string);
			}
			if (@string.Substring(9, 4) == "0102")
			{
				return WriteBitByCommand(@string);
			}
			return null;
		}

		private byte[] ReadByCommand(string command)
		{
			string text = command.Substring(13, 2);
			int num = AnalysisAddress(command.Substring(15, 4));
			int num2 = AnalysisAddress(command.Substring(19, 4));
			if (num2 > 105)
			{
				CreateResponseBack(3, command, null);
			}
			switch (text)
			{
			case "0C":
				return CreateResponseBack(0, command, dBuffer.GetBytes(num * 2, num2 * 2));
			case "0D":
				return CreateResponseBack(0, command, rBuffer.GetBytes(num * 2, num2 * 2));
			case "0E":
				return CreateResponseBack(0, command, wBuffer.GetBytes(num * 2, num2 * 2));
			case "01":
				return CreateResponseBack(0, command, xBuffer.GetBytes(num * 2, num2 * 2));
			case "00":
				return CreateResponseBack(0, command, yBuffer.GetBytes(num * 2, num2 * 2));
			case "02":
				return CreateResponseBack(0, command, mBuffer.GetBytes(num * 2, num2 * 2));
			default:
				return CreateResponseBack(2, command, null);
			}
		}

		private byte[] WriteByCommand(string command)
		{
			if (!base.EnableWrite)
			{
				return CreateResponseBack(2, command, null);
			}
			string text = command.Substring(13, 2);
			int num = AnalysisAddress(command.Substring(15, 4));
			int num2 = AnalysisAddress(command.Substring(19, 4));
			if (num2 * 4 != command.Length - 23)
			{
				return CreateResponseBack(3, command, null);
			}
			byte[] data = command.Substring(23).ToHexBytes();
			switch (text)
			{
			case "0C":
				dBuffer.SetBytes(data, num * 2);
				return CreateResponseBack(0, command, null);
			case "0D":
				rBuffer.SetBytes(data, num * 2);
				return CreateResponseBack(0, command, null);
			case "0E":
				wBuffer.SetBytes(data, num * 2);
				return CreateResponseBack(0, command, null);
			case "00":
				yBuffer.SetBytes(data, num * 2);
				return CreateResponseBack(0, command, null);
			case "02":
				mBuffer.SetBytes(data, num * 2);
				return CreateResponseBack(0, command, null);
			default:
				return CreateResponseBack(2, command, null);
			}
		}

		private byte[] WriteBitByCommand(string command)
		{
			if (!base.EnableWrite)
			{
				return CreateResponseBack(2, command, null);
			}
			string text = command.Substring(13, 2);
			int num = AnalysisAddress(command.Substring(15, 4));
			int num2 = Convert.ToInt32(command.Substring(19, 2));
			bool value = command.Substring(21, 2) != "00";
			switch (text)
			{
			case "0C":
				dBuffer.SetBool(value, num * 8 + num2);
				return CreateResponseBack(0, command, null);
			case "0D":
				rBuffer.SetBool(value, num * 8 + num2);
				return CreateResponseBack(0, command, null);
			case "0E":
				wBuffer.SetBool(value, num * 8 + num2);
				return CreateResponseBack(0, command, null);
			case "00":
				yBuffer.SetBool(value, num * 8 + num2);
				return CreateResponseBack(0, command, null);
			case "02":
				mBuffer.SetBool(value, num * 8 + num2);
				return CreateResponseBack(0, command, null);
			default:
				return CreateResponseBack(2, command, null);
			}
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			return ModbusInfo.CheckAsciiReceiveDataComplete(buffer, receivedLength);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			if (data.Length < 5)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Uknown Data：" + data.ToHexString(' '));
			}
			if (data[0] != 58)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] not 0x3A Start Data：" + data.ToHexString(' '));
			}
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Ascii " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			if (Encoding.ASCII.GetString(data, 1, 2) != station.ToString("X2"))
			{
				return new OperateResult<byte[]>($"[{GetSerialPort().PortName}] Station not match , Except: {station:X2} , Actual: {SoftBasic.GetAsciiStringRender(data)}");
			}
			byte[] array = ReadFromSPBCore(data);
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Ascii " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(array));
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				xBuffer.Dispose();
				yBuffer.Dispose();
				mBuffer.Dispose();
				dBuffer.Dispose();
				rBuffer.Dispose();
				wBuffer.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FujiSPBServer[{base.Port}]";
		}
	}
}

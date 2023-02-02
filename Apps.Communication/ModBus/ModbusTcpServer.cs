using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.ModBus
{
	/// <summary>
	/// <b>[商业授权]</b> Modbus的虚拟服务器，同时支持Tcp和Rtu的机制，支持线圈，离散输入，寄存器和输入寄存器的读写操作，同时支持掩码写入功能，可以用来当做系统的数据交换池<br />
	/// <b>[Authorization]</b> Modbus virtual server supports Tcp and Rtu mechanisms at the same time, supports read and write operations of coils, discrete inputs, r
	/// egisters and input registers, and supports mask write function, which can be used as a system data exchange pool
	/// </summary>
	/// <remarks>
	/// 可以基于本类实现一个功能复杂的modbus服务器，支持Modbus-Tcp，启动串口后，还支持Modbus-Rtu和Modbus-ASCII，会根据报文进行动态的适配。
	/// <list type="number">
	/// <item>线圈，功能码对应01，05，15</item>
	/// <item>离散输入，功能码对应02</item>
	/// <item>寄存器，功能码对应03，06，16</item>
	/// <item>输入寄存器，功能码对应04，输入寄存器在服务器端可以实现读写的操作</item>
	/// <item>掩码写入，功能码对应22，可以对字寄存器进行位操作</item>
	/// </list>
	/// </remarks>
	/// <example>
	/// 读写的地址格式为富文本地址，具体请参照下面的示例代码。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\ModbusTcpServer.cs" region="ModbusTcpServerExample" title="ModbusTcpServer示例" />
	/// </example>
	public class ModbusTcpServer : NetworkDataServerBase
	{
		private List<ModBusMonitorAddress> subscriptions;

		private SimpleHybirdLock subcriptionHybirdLock;

		private SoftBuffer coilBuffer;

		private SoftBuffer inputBuffer;

		private SoftBuffer registerBuffer;

		private SoftBuffer inputRegisterBuffer;

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

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.Station" />
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
		/// 获取或设置当前的TCP服务器是否使用modbus-rtu报文进行通信，如果设置为 <c>True</c>，那么客户端需要使用 <see cref="T:Communication.ModBus.ModbusRtuOverTcp" /><br />
		/// Get or set whether the current TCP server uses modbus-rtu messages for communication.
		/// If it is set to <c>True</c>, then the client needs to use <see cref="T:Communication.ModBus.ModbusRtuOverTcp" />
		/// </summary>
		/// <remarks>
		/// 需要注意的是，本属性设置为<c>False</c>时，客户端使用<see cref="T:Communication.ModBus.ModbusTcpNet" />，否则，使用<see cref="T:Communication.ModBus.ModbusRtuOverTcp" />，不能混合使用
		/// </remarks>
		public bool UseModbusRtuOverTcp { get; set; }

		/// <summary>
		/// 获取或设置两次请求直接的延时时间，单位毫秒，默认是0，不发生延时，设置为20的话，可以有效防止有客户端疯狂进行请求而导致服务器的CPU占用率上升。<br />
		/// Get or set the direct delay time of two requests, in milliseconds, the default is 0, no delay occurs, if it is set to 20, 
		/// it can effectively prevent the client from making crazy requests and causing the server's CPU usage to increase.
		/// </summary>
		public int RequestDelayTime { get; set; }

		/// <summary>
		/// 实例化一个Modbus Tcp及Rtu的服务器，支持数据读写操作
		/// </summary>
		public ModbusTcpServer()
		{
			coilBuffer = new SoftBuffer(65536);
			inputBuffer = new SoftBuffer(65536);
			registerBuffer = new SoftBuffer(131072);
			inputRegisterBuffer = new SoftBuffer(131072);
			registerBuffer.IsBoolReverseByWord = true;
			inputRegisterBuffer.IsBoolReverseByWord = true;
			subscriptions = new List<ModBusMonitorAddress>();
			subcriptionHybirdLock = new SimpleHybirdLock();
			base.ByteTransform = new ReverseWordTransform();
			base.WordLength = 1;
			receiveAtleastTime = 200;
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[393216];
			Array.Copy(coilBuffer.GetBytes(), 0, array, 0, 65536);
			Array.Copy(inputBuffer.GetBytes(), 0, array, 65536, 65536);
			Array.Copy(registerBuffer.GetBytes(), 0, array, 131072, 131072);
			Array.Copy(inputRegisterBuffer.GetBytes(), 0, array, 262144, 131072);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 393216)
			{
				throw new Exception("File is not correct");
			}
			coilBuffer.SetBytes(content, 0, 0, 65536);
			inputBuffer.SetBytes(content, 65536, 0, 65536);
			registerBuffer.SetBytes(content, 131072, 0, 131072);
			inputRegisterBuffer.SetBytes(content, 262144, 0, 131072);
		}

		/// <summary>
		/// 读取地址的线圈的通断情况
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public bool ReadCoil(string address)
		{
			ushort index = ushort.Parse(address);
			return coilBuffer.GetByte(index) != 0;
		}

		/// <summary>
		/// 批量读取地址的线圈的通断情况
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="length">读取长度</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public bool[] ReadCoil(string address, ushort length)
		{
			ushort index = ushort.Parse(address);
			return (from m in coilBuffer.GetBytes(index, length)
				select m != 0).ToArray();
		}

		/// <summary>
		/// 写入线圈的通断值
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="data">是否通断</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public void WriteCoil(string address, bool data)
		{
			ushort index = ushort.Parse(address);
			coilBuffer.SetValue((byte)(data ? 1u : 0u), index);
		}

		/// <summary>
		/// 写入线圈数组的通断值
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="data">是否通断</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public void WriteCoil(string address, bool[] data)
		{
			if (data != null)
			{
				ushort destIndex = ushort.Parse(address);
				coilBuffer.SetBytes(data.Select((bool m) => (byte)(m ? 1u : 0u)).ToArray(), destIndex);
			}
		}

		/// <summary>
		/// 读取地址的离散线圈的通断情况
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public bool ReadDiscrete(string address)
		{
			ushort index = ushort.Parse(address);
			return inputBuffer.GetByte(index) != 0;
		}

		/// <summary>
		/// 批量读取地址的离散线圈的通断情况
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="length">读取长度</param>
		/// <returns><c>True</c>或是<c>False</c></returns>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public bool[] ReadDiscrete(string address, ushort length)
		{
			ushort index = ushort.Parse(address);
			return (from m in inputBuffer.GetBytes(index, length)
				select m != 0).ToArray();
		}

		/// <summary>
		/// 写入离散线圈的通断值
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="data">是否通断</param>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public void WriteDiscrete(string address, bool data)
		{
			ushort index = ushort.Parse(address);
			inputBuffer.SetValue((byte)(data ? 1u : 0u), index);
		}

		/// <summary>
		/// 写入离散线圈数组的通断值
		/// </summary>
		/// <param name="address">起始地址，示例："100"</param>
		/// <param name="data">是否通断</param>
		/// <exception cref="T:System.IndexOutOfRangeException"></exception>
		public void WriteDiscrete(string address, bool[] data)
		{
			if (data != null)
			{
				ushort destIndex = ushort.Parse(address);
				inputBuffer.SetBytes(data.Select((bool m) => (byte)(m ? 1u : 0u)).ToArray(), destIndex);
			}
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<ModbusAddress> operateResult = ModbusInfo.AnalysisAddress(address, (byte)Station, isStartWithZero: true, 3);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.Function == 3)
			{
				return OperateResult.CreateSuccessResult(registerBuffer.GetBytes(operateResult.Content.Address * 2, length * 2));
			}
			if (operateResult.Content.Function == 4)
			{
				return OperateResult.CreateSuccessResult(inputRegisterBuffer.GetBytes(operateResult.Content.Address * 2, length * 2));
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<ModbusAddress> operateResult = ModbusInfo.AnalysisAddress(address, (byte)Station, isStartWithZero: true, 3);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.Function == 3)
			{
				registerBuffer.SetBytes(value, operateResult.Content.Address * 2);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.Function == 4)
			{
				inputRegisterBuffer.SetBytes(value, operateResult.Content.Address * 2);
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<ModbusAddress> operateResult = ModbusInfo.AnalysisAddress(address, (byte)Station, isStartWithZero: true, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content.Function == 1)
			{
				return OperateResult.CreateSuccessResult((from m in coilBuffer.GetBytes(operateResult.Content.Address, length)
					select m != 0).ToArray());
			}
			if (operateResult.Content.Function == 2)
			{
				return OperateResult.CreateSuccessResult((from m in inputBuffer.GetBytes(operateResult.Content.Address, length)
					select m != 0).ToArray());
			}
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<ModbusAddress> operateResult = ModbusInfo.AnalysisAddress(address, (byte)Station, isStartWithZero: true, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.Function == 1)
			{
				coilBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1u : 0u)).ToArray(), operateResult.Content.Address);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.Function == 2)
			{
				inputBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1u : 0u)).ToArray(), operateResult.Content.Address);
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			if (address.IndexOf('.') < 0)
			{
				return base.Write(address, value);
			}
			try
			{
				int num = Convert.ToInt32(address.Substring(address.IndexOf('.') + 1));
				address = address.Substring(0, address.IndexOf('.'));
				OperateResult<ModbusAddress> operateResult = ModbusInfo.AnalysisAddress(address, (byte)Station, isStartWithZero: true, 3);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				num = operateResult.Content.Address * 16 + num;
				if (operateResult.Content.Function == 3)
				{
					registerBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				}
				if (operateResult.Content.Function == 4)
				{
					inputRegisterBuffer.SetBool(value, num);
					return OperateResult.CreateSuccessResult();
				}
				return new OperateResult(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
		}

		/// <summary>
		/// 写入寄存器数据，指定字节数据
		/// </summary>
		/// <param name="address">起始地址，示例："100"，如果是输入寄存器："x=4;100"</param>
		/// <param name="high">高位数据</param>
		/// <param name="low">地位数据</param>
		public void Write(string address, byte high, byte low)
		{
			Write(address, new byte[2] { high, low });
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
			if (!UseModbusRtuOverTcp)
			{
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 2000, new ModbusTcpMessage());
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				if (RequestDelayTime > 0)
				{
					Thread.Sleep(RequestDelayTime);
				}
				if (!CheckModbusMessageLegal(read1.Content.RemoveBegin(6)))
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
				byte[] back2 = ModbusInfo.PackCommandToTcp(id: (ushort)(read1.Content[0] * 256 + read1.Content[1]), modbus: ReadFromModbusCore(read1.Content.RemoveBegin(6)));
				if (back2 == null)
				{
					RemoveClient(session);
					return;
				}
				if (!Send(session.WorkSocket, back2).IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back2.ToHexString(' ')}");
				RaiseDataReceived(session, read1.Content);
			}
			else
			{
				OperateResult<byte[]> read2 = await ReceiveByMessageAsync(session.WorkSocket, 2000, null);
				if (!read2.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				if (RequestDelayTime > 0)
				{
					Thread.Sleep(RequestDelayTime);
				}

				byte[] data = read2.Content;
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Rtu {StringResources.Language.Receive}：{data.ToHexString(' ')}");
				if (SoftCRC16.CheckCRC16(data))
				{
					byte[] modbusCore = data.RemoveLast(2);
					if (!CheckModbusMessageLegal(modbusCore))
					{
						RemoveClient(session);
						return;
					}
					if (station >= 0 && station != modbusCore[0])
					{
						base.LogNet?.WriteError(ToString(), $"[{session.IpEndPoint}] Station not match Modbus-rtu : {data.ToHexString(' ')}");
						RemoveClient(session);
						return;
					}
					byte[] back = ModbusInfo.PackCommandToRtu(ReadFromModbusCore(modbusCore));
					if (!Send(session.WorkSocket, back).IsSuccess)
					{
						RemoveClient(session);
						return;
					}
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Rtu {StringResources.Language.Send}：{back.ToHexString(' ')}");
					if (base.IsStarted)
					{
						RaiseDataReceived(session, data);
					}
				}
				else
				{
					base.LogNet?.WriteWarn($"[{session.IpEndPoint}] CRC Check Failed : {data.ToHexString(' ')}");
				}
			}
			session.UpdateHeartTime();
			if (!session.WorkSocket.BeginReceiveResult(SocketAsyncCallBack, session).IsSuccess)
			{
				RemoveClient(session);
			}
		}

		/// <summary>
		/// 创建特殊的功能标识，然后返回该信息<br />
		/// Create a special feature ID and return this information
		/// </summary>
		/// <param name="modbusCore">modbus核心报文</param>
		/// <param name="error">错误码</param>
		/// <returns>携带错误码的modbus报文</returns>
		private byte[] CreateExceptionBack(byte[] modbusCore, byte error)
		{
			return new byte[3]
			{
				modbusCore[0],
				(byte)(modbusCore[1] + 128),
				error
			};
		}

		/// <summary>
		/// 创建返回消息<br />
		/// Create return message
		/// </summary>
		/// <param name="modbusCore">modbus核心报文</param>
		/// <param name="content">返回的实际数据内容</param>
		/// <returns>携带内容的modbus报文</returns>
		private byte[] CreateReadBack(byte[] modbusCore, byte[] content)
		{
			return SoftBasic.SpliceArray<byte>(new byte[3]
			{
				modbusCore[0],
				modbusCore[1],
				(byte)content.Length
			}, content);
		}

		/// <summary>
		/// 创建写入成功的反馈信号<br />
		/// Create feedback signal for successful write
		/// </summary>
		/// <param name="modbus">modbus核心报文</param>
		/// <returns>携带成功写入的信息</returns>
		private byte[] CreateWriteBack(byte[] modbus)
		{
			return modbus.SelectBegin(6);
		}

		private byte[] ReadCoilBack(byte[] modbus, string addressHead)
		{
			try
			{
				ushort num = base.ByteTransform.TransUInt16(modbus, 2);
				ushort num2 = base.ByteTransform.TransUInt16(modbus, 4);
				if (num + num2 > 65536)
				{
					return CreateExceptionBack(modbus, 2);
				}
				if (num2 > 2040)
				{
					return CreateExceptionBack(modbus, 3);
				}
				bool[] content = ReadBool(addressHead + num, num2).Content;
				return CreateReadBack(modbus, SoftBasic.BoolArrayToByte(content));
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpReadCoilException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] ReadRegisterBack(byte[] modbus, string addressHead)
		{
			try
			{
				ushort num = base.ByteTransform.TransUInt16(modbus, 2);
				ushort num2 = base.ByteTransform.TransUInt16(modbus, 4);
				if (num + num2 > 65536)
				{
					return CreateExceptionBack(modbus, 2);
				}
				if (num2 > 127)
				{
					return CreateExceptionBack(modbus, 3);
				}
				byte[] content = Read(addressHead + num, num2).Content;
				return CreateReadBack(modbus, content);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpReadRegisterException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] WriteOneCoilBack(byte[] modbus)
		{
			try
			{
				if (!base.EnableWrite)
				{
					return CreateExceptionBack(modbus, 4);
				}
				ushort num = base.ByteTransform.TransUInt16(modbus, 2);
				if (modbus[4] == byte.MaxValue && modbus[5] == 0)
				{
					Write(num.ToString(), value: true);
				}
				else if (modbus[4] == 0 && modbus[5] == 0)
				{
					Write(num.ToString(), value: false);
				}
				return CreateWriteBack(modbus);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpWriteCoilException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] WriteOneRegisterBack(byte[] modbus)
		{
			try
			{
				if (!base.EnableWrite)
				{
					return CreateExceptionBack(modbus, 4);
				}
				ushort address = base.ByteTransform.TransUInt16(modbus, 2);
				short content = ReadInt16(address.ToString()).Content;
				Write(address.ToString(), modbus[4], modbus[5]);
				short content2 = ReadInt16(address.ToString()).Content;
				OnRegisterBeforWrite(address, content, content2);
				return CreateWriteBack(modbus);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpWriteRegisterException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] WriteCoilsBack(byte[] modbus)
		{
			try
			{
				if (!base.EnableWrite)
				{
					return CreateExceptionBack(modbus, 4);
				}
				ushort num = base.ByteTransform.TransUInt16(modbus, 2);
				ushort num2 = base.ByteTransform.TransUInt16(modbus, 4);
				if (num + num2 > 65536)
				{
					return CreateExceptionBack(modbus, 2);
				}
				if (num2 > 2040)
				{
					return CreateExceptionBack(modbus, 3);
				}
				Write(num.ToString(), modbus.RemoveBegin(7).ToBoolArray(num2));
				return CreateWriteBack(modbus);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpWriteCoilException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] WriteRegisterBack(byte[] modbus)
		{
			try
			{
				if (!base.EnableWrite)
				{
					return CreateExceptionBack(modbus, 4);
				}
				ushort num = base.ByteTransform.TransUInt16(modbus, 2);
				ushort num2 = base.ByteTransform.TransUInt16(modbus, 4);
				if (num + num2 > 65536)
				{
					return CreateExceptionBack(modbus, 2);
				}
				if (num2 > 127)
				{
					return CreateExceptionBack(modbus, 3);
				}
				MonitorAddress[] array = new MonitorAddress[num2];
				for (ushort num3 = 0; num3 < num2; num3 = (ushort)(num3 + 1))
				{
					short content = ReadInt16((num + num3).ToString()).Content;
					Write((num + num3).ToString(), modbus[2 * num3 + 7], modbus[2 * num3 + 8]);
					short content2 = ReadInt16((num + num3).ToString()).Content;
					array[num3] = new MonitorAddress
					{
						Address = (ushort)(num + num3),
						ValueOrigin = content,
						ValueNew = content2
					};
				}
				for (int i = 0; i < array.Length; i++)
				{
					OnRegisterBeforWrite(array[i].Address, array[i].ValueOrigin, array[i].ValueNew);
				}
				return CreateWriteBack(modbus);
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpWriteRegisterException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		private byte[] WriteMaskRegisterBack(byte[] modbus)
		{
			try
			{
				if (!base.EnableWrite)
				{
					return CreateExceptionBack(modbus, 4);
				}
				ushort address = base.ByteTransform.TransUInt16(modbus, 2);
				int num = base.ByteTransform.TransUInt16(modbus, 4);
				int num2 = base.ByteTransform.TransUInt16(modbus, 6);
				int content = ReadInt16(address.ToString()).Content;
				short num3 = (short)((content & num) | num2);
				Write(address.ToString(), num3);
				MonitorAddress monitorAddress = default(MonitorAddress);
				monitorAddress.Address = address;
				monitorAddress.ValueOrigin = (short)content;
				monitorAddress.ValueNew = num3;
				MonitorAddress monitorAddress2 = monitorAddress;
				OnRegisterBeforWrite(monitorAddress2.Address, monitorAddress2.ValueOrigin, monitorAddress2.ValueNew);
				return modbus;
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), StringResources.Language.ModbusTcpWriteRegisterException, ex);
				return CreateExceptionBack(modbus, 4);
			}
		}

		/// <summary>
		/// 新增一个数据监视的任务，针对的是寄存器地址的数据<br />
		/// Added a data monitoring task for data at register addresses
		/// </summary>
		/// <param name="monitor">监视地址对象</param>
		public void AddSubcription(ModBusMonitorAddress monitor)
		{
			subcriptionHybirdLock.Enter();
			subscriptions.Add(monitor);
			subcriptionHybirdLock.Leave();
		}

		/// <summary>
		/// 移除一个数据监视的任务<br />
		/// Remove a data monitoring task
		/// </summary>
		/// <param name="monitor">监视地址对象</param>
		public void RemoveSubcrption(ModBusMonitorAddress monitor)
		{
			subcriptionHybirdLock.Enter();
			subscriptions.Remove(monitor);
			subcriptionHybirdLock.Leave();
		}

		/// <summary>
		/// 在数据变更后，进行触发是否产生订阅<br />
		/// Whether to generate a subscription after triggering data changes
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <param name="before">修改之前的数</param>
		/// <param name="after">修改之后的数</param>
		private void OnRegisterBeforWrite(ushort address, short before, short after)
		{
			subcriptionHybirdLock.Enter();
			for (int i = 0; i < subscriptions.Count; i++)
			{
				if (subscriptions[i].Address == address)
				{
					subscriptions[i].SetValue(after);
					if (before != after)
					{
						subscriptions[i].SetChangeValue(before, after);
					}
				}
			}
			subcriptionHybirdLock.Leave();
		}

		/// <summary>
		/// 检测当前的Modbus接收的指定是否是合法的<br />
		/// Check if the current Modbus datad designation is valid
		/// </summary>
		/// <param name="buffer">缓存数据</param>
		/// <returns>是否合格</returns>
		private bool CheckModbusMessageLegal(byte[] buffer)
		{
			bool flag = false;
			switch (buffer[1])
			{
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
			case 6:
				flag = buffer.Length == 6;
				break;
			case 15:
			case 16:
				flag = buffer.Length > 6 && buffer[6] == buffer.Length - 7;
				break;
			case 22:
				flag = buffer.Length == 8;
				break;
			default:
				flag = true;
				break;
			}
			if (!flag)
			{
				base.LogNet?.WriteError(ToString(), "Receive Nosense Modbus-rtu : " + buffer.ToHexString(' '));
			}
			return flag;
		}

		/// <summary>
		/// Modbus核心数据交互方法，允许重写自己来实现，报文只剩下核心的Modbus信息，去除了MPAB报头信息<br />
		/// The Modbus core data interaction method allows you to rewrite it to achieve the message. 
		/// Only the core Modbus information is left in the message, and the MPAB header information is removed.
		/// </summary>
		/// <param name="modbusCore">核心的Modbus报文</param>
		/// <returns>进行数据交互之后的结果</returns>
		protected virtual byte[] ReadFromModbusCore(byte[] modbusCore)
		{
			switch (modbusCore[1])
			{
			case 1:
				return ReadCoilBack(modbusCore, string.Empty);
			case 2:
				return ReadCoilBack(modbusCore, "x=2;");
			case 3:
				return ReadRegisterBack(modbusCore, string.Empty);
			case 4:
				return ReadRegisterBack(modbusCore, "x=4;");
			case 5:
				return WriteOneCoilBack(modbusCore);
			case 6:
				return WriteOneRegisterBack(modbusCore);
			case 15:
				return WriteCoilsBack(modbusCore);
			case 16:
				return WriteRegisterBack(modbusCore);
			case 22:
				return WriteMaskRegisterBack(modbusCore);
			default:
				return CreateExceptionBack(modbusCore, 1);
			}
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int datadLength)
		{
			if (datadLength > 5)
			{
				if (ModbusInfo.CheckAsciiReceiveDataComplete(buffer, datadLength))
				{
					return true;
				}
				if (ModbusInfo.CheckServerRtuReceiveDataComplete(buffer.SelectBegin(datadLength)))
				{
					return true;
				}
			}
			return false;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			if (data.Length < 3)
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Uknown Data：" + data.ToHexString(' '));
			}
			if (data[0] != 58)
			{
				base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Rtu " + StringResources.Language.Receive + "：" + data.ToHexString(' '));
				if (SoftCRC16.CheckCRC16(data))
				{
					byte[] array = data.RemoveLast(2);
					if (!CheckModbusMessageLegal(array))
					{
						return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Unlegal Data：" + data.ToHexString(' '));
					}
					if (station >= 0 && station != array[0])
					{
						return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Station not match Modbus-rtu : " + data.ToHexString(' '));
					}
					byte[] array2 = ModbusInfo.PackCommandToRtu(ReadFromModbusCore(array));
					base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Rtu " + StringResources.Language.Send + "：" + array2.ToHexString(' '));
					return OperateResult.CreateSuccessResult(array2);
				}
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] CRC Check Failed : " + data.ToHexString(' '));
			}
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Ascii " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			OperateResult<byte[]> operateResult = ModbusInfo.TransAsciiPackCommandToCore(data);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			byte[] content = operateResult.Content;
			if (!CheckModbusMessageLegal(content))
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Unlegal Data：" + data.ToHexString(' '));
			}
			if (station >= 0 && station != content[0])
			{
				return new OperateResult<byte[]>("[" + GetSerialPort().PortName + "] Station not match Modbus-Ascii : " + SoftBasic.GetAsciiStringRender(data));
			}
			byte[] array3 = ModbusInfo.TransModbusCoreToAsciiPackCommand(ReadFromModbusCore(content));
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] Ascii " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(array3));
			return OperateResult.CreateSuccessResult(array3);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				subcriptionHybirdLock?.Dispose();
				subscriptions?.Clear();
				coilBuffer?.Dispose();
				inputBuffer?.Dispose();
				registerBuffer?.Dispose();
				inputRegisterBuffer?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt32Array", "")]
		public override OperateResult<int[]> ReadInt32(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt32Array", "")]
		public override OperateResult<uint[]> ReadUInt32(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloat(System.String,System.UInt16)" />
		[HslMqttApi("ReadFloatArray", "")]
		public override OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt64Array", "")]
		public override OperateResult<long[]> ReadInt64(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt64Array", "")]
		public override OperateResult<ulong[]> ReadUInt64(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDouble(System.String,System.UInt16)" />
		[HslMqttApi("ReadDoubleArray", "")]
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int32[])" />
		[HslMqttApi("WriteInt32Array", "")]
		public override OperateResult Write(string address, int[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt32[])" />
		[HslMqttApi("WriteUInt32Array", "")]
		public override OperateResult Write(string address, uint[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Single[])" />
		[HslMqttApi("WriteFloatArray", "")]
		public override OperateResult Write(string address, float[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int64[])" />
		[HslMqttApi("WriteInt64Array", "")]
		public override OperateResult Write(string address, long[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt64[])" />
		[HslMqttApi("WriteUInt64Array", "")]
		public override OperateResult Write(string address, ulong[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Double[])" />
		[HslMqttApi("WriteDoubleArray", "")]
		public override OperateResult Write(string address, double[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloatAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 2)), (byte[] m) => transform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDoubleAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, (ushort)(length * base.WordLength * 4)), (byte[] m) => transform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int32[])" />
		public override async Task<OperateResult> WriteAsync(string address, int[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt32[])" />
		public override async Task<OperateResult> WriteAsync(string address, uint[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Single[])" />
		public override async Task<OperateResult> WriteAsync(string address, float[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int64[])" />
		public override async Task<OperateResult> WriteAsync(string address, long[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt64[])" />
		public override async Task<OperateResult> WriteAsync(string address, ulong[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Double[])" />
		public override async Task<OperateResult> WriteAsync(string address, double[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ModbusTcpServer[{base.Port}]";
		}
	}
}

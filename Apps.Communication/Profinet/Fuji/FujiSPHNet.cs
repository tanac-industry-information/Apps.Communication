using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Fuji
{
	/// <summary>
	/// 富士PLC的SPH通信协议，可以和富士PLC进行通信，<see cref="P:Communication.Profinet.Fuji.FujiSPHNet.ConnectionID" />默认CPU0，需要根据实际进行调整。
	/// </summary>
	/// <remarks>
	/// 地址支持 M1.0, M3.0, M10.0 以及I0, Q0
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term></term>
	///     <term>M</term>
	///     <term>M1.0,M1.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>M</term>
	///     <term>M3.0,M3.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>M</term>
	///     <term>M10.0,M10.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>I</term>
	///     <term>I0,I100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Q</term>
	///     <term>Q0,Q100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class FujiSPHNet : NetworkDeviceBase
	{
		/// <summary>
		/// 对于 CPU0-CPU7来说是CPU的站号，分为对应 0xFE-0xF7，对于P/PE link, FL-net是模块站号，分别对应0xF6-0xEF<br />
		/// CPU0 to CPU7: SX bus station No. of destination CPU (FEh to F7h); P/PE link, FL-net: SX bus station No. of destination module (F6H to EFH)
		/// </summary>
		public byte ConnectionID { get; set; } = 254;


		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public FujiSPHNet()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <summary>
		/// 指定IP地址和端口号来实例化一个对象<br />
		/// Specify the IP address and port number to instantiate an object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public FujiSPHNet(string ipAddress, int port = 18245)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new FujiSPHMessage();
		}

		private OperateResult<byte[]> ReadFujiSPHAddress(FujiSPHAddress address, ushort length)
		{
			OperateResult<List<byte[]>> operateResult = BuildReadCommand(ConnectionID, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			List<byte> list = new List<byte>();
			for (int i = 0; i < operateResult.Content.Count; i++)
			{
				OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content[i]);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				OperateResult<byte[]> operateResult3 = ExtractActualData(operateResult2.Content);
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				list.AddRange(operateResult3.Content);
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// 批量读取PLC的地址数据，长度单位为字。地址支持M1.1000，M3.1000，M10.1000，返回读取的原始字节数组。<br />
		/// Read PLC address data in batches, the length unit is words. The address supports M1.1000, M3.1000, M10.1000, 
		/// and returns the original byte array read.
		/// </summary>
		/// <param name="address">PLC的地址，支持M1.1000，M3.1000，M10.1000</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>包含byte[]的原始字节数据内容</returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			return ReadFujiSPHAddress(operateResult.Content, length);
		}

		/// <summary>
		/// 批量写入字节数组到PLC的地址里，地址支持M1.1000，M3.1000，M10.1000，返回是否写入成功。<br />
		/// Batch write byte array to PLC address, the address supports M1.1000, M3.1000, M10.1000, 
		/// and return whether the writing is successful.
		/// </summary>
		/// <param name="address">PLC的地址，支持M1.1000，M3.1000，M10.1000</param>
		/// <param name="value">写入的原始字节数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteCommand(ConnectionID, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ExtractActualData(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 批量读取位数据的方法，需要传入位地址，读取的位长度，地址示例：M1.100.5，M3.1000.12，M10.1000.0<br />
		/// To read the bit data in batches, you need to pass in the bit address, the length of the read bit, address examples: M1.100.5, M3.1000.12, M10.1000.0
		/// </summary>
		/// <param name="address">PLC的地址，示例：M1.100.5，M3.1000.12，M10.1000.0</param>
		/// <param name="length">读取的bool长度信息</param>
		/// <returns>包含bool[]的结果对象</returns>
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			int num = operateResult.Content.BitIndex + length;
			int num2 = ((num % 16 == 0) ? (num / 16) : (num / 16 + 1));
			OperateResult<byte[]> operateResult2 = ReadFujiSPHAddress(operateResult.Content, (ushort)num2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2.ConvertFailed<bool[]>();
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.ToBoolArray().SelectMiddle(operateResult.Content.BitIndex, length));
		}

		/// <summary>
		/// 批量写入位数据的方法，需要传入位地址，等待写入的boo[]数据，地址示例：M1.100.5，M3.1000.12，M10.1000.0<br />
		/// To write bit data in batches, you need to pass in the bit address and wait for the boo[] data to be written. Examples of addresses: M1.100.5, M3.1000.12, M10.1000.0
		/// </summary>
		/// <remarks>
		/// [警告] 由于协议没有提供位写入的命令，所有通过字写入间接实现，先读取字数据，修改中间的位，然后写入字数据，所以本质上不是安全的，确保相关的地址只有上位机可以写入。<br />
		/// [Warning] Since the protocol does not provide commands for bit writing, all are implemented indirectly through word writing. First read the word data, 
		/// modify the bits in the middle, and then write the word data, so it is inherently not safe. Make sure that the relevant address is only The host computer can write.
		/// </remarks>
		/// <param name="address">PLC的地址，示例：M1.100.5，M3.1000.12，M10.1000.0</param>
		/// <param name="value">等待写入的bool数组</param>
		/// <returns>是否写入成功的结果对象</returns>
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			int num = operateResult.Content.BitIndex + value.Length;
			int num2 = ((num % 16 == 0) ? (num / 16) : (num / 16 + 1));
			OperateResult<byte[]> operateResult2 = ReadFujiSPHAddress(operateResult.Content, (ushort)num2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2.ConvertFailed<bool[]>();
			}
			bool[] array = operateResult2.Content.ToBoolArray();
			value.CopyTo(array, operateResult.Content.BitIndex);
			OperateResult<byte[]> operateResult3 = BuildWriteCommand(ConnectionID, address, array.ToByteArray());
			if (!operateResult3.IsSuccess)
			{
				return operateResult3.ConvertFailed<byte[]>();
			}
			OperateResult<byte[]> operateResult4 = ReadFromCoreServer(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return operateResult4;
			}
			OperateResult<byte[]> operateResult5 = ExtractActualData(operateResult4.Content);
			if (!operateResult5.IsSuccess)
			{
				return operateResult5;
			}
			return OperateResult.CreateSuccessResult();
		}

		private async Task<OperateResult<byte[]>> ReadFujiSPHAddressAsync(FujiSPHAddress address, ushort length)
		{
			OperateResult<List<byte[]>> command = BuildReadCommand(ConnectionID, address, length);
			if (!command.IsSuccess)
			{
				return command.ConvertFailed<byte[]>();
			}
			List<byte> array = new List<byte>();
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content[i]);
				if (!read.IsSuccess)
				{
					return read;
				}
				OperateResult<byte[]> extra = ExtractActualData(read.Content);
				if (!extra.IsSuccess)
				{
					return extra;
				}
				array.AddRange(extra.Content);
			}
			return OperateResult.CreateSuccessResult(array.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom(address);
			if (!analysis.IsSuccess)
			{
				return analysis.ConvertFailed<byte[]>();
			}
			return await ReadFujiSPHAddressAsync(analysis.Content, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> command = BuildWriteCommand(ConnectionID, address, value);
			if (!command.IsSuccess)
			{
				return command.ConvertFailed<byte[]>();
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult<byte[]> extra = ExtractActualData(read.Content);
			if (!extra.IsSuccess)
			{
				return extra;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom(address);
			if (!analysis.IsSuccess)
			{
				return analysis.ConvertFailed<bool[]>();
			}
			int bitCount = analysis.Content.BitIndex + length;
			OperateResult<byte[]> read = await ReadFujiSPHAddressAsync(length: (ushort)((bitCount % 16 == 0) ? (bitCount / 16) : (bitCount / 16 + 1)), address: analysis.Content);
			if (!read.IsSuccess)
			{
				return read.ConvertFailed<bool[]>();
			}
			return OperateResult.CreateSuccessResult(read.Content.ToBoolArray().SelectMiddle(analysis.Content.BitIndex, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] value)
		{
			OperateResult<FujiSPHAddress> analysis = FujiSPHAddress.ParseFrom(address);
			if (!analysis.IsSuccess)
			{
				return analysis.ConvertFailed<bool[]>();
			}
			int bitCount = analysis.Content.BitIndex + value.Length;
			OperateResult<byte[]> read = await ReadFujiSPHAddressAsync(length: (ushort)((bitCount % 16 == 0) ? (bitCount / 16) : (bitCount / 16 + 1)), address: analysis.Content);
			if (!read.IsSuccess)
			{
				return read.ConvertFailed<bool[]>();
			}
			bool[] writeBoolArray = read.Content.ToBoolArray();
			value.CopyTo(writeBoolArray, analysis.Content.BitIndex);
			OperateResult<byte[]> command = BuildWriteCommand(ConnectionID, address, writeBoolArray.ToByteArray());
			if (!command.IsSuccess)
			{
				return command.ConvertFailed<byte[]>();
			}
			OperateResult<byte[]> write = await ReadFromCoreServerAsync(command.Content);
			if (!write.IsSuccess)
			{
				return write;
			}
			OperateResult<byte[]> extra = ExtractActualData(write.Content);
			if (!extra.IsSuccess)
			{
				return extra;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to start all the CPUs that exist in a configuration in a batch. 
		/// Each CPU is cold-started or warm-started,depending on its condition. If a CPU is already started up, 
		/// or if the key switch is set at "RUN" position, the CPU does not perform processing for startup, 
		/// which, however, does not result in an error, and a response is returned normally
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchStart()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 0, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to initialize and start all the CPUs that exist in a configuration in a batch. Each CPU is cold-started.
		/// If a CPU is already started up, or if the key switch is set at "RUN" position, the CPU does not perform processing for initialization 
		/// and startup, which, however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchInitializeAndStart()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 1, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop all the CPUs that exist in a configuration in a batch.
		/// If a CPU is already stopped, or if the key switch is set at "RUN" position, the CPU does not perform processing for stop, which,
		/// however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否停止成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchStop()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 2, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop all the CPUs that exist in a configuration in a batch.
		/// If a CPU is already stopped, or if the key switch is set at "RUN" position, the CPU does not perform processing for stop, which,
		/// however, does not result in an error, and a response is returned normally.
		/// </summary>
		/// <returns>是否复位成功</returns>
		[HslMqttApi]
		public OperateResult CpuBatchReset()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 3, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to start an arbitrary CPU existing in a configuration by specifying it. The CPU may be cold-started or
		/// warm-started, depending on its condition. An error occurs if the CPU is already started. A target CPU is specified by a connection
		/// mode and connection ID.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualStart()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 4, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to initialize and start an arbitrary CPU existing in a configuration by specifying it. The CPU is cold-started.
		/// An error occurs if the CPU is already started or if the key switch is set at "RUN" or "STOP" position. A target CPU is specified by
		/// a connection mode and connection ID.
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualInitializeAndStart()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 5, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to stop an arbitrary CPU existing in a configuration by specifying it. An error occurs if the CPU is already
		/// stopped or if the key switch is set at "RUN" or "STOP" position. A target CPU is specified by a connection mode and connection ID.
		/// </summary>
		/// <returns>是否停止成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualStop()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 6, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <summary>
		/// <b>[Authorization]</b> This command is used to reset an arbitrary CPU existing in a configuration by specifying it. An error occurs if the key switch is
		/// set at "RUN" or "STOP" position. A target CPU is specified by a connection mode and connection ID.
		/// </summary>
		/// <returns>是否复位成功</returns>
		[HslMqttApi]
		public OperateResult CpuIndividualReset()
		{

			return ReadFromCoreServer(PackCommand(ConnectionID, 4, 7, null)).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuBatchStart" />
		public async Task<OperateResult> CpuBatchStartAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 0, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuBatchInitializeAndStart" />
		public async Task<OperateResult> CpuBatchInitializeAndStartAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 1, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuBatchStop" />
		public async Task<OperateResult> CpuBatchStopAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 2, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuBatchReset" />
		public async Task<OperateResult> CpuBatchResetAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 3, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuIndividualStart" />
		public async Task<OperateResult> CpuIndividualStartAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 4, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuIndividualInitializeAndStartAsync" />
		public async Task<OperateResult> CpuIndividualInitializeAndStartAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 5, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuIndividualStop" />
		public async Task<OperateResult> CpuIndividualStopAsync()
		{
			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 6, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.CpuIndividualReset" />
		public async Task<OperateResult> CpuIndividualResetAsync()
		{

			return (await ReadFromCoreServerAsync(PackCommand(ConnectionID, 4, 7, null))).Check((Func<byte[], OperateResult>)ExtractActualData);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FujiSPHNet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 根据错误代号获取详细的错误描述信息
		/// </summary>
		/// <param name="code">错误码</param>
		/// <returns>错误的描述文本</returns>
		public static string GetErrorDescription(byte code)
		{
			switch (code)
			{
			case 16:
				return "Command cannot be executed because an error occurred in the CPU.";
			case 17:
				return "Command cannot be executed because the CPU is running.";
			case 18:
				return "Command cannot be executed due to the key switch condition of the CPU.";
			case 32:
				return "CPU received undefined command or mode.";
			case 34:
				return "Setting error was found in command header part.";
			case 35:
				return "Transmission is interlocked by a command from another device.";
			case 40:
				return "Requested command cannot be executed because another command is now being executed.";
			case 43:
				return "Requested command cannot be executed because the loader is now performing another processing( including program change).";
			case 47:
				return "Requested command cannot be executed because the system is now being initialized.";
			case 64:
				return "Invalid data type or number was specified.";
			case 65:
				return "Specified data cannot be found.";
			case 68:
				return "Specified address exceeds the valid range.";
			case 69:
				return "Address + the number of read/write words exceed the valid range.";
			case 160:
				return "No module exists at specified destination station No.";
			case 162:
				return "No response data is returned from the destination module.";
			case 164:
				return "Command cannot be communicated because an error occurred in the SX bus.";
			case 165:
				return "Command cannot be communicated because NAK occurred while sending data via the SX bus.";
			default:
				return StringResources.Language.UnknownError;
			}
		}

		private static byte[] PackCommand(byte connectionId, byte command, byte mode, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[20 + data.Length];
			array[0] = 251;
			array[1] = 128;
			array[2] = 128;
			array[3] = 0;
			array[4] = byte.MaxValue;
			array[5] = 123;
			array[6] = connectionId;
			array[7] = 0;
			array[8] = 17;
			array[9] = 0;
			array[10] = 0;
			array[11] = 0;
			array[12] = 0;
			array[13] = 0;
			array[14] = command;
			array[15] = mode;
			array[16] = 0;
			array[17] = 1;
			array[18] = BitConverter.GetBytes(data.Length)[0];
			array[19] = BitConverter.GetBytes(data.Length)[1];
			if (data.Length != 0)
			{
				data.CopyTo(array, 20);
			}
			return array;
		}

		/// <summary>
		/// 构建读取数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">读取的PLC的地址</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>构建成功的读取报文命令</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand(byte connectionId, string address, ushort length)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<List<byte[]>>();
			}
			return BuildReadCommand(connectionId, operateResult.Content, length);
		}

		/// <summary>
		/// 构建读取数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">读取的PLC的地址</param>
		/// <param name="length">读取的长度信息，按照字为单位</param>
		/// <returns>构建成功的读取报文命令</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand(byte connectionId, FujiSPHAddress address, ushort length)
		{
			List<byte[]> list = new List<byte[]>();
			int[] array = SoftBasic.SplitIntegerToArray(length, 230);
			for (int i = 0; i < array.Length; i++)
			{
				list.Add(PackCommand(connectionId, 0, 0, new byte[6]
				{
					address.TypeCode,
					BitConverter.GetBytes(address.AddressStart)[0],
					BitConverter.GetBytes(address.AddressStart)[1],
					BitConverter.GetBytes(address.AddressStart)[2],
					BitConverter.GetBytes(array[i])[0],
					BitConverter.GetBytes(array[i])[1]
				}));
				address.AddressStart += array[i];
			}
			return OperateResult.CreateSuccessResult(list);
		}

		/// <summary>
		/// 构建写入数据的命令报文
		/// </summary>
		/// <param name="connectionId">连接ID</param>
		/// <param name="address">写入的PLC的地址</param>
		/// <param name="data">原始数据内容</param>
		/// <returns>报文信息</returns>
		public static OperateResult<byte[]> BuildWriteCommand(byte connectionId, string address, byte[] data)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			int value = data.Length / 2;
			byte[] array = new byte[6 + data.Length];
			array[0] = operateResult.Content.TypeCode;
			array[1] = BitConverter.GetBytes(operateResult.Content.AddressStart)[0];
			array[2] = BitConverter.GetBytes(operateResult.Content.AddressStart)[1];
			array[3] = BitConverter.GetBytes(operateResult.Content.AddressStart)[2];
			array[4] = BitConverter.GetBytes(value)[0];
			array[5] = BitConverter.GetBytes(value)[1];
			data.CopyTo(array, 6);
			return OperateResult.CreateSuccessResult(PackCommand(connectionId, 1, 0, array));
		}

		/// <summary>
		/// 从PLC返回的报文里解析出实际的数据内容，如果发送了错误，则返回失败信息
		/// </summary>
		/// <param name="response">PLC返回的报文信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> ExtractActualData(byte[] response)
		{
			try
			{
				if (response[4] != 0)
				{
					return new OperateResult<byte[]>(response[4], GetErrorDescription(response[4]));
				}
				if (response.Length > 26)
				{
					return OperateResult.CreateSuccessResult(response.RemoveBegin(26));
				}
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(ex.Message + " Source: " + response.ToHexString(' '));
			}
		}
	}
}

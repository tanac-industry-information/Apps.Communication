using System;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.YASKAWA
{
	/// <summary>
	/// 扩展的Memobus协议信息，除了支持普通的线圈，输入继电器，保持寄存器，输入寄存器的读写操作，还支持扩展的保持寄存器和输入寄存器读写操作。<br />
	/// The extended Memobus protocol information not only supports reading and writing operations of ordinary coils, input relays, 
	/// holding registers, and input registers, but also supports reading and writing operations of extended holding registers and input registers.
	/// </summary>
	/// <remarks>
	/// 其中线圈和输入继电器使用<see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.ReadBool(System.String,System.UInt16)" />和<see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean)" />,<see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean[])" />的方法，读取输入继电器地址：x=2;100。
	/// 其他的方法针对的是寄存器，保持型寄存器地址：100或 x=3;100，输入寄存器：x=4;100，扩展保持型寄存器x=9;100，写入x=11;100, 扩展输入寄存器：x=10;100<br />
	/// The coil and input relay use <see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.ReadBool(System.String,System.UInt16)" /> and <see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean)" />,<see cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean[])" /> method, 
	/// read the input relay address: x=2;100. Other methods are for registers, holding register address: 100 or x=3;100, input register: x=4;100, 
	/// extended holding register x=9;100, writing x=11;100, extended input Register: x=10;100
	/// <br /><br />
	/// 读取的最大的字为 2044 个字，写入的最大的字数为 2043 个字
	/// </remarks>
	public class MemobusTcpNet : NetworkDeviceBase
	{
		private byte cpuTo = 2;

		private byte cpuFrom = 1;

		private readonly SoftIncrementCount softIncrementCount;

		/// <summary>
		/// 获取或设置发送目标的CPU的编号信息，默认为 2<br />
		/// Get or set the CPU number information of the sending destination, the default is 2
		/// </summary>
		public byte CpuTo
		{
			get
			{
				return cpuTo;
			}
			set
			{
				cpuTo = value;
			}
		}

		/// <summary>
		/// 获取或设置发送源的CPU的编号信息，默认为 1<br />
		/// Get or set the number information of the sending source CPU, the default is 1
		/// </summary>
		public byte CpuFrom
		{
			get
			{
				return cpuFrom;
			}
			set
			{
				cpuFrom = value;
			}
		}

		/// <summary>
		/// 实例化一个Memobus-Tcp协议的客户端对象<br />
		/// Instantiate a client object of the Memobus-Tcp protocol
		/// </summary>
		public MemobusTcpNet()
		{
			softIncrementCount = new SoftIncrementCount(255L, 0L);
			base.WordLength = 1;
			base.ByteTransform = new ReverseWordTransform();
			base.ByteTransform.DataFormat = DataFormat.DCBA;
		}

		/// <summary>
		/// 指定服务器地址，端口号，客户端自己的站号来初始化<br />
		/// Specify the server address, port number, and client's own station number to initialize
		/// </summary>
		/// <param name="ipAddress">服务器的Ip地址</param>
		/// <param name="port">服务器的端口号</param>
		public MemobusTcpNet(string ipAddress, int port = 502)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new MemobusMessage();
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			byte[] array = new byte[12 + command.Length];
			array[0] = 17;
			array[1] = (byte)softIncrementCount.GetCurrentValue();
			array[2] = 0;
			array[3] = 0;
			array[6] = BitConverter.GetBytes(array.Length)[0];
			array[7] = BitConverter.GetBytes(array.Length)[1];
			command.CopyTo(array, 12);
			return array;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			if (send.Length > 15 && response.Length > 15 && send[15] != response[15])
			{
				return new OperateResult<byte[]>(response[15], "Send SFC not same as back SFC:" + response.ToHexString());
			}
			return OperateResult.CreateSuccessResult(response.RemoveBegin(12));
		}

		/// <inheritdoc />
		/// <remarks>
		/// 默认使用功能码01，读取线圈操作，如果需要指定读取输入线圈，地址需要携带额外的参数，例如 x=2;100<br />
		/// The function code 01 is used by default to read the coil operation. If you need to specify the read input coil, the address needs to carry additional parameters, such as x=2;100
		/// </remarks>
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 1);
			OperateResult<byte[]> operateResult = BulidReadCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.RemoveBegin(5).ToBoolArray().SelectBegin(length));
		}

		/// <inheritdoc />
		/// <remarks>
		/// 单一线圈的状态变更，使用的主功能码为0x20, 子功能码为0x05<br />
		/// The status of a single coil is changed, the main function code used is 0x20, and the sub function code is 0x05
		/// </remarks>
		public override OperateResult Write(string address, bool value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 5);
			OperateResult<byte[]> operateResult = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		/// <remarks>
		/// 多个线圈的状态更改，默认使用的是 0x0f 子功能码。<br />
		/// The status of multiple coils is changed, and the sub-function code 0x0f is used by default.
		/// </remarks>
		public override OperateResult Write(string address, bool[] value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 15);
			OperateResult<byte[]> operateResult = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		/// <remarks>
		/// 地址默认使用功能码03，如果需要指定其他的功能码地址，需要手动指定功能码，例如：x=4;100, x=9;100, x=10;100, 当然也可以写成 x=0x0A;100<br />
		/// The address uses function code 03 by default. If you need to specify other function code addresses, 
		/// you need to manually specify the function code, for example: x=4;100, x=9;100, x=10;100, of course, it can also be written as x=0x0A; 100
		/// </remarks>
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 3);
			OperateResult<byte[]> operateResult = BulidReadCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			if (operateResult2.Content[3] == 3 || operateResult2.Content[3] == 4)
			{
				return OperateResult.CreateSuccessResult(operateResult2.Content.RemoveBegin(5));
			}
			if (operateResult2.Content[3] == 9 || operateResult2.Content[3] == 10)
			{
				return OperateResult.CreateSuccessResult(SoftBasic.BytesReverseByWord(operateResult2.Content.RemoveBegin(8)));
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.RemoveBegin(5));
		}

		/// <inheritdoc />
		/// <remarks>
		/// 连续的寄存器写入操作，默认功能码是0x10，如果需要写入扩展的寄存器，使用 x=0xA;100 或是 x=10;100 即可。<br />
		/// For continuous register write operation, the default function code is 0x10. If you need to write an extended register, use x=0xA;100 or x=10;100.
		/// </remarks>
		public override OperateResult Write(string address, byte[] value)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "x", 16);
			if (b == 3)
			{
				b = 16;
			}
			if (b == 9)
			{
				b = 11;
			}
			OperateResult<byte[]> operateResult = BuildWriteCommand(32, b, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		/// <remarks>
		/// 单一保持寄存器的值变更，使用的主功能码为0x20, 默认子功能码为0x06，也可以写入扩展的保持型寄存器，子功能码为0x0B
		/// </remarks>
		public override OperateResult Write(string address, short value)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "x", 6);
			if (b == 11 || b == 9)
			{
				return base.Write($"x={b};{address}", value);
			}
			if (b == 3)
			{
				b = 6;
			}
			OperateResult<byte[]> operateResult = BuildWriteCommand(32, b, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		/// <remarks>
		/// 单一保持寄存器的值变更，使用的主功能码为0x20, 默认子功能码为0x06
		/// </remarks>
		public override OperateResult Write(string address, ushort value)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "x", 6);
			if (b == 11 || b == 9)
			{
				return base.Write($"x={b};{address}", value);
			}
			if (b == 3)
			{
				b = 6;
			}
			OperateResult<byte[]> operateResult = BuildWriteCommand(32, b, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 1);
			OperateResult<byte[]> command = BulidReadCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), length);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content.RemoveBegin(5).ToBoolArray().SelectBegin(length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 5);
			OperateResult<byte[]> command = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 15);
			OperateResult<byte[]> command = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 3);
			OperateResult<byte[]> command = BulidReadCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			if (read.Content[3] == 3 || read.Content[3] == 4)
			{
				return OperateResult.CreateSuccessResult(read.Content.RemoveBegin(5));
			}
			if (read.Content[3] == 9 || read.Content[3] == 10)
			{
				return OperateResult.CreateSuccessResult(SoftBasic.BytesReverseByWord(read.Content.RemoveBegin(8)));
			}
			return OperateResult.CreateSuccessResult(read.Content.RemoveBegin(5));
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 16);
			if (sfc == 3)
			{
				sfc = 16;
			}
			if (sfc == 9)
			{
				sfc = 11;
			}
			OperateResult<byte[]> command = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.Int16)" />
		public override async Task<OperateResult> WriteAsync(string address, short value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 6);
			if (sfc == 11 || sfc == 9)
			{
				return base.Write($"x={sfc};{address}", value);
			}
			if (sfc == 3)
			{
				sfc = 6;
			}
			OperateResult<byte[]> command = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.YASKAWA.MemobusTcpNet.Write(System.String,System.UInt16)" />
		public override async Task<OperateResult> WriteAsync(string address, ushort value)
		{
			byte sfc = (byte)HslHelper.ExtractParameter(ref address, "x", 6);
			if (sfc == 11 || sfc == 9)
			{
				return base.Write($"x={sfc};{address}", value);
			}
			if (sfc == 3)
			{
				sfc = 6;
			}
			OperateResult<byte[]> command = BuildWriteCommand(32, sfc, cpuTo, cpuFrom, ushort.Parse(address), value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MemobusTcpNet[{IpAddress}:{Port}]";
		}

		private static void SetByteHead(byte[] buffer, byte mfc, byte sfc, byte cpuTo, byte cpuFrom)
		{
			buffer[0] = BitConverter.GetBytes(buffer.Length - 2)[0];
			buffer[1] = BitConverter.GetBytes(buffer.Length - 2)[1];
			buffer[2] = mfc;
			buffer[3] = sfc;
			buffer[4] = (byte)((cpuTo << 4) + cpuFrom);
		}

		/// <summary>
		/// 构建读取的命令报文，支持功能码 01,02,03,04,09,0A
		/// </summary>
		/// <param name="mfc">主功能码</param>
		/// <param name="sfc">子功能码</param>
		/// <param name="cpuTo">目标的CPU编号</param>
		/// <param name="cpuFrom">发送源CPU编号</param>
		/// <param name="address">起始地址</param>
		/// <param name="length">读取地址长度</param>
		/// <returns>结果报文信息</returns>
		private static OperateResult<byte[]> BulidReadCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, ushort length)
		{
			if (sfc == 1 || sfc == 2 || sfc == 3 || sfc == 4)
			{
				byte[] array = new byte[9];
				SetByteHead(array, mfc, sfc, cpuTo, cpuFrom);
				array[5] = BitConverter.GetBytes(address)[1];
				array[6] = BitConverter.GetBytes(address)[0];
				array[7] = BitConverter.GetBytes(length)[1];
				array[8] = BitConverter.GetBytes(length)[0];
				return OperateResult.CreateSuccessResult(array);
			}
			if (sfc == 9 || sfc == 10)
			{
				byte[] array2 = new byte[10];
				SetByteHead(array2, mfc, sfc, cpuTo, cpuFrom);
				array2[5] = BitConverter.GetBytes(address)[0];
				array2[6] = BitConverter.GetBytes(address)[1];
				array2[7] = BitConverter.GetBytes(length)[0];
				array2[8] = BitConverter.GetBytes(length)[1];
				return OperateResult.CreateSuccessResult(array2);
			}
			return new OperateResult<byte[]>($"SFC:{sfc} {StringResources.Language.NotSupportedFunction}");
		}

		/// <summary>
		/// 构建写入单一的线圈的状态变更的报文
		/// </summary>
		/// <param name="mfc">主功能码</param>
		/// <param name="sfc">子功能码</param>
		/// <param name="cpuTo">目标的CPU编号</param>
		/// <param name="cpuFrom">发送源CPU编号</param>
		/// <param name="address">起始地址</param>
		/// <param name="value">写入的通断值信息</param>
		/// <returns>写入的报文</returns>
		private static OperateResult<byte[]> BuildWriteCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, bool value)
		{
			byte[] array = new byte[9];
			SetByteHead(array, mfc, sfc, cpuTo, cpuFrom);
			array[5] = BitConverter.GetBytes(address)[1];
			array[6] = BitConverter.GetBytes(address)[0];
			array[7] = (byte)(value ? 255u : 0u);
			array[8] = 0;
			return OperateResult.CreateSuccessResult(array);
		}

		private static OperateResult<byte[]> BuildWriteCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, bool[] value)
		{
			byte[] array = SoftBasic.BoolArrayToByte(value);
			byte[] array2 = new byte[9 + array.Length];
			SetByteHead(array2, mfc, sfc, cpuTo, cpuFrom);
			array2[5] = BitConverter.GetBytes(address)[1];
			array2[6] = BitConverter.GetBytes(address)[0];
			array2[7] = BitConverter.GetBytes(value.Length)[1];
			array2[8] = BitConverter.GetBytes(value.Length)[0];
			array.CopyTo(array2, 9);
			return OperateResult.CreateSuccessResult(array2);
		}

		private static OperateResult<byte[]> BuildWriteCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, short value)
		{
			byte[] array = new byte[9];
			SetByteHead(array, mfc, sfc, cpuTo, cpuFrom);
			array[5] = BitConverter.GetBytes(address)[1];
			array[6] = BitConverter.GetBytes(address)[0];
			array[7] = BitConverter.GetBytes(value)[1];
			array[8] = BitConverter.GetBytes(value)[0];
			return OperateResult.CreateSuccessResult(array);
		}

		private static OperateResult<byte[]> BuildWriteCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, ushort value)
		{
			byte[] array = new byte[9];
			SetByteHead(array, mfc, sfc, cpuTo, cpuFrom);
			array[5] = BitConverter.GetBytes(address)[1];
			array[6] = BitConverter.GetBytes(address)[0];
			array[7] = BitConverter.GetBytes(value)[1];
			array[8] = BitConverter.GetBytes(value)[0];
			return OperateResult.CreateSuccessResult(array);
		}

		private static OperateResult<byte[]> BuildWriteCommand(byte mfc, byte sfc, byte cpuTo, byte cpuFrom, ushort address, byte[] value)
		{
			switch (sfc)
			{
			case 11:
			{
				byte[] array2 = new byte[10 + value.Length];
				SetByteHead(array2, mfc, sfc, cpuTo, cpuFrom);
				array2[6] = BitConverter.GetBytes(address)[0];
				array2[7] = BitConverter.GetBytes(address)[1];
				array2[8] = BitConverter.GetBytes(value.Length / 2)[0];
				array2[9] = BitConverter.GetBytes(value.Length / 2)[1];
				SoftBasic.BytesReverseByWord(value).CopyTo(array2, 10);
				return OperateResult.CreateSuccessResult(array2);
			}
			case 16:
			{
				byte[] array = new byte[9 + value.Length];
				SetByteHead(array, mfc, sfc, cpuTo, cpuFrom);
				array[5] = BitConverter.GetBytes(address)[1];
				array[6] = BitConverter.GetBytes(address)[0];
				array[7] = BitConverter.GetBytes(value.Length / 2)[1];
				array[8] = BitConverter.GetBytes(value.Length / 2)[0];
				value.CopyTo(array, 9);
				return OperateResult.CreateSuccessResult(array);
			}
			default:
				return new OperateResult<byte[]>($"SFC:{sfc} {StringResources.Language.NotSupportedFunction}");
			}
		}
	}
}

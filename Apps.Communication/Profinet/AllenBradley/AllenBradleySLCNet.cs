using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.AllenBradley
{
	/// <summary>
	/// AllenBradley品牌的PLC，针对SLC系列的通信的实现，测试PLC为1747。<br />
	/// AllenBradley brand PLC, for the realization of SLC series communication, the test PLC is 1747.
	/// </summary>
	/// <remarks>
	/// 地址支持的举例：A9:0, N9:0, B9:0, F9:0, S:0, C9:0, I:0/10, O:0/1, R9:0, T9:0
	/// </remarks>
	/// <example>
	/// 地址格式如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址代号</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>A</term>
	///     <term>A9:0</term>
	///     <term>A9:0/1 或 A9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>B</term>
	///     <term>B9:0</term>
	///     <term>B9:0/1 或 B9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>N</term>
	///     <term>N9:0</term>
	///     <term>N9:0/1 或 N9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>F</term>
	///     <term>F9:0</term>
	///     <term>F9:0/1 或 F9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>S</term>
	///     <term>S:0</term>
	///     <term>S:0/1 或 S:0.1</term>
	///     <term>S:0 等同于 S2:0</term>
	///   </item>
	///   <item>
	///     <term>C</term>
	///     <term>C9:0</term>
	///     <term>C9:0/1 或 C9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>I</term>
	///     <term>I:0</term>
	///     <term>I:0/1 或 I9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>O</term>
	///     <term>O:0</term>
	///     <term>O:0/1 或 O9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>R</term>
	///     <term>R9:0</term>
	///     <term>R9:0/1 或 R9:0.1</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>T</term>
	///     <term>T9:0</term>
	///     <term>T9:0/1 或 T9:0.1</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 感谢 seedee 的测试支持。
	/// </example>
	public class AllenBradleySLCNet : NetworkDeviceBase
	{
		/// <summary>
		/// The current session handle, which is determined by the PLC when communicating with the PLC handshake
		/// </summary>
		public uint SessionHandle { get; protected set; }

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		public AllenBradleySLCNet()
		{
			base.WordLength = 2;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		/// <param name="ipAddress">PLC IpAddress</param>
		/// <param name="port">PLC Port</param>
		public AllenBradleySLCNet(string ipAddress, int port = 44818)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new AllenBradleySLCMessage();
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, "01 01 00 00 00 00 00 00 00 00 00 00 00 04 00 05 00 00 00 00 00 00 00 00 00 00 00 00".ToHexBytes());
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			SessionHandle = base.ByteTransform.TransUInt32(operateResult.Content, 4);
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(socket, "01 01 00 00 00 00 00 00 00 00 00 00 00 04 00 05 00 00 00 00 00 00 00 00 00 00 00 00".ToHexBytes());
			if (!read.IsSuccess)
			{
				return read;
			}
			SessionHandle = base.ByteTransform.TransUInt32(read.Content, 4);
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// Read data information, data length for read array length information
		/// </summary>
		/// <param name="address">Address format of the node</param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ExtraActualContent(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult(operateResult3.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteCommand(address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ExtraActualContent(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult(operateResult3.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			address = AnalysisBitIndex(address, out var bitIndex);
			OperateResult<byte[]> operateResult = Read(address, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content.ToBoolArray()[bitIndex]);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			OperateResult<byte[]> operateResult = BuildWriteCommand(address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ExtraActualContent(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult(operateResult3.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleySLCNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[]> command = BuildReadCommand(address, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult<byte[]> extra = ExtraActualContent(read.Content);
			if (!extra.IsSuccess)
			{
				return extra;
			}
			return OperateResult.CreateSuccessResult(extra.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> command = BuildWriteCommand(address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult<byte[]> extra = ExtraActualContent(read.Content);
			if (!extra.IsSuccess)
			{
				return extra;
			}
			return OperateResult.CreateSuccessResult(extra.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			address = AnalysisBitIndex(address, out var bitIndex);
			OperateResult<byte[]> read = await ReadAsync(address, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content.ToBoolArray()[bitIndex]);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			OperateResult<byte[]> command = BuildWriteCommand(address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult<byte[]> extra = ExtraActualContent(read.Content);
			if (!extra.IsSuccess)
			{
				return extra;
			}
			return OperateResult.CreateSuccessResult(extra.Content);
		}

		private byte[] PackCommand(byte[] coreCmd)
		{
			byte[] array = new byte[28 + coreCmd.Length];
			array[0] = 1;
			array[1] = 7;
			array[2] = (byte)(coreCmd.Length / 256);
			array[3] = (byte)(coreCmd.Length % 256);
			BitConverter.GetBytes(SessionHandle).CopyTo(array, 4);
			coreCmd.CopyTo(array, 28);
			return array;
		}

		/// <summary>
		/// 分析地址数据信息里的位索引的信息
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <param name="bitIndex">位索引</param>
		/// <returns>地址信息</returns>
		public static string AnalysisBitIndex(string address, out int bitIndex)
		{
			bitIndex = 0;
			int num = address.IndexOf('/');
			if (num < 0)
			{
				num = address.IndexOf('.');
			}
			if (num > 0)
			{
				bitIndex = int.Parse(address.Substring(num + 1));
				address = address.Substring(0, num);
			}
			return address;
		}

		/// <summary>
		/// 分析当前的地址信息，返回类型代号，区块号，起始地址<br />
		/// Analyze the current address information, return the type code, block number, and actual address
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>结果内容对象</returns>
		public static OperateResult<byte, ushort, ushort> AnalysisAddress(string address)
		{
			if (!address.Contains(":"))
			{
				return new OperateResult<byte, ushort, ushort>("Address can't find ':', example : A9:0");
			}
			string[] array = address.Split(':');
			try
			{
				OperateResult<byte, ushort, ushort> operateResult = new OperateResult<byte, ushort, ushort>();
				switch (array[0][0])
				{
				case 'A':
					operateResult.Content1 = 142;
					break;
				case 'B':
					operateResult.Content1 = 133;
					break;
				case 'N':
					operateResult.Content1 = 137;
					break;
				case 'F':
					operateResult.Content1 = 138;
					break;
				case 'S':
					if (array[0].Length > 1 && array[0][1] == 'T')
					{
						operateResult.Content1 = 141;
					}
					else
					{
						operateResult.Content1 = 132;
					}
					break;
				case 'C':
					operateResult.Content1 = 135;
					break;
				case 'I':
					operateResult.Content1 = 131;
					break;
				case 'O':
					operateResult.Content1 = 130;
					break;
				case 'R':
					operateResult.Content1 = 136;
					break;
				case 'T':
					operateResult.Content1 = 134;
					break;
				case 'L':
					operateResult.Content1 = 145;
					break;
				default:
					throw new Exception("Address code wrong, must be A,B,N,F,S,C,I,O,R,T,ST,L");
				}
				switch (operateResult.Content1)
				{
				case 132:
					operateResult.Content2 = (ushort)((array[0].Length == 1) ? 2 : ushort.Parse(array[0].Substring(1)));
					break;
				case 130:
					operateResult.Content2 = (ushort)((array[0].Length != 1) ? ushort.Parse(array[0].Substring(1)) : 0);
					break;
				case 131:
					operateResult.Content2 = (ushort)((array[0].Length == 1) ? 1 : ushort.Parse(array[0].Substring(1)));
					break;
				case 141:
					operateResult.Content2 = (ushort)((array[0].Length == 2) ? 1 : ushort.Parse(array[0].Substring(2)));
					break;
				default:
					operateResult.Content2 = ushort.Parse(array[0].Substring(1));
					break;
				}
				operateResult.Content3 = ushort.Parse(array[1]);
				operateResult.IsSuccess = true;
				operateResult.Message = StringResources.Language.SuccessText;
				return operateResult;
			}
			catch (Exception ex)
			{
				return new OperateResult<byte, ushort, ushort>("Wrong Address formate: " + ex.Message);
			}
		}

		/// <summary>
		/// 构建读取的指令信息
		/// </summary>
		/// <param name="address">地址信息，举例：A9:0</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>是否成功</returns>
		public static OperateResult<byte[]> BuildReadCommand(string address, ushort length)
		{
			OperateResult<byte, ushort, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (length < 2)
			{
				length = 2;
			}
			if (operateResult.Content1 == 142)
			{
				operateResult.Content3 /= 2;
			}
			byte[] array = new byte[14]
			{
				0,
				5,
				0,
				0,
				15,
				0,
				0,
				1,
				162,
				(byte)length,
				(byte)operateResult.Content2,
				operateResult.Content1,
				0,
				0
			};
			BitConverter.GetBytes(operateResult.Content3).CopyTo(array, 12);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 构建写入的报文内容，变成实际的数据
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteCommand(string address, byte[] value)
		{
			OperateResult<byte, ushort, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content1 == 142)
			{
				operateResult.Content3 /= 2;
			}
			byte[] array = new byte[18 + value.Length];
			array[0] = 0;
			array[1] = 5;
			array[2] = 0;
			array[3] = 0;
			array[4] = 15;
			array[5] = 0;
			array[6] = 0;
			array[7] = 1;
			array[8] = 171;
			array[9] = byte.MaxValue;
			array[10] = BitConverter.GetBytes(value.Length)[0];
			array[11] = BitConverter.GetBytes(value.Length)[1];
			array[12] = (byte)operateResult.Content2;
			array[13] = operateResult.Content1;
			BitConverter.GetBytes(operateResult.Content3).CopyTo(array, 14);
			array[16] = byte.MaxValue;
			array[17] = byte.MaxValue;
			value.CopyTo(array, 18);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 构建写入的报文内容，变成实际的数据
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteCommand(string address, bool value)
		{
			address = AnalysisBitIndex(address, out var bitIndex);
			OperateResult<byte, ushort, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content1 == 142)
			{
				operateResult.Content3 /= 2;
			}
			int value2 = 1 << bitIndex;
			byte[] array = new byte[20]
			{
				0,
				5,
				0,
				0,
				15,
				0,
				0,
				1,
				171,
				255,
				2,
				0,
				(byte)operateResult.Content2,
				operateResult.Content1,
				0,
				0,
				0,
				0,
				0,
				0
			};
			BitConverter.GetBytes(operateResult.Content3).CopyTo(array, 14);
			array[16] = BitConverter.GetBytes(value2)[0];
			array[17] = BitConverter.GetBytes(value2)[1];
			if (value)
			{
				array[18] = BitConverter.GetBytes(value2)[0];
				array[19] = BitConverter.GetBytes(value2)[1];
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 解析当前的实际报文内容，变成数据内容
		/// </summary>
		/// <param name="content">报文内容</param>
		/// <returns>是否成功</returns>
		public static OperateResult<byte[]> ExtraActualContent(byte[] content)
		{
			if (content.Length < 36)
			{
				return new OperateResult<byte[]>(StringResources.Language.ReceiveDataLengthTooShort + content.ToHexString(' '));
			}
			return OperateResult.CreateSuccessResult(content.RemoveBegin(36));
		}
	}
}

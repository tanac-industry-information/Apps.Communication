using System;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.AllenBradley
{
	/// <summary>
	/// 在CIP协议里，使用PCCC命令进行访问设备的原始数据文件的通信方法，
	/// </summary>
	public class AllenBradleyPcccNet : NetworkConnectedCip
	{
		private SoftIncrementCount incrementCount = new SoftIncrementCount(65535L, 2L, 2);

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public AllenBradleyPcccNet()
		{
			base.WordLength = 2;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 根据指定的IP及端口来实例化这个连接对象
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口号信息</param>
		public AllenBradleyPcccNet(string ipAddress, int port = 44818)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override byte[] GetLargeForwardOpen()
		{
			TOConnectionId = (uint)new Random().Next();
			byte[] array = "\r\n00 00 00 00 0a 00 02 00 00 00 00 00 b2 00 30 00\r\n54 02 20 06 24 01 0a 05 00 00 00 00 e8 a3 14 00\r\n27 04 09 10 0b 46 a5 c1 07 00 00 00 01 40 20 00\r\nf4 43 01 40 20 00 f4 43 a3 03 01 00 20 02 24 01".ToHexBytes();
			BitConverter.GetBytes((ushort)4105).CopyTo(array, 34);
			BitConverter.GetBytes(3248834059u).CopyTo(array, 36);
			BitConverter.GetBytes(TOConnectionId).CopyTo(array, 28);
			return array;
		}

		/// <inheritdoc />
		protected override byte[] GetLargeForwardClose()
		{
			return "\r\n00 00 00 00 0a 00 02 00 00 00 00 00 b2 00 18 00\r\n4e 02 20 06 24 01 0a 05 27 04 09 10 0b 46 a5 c1\r\n03 00 01 00 20 02 24 01".ToHexBytes();
		}

		/// <inheritdoc />
		/// <remarks>
		/// 读取PLC的原始数据信息，地址示例：N7:0
		/// </remarks>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = AllenBradleyHelper.PackExecutePCCCRead((int)incrementCount.GetCurrentValue(), address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommandService(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = AllenBradleyHelper.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			OperateResult<byte[], ushort, bool> operateResult4 = NetworkConnectedCip.ExtractActualData(operateResult2.Content, isRead: true);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(operateResult4.Content1);
		}

		/// <inheritdoc />
		/// <remarks>
		/// 写入PLC的原始数据信息，地址示例：N7:0
		/// </remarks>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = AllenBradleyHelper.PackExecutePCCCWrite((int)incrementCount.GetCurrentValue(), address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommandService(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = AllenBradleyHelper.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			OperateResult<byte[], ushort, bool> operateResult4 = NetworkConnectedCip.ExtractActualData(operateResult2.Content, isRead: true);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			address = AllenBradleySLCNet.AnalysisBitIndex(address, out var bitIndex);
			OperateResult<byte[]> operateResult = Read(address, (ushort)(bitIndex / 16 * 2 + 2));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content.ToBoolArray()[bitIndex]);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.Read(System.String,System.UInt16)" />
		public OperateResult<string> ReadString(string address)
		{
			return ReadString(address, 0, Encoding.ASCII);
		}

		/// <inheritdoc />
		/// <remarks>
		/// 读取PLC的地址信息，如果输入了 ST 的地址，例如 ST10:2, 当长度指定为 0 的时候，这时候就是动态的读取PLC来获取实际的字符串长度。<br />
		/// Read the PLC address information, if the ST address is entered, such as ST10:2, when the length is specified as 0, then the PLC is dynamically read to obtain the actual string length.
		/// </remarks>
		public override OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			if (!string.IsNullOrEmpty(address) && address.StartsWith("ST"))
			{
				if (length <= 0)
				{
					OperateResult<byte[]> operateResult = Read(address, 2);
					if (!operateResult.IsSuccess)
					{
						return OperateResult.CreateFailedResult<string>(operateResult);
					}
					int num = base.ByteTransform.TransUInt16(operateResult.Content, 0);
					operateResult = Read(address, (ushort)(2 + ((num % 2 != 0) ? (num + 1) : num)));
					if (!operateResult.IsSuccess)
					{
						return OperateResult.CreateFailedResult<string>(operateResult);
					}
					return OperateResult.CreateSuccessResult(encoding.GetString(SoftBasic.BytesReverseByWord(operateResult.Content), 2, num));
				}
				OperateResult<byte[]> operateResult2 = Read(address, (ushort)(((int)length % 2 != 0) ? (length + 3) : (length + 2)));
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(operateResult2);
				}
				int num2 = base.ByteTransform.TransUInt16(operateResult2.Content, 0);
				if (num2 + 2 > operateResult2.Content.Length)
				{
					num2 = operateResult2.Content.Length - 2;
				}
				return OperateResult.CreateSuccessResult(encoding.GetString(SoftBasic.BytesReverseByWord(operateResult2.Content), 2, num2));
			}
			return base.ReadString(address, length, encoding);
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, string value, Encoding encoding)
		{
			if (!string.IsNullOrEmpty(address) && address.StartsWith("ST"))
			{
				byte[] array = base.ByteTransform.TransByte(value, encoding);
				int value2 = array.Length;
				array = SoftBasic.ArrayExpandToLengthEven(array);
				return Write(address, SoftBasic.SpliceArray<byte>(new byte[2]
				{
					BitConverter.GetBytes(value2)[0],
					BitConverter.GetBytes(value2)[1]
				}, SoftBasic.BytesReverseByWord(array)));
			}
			return base.Write(address, value, encoding);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[]> command = AllenBradleyHelper.PackExecutePCCCRead((int)incrementCount.GetCurrentValue(), address, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommandService(command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			OperateResult<byte[], ushort, bool> extra = NetworkConnectedCip.ExtractActualData(read.Content, isRead: true);
			if (!extra.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(extra);
			}
			return OperateResult.CreateSuccessResult(extra.Content1);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> command = AllenBradleyHelper.PackExecutePCCCWrite((int)incrementCount.GetCurrentValue(), address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommandService(command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			OperateResult<byte[], ushort, bool> extra = NetworkConnectedCip.ExtractActualData(read.Content, isRead: true);
			if (!extra.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(extra);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.ReadBool(System.String)" />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			address = AllenBradleySLCNet.AnalysisBitIndex(address, out var bitIndex);
			OperateResult<byte[]> read = await ReadAsync(address, (ushort)(bitIndex / 16 * 2 + 2));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content.ToBoolArray()[bitIndex]);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.Read(System.String,System.UInt16)" />
		public async Task<OperateResult<string>> ReadStringAsync(string address)
		{
			return await ReadStringAsync(address, 0, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyPcccNet.ReadString(System.String,System.UInt16,System.Text.Encoding)" />
		public override async Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding)
		{
			if (!string.IsNullOrEmpty(address) && address.StartsWith("ST"))
			{
				if (length <= 0)
				{
					OperateResult<byte[]> read2 = await ReadAsync(address, 2);
					if (!read2.IsSuccess)
					{
						return OperateResult.CreateFailedResult<string>(read2);
					}
					int len2 = base.ByteTransform.TransUInt16(read2.Content, 0);
					read2 = await ReadAsync(address, (ushort)(2 + ((len2 % 2 != 0) ? (len2 + 1) : len2)));
					if (!read2.IsSuccess)
					{
						return OperateResult.CreateFailedResult<string>(read2);
					}
					return OperateResult.CreateSuccessResult(encoding.GetString(SoftBasic.BytesReverseByWord(read2.Content), 2, len2));
				}
				OperateResult<byte[]> read = await ReadAsync(address, (ushort)(((int)length % 2 != 0) ? (length + 3) : (length + 2)));
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(read);
				}
				int len = base.ByteTransform.TransUInt16(read.Content, 0);
				if (len + 2 > read.Content.Length)
				{
					len = read.Content.Length - 2;
				}
				return OperateResult.CreateSuccessResult(encoding.GetString(SoftBasic.BytesReverseByWord(read.Content), 2, len));
			}
			return base.ReadString(address, length, encoding);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, string value, Encoding encoding)
		{
			if (!string.IsNullOrEmpty(address) && address.StartsWith("ST"))
			{
				byte[] temp = base.ByteTransform.TransByte(value, encoding);
				int len = temp.Length;
				temp = SoftBasic.ArrayExpandToLengthEven(temp);
				return await WriteAsync(address, SoftBasic.SpliceArray<byte>(new byte[2]
				{
					BitConverter.GetBytes(len)[0],
					BitConverter.GetBytes(len)[1]
				}, SoftBasic.BytesReverseByWord(temp)));
			}
			return await base.WriteAsync(address, value, encoding);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"AllenBradleyPcccNet[{IpAddress}:{Port}]";
		}
	}
}

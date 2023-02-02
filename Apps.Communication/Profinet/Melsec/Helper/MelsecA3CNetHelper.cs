using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;

namespace Apps.Communication.Profinet.Melsec.Helper
{
	/// <summary>
	/// MelsecA3CNet1协议通信的辅助类
	/// </summary>
	public class MelsecA3CNetHelper
	{
		/// <summary>
		/// 将命令进行打包传送，可选站号及是否和校验机制
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="mcCommand">mc协议的命令</param>
		/// <param name="station">PLC的站号</param>
		/// <returns>最终的原始报文信息</returns>
		public static byte[] PackCommand(IReadWriteA3C plc, byte[] mcCommand, byte station = 0)
		{
			MemoryStream memoryStream = new MemoryStream();
			if (plc.Format != 3)
			{
				memoryStream.WriteByte(5);
			}
			else
			{
				memoryStream.WriteByte(2);
			}
			if (plc.Format == 2)
			{
				memoryStream.WriteByte(48);
				memoryStream.WriteByte(48);
			}
			memoryStream.WriteByte(70);
			memoryStream.WriteByte(57);
			memoryStream.WriteByte(SoftBasic.BuildAsciiBytesFrom(station)[0]);
			memoryStream.WriteByte(SoftBasic.BuildAsciiBytesFrom(station)[1]);
			memoryStream.WriteByte(48);
			memoryStream.WriteByte(48);
			memoryStream.WriteByte(70);
			memoryStream.WriteByte(70);
			memoryStream.WriteByte(48);
			memoryStream.WriteByte(48);
			memoryStream.Write(mcCommand, 0, mcCommand.Length);
			if (plc.Format == 3)
			{
				memoryStream.WriteByte(3);
			}
			if (plc.SumCheck)
			{
				byte[] array = memoryStream.ToArray();
				int num = 0;
				for (int i = 1; i < array.Length; i++)
				{
					num += array[i];
				}
				memoryStream.WriteByte(SoftBasic.BuildAsciiBytesFrom((byte)num)[0]);
				memoryStream.WriteByte(SoftBasic.BuildAsciiBytesFrom((byte)num)[1]);
			}
			if (plc.Format == 4)
			{
				memoryStream.WriteByte(13);
				memoryStream.WriteByte(10);
			}
			byte[] result = memoryStream.ToArray();
			memoryStream.Dispose();
			return result;
		}

		private static int GetErrorCodeOrDataStartIndex(IReadWriteA3C plc)
		{
			int result = 11;
			switch (plc.Format)
			{
			case 1:
				result = 11;
				break;
			case 2:
				result = 13;
				break;
			case 3:
				result = 15;
				break;
			case 4:
				result = 11;
				break;
			}
			return result;
		}

		/// <summary>
		/// 根据PLC返回的数据信息，获取到实际的数据内容
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="response">PLC返回的数据信息</param>
		/// <returns>带有是否成功的读取结果对象内容</returns>
		public static OperateResult<byte[]> ExtraReadActualResponse(IReadWriteA3C plc, byte[] response)
		{
			try
			{
				int errorCodeOrDataStartIndex = GetErrorCodeOrDataStartIndex(plc);
				if (plc.Format == 1 || plc.Format == 2 || plc.Format == 4)
				{
					if (response[0] == 21)
					{
						int num = Convert.ToInt32(Encoding.ASCII.GetString(response, errorCodeOrDataStartIndex, 4), 16);
						return new OperateResult<byte[]>(num, MelsecHelper.GetErrorDescription(num));
					}
					if (response[0] != 2)
					{
						return new OperateResult<byte[]>(response[0], "Read Faild:" + SoftBasic.GetAsciiStringRender(response));
					}
				}
				else if (plc.Format == 3)
				{
					string @string = Encoding.ASCII.GetString(response, 11, 4);
					if (@string == "QNAK")
					{
						int num2 = Convert.ToInt32(Encoding.ASCII.GetString(response, errorCodeOrDataStartIndex, 4), 16);
						return new OperateResult<byte[]>(num2, MelsecHelper.GetErrorDescription(num2));
					}
					if (@string != "QACK")
					{
						return new OperateResult<byte[]>(response[0], "Read Faild:" + SoftBasic.GetAsciiStringRender(response));
					}
				}
				int num3 = -1;
				for (int i = errorCodeOrDataStartIndex; i < response.Length; i++)
				{
					if (response[i] == 3)
					{
						num3 = i;
						break;
					}
				}
				if (num3 == -1)
				{
					num3 = response.Length;
				}
				return OperateResult.CreateSuccessResult(response.SelectMiddle(errorCodeOrDataStartIndex, num3 - errorCodeOrDataStartIndex));
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("ExtraReadActualResponse Wrong:" + ex.Message + Environment.NewLine + "Source: " + response.ToHexString(' '));
			}
		}

		private static OperateResult CheckWriteResponse(IReadWriteA3C plc, byte[] response)
		{
			int errorCodeOrDataStartIndex = GetErrorCodeOrDataStartIndex(plc);
			if (plc.Format == 1 || plc.Format == 2)
			{
				if (response[0] == 21)
				{
					int num = Convert.ToInt32(Encoding.ASCII.GetString(response, errorCodeOrDataStartIndex, 4), 16);
					return new OperateResult<byte[]>(num, MelsecHelper.GetErrorDescription(num));
				}
				if (response[0] != 6)
				{
					return new OperateResult<byte[]>(response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender(response));
				}
			}
			else if (plc.Format == 3)
			{
				if (response[0] != 2)
				{
					return new OperateResult<byte[]>(response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender(response));
				}
				string @string = Encoding.ASCII.GetString(response, 11, 4);
				if (@string == "QNAK")
				{
					int num2 = Convert.ToInt32(Encoding.ASCII.GetString(response, errorCodeOrDataStartIndex, 4), 16);
					return new OperateResult<byte[]>(num2, MelsecHelper.GetErrorDescription(num2));
				}
				if (@string != "QACK")
				{
					return new OperateResult<byte[]>(response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender(response));
				}
			}
			else if (plc.Format == 4)
			{
				if (response[0] == 21)
				{
					int num3 = Convert.ToInt32(Encoding.ASCII.GetString(response, errorCodeOrDataStartIndex, 4), 16);
					return new OperateResult<byte[]>(num3, MelsecHelper.GetErrorDescription(num3));
				}
				if (response[0] != 6)
				{
					return new OperateResult<byte[]>(response[0], "Write Faild:" + SoftBasic.GetAsciiStringRender(response));
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 批量读取PLC的数据，以字为单位，支持读取X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <returns>读取结果信息</returns>
		public static OperateResult<byte[]> Read(IReadWriteA3C plc, string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			List<byte> list = new List<byte>();
			ushort num = 0;
			while (num < length)
			{
				ushort num2 = (ushort)Math.Min(length - num, McHelper.GetReadWordLength(McType.MCAscii));
				operateResult.Content.Length = num2;
				byte[] mcCommand = McAsciiHelper.BuildAsciiReadMcCoreCommand(operateResult.Content, isBit: false);
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(PackCommand(plc, mcCommand, station));
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				OperateResult<byte[]> operateResult3 = ExtraReadActualResponse(plc, operateResult2.Content);
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				list.AddRange(MelsecHelper.TransAsciiByteArrayToByteArray(operateResult3.Content));
				num = (ushort)(num + num2);
				if (operateResult.Content.McDataType.DataType == 0)
				{
					operateResult.Content.AddressStart += num2;
				}
				else
				{
					operateResult.Content.AddressStart += num2 * 16;
				}
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.Read(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.UInt16)" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IReadWriteA3C plc, string address, ushort length)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom(address, length);
			if (!addressResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(addressResult);
			}
			List<byte> bytesContent = new List<byte>();
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort readLength = (ushort)Math.Min(length - alreadyFinished, McHelper.GetReadWordLength(McType.MCAscii));
				addressResult.Content.Length = readLength;
				byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand(addressResult.Content, isBit: false);
				OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, command, stat));
				if (!read.IsSuccess)
				{
					return read;
				}
				OperateResult<byte[]> check = ExtraReadActualResponse(plc, read.Content);
				if (!check.IsSuccess)
				{
					return check;
				}
				bytesContent.AddRange(MelsecHelper.TransAsciiByteArrayToByteArray(check.Content));
				alreadyFinished = (ushort)(alreadyFinished + readLength);
				if (addressResult.Content.McDataType.DataType == 0)
				{
					addressResult.Content.AddressStart += readLength;
				}
				else
				{
					addressResult.Content.AddressStart += readLength * 16;
				}
			}
			return OperateResult.CreateSuccessResult(bytesContent.ToArray());
		}

		/// <summary>
		/// 批量写入PLC的数据，以字为单位，也就是说最少2个字节信息，支持X,Y,M,S,D,T,C，具体的地址范围需要根据PLC型号来确认
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write(IReadWriteA3C plc, string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] mcCommand = McAsciiHelper.BuildAsciiWriteWordCoreCommand(operateResult.Content, value);
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(PackCommand(plc, mcCommand, station));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return CheckWriteResponse(plc, operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.Write(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.Byte[])" />
		public static async Task<OperateResult> WriteAsync(IReadWriteA3C plc, string address, byte[] value)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!addressResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(addressResult);
			}
			byte[] command = McAsciiHelper.BuildAsciiWriteWordCoreCommand(addressResult.Content, value);
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, command, stat));
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckWriteResponse(plc, read.Content);
		}

		/// <summary>
		/// 批量读取bool类型数据，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">地址信息，比如X10,Y17，注意X，Y的地址是8进制的</param>
		/// <param name="length">读取的长度</param>
		/// <returns>读取结果信息</returns>
		public static OperateResult<bool[]> ReadBool(IReadWriteA3C plc, string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			List<bool> list = new List<bool>();
			ushort num = 0;
			while (num < length)
			{
				ushort num2 = (ushort)Math.Min(length - num, McHelper.GetReadBoolLength(McType.MCAscii));
				operateResult.Content.Length = num2;
				byte[] mcCommand = McAsciiHelper.BuildAsciiReadMcCoreCommand(operateResult.Content, isBit: true);
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(PackCommand(plc, mcCommand, station));
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult2);
				}
				OperateResult<byte[]> operateResult3 = ExtraReadActualResponse(plc, operateResult2.Content);
				if (!operateResult3.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult3);
				}
				list.AddRange(operateResult3.Content.Select((byte m) => m == 49).ToArray());
				num = (ushort)(num + num2);
				operateResult.Content.AddressStart += num2;
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.ReadBool(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.UInt16)" />
		public static async Task<OperateResult<bool[]>> ReadBoolAsync(IReadWriteA3C plc, string address, ushort length)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom(address, length);
			if (!addressResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(addressResult);
			}
			List<bool> boolContent = new List<bool>();
			ushort alreadyFinished = 0;
			while (alreadyFinished < length)
			{
				ushort readLength = (ushort)Math.Min(length - alreadyFinished, McHelper.GetReadBoolLength(McType.MCAscii));
				addressResult.Content.Length = readLength;
				byte[] command = McAsciiHelper.BuildAsciiReadMcCoreCommand(addressResult.Content, isBit: true);
				OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, command, stat));
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(read);
				}
				OperateResult<byte[]> check = ExtraReadActualResponse(plc, read.Content);
				if (!check.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(check);
				}
				boolContent.AddRange(check.Content.Select((byte m) => m == 49).ToArray());
				alreadyFinished = (ushort)(alreadyFinished + readLength);
				addressResult.Content.AddressStart += readLength;
			}
			return OperateResult.CreateSuccessResult(boolContent.ToArray());
		}

		/// <summary>
		/// 批量写入bool类型的数组，支持的类型为X,Y,S,T,C，具体的地址范围取决于PLC的类型
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="value">数据信息</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write(IReadWriteA3C plc, string address, bool[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			byte[] mcCommand = McAsciiHelper.BuildAsciiWriteBitCoreCommand(operateResult.Content, value);
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(PackCommand(plc, mcCommand, station));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return CheckWriteResponse(plc, operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.Write(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.Boolean[])" />
		public static async Task<OperateResult> WriteAsync(IReadWriteA3C plc, string address, bool[] value)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", plc.Station);
			OperateResult<McAddressData> addressResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!addressResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(addressResult);
			}
			byte[] command = McAsciiHelper.BuildAsciiWriteBitCoreCommand(addressResult.Content, value);
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, command, stat));
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckWriteResponse(plc, read.Content);
		}

		/// <summary>
		/// 远程Run操作
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteRun(IReadWriteA3C plc)
		{
			OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(PackCommand(plc, Encoding.ASCII.GetBytes("1001000000010000"), plc.Station));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return CheckWriteResponse(plc, operateResult.Content);
		}

		/// <summary>
		/// 远程Stop操作
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <returns>是否成功</returns>
		public static OperateResult RemoteStop(IReadWriteA3C plc)
		{
			OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(PackCommand(plc, Encoding.ASCII.GetBytes("100200000001"), plc.Station));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return CheckWriteResponse(plc, operateResult.Content);
		}

		/// <summary>
		/// 读取PLC的型号信息
		/// </summary>
		/// <param name="plc">PLC设备通信对象</param>
		/// <returns>返回型号的结果对象</returns>
		public static OperateResult<string> ReadPlcType(IReadWriteA3C plc)
		{
			OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(PackCommand(plc, Encoding.ASCII.GetBytes("01010000"), plc.Station));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ExtraReadActualResponse(plc, operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(operateResult2.Content, 0, 16).TrimEnd());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.RemoteRun(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		public static async Task<OperateResult> RemoteRunAsync(IReadWriteA3C plc)
		{
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, Encoding.ASCII.GetBytes("1001000000010000"), plc.Station));
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckWriteResponse(plc, read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.RemoteStop(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		public static async Task<OperateResult> RemoteStopAsync(IReadWriteA3C plc)
		{
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, Encoding.ASCII.GetBytes("100200000001"), plc.Station));
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckWriteResponse(plc, read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.ReadPlcType(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		public static async Task<OperateResult<string>> ReadPlcTypeAsync(IReadWriteA3C plc)
		{
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(PackCommand(plc, Encoding.ASCII.GetBytes("01010000"), plc.Station));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			OperateResult<byte[]> check = ExtraReadActualResponse(plc, read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(check);
			}
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(check.Content, 0, 16).TrimEnd());
		}
	}
}

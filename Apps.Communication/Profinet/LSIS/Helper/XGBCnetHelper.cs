using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;

namespace Apps.Communication.Profinet.LSIS.Helper
{
	/// <summary>
	/// Cnet的辅助类
	/// </summary>
	public class XGBCnetHelper
	{
		/// <summary>
		/// 根据错误号，获取到真实的错误描述信息<br />
		/// According to the error number, get the real error description information
		/// </summary>
		/// <param name="err">错误号</param>
		/// <returns>真实的错误描述信息</returns>
		public static string GetErrorText(int err)
		{
			switch (err)
			{
			case 3:
				return StringResources.Language.LsisCnet0003;
			case 4:
				return StringResources.Language.LsisCnet0004;
			case 7:
				return StringResources.Language.LsisCnet0007;
			case 17:
				return StringResources.Language.LsisCnet0011;
			case 144:
				return StringResources.Language.LsisCnet0090;
			case 400:
				return StringResources.Language.LsisCnet0190;
			case 656:
				return StringResources.Language.LsisCnet0290;
			case 4402:
				return StringResources.Language.LsisCnet1132;
			case 4658:
				return StringResources.Language.LsisCnet1232;
			case 4660:
				return StringResources.Language.LsisCnet1234;
			case 4914:
				return StringResources.Language.LsisCnet1332;
			case 5170:
				return StringResources.Language.LsisCnet1432;
			case 28978:
				return StringResources.Language.LsisCnet7132;
			default:
				return StringResources.Language.UnknownError;
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.UnpackResponseContent(System.Byte[],System.Byte[])" />
		public static OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			try
			{
				if (response[0] == 6)
				{
					if (response[3] == 87 || response[3] == 119)
					{
						return OperateResult.CreateSuccessResult(response);
					}
					string @string = Encoding.ASCII.GetString(response, 4, 2);
					if (@string == "SS")
					{
						int num = Convert.ToInt32(Encoding.ASCII.GetString(response, 6, 2), 16);
						int num2 = 8;
						List<byte> list = new List<byte>();
						for (int i = 0; i < num; i++)
						{
							int num3 = Convert.ToInt32(Encoding.ASCII.GetString(response, num2, 2), 16);
							list.AddRange(Encoding.ASCII.GetString(response, num2 + 2, num3 * 2).ToHexBytes());
							num2 += 2 + num3 * 2;
						}
						return OperateResult.CreateSuccessResult(list.ToArray());
					}
					if (@string == "SB")
					{
						int num4 = Convert.ToInt32(Encoding.ASCII.GetString(response, 8, 2), 16);
						byte[] value = Encoding.ASCII.GetString(response, 10, num4 * 2).ToHexBytes();
						return OperateResult.CreateSuccessResult(value);
					}
					return new OperateResult<byte[]>(1, "Command Wrong:" + @string + Environment.NewLine + "Source: " + response.ToHexString());
				}
				if (response[0] == 21)
				{
					int err = Convert.ToInt32(Encoding.ASCII.GetString(response, 6, 4), 16);
					return new OperateResult<byte[]>(err, GetErrorText(err));
				}
				return new OperateResult<byte[]>(response[0], "Source: " + SoftBasic.GetAsciiStringRender(response));
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(1, "Wrong:" + ex.Message + Environment.NewLine + "Source: " + response.ToHexString());
			}
		}

		/// <summary>
		/// AnalysisAddress IX0.0.0 QX0.0.0  MW1.0  MB1.0
		/// </summary>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="QI">是否输入输出的情况</param>
		/// <returns>实际的偏移地址</returns>
		public static int CalculateAddressStarted(string address, bool QI = false)
		{
			if (address.IndexOf('.') < 0)
			{
				return Convert.ToInt32(address);
			}
			string[] array = address.Split('.');
			if (!QI)
			{
				return Convert.ToInt32(array[0]);
			}
			return Convert.ToInt32(array[2]);
		}

		/// <summary>
		/// NumberStyles HexNumber
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static bool IsHex(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			bool result = false;
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
					result = true;
					break;
				}
			}
			return result;
		}

		/// <summary>
		/// AnalysisAddress
		/// </summary>
		/// <param name="address">start address</param>
		/// <returns>analysis result</returns>
		public static OperateResult<string> AnalysisAddress(string address)
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				stringBuilder.Append("%");
				char[] array = new char[15]
				{
					'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q',
					'I', 'N', 'U', 'Z', 'R'
				};
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] != address[0])
					{
						continue;
					}
					stringBuilder.Append(array[i]);
					char c = address[1];
					char c2 = c;
					if (c2 == 'X')
					{
						stringBuilder.Append("X");
						if (address[0] == 'I' || address[0] == 'Q')
						{
							stringBuilder.Append(CalculateAddressStarted(address.Substring(2), QI: true));
						}
						else if (IsHex(address.Substring(2)))
						{
							stringBuilder.Append(address.Substring(2));
						}
						else
						{
							stringBuilder.Append(CalculateAddressStarted(address.Substring(2)));
						}
					}
					else
					{
						stringBuilder.Append("B");
						int num = 0;
						if (address[1] == 'B')
						{
							num = CalculateAddressStarted(address.Substring(2));
							stringBuilder.Append(num);
						}
						else if (address[1] == 'W')
						{
							num = CalculateAddressStarted(address.Substring(2));
							stringBuilder.Append(num *= 2);
						}
						else if (address[1] == 'D')
						{
							num = CalculateAddressStarted(address.Substring(2));
							stringBuilder.Append(num *= 4);
						}
						else if (address[1] == 'L')
						{
							num = CalculateAddressStarted(address.Substring(2));
							stringBuilder.Append(num *= 8);
						}
						else if (address[0] == 'I' || address[0] == 'Q')
						{
							stringBuilder.Append(CalculateAddressStarted(address.Substring(1), QI: true));
						}
						else if (IsHex(address.Substring(1)))
						{
							stringBuilder.Append(address.Substring(1));
						}
						else
						{
							stringBuilder.Append(CalculateAddressStarted(address.Substring(1)));
						}
					}
					flag = true;
					break;
				}
				if (!flag)
				{
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(stringBuilder.ToString());
		}

		/// <summary>
		/// reading address  Type of ReadByte
		/// </summary>
		/// <param name="station">plc station</param>
		/// <param name="address">address, for example: M100, D100, DW100</param>
		/// <param name="length">read length</param>
		/// <returns>command bytes</returns>
		public static OperateResult<byte[]> BuildReadByteCommand(byte station, string address, ushort length)
		{
			OperateResult<string> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			List<byte> list = new List<byte>();
			list.Add(5);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
			list.Add(114);
			list.Add(83);
			list.Add(66);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)operateResult.Content.Length));
			list.AddRange(Encoding.ASCII.GetBytes(operateResult.Content));
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)length));
			list.Add(4);
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				num += list[i];
			}
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.BuildReadIndividualCommand(System.Byte,System.String[])" />
		public static OperateResult<byte[]> BuildReadIndividualCommand(byte station, string address)
		{
			return BuildReadIndividualCommand(station, new string[1] { address });
		}

		/// <summary>
		/// Multi reading address Type of Read Individual
		/// </summary>
		/// <param name="station">plc station</param>
		/// <param name="addresses">address, for example: MX100, PX100</param>
		/// <returns></returns>
		public static OperateResult<byte[]> BuildReadIndividualCommand(byte station, string[] addresses)
		{
			List<byte> list = new List<byte>();
			list.Add(5);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
			list.Add(114);
			list.Add(83);
			list.Add(83);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)addresses.Length));
			if (addresses.Length > 1)
			{
				foreach (string text in addresses)
				{
					string text2 = (text.StartsWith("%") ? text : ("%" + text));
					list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)text2.Length));
					list.AddRange(Encoding.ASCII.GetBytes(text2));
				}
			}
			else
			{
				foreach (string address in addresses)
				{
					OperateResult<string> operateResult = AnalysisAddress(address);
					if (!operateResult.IsSuccess)
					{
						return OperateResult.CreateFailedResult<byte[]>(operateResult);
					}
					list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)operateResult.Content.Length));
					list.AddRange(Encoding.ASCII.GetBytes(operateResult.Content));
				}
			}
			list.Add(4);
			int num = 0;
			for (int k = 0; k < list.Count; k++)
			{
				num += list[k];
			}
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// build read command. 
		/// </summary>
		/// <param name="station">station</param>
		/// <param name="address">start address</param>
		/// <param name="length">address length</param>
		/// <returns> command</returns>
		public static OperateResult<byte[]> BuildReadCommand(byte station, string address, ushort length)
		{
			OperateResult<string> dataTypeToAddress = XGBFastEnet.GetDataTypeToAddress(address);
			if (!dataTypeToAddress.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress);
			}
			switch (dataTypeToAddress.Content)
			{
			case "Bit":
				return BuildReadIndividualCommand(station, address);
			case "Word":
			case "DWord":
			case "LWord":
			case "Continuous":
				return BuildReadByteCommand(station, address, length);
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <summary>
		/// write data to address  Type of ReadByte
		/// </summary>
		/// <param name="station">plc station</param>
		/// <param name="address">address, for example: M100, D100, DW100</param>
		/// <param name="value">source value</param>
		/// <returns>command bytes</returns>
		public static OperateResult<byte[]> BuildWriteByteCommand(byte station, string address, byte[] value)
		{
			OperateResult<string> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			List<byte> list = new List<byte>();
			list.Add(5);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
			list.Add(119);
			list.Add(83);
			list.Add(66);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)operateResult.Content.Length));
			list.AddRange(Encoding.ASCII.GetBytes(operateResult.Content));
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)value.Length));
			list.AddRange(SoftBasic.BytesToAsciiBytes(value));
			list.Add(4);
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				num += list[i];
			}
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// write data to address  Type of One
		/// </summary>
		/// <param name="station">plc station</param>
		/// <param name="address">address, for example: M100, D100, DW100</param>
		/// <param name="value">source value</param>
		/// <returns>command bytes</returns>
		public static OperateResult<byte[]> BuildWriteOneCommand(byte station, string address, byte[] value)
		{
			OperateResult<string> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			List<byte> list = new List<byte>();
			list.Add(5);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
			list.Add(119);
			list.Add(83);
			list.Add(83);
			list.Add(48);
			list.Add(49);
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)operateResult.Content.Length));
			list.AddRange(Encoding.ASCII.GetBytes(operateResult.Content));
			list.AddRange(SoftBasic.BytesToAsciiBytes(value));
			list.Add(4);
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				num += list[i];
			}
			list.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// write data to address  Type of ReadByte
		/// </summary>
		/// <param name="station">plc station</param>
		/// <param name="address">address, for example: M100, D100, DW100</param>
		/// <param name="value">source value</param>
		/// <returns>command bytes</returns>
		public static OperateResult<byte[]> BuildWriteCommand(byte station, string address, byte[] value)
		{
			OperateResult<string> dataTypeToAddress = XGBFastEnet.GetDataTypeToAddress(address);
			if (!dataTypeToAddress.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress);
			}
			switch (dataTypeToAddress.Content)
			{
			case "Bit":
				return BuildWriteOneCommand(station, address, value);
			case "Word":
			case "DWord":
			case "LWord":
			case "Continuous":
				return BuildWriteByteCommand(station, address, value);
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <summary>
		/// 从PLC的指定地址读取原始的字节数据信息，地址示例：MB100, MW100, MD100, 如果输入了M100等同于MB100<br />
		/// Read the original byte data information from the designated address of the PLC. 
		/// Examples of addresses: MB100, MW100, MD100, if the input M100 is equivalent to MB100
		/// </summary>
		/// <remarks>
		/// 地址类型支持 P,M,L,K,F,T,C,D,R,I,Q,W, 支持携带站号的形式，例如 s=2;MW100
		/// </remarks>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="station">站号信息</param>
		/// <param name="address">PLC的地址信息，例如 M100, MB100, MW100, MD100</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>返回是否读取成功的结果对象</returns>
		public static OperateResult<byte[]> Read(IReadWriteDevice plc, int station, string address, ushort length)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildReadCommand(station2, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return plc.ReadFromCoreServer(operateResult.Content);
		}

		/// <summary>
		/// 从PLC设备读取多个地址的数据信息，返回连续的字节数组，需要按照实际情况进行按顺序解析。<br />
		/// Read the data information of multiple addresses from the PLC device and return a continuous byte array, which needs to be parsed in order according to the actual situation.
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="station">站号信息</param>
		/// <param name="address">PLC的地址信息，例如 M100, MB100, MW100, MD100</param>
		/// <returns>结果对象数据</returns>
		public static OperateResult<byte[]> Read(IReadWriteDevice plc, int station, string[] address)
		{
			List<string[]> list = SoftBasic.ArraySplitByLength(address, 16);
			List<byte> list2 = new List<byte>(32);
			for (int i = 0; i < list.Count; i++)
			{
				OperateResult<byte[]> operateResult = BuildReadIndividualCommand((byte)station, list[i]);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				list2.AddRange(operateResult2.Content);
			}
			return OperateResult.CreateSuccessResult(list2.ToArray());
		}

		/// <summary>
		/// 将原始数据写入到PLC的指定的地址里，地址示例：MB100, MW100, MD100, 如果输入了M100等同于MB100<br />
		/// Write the original data to the designated address of the PLC. 
		/// Examples of addresses: MB100, MW100, MD100, if input M100 is equivalent to MB100
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="station">站号信息</param>
		/// <param name="address">PLC的地址信息，例如 M100, MB100, MW100, MD100</param>
		/// <param name="value">等待写入的原始数据内容</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult Write(IReadWriteDevice plc, int station, string address, byte[] value)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildWriteCommand(station2, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return plc.ReadFromCoreServer(operateResult.Content);
		}

		/// <summary>
		/// 从PLC的指定地址读取原始的位数据信息，地址示例：MX100, MX10A<br />
		/// Read the original bool data information from the designated address of the PLC. 
		/// Examples of addresses: MX100, MX10A
		/// </summary>
		/// <remarks>
		/// 地址类型支持 P,M,L,K,F,T,C,D,R,I,Q,W, 支持携带站号的形式，例如 s=2;MX100
		/// </remarks>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="station">站号信息</param>
		/// <param name="address">PLC的地址信息，例如 MX100, MX10A</param>
		/// <returns>返回是否读取成功的结果对象</returns>
		public static OperateResult<bool> ReadBool(IReadWriteDevice plc, int station, string address)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildReadIndividualCommand(station2, address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult2.Content, 1)[0]);
		}

		/// <summary>
		/// 将bool数据写入到PLC的指定的地址里，地址示例：MX100, MX10A<br />
		/// Write the bool data to the designated address of the PLC. Examples of addresses: MX100, MX10A
		/// </summary>
		/// <remarks>
		/// 地址类型支持 P,M,L,K,F,T,C,D,R,I,Q,W, 支持携带站号的形式，例如 s=2;MX100
		/// </remarks>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="station">站号信息</param>
		/// <param name="address">PLC的地址信息，例如 MX100, MX10A</param>
		/// <param name="value">bool值信息</param>
		/// <returns>返回是否读取成功的结果对象</returns>
		public static OperateResult Write(IReadWriteDevice plc, int station, string address, bool value)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildWriteOneCommand(station2, address, new byte[1] { (byte)(value ? 1u : 0u) });
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return plc.ReadFromCoreServer(operateResult.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Read(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.UInt16)" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IReadWriteDevice plc, int station, string address, ushort length)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> command = BuildReadCommand(stat, address, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await plc.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Read(Communication.Core.IReadWriteDevice,System.Int32,System.String[])" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IReadWriteDevice plc, int station, string[] address)
		{
			List<string[]> list = SoftBasic.ArraySplitByLength(address, 16);
			List<byte> result = new List<byte>(32);
			for (int i = 0; i < list.Count; i++)
			{
				OperateResult<byte[]> command = BuildReadIndividualCommand((byte)station, list[i]);
				if (!command.IsSuccess)
				{
					return command;
				}
				OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content);
				if (!read.IsSuccess)
				{
					return read;
				}
				result.AddRange(read.Content);
			}
			return OperateResult.CreateSuccessResult(result.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Write(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.Byte[])" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice plc, int station, string address, byte[] value)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> command = BuildWriteCommand(stat, address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await plc.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.ReadBool(Communication.Core.IReadWriteDevice,System.Int32,System.String)" />
		public static async Task<OperateResult<bool>> ReadBoolAsync(IReadWriteDevice plc, int station, string address)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> command = BuildReadIndividualCommand(stat, address);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(command);
			}
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(read.Content, 1)[0]);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Write(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.Boolean)" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice plc, int station, string address, bool value)
		{
			byte stat = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> command = BuildWriteOneCommand(stat, address, new byte[1] { (byte)(value ? 1u : 0u) });
			if (!command.IsSuccess)
			{
				return command;
			}
			return await plc.ReadFromCoreServerAsync(command.Content);
		}
	}
}

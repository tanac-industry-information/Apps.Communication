using System;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;

namespace Apps.Communication.Profinet.Siemens.Helper
{
	/// <summary>
	/// 西门子PPI协议的辅助类对象
	/// </summary>
	public class SiemensPPIHelper
	{
		/// <summary>
		/// 解析数据地址，解析出地址类型，起始地址，DB块的地址<br />
		/// Parse data address, parse out address type, start address, db block address
		/// </summary>
		/// <param name="address">起始地址，例如M100，I0，Q0，V100 -&gt;
		/// Start address, such as M100,I0,Q0,V100</param>
		/// <returns>解析数据地址，解析出地址类型，起始地址，DB块的地址 -&gt;
		/// Parse data address, parse out address type, start address, db block address</returns>
		public static OperateResult<byte, int, ushort> AnalysisAddress(string address)
		{
			OperateResult<byte, int, ushort> operateResult = new OperateResult<byte, int, ushort>();
			try
			{
				operateResult.Content3 = 0;
				if (address.Substring(0, 2) == "AI")
				{
					operateResult.Content1 = 6;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(2));
				}
				else if (address.Substring(0, 2) == "AQ")
				{
					operateResult.Content1 = 7;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(2));
				}
				else if (address[0] == 'T')
				{
					operateResult.Content1 = 31;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'C')
				{
					operateResult.Content1 = 30;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address.Substring(0, 2) == "SM")
				{
					operateResult.Content1 = 5;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(2));
				}
				else if (address[0] == 'S')
				{
					operateResult.Content1 = 4;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'I')
				{
					operateResult.Content1 = 129;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'Q')
				{
					operateResult.Content1 = 130;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'M')
				{
					operateResult.Content1 = 131;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'D' || address.Substring(0, 2) == "DB")
				{
					operateResult.Content1 = 132;
					string[] array = address.Split('.');
					if (address[1] == 'B')
					{
						operateResult.Content3 = Convert.ToUInt16(array[0].Substring(2));
					}
					else
					{
						operateResult.Content3 = Convert.ToUInt16(array[0].Substring(1));
					}
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(address.IndexOf('.') + 1));
				}
				else
				{
					if (address[0] != 'V')
					{
						operateResult.Message = StringResources.Language.NotSupportedDataType;
						operateResult.Content1 = 0;
						operateResult.Content2 = 0;
						operateResult.Content3 = 0;
						return operateResult;
					}
					operateResult.Content1 = 132;
					operateResult.Content3 = 1;
					operateResult.Content2 = S7AddressData.CalculateAddressStarted(address.Substring(1));
				}
			}
			catch (Exception ex)
			{
				operateResult.Message = ex.Message;
				return operateResult;
			}
			operateResult.IsSuccess = true;
			return operateResult;
		}

		/// <summary>
		/// 生成一个读取字数据指令头的通用方法<br />
		/// A general method for generating a command header to read a Word data
		/// </summary>
		/// <param name="station">设备的站号信息 -&gt; Station number information for the device</param>
		/// <param name="address">起始地址，例如M100，I0，Q0，V100 -&gt;
		/// Start address, such as M100,I0,Q0,V100</param>
		/// <param name="length">读取数据长度 -&gt; Read Data length</param>
		/// <param name="isBit">是否为位读取</param>
		/// <returns>包含结果对象的报文 -&gt; Message containing the result object</returns>
		public static OperateResult<byte[]> BuildReadCommand(byte station, string address, ushort length, bool isBit)
		{
			OperateResult<byte, int, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[33];
			array[0] = 104;
			array[1] = BitConverter.GetBytes(array.Length - 6)[0];
			array[2] = BitConverter.GetBytes(array.Length - 6)[0];
			array[3] = 104;
			array[4] = station;
			array[5] = 0;
			array[6] = 108;
			array[7] = 50;
			array[8] = 1;
			array[9] = 0;
			array[10] = 0;
			array[11] = 0;
			array[12] = 0;
			array[13] = 0;
			array[14] = 14;
			array[15] = 0;
			array[16] = 0;
			array[17] = 4;
			array[18] = 1;
			array[19] = 18;
			array[20] = 10;
			array[21] = 16;
			array[22] = (byte)(isBit ? 1 : 2);
			array[23] = 0;
			array[24] = BitConverter.GetBytes(length)[0];
			array[25] = BitConverter.GetBytes(length)[1];
			array[26] = (byte)operateResult.Content3;
			array[27] = operateResult.Content1;
			array[28] = BitConverter.GetBytes(operateResult.Content2)[2];
			array[29] = BitConverter.GetBytes(operateResult.Content2)[1];
			array[30] = BitConverter.GetBytes(operateResult.Content2)[0];
			int num = 0;
			for (int i = 4; i < 31; i++)
			{
				num += array[i];
			}
			array[31] = BitConverter.GetBytes(num)[0];
			array[32] = 22;
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 生成一个写入PLC数据信息的报文内容
		/// </summary>
		/// <param name="station">PLC的站号</param>
		/// <param name="address">地址</param>
		/// <param name="values">数据值</param>
		/// <returns>是否写入成功</returns>
		public static OperateResult<byte[]> BuildWriteCommand(byte station, string address, byte[] values)
		{
			OperateResult<byte, int, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			int num = values.Length;
			byte[] array = new byte[37 + values.Length];
			array[0] = 104;
			array[1] = BitConverter.GetBytes(array.Length - 6)[0];
			array[2] = BitConverter.GetBytes(array.Length - 6)[0];
			array[3] = 104;
			array[4] = station;
			array[5] = 0;
			array[6] = 124;
			array[7] = 50;
			array[8] = 1;
			array[9] = 0;
			array[10] = 0;
			array[11] = 0;
			array[12] = 0;
			array[13] = 0;
			array[14] = 14;
			array[15] = 0;
			array[16] = (byte)(values.Length + 4);
			array[17] = 5;
			array[18] = 1;
			array[19] = 18;
			array[20] = 10;
			array[21] = 16;
			array[22] = 2;
			array[23] = 0;
			array[24] = BitConverter.GetBytes(num)[0];
			array[25] = BitConverter.GetBytes(num)[1];
			array[26] = (byte)operateResult.Content3;
			array[27] = operateResult.Content1;
			array[28] = BitConverter.GetBytes(operateResult.Content2)[2];
			array[29] = BitConverter.GetBytes(operateResult.Content2)[1];
			array[30] = BitConverter.GetBytes(operateResult.Content2)[0];
			array[31] = 0;
			array[32] = 4;
			array[33] = BitConverter.GetBytes(num * 8)[1];
			array[34] = BitConverter.GetBytes(num * 8)[0];
			values.CopyTo(array, 35);
			int num2 = 0;
			for (int i = 4; i < array.Length - 2; i++)
			{
				num2 += array[i];
			}
			array[array.Length - 2] = BitConverter.GetBytes(num2)[0];
			array[array.Length - 1] = 22;
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 根据错误代号信息，获取到指定的文本信息<br />
		/// According to the error code information, get the specified text information
		/// </summary>
		/// <param name="code">错误状态信息</param>
		/// <returns>消息文本</returns>
		public static string GetMsgFromStatus(byte code)
		{
			switch (code)
			{
			case byte.MaxValue:
				return "No error";
			case 1:
				return "Hardware fault";
			case 3:
				return "Illegal object access";
			case 5:
				return "Invalid address(incorrent variable address)";
			case 6:
				return "Data type is not supported";
			case 10:
				return "Object does not exist or length error";
			default:
				return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 根据错误信息，获取到文本信息
		/// </summary>
		/// <param name="errorClass">错误类型</param>
		/// <param name="errorCode">错误代码</param>
		/// <returns>错误信息</returns>
		public static string GetMsgFromStatus(byte errorClass, byte errorCode)
		{
			if (errorClass == 128 && errorCode == 1)
			{
				return "Switch\u2002in\u2002wrong\u2002position\u2002for\u2002requested\u2002operation";
			}
			if (errorClass == 129 && errorCode == 4)
			{
				return "Miscellaneous\u2002structure\u2002error\u2002in\u2002command.\u2002\u2002Command is not supportedby CPU";
			}
			if (errorClass == 132 && errorCode == 4)
			{
				return "CPU is busy processing an upload or download CPU cannot process command because of system fault condition";
			}
			if (errorClass == 133 && errorCode == 0)
			{
				return "Length fields are not correct or do not agree with the amount of data received";
			}
			int num;
			switch (errorClass)
			{
			case 210:
				return "Error in upload or download command";
			case 214:
				return "Protection error(password)";
			case 220:
				num = ((errorCode == 1) ? 1 : 0);
				break;
			default:
				num = 0;
				break;
			}
			if (num != 0)
			{
				return "Error in time-of-day clock data";
			}
			return StringResources.Language.UnknownError;
		}

		/// <summary>
		/// 创建写入PLC的bool类型数据报文指令
		/// </summary>
		/// <param name="station">PLC的站号信息</param>
		/// <param name="address">地址信息</param>
		/// <param name="values">bool[]数据值</param>
		/// <returns>带有成功标识的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteCommand(byte station, string address, bool[] values)
		{
			OperateResult<byte, int, ushort> operateResult = AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = SoftBasic.BoolArrayToByte(values);
			byte[] array2 = new byte[37 + array.Length];
			array2[0] = 104;
			array2[1] = BitConverter.GetBytes(array2.Length - 6)[0];
			array2[2] = BitConverter.GetBytes(array2.Length - 6)[0];
			array2[3] = 104;
			array2[4] = station;
			array2[5] = 0;
			array2[6] = 124;
			array2[7] = 50;
			array2[8] = 1;
			array2[9] = 0;
			array2[10] = 0;
			array2[11] = 0;
			array2[12] = 0;
			array2[13] = 0;
			array2[14] = 14;
			array2[15] = 0;
			array2[16] = 5;
			array2[17] = 5;
			array2[18] = 1;
			array2[19] = 18;
			array2[20] = 10;
			array2[21] = 16;
			array2[22] = 1;
			array2[23] = 0;
			array2[24] = BitConverter.GetBytes(values.Length)[0];
			array2[25] = BitConverter.GetBytes(values.Length)[1];
			array2[26] = (byte)operateResult.Content3;
			array2[27] = operateResult.Content1;
			array2[28] = BitConverter.GetBytes(operateResult.Content2)[2];
			array2[29] = BitConverter.GetBytes(operateResult.Content2)[1];
			array2[30] = BitConverter.GetBytes(operateResult.Content2)[0];
			array2[31] = 0;
			array2[32] = 3;
			array2[33] = BitConverter.GetBytes(values.Length)[1];
			array2[34] = BitConverter.GetBytes(values.Length)[0];
			array.CopyTo(array2, 35);
			int num = 0;
			for (int i = 4; i < array2.Length - 2; i++)
			{
				num += array2[i];
			}
			array2[array2.Length - 2] = BitConverter.GetBytes(num)[0];
			array2[array2.Length - 1] = 22;
			return OperateResult.CreateSuccessResult(array2);
		}

		/// <summary>
		/// 检查西门子PLC的返回的数据和合法性，对反馈的数据进行初步的校验
		/// </summary>
		/// <param name="content">服务器返回的原始的数据内容</param>
		/// <returns>是否校验成功</returns>
		public static OperateResult CheckResponse(byte[] content)
		{
			if (content.Length < 21)
			{
				return new OperateResult(10000, "Failed, data too short:" + SoftBasic.ByteToHexString(content, ' '));
			}
			if (content[17] != 0 || content[18] != 0)
			{
				return new OperateResult(content[19], GetMsgFromStatus(content[18], content[19]));
			}
			if (content[21] != byte.MaxValue)
			{
				return new OperateResult(content[21], GetMsgFromStatus(content[21]));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 根据站号信息获取命令二次确认的报文信息
		/// </summary>
		/// <param name="station">站号信息</param>
		/// <returns>二次命令确认的报文</returns>
		public static byte[] GetExecuteConfirm(byte station)
		{
			byte[] array = new byte[6] { 16, 2, 0, 92, 94, 22 };
			array[1] = station;
			int num = 0;
			for (int i = 1; i < 4; i++)
			{
				num += array[i];
			}
			array[4] = (byte)num;
			return array;
		}

		/// <summary>
		/// 从西门子的PLC中读取数据信息，地址为"M100","AI100","I0","Q0","V100","S100"等<br />
		/// Read data information from Siemens PLC with addresses "M100", "AI100", "I0", "Q0", "V100", "S100", etc.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>带返回结果的结果对象</returns>
		public static OperateResult<byte[]> Read(IReadWriteDevice plc, string address, ushort length, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildReadCommand(station2, address, length, isBit: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				if (operateResult2.Content[0] != 229)
				{
					return new OperateResult<byte[]>("PLC Receive Check Failed:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
				}
				OperateResult<byte[]> operateResult3 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				OperateResult operateResult4 = CheckResponse(operateResult3.Content);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult4);
				}
				byte[] array = new byte[length];
				if (operateResult3.Content[21] == byte.MaxValue && operateResult3.Content[22] == 4)
				{
					Array.Copy(operateResult3.Content, 25, array, 0, length);
				}
				return OperateResult.CreateSuccessResult(array);
			}
		}

		/// <summary>
		/// 从西门子的PLC中读取bool数据信息，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等<br />
		/// Read bool data information from Siemens PLC, the addresses are "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>带返回结果的结果对象</returns>
		public static OperateResult<bool[]> ReadBool(IReadWriteDevice plc, string address, ushort length, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildReadCommand(station2, address, length, isBit: true);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult2);
				}
				if (operateResult2.Content[0] != 229)
				{
					return new OperateResult<bool[]>("PLC Receive Check Failed:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
				}
				OperateResult<byte[]> operateResult3 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult3.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult3);
				}
				OperateResult operateResult4 = CheckResponse(operateResult3.Content);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult4);
				}
				byte[] array = new byte[operateResult3.Content.Length - 27];
				if (operateResult3.Content[21] == byte.MaxValue && operateResult3.Content[22] == 3)
				{
					Array.Copy(operateResult3.Content, 25, array, 0, array.Length);
				}
				return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(array, length));
			}
		}

		/// <summary>
		/// 将字节数据写入到西门子PLC中，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等<br />
		/// Write byte data to Siemens PLC with addresses "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>带返回结果的结果对象</returns>
		public static OperateResult Write(IReadWriteDevice plc, string address, byte[] value, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildWriteCommand(station2, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				if (operateResult2.Content[0] != 229)
				{
					return new OperateResult<byte[]>("PLC Receive Check Failed:" + operateResult2.Content[0]);
				}
				OperateResult<byte[]> operateResult3 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				OperateResult operateResult4 = CheckResponse(operateResult3.Content);
				if (!operateResult4.IsSuccess)
				{
					return operateResult4;
				}
				return OperateResult.CreateSuccessResult();
			}
		}

		/// <summary>
		/// 将bool数据写入到西门子PLC中，地址为"M100.0","AI100.1","I0.3","Q0.6","V100.4","S100"等<br />
		/// Write the bool data to Siemens PLC with the addresses "M100.0", "AI100.1", "I0.3", "Q0.6", "V100.4", "S100", etc.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>带返回结果的结果对象</returns>
		public static OperateResult Write(IReadWriteDevice plc, string address, bool[] value, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = BuildWriteCommand(station2, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				if (operateResult2.Content[0] != 229)
				{
					return new OperateResult<byte[]>("PLC Receive Check Failed:" + operateResult2.Content[0]);
				}
				OperateResult<byte[]> operateResult3 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult3.IsSuccess)
				{
					return operateResult3;
				}
				OperateResult operateResult4 = CheckResponse(operateResult3.Content);
				if (!operateResult4.IsSuccess)
				{
					return operateResult4;
				}
				return OperateResult.CreateSuccessResult();
			}
		}

		/// <summary>
		/// 启动西门子PLC为RUN模式，参数信息可以携带站号信息 "s=2;", 注意，分号是必须的。<br />
		/// Start Siemens PLC in RUN mode, parameter information can carry station number information "s=2;", note that the semicolon is required.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="parameter">额外的参数信息，例如可以携带站号信息 "s=2;", 注意，分号是必须的。</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult Start(IReadWriteDevice plc, string parameter, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref parameter, "s", station);
			byte[] obj = new byte[39]
			{
				104, 33, 33, 104, 0, 0, 108, 50, 1, 0,
				0, 0, 0, 0, 20, 0, 0, 40, 0, 0,
				0, 0, 0, 0, 253, 0, 0, 9, 80, 95,
				80, 82, 79, 71, 82, 65, 77, 170, 22
			};
			obj[4] = station;
			byte[] send = obj;
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(send);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				if (operateResult.Content[0] != 229)
				{
					return new OperateResult<byte[]>("PLC Receive Check Failed:" + operateResult.Content[0]);
				}
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				return OperateResult.CreateSuccessResult();
			}
		}

		/// <summary>
		/// 停止西门子PLC，切换为Stop模式，参数信息可以携带站号信息 "s=2;", 注意，分号是必须的。<br />
		/// Stop Siemens PLC and switch to Stop mode, parameter information can carry station number information "s=2;", note that the semicolon is required.
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="parameter">额外的参数信息，例如可以携带站号信息 "s=2;", 注意，分号是必须的。</param>
		/// <param name="station">当前的站号信息</param>
		/// <param name="communicationLock">当前的同通信锁</param>
		/// <returns>是否停止成功</returns>
		public static OperateResult Stop(IReadWriteDevice plc, string parameter, byte station, object communicationLock)
		{
			byte station2 = (byte)HslHelper.ExtractParameter(ref parameter, "s", station);
			byte[] obj = new byte[35]
			{
				104, 29, 29, 104, 0, 0, 108, 50, 1, 0,
				0, 0, 0, 0, 16, 0, 0, 41, 0, 0,
				0, 0, 0, 9, 80, 95, 80, 82, 79, 71,
				82, 65, 77, 170, 22
			};
			obj[4] = station;
			byte[] send = obj;
			lock (communicationLock)
			{
				OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(send);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				if (operateResult.Content[0] != 229)
				{
					return new OperateResult<byte[]>("PLC Receive Check Failed:" + operateResult.Content[0]);
				}
				OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(GetExecuteConfirm(station2));
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				return OperateResult.CreateSuccessResult();
			}
		}
	}
}

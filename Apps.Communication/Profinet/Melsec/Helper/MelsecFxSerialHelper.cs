using System;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;

namespace Apps.Communication.Profinet.Melsec.Helper
{
	/// <summary>
	/// 三菱编程口协议的辅助方法
	/// </summary>
	public class MelsecFxSerialHelper
	{
		/// <summary>
		/// 根据指定的地址及长度信息从三菱PLC中读取原始的字节数据，根据PLC中实际定义的规则，可以解析出任何类的数据信息<br />
		/// Read the original byte data from the Mitsubishi PLC according to the specified address and length information. 
		/// According to the rules actually defined in the PLC, any type of data information can be parsed
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="address">读取地址，，支持的类型参考文档说明</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="isNewVersion">是否是新版的串口访问类</param>
		/// <returns>带成功标志的结果数据对象</returns>
		/// <example>
		/// 假设起始地址为D100，D100存储了温度，100.6℃值为1006，D101存储了压力，1.23Mpa值为123，D102，D103存储了产量计数，读取如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="ReadExample2" title="Read示例" />
		/// 以下是读取不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="ReadExample1" title="Read示例" />
		/// </example>
		public static OperateResult<byte[]> Read(IReadWriteDevice plc, string address, ushort length, bool isNewVersion)
		{
			OperateResult<byte[]> operateResult = BuildReadWordCommand(address, length, isNewVersion);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			OperateResult operateResult3 = CheckPlcReadResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			return ExtractActualData(operateResult2.Content);
		}

		/// <summary>
		/// 从三菱PLC中批量读取位软元件，返回读取结果，该读取地址最好从0，16，32...等开始读取，这样可以读取比较长的数据数组<br />
		/// Read bit devices in batches from Mitsubishi PLC and return the read results. 
		/// The read address should preferably be read from 0, 16, 32... etc., so that a relatively long data array can be read
		/// </summary>
		/// <param name="plc">PLC的通信对象</param>
		/// <param name="address">起始地址</param>
		/// <param name="length">读取的长度</param>
		/// <returns>带成功标志的结果数据对象</returns>
		/// <example>
		///  <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="ReadBool" title="Bool类型示例" />
		/// </example>
		public static OperateResult<bool[]> ReadBool(IReadWriteDevice plc, string address, ushort length)
		{
			OperateResult<byte[], int> operateResult = BuildReadBoolCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content1);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult operateResult3 = CheckPlcReadResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			return ExtractActualBoolData(operateResult2.Content, operateResult.Content2, length);
		}

		/// <summary>
		/// 根据指定的地址向PLC写入数据，数据格式为原始的字节类型<br />
		/// Write data to the PLC according to the specified address, the data format is the original byte type
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="address">初始地址，支持的类型参考文档说明</param>
		/// <param name="value">原始的字节数据</param>
		/// <param name="isNewVersion">是否是新版的串口访问类</param>
		/// <example>
		/// 假设起始地址为D100，D100存储了温度，100.6℃值为1006，D101存储了压力，1.23Mpa值为123，D102，D103存储了产量计数，写入如下：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="WriteExample2" title="Write示例" />
		/// 以下是读取不同类型数据的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="WriteExample1" title="Write示例" />
		/// </example>
		/// <returns>是否写入成功的结果对象</returns>
		public static OperateResult Write(IReadWriteDevice plc, string address, byte[] value, bool isNewVersion)
		{
			OperateResult<byte[]> operateResult = BuildWriteWordCommand(address, value, isNewVersion);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return CheckPlcWriteResponse(operateResult2.Content);
		}

		/// <summary>
		/// 强制写入位数据的通断，支持的类型参考文档说明<br />
		/// The on-off of the forced write bit data, please refer to the document description for the supported types
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <param name="address">地址信息</param>
		/// <param name="value">是否为通</param>
		/// <returns>是否写入成功的结果对象</returns>
		public static OperateResult Write(IReadWriteDevice plc, string address, bool value)
		{
			OperateResult<byte[]> operateResult = BuildWriteBoolPacket(address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return CheckPlcWriteResponse(operateResult2.Content);
		}

		/// <summary>
		/// 激活PLC的接收状态，需要再和PLC交互之前进行调用，之后就需要再调用了。<br />
		/// </summary>
		/// <param name="plc">PLC通信对象</param>
		/// <returns>是否激活成功</returns>
		public static OperateResult ActivePlc(IReadWriteDevice plc)
		{
			OperateResult<byte[]> operateResult = plc.ReadFromCoreServer(new byte[1] { 5 });
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			if (operateResult.Content[0] != 6)
			{
				return new OperateResult("Send 0x05, Check Receive 0x06 failed");
			}
			OperateResult<byte[]> operateResult2 = plc.ReadFromCoreServer(new byte[11]
			{
				2, 48, 48, 69, 48, 50, 48, 50, 3, 54,
				67
			});
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return plc.ReadFromCoreServer(new byte[11]
			{
				2, 48, 48, 69, 48, 50, 48, 50, 3, 54,
				67
			});
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.Read(Communication.Core.IReadWriteDevice,System.String,System.UInt16,System.Boolean)" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IReadWriteDevice plc, string address, ushort length, bool isNewVersion)
		{
			OperateResult<byte[]> command = BuildReadWordCommand(address, length, isNewVersion);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(command);
			}
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			OperateResult ackResult = CheckPlcReadResponse(read.Content);
			if (!ackResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(ackResult);
			}
			return ExtractActualData(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.ReadBool(Communication.Core.IReadWriteDevice,System.String,System.UInt16)" />
		public static async Task<OperateResult<bool[]>> ReadBoolAsync(IReadWriteDevice plc, string address, ushort length)
		{
			OperateResult<byte[], int> command = BuildReadBoolCommand(address, length);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			OperateResult ackResult = CheckPlcReadResponse(read.Content);
			if (!ackResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(ackResult);
			}
			return ExtractActualBoolData(read.Content, command.Content2, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Byte[],System.Boolean)" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice plc, string address, byte[] value, bool isNewVersion)
		{
			OperateResult<byte[]> command = BuildWriteWordCommand(address, value, isNewVersion);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckPlcWriteResponse(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Boolean)" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice plc, string address, bool value)
		{
			OperateResult<byte[]> command = BuildWriteBoolPacket(address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await plc.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckPlcWriteResponse(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.ActivePlc(Communication.Core.IReadWriteDevice)" />
		public static async Task<OperateResult> ActivePlcAsync(IReadWriteDevice plc)
		{
			OperateResult<byte[]> read1 = await plc.ReadFromCoreServerAsync(new byte[1] { 5 });
			if (!read1.IsSuccess)
			{
				return read1;
			}
			if (read1.Content[0] != 6)
			{
				return new OperateResult("Send 0x05, Check Receive 0x06 failed");
			}
			OperateResult<byte[]> read2 = await plc.ReadFromCoreServerAsync(new byte[11]
			{
				2, 48, 48, 69, 48, 50, 48, 50, 3, 54,
				67
			});
			if (!read2.IsSuccess)
			{
				return read2;
			}
			return plc.ReadFromCoreServer(new byte[11]
			{
				2, 48, 48, 69, 48, 50, 48, 50, 3, 54,
				67
			});
		}

		/// <summary>
		/// 检查PLC返回的读取数据是否是正常的
		/// </summary>
		/// <param name="ack">Plc反馈的数据信息</param>
		/// <returns>检查结果</returns>
		public static OperateResult CheckPlcReadResponse(byte[] ack)
		{
			if (ack.Length == 0)
			{
				return new OperateResult(StringResources.Language.MelsecFxReceiveZero);
			}
			if (ack[0] == 21)
			{
				return new OperateResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
			}
			if (ack[0] != 2)
			{
				return new OperateResult(StringResources.Language.MelsecFxAckWrong + ack[0] + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
			}
			if (!MelsecHelper.CheckCRC(ack))
			{
				return new OperateResult(StringResources.Language.MelsecFxCrcCheckFailed + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 检查PLC返回的写入的数据是否是正常的
		/// </summary>
		/// <param name="ack">Plc反馈的数据信息</param>
		/// <returns>检查结果</returns>
		public static OperateResult CheckPlcWriteResponse(byte[] ack)
		{
			if (ack.Length == 0)
			{
				return new OperateResult(StringResources.Language.MelsecFxReceiveZero);
			}
			if (ack[0] == 21)
			{
				return new OperateResult(StringResources.Language.MelsecFxAckNagative + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
			}
			if (ack[0] != 6)
			{
				return new OperateResult(StringResources.Language.MelsecFxAckWrong + ack[0] + " Actual: " + SoftBasic.ByteToHexString(ack, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 生成位写入的数据报文信息，该报文可直接用于发送串口给PLC
		/// </summary>
		/// <param name="address">地址信息，每个地址存在一定的范围，需要谨慎传入数据。举例：M10,S10,X5,Y10,C10,T10</param>
		/// <param name="value"><c>True</c>或是<c>False</c></param>
		/// <returns>带报文信息的结果对象</returns>
		public static OperateResult<byte[]> BuildWriteBoolPacket(string address, bool value)
		{
			OperateResult<MelsecMcDataType, ushort> operateResult = FxAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			ushort content = operateResult.Content2;
			if (operateResult.Content1 == MelsecMcDataType.M)
			{
				content = ((content < 8000) ? ((ushort)(content + 2048)) : ((ushort)(content - 8000 + 3840)));
			}
			else if (operateResult.Content1 == MelsecMcDataType.S)
			{
				content = content;
			}
			else if (operateResult.Content1 == MelsecMcDataType.X)
			{
				content = (ushort)(content + 1024);
			}
			else if (operateResult.Content1 == MelsecMcDataType.Y)
			{
				content = (ushort)(content + 1280);
			}
			else if (operateResult.Content1 == MelsecMcDataType.CS)
			{
				content = (ushort)(content + 448);
			}
			else if (operateResult.Content1 == MelsecMcDataType.CC)
			{
				content = (ushort)(content + 960);
			}
			else if (operateResult.Content1 == MelsecMcDataType.CN)
			{
				content = (ushort)(content + 3584);
			}
			else if (operateResult.Content1 == MelsecMcDataType.TS)
			{
				content = (ushort)(content + 192);
			}
			else if (operateResult.Content1 == MelsecMcDataType.TC)
			{
				content = (ushort)(content + 704);
			}
			else
			{
				if (operateResult.Content1 != MelsecMcDataType.TN)
				{
					return new OperateResult<byte[]>(StringResources.Language.MelsecCurrentTypeNotSupportedBitOperate);
				}
				content = (ushort)(content + 1536);
			}
			byte[] array = new byte[9]
			{
				2,
				(byte)(value ? 55 : 56),
				SoftBasic.BuildAsciiBytesFrom(content)[2],
				SoftBasic.BuildAsciiBytesFrom(content)[3],
				SoftBasic.BuildAsciiBytesFrom(content)[0],
				SoftBasic.BuildAsciiBytesFrom(content)[1],
				3,
				0,
				0
			};
			MelsecHelper.FxCalculateCRC(array).CopyTo(array, 7);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 根据类型地址长度确认需要读取的指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">长度</param>
		/// <param name="isNewVersion">是否是新版的串口访问类</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildReadWordCommand(string address, ushort length, bool isNewVersion)
		{
			OperateResult<ushort> operateResult = FxCalculateWordStartAddress(address, isNewVersion);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			length = (ushort)(length * 2);
			ushort content = operateResult.Content;
			if (isNewVersion)
			{
				byte[] array = new byte[13]
				{
					2,
					69,
					48,
					48,
					SoftBasic.BuildAsciiBytesFrom(content)[0],
					SoftBasic.BuildAsciiBytesFrom(content)[1],
					SoftBasic.BuildAsciiBytesFrom(content)[2],
					SoftBasic.BuildAsciiBytesFrom(content)[3],
					SoftBasic.BuildAsciiBytesFrom((byte)length)[0],
					SoftBasic.BuildAsciiBytesFrom((byte)length)[1],
					3,
					0,
					0
				};
				MelsecHelper.FxCalculateCRC(array).CopyTo(array, 11);
				return OperateResult.CreateSuccessResult(array);
			}
			byte[] array2 = new byte[11]
			{
				2,
				48,
				SoftBasic.BuildAsciiBytesFrom(content)[0],
				SoftBasic.BuildAsciiBytesFrom(content)[1],
				SoftBasic.BuildAsciiBytesFrom(content)[2],
				SoftBasic.BuildAsciiBytesFrom(content)[3],
				SoftBasic.BuildAsciiBytesFrom((byte)length)[0],
				SoftBasic.BuildAsciiBytesFrom((byte)length)[1],
				3,
				0,
				0
			};
			MelsecHelper.FxCalculateCRC(array2).CopyTo(array2, 9);
			return OperateResult.CreateSuccessResult(array2);
		}

		/// <summary>
		/// 根据类型地址长度确认需要读取的指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">bool数组长度</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[], int> BuildReadBoolCommand(string address, ushort length)
		{
			OperateResult<ushort, ushort, ushort> operateResult = FxCalculateBoolStartAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], int>(operateResult);
			}
			ushort num = (ushort)((operateResult.Content2 + length - 1) / 8 - (int)operateResult.Content2 / 8 + 1);
			ushort content = operateResult.Content1;
			byte[] array = new byte[11]
			{
				2,
				48,
				SoftBasic.BuildAsciiBytesFrom(content)[0],
				SoftBasic.BuildAsciiBytesFrom(content)[1],
				SoftBasic.BuildAsciiBytesFrom(content)[2],
				SoftBasic.BuildAsciiBytesFrom(content)[3],
				SoftBasic.BuildAsciiBytesFrom((byte)num)[0],
				SoftBasic.BuildAsciiBytesFrom((byte)num)[1],
				3,
				0,
				0
			};
			MelsecHelper.FxCalculateCRC(array).CopyTo(array, 9);
			return OperateResult.CreateSuccessResult(array, (int)operateResult.Content3);
		}

		/// <summary>
		/// 根据类型地址以及需要写入的数据来生成指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">实际的数据信息</param>
		/// <param name="isNewVersion">是否是新版的串口访问类</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand(string address, byte[] value, bool isNewVersion)
		{
			OperateResult<ushort> operateResult = FxCalculateWordStartAddress(address, isNewVersion);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (value != null)
			{
				value = SoftBasic.BuildAsciiBytesFrom(value);
			}
			ushort content = operateResult.Content;
			if (isNewVersion)
			{
				byte[] array = new byte[13 + value.Length];
				array[0] = 2;
				array[1] = 69;
				array[2] = 49;
				array[3] = 48;
				array[4] = SoftBasic.BuildAsciiBytesFrom(content)[0];
				array[5] = SoftBasic.BuildAsciiBytesFrom(content)[1];
				array[6] = SoftBasic.BuildAsciiBytesFrom(content)[2];
				array[7] = SoftBasic.BuildAsciiBytesFrom(content)[3];
				array[8] = SoftBasic.BuildAsciiBytesFrom((byte)(value.Length / 2))[0];
				array[9] = SoftBasic.BuildAsciiBytesFrom((byte)(value.Length / 2))[1];
				Array.Copy(value, 0, array, 10, value.Length);
				array[array.Length - 3] = 3;
				MelsecHelper.FxCalculateCRC(array).CopyTo(array, array.Length - 2);
				return OperateResult.CreateSuccessResult(array);
			}
			byte[] array2 = new byte[11 + value.Length];
			array2[0] = 2;
			array2[1] = 49;
			array2[2] = SoftBasic.BuildAsciiBytesFrom(content)[0];
			array2[3] = SoftBasic.BuildAsciiBytesFrom(content)[1];
			array2[4] = SoftBasic.BuildAsciiBytesFrom(content)[2];
			array2[5] = SoftBasic.BuildAsciiBytesFrom(content)[3];
			array2[6] = SoftBasic.BuildAsciiBytesFrom((byte)(value.Length / 2))[0];
			array2[7] = SoftBasic.BuildAsciiBytesFrom((byte)(value.Length / 2))[1];
			Array.Copy(value, 0, array2, 8, value.Length);
			array2[array2.Length - 3] = 3;
			MelsecHelper.FxCalculateCRC(array2).CopyTo(array2, array2.Length - 2);
			return OperateResult.CreateSuccessResult(array2);
		}

		/// <summary>
		/// 从PLC反馈的数据进行提炼操作
		/// </summary>
		/// <param name="response">PLC反馈的真实数据</param>
		/// <returns>数据提炼后的真实数据</returns>
		public static OperateResult<byte[]> ExtractActualData(byte[] response)
		{
			try
			{
				byte[] array = new byte[(response.Length - 4) / 2];
				for (int i = 0; i < array.Length; i++)
				{
					byte[] bytes = new byte[2]
					{
						response[i * 2 + 1],
						response[i * 2 + 2]
					};
					array[i] = Convert.ToByte(Encoding.ASCII.GetString(bytes), 16);
				}
				return OperateResult.CreateSuccessResult(array);
			}
			catch (Exception ex)
			{
				OperateResult<byte[]> operateResult = new OperateResult<byte[]>();
				operateResult.Message = "Extract Msg：" + ex.Message + Environment.NewLine + "Data: " + SoftBasic.ByteToHexString(response);
				return operateResult;
			}
		}

		/// <summary>
		/// 从PLC反馈的数据进行提炼bool数组操作
		/// </summary>
		/// <param name="response">PLC反馈的真实数据</param>
		/// <param name="start">起始提取的点信息</param>
		/// <param name="length">bool数组的长度</param>
		/// <returns>数据提炼后的真实数据</returns>
		public static OperateResult<bool[]> ExtractActualBoolData(byte[] response, int start, int length)
		{
			OperateResult<byte[]> operateResult = ExtractActualData(response);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			try
			{
				bool[] array = new bool[length];
				bool[] array2 = SoftBasic.ByteToBoolArray(operateResult.Content, operateResult.Content.Length * 8);
				for (int i = 0; i < length; i++)
				{
					array[i] = array2[i + start];
				}
				return OperateResult.CreateSuccessResult(array);
			}
			catch (Exception ex)
			{
				OperateResult<bool[]> operateResult2 = new OperateResult<bool[]>();
				operateResult2.Message = "Extract Msg：" + ex.Message + Environment.NewLine + "Data: " + SoftBasic.ByteToHexString(response);
				return operateResult2;
			}
		}

		/// <summary>
		/// 解析数据地址成不同的三菱地址类型
		/// </summary>
		/// <param name="address">数据地址</param>
		/// <returns>地址结果对象</returns>
		private static OperateResult<MelsecMcDataType, ushort> FxAnalysisAddress(string address)
		{
			OperateResult<MelsecMcDataType, ushort> operateResult = new OperateResult<MelsecMcDataType, ushort>();
			try
			{
				switch (address[0])
				{
				case 'M':
				case 'm':
					operateResult.Content1 = MelsecMcDataType.M;
					operateResult.Content2 = Convert.ToUInt16(address.Substring(1), MelsecMcDataType.M.FromBase);
					break;
				case 'X':
				case 'x':
					operateResult.Content1 = MelsecMcDataType.X;
					operateResult.Content2 = Convert.ToUInt16(address.Substring(1), 8);
					break;
				case 'Y':
				case 'y':
					operateResult.Content1 = MelsecMcDataType.Y;
					operateResult.Content2 = Convert.ToUInt16(address.Substring(1), 8);
					break;
				case 'D':
				case 'd':
					operateResult.Content1 = MelsecMcDataType.D;
					operateResult.Content2 = Convert.ToUInt16(address.Substring(1), MelsecMcDataType.D.FromBase);
					break;
				case 'S':
				case 's':
					operateResult.Content1 = MelsecMcDataType.S;
					operateResult.Content2 = Convert.ToUInt16(address.Substring(1), MelsecMcDataType.S.FromBase);
					break;
				case 'T':
				case 't':
					if (address[1] == 'N' || address[1] == 'n')
					{
						operateResult.Content1 = MelsecMcDataType.TN;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						operateResult.Content1 = MelsecMcDataType.TS;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						operateResult.Content1 = MelsecMcDataType.TC;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.TC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'C':
				case 'c':
					if (address[1] == 'N' || address[1] == 'n')
					{
						operateResult.Content1 = MelsecMcDataType.CN;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						operateResult.Content1 = MelsecMcDataType.CS;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						operateResult.Content1 = MelsecMcDataType.CC;
						operateResult.Content2 = Convert.ToUInt16(address.Substring(2), MelsecMcDataType.CC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
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
		/// 返回读取的地址及长度信息
		/// </summary>
		/// <param name="address">读取的地址信息</param>
		/// <param name="isNewVersion">是否是新版的串口访问类</param>
		/// <returns>带起始地址的结果对象</returns>
		private static OperateResult<ushort> FxCalculateWordStartAddress(string address, bool isNewVersion)
		{
			OperateResult<MelsecMcDataType, ushort> operateResult = FxAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort>(operateResult);
			}
			ushort content = operateResult.Content2;
			if (operateResult.Content1 == MelsecMcDataType.D)
			{
				content = ((content < 8000) ? (isNewVersion ? ((ushort)(content * 2 + 16384)) : ((ushort)(content * 2 + 4096))) : ((ushort)((content - 8000) * 2 + 3584)));
			}
			else if (operateResult.Content1 == MelsecMcDataType.CN)
			{
				content = ((content < 200) ? ((ushort)(content * 2 + 2560)) : ((ushort)((content - 200) * 4 + 3072)));
			}
			else
			{
				if (operateResult.Content1 != MelsecMcDataType.TN)
				{
					return new OperateResult<ushort>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);
				}
				content = (ushort)(content * 2 + 2048);
			}
			return OperateResult.CreateSuccessResult(content);
		}

		/// <summary>
		/// 返回读取的地址及长度信息，以及当前的偏置信息
		/// </summary><param name="address">读取的地址信息</param>
		/// <returns>带起始地址的结果对象</returns>
		private static OperateResult<ushort, ushort, ushort> FxCalculateBoolStartAddress(string address)
		{
			OperateResult<MelsecMcDataType, ushort> operateResult = FxAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort, ushort>(operateResult);
			}
			ushort content = operateResult.Content2;
			if (operateResult.Content1 == MelsecMcDataType.M)
			{
				content = ((content < 8000) ? ((ushort)((int)content / 8 + 256)) : ((ushort)((content - 8000) / 8 + 480)));
			}
			else if (operateResult.Content1 == MelsecMcDataType.X)
			{
				content = (ushort)((int)content / 8 + 128);
			}
			else if (operateResult.Content1 == MelsecMcDataType.Y)
			{
				content = (ushort)((int)content / 8 + 160);
			}
			else if (operateResult.Content1 == MelsecMcDataType.S)
			{
				content = (ushort)((int)content / 8);
			}
			else if (operateResult.Content1 == MelsecMcDataType.CS)
			{
				content = (ushort)((int)content / 8 + 448);
			}
			else if (operateResult.Content1 == MelsecMcDataType.CC)
			{
				content = (ushort)((int)content / 8 + 960);
			}
			else if (operateResult.Content1 == MelsecMcDataType.TS)
			{
				content = (ushort)((int)content / 8 + 192);
			}
			else
			{
				if (operateResult.Content1 != MelsecMcDataType.TC)
				{
					return new OperateResult<ushort, ushort, ushort>(StringResources.Language.MelsecCurrentTypeNotSupportedBitOperate);
				}
				content = (ushort)((int)content / 8 + 704);
			}
			return OperateResult.CreateSuccessResult(content, operateResult.Content2, (ushort)((int)operateResult.Content2 % 8));
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// Omron PLC的FINS协议相关的辅助类，主要是一些地址解析，读写的指令生成。<br />
	/// The auxiliary classes related to the FINS protocol of Omron PLC are mainly some address resolution and the generation of read and write instructions.
	/// </summary>
	public class OmronFinsNetHelper
	{
		/// <summary>
		/// 根据读取的地址，长度，是否位读取创建Fins协议的核心报文<br />
		/// According to the read address, length, whether to read the core message that creates the Fins protocol
		/// </summary>
		/// <param name="address">地址，具体格式请参照示例说明</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="isBit">是否使用位读取</param>
		/// <param name="splitLength">读取的长度切割，默认500</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<List<byte[]>> BuildReadCommand(string address, ushort length, bool isBit, int splitLength = 500)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<List<byte[]>>(operateResult);
			}
			List<byte[]> list = new List<byte[]>();
			int[] array = SoftBasic.SplitIntegerToArray(length, isBit ? int.MaxValue : splitLength);
			for (int i = 0; i < array.Length; i++)
			{
				byte[] array2 = new byte[8] { 1, 1, 0, 0, 0, 0, 0, 0 };
				if (isBit)
				{
					array2[2] = operateResult.Content.BitCode;
				}
				else
				{
					array2[2] = operateResult.Content.WordCode;
				}
				array2[3] = (byte)(operateResult.Content.AddressStart / 16 / 256);
				array2[4] = (byte)(operateResult.Content.AddressStart / 16 % 256);
				array2[5] = (byte)(operateResult.Content.AddressStart % 16);
				array2[6] = (byte)(array[i] / 256);
				array2[7] = (byte)(array[i] % 256);
				list.Add(array2);
				operateResult.Content.AddressStart += (isBit ? array[i] : (array[i] * 16));
			}
			return OperateResult.CreateSuccessResult(list);
		}

		/// <summary>
		/// 根据写入的地址，数据，是否位写入生成Fins协议的核心报文<br />
		/// According to the written address, data, whether the bit is written to generate the core message of the Fins protocol
		/// </summary>
		/// <param name="address">地址内容，具体格式请参照示例说明</param>
		/// <param name="value">实际的数据</param>
		/// <param name="isBit">是否位数据</param>
		/// <returns>带有成功标识的Fins核心报文</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand(string address, byte[] value, bool isBit)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[8 + value.Length];
			array[0] = 1;
			array[1] = 2;
			if (isBit)
			{
				array[2] = operateResult.Content.BitCode;
			}
			else
			{
				array[2] = operateResult.Content.WordCode;
			}
			array[3] = (byte)(operateResult.Content.AddressStart / 16 / 256);
			array[4] = (byte)(operateResult.Content.AddressStart / 16 % 256);
			array[5] = (byte)(operateResult.Content.AddressStart % 16);
			if (isBit)
			{
				array[6] = (byte)(value.Length / 256);
				array[7] = (byte)(value.Length % 256);
			}
			else
			{
				array[6] = (byte)(value.Length / 2 / 256);
				array[7] = (byte)(value.Length / 2 % 256);
			}
			value.CopyTo(array, 8);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 验证欧姆龙的Fins-TCP返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容<br />
		/// Verify that the data returned by Omron's Fins-TCP is correct data, if correct, and return all data content
		/// </summary>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> ResponseValidAnalysis(byte[] response)
		{
			if (response.Length >= 16)
			{
				int num = BitConverter.ToInt32(new byte[4]
				{
					response[15],
					response[14],
					response[13],
					response[12]
				}, 0);
				if (num > 0)
				{
					return new OperateResult<byte[]>(num, GetStatusDescription(num));
				}
				return UdpResponseValidAnalysis(response.RemoveBegin(16));
			}
			return new OperateResult<byte[]>(StringResources.Language.OmronReceiveDataError);
		}

		/// <summary>
		/// 验证欧姆龙的Fins-Udp返回的数据是否正确的数据，如果正确的话，并返回所有的数据内容<br />
		/// Verify that the data returned by Omron's Fins-Udp is correct data, if correct, and return all data content
		/// </summary>
		/// <param name="response">来自欧姆龙返回的数据内容</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> UdpResponseValidAnalysis(byte[] response)
		{
			if (response.Length >= 14)
			{
				int num = response[12] * 256 + response[13];
				if (((response[10] == 1) & (response[11] == 1)) || ((response[10] == 1) & (response[11] == 4)) || ((response[10] == 2) & (response[11] == 1)) || ((response[10] == 3) & (response[11] == 6)) || ((response[10] == 5) & (response[11] == 1)) || ((response[10] == 5) & (response[11] == 2)) || ((response[10] == 6) & (response[11] == 1)) || ((response[10] == 6) & (response[11] == 32)) || ((response[10] == 7) & (response[11] == 1)) || ((response[10] == 9) & (response[11] == 32)) || ((response[10] == 33) & (response[11] == 2)) || ((response[10] == 34) & (response[11] == 2)))
				{
					byte[] array = new byte[response.Length - 14];
					if (array.Length != 0)
					{
						Array.Copy(response, 14, array, 0, array.Length);
					}
					OperateResult<byte[]> operateResult = OperateResult.CreateSuccessResult(array);
					if (array.Length == 0)
					{
						operateResult.IsSuccess = false;
					}
					operateResult.ErrorCode = num;
					operateResult.Message = GetStatusDescription(num) + " Received:" + SoftBasic.ByteToHexString(response, ' ');
					return operateResult;
				}
				OperateResult<byte[]> operateResult2 = OperateResult.CreateSuccessResult(new byte[0]);
				operateResult2.ErrorCode = num;
				operateResult2.Message = GetStatusDescription(num) + " Received:" + SoftBasic.ByteToHexString(response, ' ');
				return operateResult2;
			}
			return new OperateResult<byte[]>(StringResources.Language.OmronReceiveDataError);
		}

		/// <summary>
		/// 根据欧姆龙返回的错误码，获取错误信息的字符串描述文本<br />
		/// According to the error code returned by Omron, get the string description text of the error message
		/// </summary>
		/// <param name="err">错误码</param>
		/// <returns>文本描述</returns>
		public static string GetStatusDescription(int err)
		{
			switch (err)
			{
			case 0:
				return StringResources.Language.OmronStatus0;
			case 1:
				return StringResources.Language.OmronStatus1;
			case 2:
				return StringResources.Language.OmronStatus2;
			case 3:
				return StringResources.Language.OmronStatus3;
			case 32:
				return StringResources.Language.OmronStatus20;
			case 33:
				return StringResources.Language.OmronStatus21;
			case 34:
				return StringResources.Language.OmronStatus22;
			case 35:
				return StringResources.Language.OmronStatus23;
			case 36:
				return StringResources.Language.OmronStatus24;
			case 37:
				return StringResources.Language.OmronStatus25;
			default:
				return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 从欧姆龙PLC中读取想要的数据，返回读取结果，读取长度的单位为字，地址格式为"D100","C100","W100","H100","A100"<br />
		/// Read the desired data from the Omron PLC and return the read result. The unit of the read length is word. The address format is "D100", "C100", "W100", "H100", "A100"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">读取地址，格式为"D100","C100","W100","H100","A100"</param>
		/// <param name="length">读取的数据长度</param>
		/// <param name="splits">分割信息</param>
		/// <returns>带成功标志的结果数据对象</returns>
		public static OperateResult<byte[]> Read(IReadWriteDevice omron, string address, ushort length, int splits)
		{
			OperateResult<List<byte[]>> operateResult = BuildReadCommand(address, length, isBit: false, splits);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			List<byte> list = new List<byte>();
			for (int i = 0; i < operateResult.Content.Count; i++)
			{
				OperateResult<byte[]> operateResult2 = omron.ReadFromCoreServer(operateResult.Content[i]);
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult2);
				}
				list.AddRange(operateResult2.Content);
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// 向PLC写入数据，数据格式为原始的字节类型，地址格式为"D100","C100","W100","H100","A100"<br />
		/// Write data to PLC, the data format is the original byte type, and the address format is "D100", "C100", "W100", "H100", "A100"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">初始地址</param>
		/// <param name="value">原始的字节数据</param>
		/// <returns>结果</returns>
		public static OperateResult Write(IReadWriteDevice omron, string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteWordCommand(address, value, isBit: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = omron.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Read(Communication.Core.IReadWriteDevice,System.String,System.UInt16,System.Int32)" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IReadWriteDevice omron, string address, ushort length, int splits)
		{
			OperateResult<List<byte[]>> command = BuildReadCommand(address, length, isBit: false, splits);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(command);
			}
			List<byte> contentArray = new List<byte>();
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync(command.Content[i]);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(read);
				}
				contentArray.AddRange(read.Content);
			}
			return OperateResult.CreateSuccessResult(contentArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Byte[])" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice omron, string address, byte[] value)
		{
			OperateResult<byte[]> command = BuildWriteWordCommand(address, value, isBit: false);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 从欧姆龙PLC中批量读取位软元件，地址格式为"D100.0","C100.0","W100.0","H100.0","A100.0"<br />
		/// Read bit devices in batches from Omron PLC with address format "D100.0", "C100.0", "W100.0", "H100.0", "A100.0"
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">读取地址，格式为"D100","C100","W100","H100","A100"</param>
		/// <param name="length">读取的长度</param>
		/// <param name="splits">分割信息</param>
		/// <returns>带成功标志的结果数据对象</returns>
		public static OperateResult<bool[]> ReadBool(IReadWriteDevice omron, string address, ushort length, int splits)
		{
			OperateResult<List<byte[]>> operateResult = BuildReadCommand(address, length, isBit: true, splits);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			List<bool> list = new List<bool>();
			for (int i = 0; i < operateResult.Content.Count; i++)
			{
				OperateResult<byte[]> operateResult2 = omron.ReadFromCoreServer(operateResult.Content[i]);
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult2);
				}
				list.AddRange(operateResult2.Content.Select((byte m) => m != 0));
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <summary>
		/// 向PLC中位软元件写入bool数组，返回是否写入成功，比如你写入D100,values[0]对应D100.0，地址格式为"D100.0","C100.0","W100.0","H100.0","A100.0"<br />
		/// Write the bool array to the PLC's median device and return whether the write was successful. For example, if you write D100, values [0] corresponds to D100.0 
		/// and the address format is "D100.0", "C100.0", "W100. 0 "," H100.0 "," A100.0 "
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <param name="address">要写入的数据地址</param>
		/// <param name="values">要写入的实际数据，可以指定任意的长度</param>
		/// <returns>返回写入结果</returns>
		public static OperateResult Write(IReadWriteDevice omron, string address, bool[] values)
		{
			OperateResult<byte[]> operateResult = BuildWriteWordCommand(address, values.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), isBit: true);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = omron.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.ReadBool(Communication.Core.IReadWriteDevice,System.String,System.UInt16,System.Int32)" />
		public static async Task<OperateResult<bool[]>> ReadBoolAsync(IReadWriteDevice omron, string address, ushort length, int splits)
		{
			OperateResult<List<byte[]>> command = BuildReadCommand(address, length, isBit: true, splits);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			List<bool> contentArray = new List<bool>();
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync(command.Content[i]);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(read);
				}
				contentArray.AddRange(read.Content.Select((byte m) => m != 0));
			}
			return OperateResult.CreateSuccessResult(contentArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Boolean[])" />
		public static async Task<OperateResult> WriteAsync(IReadWriteDevice omron, string address, bool[] values)
		{
			OperateResult<byte[]> command = BuildWriteWordCommand(address, values.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), isBit: true);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await omron.ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 将CPU单元的操作模式更改为RUN，从而使PLC能够执行其程序。<br />
		/// Changes the CPU Unit’s operating mode to RUN, enabling the PLC to execute its program.
		/// </summary>
		/// <remarks>
		/// 当执行RUN时，CPU单元将开始运行。 在执行RUN之前，您必须确认系统的安全性。 启用“禁止覆盖受保护程序”设置时，无法执行此命令。<br />
		/// The CPU Unit will start operation when RUN is executed. You must confirm the safety of the system before executing RUN.
		/// When the “prohibit overwriting of protected program” setting is enabled, this command cannot be executed.
		/// </remarks>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否启动成功</returns>
		public static OperateResult Run(IReadWriteDevice omron)
		{
			return omron.ReadFromCoreServer(new byte[5] { 4, 1, 255, 255, 4 });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Run(Communication.Core.IReadWriteDevice)" />
		public static async Task<OperateResult> RunAsync(IReadWriteDevice omron)
		{
			return await omron.ReadFromCoreServerAsync(new byte[5] { 4, 1, 255, 255, 4 });
		}

		/// <summary>
		/// 将CPU单元的操作模式更改为PROGRAM，停止程序执行。<br />
		/// Changes the CPU Unit’s operating mode to PROGRAM, stopping program execution.
		/// </summary>
		/// <remarks>
		/// 当执行STOP时，CPU单元将停止操作。 在执行STOP之前，您必须确认系统的安全性。<br />
		/// The CPU Unit will stop operation when STOP is executed. You must confirm the safety of the system before executing STOP.
		/// </remarks>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否停止成功</returns>
		public static OperateResult Stop(IReadWriteDevice omron)
		{
			return omron.ReadFromCoreServer(new byte[4] { 4, 2, 255, 255 });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Stop(Communication.Core.IReadWriteDevice)" />
		public static async Task<OperateResult> StopAsync(IReadWriteDevice omron)
		{
			return await omron.ReadFromCoreServerAsync(new byte[4] { 4, 2, 255, 255 });
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取CPU的一些数据信息，主要包含型号，版本，一些数据块的大小<br />
		/// <b>[Authorization]</b> Read some data information of the CPU, mainly including the model, version, and the size of some data blocks
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否读取成功</returns>
		public static OperateResult<OmronCpuUnitData> ReadCpuUnitData(IReadWriteDevice omron)
		{
			return omron.ReadFromCoreServer(new byte[3] { 5, 1, 0 }).Then((byte[] m) => OperateResult.CreateSuccessResult(new OmronCpuUnitData(m)));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.ReadCpuUnitData(Communication.Core.IReadWriteDevice)" />
		public static async Task<OperateResult<OmronCpuUnitData>> ReadCpuUnitDataAsync(IReadWriteDevice omron)
		{
			return (await omron.ReadFromCoreServerAsync(new byte[3] { 5, 1, 0 })).Then((byte[] m) => OperateResult.CreateSuccessResult(new OmronCpuUnitData(m)));
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取CPU单元的一些操作状态数据，主要包含运行状态，工作模式，错误信息等。<br />
		/// <b>[Authorization]</b> Read some operating status data of the CPU unit, mainly including operating status, working mode, error information, etc.
		/// </summary>
		/// <param name="omron">PLC设备的连接对象</param>
		/// <returns>是否读取成功</returns>
		public static OperateResult<OmronCpuUnitStatus> ReadCpuUnitStatus(IReadWriteDevice omron)
		{
			return omron.ReadFromCoreServer(new byte[2] { 6, 1 }).Then((byte[] m) => OperateResult.CreateSuccessResult(new OmronCpuUnitStatus(m)));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.ReadCpuUnitStatus(Communication.Core.IReadWriteDevice)" />
		public static async Task<OperateResult<OmronCpuUnitStatus>> ReadCpuUnitStatusAsync(IReadWriteDevice omron)
		{

			return (await omron.ReadFromCoreServerAsync(new byte[2] { 6, 1 })).Then((byte[] m) => OperateResult.CreateSuccessResult(new OmronCpuUnitStatus(m)));
		}
	}
}

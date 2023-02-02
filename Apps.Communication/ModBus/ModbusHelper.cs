using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Serial;

namespace Apps.Communication.ModBus
{
	/// <summary>
	/// Modbus协议相关辅助类
	/// </summary>
	internal class ModbusHelper
	{
		public static OperateResult<byte[]> ExtraRtuResponseContent(byte[] send, byte[] response)
		{
			if (response.Length < 5)
			{
				return new OperateResult<byte[]>(StringResources.Language.ReceiveDataLengthTooShort + "5");
			}
			if (!SoftCRC16.CheckCRC16(response))
			{
				return new OperateResult<byte[]>(StringResources.Language.ModbusCRCCheckFailed + SoftBasic.ByteToHexString(response, ' '));
			}
			if (send[1] + 128 == response[1])
			{
				return new OperateResult<byte[]>(response[2], ModbusInfo.GetDescriptionByErrorCode(response[2]));
			}
			if (send[1] != response[1])
			{
				return new OperateResult<byte[]>(response[1], "Receive Command Check Failed: ");
			}
			return ModbusInfo.ExtractActualData(ModbusInfo.ExplodeRtuCommandToCore(response));
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Read(System.String,System.UInt16)" />
		public static OperateResult<byte[]> Read(IModbus modbus, string address, ushort length)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 3);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			OperateResult<byte[][]> operateResult2 = ModbusInfo.BuildReadModbusCommand(operateResult.Content, length, modbus.Station, modbus.AddressStartWithZero, 3);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			List<byte> list = new List<byte>();
			for (int i = 0; i < operateResult2.Content.Length; i++)
			{
				OperateResult<byte[]> operateResult3 = modbus.ReadFromCoreServer(operateResult2.Content[i]);
				if (!operateResult3.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult3);
				}
				list.AddRange(operateResult3.Content);
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Read(Communication.ModBus.IModbus,System.String,System.UInt16)" />
		public static async Task<OperateResult<byte[]>> ReadAsync(IModbus modbus, string address, ushort length)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 3);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress.ConvertFailed<byte[]>();
			}
			OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand(modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, 3);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(command);
			}
			List<byte> resultArray = new List<byte>();
			for (int i = 0; i < command.Content.Length; i++)
			{
				OperateResult<byte[]> read = await modbus.ReadFromCoreServerAsync(command.Content[i]);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(read);
				}
				resultArray.AddRange(read.Content);
			}
			return OperateResult.CreateSuccessResult(resultArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Byte[])" />
		public static OperateResult Write(IModbus modbus, string address, byte[] value)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 16);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteWordModbusCommand(operateResult.Content, value, modbus.Station, modbus.AddressStartWithZero, 16);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Write(Communication.ModBus.IModbus,System.String,System.Byte[])" />
		public static async Task<OperateResult> WriteAsync(IModbus modbus, string address, byte[] value)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 16);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand(modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, 16);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Int16)" />
		public static OperateResult Write(IModbus modbus, string address, short value)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 6);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteWordModbusCommand(operateResult.Content, value, modbus.Station, modbus.AddressStartWithZero, 6);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Write(Communication.ModBus.IModbus,System.String,System.Int16)" />
		public static async Task<OperateResult> WriteAsync(IModbus modbus, string address, short value)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 6);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand(modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, 6);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.UInt16)" />
		public static OperateResult Write(IModbus modbus, string address, ushort value)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 6);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteWordModbusCommand(operateResult.Content, value, modbus.Station, modbus.AddressStartWithZero, 6);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Write(Communication.ModBus.IModbus,System.String,System.UInt16)" />
		public static async Task<OperateResult> WriteAsync(IModbus modbus, string address, ushort value)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 6);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteWordModbusCommand(modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, 6);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteMask(System.String,System.UInt16,System.UInt16)" />
		public static OperateResult WriteMask(IModbus modbus, string address, ushort andMask, ushort orMask)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 22);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteMaskModbusCommand(operateResult.Content, andMask, orMask, modbus.Station, modbus.AddressStartWithZero, 22);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.WriteMask(Communication.ModBus.IModbus,System.String,System.UInt16,System.UInt16)" />
		public static async Task<OperateResult> WriteMaskAsync(IModbus modbus, string address, ushort andMask, ushort orMask)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 22);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteMaskModbusCommand(modbusAddress.Content, andMask, orMask, modbus.Station, modbus.AddressStartWithZero, 22);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}

		public static OperateResult<bool[]> ReadBoolHelper(IModbus modbus, string address, ushort length, byte function)
		{
			if (address.IndexOf('.') > 0)
			{
				string[] array = address.SplitDot();
				int num = 0;
				try
				{
					num = Convert.ToInt32(array[1]);
				}
				catch (Exception ex)
				{
					return new OperateResult<bool[]>("Bit Index format wrong, " + ex.Message);
				}
				ushort length2 = (ushort)((length + num + 15) / 16);
				OperateResult<byte[]> operateResult = modbus.Read(array[0], length2);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult);
				}
				return OperateResult.CreateSuccessResult(SoftBasic.BytesReverseByWord(operateResult.Content).ToBoolArray().SelectMiddle(num, length));
			}
			OperateResult<string> operateResult2 = modbus.TranslateToModbusAddress(address, function);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2.ConvertFailed<bool[]>();
			}
			OperateResult<byte[][]> operateResult3 = ModbusInfo.BuildReadModbusCommand(operateResult2.Content, length, modbus.Station, modbus.AddressStartWithZero, function);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			List<bool> list = new List<bool>();
			for (int i = 0; i < operateResult3.Content.Length; i++)
			{
				OperateResult<byte[]> operateResult4 = modbus.ReadFromCoreServer(operateResult3.Content[i]);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult4);
				}
				int length3 = operateResult3.Content[i][4] * 256 + operateResult3.Content[i][5];
				list.AddRange(SoftBasic.ByteToBoolArray(operateResult4.Content, length3));
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		internal static async Task<OperateResult<bool[]>> ReadBoolHelperAsync(IModbus modbus, string address, ushort length, byte function)
		{
			if (address.IndexOf('.') > 0)
			{
				string[] addressSplits = address.SplitDot();
				int bitIndex;
				try
				{
					bitIndex = Convert.ToInt32(addressSplits[1]);
				}
				catch (Exception ex2)
				{
					Exception ex = ex2;
					return new OperateResult<bool[]>("Bit Index format wrong, " + ex.Message);
				}
				OperateResult<byte[]> read2 = await modbus.ReadAsync(length: (ushort)((length + bitIndex + 15) / 16), address: addressSplits[0]);
				if (!read2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(read2);
				}
				return OperateResult.CreateSuccessResult(SoftBasic.BytesReverseByWord(read2.Content).ToBoolArray().SelectMiddle(bitIndex, length));
			}
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, function);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress.ConvertFailed<bool[]>();
			}
			OperateResult<byte[][]> command = ModbusInfo.BuildReadModbusCommand(modbusAddress.Content, length, modbus.Station, modbus.AddressStartWithZero, function);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			List<bool> resultArray = new List<bool>();
			for (int i = 0; i < command.Content.Length; i++)
			{
				OperateResult<byte[]> read = await modbus.ReadFromCoreServerAsync(command.Content[i]);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(read);
				}
				resultArray.AddRange(SoftBasic.ByteToBoolArray(length: command.Content[i][4] * 256 + command.Content[i][5], InBytes: read.Content));
			}
			return OperateResult.CreateSuccessResult(resultArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean[])" />
		public static OperateResult Write(IModbus modbus, string address, bool[] values)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 15);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteBoolModbusCommand(operateResult.Content, values, modbus.Station, modbus.AddressStartWithZero, 15);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Write(Communication.ModBus.IModbus,System.String,System.Boolean[])" />
		public static async Task<OperateResult> WriteAsync(IModbus modbus, string address, bool[] values)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 15);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand(modbusAddress.Content, values, modbus.Station, modbus.AddressStartWithZero, 15);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean)" />
		public static OperateResult Write(IModbus modbus, string address, bool value)
		{
			OperateResult<string> operateResult = modbus.TranslateToModbusAddress(address, 5);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ModbusInfo.BuildWriteBoolModbusCommand(operateResult.Content, value, modbus.Station, modbus.AddressStartWithZero, 5);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return modbus.ReadFromCoreServer(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusHelper.Write(Communication.ModBus.IModbus,System.String,System.Boolean)" />
		public static async Task<OperateResult> WriteAsync(IModbus modbus, string address, bool value)
		{
			OperateResult<string> modbusAddress = modbus.TranslateToModbusAddress(address, 5);
			if (!modbusAddress.IsSuccess)
			{
				return modbusAddress;
			}
			OperateResult<byte[]> command = ModbusInfo.BuildWriteBoolModbusCommand(modbusAddress.Content, value, modbus.Station, modbus.AddressStartWithZero, 5);
			if (!command.IsSuccess)
			{
				return command;
			}
			return await modbus.ReadFromCoreServerAsync(command.Content);
		}
	}
}

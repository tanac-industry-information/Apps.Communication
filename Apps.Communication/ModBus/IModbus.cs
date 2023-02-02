using Apps.Communication.Core;

namespace Apps.Communication.ModBus
{
	/// <summary>
	/// Modbus设备的接口，用来表示Modbus相关的设备对象，<see cref="T:Communication.ModBus.ModbusTcpNet" />, <see cref="T:Communication.ModBus.ModbusRtu" />,
	/// <see cref="T:Communication.ModBus.ModbusAscii" />,<see cref="T:Communication.ModBus.ModbusRtuOverTcp" />,<see cref="T:Communication.ModBus.ModbusUdpNet" />均实现了该接口信息<br />
	/// Modbus device interface, used to represent Modbus-related device objects, <see cref="T:Communication.ModBus.ModbusTcpNet" />, 
	/// <see cref="T:Communication.ModBus.ModbusRtu" />,<see cref="T:Communication.ModBus.ModbusAscii" />,<see cref="T:Communication.ModBus.ModbusRtuOverTcp" />,<see cref="T:Communication.ModBus.ModbusUdpNet" /> all implement the interface information
	/// </summary>
	public interface IModbus : IReadWriteDevice, IReadWriteNet
	{
		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.AddressStartWithZero" />
		bool AddressStartWithZero { get; set; }

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.Station" />
		byte Station { get; set; }

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.DataFormat" />
		DataFormat DataFormat { get; set; }

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.IsStringReverse" />
		bool IsStringReverse { get; set; }

		/// <summary>
		/// 将当前的地址信息转换成Modbus格式的地址，如果转换失败，返回失败的消息。默认不进行任何的转换。<br />
		/// Convert the current address information into a Modbus format address. If the conversion fails, a failure message will be returned. No conversion is performed by default.
		/// </summary>
		/// <param name="address">传入的地址</param>
		/// <param name="modbusCode">Modbus的功能码</param>
		/// <returns>转换之后Modbus的地址</returns>
		OperateResult<string> TranslateToModbusAddress(string address, byte modbusCode);
	}
}

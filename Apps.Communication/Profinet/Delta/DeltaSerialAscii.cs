using Apps.Communication.Core;
using Apps.Communication.ModBus;
using Apps.Communication.Profinet.Delta.Helper;

namespace Apps.Communication.Profinet.Delta
{
	/// <summary>
	/// 台达PLC的串口通讯类，基于Modbus-Ascii协议开发，按照台达的地址进行实现。<br />
	/// The serial communication class of Delta PLC is developed based on the Modbus-Ascii protocol and implemented according to Delta's address.
	/// </summary>
	/// <remarks>
	/// 适用于DVP-ES/EX/EC/SS型号，DVP-SA/SC/SX/EH型号以及AS300型号，地址参考API文档，同时地址可以携带站号信息，举例：[s=2;D100],[s=3;M100]，可以动态修改当前报文的站号信息。<br />
	/// Suitable for DVP-ES/EX/EC/SS models, DVP-SA/SC/SX/EH models and AS300 model, the address refers to the API document, and the address can carry station number information,
	/// for example: [s=2;D100],[s= 3;M100], you can dynamically modify the station number information of the current message.
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.Delta.DeltaSerial" path="example" />
	/// </example>
	public class DeltaSerialAscii : ModbusAscii, IDelta, IReadWriteDevice, IReadWriteNet
	{
		/// <inheritdoc cref="P:Communication.Profinet.Delta.IDelta.Series" />
		public DeltaSeries Series { get; set; } = DeltaSeries.Dvp;


		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaSerial.#ctor" />
		public DeltaSerialAscii()
		{
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaSerial.#ctor(System.Byte)" />
		public DeltaSerialAscii(byte station = 1)
			: base(station)
		{
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <inheritdoc />
		public override OperateResult<string> TranslateToModbusAddress(string address, byte modbusCode)
		{
			return DeltaHelper.TranslateToModbusAddress(this, address, modbusCode);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.ReadBool(System.String,System.UInt16)" />
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return DeltaHelper.ReadBool(this, base.ReadBool, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Write(System.String,System.Boolean[])" />
		public override OperateResult Write(string address, bool[] values)
		{
			return DeltaHelper.Write(this, base.Write, address, values);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Read(System.String,System.UInt16)" />
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return DeltaHelper.Read(this, base.Read, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Write(System.String,System.Byte[])" />
		public override OperateResult Write(string address, byte[] value)
		{
			return DeltaHelper.Write(this, base.Write, address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DeltaSerialAscii[{base.PortName}:{base.BaudRate}]";
		}
	}
}

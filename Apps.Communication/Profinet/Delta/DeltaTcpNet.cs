using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.ModBus;
using Apps.Communication.Profinet.Delta.Helper;

namespace Apps.Communication.Profinet.Delta
{
	/// <summary>
	/// 台达PLC的网口通讯类，基于Modbus-Rtu协议开发，按照台达的地址进行实现。<br />
	/// The tcp communication class of Delta PLC is developed based on the Modbus-Tcp protocol and implemented according to Delta's address.
	/// </summary>
	/// <remarks>
	/// 适用于DVP-ES/EX/EC/SS型号，DVP-SA/SC/SX/EH型号以及AS300型号，地址参考API文档，同时地址可以携带站号信息，举例：[s=2;D100],[s=3;M100]，可以动态修改当前报文的站号信息。<br />
	/// Suitable for DVP-ES/EX/EC/SS models, DVP-SA/SC/SX/EH models and AS300 model, the address refers to the API document, and the address can carry station number information,
	/// for example: [s=2;D100],[s= 3;M100], you can dynamically modify the station number information of the current message.
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.Delta.DeltaSerial" path="example" />
	/// </example>
	public class DeltaTcpNet : ModbusTcpNet, IDelta, IReadWriteDevice, IReadWriteNet
	{
		/// <inheritdoc cref="P:Communication.Profinet.Delta.IDelta.Series" />
		public DeltaSeries Series { get; set; } = DeltaSeries.Dvp;


		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaSerial.#ctor" />
		public DeltaTcpNet()
		{
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.#ctor(System.String,System.Int32,System.Byte)" />
		public DeltaTcpNet(string ipAddress, int port = 502, byte station = 1)
			: base(ipAddress, port, station)
		{
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <inheritdoc />
		public override OperateResult<string> TranslateToModbusAddress(string address, byte modbusCode)
		{
			return DeltaHelper.TranslateToModbusAddress(this, address, modbusCode);
		}

		/// <inheritdoc />
		/// <remarks>
		/// 地址支持X,Y,M,SM,S,T,C,HC，其中X和Y地址使用DD.DD格式，范围 X0.0~X63.15, Y0.0~Y63.15，其中X地址使用的是02功能码，其余的都是01功能码。<br />
		/// Address supports X, Y, M, SM, S, T, C, HC, where X and Y addresses use the DD.DD format, and the range is X0.0~X63.15, Y0.0~Y63.15, 
		/// where X addresses are used The one is 02 function code, and the rest are 01 function code.
		/// </remarks>
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return DeltaHelper.ReadBool(this, base.ReadBool, address, length);
		}

		/// <inheritdoc />
		/// <remarks>
		/// 地址支持Y,M,SM,S,T,C,HC，其中Y地址使用DD.DD格式，Y0.0~Y63.15，不支持X地址的写入。<br />
		/// The address supports Y, M, SM, S, T, C, HC, where the Y address uses the DD.DD format, Y0.0~Y63.15, the writing of the X address is not supported.
		/// </remarks>
		public override OperateResult Write(string address, bool[] values)
		{
			return DeltaHelper.Write(this, base.Write, address, values);
		}

		/// <inheritdoc />
		/// <remarks>
		/// 地址支持Y,M,SM,S,T,C,HC，其中Y地址使用DD.DD格式，Y0.0~Y63.15，不支持X地址的写入。<br />
		/// The address supports Y, M, SM, S, T, C, HC, where the Y address uses the DD.DD format, Y0.0~Y63.15, the writing of the X address is not supported.
		/// </remarks>
		public override OperateResult Write(string address, bool value)
		{
			return base.Write(address, value);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Read(System.String,System.UInt16)" />
		/// <remarks>
		/// 字地址支持X,Y,SR,D,T,C,HC,E, 所有的地址都是十进制的方式，地址范围参照API文档事例，举例：X1,Y10,SR100,D10,T20,C20,HC200,E2<br />
		/// Word address supports X, Y, SR, D, T, C, HC, E, all addresses are in decimal format, and the address range refers to the API document example, 
		/// for example: X1, Y10, SR100, D10, T20, C20, HC200 ,E2
		/// </remarks>
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return DeltaHelper.Read(this, base.Read, address, length);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Byte[])" />
		/// <remarks>
		/// 字地址支持Y,SR,D,T,C,HC,E, 所有的地址都是十进制的方式，地址范围参照API文档事例，举例：Y10,SR100,D10,T20,C20,HC200,E2<br />
		/// Word address supports Y, SR, D, T, C, HC, E, all addresses are in decimal format, and the address range refers to the API document example, 
		/// for example: Y10, SR100, D10, T20, C20, HC200 ,E2
		/// </remarks>
		public override OperateResult Write(string address, byte[] value)
		{
			return DeltaHelper.Write(this, base.Write, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await DeltaHelper.ReadBoolAsync(this, (string _address, ushort _length) => base.ReadBoolAsync(address, length), address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			return await DeltaHelper.WriteAsync(this, (string _address, bool[] _values) => base.WriteAsync(address, values), address, values);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await DeltaHelper.ReadAsync(this, (string _address, ushort _length) => base.ReadAsync(address, length), address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Delta.DeltaTcpNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await DeltaHelper.WriteAsync(this, (string _address, byte[] _value) => base.WriteAsync(address, value), address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DeltaTcpNet[{IpAddress}:{Port}]";
		}
	}
}

using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Profinet.LSIS.Helper;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.LSIS
{
	/// <summary>
	/// XGB Cnet I/F module supports Serial Port. The address can carry station number information, for example: s=2;D100
	/// </summary>
	/// <remarks>
	/// XGB 主机的通道 0 仅支持 1:1 通信。 对于具有主从格式的 1:N 系统，在连接 XGL-C41A 模块的通道 1 或 XGB 主机中使用 RS-485 通信。 XGL-C41A 模块支持 RS-422/485 协议。
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.LSIS.XGBCnetOverTcp" path="example" />
	/// </example>
	public class XGBCnet : SerialDeviceBase
	{
		/// <inheritdoc cref="P:Communication.Profinet.LSIS.XGBCnetOverTcp.Station" />
		public byte Station { get; set; } = 5;


		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGBCnet()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 2;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return XGBCnetHelper.UnpackResponseContent(send, response);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadByte(System.String)" />
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.Write(System.String,System.Byte)" />
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.ReadBool(Communication.Core.IReadWriteDevice,System.Int32,System.String)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			return XGBCnetHelper.ReadBool(this, Station, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadCoil(System.String)" />
		public OperateResult<bool> ReadCoil(string address)
		{
			return ReadBool(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadCoil(System.String,System.UInt16)" />
		public OperateResult<bool[]> ReadCoil(string address, ushort length)
		{
			return ReadBool(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.WriteCoil(System.String,System.Boolean)" />
		public OperateResult WriteCoil(string address, bool value)
		{
			return Write(address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Write(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return XGBCnetHelper.Write(this, Station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.ReadBool(System.String)" />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			return await XGBCnetHelper.ReadBoolAsync(this, Station, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.ReadCoil(System.String)" />
		public async Task<OperateResult<bool>> ReadCoilAsync(string address)
		{
			return await ReadBoolAsync(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.ReadCoil(System.String,System.UInt16)" />
		public async Task<OperateResult<bool[]>> ReadCoilAsync(string address, ushort length)
		{
			return await ReadBoolAsync(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.WriteCoil(System.String,System.Boolean)" />
		public async Task<OperateResult> WriteCoilAsync(string address, bool value)
		{
			return await WriteAsync(address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await XGBCnetHelper.WriteAsync(this, Station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Read(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return XGBCnetHelper.Read(this, Station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Read(Communication.Core.IReadWriteDevice,System.Int32,System.String[])" />
		public OperateResult<byte[]> Read(string[] address)
		{
			return XGBCnetHelper.Read(this, Station, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.Helper.XGBCnetHelper.Write(Communication.Core.IReadWriteDevice,System.Int32,System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return XGBCnetHelper.Write(this, Station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await XGBCnetHelper.ReadAsync(this, Station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.Read(System.String[])" />
		public async Task<OperateResult<byte[]>> ReadAsync(string[] address)
		{
			return await XGBCnetHelper.ReadAsync(this, Station, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await XGBCnetHelper.WriteAsync(this, Station, address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"XGBCnet[{base.PortName}:{base.BaudRate}]";
		}
	}
}

using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Profinet.Melsec.Helper;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 三菱的串口通信的对象，适用于读取FX系列的串口数据，支持的类型参考文档说明<br />
	/// Mitsubishi's serial communication object is suitable for reading serial data of the FX series. Refer to the documentation for the supported types.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.Melsec.MelsecFxSerialOverTcp" path="remarks" />
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecFxSerial.cs" region="Usage" title="简单的使用" />
	/// </example>
	public class MelsecFxSerial : SerialDeviceBase
	{
		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecFxSerialOverTcp.IsNewVersion" />
		public bool IsNewVersion { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public MelsecFxSerial()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
			IsNewVersion = true;
			base.ByteTransform.IsStringReverseByteWord = true;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxSerialOverTcp.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return MelsecFxSerialHelper.Read(this, address, length, IsNewVersion);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxSerialOverTcp.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return MelsecFxSerialHelper.Write(this, address, value, IsNewVersion);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxSerialOverTcp.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return MelsecFxSerialHelper.ReadBool(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxSerialOverTcp.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return MelsecFxSerialHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.ActivePlc(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi]
		public OperateResult ActivePlc()
		{
			return MelsecFxSerialHelper.ActivePlc(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecFxSerialHelper.ActivePlc(Communication.Core.IReadWriteDevice)" />
		public async Task<OperateResult> ActivePlcAsync()
		{
			return await MelsecFxSerialHelper.ActivePlcAsync(this);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await Task.Run(() => Write(address, value));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecFxSerial[{base.PortName}:{base.BaudRate}]";
		}
	}
}

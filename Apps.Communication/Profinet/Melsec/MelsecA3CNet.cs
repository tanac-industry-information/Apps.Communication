using Apps.Communication.Core;
using Apps.Communication.Profinet.Melsec.Helper;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 基于Qna 兼容3C帧的格式一的通讯，具体的地址需要参照三菱的基本地址<br />
	/// Based on Qna-compatible 3C frame format one communication, the specific address needs to refer to the basic address of Mitsubishi.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.Melsec.MelsecA3CNetOverTcp" path="remarks" />
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.Melsec.MelsecA3CNetOverTcp" path="example" />
	/// </example>
	public class MelsecA3CNet : SerialDeviceBase, IReadWriteA3C, IReadWriteDevice, IReadWriteNet
	{
		private byte station = 0;

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.Helper.IReadWriteA3C.Station" />
		public byte Station
		{
			get
			{
				return station;
			}
			set
			{
				station = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.Helper.IReadWriteA3C.SumCheck" />
		public bool SumCheck { get; set; } = true;


		/// <inheritdoc cref="P:Communication.Profinet.Melsec.Helper.IReadWriteA3C.Format" />
		public int Format { get; set; } = 1;


		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecA3CNetOverTcp.#ctor" />
		public MelsecA3CNet()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.Read(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return MelsecA3CNetHelper.Read(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.Write(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return MelsecA3CNetHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.ReadBool(Communication.Profinet.Melsec.Helper.IReadWriteA3C,System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return MelsecA3CNetHelper.ReadBool(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecA3CNetOverTcp.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return MelsecA3CNetHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.RemoteRun(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		[HslMqttApi]
		public OperateResult RemoteRun()
		{
			return MelsecA3CNetHelper.RemoteRun(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.RemoteStop(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		[HslMqttApi]
		public OperateResult RemoteStop()
		{
			return MelsecA3CNetHelper.RemoteStop(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.MelsecA3CNetHelper.ReadPlcType(Communication.Profinet.Melsec.Helper.IReadWriteA3C)" />
		[HslMqttApi]
		public OperateResult<string> ReadPlcType()
		{
			return MelsecA3CNetHelper.ReadPlcType(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecA3CNet[{base.PortName}:{base.BaudRate}]";
		}
	}
}

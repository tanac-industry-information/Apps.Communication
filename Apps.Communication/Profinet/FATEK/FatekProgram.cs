using Apps.Communication.Core;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.FATEK
{
	/// <summary>
	/// 台湾永宏公司的编程口协议，具体的地址信息请查阅api文档信息，地址允许携带站号信息，例如：s=2;D100<br />
	/// The programming port protocol of Taiwan Yonghong company, 
	/// please refer to the api document for specific address information, The address can carry station number information, such as s=2;D100
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.FATEK.FatekProgramOverTcp" path="remarks" />
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.FATEK.FatekProgramOverTcp" path="example" />
	/// </example>
	public class FatekProgram : SerialDeviceBase
	{
		private byte station = 1;

		/// <inheritdoc cref="P:Communication.Profinet.FATEK.FatekProgramOverTcp.Station" />
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

		/// <inheritdoc cref="M:Communication.Profinet.FATEK.FatekProgramOverTcp.#ctor" />
		public FatekProgram()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <inheritdoc cref="M:Communication.Profinet.FATEK.FatekProgramHelper.Read(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return FatekProgramHelper.Read(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.FATEK.FatekProgramHelper.Write(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return FatekProgramHelper.Write(this, station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.FATEK.FatekProgramHelper.ReadBool(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return FatekProgramHelper.ReadBool(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.FATEK.FatekProgramHelper.Write(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return FatekProgramHelper.Write(this, station, address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FatekProgram[{base.PortName}:{base.BaudRate}]";
		}
	}
}

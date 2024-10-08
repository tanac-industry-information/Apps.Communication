using Apps.Communication.Core.Address;
using Apps.Communication.Profinet.Melsec;

namespace Apps.Communication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士PLC的数据通信类，使用QnA兼容3E帧的通信协议实现，使用ASCII的格式，地址格式需要进行转换成三菱的格式，详细参照备注说明<br />
	/// Keyence PLC's data communication class is implemented using QnA compatible 3E frame communication protocol. 
	/// It uses ascii format. The address format needs to be converted to Mitsubishi format.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.Keyence.KeyenceMcNet" path="remarks" />
	/// </remarks>
	public class KeyenceMcAsciiNet : MelsecMcAsciiNet
	{
		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceMcNet.#ctor" />
		public KeyenceMcAsciiNet()
		{
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceMcNet.#ctor(System.String,System.Int32)" />
		public KeyenceMcAsciiNet(string ipAddress, int port)
			: base(ipAddress, port)
		{
		}

		/// <inheritdoc />
		public override OperateResult<McAddressData> McAnalysisAddress(string address, ushort length)
		{
			return McAddressData.ParseKeyenceFrom(address, length);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"KeyenceMcAsciiNet[{IpAddress}:{Port}]";
		}
	}
}

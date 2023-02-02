using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Melsec.Helper;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯类，采用UDP的协议实现，采用Qna兼容3E帧协议实现，需要在PLC侧先的以太网模块先进行配置，必须为ascii通讯<br />
	/// Mitsubishi PLC communication class is implemented using UDP protocol and Qna compatible 3E frame protocol. 
	/// The Ethernet module needs to be configured first on the PLC side, and it must be ascii communication.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.Melsec.MelsecMcNet" path="remarks" />
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecAscii.cs" region="Usage" title="简单的短连接使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\MelsecAscii.cs" region="Usage2" title="简单的长连接使用" />
	/// </example>
	public class MelsecMcAsciiUdp : NetworkUdpDeviceBase, IReadWriteMc, IReadWriteDevice, IReadWriteNet
	{
		/// <inheritdoc cref="P:Communication.Profinet.Melsec.Helper.IReadWriteMc.McType" />
		public McType McType => McType.MCAscii;

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecMcNet.NetworkNumber" />
		public byte NetworkNumber { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecMcNet.NetworkStationNumber" />
		public byte NetworkStationNumber { get; set; } = 0;


		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcNet.#ctor" />
		public MelsecMcAsciiUdp()
		{
			base.WordLength = 1;
			LogMsgFormatBinary = false;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcNet.#ctor(System.String,System.Int32)" />
		public MelsecMcAsciiUdp(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcNet.McAnalysisAddress(System.String,System.UInt16)" />
		public virtual OperateResult<McAddressData> McAnalysisAddress(string address, ushort length)
		{
			return McAddressData.ParseMelsecFrom(address, length);
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return McAsciiHelper.PackMcCommand(command, NetworkNumber, NetworkStationNumber);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			OperateResult operateResult = McAsciiHelper.CheckResponseContent(response);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(response.RemoveBegin(22));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.IReadWriteMc.ExtractActualData(System.Byte[],System.Boolean)" />
		public byte[] ExtractActualData(byte[] response, bool isBit)
		{
			return McAsciiHelper.ExtractActualDataHelper(response, isBit);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.Read(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return McHelper.Read(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return McHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcNet.ReadRandom(System.String[])" />
		[HslMqttApi("随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据")]
		public OperateResult<byte[]> ReadRandom(string[] address)
		{
			return McHelper.ReadRandom(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcNet.ReadRandom(System.String[],System.UInt16[])" />
		[HslMqttApi(ApiTopic = "ReadRandoms", Description = "随机读取PLC的数据信息，可以跨地址，跨类型组合，每个地址是任意的长度。收到结果后，需要自行解析数据，目前只支持字地址，比如D区，W区，R区，不支持X，Y，M，B，L等等")]
		public OperateResult<byte[]> ReadRandom(string[] address, ushort[] length)
		{
			return McHelper.ReadRandom(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandomInt16(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[])" />
		public OperateResult<short[]> ReadRandomInt16(string[] address)
		{
			return McHelper.ReadRandomInt16(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandomUInt16(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[])" />
		public OperateResult<ushort[]> ReadRandomUInt16(string[] address)
		{
			return McHelper.ReadRandomUInt16(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadBool(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String)" />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			return base.ReadBool(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadBool(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return McHelper.ReadBool(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			return McHelper.Write(this, address, values);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadExtend(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.UInt16,System.String,System.UInt16)" />
		[HslMqttApi(ApiTopic = "ReadExtend", Description = "读取扩展的数据信息，需要在原有的地址，长度信息之外，输入扩展值信息")]
		public OperateResult<byte[]> ReadExtend(ushort extend, string address, ushort length)
		{
			return McHelper.ReadExtend(this, extend, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadMemory(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String,System.UInt16)" />
		[HslMqttApi(ApiTopic = "ReadMemory", Description = "读取缓冲寄存器的数据信息，地址直接为偏移地址")]
		public OperateResult<byte[]> ReadMemory(string address, ushort length)
		{
			return McHelper.ReadMemory(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadSmartModule(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.UInt16,System.String,System.UInt16)" />
		[HslMqttApi(ApiTopic = "ReadSmartModule", Description = "读取智能模块的数据信息，需要指定模块地址，偏移地址，读取的字节长度")]
		public OperateResult<byte[]> ReadSmartModule(ushort module, string address, ushort length)
		{
			return McHelper.ReadSmartModule(this, module, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.RemoteRun(Communication.Profinet.Melsec.Helper.IReadWriteMc)" />
		[HslMqttApi(ApiTopic = "RemoteRun", Description = "远程Run操作")]
		public OperateResult RemoteRun()
		{
			return McHelper.RemoteRun(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.RemoteStop(Communication.Profinet.Melsec.Helper.IReadWriteMc)" />
		[HslMqttApi(ApiTopic = "RemoteStop", Description = "远程Stop操作")]
		public OperateResult RemoteStop()
		{
			return McHelper.RemoteStop(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.RemoteReset(Communication.Profinet.Melsec.Helper.IReadWriteMc)" />
		[HslMqttApi(ApiTopic = "RemoteReset", Description = "LED 熄灭 出错代码初始化")]
		public OperateResult RemoteReset()
		{
			return McHelper.RemoteReset(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadPlcType(Communication.Profinet.Melsec.Helper.IReadWriteMc)" />
		[HslMqttApi(ApiTopic = "ReadPlcType", Description = "读取PLC的型号信息，例如 Q02HCPU")]
		public OperateResult<string> ReadPlcType()
		{
			return McHelper.ReadPlcType(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ErrorStateReset(Communication.Profinet.Melsec.Helper.IReadWriteMc)" />
		[HslMqttApi(ApiTopic = "ErrorStateReset", Description = "LED 熄灭 出错代码初始化")]
		public OperateResult ErrorStateReset()
		{
			return McHelper.ErrorStateReset(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecMcAsciiUdp[{IpAddress}:{Port}]";
		}
	}
}

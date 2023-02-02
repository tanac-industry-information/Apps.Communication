using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士KV上位链路协议的通信对象,适用于KV5000/5500/3000,KV1000,KV700,以及L20V通信模块，本类是基于tcp通信<br />
	/// The communication object of KEYENCE KV upper link protocol is suitable for KV5000/5500/3000, KV1000, KV700, and L20V communication modules. This type is based on tcp communication
	/// </summary>
	/// <remarks>
	/// 位读写的数据类型为 R,B,MR,LR,CR,VB,以及读定时器的计数器的触点，字读写的数据类型为 DM,EM,FM,ZF,W,TM,Z,AT,CM,VM 双字读写为T,C,TC,CC,TS,CS。如果想要读写扩展的缓存器，地址示例：unit=2;1000  前面的是单元编号，后面的是偏移地址<br />
	/// 注意：在端口 2 以多分支连接 KV-L21V 时，请一定加上站号。在将端口 2 设定为使用 RS-422A、 RS-485 时， KV-L21V 即使接收本站以外的带站号的指令，也将变为无应答，不返回响应消息。
	/// </remarks>
	/// <example>
	/// 地址示例如下：
	/// 当读取Bool的输入的格式说明如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址范围</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>继电器</term>
	///     <term>R</term>
	///     <term>R0,R100</term>
	///     <term>0-59915</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链路继电器</term>
	///     <term>B</term>
	///     <term>B0,B100</term>
	///     <term>0-3FFF</term>
	///     <term>KV5500/KV5000/KV3000</term>
	///   </item>
	///   <item>
	///     <term>控制继电器</term>
	///     <term>CR</term>
	///     <term>CR0,CR100</term>
	///     <term>0-3915</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>内部辅助继电器</term>
	///     <term>MR</term>
	///     <term>MR0,MR100</term>
	///     <term>0-99915</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>锁存继电器</term>
	///     <term>LR</term>
	///     <term>LR0,LR100</term>
	///     <term>0-99915</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>工作继电器</term>
	///     <term>VB</term>
	///     <term>VB0,VB100</term>
	///     <term>0-3FFF</term>
	///     <term>KV5500/KV5000/KV3000</term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0,T100</term>
	///     <term>0-3999</term>
	///     <term>通断</term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0,C100</term>
	///     <term>0-3999</term>
	///     <term>通断</term>
	///   </item>
	///   <item>
	///     <term>高速计数器</term>
	///     <term>CTH</term>
	///     <term>CTH0,CTH1</term>
	///     <term>0-1</term>
	///     <term>通断</term>
	///   </item>
	///   <item>
	///     <term>高速计数器比较器</term>
	///     <term>CTC</term>
	///     <term>CTC0,CTC1</term>
	///     <term>0-1</term>
	///     <term>通断</term>
	///   </item>
	/// </list>
	/// 读取数据的地址如下：
	///
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址范围</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据存储器</term>
	///     <term>DM</term>
	///     <term>DM0,DM100</term>
	///     <term>0-65534</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>控制存储器</term>
	///     <term>CM</term>
	///     <term>CM0,CM100</term>
	///     <term>0-11998</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>临时数据存储器</term>
	///     <term>TM</term>
	///     <term>TM0,TM100</term>
	///     <term>0-511</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>扩展数据存储器</term>
	///     <term>EM</term>
	///     <term>EM0,EM100</term>
	///     <term>0-65534</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>扩展数据存储器</term>
	///     <term>FM</term>
	///     <term>FM0,FM100</term>
	///     <term>0-32766</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>变址寄存器</term>
	///     <term>Z</term>
	///     <term>Z1,Z5</term>
	///     <term>1-12</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数字微调器</term>
	///     <term>AT</term>
	///     <term>AT0,AT5</term>
	///     <term>0-7</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链路寄存器</term>
	///     <term>W</term>
	///     <term>W1,W5</term>
	///     <term>0-3FFF</term>
	///     <term>KV5500/KV5000/KV3000</term>
	///   </item>
	///   <item>
	///     <term>工作寄存器</term>
	///     <term>VM</term>
	///     <term>VM1,VM5</term>
	///     <term>0-59999</term>
	///     <term>KV5500/KV5000/KV3000</term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0,T100</term>
	///     <term>0-3999</term>
	///     <term>当前值(current value), 读int</term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0,C100</term>
	///     <term>0-3999</term>
	///     <term>当前值(current value), 读int</term>
	///   </item>
	/// </list>
	/// </example>
	public class KeyenceNanoSerialOverTcp : NetworkDeviceBase
	{
		/// <summary>
		/// 获取或设置当前的站号信息，在RS232连接模式下，设置为0，如果是RS485/RS422连接下，必须设置正确的站号<br />
		/// Get or set the current station number information. In RS232 connection mode, set it to 0. 
		/// If it is RS485/RS422 connection, you must set the correct station number.
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 获取或设置当前是否启用站号信息，当不启动站号时，在连接和断开的时候，将不使用站号报文。<br />
		/// Get or set whether the station number information is currently enabled or not. 
		/// When the station number is not activated, the station number message will not be used when connecting and disconnecting.
		/// </summary>
		public bool UseStation { get; set; }

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public KeyenceNanoSerialOverTcp()
		{
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
			base.ByteTransform.IsStringReverseByteWord = true;
		}

		/// <summary>
		/// 使用指定的ip地址和端口号来初始化对象<br />
		/// Initialize the object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">Ip地址数据</param>
		/// <param name="port">端口号</param>
		public KeyenceNanoSerialOverTcp(string ipAddress, int port = 8501)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, KeyenceNanoHelper.GetConnectCmd(Station, UseStation));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override OperateResult ExtraOnDisconnect(Socket socket)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, KeyenceNanoHelper.GetDisConnectCmd(Station, UseStation));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override OperateResult<byte[]> ReadFromCoreServer(Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true)
		{
			base.LogNet?.WriteDebug(ToString(), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? send.ToHexString(' ') : SoftBasic.GetAsciiStringRender(send)));
			OperateResult operateResult = Send(socket, usePackHeader ? PackCommandWithHeader(send) : send);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (base.ReceiveTimeOut < 0)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (!hasResponseData)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (base.SleepTime > 0)
			{
				Thread.Sleep(base.SleepTime);
			}
			OperateResult<byte[]> operateResult2 = ReceiveCommandLineFromSocket(socket, 13, 10, base.ReceiveTimeOut);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			base.LogNet?.WriteDebug(ToString(), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? operateResult2.Content.ToHexString(' ') : SoftBasic.GetAsciiStringRender(operateResult2.Content)));
			return OperateResult.CreateSuccessResult(operateResult2.Content);
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			OperateResult<byte[]> result = await ReadFromCoreServerAsync(socket, KeyenceNanoHelper.GetConnectCmd(Station, UseStation));
			if (!result.IsSuccess)
			{
				return result;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> ExtraOnDisconnectAsync(Socket socket)
		{
			OperateResult<byte[]> result = await ReadFromCoreServerAsync(socket, KeyenceNanoHelper.GetDisConnectCmd(Station, UseStation));
			if (!result.IsSuccess)
			{
				return result;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override async Task<OperateResult<byte[]>> ReadFromCoreServerAsync(Socket socket, byte[] send, bool hasResponseData = true, bool usePackHeader = true)
		{
			byte[] sendValue = (usePackHeader ? PackCommandWithHeader(send) : send);
			base.LogNet?.WriteDebug(ToString(), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? sendValue.ToHexString(' ') : SoftBasic.GetAsciiStringRender(sendValue)));
			OperateResult sendResult = Send(socket, sendValue);
			if (!sendResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(sendResult);
			}
			if (base.ReceiveTimeOut < 0)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (!hasResponseData)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (base.SleepTime > 0)
			{
				Thread.Sleep(base.SleepTime);
			}
			OperateResult<byte[]> resultReceive = await ReceiveCommandLineFromSocketAsync(socket, 13, 10, base.ReceiveTimeOut);
			if (!resultReceive.IsSuccess)
			{
				return resultReceive;
			}
			base.LogNet?.WriteDebug(ToString(), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? resultReceive.Content.ToHexString(' ') : SoftBasic.GetAsciiStringRender(resultReceive.Content)));
			return UnpackResponseContent(sendValue, resultReceive.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return KeyenceNanoHelper.Read(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return KeyenceNanoHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await KeyenceNanoHelper.ReadAsync(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await KeyenceNanoHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return KeyenceNanoHelper.ReadBool(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return KeyenceNanoHelper.Write(this, address, value);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return KeyenceNanoHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await KeyenceNanoHelper.ReadBoolAsync(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await KeyenceNanoHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] value)
		{
			return await KeyenceNanoHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.ReadPlcType(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi("查询PLC的型号信息")]
		public OperateResult<KeyencePLCS> ReadPlcType()
		{
			return KeyenceNanoHelper.ReadPlcType(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.ReadPlcMode(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi("读取当前PLC的模式，如果是0，代表 PROG模式或者梯形图未登录，如果为1，代表RUN模式")]
		public OperateResult<int> ReadPlcMode()
		{
			return KeyenceNanoHelper.ReadPlcMode(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.SetPlcDateTime(Communication.Core.IReadWriteDevice,System.DateTime)" />
		[HslMqttApi("设置PLC的时间")]
		public OperateResult SetPlcDateTime(DateTime dateTime)
		{
			return KeyenceNanoHelper.SetPlcDateTime(this, dateTime);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.ReadAddressAnnotation(Communication.Core.IReadWriteDevice,System.String)" />
		[HslMqttApi("读取指定软元件的注释信息")]
		public OperateResult<string> ReadAddressAnnotation(string address)
		{
			return KeyenceNanoHelper.ReadAddressAnnotation(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.ReadExpansionMemory(Communication.Core.IReadWriteDevice,System.Byte,System.UInt16,System.UInt16)" />
		[HslMqttApi("从扩展单元缓冲存储器连续读取指定个数的数据，单位为字")]
		public OperateResult<byte[]> ReadExpansionMemory(byte unit, ushort address, ushort length)
		{
			return KeyenceNanoHelper.ReadExpansionMemory(this, unit, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoHelper.WriteExpansionMemory(Communication.Core.IReadWriteDevice,System.Byte,System.UInt16,System.Byte[])" />
		[HslMqttApi("将原始字节数据写入到扩展的缓冲存储器，需要指定单元编号，偏移地址，写入的数据")]
		public OperateResult WriteExpansionMemory(byte unit, ushort address, byte[] value)
		{
			return KeyenceNanoHelper.WriteExpansionMemory(this, unit, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.ReadPlcType" />
		public async Task<OperateResult<KeyencePLCS>> ReadPlcTypeAsync()
		{
			return await KeyenceNanoHelper.ReadPlcTypeAsync(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.ReadPlcMode" />
		public async Task<OperateResult<int>> ReadPlcModeAsync()
		{
			return await KeyenceNanoHelper.ReadPlcModeAsync(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.SetPlcDateTime(System.DateTime)" />
		public async Task<OperateResult> SetPlcDateTimeAsync(DateTime dateTime)
		{
			return await KeyenceNanoHelper.SetPlcDateTimeAsync(this, dateTime);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.ReadAddressAnnotation(System.String)" />
		public async Task<OperateResult<string>> ReadAddressAnnotationAsync(string address)
		{
			return await KeyenceNanoHelper.ReadAddressAnnotationAsync(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.ReadExpansionMemory(System.Byte,System.UInt16,System.UInt16)" />
		public async Task<OperateResult<byte[]>> ReadExpansionMemoryAsync(byte unit, ushort address, ushort length)
		{
			return await KeyenceNanoHelper.ReadExpansionMemoryAsync(this, unit, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.WriteExpansionMemory(System.Byte,System.UInt16,System.Byte[])" />
		public async Task<OperateResult> WriteExpansionMemoryAsync(byte unit, ushort address, byte[] value)
		{
			return await KeyenceNanoHelper.WriteExpansionMemoryAsync(this, unit, address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"KeyenceNanoSerialOverTcp[{IpAddress}:{Port}]";
		}
	}
}

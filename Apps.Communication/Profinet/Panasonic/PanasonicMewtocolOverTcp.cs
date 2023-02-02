using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Panasonic
{
	/// <summary>
	/// 松下PLC的数据交互协议，采用Mewtocol协议通讯，基于Tcp透传实现的机制，支持的地址列表参考api文档<br />
	/// The data exchange protocol of Panasonic PLC adopts Mewtocol protocol for communication. 
	/// It is based on the mechanism of Tcp transparent transmission. For the list of supported addresses, refer to the api document.
	/// </summary>
	/// <remarks>
	/// 地址支持携带站号的访问方式，例如：s=2;D100
	/// </remarks>
	/// <example>
	/// 触点地址的输入的格式说明如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>外部输入继电器</term>
	///     <term>X</term>
	///     <term>X11,X1F</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>X33 等同于 X3.3</term>
	///   </item>
	///   <item>
	///     <term>外部输出继电器</term>
	///     <term>Y</term>
	///     <term>Y22,Y2A</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>Y21 等同于 Y2.1</term>
	///   </item>
	///   <item>
	///     <term>内部继电器</term>
	///     <term>R</term>
	///     <term>R0F,R100F</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>R21 等同于 R2.1</term>
	///   </item>
	///   <item>
	///     <term>定时器</term>
	///     <term>T</term>
	///     <term>T0,T100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器</term>
	///     <term>C</term>
	///     <term>C0,C100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接继电器</term>
	///     <term>L</term>
	///     <term>L0F,L100F</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term>L21 等同于 L2.1</term>
	///   </item>
	/// </list>
	/// 数据地址的输入的格式说明如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>数据寄存器 DT</term>
	///     <term>D</term>
	///     <term>D0,D100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接寄存器 LD</term>
	///     <term>LD</term>
	///     <term>LD0,LD100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>文件寄存器 FL</term>
	///     <term>F</term>
	///     <term>F0,F100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>目标值 SV</term>
	///     <term>S</term>
	///     <term>S0,S100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>经过值 EV</term>
	///     <term>K</term>
	///     <term>K0,K100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>索引寄存器 IX</term>
	///     <term>IX</term>
	///     <term>IX0,IX100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>索引寄存器 IY</term>
	///     <term>IY</term>
	///     <term>IY0,IY100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class PanasonicMewtocolOverTcp : NetworkDeviceBase
	{
		/// <summary>
		/// PLC设备的目标站号，需要根据实际的设置来填写<br />
		/// The target station number of the PLC device needs to be filled in according to the actual settings
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 实例化一个默认的松下PLC通信对象，默认站号为0xEE<br />
		/// Instantiate a default Panasonic PLC communication object, the default station number is 0xEE
		/// </summary>
		/// <param name="station">站号信息，默认为0xEE</param>
		public PanasonicMewtocolOverTcp(byte station = 238)
		{
			base.ByteTransform = new RegularByteTransform();
			Station = station;
			base.ByteTransform.DataFormat = DataFormat.DCBA;
			base.SleepTime = 20;
		}

		/// <summary>
		/// 实例化一个默认的松下PLC通信对象，指定ip地址，端口，默认站号为0xEE<br />
		/// Instantiate a default Panasonic PLC communication object, specify the IP address, port, and the default station number is 0xEE
		/// </summary>
		/// <param name="ipAddress">Ip地址数据</param>
		/// <param name="port">端口号</param>
		/// <param name="station">站号信息，默认为0xEE</param>
		public PanasonicMewtocolOverTcp(string ipAddress, int port, byte station = 238)
			: this(238)
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <summary>
		/// 读取指定地址的原始数据，地址示例：D0  F0  K0  T0  C0, 地址支持携带站号的访问方式，例如：s=2;D100<br />
		/// Read the original data of the specified address, address example: D0 F0 K0 T0 C0, the address supports carrying station number information, for example: s=2;D100
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">长度</param>
		/// <returns>原始的字节数据的信息</returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <summary>
		/// 将数据写入到指定的地址里去，地址示例：D0  F0  K0  T0  C0, 地址支持携带站号的访问方式，例如：s=2;D100<br />
		/// Write data to the specified address, address example: D0 F0 K0 T0 C0, the address supports carrying station number information, for example: s=2;D100
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">真实数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildWriteCommand(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return PanasonicHelper.ExtraActualData(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteCommand(station, address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return PanasonicHelper.ExtraActualData(read.Content);
		}

		/// <summary>
		/// 批量读取松下PLC的位数据，按照字为单位，地址为 X0,X10,Y10，读取的长度为16的倍数<br />
		/// Read the bit data of Panasonic PLC in batches, the unit is word, the address is X0, X10, Y10, and the read length is a multiple of 16
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>读取结果对象</returns>
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> operateResult = PanasonicHelper.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			OperateResult<byte[]> operateResult4 = PanasonicHelper.ExtraActualData(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult4.Content).SelectMiddle(operateResult.Content2 % 16, length));
		}

		/// <summary>
		/// 读取单个的地址信息的bool值，地址举例：SR0.0  X0.0  Y0.0  R0.0  L0.0<br />
		/// Read the bool value of a single address, for example: SR0.0 X0.0 Y0.0 R0.0 L0.0
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <returns>读取结果对象</returns>
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildReadOneCoil(station, address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult2);
			}
			return PanasonicHelper.ExtraActualBool(operateResult2.Content);
		}

		/// <summary>
		/// 往指定的地址写入 <see cref="T:System.Boolean" /> 数组，地址举例：SR0.0  X0.0  Y0.0  R0.0  L0.0，
		/// 起始的位地址必须为16的倍数，写入的 <see cref="T:System.Boolean" /> 数组长度也为16的倍数。<br />
		/// Write the <see cref="T:System.Boolean" /> array to the specified address, address example: SR0.0 X0.0 Y0.0 R0.0 L0.0, 
		/// the starting bit address must be a multiple of 16. <see cref="T:System.Boolean" /> The length of the array is also a multiple of 16. <br />
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="values">数据值信息</param>
		/// <returns>返回是否成功的结果对象</returns>
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> operateResult = PanasonicHelper.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content2 % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicAddressBitStartMulti16);
			}
			if (values.Length % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicBoolLengthMulti16);
			}
			byte[] values2 = SoftBasic.BoolArrayToByte(values);
			OperateResult<byte[]> operateResult2 = PanasonicHelper.BuildWriteCommand(station, address, values2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return PanasonicHelper.ExtraActualData(operateResult3.Content);
		}

		/// <summary>
		/// 往指定的地址写入bool数据，地址举例：SR0.0  X0.0  Y0.0  R0.0  L0.0<br />
		/// Write bool data to the specified address. Example address: SR0.0 X0.0 Y0.0 R0.0 L0.0
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">数据值信息</param>
		/// <returns>返回是否成功的结果对象</returns>
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildWriteOneCoil(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.ReadBool(System.String)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> analysis = PanasonicHelper.AnalysisAddress(address);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(analysis);
			}
			OperateResult<byte[]> command = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			OperateResult<byte[]> extra = PanasonicHelper.ExtraActualData(read.Content);
			if (!extra.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(extra);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extra.Content).SelectMiddle(analysis.Content2 % 16, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.ReadBool(System.String)" />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = PanasonicHelper.BuildReadOneCoil(station, address);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			return PanasonicHelper.ExtraActualBool(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> analysis = PanasonicHelper.AnalysisAddress(address);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(analysis);
			}
			if (analysis.Content2 % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicAddressBitStartMulti16);
			}
			if (values.Length % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicBoolLengthMulti16);
			}
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteCommand(values: SoftBasic.BoolArrayToByte(values), station: station, address: address);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return PanasonicHelper.ExtraActualData(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = PanasonicHelper.BuildWriteOneCoil(station, address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return PanasonicHelper.ExtraActualData(read.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"PanasonicMewtocolOverTcp[{IpAddress}:{Port}]";
		}
	}
}

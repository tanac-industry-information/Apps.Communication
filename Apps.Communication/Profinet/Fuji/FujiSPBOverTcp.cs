using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Fuji
{
	/// <summary>
	/// 富士PLC的SPB协议，详细的地址信息见api文档说明，地址可以携带站号信息，例如：s=2;D100，PLC侧需要配置无BCC计算，包含0D0A结束码<br />
	/// Fuji PLC's SPB protocol. For detailed address information, see the api documentation, 
	/// The address can carry station number information, for example: s=2;D100, PLC side needs to be configured with no BCC calculation, including 0D0A end code
	/// </summary>
	/// <remarks>
	/// 其所支持的地址形式如下：
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
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读写字单位的时候，M2代表位的M32</term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X10,X20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读取字单位的时候，X2代表位的X32</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读写字单位的时候，Y2代表位的Y32</term>
	///   </item>
	///   <item>
	///     <term>锁存继电器</term>
	///     <term>L</term>
	///     <term>L100,L200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的线圈</term>
	///     <term>TC</term>
	///     <term>TC100,TC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器的当前值</term>
	///     <term>TN</term>
	///     <term>TN100,TN200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的线圈</term>
	///     <term>CC</term>
	///     <term>CC100,CC200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器的当前</term>
	///     <term>CN</term>
	///     <term>CN100,CN200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D1000,D2000</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读位的时候，D10.15代表第10个字的第15位</term>
	///   </item>
	///   <item>
	///     <term>文件寄存器</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读位的时候，R10.15代表第10个字的第15位</term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>读位的时候，W10.15代表第10个字的第15位</term>
	///   </item>
	/// </list>
	/// </remarks>
	public class FujiSPBOverTcp : NetworkDeviceBase
	{
		private byte station = 1;

		/// <summary>
		/// PLC的站号信息<br />
		/// PLC station number information
		/// </summary>
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

		/// <summary>
		/// 使用默认的构造方法实例化对象<br />
		/// Instantiate the object using the default constructor
		/// </summary>
		public FujiSPBOverTcp()
		{
			base.WordLength = 1;
			LogMsgFormatBinary = false;
			base.ByteTransform = new RegularByteTransform();
			base.SleepTime = 20;
		}

		/// <summary>
		/// 使用指定的ip地址和端口来实例化一个对象<br />
		/// Instantiate an object with the specified IP address and port
		/// </summary>
		/// <param name="ipAddress">设备的Ip地址</param>
		/// <param name="port">设备的端口号</param>
		public FujiSPBOverTcp(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new FujiSPBMessage();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBHelper.Read(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return FujiSPBHelper.Read(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBHelper.Write(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return FujiSPBHelper.Write(this, station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBHelper.ReadBool(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return FujiSPBHelper.ReadBool(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBHelper.Write(Communication.Core.IReadWriteDevice,System.Byte,System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return FujiSPBHelper.Write(this, station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await FujiSPBHelper.ReadAsync(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await FujiSPBHelper.WriteAsync(this, station, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await FujiSPBHelper.ReadBoolAsync(this, station, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPBOverTcp.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await FujiSPBHelper.WriteAsync(this, station, address, value);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FujiSPBOverTcp[{IpAddress}:{Port}]";
		}
	}
}

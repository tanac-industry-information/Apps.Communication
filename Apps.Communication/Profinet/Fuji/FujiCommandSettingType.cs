using System;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Fuji
{
	/// <summary>
	/// 基于Command-Setting-Type通信模式的协议实现，地址格式参数API文档
	/// </summary>
	/// <remarks>
	/// 本类实例化对象之后，还需要设置<see cref="P:Communication.Profinet.Fuji.FujiCommandSettingType.DataSwap" />属性，根据实际情况来设置。
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
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
	///     <term>I/O Relay</term>
	///     <term>B</term>
	///     <term>B100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Auxiliary Relay</term>
	///     <term>M</term>
	///     <term>M100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Keep Relay</term>
	///     <term>K</term>
	///     <term>K100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Special Relay</term>
	///     <term>F</term>
	///     <term>F100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Announce Relay</term>
	///     <term>A</term>
	///     <term>A100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Differential Relay</term>
	///     <term>D</term>
	///     <term>D100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits</term>
	///   </item>
	///   <item>
	///     <term>Step Relay</term>
	///     <term>S</term>
	///     <term>S100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>8 bits</term>
	///   </item>
	///   <item>
	///     <term>Current value of 0.1-sec timer</term>
	///     <term>W9</term>
	///     <term>W9.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits</term>
	///   </item>
	///   <item>
	///     <term>Set value of timer</term>
	///     <term>TS</term>
	///     <term>TS100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits: 定时设置值</term>
	///   </item>
	///   <item>
	///     <term>Current value of timer</term>
	///     <term>TR</term>
	///     <term>TR100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits: 定时当前值</term>
	///   </item>
	///   <item>
	///     <term>set value of counter</term>
	///     <term>CS</term>
	///     <term>CS100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits: 计数器设定值</term>
	///   </item>
	///   <item>
	///     <term>Current value of counter</term>
	///     <term>CR</term>
	///     <term>CR100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits: 计数器当前值</term>
	///   </item>
	///   <item>
	///     <term>Data Memory(BCD)</term>
	///     <term>BD</term>
	///     <term>BD100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits:</term>
	///   </item>
	///   <item>
	///     <term>No.1 block</term>
	///     <term>WL</term>
	///     <term>WL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>No.2 block</term>
	///     <term>W21</term>
	///     <term>W21.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>No.3 block</term>
	///     <term>W22</term>
	///     <term>W22.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>No.4 block</term>
	///     <term>W23</term>
	///     <term>W23.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>Direct I/O</term>
	///     <term>W24</term>
	///     <term>W24.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>Analog work area</term>
	///     <term>W25</term>
	///     <term>W25.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits:</term>
	///   </item>
	///   <item>
	///     <term>Last value differential relay</term>
	///     <term>W26</term>
	///     <term>W26.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>File memory</term>
	///     <term>W30-W109</term>
	///     <term>W30.100, W109.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>32 bits or 16 bis depends by plc</term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>W120-W123</term>
	///     <term>W120.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	///   <item>
	///     <term>Calendar</term>
	///     <term>W125</term>
	///     <term>W125.100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term>16 bits:</term>
	///   </item>
	/// </list>
	/// </example>
	public class FujiCommandSettingType : NetworkDeviceBase
	{
		private bool dataSwap = false;

		/// <summary>
		/// 获取或设置当前的对象是否进行数据交换操作，将根据PLC的实际值来设定。<br />
		/// Get or set whether the current object performs data exchange operation or not, it will be set according to the actual value of the PLC.
		/// </summary>
		public bool DataSwap
		{
			get
			{
				return dataSwap;
			}
			set
			{
				dataSwap = value;
				if (value)
				{
					base.ByteTransform = new RegularByteTransform();
				}
				else
				{
					base.ByteTransform = new ReverseBytesTransform();
				}
			}
		}

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public FujiCommandSettingType()
		{
			base.ByteTransform = new ReverseBytesTransform();
			base.WordLength = 2;
		}

		/// <summary>
		/// 使用指定的IP地址和端口号来实例化一个对象
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号信息</param>
		public FujiCommandSettingType(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new FujiCommandSettingTypeMessage();
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return UnpackResponseContentHelper(send, response);
		}

		/// <summary>
		/// 从PLC读取原始的字节报文数据
		/// </summary>
		/// <param name="address">PLC地址信息</param>
		/// <param name="length">长度信息</param>
		/// <returns>结果报文数据</returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ReadFromCoreServer(operateResult.Content);
		}

		/// <summary>
		/// 写入原始数据到PLC指定的地址里，返回是否写入成功
		/// </summary>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="value">原始数据内容</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteCommand(address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ReadFromCoreServer(operateResult.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiCommandSettingType.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[]> bulid = BuildReadCommand(address, length);
			if (!bulid.IsSuccess)
			{
				return bulid;
			}
			return await ReadFromCoreServerAsync(bulid.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiCommandSettingType.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> bulid = BuildWriteCommand(address, value);
			if (!bulid.IsSuccess)
			{
				return bulid;
			}
			return await ReadFromCoreServerAsync(bulid.Content);
		}

		/// <summary>
		/// 读取指定地址的byte数据，地址格式 S100 <br />
		/// Reads the byte data of the specified address, the address format S100
		/// </summary>
		/// <param name="address">起始地址，格式为S100 </param>
		/// <returns>是否读取成功的结果对象</returns>
		/// <example>参考<see cref="M:Communication.Profinet.Fuji.FujiCommandSettingType.Read(System.String,System.UInt16)" />的注释</example>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <summary>
		/// 向PLC中写入byte数据，返回值说明<br />
		/// Write byte data to the PLC, return value description
		/// </summary>
		/// <param name="address">起始地址，格式为 S100</param>
		/// <param name="value">byte数据</param>
		/// <returns>是否写入成功的结果对象 </returns>
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiCommandSettingType.Read(System.String,System.UInt16)" />
		public async Task<OperateResult<byte>> ReadByteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiCommandSettingType.Write(System.String,System.Byte[])" />
		public async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteAsync(address, new byte[1] { value });
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FujiCommandSettingType[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 构建读取的报文指令
		/// </summary>
		/// <param name="address">PLC的地址信息</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>报文构建的结果对象</returns>
		public static OperateResult<byte[]> BuildReadCommand(string address, ushort length)
		{
			OperateResult<FujiCommandSettingTypeAddress> operateResult = FujiCommandSettingTypeAddress.ParseFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(new byte[9]
			{
				0,
				0,
				0,
				operateResult.Content.DataCode,
				4,
				BitConverter.GetBytes(operateResult.Content.AddressStart)[0],
				BitConverter.GetBytes(operateResult.Content.AddressStart)[1],
				BitConverter.GetBytes(operateResult.Content.Length)[0],
				BitConverter.GetBytes(operateResult.Content.Length)[1]
			});
		}

		/// <summary>
		/// 构建写入原始报文数据的请求信息
		/// </summary>
		/// <param name="address">地址数据</param>
		/// <param name="value">原始报文的数据</param>
		/// <returns>原始的写入报文数据</returns>
		public static OperateResult<byte[]> BuildWriteCommand(string address, byte[] value)
		{
			OperateResult<FujiCommandSettingTypeAddress> operateResult = FujiCommandSettingTypeAddress.ParseFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[9 + value.Length];
			array[0] = 1;
			array[1] = 0;
			array[2] = 0;
			array[3] = operateResult.Content.DataCode;
			array[4] = (byte)(4 + value.Length);
			array[5] = BitConverter.GetBytes(operateResult.Content.AddressStart)[0];
			array[6] = BitConverter.GetBytes(operateResult.Content.AddressStart)[1];
			array[7] = BitConverter.GetBytes(operateResult.Content.Length)[0];
			array[8] = BitConverter.GetBytes(operateResult.Content.Length)[0];
			value.CopyTo(array, 9);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 根据错误信息获取相关数据信息
		/// </summary>
		/// <param name="error">错误代号</param>
		/// <returns>实际的错误描述信息</returns>
		public static string GetErrorText(int error)
		{
			switch (error)
			{
			case 18:
				return "Write of data to the program area";
			case 32:
				return "Non-existing CMND code";
			case 33:
				return "Input data is not in the order of data corresponding to CMND";
			case 34:
				return "Operation only from the loader is effective. Operation from any other node is disabled";
			case 36:
				return "A non-existing module has been specified";
			case 50:
				return "An address out of the memory size has been specified";
			default:
				return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 根据PLC返回的数据，解析出实际的数据内容
		/// </summary>
		/// <param name="send">发送给PLC的数据</param>
		/// <param name="response">PLC返回的数据</param>
		/// <returns>结果数据信息</returns>
		public static OperateResult<byte[]> UnpackResponseContentHelper(byte[] send, byte[] response)
		{
			if (response[1] != 0)
			{
				return new OperateResult<byte[]>(GetErrorText(response[1]));
			}
			return OperateResult.CreateSuccessResult(response.RemoveBegin(10));
		}
	}
}

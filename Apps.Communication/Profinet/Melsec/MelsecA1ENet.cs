using System;
using System.Linq;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 三菱PLC通讯协议，采用A兼容1E帧协议实现，使用二进制码通讯，请根据实际型号来进行选取<br />
	/// Mitsubishi PLC communication protocol, implemented using A compatible 1E frame protocol, using binary code communication, please choose according to the actual model
	/// </summary>
	/// <remarks>
	/// 本类适用于的PLC列表
	/// <list type="number">
	/// <item>FX3U(C) PLC   测试人sandy_liao</item>
	/// </list>
	/// <note type="important">本通讯类由CKernal推送，感谢</note>
	/// </remarks>
	/// <example>
	/// 数据地址支持的格式如下：
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
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X10,X20</term>
	///     <term>动态</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>地址前面带0就是8进制比如X010，不带则是16进制，X40</term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y10,Y20</term>
	///     <term>动态</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>地址前面带0就是8进制比如Y020，不带则是16进制，Y40</term>
	///   </item>
	///   <item>
	///     <term>步进继电器</term>
	///     <term>S</term>
	///     <term>S100,S200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>报警器</term>
	///     <term>F</term>
	///     <term>F100,F200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接继电器</term>
	///     <term>B</term>
	///     <term>B1A0,B2A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器触点</term>
	///     <term>TS</term>
	///     <term>TS0,TS100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器线圈</term>
	///     <term>TC</term>
	///     <term>TC0,TC100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>定时器当前值</term>
	///     <term>TN</term>
	///     <term>TN0,TN100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器器触点</term>
	///     <term>CS</term>
	///     <term>CS0,CS100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器线圈</term>
	///     <term>CC</term>
	///     <term>CC0,CC100</term>
	///     <term>10</term>
	///     <term>×</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>计数器当前值</term>
	///     <term>CN</term>
	///     <term>CN0,CN100</term>
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
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W0,W1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>文件寄存器</term>
	///     <term>R</term>
	///     <term>R100,R200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// <see cref="M:Communication.Profinet.Melsec.MelsecA1ENet.ReadBool(System.String,System.UInt16)" /> 方法一次读取的最多点数是256点。
	/// </example>
	public class MelsecA1ENet : NetworkDeviceBase
	{
		/// <summary>
		/// PLC编号，默认为0xFF<br />
		/// PLC number, default is 0xFF
		/// </summary>
		public byte PLCNumber { get; set; } = byte.MaxValue;


		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public MelsecA1ENet()
		{
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 指定ip地址和端口来来实例化一个默认的对象<br />
		/// Specify the IP address and port to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecA1ENet(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new MelsecA1EBinaryMessage();
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadCommand(address, length, isBit: false, PLCNumber);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponseLegal(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			return ExtractActualData(operateResult2.Content, isBit: false);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteWordCommand(address, value, PLCNumber);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = CheckResponseLegal(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[]> command = BuildReadCommand(address, length, isBit: false, PLCNumber);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			OperateResult check = CheckResponseLegal(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return ExtractActualData(read.Content, isBit: false);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> command = BuildWriteWordCommand(address, value, PLCNumber);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponseLegal(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 批量读取<see cref="T:System.Boolean" />数组信息，需要指定地址和长度，地址示例M100，S100，B1A，如果是X,Y, X017就是8进制地址，Y10就是16进制地址。<br />
		/// Batch read <see cref="T:System.Boolean" /> array information, need to specify the address and length, return <see cref="T:System.Boolean" /> array. 
		/// Examples of addresses M100, S100, B1A, if it is X, Y, X017 is an octal address, Y10 is a hexadecimal address.
		/// </summary>
		/// <remarks>
		/// 根据协议的规范，最多读取256长度的bool数组信息，如果需要读取更长的bool信息，需要按字为单位进行读取的操作。
		/// </remarks>
		/// <param name="address">数据地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>带有成功标识的byte[]数组</returns>
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadCommand(address, length, isBit: true, PLCNumber);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponseLegal(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			OperateResult<byte[]> operateResult4 = ExtractActualData(operateResult2.Content, isBit: true);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(operateResult4.Content.Select((byte m) => m == 1).Take(length).ToArray());
		}

		/// <summary>
		/// 批量写入<see cref="T:System.Boolean" />数组数据，返回是否成功，地址示例M100，S100，B1A，如果是X,Y, X017就是8进制地址，Y10就是16进制地址。<br />
		/// Batch write <see cref="T:System.Boolean" /> array data, return whether the write was successful. 
		/// Examples of addresses M100, S100, B1A, if it is X, Y, X017 is an octal address, Y10 is a hexadecimal address.
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">写入值</param>
		/// <returns>带有成功标识的结果类对象</returns>
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteBoolCommand(address, value, PLCNumber);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return CheckResponseLegal(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecA1ENet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			OperateResult<byte[]> command = BuildReadCommand(address, length, isBit: true, PLCNumber);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			OperateResult check = CheckResponseLegal(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(check);
			}
			OperateResult<byte[]> extract = ExtractActualData(read.Content, isBit: true);
			if (!extract.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(extract);
			}
			return OperateResult.CreateSuccessResult(extract.Content.Select((byte m) => m == 1).Take(length).ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecA1ENet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			OperateResult<byte[]> command = BuildWriteBoolCommand(address, values, PLCNumber);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return CheckResponseLegal(read.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecA1ENet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 根据类型地址长度确认需要读取的指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">长度</param>
		/// <param name="isBit">指示是否按照位成批的读出</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildReadCommand(string address, ushort length, bool isBit, byte plcNumber)
		{
			OperateResult<MelsecA1EDataType, int> operateResult = MelsecHelper.McA1EAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte b = (byte)((!isBit) ? 1 : 0);
			return OperateResult.CreateSuccessResult(new byte[12]
			{
				b,
				plcNumber,
				10,
				0,
				BitConverter.GetBytes(operateResult.Content2)[0],
				BitConverter.GetBytes(operateResult.Content2)[1],
				BitConverter.GetBytes(operateResult.Content2)[2],
				BitConverter.GetBytes(operateResult.Content2)[3],
				BitConverter.GetBytes(operateResult.Content1.DataCode)[0],
				BitConverter.GetBytes(operateResult.Content1.DataCode)[1],
				BitConverter.GetBytes(length)[0],
				BitConverter.GetBytes(length)[1]
			});
		}

		/// <summary>
		/// 根据类型地址以及需要写入的数据来生成指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">数据值</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildWriteWordCommand(string address, byte[] value, byte plcNumber)
		{
			OperateResult<MelsecA1EDataType, int> operateResult = MelsecHelper.McA1EAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[12 + value.Length];
			array[0] = 3;
			array[1] = plcNumber;
			array[2] = 10;
			array[3] = 0;
			array[4] = BitConverter.GetBytes(operateResult.Content2)[0];
			array[5] = BitConverter.GetBytes(operateResult.Content2)[1];
			array[6] = BitConverter.GetBytes(operateResult.Content2)[2];
			array[7] = BitConverter.GetBytes(operateResult.Content2)[3];
			array[8] = BitConverter.GetBytes(operateResult.Content1.DataCode)[0];
			array[9] = BitConverter.GetBytes(operateResult.Content1.DataCode)[1];
			array[10] = BitConverter.GetBytes(value.Length / 2)[0];
			array[11] = BitConverter.GetBytes(value.Length / 2)[1];
			Array.Copy(value, 0, array, 12, value.Length);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 根据类型地址以及需要写入的数据来生成指令头
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="value">数据值</param>
		/// <param name="plcNumber">PLC编号</param>
		/// <returns>带有成功标志的指令数据</returns>
		public static OperateResult<byte[]> BuildWriteBoolCommand(string address, bool[] value, byte plcNumber)
		{
			OperateResult<MelsecA1EDataType, int> operateResult = MelsecHelper.McA1EAnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = MelsecHelper.TransBoolArrayToByteData(value);
			byte[] array2 = new byte[12 + array.Length];
			array2[0] = 2;
			array2[1] = plcNumber;
			array2[2] = 10;
			array2[3] = 0;
			array2[4] = BitConverter.GetBytes(operateResult.Content2)[0];
			array2[5] = BitConverter.GetBytes(operateResult.Content2)[1];
			array2[6] = BitConverter.GetBytes(operateResult.Content2)[2];
			array2[7] = BitConverter.GetBytes(operateResult.Content2)[3];
			array2[8] = BitConverter.GetBytes(operateResult.Content1.DataCode)[0];
			array2[9] = BitConverter.GetBytes(operateResult.Content1.DataCode)[1];
			array2[10] = BitConverter.GetBytes(value.Length)[0];
			array2[11] = BitConverter.GetBytes(value.Length)[1];
			Array.Copy(array, 0, array2, 12, array.Length);
			return OperateResult.CreateSuccessResult(array2);
		}

		/// <summary>
		/// 检测反馈的消息是否合法
		/// </summary>
		/// <param name="response">接收的报文</param>
		/// <returns>是否成功</returns>
		public static OperateResult CheckResponseLegal(byte[] response)
		{
			if (response.Length < 2)
			{
				return new OperateResult(StringResources.Language.ReceiveDataLengthTooShort);
			}
			if (response[1] == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			if (response[1] == 91)
			{
				return new OperateResult(response[2], StringResources.Language.MelsecPleaseReferToManualDocument);
			}
			return new OperateResult(response[1], StringResources.Language.MelsecPleaseReferToManualDocument);
		}

		/// <summary>
		/// 从PLC反馈的数据中提取出实际的数据内容，需要传入反馈数据，是否位读取
		/// </summary>
		/// <param name="response">反馈的数据内容</param>
		/// <param name="isBit">是否位读取</param>
		/// <returns>解析后的结果对象</returns>
		public static OperateResult<byte[]> ExtractActualData(byte[] response, bool isBit)
		{
			if (isBit)
			{
				byte[] array = new byte[(response.Length - 2) * 2];
				for (int i = 2; i < response.Length; i++)
				{
					if ((response[i] & 0x10) == 16)
					{
						array[(i - 2) * 2] = 1;
					}
					if ((response[i] & 1) == 1)
					{
						array[(i - 2) * 2 + 1] = 1;
					}
				}
				return OperateResult.CreateSuccessResult(array);
			}
			return OperateResult.CreateSuccessResult(response.RemoveBegin(2));
		}
	}
}

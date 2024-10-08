using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Serial;

namespace Apps.Communication.Instrument.DLT
{
	/// <summary>
	/// 基于多功能电能表通信协议实现的通讯类，参考的文档是DLT645-2007，主要实现了对电表数据的读取和一些功能方法，
	/// 在点对点模式下，需要在打开串口后调用 <see cref="M:Communication.Instrument.DLT.DLT645.ReadAddress" /> 方法，数据标识格式为 00-00-00-00，具体参照文档手册。<br />
	/// The communication type based on the communication protocol of the multifunctional electric energy meter. 
	/// The reference document is DLT645-2007, which mainly realizes the reading of the electric meter data and some functional methods. 
	/// In the point-to-point mode, you need to call <see cref="M:Communication.Instrument.DLT.DLT645.ReadAddress" /> method after opening the serial port.
	/// the data identification format is 00-00-00-00, refer to the documentation manual for details.
	/// </summary>
	/// <remarks>
	/// 如果一对多的模式，地址可以携带地址域访问，例如 "s=2;00-00-00-00"，主要使用 <see cref="M:Communication.Instrument.DLT.DLT645.ReadDouble(System.String,System.UInt16)" /> 方法来读取浮点数，
	/// <see cref="M:Communication.Serial.SerialDeviceBase.ReadString(System.String,System.UInt16)" /> 方法来读取字符串
	/// </remarks>
	/// <example>
	/// 具体的地址请参考相关的手册内容，如果没有，可以联系HSL作者或者，下面列举一些常用的地址<br />
	/// 对于电能来说，DI0是结算日的信息，现在的就是写0，上一结算日的就写 01，上12结算日就写 0C
	/// <list type="table">
	///   <listheader>
	///     <term>DI3</term>
	///     <term>DI2</term>
	///     <term>DI1</term>
	///     <term>DI0</term>
	///     <term>地址示例</term>
	///     <term>读取方式</term>
	///     <term>数据项名称</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-00-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）组合有功总电能(kwh)</term>
	///     <term>00-00-01-00到00-00-3F-00分别是组合有功费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>00</term>
	///     <term>01</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-01-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）正向有功总电能(kwh)</term>
	///     <term>00-01-01-00到00-01-3F-00分别是正向有功费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>00</term>
	///     <term>02</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-02-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）反向有功总电能(kwh)</term>
	///     <term>00-02-01-00到00-02-3F-00分别是反向有功费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>00</term>
	///     <term>03</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-03-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）组合无功总电能(kvarh)</term>
	///     <term>00-03-01-00到00-03-3F-00分别是组合无功费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>00</term>
	///     <term>09</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-09-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）正向视在总电能(kvah)</term>
	///     <term>00-09-01-00到00-09-3F-00分别是正向视在费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>00</term>
	///     <term>0A</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>00-0A-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>（当前）反向视在总电能(kvah)</term>
	///     <term>00-0A-01-00到00-0A-3F-00分别是反向视在费率1~63电能</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>01</term>
	///     <term>01</term>
	///     <term>00</term>
	///     <term>02-01-01-00</term>
	///     <term>ReadDouble</term>
	///     <term>A相电压(V)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>01</term>
	///     <term>02</term>
	///     <term>00</term>
	///     <term>02-01-02-00</term>
	///     <term>ReadDouble</term>
	///     <term>B相电压(V)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>01</term>
	///     <term>03</term>
	///     <term>00</term>
	///     <term>02-01-03-00</term>
	///     <term>ReadDouble</term>
	///     <term>C相电压(V)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>02</term>
	///     <term>01</term>
	///     <term>00</term>
	///     <term>02-02-01-00</term>
	///     <term>ReadDouble</term>
	///     <term>A相电流(A)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>02</term>
	///     <term>02</term>
	///     <term>00</term>
	///     <term>02-02-02-00</term>
	///     <term>ReadDouble</term>
	///     <term>B相电流(A)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>02</term>
	///     <term>03</term>
	///     <term>00</term>
	///     <term>02-02-03-00</term>
	///     <term>ReadDouble</term>
	///     <term>C相电流(A)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>03</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>02-03-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>瞬时总有功功率(kw)</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>04</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>02-04-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>瞬时总无功功率(kvar)</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>05</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>02-05-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>瞬时总视在功率(kva)</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>06</term>
	///     <term>00</term>
	///     <term>00</term>
	///     <term>02-06-00-00</term>
	///     <term>ReadDouble</term>
	///     <term>总功率因素</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>07</term>
	///     <term>01</term>
	///     <term>00</term>
	///     <term>02-07-01-00</term>
	///     <term>ReadDouble</term>
	///     <term>A相相角(°)</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>08</term>
	///     <term>01</term>
	///     <term>00</term>
	///     <term>02-08-01-00</term>
	///     <term>ReadDouble</term>
	///     <term>A相电压波形失真度(%)</term>
	///     <term>DI1=1时表示A相，2时表示B相，3时表示C相</term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>01</term>
	///     <term>02-80-00-01</term>
	///     <term>ReadDouble</term>
	///     <term>零线电流(A)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>02</term>
	///     <term>02-80-00-02</term>
	///     <term>ReadDouble</term>
	///     <term>电网频率(HZ)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>03</term>
	///     <term>02-80-00-03</term>
	///     <term>ReadDouble</term>
	///     <term>一分钟有功总平均功率(kw)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>04</term>
	///     <term>02-80-00-04</term>
	///     <term>ReadDouble</term>
	///     <term>当前有功需量(kw)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>05</term>
	///     <term>02-80-00-05</term>
	///     <term>ReadDouble</term>
	///     <term>当前无功需量(kvar)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>06</term>
	///     <term>02-80-00-06</term>
	///     <term>ReadDouble</term>
	///     <term>当前视在需量(kva)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>07</term>
	///     <term>02-80-00-07</term>
	///     <term>ReadDouble</term>
	///     <term>表内温度(摄氏度)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>08</term>
	///     <term>02-80-00-08</term>
	///     <term>ReadDouble</term>
	///     <term>时钟电池电压(V)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>09</term>
	///     <term>02-80-00-09</term>
	///     <term>ReadDouble</term>
	///     <term>停电抄表电池电压(V)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>02</term>
	///     <term>80</term>
	///     <term>00</term>
	///     <term>0A</term>
	///     <term>02-80-00-0A</term>
	///     <term>ReadDouble</term>
	///     <term>内部电池工作时间(分钟)</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>04</term>
	///     <term>00</term>
	///     <term>04</term>
	///     <term>03</term>
	///     <term>04-00-04-03</term>
	///     <term>ReadString("04-00-04-03", 32)</term>
	///     <term>资产管理编码</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>04</term>
	///     <term>00</term>
	///     <term>04</term>
	///     <term>0B</term>
	///     <term>04-00-04-0B</term>
	///     <term>ReadString("04-00-04-0B", 10)</term>
	///     <term>电表型号</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>04</term>
	///     <term>00</term>
	///     <term>04</term>
	///     <term>0C</term>
	///     <term>04-00-04-0C</term>
	///     <term>ReadString("04-00-04-0C", 10)</term>
	///     <term>生产日期</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// 直接串口初始化，打开串口，就可以对数据进行读取了，地址如上图所示。
	/// </example>
	public class DLT645 : SerialDeviceBase
	{
		private string station = "1";

		private string password = "00000000";

		private string opCode = "00000000";

		/// <summary>
		/// 获取或设置当前的地址域信息，是一个12个字符的BCD码，例如：149100007290<br />
		/// Get or set the current address domain information, which is a 12-character BCD code, for example: 149100007290
		/// </summary>
		public string Station
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
		/// 指定地址域，密码，操作者代码来实例化一个对象<br />
		/// Get or set the current address domain information, which is a 12-character BCD code, for example: 149100007290
		/// </summary>
		/// <param name="station">设备的站号信息</param>
		/// <param name="password">密码，写入的时候进行验证的信息</param>
		/// <param name="opCode">操作者代码</param>
		public DLT645(string station, string password = "", string opCode = "")
		{
			base.ByteTransform = new RegularByteTransform();
			this.station = station;
			this.password = (string.IsNullOrEmpty(password) ? "00000000" : password);
			this.opCode = (string.IsNullOrEmpty(opCode) ? "00000000" : opCode);
		}

		/// <summary>
		/// 激活设备的命令，只发送数据到设备，不等待设备数据返回<br />
		/// The command to activate the device, only send data to the device, do not wait for the device data to return
		/// </summary>
		/// <returns>是否发送成功</returns>
		public OperateResult ActiveDeveice()
		{
			return ReadFromCoreServer(new byte[4] { 254, 254, 254, 254 }, hasResponseData: false);
		}

		private OperateResult<byte[]> ReadWithAddress(string address, byte[] dataArea)
		{
			OperateResult<byte[]> operateResult = BuildEntireCommand(address, 17, dataArea);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			if (operateResult2.Content.Length < 16)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.SelectMiddle(14, operateResult2.Content.Length - 16));
		}

		/// <summary>
		/// 根据指定的数据标识来读取相关的原始数据信息，地址标识根据手册来，从高位到地位，例如 00-00-00-00，分割符可以任意特殊字符或是没有分隔符。<br />
		/// Read the relevant original data information according to the specified data identifier. The address identifier is based on the manual, 
		/// from high to position, such as 00-00-00-00. The separator can be any special character or no separator.
		/// </summary>
		/// <remarks>
		/// 地址可以携带地址域信息，例如 "s=2;00-00-00-00" 或是 "s=100000;00-00-02-00"，关于数据域信息，需要查找手册，例如:00-01-00-00 表示： (当前)正向有功总电能
		/// </remarks>
		/// <param name="address">数据标识，具体需要查找手册来对应</param>
		/// <param name="length">数据长度信息</param>
		/// <returns>结果信息</returns>
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<string, byte[]> operateResult = AnalysisBytesAddress(address, station, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return ReadWithAddress(operateResult.Content1, operateResult.Content2);
		}

		/// <inheritdoc />
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			OperateResult<string, byte[]> operateResult = AnalysisBytesAddress(address, station, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadWithAddress(operateResult.Content1, operateResult.Content2);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(operateResult2);
			}
			return DLTTransform.TransDoubleFromDLt(operateResult2.Content, length, GetFormatWithDataArea(operateResult.Content2));
		}

		/// <inheritdoc />
		public override OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> operateResult = Read(address, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			return DLTTransform.TransStringFromDLt(operateResult.Content, length);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ReadDouble(System.String,System.UInt16)" />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			return await Task.Run(() => ReadDouble(address, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> read = await ReadAsync(address, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			return DLTTransform.TransStringFromDLt(read.Content, length);
		}

		/// <summary>
		/// 根据指定的数据标识来写入相关的原始数据信息，地址标识根据手册来，从高位到地位，例如 00-00-00-00，分割符可以任意特殊字符或是没有分隔符。<br />
		/// Read the relevant original data information according to the specified data identifier. The address identifier is based on the manual, 
		/// from high to position, such as 00-00-00-00. The separator can be any special character or no separator.
		/// </summary>
		/// <remarks>
		/// 地址可以携带地址域信息，例如 "s=2;00-00-00-00" 或是 "s=100000;00-00-02-00"，关于数据域信息，需要查找手册，例如:00-01-00-00 表示： (当前)正向有功总电能<br />
		/// 注意：本命令必须与编程键配合使用
		/// </remarks>
		/// <param name="address">地址信息</param>
		/// <param name="value">写入的数据值</param>
		/// <returns>是否写入成功</returns>
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<string, byte[]> operateResult = AnalysisBytesAddress(address, station, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] dataArea = SoftBasic.SpliceArray<byte>(operateResult.Content2, password.ToHexBytes(), opCode.ToHexBytes(), value);
			OperateResult<byte[]> operateResult2 = BuildEntireCommand(operateResult.Content1, 21, dataArea);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return CheckResponse(operateResult3.Content);
		}

		/// <summary>
		/// 读取设备的通信地址，仅支持点对点通讯的情况，返回地址域数据，例如：149100007290<br />
		/// Read the communication address of the device, only support point-to-point communication, and return the address field data, for example: 149100007290
		/// </summary>
		/// <returns>设备的通信地址</returns>
		public OperateResult<string> ReadAddress()
		{
			OperateResult<byte[]> operateResult = BuildEntireCommand("AAAAAAAAAAAA", 19, null);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult3);
			}
			station = operateResult2.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString();
			return OperateResult.CreateSuccessResult(operateResult2.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString());
		}

		/// <summary>
		/// 写入设备的地址域信息，仅支持点对点通讯的情况，需要指定地址域信息，例如：149100007290<br />
		/// Write the address domain information of the device, only support point-to-point communication, 
		/// you need to specify the address domain information, for example: 149100007290
		/// </summary>
		/// <param name="address">等待写入的地址域</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteAddress(string address)
		{
			OperateResult<byte[]> addressByteFromString = GetAddressByteFromString(address);
			if (!addressByteFromString.IsSuccess)
			{
				return addressByteFromString;
			}
			OperateResult<byte[]> operateResult = BuildEntireCommand("AAAAAAAAAAAA", 21, addressByteFromString.Content);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			if (SoftBasic.IsTwoBytesEquel(operateResult2.Content.SelectMiddle(1, 6), GetAddressByteFromString(address).Content))
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <summary>
		/// 广播指定的时间，强制从站与主站时间同步，传入<see cref="T:System.DateTime" />时间对象，没有数据返回。<br />
		/// Broadcast the specified time, force the slave station to synchronize with the master station time, 
		/// pass in the <see cref="T:System.DateTime" /> time object, and no data will be returned.
		/// </summary>
		/// <param name="dateTime">时间对象</param>
		/// <returns>是否成功</returns>
		public OperateResult BroadcastTime(DateTime dateTime)
		{
			string value = $"{dateTime.Second:D2}{dateTime.Minute:D2}{dateTime.Hour:D2}{dateTime.Day:D2}{dateTime.Month:D2}{dateTime.Year % 100:D2}";
			OperateResult<byte[]> operateResult = BuildEntireCommand("999999999999", 8, value.ToHexBytes());
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ReadFromCoreServer(operateResult.Content, hasResponseData: false);
		}

		/// <summary>
		/// 对设备发送冻结命令，默认点对点操作，地址域为 99999999999999 时为广播，数据域格式说明：MMDDhhmm(月日时分)，
		/// 99DDhhmm表示月为周期定时冻结，9999hhmm表示日为周期定时冻结，999999mm表示以小时为周期定时冻结，99999999表示瞬时冻结<br />
		/// Send a freeze command to the device, the default point-to-point operation, when the address field is 9999999999999, 
		/// it is broadcast, and the data field format description: MMDDhhmm (month, day, hour and minute), 
		/// 99DDhhmm means the month is the periodic fixed freeze, 9999hhmm means the day is the periodic periodic freeze, 
		/// and 999999mm means the hour It is periodic timed freezing, 99999999 means instantaneous freezing
		/// </summary>
		/// <param name="dataArea">数据域信息</param>
		/// <returns>是否成功冻结</returns>
		public OperateResult FreezeCommand(string dataArea)
		{
			OperateResult<string, byte[]> operateResult = AnalysisBytesAddress(dataArea, station, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = BuildEntireCommand(operateResult.Content1, 22, operateResult.Content2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult.Content1 == "999999999999")
			{
				return ReadFromCoreServer(operateResult2.Content, hasResponseData: false);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return CheckResponse(operateResult3.Content);
		}

		/// <summary>
		/// 更改通信速率，波特率可选 600,1200,2400,4800,9600,19200，其他值无效，可以携带地址域信息，s=1;9600 <br />
		/// Change the communication rate, the baud rate can be 600, 1200, 2400, 4800, 9600, 19200, 
		/// other values are invalid, you can carry address domain information, s=1;9600
		/// </summary>
		/// <param name="baudRate">波特率的信息</param>
		/// <returns>是否更改成功</returns>
		public OperateResult ChangeBaudRate(string baudRate)
		{
			OperateResult<string, int> operateResult = AnalysisIntegerAddress(baudRate, station);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			byte b = 0;
			switch (operateResult.Content2)
			{
			case 600:
				b = 2;
				break;
			case 1200:
				b = 4;
				break;
			case 2400:
				b = 8;
				break;
			case 4800:
				b = 16;
				break;
			case 9600:
				b = 32;
				break;
			case 19200:
				b = 64;
				break;
			default:
				return new OperateResult(StringResources.Language.NotSupportedFunction);
			}
			OperateResult<byte[]> operateResult2 = BuildEntireCommand(operateResult.Content1, 23, new byte[1] { b });
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			OperateResult operateResult4 = CheckResponse(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return operateResult4;
			}
			if (operateResult3.Content[10] == b)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DLT645[{base.PortName}:{base.BaudRate}]";
		}

		/// <summary>
		/// 将地址解析成BCD码的地址，并且扩充到12位，不够的补0操作
		/// </summary>
		/// <param name="address">地址域信息</param>
		/// <returns>实际的结果</returns>
		public static OperateResult<byte[]> GetAddressByteFromString(string address)
		{
			if (address == null || address.Length == 0)
			{
				return new OperateResult<byte[]>(StringResources.Language.DLTAddressCannotNull);
			}
			if (address.Length > 12)
			{
				return new OperateResult<byte[]>(StringResources.Language.DLTAddressCannotMoreThan12);
			}
			if (!Regex.IsMatch(address, "^[0-9A-A]+$"))
			{
				return new OperateResult<byte[]>(StringResources.Language.DLTAddressMatchFailed);
			}
			if (address.Length < 12)
			{
				address = address.PadLeft(12, '0');
			}
			return OperateResult.CreateSuccessResult(address.ToHexBytes().Reverse().ToArray());
		}

		/// <summary>
		/// 将指定的地址信息，控制码信息，数据域信息打包成完整的报文命令
		/// </summary>
		/// <param name="address">地址域信息，地址域由6个字节构成，每字节2位BCD码，地址长度可达12位十进制数。地址域支持锁位寻址，即从若干低位起，剩余高位补AAH作为通配符进行读表操作</param>
		/// <param name="control">控制码信息</param>
		/// <param name="dataArea">数据域的内容</param>
		/// <returns>返回是否报文创建成功</returns>
		public static OperateResult<byte[]> BuildEntireCommand(string address, byte control, byte[] dataArea)
		{
			if (dataArea == null)
			{
				dataArea = new byte[0];
			}
			OperateResult<byte[]> addressByteFromString = GetAddressByteFromString(address);
			if (!addressByteFromString.IsSuccess)
			{
				return addressByteFromString;
			}
			byte[] array = new byte[12 + dataArea.Length];
			array[0] = 104;
			addressByteFromString.Content.CopyTo(array, 1);
			array[7] = 104;
			array[8] = control;
			array[9] = (byte)dataArea.Length;
			if (dataArea.Length != 0)
			{
				dataArea.CopyTo(array, 10);
				for (int i = 0; i < dataArea.Length; i++)
				{
					array[i + 10] += 51;
				}
			}
			int num = 0;
			for (int j = 0; j < array.Length - 2; j++)
			{
				num += array[j];
			}
			array[array.Length - 2] = (byte)num;
			array[array.Length - 1] = 22;
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 从用户输入的地址信息中解析出真实的地址及数据标识
		/// </summary>
		/// <param name="address">用户输入的地址信息</param>
		/// <param name="defaultStation">默认的地址域</param>
		/// <param name="length">数据长度信息</param>
		/// <returns>解析结果信息</returns>
		public static OperateResult<string, byte[]> AnalysisBytesAddress(string address, string defaultStation, ushort length = 1)
		{
			string value = defaultStation;
			byte[] array = ((length == 1) ? new byte[4] : new byte[5]);
			if (length != 1)
			{
				array[4] = (byte)length;
			}
			if (address.IndexOf(';') > 0)
			{
				string[] array2 = address.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array2.Length; i++)
				{
					if (array2[i].StartsWith("s="))
					{
						value = array2[i].Substring(2);
					}
					else
					{
						array2[i].ToHexBytes().Reverse().ToArray()
							.CopyTo(array, 0);
					}
				}
			}
			else
			{
				address.ToHexBytes().Reverse().ToArray()
					.CopyTo(array, 0);
			}
			return OperateResult.CreateSuccessResult(value, array);
		}

		/// <summary>
		/// 根据不同的数据地址，返回实际的数据格式，然后解析出正确的数据
		/// </summary>
		/// <param name="dataArea">数据标识地址，实际的byte数组，地位在前，高位在后</param>
		/// <returns>实际的数据格式信息</returns>
		public static string GetFormatWithDataArea(byte[] dataArea)
		{
			if (dataArea[3] == 0)
			{
				return "XXXXXX.XX";
			}
			if (dataArea[3] == 1)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 1)
			{
				return "XXX.X";
			}
			if (dataArea[3] == 2 && dataArea[2] == 2)
			{
				return "XXX.XXX";
			}
			if (dataArea[3] == 2 && dataArea[2] < 6)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 6)
			{
				return "X.XXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 7)
			{
				return "XXX.X";
			}
			if (dataArea[3] == 2 && dataArea[2] < 128)
			{
				return "XX.XX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 1)
			{
				return "XXX.XXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 2)
			{
				return "XX.XX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 3)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 4)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 5)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 6)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 7)
			{
				return "XX.XX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 8)
			{
				return "XX.XX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 9)
			{
				return "XX.XX";
			}
			if (dataArea[3] == 2 && dataArea[2] == 128 && dataArea[0] == 10)
			{
				return "XXXXXXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 4 && dataArea[0] <= 2)
			{
				return "XXXXXXXXXXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 4 && dataArea[0] == 9)
			{
				return "XXXXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 4 && dataArea[0] == 10)
			{
				return "XXXXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 5)
			{
				return "XXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 6)
			{
				return "XX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 7)
			{
				return "XX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 8)
			{
				return "XX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 9)
			{
				return "XX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 13)
			{
				return "X.XXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 14 && dataArea[0] < 3)
			{
				return "XX.XXXX";
			}
			if (dataArea[3] == 4 && dataArea[2] == 0 && dataArea[1] == 14)
			{
				return "XXX.X";
			}
			return "XXXXXX.XX";
		}

		/// <summary>
		/// 从用户输入的地址信息中解析出真实的地址及数据标识
		/// </summary>
		/// <param name="address">用户输入的地址信息</param>
		/// <param name="defaultStation">默认的地址域</param>
		/// <returns>解析结果信息</returns>
		public static OperateResult<string, int> AnalysisIntegerAddress(string address, string defaultStation)
		{
			try
			{
				string value = defaultStation;
				int value2 = 0;
				if (address.IndexOf(';') > 0)
				{
					string[] array = address.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i].StartsWith("s="))
						{
							value = array[i].Substring(2);
						}
						else
						{
							value2 = Convert.ToInt32(array[i]);
						}
					}
				}
				else
				{
					value2 = Convert.ToInt32(address);
				}
				return OperateResult.CreateSuccessResult(value, value2);
			}
			catch (Exception ex)
			{
				return new OperateResult<string, int>(ex.Message);
			}
		}

		/// <summary>
		/// 检查当前的反馈数据信息是否正确
		/// </summary>
		/// <param name="response">从仪表反馈的数据信息</param>
		/// <returns>是否校验成功</returns>
		public static OperateResult CheckResponse(byte[] response)
		{
			if (response.Length < 9)
			{
				return new OperateResult(StringResources.Language.ReceiveDataLengthTooShort);
			}
			if ((response[8] & 0x40) == 64)
			{
				byte byt = response[10];
				if (byt.GetBoolByIndex(0))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit0);
				}
				if (byt.GetBoolByIndex(1))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit1);
				}
				if (byt.GetBoolByIndex(2))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit2);
				}
				if (byt.GetBoolByIndex(3))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit3);
				}
				if (byt.GetBoolByIndex(4))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit4);
				}
				if (byt.GetBoolByIndex(5))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit5);
				}
				if (byt.GetBoolByIndex(6))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit6);
				}
				if (byt.GetBoolByIndex(7))
				{
					return new OperateResult(StringResources.Language.DLTErrorInfoBit7);
				}
				return OperateResult.CreateSuccessResult();
			}
			return OperateResult.CreateSuccessResult();
		}
	}
}

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Instrument.DLT
{
	/// <summary>
	/// 基于多功能电能表通信协议实现的通讯类，参考的文档是DLT645-2007，主要实现了对电表数据的读取和一些功能方法，
	/// 在点对点模式下，需要在连接后调用 <see cref="M:Communication.Instrument.DLT.DLT645OverTcp.ReadAddress" /> 方法，数据标识格式为 00-00-00-00，具体参照文档手册。<br />
	/// The communication type based on the communication protocol of the multifunctional electric energy meter. 
	/// The reference document is DLT645-2007, which mainly realizes the reading of the electric meter data and some functional methods. 
	/// In the point-to-point mode, you need to call <see cref="M:Communication.Instrument.DLT.DLT645OverTcp.ReadAddress" /> method after connect the device.
	/// the data identification format is 00-00-00-00, refer to the documentation manual for details.
	/// </summary>
	/// <remarks>
	/// 如果一对多的模式，地址可以携带地址域访问，例如 "s=2;00-00-00-00"，主要使用 <see cref="M:Communication.Instrument.DLT.DLT645OverTcp.ReadDouble(System.String,System.UInt16)" /> 方法来读取浮点数，
	/// <see cref="M:Communication.Core.Net.NetworkDeviceBase.ReadString(System.String,System.UInt16)" /> 方法来读取字符串
	/// </remarks>
	public class DLT645OverTcp : NetworkDeviceBase
	{
		private string station = "1";

		private string password = "00000000";

		private string opCode = "00000000";

		/// <inheritdoc cref="P:Communication.Instrument.DLT.DLT645.Station" />
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
		/// 指定IP地址，端口，地址域，密码，操作者代码来实例化一个对象<br />
		/// Specify the IP address, port, address field, password, and operator code to instantiate an object
		/// </summary>
		/// <param name="ipAddress">TcpServer的IP地址</param>
		/// <param name="port">TcpServer的端口</param>
		/// <param name="station">设备的站号信息</param>
		/// <param name="password">密码，写入的时候进行验证的信息</param>
		/// <param name="opCode">操作者代码</param>
		public DLT645OverTcp(string ipAddress, int port = 502, string station = "1", string password = "", string opCode = "")
		{
			IpAddress = ipAddress;
			Port = port;
			base.WordLength = 1;
			base.ByteTransform = new ReverseWordTransform();
			this.station = station;
			this.password = (string.IsNullOrEmpty(password) ? "00000000" : password);
			this.opCode = (string.IsNullOrEmpty(opCode) ? "00000000" : opCode);
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new DLT645Message();
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ActiveDeveice" />
		public OperateResult ActiveDeveice()
		{
			return ReadFromCoreServer(new byte[4] { 254, 254, 254, 254 }, hasResponseData: false);
		}

		private OperateResult<byte[]> ReadWithAddress(string address, byte[] dataArea)
		{
			OperateResult<byte[]> operateResult = DLT645.BuildEntireCommand(address, 17, dataArea);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = DLT645.CheckResponse(operateResult2.Content);
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

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.Read(System.String,System.UInt16)" />
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<string, byte[]> operateResult = DLT645.AnalysisBytesAddress(address, station, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return ReadWithAddress(operateResult.Content1, operateResult.Content2);
		}

		/// <inheritdoc />
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			OperateResult<string, byte[]> operateResult = DLT645.AnalysisBytesAddress(address, station, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadWithAddress(operateResult.Content1, operateResult.Content2);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(operateResult2);
			}
			return DLTTransform.TransDoubleFromDLt(operateResult2.Content, length, DLT645.GetFormatWithDataArea(operateResult.Content2));
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

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ActiveDeveice" />
		public async Task<OperateResult> ActiveDeveiceAsync()
		{
			return await ReadFromCoreServerAsync(new byte[4] { 254, 254, 254, 254 }, hasResponseData: false);
		}

		private async Task<OperateResult<byte[]>> ReadWithAddressAsync(string address, byte[] dataArea)
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand(address, 17, dataArea);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = DLT645.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			if (read.Content.Length < 16)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			return OperateResult.CreateSuccessResult(read.Content.SelectMiddle(14, read.Content.Length - 16));
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress(address, station, length);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(analysis);
			}
			return await ReadWithAddressAsync(analysis.Content1, analysis.Content2);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645OverTcp.ReadDouble(System.String,System.UInt16)" />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress(address, station, length);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(analysis);
			}
			OperateResult<byte[]> read = await ReadWithAddressAsync(analysis.Content1, analysis.Content2);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(read);
			}
			return DLTTransform.TransDoubleFromDLt(read.Content, length, DLT645.GetFormatWithDataArea(analysis.Content2));
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

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.Write(System.String,System.Byte[])" />
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<string, byte[]> operateResult = DLT645.AnalysisBytesAddress(address, station, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] dataArea = SoftBasic.SpliceArray<byte>(operateResult.Content2, password.ToHexBytes(), opCode.ToHexBytes(), value);
			OperateResult<byte[]> operateResult2 = DLT645.BuildEntireCommand(operateResult.Content1, 21, dataArea);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return DLT645.CheckResponse(operateResult3.Content);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ReadAddress" />
		public OperateResult<string> ReadAddress()
		{
			OperateResult<byte[]> operateResult = DLT645.BuildEntireCommand("AAAAAAAAAAAA", 19, null);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = base.ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			OperateResult operateResult3 = DLT645.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult3);
			}
			station = operateResult2.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString();
			return OperateResult.CreateSuccessResult(operateResult2.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString());
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.WriteAddress(System.String)" />
		public OperateResult WriteAddress(string address)
		{
			OperateResult<byte[]> addressByteFromString = DLT645.GetAddressByteFromString(address);
			if (!addressByteFromString.IsSuccess)
			{
				return addressByteFromString;
			}
			OperateResult<byte[]> operateResult = DLT645.BuildEntireCommand("AAAAAAAAAAAA", 21, addressByteFromString.Content);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			OperateResult operateResult3 = DLT645.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			if (SoftBasic.IsTwoBytesEquel(operateResult2.Content.SelectMiddle(1, 6), DLT645.GetAddressByteFromString(address).Content))
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.BroadcastTime(System.DateTime)" />
		public OperateResult BroadcastTime(DateTime dateTime)
		{
			string value = $"{dateTime.Second:D2}{dateTime.Minute:D2}{dateTime.Hour:D2}{dateTime.Day:D2}{dateTime.Month:D2}{dateTime.Year % 100:D2}";
			OperateResult<byte[]> operateResult = DLT645.BuildEntireCommand("999999999999", 8, value.ToHexBytes());
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ReadFromCoreServer(operateResult.Content, hasResponseData: false);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.FreezeCommand(System.String)" />
		public OperateResult FreezeCommand(string dataArea)
		{
			OperateResult<string, byte[]> operateResult = DLT645.AnalysisBytesAddress(dataArea, station, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = DLT645.BuildEntireCommand(operateResult.Content1, 22, operateResult.Content2);
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
			return DLT645.CheckResponse(operateResult3.Content);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ChangeBaudRate(System.String)" />
		public OperateResult ChangeBaudRate(string baudRate)
		{
			OperateResult<string, int> operateResult = DLT645.AnalysisIntegerAddress(baudRate, station);
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
			OperateResult<byte[]> operateResult2 = DLT645.BuildEntireCommand(operateResult.Content1, 23, new byte[1] { b });
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			OperateResult operateResult4 = DLT645.CheckResponse(operateResult3.Content);
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

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645OverTcp.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress(address, station, 1);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(analysis);
			}
			OperateResult<byte[]> command = DLT645.BuildEntireCommand(dataArea: SoftBasic.SpliceArray<byte>(analysis.Content2, password.ToHexBytes(), opCode.ToHexBytes(), value), address: analysis.Content1, control: 21);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return DLT645.CheckResponse(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ReadAddress" />
		public async Task<OperateResult<string>> ReadAddressAsync()
		{
			OperateResult<byte[]> command = DLT645.BuildEntireCommand("AAAAAAAAAAAA", 19, null);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			OperateResult check = DLT645.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(check);
			}
			station = read.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString();
			return OperateResult.CreateSuccessResult(read.Content.SelectMiddle(1, 6).Reverse().ToArray()
				.ToHexString());
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.WriteAddress(System.String)" />
		public async Task<OperateResult> WriteAddressAsync(string address)
		{
			OperateResult<byte[]> add = DLT645.GetAddressByteFromString(address);
			if (!add.IsSuccess)
			{
				return add;
			}
			OperateResult<byte[]> command = DLT645.BuildEntireCommand("AAAAAAAAAAAA", 21, add.Content);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			OperateResult check = DLT645.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			if (SoftBasic.IsTwoBytesEquel(read.Content.SelectMiddle(1, 6), DLT645.GetAddressByteFromString(address).Content))
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.BroadcastTime(System.DateTime)" />
		public async Task<OperateResult> BroadcastTimeAsync(DateTime dateTime)
		{
			string hex = $"{dateTime.Second:D2}{dateTime.Minute:D2}{dateTime.Hour:D2}{dateTime.Day:D2}{dateTime.Month:D2}{dateTime.Year % 100:D2}";
			OperateResult<byte[]> command = DLT645.BuildEntireCommand("999999999999", 8, hex.ToHexBytes());
			if (!command.IsSuccess)
			{
				return command;
			}
			return await ReadFromCoreServerAsync(command.Content, hasResponseData: false);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.FreezeCommand(System.String)" />
		public async Task<OperateResult> FreezeCommandAsync(string dataArea)
		{
			OperateResult<string, byte[]> analysis = DLT645.AnalysisBytesAddress(dataArea, station, 1);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(analysis);
			}
			OperateResult<byte[]> command = DLT645.BuildEntireCommand(analysis.Content1, 22, analysis.Content2);
			if (!command.IsSuccess)
			{
				return command;
			}
			if (analysis.Content1 == "999999999999")
			{
				return await ReadFromCoreServerAsync(command.Content, hasResponseData: false);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return DLT645.CheckResponse(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Instrument.DLT.DLT645.ChangeBaudRate(System.String)" />
		public async Task<OperateResult> ChangeBaudRateAsync(string baudRate)
		{
			OperateResult<string, int> analysis = DLT645.AnalysisIntegerAddress(baudRate, station);
			if (!analysis.IsSuccess)
			{
				return analysis;
			}
			byte code;
			switch (analysis.Content2)
			{
			case 600:
				code = 2;
				break;
			case 1200:
				code = 4;
				break;
			case 2400:
				code = 8;
				break;
			case 4800:
				code = 16;
				break;
			case 9600:
				code = 32;
				break;
			case 19200:
				code = 64;
				break;
			default:
				return new OperateResult(StringResources.Language.NotSupportedFunction);
			}
			OperateResult<byte[]> command = DLT645.BuildEntireCommand(analysis.Content1, 23, new byte[1] { code });
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = DLT645.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			if (read.Content[10] == code)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(StringResources.Language.DLTErrorWriteReadCheckFailed);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DLT645OverTcp[{IpAddress}:{Port}]";
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.CNC.Fanuc
{
	/// <summary>
	/// 一个FANUC的机床通信类对象
	/// </summary>
	public class FanucSeries0i : NetworkDoubleBase
	{
		private Encoding encoding;

		/// <summary>
		/// 获取或设置当前的文本的字符编码信息，如果你不清楚，可以调用<see cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadLanguage" />方法来自动匹配。<br />
		/// Get or set the character encoding information of the current text. 
		/// If you are not sure, you can call the <see cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadLanguage" /> method to automatically match.
		/// </summary>
		public Encoding TextEncoding
		{
			get
			{
				return encoding;
			}
			set
			{
				encoding = value;
			}
		}

		/// <summary>
		/// 根据IP及端口来实例化一个对象内容
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号</param>
		public FanucSeries0i(string ipAddress, int port = 8193)
		{
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new ReverseBytesTransform();
			encoding = Encoding.Default;
			base.ReceiveTimeOut = 30000;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new CNCFanucSeriesMessage();
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, "a0 a0 a0 a0 00 01 01 01 00 02 00 02".ToHexBytes());
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(socket, "a0 a0 a0 a0 00 01 21 01 00 1e 00 01 00 1c 00 01 00 01 00 18 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00".ToHexBytes());
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override OperateResult ExtraOnDisconnect(Socket socket)
		{
			return ReadFromCoreServer(socket, "a0 a0 a0 a0 00 01 02 01 00 00".ToHexBytes());
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(socket, "a0 a0 a0 a0 00 01 01 01 00 02 00 02".ToHexBytes());
			if (!read1.IsSuccess)
			{
				return read1;
			}
			OperateResult<byte[]> read2 = await ReadFromCoreServerAsync(socket, BuildReadArray(BuildReadSingle(24, 0, 0, 0, 0, 0)));
			if (!read2.IsSuccess)
			{
				return read2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> ExtraOnDisconnectAsync(Socket socket)
		{
			return await ReadFromCoreServerAsync(socket, "a0 a0 a0 a0 00 01 02 01 00 00".ToHexBytes());
		}

		private double GetFanucDouble(byte[] content, int index)
		{
			return GetFanucDouble(content, index, 1)[0];
		}

		private double[] GetFanucDouble(byte[] content, int index, int length)
		{
			double[] array = new double[length];
			for (int i = 0; i < length; i++)
			{
				int num = base.ByteTransform.TransInt32(content, index + 8 * i);
				int num2 = base.ByteTransform.TransInt16(content, index + 8 * i + 6);
				if (num == 0)
				{
					array[i] = 0.0;
				}
				else
				{
					array[i] = Math.Round((double)num * Math.Pow(0.1, num2), num2);
				}
			}
			return array;
		}

		private byte[] CreateFromFanucDouble(double value)
		{
			byte[] array = new byte[8];
			int value2 = (int)(value * 1000.0);
			base.ByteTransform.TransByte(value2).CopyTo(array, 0);
			array[5] = 10;
			array[7] = 3;
			return array;
		}

		private void ChangeTextEncoding(ushort code)
		{
			switch (code)
			{
			case 0:
				encoding = Encoding.Default;
				break;
			case 1:
			case 4:
				encoding = Encoding.GetEncoding("shift_jis", EncoderFallback.ReplacementFallback, new DecoderReplacementFallback());
				break;
			case 6:
				encoding = Encoding.GetEncoding("ks_c_5601-1987");
				break;
			case 15:
				encoding = Encoding.Default;
				break;
			case 16:
				encoding = Encoding.GetEncoding("windows-1251");
				break;
			case 17:
				encoding = Encoding.GetEncoding("windows-1254");
				break;
			}
		}

		/// <summary>
		/// 主轴转速及进给倍率<br />
		/// Spindle speed and feedrate override
		/// </summary>
		/// <returns>主轴转速及进给倍率</returns>
		[HslMqttApi(Description = "Spindle speed and feedrate override")]
		public OperateResult<double, double> ReadSpindleSpeedAndFeedRate()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(164, 3, 0, 0, 0, 0), BuildReadSingle(138, 1, 0, 0, 0, 0), BuildReadSingle(136, 3, 0, 0, 0, 0), BuildReadSingle(136, 4, 0, 0, 0, 0), BuildReadSingle(36, 0, 0, 0, 0, 0), BuildReadSingle(37, 0, 0, 0, 0, 0), BuildReadSingle(164, 3, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double, double>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult(GetFanucDouble(list[5], 14), GetFanucDouble(list[4], 14));
		}

		/// <summary>
		/// 读取程序名及程序号<br />
		/// Read program name and program number
		/// </summary>
		/// <returns>程序名及程序号</returns>
		[HslMqttApi(Description = "Read program name and program number")]
		public OperateResult<string, int> ReadSystemProgramCurrent()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(207, 0, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string, int>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int value = base.ByteTransform.TransInt32(list[0], 14);
			string value2 = encoding.GetString(list[0].SelectMiddle(18, 36)).TrimEnd(default(char));
			return OperateResult.CreateSuccessResult(value2, value);
		}

		/// <summary>
		/// 读取机床的语言设定信息，具体值的含义参照API文档说明<br />
		/// Read the language setting information of the machine tool, refer to the API documentation for the meaning of the specific values
		/// </summary>
		/// <remarks>此处举几个常用值 0: 英语 1: 日语 2: 德语 3: 法语 4: 中文繁体 6: 韩语 15: 中文简体 16: 俄语 17: 土耳其语</remarks>
		/// <returns>返回的语言代号</returns>
		[HslMqttApi(Description = "Read the language setting information of the machine tool")]
		public OperateResult<ushort> ReadLanguage()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(141, 3281, 3281, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			ushort num = base.ByteTransform.TransUInt16(list[0], 24);
			ChangeTextEncoding(num);
			return OperateResult.CreateSuccessResult(num);
		}

		/// <summary>
		/// 读取宏变量，可以用来读取刀具号<br />
		/// Read macro variable, can be used to read tool number
		/// </summary>
		/// <param name="number">刀具号</param>
		/// <returns>读宏变量信息</returns>
		[HslMqttApi(Description = "Read macro variable, can be used to read tool number")]
		public OperateResult<double> ReadSystemMacroValue(int number)
		{
			return ByteTransformHelper.GetResultFromArray(ReadSystemMacroValue(number, 1));
		}

		/// <summary>
		/// 读取宏变量，可以用来读取刀具号<br />
		/// Read macro variable, can be used to read tool number
		/// </summary>
		/// <param name="number">宏变量地址</param>
		/// <param name="length">读取的长度信息</param>
		/// <returns>是否成功</returns>
		[HslMqttApi(ApiTopic = "ReadSystemMacroValueArray", Description = "Read macro variable, can be used to read tool number")]
		public OperateResult<double[]> ReadSystemMacroValue(int number, int length)
		{
			int[] array = SoftBasic.SplitIntegerToArray(length, 5);
			int num = number;
			List<byte> list = new List<byte>();
			for (int i = 0; i < array.Length; i++)
			{
				OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(21, num, num + array[i] - 1, 0, 0, 0)));
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<double[]>(operateResult);
				}
				list.AddRange(ExtraContentArray(operateResult.Content.RemoveBegin(10))[0].RemoveBegin(14));
				num += array[i];
			}
			try
			{
				return OperateResult.CreateSuccessResult(GetFanucDouble(list.ToArray(), 0, length));
			}
			catch (Exception ex)
			{
				return new OperateResult<double[]>(ex.Message + " Source:" + list.ToArray().ToHexString(' '));
			}
		}

		/// <summary>
		/// 写宏变量，需要指定地址及写入的数据<br />
		/// Write macro variable, need to specify the address and write data
		/// </summary>
		/// <param name="number">地址</param>
		/// <param name="values">数据值</param>
		/// <returns>是否成功</returns>
		[HslMqttApi(Description = "Write macro variable, need to specify the address and write data")]
		public OperateResult WriteSystemMacroValue(int number, double[] values)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildWriteSingle(22, number, number + values.Length - 1, 0, 0, values)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string, int>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			if (base.ByteTransform.TransUInt16(list[0], 6) == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(base.ByteTransform.TransUInt16(list[0], 6), "Unknown Error");
		}

		/// <summary>
		/// 根据刀具号写入长度形状补偿，刀具号为1-24<br />
		/// Write length shape compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(Description = "Write length shape compensation according to the tool number, the tool number is 1-24")]
		public OperateResult WriteCutterLengthShapeOffset(int cutter, double offset)
		{
			return WriteSystemMacroValue(11000 + cutter, new double[1] { offset });
		}

		/// <summary>
		/// 根据刀具号写入长度磨损补偿，刀具号为1-24<br />
		/// Write length wear compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(Description = "Write length wear compensation according to the tool number, the tool number is 1-24")]
		public OperateResult WriteCutterLengthWearOffset(int cutter, double offset)
		{
			return WriteSystemMacroValue(10000 + cutter, new double[1] { offset });
		}

		/// <summary>
		/// 根据刀具号写入半径形状补偿，刀具号为1-24<br />
		/// Write radius shape compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(Description = "Write radius shape compensation according to the tool number, the tool number is 1-24")]
		public OperateResult WriteCutterRadiusShapeOffset(int cutter, double offset)
		{
			return WriteSystemMacroValue(13000 + cutter, new double[1] { offset });
		}

		/// <summary>
		/// 根据刀具号写入半径磨损补偿，刀具号为1-24<br />
		/// Write radius wear compensation according to the tool number, the tool number is 1-24
		/// </summary>
		/// <param name="cutter">刀具号，范围为1-24</param>
		/// <param name="offset">补偿值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi(Description = "Write radius wear compensation according to the tool number, the tool number is 1-24")]
		public OperateResult WriteCutterRadiusWearOffset(int cutter, double offset)
		{
			return WriteSystemMacroValue(12000 + cutter, new double[1] { offset });
		}

		/// <summary>
		/// 读取伺服负载<br />
		/// Read servo load
		/// </summary>
		/// <returns>轴负载</returns>
		[HslMqttApi(Description = "Read servo load")]
		public OperateResult<double[]> ReadFanucAxisLoad()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(164, 2, 0, 0, 0, 0), BuildReadSingle(137, 0, 0, 0, 0, 0), BuildReadSingle(86, 1, 0, 0, 0, 0), BuildReadSingle(164, 2, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int length = base.ByteTransform.TransUInt16(list[0], 14);
			return OperateResult.CreateSuccessResult(GetFanucDouble(list[2], 14, length));
		}

		/// <summary>
		/// 读取机床的坐标，包括机械坐标，绝对坐标，相对坐标<br />
		/// Read the coordinates of the machine tool, including mechanical coordinates, absolute coordinates, and relative coordinates
		/// </summary>
		/// <returns>数控机床的坐标信息，包括机械坐标，绝对坐标，相对坐标</returns>
		[HslMqttApi(Description = "Read the coordinates of the machine tool, including mechanical coordinates, absolute coordinates, and relative coordinates")]
		public OperateResult<SysAllCoors> ReadSysAllCoors()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(164, 0, 0, 0, 0, 0), BuildReadSingle(137, -1, 0, 0, 0, 0), BuildReadSingle(136, 1, 0, 0, 0, 0), BuildReadSingle(136, 2, 0, 0, 0, 0), BuildReadSingle(163, 0, -1, 0, 0, 0), BuildReadSingle(38, 0, -1, 0, 0, 0), BuildReadSingle(38, 1, -1, 0, 0, 0), BuildReadSingle(38, 2, -1, 0, 0, 0), BuildReadSingle(38, 3, -1, 0, 0, 0), BuildReadSingle(164, 0, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysAllCoors>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int length = base.ByteTransform.TransUInt16(list[0], 14);
			SysAllCoors sysAllCoors = new SysAllCoors();
			sysAllCoors.Absolute = GetFanucDouble(list[5], 14, length);
			sysAllCoors.Machine = GetFanucDouble(list[6], 14, length);
			sysAllCoors.Relative = GetFanucDouble(list[7], 14, length);
			return OperateResult.CreateSuccessResult(sysAllCoors);
		}

		/// <summary>
		/// 读取报警信息<br />
		/// Read alarm information
		/// </summary>
		/// <returns>机床的当前的所有的报警信息</returns>
		[HslMqttApi(Description = "Read alarm information")]
		public OperateResult<SysAlarm[]> ReadSystemAlarm()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(35, -1, 10, 2, 64, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysAlarm[]>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			if (base.ByteTransform.TransUInt16(list[0], 12) > 0)
			{
				int num = (int)base.ByteTransform.TransUInt16(list[0], 12) / 80;
				SysAlarm[] array = new SysAlarm[num];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new SysAlarm();
					array[i].AlarmId = base.ByteTransform.TransInt32(list[0], 14 + 80 * i);
					array[i].Type = base.ByteTransform.TransInt16(list[0], 20 + 80 * i);
					array[i].Axis = base.ByteTransform.TransInt16(list[0], 24 + 80 * i);
					ushort count = base.ByteTransform.TransUInt16(list[0], 28 + 80 * i);
					array[i].Message = encoding.GetString(list[0], 30 + 80 * i, count);
				}
				return OperateResult.CreateSuccessResult(array);
			}
			return OperateResult.CreateSuccessResult(new SysAlarm[0]);
		}

		/// <summary>
		/// 读取fanuc机床的时间，0是开机时间，1是运行时间，2是切割时间，3是循环时间，4是空闲时间，返回秒为单位的信息<br />
		/// Read the time of the fanuc machine tool, 0 is the boot time, 1 is the running time, 2 is the cutting time, 
		/// 3 is the cycle time, 4 is the idle time, and returns the information in seconds.
		/// </summary>
		/// <param name="timeType">读取的时间类型</param>
		/// <returns>秒为单位的结果</returns>
		[HslMqttApi(Description = "Read the time of the fanuc machine tool, 0 is the boot time, 1 is the running time, 2 is the cutting time, 3 is the cycle time, 4 is the idle time, and returns the information in seconds.")]
		public OperateResult<long> ReadTimeData(int timeType)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(288, timeType, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<long>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int num = base.ByteTransform.TransInt32(list[0], 18);
			long num2 = base.ByteTransform.TransInt32(list[0], 14);
			if (num < 0 || num > 60000)
			{
				num = BitConverter.ToInt32(list[0], 18);
				num2 = BitConverter.ToInt32(list[0], 14);
			}
			long num3 = num / 1000;
			return OperateResult.CreateSuccessResult(num2 * 60 + num3);
		}

		/// <summary>
		/// 读取报警状态信息<br />
		/// Read alarm status information
		/// </summary>
		/// <returns>报警状态数据</returns>
		[HslMqttApi(Description = "Read alarm status information")]
		public OperateResult<int> ReadAlarmStatus()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(26, 0, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult((int)base.ByteTransform.TransUInt16(list[0], 16));
		}

		/// <summary>
		/// 读取系统的基本信息状态，工作模式，运行状态，是否急停等等操作<br />
		/// Read the basic information status of the system, working mode, running status, emergency stop, etc.
		/// </summary>
		/// <returns>结果信息数据</returns>
		[HslMqttApi(Description = "Read the basic information status of the system, working mode, running status, emergency stop, etc.")]
		public OperateResult<SysStatusInfo> ReadSysStatusInfo()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(25, 0, 0, 0, 0, 0), BuildReadSingle(225, 0, 0, 0, 0, 0), BuildReadSingle(152, 0, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysStatusInfo>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			SysStatusInfo sysStatusInfo = new SysStatusInfo();
			sysStatusInfo.Dummy = base.ByteTransform.TransInt16(list[1], 14);
			sysStatusInfo.TMMode = (short)((list[2].Length >= 16) ? base.ByteTransform.TransInt16(list[2], 14) : 0);
			sysStatusInfo.WorkMode = (CNCWorkMode)base.ByteTransform.TransInt16(list[0], 14);
			sysStatusInfo.RunStatus = (CNCRunStatus)base.ByteTransform.TransInt16(list[0], 16);
			sysStatusInfo.Motion = base.ByteTransform.TransInt16(list[0], 18);
			sysStatusInfo.MSTB = base.ByteTransform.TransInt16(list[0], 20);
			sysStatusInfo.Emergency = base.ByteTransform.TransInt16(list[0], 22);
			sysStatusInfo.Alarm = base.ByteTransform.TransInt16(list[0], 24);
			sysStatusInfo.Edit = base.ByteTransform.TransInt16(list[0], 26);
			return OperateResult.CreateSuccessResult(sysStatusInfo);
		}

		/// <summary>
		/// 读取设备的程序列表<br />
		/// Read the program list of the device
		/// </summary>
		/// <returns>读取结果信息</returns>
		[HslMqttApi(Description = "Read the program list of the device")]
		public OperateResult<int[]> ReadProgramList()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(6, 1, 19, 0, 0, 0)));
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(BuildReadArray(BuildReadSingle(6, 6667, 19, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int[]>(operateResult);
			}
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int[]>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int num = (list[0].Length - 14) / 72;
			int[] array = new int[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = base.ByteTransform.TransInt32(list[0], 14 + 72 * i);
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// 读取当前的刀具补偿信息<br />
		/// Read current tool compensation information
		/// </summary>
		/// <param name="cutterNumber">刀具数量</param>
		/// <returns>结果内容</returns>
		[HslMqttApi(Description = "Read current tool compensation information")]
		public OperateResult<CutterInfo[]> ReadCutterInfos(int cutterNumber = 24)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 1, 0, 0)));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 2, 0, 0)));
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(operateResult3);
			}
			OperateResult<byte[]> operateResult4 = ReadFromCoreServer(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 3, 0, 0)));
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(operateResult4);
			}
			return ExtraCutterInfos(operateResult.Content, operateResult2.Content, operateResult3.Content, operateResult4.Content, cutterNumber);
		}

		/// <summary>
		/// 读取当前的正在使用的刀具号<br />
		/// Read the tool number currently in use
		/// </summary>
		/// <returns>刀具号信息</returns>
		[HslMqttApi(Description = "Read the tool number currently in use")]
		public OperateResult<int> ReadCutterNumber()
		{
			OperateResult<double[]> operateResult = ReadSystemMacroValue(4120, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(operateResult);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(operateResult.Content[0]));
		}

		/// <summary>
		/// 读取R数据，需要传入起始地址和结束地址，返回byte[]数据信息<br />
		/// To read R data, you need to pass in the start address and end address, and return byte[] data information
		/// </summary>
		/// <param name="start">起始地址</param>
		/// <param name="end">结束地址</param>
		/// <returns>读取结果</returns>
		[HslMqttApi(Description = "To read R data, you need to pass in the start address and end address, and return byte[] data information")]
		public OperateResult<byte[]> ReadRData(int start, int end)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadMulti(2, 32769, start, end, 5, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int length = base.ByteTransform.TransUInt16(list[0], 12);
			return OperateResult.CreateSuccessResult(list[0].SelectMiddle(14, length));
		}

		/// <summary>
		/// 读取工件尺寸<br />
		/// Read workpiece size
		/// </summary>
		/// <returns>结果数据信息</returns>
		[HslMqttApi(Description = "Read workpiece size")]
		public OperateResult<double[]> ReadDeviceWorkPiecesSize()
		{
			return ReadSystemMacroValue(601, 20);
		}

		/// <summary>
		/// 读取当前的程序内容，只能读取程序的片段，返回程序内容。<br />
		/// Read the current program content, only read the program fragments, and return the program content.
		/// </summary>
		/// <returns>程序内容</returns>
		[HslMqttApi(Description = "Read the current program content, only read the program fragments, and return the program content.")]
		public OperateResult<string> ReadCurrentProgram()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(32, 1428, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			byte[] array = ExtraContentArray(operateResult.Content.RemoveBegin(10))[0];
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(array, 18, array.Length - 18));
		}

		/// <summary>
		/// 设置指定的程序号为当前的主程序，如果程序号不存在，返回错误信息<br />
		/// Set the specified program number as the current main program, if the program number does not exist, an error message will be returned
		/// </summary>
		/// <param name="programNum">程序号信息</param>
		/// <returns>是否设置成功</returns>
		[HslMqttApi(Description = "Set the specified program number as the current main program, if the program number does not exist, an error message will be returned.")]
		public OperateResult SetCurrentProgram(ushort programNum)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(3, programNum, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(operateResult);
			}
			byte[] buffer = ExtraContentArray(operateResult.Content.RemoveBegin(10))[0];
			short num = base.ByteTransform.TransInt16(buffer, 6);
			if (num == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(num, StringResources.Language.UnknownError);
		}

		/// <summary>
		/// 启动加工程序<br />
		/// Start the processing program
		/// </summary>
		/// <returns>是否启动成功</returns>
		[HslMqttApi(Description = "Start the processing program")]
		public OperateResult StartProcessing()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(1, 0, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(operateResult);
			}
			byte[] buffer = ExtraContentArray(operateResult.Content.RemoveBegin(10))[0];
			short num = base.ByteTransform.TransInt16(buffer, 6);
			if (num == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(num, StringResources.Language.UnknownError);
		}

		/// <summary>
		/// <b>[商业授权]</b> 将指定文件的NC加工程序，下载到数控机床里，返回是否下载成功<br />
		/// <b>[Authorization]</b> Download the NC machining program of the specified file to the CNC machine tool, and return whether the download is successful
		/// </summary>
		/// <remarks>
		/// 程序文件的内容必须%开始，%结束，下面是一个非常简单的例子：<br />
		/// %<br />
		/// O0006<br />
		/// G90G10L2P1<br />
		/// M30<br />
		/// %
		/// </remarks>
		/// <param name="file">程序文件的路径</param>
		/// <returns>是否下载成功</returns>
		[HslMqttApi(Description = "Download the NC machining program of the specified file to the CNC machine tool, and return whether the download is successful")]
		public OperateResult WriteProgramFile(string file)
		{
			string program = File.ReadAllText(file);
			return WriteProgramContent(program);
		}

		/// <summary>
		/// <b>[商业授权]</b> 将指定程序内容的NC加工程序，写入到数控机床里，返回是否下载成功<br />
		/// <b>[Authorization]</b> Download the NC machining program to the CNC machine tool, and return whether the download is successful
		/// </summary>
		/// <remarks>
		/// 程序文件的内容必须%开始，%结束，下面是一个非常简单的例子：<br />
		/// %<br />
		/// O0006<br />
		/// G90G10L2P1<br />
		/// M30<br />
		/// %
		/// </remarks>
		/// <param name="program">程序内容信息</param>
		/// <param name="everyWriteSize">每次写入的长度信息</param>
		/// <returns>是否下载成功</returns>
		[HslMqttApi(Description = "Download the NC machining program to the CNC machine tool, and return whether the download is successful")]
		public OperateResult WriteProgramContent(string program, int everyWriteSize = 512)
		{
			OperateResult<Socket> operateResult = CreateSocketAndConnect(IpAddress, Port, ConnectTimeOut);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<int>();
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes());
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult.Content, BulidWriteProgramFilePre());
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			List<byte[]> list = BulidWriteProgram(Encoding.ASCII.GetBytes(program), everyWriteSize);
			for (int i = 0; i < list.Count; i++)
			{
				OperateResult<byte[]> operateResult4 = ReadFromCoreServer(operateResult.Content, list[i], hasResponseData: false);
				if (!operateResult4.IsSuccess)
				{
					return operateResult4;
				}
			}
			OperateResult<byte[]> operateResult5 = ReadFromCoreServer(operateResult.Content, new byte[10] { 160, 160, 160, 160, 0, 1, 19, 1, 0, 0 });
			if (!operateResult5.IsSuccess)
			{
				return operateResult5;
			}
			operateResult.Content?.Close();
			if (operateResult5.Content.Length >= 14)
			{
				int num = base.ByteTransform.TransInt16(operateResult5.Content, 12);
				if (num != 0)
				{
					return new OperateResult<string>(num, StringResources.Language.UnknownError);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// <b>[商业授权]</b> 读取指定程序号的程序内容<br />
		/// <b>[Authorization]</b> Read the program content of the specified program number
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>程序内容</returns>
		[HslMqttApi(Description = "Read the program content of the specified program number")]
		public OperateResult<string> ReadProgram(int program)
		{
			OperateResult<Socket> operateResult = CreateSocketAndConnect(IpAddress, Port, ConnectTimeOut);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<string>();
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes());
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult.Content, BuildReadProgramPre(program));
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult3);
			}
			int num = operateResult3.Content[12] * 256 + operateResult3.Content[13];
			if (num != 0)
			{
				operateResult.Content?.Close();
				return new OperateResult<string>(num, StringResources.Language.UnknownError);
			}
			StringBuilder stringBuilder = new StringBuilder();
			while (true)
			{
				OperateResult<byte[]> operateResult4 = ReadFromCoreServer(operateResult.Content, null);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(operateResult4);
				}
				if (operateResult4.Content[6] == 22)
				{
					stringBuilder.Append(Encoding.ASCII.GetString(operateResult4.Content, 10, operateResult4.Content.Length - 10));
				}
				else if (operateResult4.Content[6] == 23)
				{
					break;
				}
			}
			OperateResult operateResult5 = Send(operateResult.Content, new byte[10] { 160, 160, 160, 160, 0, 1, 23, 2, 0, 0 });
			if (!operateResult5.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult5);
			}
			operateResult.Content?.Close();
			return OperateResult.CreateSuccessResult(stringBuilder.ToString());
		}

		/// <summary>
		/// 根据指定的程序号信息，删除当前的程序信息<br />
		/// According to the designated program number information, delete the current program information
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>是否删除成功</returns>
		[HslMqttApi(Description = "According to the designated program number information, delete the current program information")]
		public OperateResult DeleteProgram(int program)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(5, program, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(operateResult);
			}
			byte[] buffer = ExtraContentArray(operateResult.Content.RemoveBegin(10))[0];
			short num = base.ByteTransform.TransInt16(buffer, 6);
			if (num == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(num, StringResources.Language.UnknownError);
		}

		/// <summary>
		/// 读取当前程序的前台路径<br />
		/// Read the foreground path of the current program
		/// </summary>
		/// <returns>程序的路径信息</returns>
		[HslMqttApi(Description = "Read the foreground path of the current program")]
		public OperateResult<string> ReadCurrentForegroundDir()
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(BuildReadArray(BuildReadSingle(176, 1, 0, 0, 0, 0)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			List<byte[]> list = ExtraContentArray(operateResult.Content.RemoveBegin(10));
			int num = 0;
			for (int i = 14; i < list[0].Length; i++)
			{
				if (list[0][i] == 0)
				{
					num = i;
					break;
				}
			}
			if (num == 0)
			{
				num = list[0].Length;
			}
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(list[0], 14, num - 14));
		}

		/// <summary>
		/// 设置指定路径为当前路径<br />
		/// Set the specified path as the current path
		/// </summary>
		/// <param name="programName">程序名</param>
		/// <returns>结果信息</returns>
		[HslMqttApi(Description = "Set the specified path as the current path")]
		public OperateResult SetDeviceProgsCurr(string programName)
		{
			OperateResult<string> operateResult = ReadCurrentForegroundDir();
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			byte[] array = new byte[256];
			Encoding.ASCII.GetBytes(operateResult.Content + programName).CopyTo(array, 0);
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(BuildReadArray(BuildWriteSingle(186, 0, 0, 0, 0, array)));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			List<byte[]> list = ExtraContentArray(operateResult2.Content.RemoveBegin(10));
			int num = list[0][10] * 256 + list[0][11];
			if (num == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(num, StringResources.Language.UnknownError);
		}

		/// <summary>
		/// 读取机床的当前时间信息<br />
		/// Read the current time information of the machine tool
		/// </summary>
		/// <returns>时间信息</returns>
		[HslMqttApi(Description = "Read the current time information of the machine tool")]
		public OperateResult<DateTime> ReadCurrentDateTime()
		{
			OperateResult<double> operateResult = ReadSystemMacroValue(3011);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(operateResult);
			}
			OperateResult<double> operateResult2 = ReadSystemMacroValue(3012);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(operateResult2);
			}
			string text = Convert.ToInt32(operateResult.Content).ToString();
			string text2 = Convert.ToInt32(operateResult2.Content).ToString().PadLeft(6, '0');
			return OperateResult.CreateSuccessResult(new DateTime(int.Parse(text.Substring(0, 4)), int.Parse(text.Substring(4, 2)), int.Parse(text.Substring(6)), int.Parse(text2.Substring(0, 2)), int.Parse(text2.Substring(2, 2)), int.Parse(text2.Substring(4))));
		}

		/// <summary>
		/// 读取当前的已加工的零件数量<br />
		/// Read the current number of processed parts
		/// </summary>
		/// <returns>已经加工的零件数量</returns>
		[HslMqttApi(Description = "Read the current number of processed parts")]
		public OperateResult<int> ReadCurrentProduceCount()
		{
			OperateResult<double> operateResult = ReadSystemMacroValue(3901);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(operateResult);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(operateResult.Content));
		}

		/// <summary>
		/// 读取期望的加工的零件数量<br />
		/// Read the expected number of processed parts
		/// </summary>
		/// <returns>期望的加工的零件数量</returns>
		[HslMqttApi(Description = "Read the expected number of processed parts")]
		public OperateResult<int> ReadExpectProduceCount()
		{
			OperateResult<double> operateResult = ReadSystemMacroValue(3902);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(operateResult);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(operateResult.Content));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSpindleSpeedAndFeedRate" />
		public async Task<OperateResult<double, double>> ReadSpindleSpeedAndFeedRateAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(164, 3, 0, 0, 0, 0), BuildReadSingle(138, 1, 0, 0, 0, 0), BuildReadSingle(136, 3, 0, 0, 0, 0), BuildReadSingle(136, 4, 0, 0, 0, 0), BuildReadSingle(36, 0, 0, 0, 0, 0), BuildReadSingle(37, 0, 0, 0, 0, 0), BuildReadSingle(164, 3, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double, double>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult(GetFanucDouble(result[5], 14), GetFanucDouble(result[4], 14));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSystemProgramCurrent" />
		public async Task<OperateResult<string, int>> ReadSystemProgramCurrentAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(207, 0, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string, int>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int number = base.ByteTransform.TransInt32(result[0], 14);
			string name = encoding.GetString(result[0].SelectMiddle(18, 36)).TrimEnd(default(char));
			return OperateResult.CreateSuccessResult(name, number);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadLanguage" />
		public async Task<OperateResult<ushort>> ReadLanguageAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(141, 3281, 3281, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			ushort code = base.ByteTransform.TransUInt16(result[0], 24);
			ChangeTextEncoding(code);
			return OperateResult.CreateSuccessResult(code);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSystemMacroValue(System.Int32)" />
		public async Task<OperateResult<double>> ReadSystemMacroValueAsync(int number)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadSystemMacroValueAsync(number, 1));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSystemMacroValue(System.Int32,System.Int32)" />
		public async Task<OperateResult<double[]>> ReadSystemMacroValueAsync(int number, int length)
		{
			int[] lenArray = SoftBasic.SplitIntegerToArray(length, 5);
			int index = number;
			List<byte> result = new List<byte>();
			for (int i = 0; i < lenArray.Length; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(21, index, index + lenArray[i] - 1, 0, 0, 0)));
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<double[]>(read);
				}
				result.AddRange(ExtraContentArray(read.Content.RemoveBegin(10))[0].RemoveBegin(14));
				index += lenArray[i];
			}
			try
			{
				return OperateResult.CreateSuccessResult(GetFanucDouble(result.ToArray(), 0, length));
			}
			catch (Exception ex)
			{
				return new OperateResult<double[]>(ex.Message + " Source:" + result.ToArray().ToHexString(' '));
			}
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCutterNumber" />
		public async Task<OperateResult<int>> ReadCutterNumberAsync()
		{
			OperateResult<double[]> read = await ReadSystemMacroValueAsync(4120, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(read);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(read.Content[0]));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteSystemMacroValue(System.Int32,System.Double[])" />
		public async Task<OperateResult> WriteSystemMacroValueAsync(int number, double[] values)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildWriteSingle(22, number, number + values.Length - 1, 0, 0, values)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string, int>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			if (base.ByteTransform.TransUInt16(result[0], 6) == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(base.ByteTransform.TransUInt16(result[0], 6), "Unknown Error");
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteCutterLengthShapeOffset(System.Int32,System.Double)" />
		public async Task<OperateResult> WriteCutterLengthSharpOffsetAsync(int cutter, double offset)
		{
			return await WriteSystemMacroValueAsync(11000 + cutter, new double[1] { offset });
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteCutterLengthWearOffset(System.Int32,System.Double)" />
		public async Task<OperateResult> WriteCutterLengthWearOffsetAsync(int cutter, double offset)
		{
			return await WriteSystemMacroValueAsync(10000 + cutter, new double[1] { offset });
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteCutterRadiusShapeOffset(System.Int32,System.Double)" />
		public async Task<OperateResult> WriteCutterRadiusSharpOffsetAsync(int cutter, double offset)
		{
			return await WriteSystemMacroValueAsync(13000 + cutter, new double[1] { offset });
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteCutterRadiusWearOffset(System.Int32,System.Double)" />
		public async Task<OperateResult> WriteCutterRadiusWearOffsetAsync(int cutter, double offset)
		{
			return await WriteSystemMacroValueAsync(12000 + cutter, new double[1] { offset });
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadFanucAxisLoad" />
		public async Task<OperateResult<double[]>> ReadFanucAxisLoadAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(164, 2, 0, 0, 0, 0), BuildReadSingle(137, 0, 0, 0, 0, 0), BuildReadSingle(86, 1, 0, 0, 0, 0), BuildReadSingle(164, 2, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<double[]>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult(GetFanucDouble(length: base.ByteTransform.TransUInt16(result[0], 14), content: result[2], index: 14));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSysAllCoors" />
		public async Task<OperateResult<SysAllCoors>> ReadSysAllCoorsAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(164, 0, 0, 0, 0, 0), BuildReadSingle(137, -1, 0, 0, 0, 0), BuildReadSingle(136, 1, 0, 0, 0, 0), BuildReadSingle(136, 2, 0, 0, 0, 0), BuildReadSingle(163, 0, -1, 0, 0, 0), BuildReadSingle(38, 0, -1, 0, 0, 0), BuildReadSingle(38, 1, -1, 0, 0, 0), BuildReadSingle(38, 2, -1, 0, 0, 0), BuildReadSingle(38, 3, -1, 0, 0, 0), BuildReadSingle(164, 0, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysAllCoors>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int length = base.ByteTransform.TransUInt16(result[0], 14);
			return OperateResult.CreateSuccessResult(new SysAllCoors
			{
				Absolute = GetFanucDouble(result[5], 14, length),
				Machine = GetFanucDouble(result[6], 14, length),
				Relative = GetFanucDouble(result[7], 14, length)
			});
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSystemAlarm" />
		public async Task<OperateResult<SysAlarm[]>> ReadSystemAlarmAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(35, -1, 10, 2, 64, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysAlarm[]>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			if (base.ByteTransform.TransUInt16(result[0], 12) > 0)
			{
				int length = (int)base.ByteTransform.TransUInt16(result[0], 12) / 80;
				SysAlarm[] alarms = new SysAlarm[length];
				for (int i = 0; i < alarms.Length; i++)
				{
					alarms[i] = new SysAlarm();
					alarms[i].AlarmId = base.ByteTransform.TransInt32(result[0], 14 + 80 * i);
					alarms[i].Type = base.ByteTransform.TransInt16(result[0], 20 + 80 * i);
					alarms[i].Axis = base.ByteTransform.TransInt16(result[0], 24 + 80 * i);
					ushort msgLength = base.ByteTransform.TransUInt16(result[0], 28 + 80 * i);
					alarms[i].Message = encoding.GetString(result[0], 30 + 80 * i, msgLength);
				}
				return OperateResult.CreateSuccessResult(alarms);
			}
			return OperateResult.CreateSuccessResult(new SysAlarm[0]);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadTimeData(System.Int32)" />
		public async Task<OperateResult<long>> ReadTimeDataAsync(int timeType)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(288, timeType, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<long>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int millisecond = base.ByteTransform.TransInt32(result[0], 18);
			long munite = base.ByteTransform.TransInt32(result[0], 14);
			if (millisecond < 0 || millisecond > 60000)
			{
				millisecond = BitConverter.ToInt32(result[0], 18);
				munite = BitConverter.ToInt32(result[0], 14);
			}
			long seconds = millisecond / 1000;
			return OperateResult.CreateSuccessResult(munite * 60 + seconds);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadAlarmStatus" />
		public async Task<OperateResult<int>> ReadAlarmStatusAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(26, 0, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult((int)base.ByteTransform.TransUInt16(result[0], 16));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadSysStatusInfo" />
		public async Task<OperateResult<SysStatusInfo>> ReadSysStatusInfoAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(25, 0, 0, 0, 0, 0), BuildReadSingle(225, 0, 0, 0, 0, 0), BuildReadSingle(152, 0, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<SysStatusInfo>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult(new SysStatusInfo
			{
				Dummy = base.ByteTransform.TransInt16(result[1], 14),
				TMMode = (short)((result[2].Length >= 16) ? base.ByteTransform.TransInt16(result[2], 14) : 0),
				WorkMode = (CNCWorkMode)base.ByteTransform.TransInt16(result[0], 14),
				RunStatus = (CNCRunStatus)base.ByteTransform.TransInt16(result[0], 16),
				Motion = base.ByteTransform.TransInt16(result[0], 18),
				MSTB = base.ByteTransform.TransInt16(result[0], 20),
				Emergency = base.ByteTransform.TransInt16(result[0], 22),
				Alarm = base.ByteTransform.TransInt16(result[0], 24),
				Edit = base.ByteTransform.TransInt16(result[0], 26)
			});
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadProgramList" />
		public async Task<OperateResult<int[]>> ReadProgramListAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(6, 1, 19, 0, 0, 0)));
			OperateResult<byte[]> check = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(6, 6667, 19, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int[]>(read);
			}
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int[]>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int length = (result[0].Length - 14) / 72;
			int[] programs = new int[length];
			for (int i = 0; i < length; i++)
			{
				programs[i] = base.ByteTransform.TransInt32(result[0], 14 + 72 * i);
			}
			return OperateResult.CreateSuccessResult(programs);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCutterInfos(System.Int32)" />
		public async Task<OperateResult<CutterInfo[]>> ReadCutterInfosAsync(int cutterNumber = 24)
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 0, 0, 0)));
			if (!read1.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(read1);
			}
			OperateResult<byte[]> read2 = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 1, 0, 0)));
			if (!read2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(read2);
			}
			OperateResult<byte[]> read3 = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 2, 0, 0)));
			if (!read3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(read3);
			}
			OperateResult<byte[]> read4 = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(8, 1, cutterNumber, 3, 0, 0)));
			if (!read4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<CutterInfo[]>(read4);
			}
			return ExtraCutterInfos(read1.Content, read2.Content, read3.Content, read4.Content, cutterNumber);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadRData(System.Int32,System.Int32)" />
		public async Task<OperateResult<byte[]>> ReadRDataAsync(int start, int end)
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(BuildReadArray(BuildReadMulti(2, 32769, start, end, 5, 0, 0)));
			if (!read1.IsSuccess)
			{
				return read1;
			}
			List<byte[]> result = ExtraContentArray(read1.Content.RemoveBegin(10));
			return OperateResult.CreateSuccessResult(HslExtension.SelectMiddle(length: base.ByteTransform.TransUInt16(result[0], 12), value: result[0], index: 14));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadDeviceWorkPiecesSize" />
		public async Task<OperateResult<double[]>> ReadDeviceWorkPiecesSizeAsync()
		{
			return await ReadSystemMacroValueAsync(601, 20);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCurrentForegroundDir" />
		public async Task<OperateResult<string>> ReadCurrentForegroundDirAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(176, 1, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int index = 0;
			for (int i = 14; i < result[0].Length; i++)
			{
				if (result[0][i] == 0)
				{
					index = i;
					break;
				}
			}
			if (index == 0)
			{
				index = result[0].Length;
			}
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(result[0], 14, index - 14));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.SetDeviceProgsCurr(System.String)" />
		public async Task<OperateResult> SetDeviceProgsCurrAsync(string programName)
		{
			OperateResult<string> path = await ReadCurrentForegroundDirAsync();
			if (!path.IsSuccess)
			{
				return path;
			}
			byte[] buffer = new byte[256];
			Encoding.ASCII.GetBytes(path.Content + programName).CopyTo(buffer, 0);
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildWriteSingle(186, 0, 0, 0, 0, buffer)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			List<byte[]> result = ExtraContentArray(read.Content.RemoveBegin(10));
			int status = result[0][10] * 256 + result[0][11];
			if (status == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(status, StringResources.Language.UnknownError);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCurrentDateTime" />
		public async Task<OperateResult<DateTime>> ReadCurrentDateTimeAsync()
		{
			OperateResult<double> read1 = await ReadSystemMacroValueAsync(3011);
			if (!read1.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(read1);
			}
			OperateResult<double> read2 = await ReadSystemMacroValueAsync(3012);
			if (!read2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(read2);
			}
			string date = Convert.ToInt32(read1.Content).ToString();
			string time = Convert.ToInt32(read2.Content).ToString().PadLeft(6, '0');
			return OperateResult.CreateSuccessResult(new DateTime(int.Parse(date.Substring(0, 4)), int.Parse(date.Substring(4, 2)), int.Parse(date.Substring(6)), int.Parse(time.Substring(0, 2)), int.Parse(time.Substring(2, 2)), int.Parse(time.Substring(4))));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCurrentProduceCount" />
		public async Task<OperateResult<int>> ReadCurrentProduceCountAsync()
		{
			OperateResult<double> read = await ReadSystemMacroValueAsync(3901);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(read);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(read.Content));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadExpectProduceCount" />
		public async Task<OperateResult<int>> ReadExpectProduceCountAsync()
		{
			OperateResult<double> read = await ReadSystemMacroValueAsync(3902);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int>(read);
			}
			return OperateResult.CreateSuccessResult(Convert.ToInt32(read.Content));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadCurrentProgram" />
		public async Task<OperateResult<string>> ReadCurrentProgramAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(32, 1428, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			byte[] result = ExtraContentArray(read.Content.RemoveBegin(10))[0];
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(result, 18, result.Length - 18));
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.SetCurrentProgram(System.UInt16)" />
		public async Task<OperateResult> SetCurrentProgramAsync(ushort programNum)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(3, programNum, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(read);
			}
			byte[] result = ExtraContentArray(read.Content.RemoveBegin(10))[0];
			short err = base.ByteTransform.TransInt16(result, 6);
			if (err == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(err, StringResources.Language.UnknownError);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.StartProcessing" />
		public async Task<OperateResult> StartProcessingAsync()
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(1, 0, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(read);
			}
			byte[] result = ExtraContentArray(read.Content.RemoveBegin(10))[0];
			short err = base.ByteTransform.TransInt16(result, 6);
			if (err == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(err, StringResources.Language.UnknownError);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteProgramFile(System.String)" />
		public async Task<OperateResult> WriteProgramFileAsync(string file)
		{
			string content = File.ReadAllText(file);
			return await WriteProgramContentAsync(content);
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.WriteProgramContent(System.String,System.Int32)" />
		public async Task<OperateResult> WriteProgramContentAsync(string program, int everyWriteSize = 512)
		{
			OperateResult<Socket> socket = await CreateSocketAndConnectAsync(IpAddress, Port, ConnectTimeOut);
			if (!socket.IsSuccess)
			{
				return socket.ConvertFailed<int>();
			}
			OperateResult<byte[]> ini1 = await ReadFromCoreServerAsync(socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes());
			if (!ini1.IsSuccess)
			{
				return ini1;
			}
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(socket.Content, BulidWriteProgramFilePre());
			if (!read1.IsSuccess)
			{
				return read1;
			}
			List<byte[]> contents = BulidWriteProgram(Encoding.ASCII.GetBytes(program), everyWriteSize);
			for (int i = 0; i < contents.Count; i++)
			{
				OperateResult<byte[]> read2 = await ReadFromCoreServerAsync(socket.Content, contents[i], hasResponseData: false);
				if (!read2.IsSuccess)
				{
					return read2;
				}
			}
			OperateResult<byte[]> read3 = await ReadFromCoreServerAsync(socket.Content, new byte[10] { 160, 160, 160, 160, 0, 1, 19, 1, 0, 0 });
			if (!read3.IsSuccess)
			{
				return read3;
			}
			socket.Content?.Close();
			if (read3.Content.Length >= 14)
			{
				int err = base.ByteTransform.TransInt16(read3.Content, 12);
				if (err != 0)
				{
					return new OperateResult<string>(err, StringResources.Language.UnknownError);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.ReadProgram(System.Int32)" />
		public async Task<OperateResult<string>> ReadProgramAsync(int program)
		{

			OperateResult<Socket> socket = await CreateSocketAndConnectAsync(IpAddress, Port, ConnectTimeOut);
			if (!socket.IsSuccess)
			{
				return socket.ConvertFailed<string>();
			}
			OperateResult<byte[]> ini1 = await ReadFromCoreServerAsync(socket.Content, "a0 a0 a0 a0 00 01 01 01 00 02 00 01".ToHexBytes());
			if (!ini1.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(ini1);
			}
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(socket.Content, BuildReadProgramPre(program));
			if (!read1.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read1);
			}
			int err = read1.Content[12] * 256 + read1.Content[13];
			if (err != 0)
			{
				socket.Content?.Close();
				return new OperateResult<string>(err, StringResources.Language.UnknownError);
			}
			StringBuilder sb = new StringBuilder();
			while (true)
			{
				OperateResult<byte[]> read2 = await ReadFromCoreServerAsync(socket.Content, null);
				if (!read2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(read2);
				}
				if (read2.Content[6] == 22)
				{
					sb.Append(Encoding.ASCII.GetString(read2.Content, 10, read2.Content.Length - 10));
				}
				else if (read2.Content[6] == 23)
				{
					break;
				}
			}
			OperateResult send = await SendAsync(socket.Content, new byte[10] { 160, 160, 160, 160, 0, 1, 23, 2, 0, 0 });
			if (!send.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(send);
			}
			socket.Content?.Close();
			return OperateResult.CreateSuccessResult(sb.ToString());
		}

		/// <inheritdoc cref="M:Communication.CNC.Fanuc.FanucSeries0i.DeleteProgram(System.Int32)" />
		public async Task<OperateResult> DeleteProgramAsync(int program)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(BuildReadArray(BuildReadSingle(5, program, 0, 0, 0, 0)));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<int, string>(read);
			}
			byte[] result = ExtraContentArray(read.Content.RemoveBegin(10))[0];
			short err = base.ByteTransform.TransInt16(result, 6);
			if (err == 0)
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult(err, StringResources.Language.UnknownError);
		}

		/// <summary>
		/// 构建读取一个命令的数据内容
		/// </summary>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="e">第五个参数内容</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadSingle(ushort code, int a, int b, int c, int d, int e)
		{
			return BuildReadMulti(1, code, a, b, c, d, e);
		}

		/// <summary>
		/// 构建读取多个命令的数据内容
		/// </summary>
		/// <param name="mode">模式</param>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="e">第五个参数内容</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadMulti(ushort mode, ushort code, int a, int b, int c, int d, int e)
		{
			byte[] array = new byte[28];
			array[1] = 28;
			base.ByteTransform.TransByte(mode).CopyTo(array, 2);
			array[5] = 1;
			base.ByteTransform.TransByte(code).CopyTo(array, 6);
			base.ByteTransform.TransByte(a).CopyTo(array, 8);
			base.ByteTransform.TransByte(b).CopyTo(array, 12);
			base.ByteTransform.TransByte(c).CopyTo(array, 16);
			base.ByteTransform.TransByte(d).CopyTo(array, 20);
			base.ByteTransform.TransByte(e).CopyTo(array, 24);
			return array;
		}

		/// <summary>
		/// 创建写入byte[]数组的报文信息
		/// </summary>
		/// <param name="code">命令码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="data">等待写入的byte数组信息</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildWriteSingle(ushort code, int a, int b, int c, int d, byte[] data)
		{
			byte[] array = new byte[28 + data.Length];
			base.ByteTransform.TransByte((ushort)array.Length).CopyTo(array, 0);
			array[3] = 1;
			array[5] = 1;
			base.ByteTransform.TransByte(code).CopyTo(array, 6);
			base.ByteTransform.TransByte(a).CopyTo(array, 8);
			base.ByteTransform.TransByte(b).CopyTo(array, 12);
			base.ByteTransform.TransByte(c).CopyTo(array, 16);
			base.ByteTransform.TransByte(d).CopyTo(array, 20);
			base.ByteTransform.TransByte(data.Length).CopyTo(array, 24);
			if (data.Length != 0)
			{
				data.CopyTo(array, 28);
			}
			return array;
		}

		/// <summary>
		/// 创建写入单个double数组的报文信息
		/// </summary>
		/// <param name="code">功能码</param>
		/// <param name="a">第一个参数内容</param>
		/// <param name="b">第二个参数内容</param>
		/// <param name="c">第三个参数内容</param>
		/// <param name="d">第四个参数内容</param>
		/// <param name="data">等待写入的double数组信息</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildWriteSingle(ushort code, int a, int b, int c, int d, double[] data)
		{
			byte[] array = new byte[data.Length * 8];
			for (int i = 0; i < data.Length; i++)
			{
				CreateFromFanucDouble(data[i]).CopyTo(array, 0);
			}
			return BuildWriteSingle(code, a, b, c, d, array);
		}

		/// <summary>
		/// 创建多个命令报文的总报文信息
		/// </summary>
		/// <param name="commands">报文命令的数组</param>
		/// <returns>总报文信息</returns>
		private byte[] BuildReadArray(params byte[][] commands)
		{
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.Write(new byte[10] { 160, 160, 160, 160, 0, 1, 33, 1, 0, 30 }, 0, 10);
			memoryStream.Write(base.ByteTransform.TransByte((ushort)commands.Length), 0, 2);
			for (int i = 0; i < commands.Length; i++)
			{
				memoryStream.Write(commands[i], 0, commands[i].Length);
			}
			byte[] array = memoryStream.ToArray();
			base.ByteTransform.TransByte((ushort)(array.Length - 10)).CopyTo(array, 8);
			return array;
		}

		private byte[] BulidWriteProgramFilePre()
		{
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.Write(new byte[10] { 160, 160, 160, 160, 0, 1, 17, 1, 2, 4 }, 0, 10);
			memoryStream.Write(new byte[4] { 0, 0, 0, 1 }, 0, 4);
			for (int i = 0; i < 512; i++)
			{
				memoryStream.WriteByte(0);
			}
			return memoryStream.ToArray();
		}

		/// <summary>
		/// 创建读取运行程序的报文信息
		/// </summary>
		/// <param name="program">程序号</param>
		/// <returns>总报文</returns>
		private byte[] BuildReadProgramPre(int program)
		{
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.Write(new byte[10] { 160, 160, 160, 160, 0, 1, 21, 1, 2, 4 }, 0, 10);
			memoryStream.Write(new byte[4] { 0, 0, 0, 1 }, 0, 4);
			for (int i = 0; i < 512; i++)
			{
				memoryStream.WriteByte(0);
			}
			byte[] array = memoryStream.ToArray();
			string s = "O" + program + "-O" + program;
			Encoding.ASCII.GetBytes(s).CopyTo(array, 14);
			return array;
		}

		private List<byte[]> BulidWriteProgram(byte[] program, int everyWriteSize)
		{
			List<byte[]> list = new List<byte[]>();
			int[] array = SoftBasic.SplitIntegerToArray(program.Length, everyWriteSize);
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				MemoryStream memoryStream = new MemoryStream();
				memoryStream.Write(new byte[10] { 160, 160, 160, 160, 0, 1, 18, 4, 0, 0 }, 0, 10);
				memoryStream.Write(program, num, array[i]);
				byte[] array2 = memoryStream.ToArray();
				base.ByteTransform.TransByte((ushort)(array2.Length - 10)).CopyTo(array2, 8);
				list.Add(array2);
				num += array[i];
			}
			return list;
		}

		/// <summary>
		/// 从机床返回的数据里解析出实际的数据内容，去除了一些多余的信息报文。
		/// </summary>
		/// <param name="content">返回的报文信息</param>
		/// <returns>解析之后的报文信息</returns>
		private List<byte[]> ExtraContentArray(byte[] content)
		{
			List<byte[]> list = new List<byte[]>();
			int num = base.ByteTransform.TransUInt16(content, 0);
			int num2 = 2;
			for (int i = 0; i < num; i++)
			{
				ushort num3 = base.ByteTransform.TransUInt16(content, num2);
				list.Add(content.SelectMiddle(num2 + 2, num3 - 2));
				num2 += num3;
			}
			return list;
		}

		private OperateResult<CutterInfo[]> ExtraCutterInfos(byte[] content1, byte[] content2, byte[] content3, byte[] content4, int cutterNumber)
		{
			List<byte[]> list = ExtraContentArray(content1.RemoveBegin(10));
			List<byte[]> list2 = ExtraContentArray(content2.RemoveBegin(10));
			List<byte[]> list3 = ExtraContentArray(content3.RemoveBegin(10));
			List<byte[]> list4 = ExtraContentArray(content4.RemoveBegin(10));
			bool flag = base.ByteTransform.TransInt16(list[0], 6) == 0;
			bool flag2 = base.ByteTransform.TransInt16(list2[0], 6) == 0;
			bool flag3 = base.ByteTransform.TransInt16(list3[0], 6) == 0;
			bool flag4 = base.ByteTransform.TransInt16(list4[0], 6) == 0;
			CutterInfo[] array = new CutterInfo[cutterNumber];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new CutterInfo();
				array[i].LengthSharpOffset = (flag ? GetFanucDouble(list[0], 14 + 8 * i) : double.NaN);
				array[i].LengthWearOffset = (flag2 ? GetFanucDouble(list2[0], 14 + 8 * i) : double.NaN);
				array[i].RadiusSharpOffset = (flag3 ? GetFanucDouble(list3[0], 14 + 8 * i) : double.NaN);
				array[i].RadiusWearOffset = (flag4 ? GetFanucDouble(list4[0], 14 + 8 * i) : double.NaN);
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FanucSeries0i[{IpAddress}:{Port}]";
		}
	}
}

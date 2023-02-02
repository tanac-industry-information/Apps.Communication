using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apps.Communication.Robot.ABB
{
	/// <summary>
	/// ABB机器人的web api接口的客户端，可以方便快速的获取到abb机器人的一些数据信息<br />
	/// The client of ABB robot's web API interface can easily and quickly obtain some data information of ABB robot
	/// </summary>
	/// <remarks>
	/// 参考的界面信息是：http://developercenter.robotstudio.com/webservice/api_reference
	///
	/// 关于额外的地址说明，如果想要查看，可以调用<see cref="M:Communication.Robot.ABB.ABBWebApiClient.GetSelectStrings" /> 返回字符串列表来看看。
	/// </remarks>
	public class ABBWebApiClient : NetworkWebApiRobotBase, IRobotNet
	{
		/// <summary>
		/// 使用指定的ip地址来初始化对象<br />
		/// Initializes the object using the specified IP address
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		public ABBWebApiClient(string ipAddress)
			: base(ipAddress)
		{
		}

		/// <summary>
		/// 使用指定的ip地址和端口号来初始化对象<br />
		/// Initializes the object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		public ABBWebApiClient(string ipAddress, int port)
			: base(ipAddress, port)
		{
		}

		/// <summary>
		/// 使用指定的ip地址，端口号，用户名，密码来初始化对象<br />
		/// Initialize the object with the specified IP address, port number, username, and password
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="port">端口号信息</param>
		/// <param name="name">用户名</param>
		/// <param name="password">密码</param>
		public ABBWebApiClient(string ipAddress, int port, string name, string password)
			: base(ipAddress, port, name, password)
		{
		}

		/// <inheritdoc />
		[HslMqttApi(ApiTopic = "ReadRobotByte", Description = "Read the other side of the data information, usually designed for the GET method information.If you start with url=, you are using native address access")]
		public override OperateResult<byte[]> Read(string address)
		{
			return base.Read(address);
		}

		/// <inheritdoc />
		[HslMqttApi(ApiTopic = "ReadRobotString", Description = "The string data information that reads the other party information, usually designed for the GET method information.If you start with url=, you are using native address access")]
		public override OperateResult<string> ReadString(string address)
		{
			return base.ReadString(address);
		}

		/// <inheritdoc />
		[HslMqttApi(ApiTopic = "WriteRobotByte", Description = "Using POST to request data information from the other party, we need to start with url= to indicate that we are using native address access")]
		public override OperateResult Write(string address, byte[] value)
		{
			return base.Write(address, value);
		}

		/// <inheritdoc />
		[HslMqttApi(ApiTopic = "WriteRobotString", Description = "Using POST to request data information from the other party, we need to start with url= to indicate that we are using native address access")]
		public override OperateResult Write(string address, string value)
		{
			return base.Write(address, value);
		}

		/// <inheritdoc />
		protected override OperateResult<string> ReadByAddress(string address)
		{
			if (address.ToUpper() == "ErrorState".ToUpper())
			{
				return GetErrorState();
			}
			if (address.ToUpper() == "jointtarget".ToUpper())
			{
				return GetJointTarget();
			}
			if (address.ToUpper() == "PhysicalJoints".ToUpper())
			{
				return GetJointTarget();
			}
			if (address.ToUpper() == "SpeedRatio".ToUpper())
			{
				return GetSpeedRatio();
			}
			if (address.ToUpper() == "OperationMode".ToUpper())
			{
				return GetOperationMode();
			}
			if (address.ToUpper() == "CtrlState".ToUpper())
			{
				return GetCtrlState();
			}
			if (address.ToUpper() == "ioin".ToUpper())
			{
				return GetIOIn();
			}
			if (address.ToUpper() == "ioout".ToUpper())
			{
				return GetIOOut();
			}
			if (address.ToUpper() == "io2in".ToUpper())
			{
				return GetIO2In();
			}
			if (address.ToUpper() == "io2out".ToUpper())
			{
				return GetIO2Out();
			}
			if (address.ToUpper().StartsWith("log".ToUpper()))
			{
				if (address.Length > 3 && int.TryParse(address.Substring(3), out var result))
				{
					return GetLog(result);
				}
				return GetLog();
			}
			if (address.ToUpper() == "system".ToUpper())
			{
				return GetSystem();
			}
			if (address.ToUpper() == "robtarget".ToUpper())
			{
				return GetRobotTarget();
			}
			if (address.ToUpper() == "ServoEnable".ToUpper())
			{
				return GetServoEnable();
			}
			if (address.ToUpper() == "RapidExecution".ToUpper())
			{
				return GetRapidExecution();
			}
			if (address.ToUpper() == "RapidTasks".ToUpper())
			{
				return GetRapidTasks();
			}
			return base.ReadByAddress(address);
		}

		/// <inheritdoc />
		protected override async Task<OperateResult<string>> ReadByAddressAsync(string address)
		{
			if (address.ToUpper() == "ErrorState".ToUpper())
			{
				return await GetErrorStateAsync();
			}
			if (address.ToUpper() == "jointtarget".ToUpper())
			{
				return await GetJointTargetAsync();
			}
			if (address.ToUpper() == "PhysicalJoints".ToUpper())
			{
				return await GetJointTargetAsync();
			}
			if (address.ToUpper() == "SpeedRatio".ToUpper())
			{
				return await GetSpeedRatioAsync();
			}
			if (address.ToUpper() == "OperationMode".ToUpper())
			{
				return await GetOperationModeAsync();
			}
			if (address.ToUpper() == "CtrlState".ToUpper())
			{
				return await GetCtrlStateAsync();
			}
			if (address.ToUpper() == "ioin".ToUpper())
			{
				return await GetIOInAsync();
			}
			if (address.ToUpper() == "ioout".ToUpper())
			{
				return await GetIOOutAsync();
			}
			if (address.ToUpper() == "io2in".ToUpper())
			{
				return await GetIO2InAsync();
			}
			if (address.ToUpper() == "io2out".ToUpper())
			{
				return await GetIO2OutAsync();
			}
			if (address.ToUpper().StartsWith("log".ToUpper()))
			{
				if (address.Length > 3 && int.TryParse(address.Substring(3), out var length))
				{
					return await GetLogAsync(length);
				}
				return await GetLogAsync();
			}
			if (address.ToUpper() == "system".ToUpper())
			{
				return await GetSystemAsync();
			}
			if (address.ToUpper() == "robtarget".ToUpper())
			{
				return await GetRobotTargetAsync();
			}
			if (address.ToUpper() == "ServoEnable".ToUpper())
			{
				return await GetServoEnableAsync();
			}
			if (address.ToUpper() == "RapidExecution".ToUpper())
			{
				return await GetRapidExecutionAsync();
			}
			if (address.ToUpper() == "RapidTasks".ToUpper())
			{
				return await GetRapidTasksAsync();
			}
			return await base.ReadByAddressAsync(address);
		}

		/// <summary>
		/// 获取当前支持的读取的地址列表<br />
		/// Gets a list of addresses for currently supported reads
		/// </summary>
		/// <returns>数组信息</returns>
		public static List<string> GetSelectStrings()
		{
			return new List<string>
			{
				"ErrorState", "jointtarget", "PhysicalJoints", "SpeedRatio", "OperationMode", "CtrlState", "ioin", "ioout", "io2in", "io2out",
				"log", "system", "robtarget", "ServoEnable", "RapidExecution", "RapidTasks"
			};
		}

		private OperateResult<string> AnalysisClassAttribute(string content, string[] atts)
		{
			JObject jObject = new JObject();
			for (int i = 0; i < atts.Length; i++)
			{
				Match match = Regex.Match(content, "<span class=\"" + atts[i] + "\">[^<]*");
				if (!match.Success)
				{
					return new OperateResult<string>(content);
				}
				jObject.Add(atts[i], new JValue(match.Value.Substring(15 + atts[i].Length)));
			}
			return OperateResult.CreateSuccessResult(jObject.ToString());
		}

		private OperateResult<string> AnalysisSystem(string content)
		{
			return AnalysisClassAttribute(content, new string[11]
			{
				"major", "minor", "build", "title", "type", "description", "date", "mctimestamp", "name", "sysid",
				"starttm"
			});
		}

		private OperateResult<string> AnalysisRobotTarget(string content)
		{
			return AnalysisClassAttribute(content, new string[6] { "x", "y", "z", "q1", "q2", "q3" });
		}

		/// <summary>
		/// 获取当前的控制状态，Content属性就是机器人的控制信息<br />
		/// Get the current control state. The Content attribute is the control information of the robot
		/// </summary>
		/// <returns>带有状态信息的结果类对象</returns>
		[HslMqttApi(Description = "Get the current control state. The Content attribute is the control information of the robot")]
		public OperateResult<string> GetCtrlState()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/panel/ctrlstate");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"ctrlstate\">[^<]+");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(24));
		}

		/// <summary>
		/// 获取当前的错误状态，Content属性就是机器人的状态信息<br />
		/// Gets the current error state. The Content attribute is the state information of the robot
		/// </summary>
		/// <returns>带有状态信息的结果类对象</returns>
		[HslMqttApi(Description = "Gets the current error state. The Content attribute is the state information of the robot")]
		public OperateResult<string> GetErrorState()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/motionsystem/errorstate");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"err-state\">[^<]+");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(24));
		}

		/// <summary>
		/// 获取当前机器人的物理关节点信息，返回json格式的关节信息<br />
		/// Get the physical node information of the current robot and return the joint information in json format
		/// </summary>
		/// <returns>带有关节信息的结果类对象</returns>
		[HslMqttApi(Description = "Get the physical node information of the current robot and return the joint information in json format")]
		public OperateResult<string> GetJointTarget()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/motionsystem/mechunits/ROB_1/jointtarget");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			MatchCollection matchCollection = Regex.Matches(operateResult.Content, "<span class=\"rax[^<]*");
			if (matchCollection.Count != 6)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			double[] array = new double[6];
			for (int i = 0; i < matchCollection.Count; i++)
			{
				if (matchCollection[i].Length > 17)
				{
					array[i] = double.Parse(matchCollection[i].Value.Substring(20));
				}
			}
			return OperateResult.CreateSuccessResult(JArray.FromObject(array).ToString(Formatting.None));
		}

		/// <summary>
		/// 获取当前机器人的速度配比信息<br />
		/// Get the speed matching information of the current robot
		/// </summary>
		/// <returns>带有速度信息的结果类对象</returns>
		[HslMqttApi(Description = "Get the speed matching information of the current robot")]
		public OperateResult<string> GetSpeedRatio()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/panel/speedratio");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"speedratio\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(25));
		}

		/// <summary>
		/// 获取当前机器人的工作模式<br />
		/// Gets the current working mode of the robot
		/// </summary>
		/// <returns>带有工作模式信息的结果类对象</returns>
		[HslMqttApi(Description = "Gets the current working mode of the robot")]
		public OperateResult<string> GetOperationMode()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/panel/opmode");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"opmode\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <summary>
		/// 获取当前机器人的本机的输入IO<br />
		/// Gets the input IO of the current robot's native
		/// </summary>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi(Description = "Gets the input IO of the current robot's native")]
		public OperateResult<string> GetIOIn()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/iosystem/devices/D652_10");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"indata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <summary>
		/// 获取当前机器人的本机的输出IO<br />
		/// Gets the output IO of the current robot's native
		/// </summary>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi(Description = "Gets the output IO of the current robot's native")]
		public OperateResult<string> GetIOOut()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/iosystem/devices/D652_10");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"outdata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(22));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOIn" />
		[HslMqttApi(Description = "Gets the input IO2 of the current robot's native")]
		public OperateResult<string> GetIO2In()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/iosystem/devices/BK5250");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"indata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOOut" />
		[HslMqttApi(Description = "Gets the output IO2 of the current robot's native")]
		public OperateResult<string> GetIO2Out()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/iosystem/devices/BK5250");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<span class=\"outdata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(22));
		}

		/// <summary>
		/// 获取当前机器人的日志记录，默认记录为10条<br />
		/// Gets the log record for the current robot, which is 10 by default
		/// </summary>
		/// <param name="logCount">读取的最大的日志总数</param>
		/// <returns>带有IO信息的结果类对象</returns>
		[HslMqttApi(Description = "Gets the log record for the current robot, which is 10 by default")]
		public OperateResult<string> GetLog(int logCount = 10)
		{
			OperateResult<string> operateResult = ReadString("url=/rw/elog/0?lang=zh&amp;resource=title");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			MatchCollection matchCollection = Regex.Matches(operateResult.Content, "<li class=\"elog-message-li\" title=\"/rw/elog/0/[0-9]+\">[\\S\\s]+?</li>");
			JArray jArray = new JArray();
			for (int i = 0; i < matchCollection.Count && i < logCount; i++)
			{
				Match match = Regex.Match(matchCollection[i].Value, "[0-9]+\"");
				JObject jObject = new JObject();
				jObject["id"] = match.Value.TrimEnd('"');
				foreach (XElement item in XElement.Parse(matchCollection[i].Value).Elements("span"))
				{
					jObject[item.Attribute("class").Value] = item.Value;
				}
				jArray.Add(jObject);
			}
			return OperateResult.CreateSuccessResult(jArray.ToString());
		}

		/// <summary>
		/// 获取当前机器人的系统信息，版本号，唯一ID等信息<br />
		/// Get the current robot's system information, version number, unique ID and other information
		/// </summary>
		/// <returns>系统的基本信息</returns>
		[HslMqttApi(Description = "Get the current robot's system information, version number, unique ID and other information")]
		public OperateResult<string> GetSystem()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/system");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return AnalysisSystem(operateResult.Content);
		}

		/// <summary>
		/// 获取机器人的目标坐标信息<br />
		/// Get the current robot's target information
		/// </summary>
		/// <returns>系统的基本信息</returns>
		[HslMqttApi(Description = "Get the current robot's target information")]
		public OperateResult<string> GetRobotTarget()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/motionsystem/mechunits/ROB_1/robtarget");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return AnalysisRobotTarget(operateResult.Content);
		}

		/// <summary>
		/// 获取当前机器人的伺服使能状态<br />
		/// Get the current robot servo enable state
		/// </summary>
		/// <returns>机器人的伺服使能状态</returns>
		[HslMqttApi(Description = "Get the current robot servo enable state")]
		public OperateResult<string> GetServoEnable()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/iosystem/signals/Local/DRV_1/DRV1K1");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<li class=\"ios-signal\"[\\S\\s]+?</li>");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			JObject jObject = new JObject();
			foreach (XElement item in XElement.Parse(match.Value).Elements("span"))
			{
				jObject[item.Attribute("class").Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult(jObject.ToString());
		}

		/// <summary>
		/// 获取当前机器人的当前程序运行状态<br />
		/// Get the current program running status of the current robot
		/// </summary>
		/// <returns>机器人的当前的程序运行状态</returns>
		[HslMqttApi(Description = "Get the current program running status of the current robot")]
		public OperateResult<string> GetRapidExecution()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/rapid/execution");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			Match match = Regex.Match(operateResult.Content, "<li class=\"rap-execution\"[\\S\\s]+?</li>");
			if (!match.Success)
			{
				return new OperateResult<string>(operateResult.Content);
			}
			JObject jObject = new JObject();
			foreach (XElement item in XElement.Parse(match.Value).Elements("span"))
			{
				jObject[item.Attribute("class").Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult(jObject.ToString());
		}

		/// <summary>
		/// 获取当前机器人的任务列表<br />
		/// Get the task list of the current robot
		/// </summary>
		/// <returns>任务信息的列表</returns>
		[HslMqttApi(Description = "Get the task list of the current robot")]
		public OperateResult<string> GetRapidTasks()
		{
			OperateResult<string> operateResult = ReadString("url=/rw/rapid/tasks");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			MatchCollection matchCollection = Regex.Matches(operateResult.Content, "<li class=\"rap-task-li\" [\\S\\s]+?</li>");
			JArray jArray = new JArray();
			for (int i = 0; i < matchCollection.Count; i++)
			{
				JObject jObject = new JObject();
				foreach (XElement item in XElement.Parse(matchCollection[i].Value).Elements("span"))
				{
					jObject[item.Attribute("class").Value] = item.Value;
				}
				jArray.Add(jObject);
			}
			return OperateResult.CreateSuccessResult(jArray.ToString());
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetCtrlState" />
		public async Task<OperateResult<string>> GetCtrlStateAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/panel/ctrlstate");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"ctrlstate\">[^<]+");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(24));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetErrorState" />
		public async Task<OperateResult<string>> GetErrorStateAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/motionsystem/errorstate");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"err-state\">[^<]+");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(24));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetJointTarget" />
		public async Task<OperateResult<string>> GetJointTargetAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/motionsystem/mechunits/ROB_1/jointtarget");
			if (!read.IsSuccess)
			{
				return read;
			}
			MatchCollection mc = Regex.Matches(read.Content, "<span class=\"rax[^<]*");
			if (mc.Count != 6)
			{
				return new OperateResult<string>(read.Content);
			}
			double[] joints = new double[6];
			for (int i = 0; i < mc.Count; i++)
			{
				if (mc[i].Length > 17)
				{
					joints[i] = double.Parse(mc[i].Value.Substring(20));
				}
			}
			return OperateResult.CreateSuccessResult(JArray.FromObject(joints).ToString(Formatting.None));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetSpeedRatio" />
		public async Task<OperateResult<string>> GetSpeedRatioAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/panel/speedratio");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"speedratio\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(25));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetOperationMode" />
		public async Task<OperateResult<string>> GetOperationModeAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/panel/opmode");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"opmode\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOIn" />
		public async Task<OperateResult<string>> GetIOInAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/iosystem/devices/D652_10");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"indata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOOut" />
		public async Task<OperateResult<string>> GetIOOutAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/iosystem/devices/D652_10");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"outdata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(22));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOIn" />
		public async Task<OperateResult<string>> GetIO2InAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/iosystem/devices/BK5250");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"indata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(21));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetIOOut" />
		public async Task<OperateResult<string>> GetIO2OutAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/iosystem/devices/BK5250");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<span class=\"outdata\">[^<]*");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			return OperateResult.CreateSuccessResult(match.Value.Substring(22));
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetLog(System.Int32)" />
		public async Task<OperateResult<string>> GetLogAsync(int logCount = 10)
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/elog/0?lang=zh&amp;resource=title");
			if (!read.IsSuccess)
			{
				return read;
			}
			MatchCollection matchs = Regex.Matches(read.Content, "<li class=\"elog-message-li\" title=\"/rw/elog/0/[0-9]+\">[\\S\\s]+?</li>");
			JArray jArray = new JArray();
			for (int i = 0; i < matchs.Count && i < logCount; i++)
			{
				Match id = Regex.Match(matchs[i].Value, "[0-9]+\"");
				JObject json = new JObject { ["id"] = id.Value.TrimEnd('"') };
				foreach (XElement item in XElement.Parse(matchs[i].Value).Elements("span"))
				{
					json[item.Attribute("class").Value] = item.Value;
				}
				jArray.Add(json);
			}
			return OperateResult.CreateSuccessResult(jArray.ToString());
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetSystem" />
		public async Task<OperateResult<string>> GetSystemAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/system");
			if (!read.IsSuccess)
			{
				return read;
			}
			return AnalysisSystem(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetRobotTarget" />
		public async Task<OperateResult<string>> GetRobotTargetAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/motionsystem/mechunits/ROB_1/robtarget");
			if (!read.IsSuccess)
			{
				return read;
			}
			return AnalysisRobotTarget(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetServoEnable" />
		public async Task<OperateResult<string>> GetServoEnableAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/iosystem/signals/Local/DRV_1/DRV1K1");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<li class=\"ios-signal\"[\\S\\s]+?</li>");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			JObject json = new JObject();
			foreach (XElement item in XElement.Parse(match.Value).Elements("span"))
			{
				json[item.Attribute("class").Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult(json.ToString());
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetRapidExecution" />
		public async Task<OperateResult<string>> GetRapidExecutionAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/rapid/execution");
			if (!read.IsSuccess)
			{
				return read;
			}
			Match match = Regex.Match(read.Content, "<li class=\"rap-execution\"[\\S\\s]+?</li>");
			if (!match.Success)
			{
				return new OperateResult<string>(read.Content);
			}
			JObject json = new JObject();
			foreach (XElement item in XElement.Parse(match.Value).Elements("span"))
			{
				json[item.Attribute("class").Value] = item.Value;
			}
			return OperateResult.CreateSuccessResult(json.ToString());
		}

		/// <inheritdoc cref="M:Communication.Robot.ABB.ABBWebApiClient.GetRapidTasks" />
		public async Task<OperateResult<string>> GetRapidTasksAsync()
		{
			OperateResult<string> read = await ReadStringAsync("url=/rw/rapid/tasks");
			if (!read.IsSuccess)
			{
				return read;
			}
			MatchCollection matchs = Regex.Matches(read.Content, "<li class=\"rap-task-li\" [\\S\\s]+?</li>");
			JArray jArray = new JArray();
			for (int i = 0; i < matchs.Count; i++)
			{
				JObject json = new JObject();
				foreach (XElement item in XElement.Parse(matchs[i].Value).Elements("span"))
				{
					json[item.Attribute("class").Value] = item.Value;
				}
				jArray.Add(json);
			}
			return OperateResult.CreateSuccessResult(jArray.ToString());
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ABBWebApiClient[{base.IpAddress}:{base.Port}]";
		}
	}
}

using System.Text;
using System.Threading;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.ModBus;

namespace Apps.Communication.Robot.Estun
{
	/// <summary>
	/// 一个埃斯顿的机器人的通信类
	/// </summary>
	public class EstunTcpNet : ModbusTcpNet
	{
		private Timer timer;

		/// <summary>
		/// 实例化一个Modbus-Tcp协议的客户端对象<br />
		/// Instantiate a client object of the Modbus-Tcp protocol
		/// </summary>
		public EstunTcpNet()
		{
			timer = new Timer(ThreadTimerTick, null, 3000, 10000);
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <summary>
		/// 指定服务器地址，端口号，客户端自己的站号来初始化<br />
		/// Specify the server address, port number, and client's own station number to initialize
		/// </summary>
		/// <param name="ipAddress">服务器的Ip地址</param>
		/// <param name="port">服务器的端口号</param>
		/// <param name="station">客户端自身的站号</param>
		public EstunTcpNet(string ipAddress, int port = 502, byte station = 1)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		private void ThreadTimerTick(object obj)
		{
			OperateResult<ushort> operateResult = ReadUInt16("0");
			if (!operateResult.IsSuccess)
			{
			}
		}

		/// <summary>
		/// 读取埃斯顿的机器人的数据
		/// </summary>
		/// <returns>机器人数据</returns>
		public OperateResult<EstunData> ReadRobotData()
		{
			OperateResult<byte[]> operateResult = Read("0", 100);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<EstunData>(operateResult);
			}
			return OperateResult.CreateSuccessResult(new EstunData(operateResult.Content, base.ByteTransform));
		}

		private OperateResult ExecuteCommand(short command)
		{
			OperateResult<short> operateResult = ReadInt16("18");
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			if (operateResult.Content != 2049)
			{
				return new OperateResult("命令状态位 0x801 确认失败！");
			}
			OperateResult operateResult2 = Write("99", (short)17);
			if (!operateResult2.IsSuccess)
			{
				return new OperateResult("打开下发权限失败！" + operateResult2.Message);
			}
			OperateResult operateResult3 = Write("51", command);
			if (!operateResult3.IsSuccess)
			{
				return new OperateResult("下发命令失败！" + operateResult3.Message);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 机器人程序启动
		/// </summary>
		/// <returns>是否启动成功</returns>
		public OperateResult RobotStartPrograme()
		{
			return ExecuteCommand(4);
		}

		/// <summary>
		/// 机器人程序停止
		/// </summary>
		/// <returns></returns>
		public OperateResult RobotStopPrograme()
		{
			return ExecuteCommand(8);
		}

		/// <summary>
		/// 机器人的错误进行复位
		/// </summary>
		/// <returns></returns>
		public OperateResult RobotResetError()
		{
			return ExecuteCommand(16);
		}

		/// <summary>
		/// 机器人重新装载程序名
		/// </summary>
		/// <param name="projectName">程序的名称</param>
		/// <returns>是否装载成功</returns>
		public OperateResult RobotLoadProject(string projectName)
		{
			byte[] value = SoftBasic.ArrayExpandToLength(Encoding.ASCII.GetBytes(projectName), 20);
			OperateResult operateResult = Write("53", value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ExecuteCommand(128);
		}

		/// <summary>
		/// 机器人卸载程序名
		/// </summary>
		/// <returns></returns>
		public OperateResult RobotUnregisterProject()
		{
			return ExecuteCommand(256);
		}

		/// <summary>
		/// 机器人设置全局速度值
		/// </summary>
		/// <param name="value">全局速度值</param>
		/// <returns>是否设置成功</returns>
		public OperateResult RobotSetGlobalSpeedValue(short value)
		{
			OperateResult operateResult = Write("52", value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			return ExecuteCommand(512);
		}

		/// <summary>
		/// 重置机器人的命令状态
		/// </summary>
		/// <returns></returns>
		public OperateResult RobotCommandStatusRestart()
		{
			return ExecuteCommand(1024);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"EstunTcpNet[{IpAddress}:{Port}]";
		}
	}
}

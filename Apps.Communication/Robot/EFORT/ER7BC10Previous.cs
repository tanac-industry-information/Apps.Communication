using System;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;
using Newtonsoft.Json;

namespace Apps.Communication.Robot.EFORT
{
	/// <summary>
	/// 埃夫特机器人对应型号为ER7B-C10，此协议为旧版的定制版，报文未对齐的版本<br />
	/// The corresponding model of the efort robot is er7b-c10. This protocol is a customized version of the old version, and the message is not aligned
	/// </summary>
	public class ER7BC10Previous : NetworkDoubleBase, IRobotNet
	{
		private SoftIncrementCount softIncrementCount;

		/// <summary>
		/// 实例化一个默认的对象，并指定IP地址和端口号，端口号通常为8008<br />
		/// Instantiate a default object and specify the IP address and port number, usually 8008
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public ER7BC10Previous(string ipAddress, int port)
		{
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
			softIncrementCount = new SoftIncrementCount(65535L, 0L);
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new EFORTMessagePrevious();
		}

		/// <summary>
		/// 获取发送的消息的命令<br />
		/// Gets the command to send the message
		/// </summary>
		/// <returns>字节数组命令</returns>
		public byte[] GetReadCommand()
		{
			byte[] array = new byte[36];
			Encoding.ASCII.GetBytes("MessageHead").CopyTo(array, 0);
			BitConverter.GetBytes((ushort)array.Length).CopyTo(array, 15);
			BitConverter.GetBytes((ushort)1001).CopyTo(array, 17);
			BitConverter.GetBytes((ushort)softIncrementCount.GetCurrentValue()).CopyTo(array, 19);
			Encoding.ASCII.GetBytes("MessageTail").CopyTo(array, 21);
			return array;
		}

		/// <inheritdoc cref="M:Communication.Core.Net.IRobotNet.Read(System.String)" />
		[HslMqttApi(ApiTopic = "ReadRobotByte", Description = "Read the robot's original byte data information according to the address")]
		public OperateResult<byte[]> Read(string address)
		{
			return ReadFromCoreServer(GetReadCommand());
		}

		/// <inheritdoc cref="M:Communication.Core.Net.IRobotNet.ReadString(System.String)" />
		[HslMqttApi(ApiTopic = "ReadRobotString", Description = "Read the string data information of the robot based on the address")]
		public OperateResult<string> ReadString(string address)
		{
			OperateResult<EfortData> operateResult = ReadEfortData();
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			return OperateResult.CreateSuccessResult(JsonConvert.SerializeObject(operateResult.Content, Formatting.Indented));
		}

		/// <summary>
		/// 本机器人不支持该方法操作，将永远返回失败，无效的操作<br />
		/// This robot does not support this method operation, will always return failed, invalid operation
		/// </summary>
		/// <param name="address">指定的地址信息，有些机器人可能不支持</param>
		/// <param name="value">原始的字节数据信息</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi(ApiTopic = "WriteRobotByte", Description = "This robot does not support this method operation, will always return failed, invalid operation")]
		public OperateResult Write(string address, byte[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <summary>
		/// 本机器人不支持该方法操作，将永远返回失败，无效的操作<br />
		/// This robot does not support this method operation, will always return failed, invalid operation
		/// </summary>
		/// <param name="address">指定的地址信息，有些机器人可能不支持</param>
		/// <param name="value">字符串的数据信息</param>
		/// <returns>是否成功的写入</returns>
		[HslMqttApi(ApiTopic = "WriteRobotString", Description = "This robot does not support this method operation, will always return failed, invalid operation")]
		public OperateResult Write(string address, string value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <summary>
		/// 读取机器人的详细信息<br />
		/// Read the details of the robot
		/// </summary>
		/// <returns>结果数据信息</returns>
		[HslMqttApi(Description = "Read the details of the robot")]
		public OperateResult<EfortData> ReadEfortData()
		{
			OperateResult<byte[]> operateResult = Read("");
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<EfortData>(operateResult);
			}
			return EfortData.PraseFromPrevious(operateResult.Content);
		}

		/// <inheritdoc cref="M:Communication.Robot.EFORT.ER7BC10Previous.Read(System.String)" />
		public async Task<OperateResult<byte[]>> ReadAsync(string address)
		{
			return await ReadFromCoreServerAsync(GetReadCommand());
		}

		/// <inheritdoc cref="M:Communication.Robot.EFORT.ER7BC10Previous.ReadString(System.String)" />
		public async Task<OperateResult<string>> ReadStringAsync(string address)
		{
			OperateResult<EfortData> read = await ReadEfortDataAsync();
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			return OperateResult.CreateSuccessResult(JsonConvert.SerializeObject(read.Content, Formatting.Indented));
		}

		/// <inheritdoc cref="M:Communication.Robot.EFORT.ER7BC10Previous.Write(System.String,System.Byte[])" />
		public async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Robot.EFORT.ER7BC10Previous.Write(System.String,System.String)" />
		public async Task<OperateResult> WriteAsync(string address, string value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Robot.EFORT.ER7BC10Previous.ReadEfortData" />
		public async Task<OperateResult<EfortData>> ReadEfortDataAsync()
		{
			OperateResult<byte[]> read = await ReadAsync("");
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<EfortData>(read);
			}
			return EfortData.PraseFromPrevious(read.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ER7BC10 Pre Robot[{IpAddress}:{Port}]";
		}
	}
}

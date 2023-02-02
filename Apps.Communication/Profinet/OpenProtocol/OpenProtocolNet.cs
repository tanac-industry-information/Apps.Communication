using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.OpenProtocol
{
	/// <summary>
	/// 开放以太网协议，仍然在开发中<br />
	/// Open Ethernet protocol, still under development
	/// </summary>
	public class OpenProtocolNet : NetworkDoubleBase
	{
		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public OpenProtocolNet()
		{
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 使用指定的IP地址及端口来初始化对象<br />
		/// Use the specified IP address and port to initialize the object
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口号</param>
		public OpenProtocolNet(string ipAddress, int port)
		{
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new OpenProtocolMessage();
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			OperateResult<string> operateResult = ReadCustomer(1, 0, 0, 0, null);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			if (operateResult.Content.Substring(4, 4) == "0002")
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult("Failed:" + operateResult.Content.Substring(4, 4));
		}

		/// <summary>
		/// 自定义的命令读取
		/// </summary>
		/// <param name="mid"></param>
		/// <param name="revison"></param>
		/// <param name="stationId"></param>
		/// <param name="spindleId"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public OperateResult<string> ReadCustomer(int mid, int revison, int stationId, int spindleId, List<string> parameters)
		{
			if (parameters != null)
			{
				parameters = new List<string>();
			}
			OperateResult<byte[]> operateResult = BuildReadCommand(mid, revison, stationId, spindleId, parameters);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(operateResult2.Content));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OpenProtocolNet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 构建一个读取的初始报文
		/// </summary>
		/// <param name="mid"></param>
		/// <param name="revison"></param>
		/// <param name="stationId"></param>
		/// <param name="spindleId"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static OperateResult<byte[]> BuildReadCommand(int mid, int revison, int stationId, int spindleId, List<string> parameters)
		{
			if (mid < 0 || mid > 9999)
			{
				return new OperateResult<byte[]>("Mid must be between 0 - 9999");
			}
			if (revison < 0 || revison > 999)
			{
				return new OperateResult<byte[]>("revison must be between 0 - 999");
			}
			if (stationId < 0 || stationId > 9)
			{
				return new OperateResult<byte[]>("stationId must be between 0 - 9");
			}
			if (spindleId < 0 || spindleId > 99)
			{
				return new OperateResult<byte[]>("spindleId must be between 0 - 99");
			}
			int count = 0;
			parameters?.ForEach(delegate(string m)
			{
				count += m.Length;
			});
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append((20 + count).ToString("D4"));
			stringBuilder.Append(mid.ToString("D4"));
			stringBuilder.Append(revison.ToString("D3"));
			stringBuilder.Append('\0');
			stringBuilder.Append(stationId.ToString("D1"));
			stringBuilder.Append(spindleId.ToString("D2"));
			stringBuilder.Append('\0');
			stringBuilder.Append('\0');
			stringBuilder.Append('\0');
			stringBuilder.Append('\0');
			stringBuilder.Append('\0');
			if (parameters != null)
			{
				for (int i = 0; i < parameters.Count; i++)
				{
					stringBuilder.Append(parameters[i]);
				}
			}
			stringBuilder.Append('\0');
			return OperateResult.CreateSuccessResult(Encoding.ASCII.GetBytes(stringBuilder.ToString()));
		}
	}
}

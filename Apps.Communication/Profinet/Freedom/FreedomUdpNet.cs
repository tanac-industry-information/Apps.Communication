using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Freedom
{
	/// <summary>
	/// 基于UDP/IP协议的自由协议，需要在地址里传入报文信息，也可以传入数据偏移信息，<see cref="P:Communication.Core.Net.NetworkUdpDeviceBase.ByteTransform" />默认为<see cref="T:Communication.Core.RegularByteTransform" />
	/// </summary>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample3" title="实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample4" title="读取" />
	/// </example>
	public class FreedomUdpNet : NetworkUdpDeviceBase
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public FreedomUdpNet()
		{
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 指定IP地址及端口号来实例化自由的TCP协议
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口</param>
		public FreedomUdpNet(string ipAddress, int port)
		{
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "特殊的地址格式，需要采用解析包起始地址的报文，例如 modbus 协议为 stx=9;00 00 00 00 00 06 01 03 00 64 00 01")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[], int> operateResult = FreedomTcpNet.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content1);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult.Content2 >= operateResult2.Content.Length)
			{
				return new OperateResult<byte[]>(StringResources.Language.ReceiveDataLengthTooShort);
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.RemoveBegin(operateResult.Content2));
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, byte[] value)
		{
			return Read(address, 0);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FreedomUdpNet<{base.ByteTransform.GetType()}>[{IpAddress}:{Port}]";
		}
	}
}

using System;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Freedom
{
	/// <summary>
	/// 基于TCP/IP协议的自由协议，需要在地址里传入报文信息，也可以传入数据偏移信息，<see cref="P:Communication.Core.Net.NetworkDoubleBase.ByteTransform" />默认为<see cref="T:Communication.Core.RegularByteTransform" />
	/// </summary>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample1" title="实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\FreedomExample.cs" region="Sample2" title="连接及读取" />
	/// </example>
	public class FreedomTcpNet : NetworkDeviceBase
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public FreedomTcpNet()
		{
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 指定IP地址及端口号来实例化自由的TCP协议
		/// </summary>
		/// <param name="ipAddress">Ip地址</param>
		/// <param name="port">端口</param>
		public FreedomTcpNet(string ipAddress, int port)
		{
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "特殊的地址格式，需要采用解析包起始地址的报文，例如 modbus 协议为 stx=9;00 00 00 00 00 06 01 03 00 64 00 01")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[], int> operateResult = AnalysisAddress(address);
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
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[], int> analysis = AnalysisAddress(address);
			if (!analysis.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(analysis);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(analysis.Content1);
			if (!read.IsSuccess)
			{
				return read;
			}
			if (analysis.Content2 >= read.Content.Length)
			{
				return new OperateResult<byte[]>(StringResources.Language.ReceiveDataLengthTooShort);
			}
			return OperateResult.CreateSuccessResult(read.Content.RemoveBegin(analysis.Content2));
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await ReadAsync(address, 0);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FreedomTcpNet<{base.ByteTransform.GetType()}>[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 分析地址的方法，会转换成一个数据报文和数据结果偏移的信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>报文结果内容</returns>
		public static OperateResult<byte[], int> AnalysisAddress(string address)
		{
			try
			{
				int value = 0;
				byte[] value2 = null;
				if (address.IndexOf(';') > 0)
				{
					string[] array = address.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						if (array[i].StartsWith("stx="))
						{
							value = Convert.ToInt32(array[i].Substring(4));
						}
						else
						{
							value2 = array[i].ToHexBytes();
						}
					}
				}
				else
				{
					value2 = address.ToHexBytes();
				}
				return OperateResult.CreateSuccessResult(value2, value);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[], int>(ex.Message);
			}
		}
	}
}

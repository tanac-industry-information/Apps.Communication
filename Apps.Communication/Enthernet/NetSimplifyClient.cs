using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Enthernet
{
	/// <summary>
	/// 同步访问数据的客户端类，用于向服务器请求一些确定的数据信息
	/// </summary>
	/// <remarks>
	/// 详细的使用说明，请参照博客<a href="http://www.cnblogs.com/dathlin/p/7697782.html">http://www.cnblogs.com/dathlin/p/7697782.html</a>
	/// </remarks>
	/// <example>
	/// 此处贴上了Demo项目的服务器配置的示例代码
	/// <code lang="cs" source="TestProject\HslCommunicationDemo\Hsl\FormSimplifyNet.cs" region="FormSimplifyNet" title="FormSimplifyNet示例" />
	/// </example>
	public class NetSimplifyClient : NetworkDoubleBase
	{
		/// <summary>
		/// 实例化一个客户端的对象，用于和服务器通信
		/// </summary>
		/// <param name="ipAddress">服务器的ip地址</param>
		/// <param name="port">服务器的端口号</param>
		public NetSimplifyClient(string ipAddress, int port)
		{
			base.ByteTransform = new RegularByteTransform();
			IpAddress = ipAddress;
			Port = port;
		}

		/// <summary>
		/// 实例化一个客户端的对象，用于和服务器通信
		/// </summary>
		/// <param name="ipAddress">服务器的ip地址</param>
		/// <param name="port">服务器的端口号</param>
		public NetSimplifyClient(IPAddress ipAddress, int port)
		{
			base.ByteTransform = new RegularByteTransform();
			IpAddress = ipAddress.ToString();
			Port = port;
		}

		/// <summary>
		/// 实例化一个客户端对象，需要手动指定Ip地址和端口
		/// </summary>
		public NetSimplifyClient()
		{
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new HslMessage();
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			if (isUseAccountCertificate)
			{
				return AccountCertificate(socket);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			if (isUseAccountCertificate)
			{
				return await AccountCertificateAsync(socket);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，忽略了自定义消息反馈
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<string> ReadFromServer(NetHandle customer, string send)
		{
			OperateResult<byte[]> operateResult = ReadFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			return OperateResult.CreateSuccessResult(Encoding.Unicode.GetString(operateResult.Content));
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数组，忽略了自定义消息反馈
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<string[]> ReadFromServer(NetHandle customer, string[] send)
		{
			OperateResult<byte[]> operateResult = ReadFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(HslProtocol.UnPackStringArrayFromByte(operateResult.Content));
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字节数据
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送的字节内容</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<byte[]> ReadFromServer(NetHandle customer, byte[] send)
		{
			return ReadFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，并返回状态信息
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<NetHandle, string> ReadCustomerFromServer(NetHandle customer, string send)
		{
			OperateResult<NetHandle, byte[]> operateResult = ReadCustomerFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, string>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content1, Encoding.Unicode.GetString(operateResult.Content2));
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，并返回状态信息
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<NetHandle, string[]> ReadCustomerFromServer(NetHandle customer, string[] send)
		{
			OperateResult<NetHandle, byte[]> operateResult = ReadCustomerFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, string[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content1, HslProtocol.UnPackStringArrayFromByte(operateResult.Content2));
		}

		/// <summary>
		/// 客户端向服务器进行请求，请求字符串数据，并返回状态信息
		/// </summary>
		/// <param name="customer">用户的指令头</param>
		/// <param name="send">发送数据</param>
		/// <returns>带返回消息的结果对象</returns>
		public OperateResult<NetHandle, byte[]> ReadCustomerFromServer(NetHandle customer, byte[] send)
		{
			return ReadCustomerFromServerBase(HslProtocol.CommandBytes(customer, base.Token, send));
		}

		/// <summary>
		/// 需要发送的底层数据
		/// </summary>
		/// <param name="send">需要发送的底层数据</param>
		/// <returns>带返回消息的结果对象</returns>
		private OperateResult<byte[]> ReadFromServerBase(byte[] send)
		{
			OperateResult<NetHandle, byte[]> operateResult = ReadCustomerFromServerBase(send);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content2);
		}

		/// <summary>
		/// 需要发送的底层数据
		/// </summary>
		/// <param name="send">需要发送的底层数据</param>
		/// <returns>带返回消息的结果对象</returns>
		private OperateResult<NetHandle, byte[]> ReadCustomerFromServerBase(byte[] send)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(send);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, byte[]>(operateResult);
			}
			return HslProtocol.ExtractHslData(operateResult.Content);
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadFromServer(Communication.NetHandle,System.String)" />
		public async Task<OperateResult<string>> ReadFromServerAsync(NetHandle customer, string send)
		{
			OperateResult<byte[]> read = await ReadFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			return OperateResult.CreateSuccessResult(Encoding.Unicode.GetString(read.Content));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadFromServer(Communication.NetHandle,System.String[])" />
		public async Task<OperateResult<string[]>> ReadFromServerAsync(NetHandle customer, string[] send)
		{
			OperateResult<byte[]> read = await ReadFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string[]>(read);
			}
			return OperateResult.CreateSuccessResult(HslProtocol.UnPackStringArrayFromByte(read.Content));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadFromServer(Communication.NetHandle,System.Byte[])" />
		public async Task<OperateResult<byte[]>> ReadFromServerAsync(NetHandle customer, byte[] send)
		{
			return await ReadFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadCustomerFromServer(Communication.NetHandle,System.String)" />
		public async Task<OperateResult<NetHandle, string>> ReadCustomerFromServerAsync(NetHandle customer, string send)
		{
			OperateResult<NetHandle, byte[]> read = await ReadCustomerFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, string>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content1, Encoding.Unicode.GetString(read.Content2));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadCustomerFromServer(Communication.NetHandle,System.String[])" />
		public async Task<OperateResult<NetHandle, string[]>> ReadCustomerFromServerAsync(NetHandle customer, string[] send)
		{
			OperateResult<NetHandle, byte[]> read = await ReadCustomerFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, string[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content1, HslProtocol.UnPackStringArrayFromByte(read.Content2));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadCustomerFromServer(Communication.NetHandle,System.Byte[])" />
		public async Task<OperateResult<NetHandle, byte[]>> ReadCustomerFromServerAsync(NetHandle customer, byte[] send)
		{
			return await ReadCustomerFromServerBaseAsync(HslProtocol.CommandBytes(customer, base.Token, send));
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadFromServerBase(System.Byte[])" />
		private async Task<OperateResult<byte[]>> ReadFromServerBaseAsync(byte[] send)
		{
			OperateResult<NetHandle, byte[]> read = await ReadCustomerFromServerBaseAsync(send);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content2);
		}

		/// <inheritdoc cref="M:Communication.Enthernet.NetSimplifyClient.ReadCustomerFromServerBase(System.Byte[])" />
		private async Task<OperateResult<NetHandle, byte[]>> ReadCustomerFromServerBaseAsync(byte[] send)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(send);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<NetHandle, byte[]>(read);
			}
			return HslProtocol.ExtractHslData(read.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"NetSimplifyClient[{IpAddress}:{Port}]";
		}
	}
}

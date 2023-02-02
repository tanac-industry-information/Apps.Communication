using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 静态的方法支持类，提供一些网络的静态支持，支持从套接字从同步接收指定长度的字节数据，并支持报告进度。<br />
	/// The static method support class provides some static support for the network, supports receiving byte data of a specified length from the socket from synchronization, and supports reporting progress.
	/// </summary>
	/// <remarks>
	/// 在接收指定数量的字节数据的时候，如果一直接收不到，就会发生假死的状态。接收的数据时保存在内存里的，不适合大数据块的接收。
	/// </remarks>
	public static class NetSupport
	{
		/// <summary>
		/// Socket传输中的缓冲池大小<br />
		/// Buffer pool size in socket transmission
		/// </summary>
		internal const int SocketBufferSize = 16384;

		/// <summary>
		/// 从socket的网络中读取数据内容，需要指定数据长度和超时的时间，为了防止数据太大导致接收失败，所以此处接收到新的数据之后就更新时间。<br />
		/// To read the data content from the socket network, you need to specify the data length and timeout period. In order to prevent the data from being too large and cause the reception to fail, the time is updated after new data is received here.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="receive">接收的长度</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		/// <returns>最终接收的指定长度的byte[]数据</returns>
		internal static byte[] ReadBytesFromSocket(Socket socket, int receive, Action<long, long> reportProgress = null)
		{
			byte[] array = new byte[receive];
			ReceiveBytesFromSocket(socket, array, 0, receive, reportProgress);
			return array;
		}

		/// <summary>
		/// 从socket的网络中读取数据内容，需要指定数据长度和超时的时间，为了防止数据太大导致接收失败，所以此处接收到新的数据之后就更新时间。<br />
		/// To read the data content from the socket network, you need to specify the data length and timeout period. In order to prevent the data from being too large and cause the reception to fail, the time is updated after new data is received here.
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="buffer">缓存的字节数组</param>
		/// <param name="offset">偏移信息</param>
		/// <param name="length">接收长度</param>
		/// <param name="reportProgress">当前接收数据的进度报告，有些协议支持传输非常大的数据内容，可以给与进度提示的功能</param>
		internal static void ReceiveBytesFromSocket(Socket socket, byte[] buffer, int offset, int length, Action<long, long> reportProgress = null)
		{
			int num = 0;
			while (num < length)
			{
				int size = Math.Min(length - num, 16384);
				int num2 = socket.Receive(buffer, num + offset, size, SocketFlags.None);
				num += num2;
				if (num2 == 0)
				{
					throw new RemoteCloseException();
				}
				reportProgress?.Invoke(num, length);
			}
		}

		/// <summary>
		/// 创建一个新的socket对象并连接到远程的地址，需要指定远程终结点，超时时间（单位是毫秒），如果需要绑定本地的IP或是端口，传入 local对象<br />
		/// To create a new socket object and connect to the remote address, you need to specify the remote endpoint, 
		/// the timeout period (in milliseconds), if you need to bind the local IP or port, pass in the local object
		/// </summary>
		/// <param name="endPoint">连接的目标终结点</param>
		/// <param name="timeOut">连接的超时时间</param>
		/// <param name="local">如果需要绑定本地的IP地址，就需要设置当前的对象</param>
		/// <returns>返回套接字的封装结果对象</returns>
		/// <example>
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkBase.cs" region="CreateSocketAndConnectExample" title="创建连接示例" />
		/// </example>
		internal static OperateResult<Socket> CreateSocketAndConnect(IPEndPoint endPoint, int timeOut, IPEndPoint local = null)
		{
			int num = 0;
			while (true)
			{
				num++;
				Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				HslTimeOut hslTimeOut = HslTimeOut.HandleTimeOutCheck(socket, timeOut);
				try
				{
					if (local != null)
					{
						socket.Bind(local);
					}
					socket.Connect(endPoint);
					hslTimeOut.IsSuccessful = true;
					return OperateResult.CreateSuccessResult(socket);
				}
				catch (Exception ex)
				{
					socket?.Close();
					hslTimeOut.IsSuccessful = true;
					if (hslTimeOut.GetConsumeTime() < TimeSpan.FromMilliseconds(500.0) && num < 2)
					{
						Thread.Sleep(100);
						continue;
					}
					if (hslTimeOut.IsTimeout)
					{
						return new OperateResult<Socket>(string.Format(StringResources.Language.ConnectTimeout, endPoint, timeOut) + " ms");
					}
					return new OperateResult<Socket>($"Socket Connect {endPoint} Exception -> " + ex.Message);
				}
			}
		}

		/// <inheritdoc cref="M:Communication.Core.NetSupport.CreateSocketAndConnect(System.Net.IPEndPoint,System.Int32,System.Net.IPEndPoint)" />
		internal static async Task<OperateResult<Socket>> CreateSocketAndConnectAsync(IPEndPoint endPoint, int timeOut, IPEndPoint local = null)
		{
			int connectCount = 0;
			while (true)
			{
				connectCount++;
				Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				HslTimeOut connectTimeout = HslTimeOut.HandleTimeOutCheck(socket, timeOut);
				try
				{
					if (local != null)
					{
						socket.Bind(local);
					}
					await Task.Factory.FromAsync(socket.BeginConnect(endPoint, null, socket), socket.EndConnect);
					connectTimeout.IsSuccessful = true;
					return OperateResult.CreateSuccessResult(socket);
				}
				catch (Exception ex)
				{
					connectTimeout.IsSuccessful = true;
					socket?.Close();
					if (!(connectTimeout.GetConsumeTime() < TimeSpan.FromMilliseconds(500.0)) || connectCount >= 2)
					{
						if (connectTimeout.IsTimeout)
						{
							return new OperateResult<Socket>(string.Format(StringResources.Language.ConnectTimeout, endPoint, timeOut) + " ms");
						}
						return new OperateResult<Socket>("Socket Exception -> " + ex.Message);
					}
					await Task.Delay(100);
				}
			}
		}
	}
}

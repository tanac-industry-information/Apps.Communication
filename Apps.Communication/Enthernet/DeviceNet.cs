using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Enthernet
{
	/// <summary>
	/// 通用设备的基础网络信息
	/// </summary>
	public class DeviceNet : NetworkServerBase
	{
		private List<DeviceState> list;

		private SimpleHybirdLock lock_list;

		private readonly byte endByte = 13;

		/// <summary>
		/// 当客户端上线的时候，触发此事件
		/// </summary>
		public event Action<DeviceState> ClientOnline;

		/// <summary>
		/// 当客户端下线的时候，触发此事件
		/// </summary>
		public event Action<DeviceState> ClientOffline;

		/// <summary>
		/// 按照ASCII文本的方式进行触发接收的数据
		/// </summary>
		public event Action<DeviceState, string> AcceptString;

		/// <summary>
		/// 按照字节的方式进行触发接收的数据
		/// </summary>
		public event Action<DeviceState, byte[]> AcceptBytes;

		/// <summary>
		/// 实例化一个通用的设备类
		/// </summary>
		public DeviceNet()
		{
			list = new List<DeviceState>();
			lock_list = new SimpleHybirdLock();
		}

		private void AddClient(DeviceState device)
		{
			lock_list.Enter();
			list.Add(device);
			lock_list.Leave();
			this.ClientOnline?.Invoke(device);
		}

		private void RemoveClient(DeviceState device)
		{
			lock_list.Enter();
			list.Remove(device);
			device.WorkSocket?.Close();
			lock_list.Leave();
			this.ClientOffline?.Invoke(device);
		}

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
		protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint)
		{
			DeviceState deviceState = new DeviceState
			{
				WorkSocket = socket,
				DeviceEndPoint = (IPEndPoint)socket.RemoteEndPoint,
				IpAddress = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString(),
				ConnectTime = DateTime.Now
			};
			AddClient(deviceState);
			try
			{
				deviceState.WorkSocket.BeginReceive(deviceState.Buffer, 0, deviceState.Buffer.Length, SocketFlags.None, ContentReceiveCallBack, deviceState);
			}
			catch (Exception ex)
			{
				RemoveClient(deviceState);
				base.LogNet?.WriteException(ToString(), StringResources.Language.NetClientLoginFailed, ex);
			}
		}

		private void ContentReceiveCallBack(IAsyncResult ar)
		{
			DeviceState deviceState = ar.AsyncState as DeviceState;
			if (deviceState == null)
			{
				return;
			}
			try
			{
				int num = deviceState.WorkSocket.EndReceive(ar);
				if (num > 0)
				{
					MemoryStream memoryStream = new MemoryStream();
					byte b = deviceState.Buffer[0];
					while (b != endByte)
					{
						memoryStream.WriteByte(b);
						byte[] array = new byte[1];
						deviceState.WorkSocket.Receive(array, 0, 1, SocketFlags.None);
						b = array[0];
					}
					deviceState.WorkSocket.BeginReceive(deviceState.Buffer, 0, deviceState.Buffer.Length, SocketFlags.None, ContentReceiveCallBack, deviceState);
					byte[] array2 = memoryStream.ToArray();
					lock_list.Enter();
					deviceState.ReceiveTime = DateTime.Now;
					lock_list.Leave();
					this.AcceptBytes?.Invoke(deviceState, array2);
					this.AcceptString?.Invoke(deviceState, Encoding.ASCII.GetString(array2));
				}
				else
				{
					RemoveClient(deviceState);
					base.LogNet?.WriteInfo(ToString(), StringResources.Language.NetClientOffline);
				}
			}
			catch (Exception ex)
			{
				RemoveClient(deviceState);
				base.LogNet?.WriteException(ToString(), StringResources.Language.NetClientLoginFailed, ex);
			}
		}
	}
}

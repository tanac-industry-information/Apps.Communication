using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Sick
{
	/// <summary>
	/// Sick的扫码器的服务器信息，只要启动服务器之后，扫码器配置将条码发送到PC的指定端口上来即可，就可以持续的接收条码信息，同样也适用于海康，基恩士，DATELOGIC 。<br />
	/// The server information of Sick's code scanner, as long as the server is started, the code scanner is configured to send the barcode to the designated port of the PC, and it can continuously receive the barcode information.
	/// </summary>
	public class SickIcrTcpServer : NetworkServerBase
	{
		/// <summary>
		/// 接收条码数据的委托信息<br />
		/// Entrusted information to receive barcode data
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <param name="barCode">条码信息</param>
		public delegate void ReceivedBarCodeDelegate(string ipAddress, string barCode);

		private int clientCount = 0;

		private List<AppSession> initiativeClients;

		private object lockClients;

		/// <summary>
		/// 获取当前在线的客户端数量<br />
		/// Get the number of clients currently online
		/// </summary>
		public int OnlineCount => clientCount;

		/// <summary>
		/// 当接收到条码数据的时候触发<br />
		/// Triggered when barcode data is received
		/// </summary>
		public event ReceivedBarCodeDelegate OnReceivedBarCode;

		/// <summary>
		/// 实例化一个默认的服务器对象<br />
		/// Instantiate a default server object
		/// </summary>
		public SickIcrTcpServer()
		{
			initiativeClients = new List<AppSession>();
			lockClients = new object();
		}

		/// <inheritdoc />
		protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint)
		{
			AppSession appSession = new AppSession(socket);
			try
			{
				socket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, appSession);
				AddClient(appSession);
			}
			catch
			{
				socket.Close();
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
			}
		}

		private void SocketAsyncCallBack(IAsyncResult ar)
		{
			AppSession appSession = ar.AsyncState as AppSession;
			if (appSession == null)
			{
				return;
			}
			try
			{
				appSession.WorkSocket.EndReceive(ar);
				byte[] array = new byte[1024];
				int num = appSession.WorkSocket.Receive(array);
				if (num > 0)
				{
					byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);
					appSession.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, appSession);
					if (true)
					{
						this.OnReceivedBarCode?.Invoke(appSession.IpAddress, TranslateCode(Encoding.ASCII.GetString(array2)));
					}
				}
				else
				{
					appSession.WorkSocket?.Close();
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
					RemoveClient(appSession);
				}
			}
			catch
			{
				appSession.WorkSocket?.Close();
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
				RemoveClient(appSession);
			}
		}

		private string TranslateCode(string code)
		{
			StringBuilder stringBuilder = new StringBuilder("");
			for (int i = 0; i < code.Length; i++)
			{
				if (char.IsLetterOrDigit(code, i))
				{
					stringBuilder.Append(code[i]);
				}
			}
			return stringBuilder.ToString();
		}

		/// <summary>
		/// 新增一个主动连接的请求，将不会收到是否连接成功的信息，当网络中断及奔溃之后，会自动重新连接。<br />
		/// A new active connection request will not receive a message whether the connection is successful. When the network is interrupted and crashed, it will automatically reconnect.
		/// </summary>
		/// <param name="ipAddress">对方的Ip地址</param>
		/// <param name="port">端口号</param>
		public void AddConnectBarcodeScan(string ipAddress, int port)
		{
			IPEndPoint state = new IPEndPoint(IPAddress.Parse(ipAddress), port);
			ThreadPool.QueueUserWorkItem(ConnectBarcodeScan, state);
		}

		private void ConnectBarcodeScan(object obj)
		{
			IPEndPoint iPEndPoint = obj as IPEndPoint;
			if (iPEndPoint == null)
			{
				return;
			}
			OperateResult<Socket> operateResult = CreateSocketAndConnect(iPEndPoint, 5000);
			if (!operateResult.IsSuccess)
			{
				Thread.Sleep(1000);
				ThreadPool.QueueUserWorkItem(ConnectBarcodeScan, iPEndPoint);
				return;
			}
			AppSession appSession = new AppSession(operateResult.Content);
			try
			{
				appSession.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, InitiativeSocketAsyncCallBack, appSession);
				AddClient(appSession);
			}
			catch
			{
				appSession.WorkSocket.Close();
				ThreadPool.QueueUserWorkItem(ConnectBarcodeScan, appSession);
			}
		}

		private void InitiativeSocketAsyncCallBack(IAsyncResult ar)
		{
			AppSession appSession = ar.AsyncState as AppSession;
			if (appSession == null)
			{
				return;
			}
			try
			{
				appSession.WorkSocket.EndReceive(ar);
				byte[] array = new byte[1024];
				int num = appSession.WorkSocket.Receive(array);
				if (num > 0)
				{
					byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);
					appSession.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, InitiativeSocketAsyncCallBack, appSession);
					if (true)
					{
						this.OnReceivedBarCode?.Invoke(appSession.IpAddress, TranslateCode(Encoding.ASCII.GetString(array2)));
					}
				}
				else
				{
					appSession.WorkSocket?.Close();
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
					RemoveClient(appSession);
				}
			}
			catch
			{
				appSession.WorkSocket?.Close();
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
				RemoveClient(appSession);
				if (base.IsStarted)
				{
					ConnectBarcodeScan(appSession);
				}
			}
		}

		private void AddClient(AppSession session)
		{
			lock (lockClients)
			{
				clientCount++;
				initiativeClients.Add(session);
			}
		}

		private void RemoveClient(AppSession session)
		{
			lock (lockClients)
			{
				clientCount--;
				initiativeClients.Remove(session);
			}
		}

		/// <inheritdoc />
		protected override void CloseAction()
		{
			lock (lockClients)
			{
				for (int i = 0; i < initiativeClients.Count; i++)
				{
					initiativeClients[i].WorkSocket?.Close();
				}
				initiativeClients.Clear();
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"SickIcrTcpServer[{base.Port}]";
		}
	}
}

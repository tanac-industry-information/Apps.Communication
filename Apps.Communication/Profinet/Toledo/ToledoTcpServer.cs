using System;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Toledo
{
	/// <summary>
	/// 托利多电子秤的TCP服务器，启动服务器后，等待电子秤的数据连接。
	/// </summary>
	public class ToledoTcpServer : NetworkServerBase
	{
		/// <summary>
		/// 托利多数据接收时的委托
		/// </summary>
		/// <param name="sender">数据发送对象</param>
		/// <param name="toledoStandardData">数据对象</param>
		public delegate void ToledoStandardDataReceivedDelegate(object sender, ToledoStandardData toledoStandardData);

		/// <summary>
		/// 获取或设置当前的报文否是含有校验的，默认为含有校验
		/// </summary>
		public bool HasChk { get; set; } = true;


		/// <summary>
		/// 当接收到一条新的托利多的数据的时候触发
		/// </summary>
		public event ToledoStandardDataReceivedDelegate OnToledoStandardDataReceived;

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ToledoTcpServer()
		{
		}

		/// <inheritdoc />
		protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint)
		{
			AppSession appSession = new AppSession(socket);
			base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOnlineInfo, appSession.IpEndPoint));
			if (!appSession.WorkSocket.BeginReceiveResult(ReceiveCallBack, appSession).IsSuccess)
			{
				base.LogNet?.WriteError(ToString(), StringResources.Language.NetClientLoginFailed);
			}
		}

		private async void ReceiveCallBack(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			AppSession appSession = asyncState as AppSession;
			if (appSession == null || !appSession.WorkSocket.EndReceiveResult(ar).IsSuccess)
			{
				return;
			}
			OperateResult<byte[]> read = await ReceiveAsync(appSession.WorkSocket, HasChk ? 18 : 17);
			if (!read.IsSuccess)
			{
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
				appSession.WorkSocket?.Close();
				return;
			}
			this.OnToledoStandardDataReceived?.Invoke(this, new ToledoStandardData(read.Content));
			if (!appSession.WorkSocket.BeginReceiveResult(ReceiveCallBack, appSession).IsSuccess)
			{
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, appSession.IpEndPoint));
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ToledoTcpServer[{base.Port}]";
		}
	}
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 带登录认证的服务器类，可以对连接的客户端进行筛选，放行用户名密码正确的连接<br />
	/// Server class with login authentication, which can filter connected clients and allow connections with correct username and password
	/// </summary>
	public class NetworkAuthenticationServerBase : NetworkServerBase, IDisposable
	{
		private Dictionary<string, string> accounts = new Dictionary<string, string>();

		private SimpleHybirdLock lockLoginAccount = new SimpleHybirdLock();

		private bool disposedValue = false;

		/// <summary>
		/// 获取或设置是否对客户端启动账号认证<br />
		/// Gets or sets whether to enable account authentication on the client
		/// </summary>
		public bool IsUseAccountCertificate { get; set; }

		/// <summary>
		/// 当客户端的socket登录的时候额外检查的信息，检查当前会话的用户名和密码<br />
		/// Additional check information when the client's socket logs in, check the username and password of the current session
		/// </summary>
		/// <param name="socket">套接字</param>
		/// <param name="endPoint">终结点</param>
		/// <returns>验证的结果</returns>
		protected override OperateResult SocketAcceptExtraCheck(Socket socket, IPEndPoint endPoint)
		{
			if (IsUseAccountCertificate)
			{
				OperateResult<byte[], byte[]> operateResult = ReceiveAndCheckBytes(socket, 2000);
				if (!operateResult.IsSuccess)
				{
					return new OperateResult($"Client login failed[{endPoint}]");
				}
				if (BitConverter.ToInt32(operateResult.Content1, 0) != 5)
				{
					base.LogNet?.WriteError(ToString(), StringResources.Language.NetClientAccountTimeout);
					socket?.Close();
					return new OperateResult($"Authentication failed[{endPoint}]");
				}
				string[] array = HslProtocol.UnPackStringArrayFromByte(operateResult.Content2);
				string text = CheckAccountLegal(array);
				SendStringAndCheckReceive(socket, (text == "success") ? 1 : 0, new string[1] { text });
				if (text != "success")
				{
					return new OperateResult($"Client login failed[{endPoint}]:{text} {SoftBasic.ArrayFormat(array)}");
				}
				base.LogNet?.WriteDebug(ToString(), $"Account Login:{array[0]} Endpoint:[{endPoint}]");
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 新增账户，如果想要启动账户登录，必须将<see cref="P:Communication.Core.Net.NetworkAuthenticationServerBase.IsUseAccountCertificate" />设置为<c>True</c>。<br />
		/// Add an account. If you want to activate account login, you must set <see cref="P:Communication.Core.Net.NetworkAuthenticationServerBase.IsUseAccountCertificate" /> to <c> True </c>
		/// </summary>
		/// <param name="userName">账户名称</param>
		/// <param name="password">账户名称</param>
		public void AddAccount(string userName, string password)
		{
			if (!string.IsNullOrEmpty(userName))
			{
				lockLoginAccount.Enter();
				if (accounts.ContainsKey(userName))
				{
					accounts[userName] = password;
				}
				else
				{
					accounts.Add(userName, password);
				}
				lockLoginAccount.Leave();
			}
		}

		/// <summary>
		/// 删除一个账户的信息<br />
		/// Delete an account's information
		/// </summary>
		/// <param name="userName">账户名称</param>
		public void DeleteAccount(string userName)
		{
			lockLoginAccount.Enter();
			if (accounts.ContainsKey(userName))
			{
				accounts.Remove(userName);
			}
			lockLoginAccount.Leave();
		}

		private string CheckAccountLegal(string[] infos)
		{
			if (infos != null && infos.Length < 2)
			{
				return "User Name input wrong";
			}
			string text = "";
			lockLoginAccount.Enter();
			text = ((!accounts.ContainsKey(infos[0])) ? "User Name input wrong" : ((!(accounts[infos[0]] != infos[1])) ? "success" : "Password is not corrent"));
			lockLoginAccount.Leave();
			return text;
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing">是否托管对象</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					ServerClose();
					lockLoginAccount?.Dispose();
				}
				disposedValue = true;
			}
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public void Dispose()
		{
			Dispose(disposing: true);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"NetworkAuthenticationServerBase[{base.Port}]";
		}
	}
}

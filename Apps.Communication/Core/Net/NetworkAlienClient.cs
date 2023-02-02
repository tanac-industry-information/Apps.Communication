using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.Core.IMessage;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 异形客户端的基类，提供了基础的异形操作<br />
	/// The base class of the profiled client provides the basic profiled operation
	/// </summary>
	public class NetworkAlienClient : NetworkServerBase, IDisposable
	{
		/// <summary>
		/// 客户上线的委托事件
		/// </summary>
		/// <param name="session">异形客户端的会话信息</param>
		public delegate void OnClientConnectedDelegate(AlienSession session);

		private byte[] password;

		private List<string> trustOnline;

		private SimpleHybirdLock trustLock;

		private bool isResponseAck = true;

		private bool isCheckPwd = true;

		private bool disposedValue;

		/// <summary>
		/// 状态登录成功
		/// </summary>
		public const byte StatusOk = 0;

		/// <summary>
		/// 重复登录
		/// </summary>
		public const byte StatusLoginRepeat = 1;

		/// <summary>
		/// 禁止登录
		/// </summary>
		public const byte StatusLoginForbidden = 2;

		/// <summary>
		/// 密码错误
		/// </summary>
		public const byte StatusPasswodWrong = 3;

		/// <summary>
		/// 是否返回响应，默认为 <c>True</c><br />
		/// The default is <c>True</c>
		/// </summary>
		public bool IsResponseAck
		{
			get
			{
				return isResponseAck;
			}
			set
			{
				isResponseAck = value;
			}
		}

		/// <summary>
		/// 是否统一检查密码，如果每个会话需要自己检查密码，就需要设置为false<br />
		/// Whether to check the password uniformly, if each session needs to check the password by itself, it needs to be set to false
		/// </summary>
		public bool IsCheckPwd
		{
			get
			{
				return isCheckPwd;
			}
			set
			{
				isCheckPwd = value;
			}
		}

		/// <summary>
		/// 当有服务器连接上来的时候触发<br />
		/// Triggered when a server is connected
		/// </summary>
		public event OnClientConnectedDelegate OnClientConnected = null;

		/// <summary>
		/// 默认的无参构造方法<br />
		/// The default parameterless constructor
		/// </summary>
		public NetworkAlienClient()
		{
			password = new byte[6];
			trustOnline = new List<string>();
			trustLock = new SimpleHybirdLock();
		}

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作<br />
		/// An action performed when a new request is received
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
		protected override async void ThreadPoolLogin(Socket socket, IPEndPoint endPoint)
		{
			OperateResult<byte[]> check = await ReceiveByMessageAsync(socket, 5000, new AlienMessage());
			if (!check.IsSuccess)
			{
				return;
			}
			byte[] content = check.Content;
			if (content != null && content.Length < 22)
			{
				socket?.Close();
				return;
			}
			if (check.Content[0] != 72)
			{
				socket?.Close();
				return;
			}
			string dtu = Encoding.ASCII.GetString(check.Content, 5, 11).Trim('\0', ' ');
			bool isPasswrodRight = true;
			if (isCheckPwd)
			{
				for (int i = 0; i < password.Length; i++)
				{
					if (check.Content[16 + i] != password[i])
					{
						isPasswrodRight = false;
						break;
					}
				}
			}
			if (!isPasswrodRight)
			{
				if (isResponseAck)
				{
					OperateResult send3 = Send(socket, GetResponse(3));
					if (send3.IsSuccess)
					{
						socket?.Close();
					}
				}
				else
				{
					socket?.Close();
				}
				base.LogNet?.WriteWarn(ToString(), "Login Password Wrong, Id:" + dtu);
				return;
			}
			AlienSession session = new AlienSession
			{
				DTU = dtu,
				Socket = socket,
				IsStatusOk = true,
				Pwd = check.Content.SelectMiddle(16, 6).ToHexString()
			};
			if (!IsClientPermission(session))
			{
				if (isResponseAck)
				{
					OperateResult send4 = Send(socket, GetResponse(2));
					if (send4.IsSuccess)
					{
						socket?.Close();
					}
				}
				else
				{
					socket?.Close();
				}
				base.LogNet?.WriteWarn(ToString(), "Login Forbidden, Id:" + session.DTU);
				return;
			}
			int status = IsClientOnline(session);
			if (status != 0)
			{
				if (isResponseAck)
				{
					OperateResult send2 = Send(socket, GetResponse(1));
					if (send2.IsSuccess)
					{
						socket?.Close();
					}
				}
				else
				{
					socket?.Close();
				}
				base.LogNet?.WriteWarn(ToString(), GetMsgFromCode(session.DTU, status));
				return;
			}
			if (isResponseAck)
			{
				OperateResult send = Send(socket, GetResponse(0));
				if (!send.IsSuccess)
				{
					return;
				}
			}
			base.LogNet?.WriteWarn(ToString(), GetMsgFromCode(session.DTU, status));
			this.OnClientConnected?.Invoke(session);
		}

		/// <summary>
		/// 获取返回的命令信息
		/// </summary>
		/// <param name="status">状态</param>
		/// <returns>回发的指令信息</returns>
		private byte[] GetResponse(byte status)
		{
			byte[] obj = new byte[6] { 72, 115, 110, 0, 1, 0 };
			obj[5] = status;
			return obj;
		}

		/// <summary>
		/// 检测当前的DTU是否在线
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <returns>当前的会话是否在线</returns>
		public virtual int IsClientOnline(AlienSession session)
		{
			return 0;
		}

		/// <summary>
		/// 检测当前的dtu是否允许登录
		/// </summary>
		/// <param name="session">当前的会话信息</param>
		/// <returns>当前的id是否可允许登录</returns>
		private bool IsClientPermission(AlienSession session)
		{
			bool result = false;
			trustLock.Enter();
			if (trustOnline.Count == 0)
			{
				result = true;
			}
			else
			{
				for (int i = 0; i < trustOnline.Count; i++)
				{
					if (trustOnline[i] == session.DTU)
					{
						result = true;
						break;
					}
				}
			}
			trustLock.Leave();
			return result;
		}

		/// <summary>
		/// 设置密码，需要传入长度为6的字节数组<br />
		/// To set the password, you need to pass in an array of bytes of length 6
		/// </summary>
		/// <param name="password">密码信息</param>
		public void SetPassword(byte[] password)
		{
			if (password != null && password.Length == 6)
			{
				password.CopyTo(this.password, 0);
			}
		}

		/// <summary>
		/// 设置可信任的客户端列表，传入一个DTU的列表信息<br />
		/// Set up the list of trusted clients, passing in the list information for a DTU
		/// </summary>
		/// <param name="clients">客户端列表</param>
		public void SetTrustClients(string[] clients)
		{
			trustOnline = new List<string>(clients);
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					trustLock?.Dispose();
					this.OnClientConnected = null;
				}
				disposedValue = true;
			}
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "NetworkAlienBase";
		}

		/// <summary>
		/// 获取错误的描述信息
		/// </summary>
		/// <param name="dtu">dtu信息</param>
		/// <param name="code">错误码</param>
		/// <returns>错误信息</returns>
		public static string GetMsgFromCode(string dtu, int code)
		{
			switch (code)
			{
			case 0:
				return "Login Success, Id:" + dtu;
			case 1:
				return "Login Repeat, Id:" + dtu;
			case 2:
				return "Login Forbidden, Id:" + dtu;
			case 3:
				return "Login Passwod Wrong, Id:" + dtu;
			default:
				return "Login Unknow reason, Id:" + dtu;
			}
		}
	}
}

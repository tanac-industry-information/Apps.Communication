using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 当前的网络会话信息，还包含了一些客户端相关的基本的参数信息<br />
	/// The current network session information also contains some basic parameter information related to the client
	/// </summary>
	public class AppSession : SessionBase
	{
		/// <summary>
		/// UDP通信中的远程端
		/// </summary>
		internal EndPoint UdpEndPoint = null;

		/// <summary>
		/// 远程对象的别名
		/// </summary>
		public string LoginAlias { get; set; }

		/// <summary>
		/// 客户端唯一的标识
		/// </summary>
		public string ClientUniqueID { get; private set; }

		/// <summary>
		/// 数据内容缓存
		/// </summary>
		internal byte[] BytesBuffer { get; set; }

		/// <summary>
		/// 用于关键字分类使用
		/// </summary>
		internal string KeyGroup { get; set; }

		/// <inheritdoc cref="M:Communication.Core.Net.SessionBase.#ctor" />
		public AppSession()
		{
			ClientUniqueID = SoftBasic.GetUniqueStringByGuidAndRandom();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.SessionBase.#ctor(System.Net.Sockets.Socket)" />
		public AppSession(Socket socket)
			: base(socket)
		{
			ClientUniqueID = SoftBasic.GetUniqueStringByGuidAndRandom();
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			return this == obj;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.IsNullOrEmpty(LoginAlias) ? $"AppSession[{base.IpEndPoint}]" : $"AppSession[{base.IpEndPoint}] [{LoginAlias}]";
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}

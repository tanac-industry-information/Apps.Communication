using System;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 远程对象关闭的异常信息<br />
	/// Exception information of remote object close
	/// </summary>
	public class RemoteCloseException : Exception
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public RemoteCloseException()
			: base("Remote Closed Exception")
		{
		}
	}
}

using System;

namespace Apps.Communication.BasicFramework
{
	/// <summary>
	/// 系统的消息类，用来发送消息，和确认消息的
	/// </summary>
	public class MessageBoard
	{
		/// <summary>
		/// 发送方名称
		/// </summary>
		public string NameSend { get; set; } = "";


		/// <summary>
		/// 接收方名称
		/// </summary>
		public string NameReceive { get; set; } = "";


		/// <summary>
		/// 发送时间
		/// </summary>
		public DateTime SendTime { get; set; } = DateTime.Now;


		/// <summary>
		/// 发送的消息内容
		/// </summary>
		public string Content { get; set; } = "";


		/// <summary>
		/// 消息是否已经被查看
		/// </summary>
		public bool HasViewed { get; set; }
	}
}

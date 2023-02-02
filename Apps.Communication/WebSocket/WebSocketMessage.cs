using System.Text;

namespace Apps.Communication.WebSocket
{
	/// <summary>
	/// websocket 协议下的单个消息的数据对象<br />
	/// Data object for a single message under the websocket protocol
	/// </summary>
	public class WebSocketMessage
	{
		/// <summary>
		/// 是否存在掩码<br />
		/// Whether a mask exists
		/// </summary>
		public bool HasMask { get; set; }

		/// <summary>
		/// 当前的websocket的操作码<br />
		/// The current websocket opcode
		/// </summary>
		public int OpCode { get; set; }

		/// <summary>
		/// 负载数据
		/// </summary>
		public byte[] Payload { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OpCode[{OpCode}] HasMask[{HasMask}] Payload: {Encoding.UTF8.GetString(Payload)}";
		}
	}
}

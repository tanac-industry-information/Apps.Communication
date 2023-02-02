using System;
using System.Text;

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// 基于MC协议的Qna兼容3E帧协议的ASCII通讯消息机制
	/// </summary>
	public class MelsecQnA3EAsciiMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 18;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			byte[] bytes = new byte[4]
			{
				HeadBytes[14],
				HeadBytes[15],
				HeadBytes[16],
				HeadBytes[17]
			};
			return Convert.ToInt32(Encoding.ASCII.GetString(bytes), 16);
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			if (HeadBytes == null)
			{
				return false;
			}
			if (HeadBytes[0] == 68 && HeadBytes[1] == 48 && HeadBytes[2] == 48 && HeadBytes[3] == 48)
			{
				return true;
			}
			return false;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return 0;
		}
	}
}

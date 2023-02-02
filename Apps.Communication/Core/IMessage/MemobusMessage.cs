using System;

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// Memobus协议的消息定义
	/// </summary>
	public class MemobusMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 12;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			if (HeadBytes?.Length >= ProtocolHeadBytesLength)
			{
				return BitConverter.ToUInt16(HeadBytes, 6) - 12;
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			return true;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return 0;
		}
	}
}

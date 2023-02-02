namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// 西门子Fetch/Write消息解析协议
	/// </summary>
	public class FetchWriteMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 16;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			if (HeadBytes[5] == 5 || HeadBytes[5] == 4)
			{
				return 0;
			}
			if (HeadBytes[5] == 6)
			{
				if (SendBytes == null)
				{
					return 0;
				}
				if (HeadBytes[8] != 0)
				{
					return 0;
				}
				if (SendBytes[8] == 1 || SendBytes[8] == 6 || SendBytes[8] == 7)
				{
					return (SendBytes[12] * 256 + SendBytes[13]) * 2;
				}
				return SendBytes[12] * 256 + SendBytes[13];
			}
			if (HeadBytes[5] == 3)
			{
				if (HeadBytes[8] == 1 || HeadBytes[8] == 6 || HeadBytes[8] == 7)
				{
					return (HeadBytes[12] * 256 + HeadBytes[13]) * 2;
				}
				return HeadBytes[12] * 256 + HeadBytes[13];
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			if (HeadBytes == null)
			{
				return false;
			}
			if (HeadBytes[0] == 83 && HeadBytes[1] == 53)
			{
				return true;
			}
			return false;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return HeadBytes[3];
		}
	}
}

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// SAM身份证通信协议的消息
	/// </summary>
	public class SAMMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 7;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			if (HeadBytes == null)
			{
				return false;
			}
			return HeadBytes[0] == 170 && HeadBytes[1] == 170 && HeadBytes[2] == 170 && HeadBytes[3] == 150 && HeadBytes[4] == 105;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.SAMMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 7)
			{
				return HeadBytes[5] * 256 + HeadBytes[6];
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return 0;
		}
	}
}

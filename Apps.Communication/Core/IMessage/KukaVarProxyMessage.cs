namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// Kuka机器人的 KRC4 控制器中的服务器KUKAVARPROXY
	/// </summary>
	public class KukaVarProxyMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 4;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			return true;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 4)
			{
				return HeadBytes[2] * 256 + HeadBytes[3];
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 4)
			{
				return HeadBytes[0] * 256 + HeadBytes[1];
			}
			return 0;
		}
	}
}

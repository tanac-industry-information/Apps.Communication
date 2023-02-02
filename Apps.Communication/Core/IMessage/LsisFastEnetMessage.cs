using System;

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// LSIS的PLC的FastEnet的消息定义
	/// </summary>
	public class LsisFastEnetMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 20;

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
			return HeadBytes[0] == 76;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 20)
			{
				return BitConverter.ToUInt16(HeadBytes, 16);
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return BitConverter.ToUInt16(HeadBytes, 14);
		}
	}
}

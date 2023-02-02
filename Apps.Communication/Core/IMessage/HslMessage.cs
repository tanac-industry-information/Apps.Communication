using System;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// 本组件系统使用的默认的消息规则，说明解析和反解析规则的
	/// </summary>
	public class HslMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 32;

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
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 32)
			{
				return SoftBasic.IsTwoBytesEquel(HeadBytes, 12, token, 0, 16);
			}
			return false;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 32)
			{
				return BitConverter.ToInt32(HeadBytes, 28);
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			byte[] headBytes = HeadBytes;
			if (headBytes != null && headBytes.Length >= 32)
			{
				return BitConverter.ToInt32(HeadBytes, 4);
			}
			return 0;
		}
	}
}

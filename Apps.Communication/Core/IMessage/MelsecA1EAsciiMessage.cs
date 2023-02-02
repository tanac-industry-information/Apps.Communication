using System;
using System.Text;

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// 三菱的A兼容1E帧ASCII协议解析规则
	/// </summary>
	public class MelsecA1EAsciiMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 4;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			if (HeadBytes[2] == 53 && HeadBytes[3] == 66)
			{
				return 4;
			}
			if (HeadBytes[2] == 48 && HeadBytes[3] == 48)
			{
				int num = Convert.ToInt32(Encoding.ASCII.GetString(SendBytes, 20, 2), 16);
				if (num == 0)
				{
					num = 256;
				}
				switch (HeadBytes[1])
				{
				case 48:
					return (num % 2 == 1) ? (num + 1) : num;
				case 49:
					return num * 4;
				case 50:
				case 51:
					return 0;
				default:
					return 0;
				}
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			if (HeadBytes != null)
			{
				return HeadBytes[0] - SendBytes[0] == 8;
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

namespace Apps.Communication.Core.IMessage
{
	/// <summary>
	/// Modbus-Tcp协议支持的消息解析类
	/// </summary>
	public class ModbusTcpMessage : INetMessage
	{
		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ProtocolHeadBytesLength" />
		public int ProtocolHeadBytesLength => 8;

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.HeadBytes" />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.ContentBytes" />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IMessage.INetMessage.SendBytes" />
		public byte[] SendBytes { get; set; }

		/// <summary>
		/// 获取或设置是否进行检查返回的消息ID和发送的消息ID是否一致，默认为true，也就是检查<br />
		/// Get or set whether to check whether the returned message ID is consistent with the sent message ID, the default is true, that is, check
		/// </summary>
		public bool IsCheckMessageId { get; set; } = true;


		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetContentLengthByHeadBytes" />
		public int GetContentLengthByHeadBytes()
		{
			if (HeadBytes?.Length >= ProtocolHeadBytesLength)
			{
				int num = HeadBytes[4] * 256 + HeadBytes[5];
				if (num == 0)
				{
					byte[] array = new byte[ProtocolHeadBytesLength - 1];
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = HeadBytes[i + 1];
					}
					HeadBytes = array;
					return HeadBytes[5] * 256 + HeadBytes[6] - 1;
				}
				return num - 2;
			}
			return 0;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.CheckHeadBytesLegal(System.Byte[])" />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			if (IsCheckMessageId)
			{
				if (HeadBytes == null)
				{
					return false;
				}
				if (SendBytes[0] != HeadBytes[0] || SendBytes[1] != HeadBytes[1])
				{
					return false;
				}
				return HeadBytes[2] == 0 && HeadBytes[3] == 0;
			}
			return true;
		}

		/// <inheritdoc cref="M:Communication.Core.IMessage.INetMessage.GetHeadBytesIdentity" />
		public int GetHeadBytesIdentity()
		{
			return HeadBytes[0] * 256 + HeadBytes[1];
		}
	}
}

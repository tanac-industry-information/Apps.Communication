using Apps.Communication.Core.IMessage;

namespace Apps.Communication.CNC.Fanuc
{
	/// <summary>
	/// Fanuc床子的消息对象
	/// </summary>
	public class CNCFanucSeriesMessage : INetMessage
	{
		/// <inheritdoc />
		public int ProtocolHeadBytesLength => 10;

		/// <inheritdoc />
		public byte[] HeadBytes { get; set; }

		/// <inheritdoc />
		public byte[] ContentBytes { get; set; }

		/// <inheritdoc />
		public byte[] SendBytes { get; set; }

		/// <inheritdoc />
		public bool CheckHeadBytesLegal(byte[] token)
		{
			return true;
		}

		/// <inheritdoc />
		public int GetContentLengthByHeadBytes()
		{
			return HeadBytes[8] * 256 + HeadBytes[9];
		}

		/// <inheritdoc />
		public int GetHeadBytesIdentity()
		{
			return 0;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "CNCFanucSeriesMessage";
		}
	}
}

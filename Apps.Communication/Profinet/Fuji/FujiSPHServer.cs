using System;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Fuji
{
	/// <summary>
	/// <b>[商业授权]</b> 富士的SPH虚拟的PLC，支持M1.0，M3.0，M10.0，I0，Q0的位与字的读写操作。<br />
	/// </summary>
	public class FujiSPHServer : NetworkDataServerBase
	{
		private SoftBuffer m1Buffer;

		private SoftBuffer m3Buffer;

		private SoftBuffer m10Buffer;

		private SoftBuffer iqBuffer;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 实例化一个基于SPH协议的虚拟的富士PLC对象，可以用来和<see cref="T:Communication.Profinet.Fuji.FujiSPHNet" />进行通信测试。
		/// </summary>
		public FujiSPHServer()
		{
			m1Buffer = new SoftBuffer(131072);
			m3Buffer = new SoftBuffer(131072);
			m10Buffer = new SoftBuffer(131072);
			iqBuffer = new SoftBuffer(131072);
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[524288];
			m1Buffer.GetBytes().CopyTo(array, 0);
			m3Buffer.GetBytes().CopyTo(array, 131072);
			m10Buffer.GetBytes().CopyTo(array, 262144);
			iqBuffer.GetBytes().CopyTo(array, 393216);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 524288)
			{
				throw new Exception("File is not correct");
			}
			m1Buffer.SetBytes(content, 0, 131072);
			m3Buffer.SetBytes(content, 131072, 131072);
			m10Buffer.SetBytes(content, 262144, 131072);
			iqBuffer.SetBytes(content, 393216, 131072);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			switch (operateResult.Content.TypeCode)
			{
			case 2:
				return OperateResult.CreateSuccessResult(m1Buffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			case 4:
				return OperateResult.CreateSuccessResult(m3Buffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			case 8:
				return OperateResult.CreateSuccessResult(m10Buffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			case 1:
				return OperateResult.CreateSuccessResult(iqBuffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Fuji.FujiSPHNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			switch (operateResult.Content.TypeCode)
			{
			case 2:
				m1Buffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				break;
			case 4:
				m3Buffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				break;
			case 8:
				m10Buffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				break;
			case 1:
				iqBuffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				break;
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			int num = operateResult.Content.BitIndex + length;
			int num2 = ((num % 16 == 0) ? (num / 16) : (num / 16 + 1));
			OperateResult<byte[]> operateResult2 = Read(address, (ushort)num2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2.ConvertFailed<bool[]>();
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content.ToBoolArray().SelectMiddle(operateResult.Content.BitIndex, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<FujiSPHAddress> operateResult = FujiSPHAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			switch (operateResult.Content.TypeCode)
			{
			case 2:
				m1Buffer.SetBool(value, operateResult.Content.AddressStart * 16 + operateResult.Content.BitIndex);
				break;
			case 4:
				m3Buffer.SetBool(value, operateResult.Content.AddressStart * 16 + operateResult.Content.BitIndex);
				break;
			case 8:
				m10Buffer.SetBool(value, operateResult.Content.AddressStart * 16 + operateResult.Content.BitIndex);
				break;
			case 1:
				iqBuffer.SetBool(value, operateResult.Content.AddressStart * 16 + operateResult.Content.BitIndex);
				break;
			default:
				return new OperateResult(StringResources.Language.NotSupportedDataType);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			AppSession appSession = new AppSession(socket);
			if (socket.BeginReceiveResult(SocketAsyncCallBack, appSession).IsSuccess)
			{
				AddClient(appSession);
			}
			else
			{
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
			}
		}

		private async void SocketAsyncCallBack(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			AppSession session = asyncState as AppSession;
			if (session == null)
			{
				return;
			}
			if (!session.WorkSocket.EndReceiveResult(ar).IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 2000, new FujiSPHMessage());
			if (!read1.IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			if (read1.Content[0] != 251 || read1.Content[1] != 128)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
			byte[] back = ReadFromSPBCore(read1.Content);
			if (back == null)
			{
				RemoveClient(session);
				return;
			}
			if (!Send(session.WorkSocket, back).IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString(' ')}");
			session.UpdateHeartTime();
			RaiseDataReceived(session, read1.Content);
			if (!session.WorkSocket.BeginReceiveResult(SocketAsyncCallBack, session).IsSuccess)
			{
				RemoveClient(session);
			}
		}

		private byte[] PackCommand(byte[] cmd, byte err, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[26 + data.Length];
			array[0] = 251;
			array[1] = 128;
			array[2] = 128;
			array[3] = 0;
			array[4] = err;
			array[5] = 123;
			array[6] = cmd[6];
			array[7] = 0;
			array[8] = 17;
			array[9] = 0;
			array[10] = 0;
			array[11] = 0;
			array[12] = 0;
			array[13] = 0;
			array[14] = cmd[14];
			array[15] = cmd[15];
			array[16] = 0;
			array[17] = 1;
			array[18] = BitConverter.GetBytes(data.Length + 6)[0];
			array[19] = BitConverter.GetBytes(data.Length + 6)[1];
			Array.Copy(cmd, 20, array, 20, 6);
			if (data.Length != 0)
			{
				data.CopyTo(array, 26);
			}
			return array;
		}

		private byte[] ReadFromSPBCore(byte[] receive)
		{
			if (receive.Length < 20)
			{
				return PackCommand(receive, 16, null);
			}
			if (receive[14] == 0 && receive[15] == 0)
			{
				return ReadByCommand(receive);
			}
			if (receive[14] == 1 && receive[15] == 0)
			{
				return WriteByCommand(receive);
			}
			return PackCommand(receive, 32, null);
		}

		private byte[] ReadByCommand(byte[] command)
		{
			try
			{
				byte b = command[20];
				int num = command[23] * 256 * 256 + command[22] * 256 + command[21];
				int num2 = command[25] * 256 + command[24];
				if (num + num2 > 65535)
				{
					return PackCommand(command, 69, null);
				}
				switch (b)
				{
				case 2:
					return PackCommand(command, 0, m1Buffer.GetBytes(num * 2, num2 * 2));
				case 4:
					return PackCommand(command, 0, m3Buffer.GetBytes(num * 2, num2 * 2));
				case 8:
					return PackCommand(command, 0, m10Buffer.GetBytes(num * 2, num2 * 2));
				case 1:
					return PackCommand(command, 0, iqBuffer.GetBytes(num * 2, num2 * 2));
				default:
					return PackCommand(command, 64, null);
				}
			}
			catch
			{
				return PackCommand(command, 164, null);
			}
		}

		private byte[] WriteByCommand(byte[] command)
		{
			if (!base.EnableWrite)
			{
				return PackCommand(command, 16, null);
			}
			try
			{
				byte b = command[20];
				int num = command[23] * 256 * 256 + command[22] * 256 + command[21];
				int num2 = command[25] * 256 + command[24];
				byte[] array = command.RemoveBegin(26);
				if (num + num2 > 65535)
				{
					return PackCommand(command, 69, null);
				}
				if (num2 * 2 != array.Length)
				{
					return PackCommand(command, 69, null);
				}
				switch (b)
				{
				case 2:
					m1Buffer.SetBytes(array, num * 2);
					return PackCommand(command, 0, null);
				case 4:
					m3Buffer.SetBytes(array, num * 2);
					return PackCommand(command, 0, null);
				case 8:
					m10Buffer.SetBytes(array, num * 2);
					return PackCommand(command, 0, null);
				case 1:
					iqBuffer.SetBytes(array, num * 2);
					return PackCommand(command, 0, null);
				default:
					return PackCommand(command, 64, null);
				}
			}
			catch
			{
				return PackCommand(command, 164, null);
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m1Buffer.Dispose();
				m3Buffer.Dispose();
				m10Buffer.Dispose();
				iqBuffer.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"FujiSPHServer[{base.Port}]";
		}
	}
}

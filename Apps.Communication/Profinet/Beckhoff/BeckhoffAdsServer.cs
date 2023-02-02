using System;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Beckhoff
{
	/// <summary>
	/// 倍福Ads协议的虚拟服务器
	/// </summary>
	public class BeckhoffAdsServer : NetworkDataServerBase
	{
		private SoftBuffer mBuffer;

		private SoftBuffer iBuffer;

		private SoftBuffer qBuffer;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 实例化一个基于ADS协议的虚拟的倍福PLC对象，可以用来和<see cref="T:Communication.Profinet.Beckhoff.BeckhoffAdsNet" />进行通信测试。
		/// </summary>
		public BeckhoffAdsServer()
		{
			mBuffer = new SoftBuffer(65536);
			iBuffer = new SoftBuffer(65536);
			qBuffer = new SoftBuffer(65536);
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 2;
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[196608];
			mBuffer.GetBytes().CopyTo(array, 0);
			iBuffer.GetBytes().CopyTo(array, 65536);
			qBuffer.GetBytes().CopyTo(array, 131072);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 196608)
			{
				throw new Exception("File is not correct");
			}
			mBuffer.SetBytes(content, 0, 65536);
			iBuffer.SetBytes(content, 65536, 65536);
			qBuffer.SetBytes(content, 131072, 65536);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<uint, uint> operateResult = BeckhoffAdsNet.AnalysisAddress(address, isBit: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			switch (operateResult.Content1)
			{
			case 16416u:
				return OperateResult.CreateSuccessResult(mBuffer.GetBytes((int)operateResult.Content2, length));
			case 61472u:
				return OperateResult.CreateSuccessResult(iBuffer.GetBytes((int)operateResult.Content2, length));
			case 61488u:
				return OperateResult.CreateSuccessResult(qBuffer.GetBytes((int)operateResult.Content2, length));
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<uint, uint> operateResult = BeckhoffAdsNet.AnalysisAddress(address, isBit: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			switch (operateResult.Content1)
			{
			case 16416u:
				mBuffer.SetBytes(value, (int)operateResult.Content2);
				break;
			case 61472u:
				iBuffer.SetBytes(value, (int)operateResult.Content2);
				break;
			case 61488u:
				qBuffer.SetBytes(value, (int)operateResult.Content2);
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
			OperateResult<uint, uint> operateResult = BeckhoffAdsNet.AnalysisAddress(address, isBit: true);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			switch (operateResult.Content1)
			{
			case 16417u:
				return OperateResult.CreateSuccessResult(mBuffer.GetBool((int)operateResult.Content2, length));
			case 61473u:
				return OperateResult.CreateSuccessResult(iBuffer.GetBool((int)operateResult.Content2, length));
			case 61489u:
				return OperateResult.CreateSuccessResult(qBuffer.GetBool((int)operateResult.Content2, length));
			default:
				return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<uint, uint> operateResult = BeckhoffAdsNet.AnalysisAddress(address, isBit: true);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<bool[]>();
			}
			switch (operateResult.Content1)
			{
			case 16417u:
				mBuffer.SetBool(value, (int)operateResult.Content2);
				break;
			case 61473u:
				iBuffer.SetBool(value, (int)operateResult.Content2);
				break;
			case 61489u:
				qBuffer.SetBool(value, (int)operateResult.Content2);
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
			OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 2000, new AdsNetMessage());
			if (!read1.IsSuccess)
			{
				RemoveClient(session);
				return;
			}

			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
			byte[] back = ReadFromAdsCore(read1.Content);
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

		private byte[] PackCommand(byte[] cmd, int err, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[32 + data.Length];
			Array.Copy(cmd, 0, array, 0, 32);
			byte[] array2 = array.SelectBegin(8);
			byte[] array3 = array.SelectMiddle(8, 8);
			array2.CopyTo(array, 8);
			array3.CopyTo(array, 0);
			array[18] = 5;
			array[19] = 0;
			BitConverter.GetBytes(data.Length).CopyTo(array, 20);
			BitConverter.GetBytes(err).CopyTo(array, 24);
			array[11] = 0;
			if (data.Length != 0)
			{
				data.CopyTo(array, 32);
			}
			return BeckhoffAdsNet.PackAmsTcpHelper(AmsTcpHeaderFlags.Command, array);
		}

		private byte[] PackDataResponse(int err, byte[] data)
		{
			if (data != null)
			{
				byte[] array = new byte[8 + data.Length];
				BitConverter.GetBytes(err).CopyTo(array, 0);
				BitConverter.GetBytes(data.Length).CopyTo(array, 4);
				if (data.Length != 0)
				{
					data.CopyTo(array, 8);
				}
				return array;
			}
			return BitConverter.GetBytes(err);
		}

		private byte[] ReadFromAdsCore(byte[] receive)
		{
			switch (BitConverter.ToUInt16(receive, 0))
			{
			case 0:
				receive = receive.RemoveBegin(6);
				base.LogNet?.WriteDebug("TargetId:" + BeckhoffAdsNet.GetAmsNetIdString(receive, 0) + " SenderId:" + BeckhoffAdsNet.GetAmsNetIdString(receive, 8));
				switch (BitConverter.ToInt16(receive, 16))
				{
				case 2:
					return ReadByCommand(receive);
				case 3:
					return WriteByCommand(receive);
				case 9:
					return ReadWriteByCommand(receive);
				default:
					return PackCommand(receive, 32, null);
				}
			case 4098:
				return BeckhoffAdsNet.PackAmsTcpHelper(AmsTcpHeaderFlags.GetLocalNetId, BeckhoffAdsNet.StrToAMSNetId("192.168.163.8.1.1"));
			case 4096:
				return BeckhoffAdsNet.PackAmsTcpHelper(AmsTcpHeaderFlags.PortConnect, BeckhoffAdsNet.StrToAMSNetId("192.168.163.8.1.1:32957"));
			default:
				return null;
			}
		}

		private byte[] ReadByCommand(byte[] command)
		{
			try
			{
				int num = BitConverter.ToInt32(command, 32);
				int num2 = BitConverter.ToInt32(command, 36);
				int length = BitConverter.ToInt32(command, 40);
				switch (num)
				{
				case 16416:
					return PackCommand(command, 0, PackDataResponse(0, mBuffer.GetBytes(num2, length)));
				case 61472:
					return PackCommand(command, 0, PackDataResponse(0, iBuffer.GetBytes(num2, length)));
				case 61488:
					return PackCommand(command, 0, PackDataResponse(0, qBuffer.GetBytes(num2, length)));
				case 16417:
					return PackCommand(command, 0, PackDataResponse(0, mBuffer.GetBool(num2, length).ToByteArray()));
				case 61473:
					return PackCommand(command, 0, PackDataResponse(0, iBuffer.GetBool(num2, length).ToByteArray()));
				case 61489:
					return PackCommand(command, 0, PackDataResponse(0, qBuffer.GetBool(num2, length).ToByteArray()));
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
				int num = BitConverter.ToInt32(command, 32);
				int destIndex = BitConverter.ToInt32(command, 36);
				int num2 = BitConverter.ToInt32(command, 40);
				byte[] data = command.RemoveBegin(44);
				switch (num)
				{
				case 16416:
					mBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61472:
					iBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61488:
					qBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 16417:
					mBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61473:
					iBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61489:
					qBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				default:
					return PackCommand(command, 64, null);
				}
			}
			catch
			{
				return PackCommand(command, 164, null);
			}
		}

		private byte[] ReadWriteByCommand(byte[] command)
		{
			try
			{
				int num = BitConverter.ToInt32(command, 32);
				int destIndex = BitConverter.ToInt32(command, 36);
				int num2 = BitConverter.ToInt32(command, 40);
				int num3 = BitConverter.ToInt32(command, 44);
				byte[] data = command.RemoveBegin(48);
				switch (num)
				{
				case 16416:
					mBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61472:
					iBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61488:
					qBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 16417:
					mBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61473:
					iBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
				case 61489:
					qBuffer.SetBytes(data, destIndex);
					return PackCommand(command, 0, PackDataResponse(0, null));
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
				mBuffer.Dispose();
				iBuffer.Dispose();
				qBuffer.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"BeckhoffAdsServer[{base.Port}]";
		}
	}
}

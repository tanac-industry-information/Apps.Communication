using System;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.AllenBradley
{
	/// <summary>
	/// 虚拟的PCCC服务器，模拟的AB 1400通信
	/// </summary>
	public class AllenBradleyPcccServer : NetworkDataServerBase
	{
		private SoftBuffer aBuffer;

		private SoftBuffer bBuffer;

		private SoftBuffer nBuffer;

		private SoftBuffer fBuffer;

		private SoftBuffer sBuffer;

		private SoftBuffer iBuffer;

		private SoftBuffer oBuffer;

		private uint sessionID = 3305331106u;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public AllenBradleyPcccServer()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 2;
			aBuffer = new SoftBuffer(65536);
			bBuffer = new SoftBuffer(65536);
			nBuffer = new SoftBuffer(65536);
			fBuffer = new SoftBuffer(65536);
			sBuffer = new SoftBuffer(65536);
			iBuffer = new SoftBuffer(65536);
			oBuffer = new SoftBuffer(65536);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte, ushort, ushort> operateResult = AllenBradleySLCNet.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			switch (operateResult.Content1)
			{
			case 142:
				return OperateResult.CreateSuccessResult(aBuffer.GetBytes(operateResult.Content2, length));
			case 133:
				return OperateResult.CreateSuccessResult(bBuffer.GetBytes(operateResult.Content2, length));
			case 137:
				return OperateResult.CreateSuccessResult(nBuffer.GetBytes(operateResult.Content2, length));
			case 138:
				return OperateResult.CreateSuccessResult(fBuffer.GetBytes(operateResult.Content2, length));
			case 132:
				return OperateResult.CreateSuccessResult(sBuffer.GetBytes(operateResult.Content2, length));
			case 131:
				return OperateResult.CreateSuccessResult(iBuffer.GetBytes(operateResult.Content2, length));
			case 130:
				return OperateResult.CreateSuccessResult(oBuffer.GetBytes(operateResult.Content2, length));
			default:
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte, ushort, ushort> operateResult = AllenBradleySLCNet.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			switch (operateResult.Content1)
			{
			case 142:
				aBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 133:
				bBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 137:
				nBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 138:
				fBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 132:
				sBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 131:
				iBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			case 130:
				oBuffer.SetBytes(value, operateResult.Content2);
				return OperateResult.CreateSuccessResult();
			default:
				return new OperateResult(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			AllenBradleyMessage netMessage = new AllenBradleyMessage();
			OperateResult<byte[]> operateResult = ReceiveByMessage(socket, 5000, netMessage);
			if (!operateResult.IsSuccess)
			{
				return;
			}
			string text = operateResult.Content.SelectMiddle(12, 8).ToHexString();
			base.LogNet?.WriteDebug("Reg1: " + operateResult.Content.ToHexString(' '));
			OperateResult operateResult2 = Send(socket, AllenBradleyHelper.PackRequestHeader(101, sessionID, new byte[4] { 1, 0, 0, 0 }));
			if (!operateResult2.IsSuccess)
			{
				return;
			}
			OperateResult<byte[]> operateResult3 = ReceiveByMessage(socket, 5000, netMessage);
			if (!operateResult3.IsSuccess)
			{
				return;
			}
			base.LogNet?.WriteDebug("Reg2: " + operateResult3.Content.ToHexString());
			OperateResult operateResult4 = Send(socket, AllenBradleyHelper.PackRequestHeader(111, sessionID, "00 00 00 00 00 04 02 00 00 00 00 00 b2 00 1e 00 d4 00 00 00 cc 31 59 a2 e8 a3 14 00 27 04 09 10 0b 46 a5 c1 01 40 20 00 01 40 20 00 00 00".ToHexBytes()));
			if (operateResult4.IsSuccess)
			{
				AppSession appSession = new AppSession(socket);
				try
				{
					socket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, appSession);
					AddClient(appSession);
				}
				catch
				{
					socket.Close();
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
				}
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
			try
			{
				session.WorkSocket.EndReceive(ar);
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new AllenBradleyMessage());
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString()}");
				byte[] receive = read1.Content;
				byte[] back = ((receive[0] == 111) ? AllenBradleyHelper.PackRequestHeader(111, sessionID, AllenBradleyHelper.PackCommandSpecificData(AllenBradleyHelper.PackCommandSingleService(null, 0), AllenBradleyHelper.PackCommandSingleService("ce 00 00 00 27 04 09 10 0b 46 a5 c1 00 00".ToHexBytes(), 178))) : ((receive[0] != 102) ? ReadWriteCommand(receive.RemoveBegin(59)) : AllenBradleyHelper.PackRequestHeader(111, sessionID, null)));
				session.WorkSocket.Send(back);
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString()}");
				session.UpdateHeartTime();
				RaiseDataReceived(session, receive);
				session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				base.LogNet?.WriteException(ToString(), ex);
				RemoveClient(session);
			}
		}

		private byte[] GetResponse(int status, byte[] data)
		{
			byte[] array = AllenBradleyHelper.PackRequestHeader(112, sessionID, AllenBradleyHelper.PackCommandSpecificData(AllenBradleyHelper.PackCommandSingleService("e8 a3 14 00".ToHexBytes(), 161), AllenBradleyHelper.PackCommandSingleService(SoftBasic.SpliceArray<byte>("09 00 cb 00 00 00 07 09 10 0b 46 a5 c1 4f 00 08 00".ToHexBytes(), data), 177)));
			base.ByteTransform.TransByte(status).CopyTo(array, 8);
			return array;
		}

		private int GetDynamicLengthData(byte[] fccc, ref int offset)
		{
			int num = fccc[offset++];
			if (num == 255)
			{
				num = BitConverter.ToUInt16(fccc, offset);
				offset += 2;
			}
			return num;
		}

		private byte[] ReadWriteCommand(byte[] fccc)
		{
			int length = fccc[5];
			int offset = 6;
			int dynamicLengthData = GetDynamicLengthData(fccc, ref offset);
			byte b = fccc[offset++];
			int dynamicLengthData2 = GetDynamicLengthData(fccc, ref offset);
			int dynamicLengthData3 = GetDynamicLengthData(fccc, ref offset);
			if (fccc[4] == 162)
			{
				switch (b)
				{
				case 142:
					return GetResponse(0, aBuffer.GetBytes(dynamicLengthData2, length));
				case 133:
					return GetResponse(0, bBuffer.GetBytes(dynamicLengthData2, length));
				case 137:
					return GetResponse(0, nBuffer.GetBytes(dynamicLengthData2, length));
				case 138:
					return GetResponse(0, fBuffer.GetBytes(dynamicLengthData2, length));
				case 132:
					return GetResponse(0, sBuffer.GetBytes(dynamicLengthData2, length));
				case 131:
					return GetResponse(0, iBuffer.GetBytes(dynamicLengthData2, length));
				case 130:
					return GetResponse(0, oBuffer.GetBytes(dynamicLengthData2, length));
				default:
					return GetResponse(1, null);
				}
			}
			if (fccc[4] == 170)
			{
				byte[] data = fccc.RemoveBegin(offset);
				switch (b)
				{
				case 142:
					aBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 133:
					bBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 137:
					nBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 138:
					fBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 132:
					sBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 131:
					iBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				case 130:
					oBuffer.SetBytes(data, dynamicLengthData2);
					return GetResponse(0, null);
				default:
					return GetResponse(1, null);
				}
			}
			return GetResponse(1, null);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"AllenBradleyPcccServer[{base.Port}]";
		}
	}
}

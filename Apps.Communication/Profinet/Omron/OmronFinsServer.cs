using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// <b>[商业授权]</b> 欧姆龙的虚拟服务器，支持DM区，CIO区，Work区，Hold区，Auxiliary区，可以方便的进行测试<br />
	/// <b>[Authorization]</b> Omron's virtual server supports DM area, CIO area, Work area, Hold area, and Auxiliary area, which can be easily tested
	/// </summary>
	public class OmronFinsServer : NetworkDataServerBase
	{
		protected SoftBuffer dBuffer;

		protected SoftBuffer cioBuffer;

		protected SoftBuffer wBuffer;

		protected SoftBuffer hBuffer;

		protected SoftBuffer arBuffer;

		protected SoftBuffer emBuffer;

		private const int DataPoolLength = 65536;

		/// <inheritdoc cref="P:Communication.Core.ByteTransformBase.DataFormat" />
		public DataFormat DataFormat
		{
			get
			{
				return base.ByteTransform.DataFormat;
			}
			set
			{
				base.ByteTransform.DataFormat = value;
			}
		}

		/// <summary>
		/// 实例化一个Fins协议的服务器<br />
		/// Instantiate a Fins protocol server
		/// </summary>
		public OmronFinsServer()
		{
			dBuffer = new SoftBuffer(131072);
			cioBuffer = new SoftBuffer(131072);
			wBuffer = new SoftBuffer(131072);
			hBuffer = new SoftBuffer(131072);
			arBuffer = new SoftBuffer(131072);
			emBuffer = new SoftBuffer(131072);
			dBuffer.IsBoolReverseByWord = true;
			cioBuffer.IsBoolReverseByWord = true;
			wBuffer.IsBoolReverseByWord = true;
			hBuffer.IsBoolReverseByWord = true;
			arBuffer.IsBoolReverseByWord = true;
			emBuffer.IsBoolReverseByWord = true;
			base.WordLength = 1;
			base.ByteTransform = new ReverseWordTransform();
			base.ByteTransform.DataFormat = DataFormat.CDAB;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.DM.WordCode)
			{
				return OperateResult.CreateSuccessResult(dBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.CIO.WordCode)
			{
				return OperateResult.CreateSuccessResult(cioBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.WR.WordCode)
			{
				return OperateResult.CreateSuccessResult(wBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.HR.WordCode)
			{
				return OperateResult.CreateSuccessResult(hBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.AR.WordCode)
			{
				return OperateResult.CreateSuccessResult(arBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
			}
			return OperateResult.CreateSuccessResult(emBuffer.GetBytes(operateResult.Content.AddressStart / 16 * 2, length * 2));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.WordCode == OmronFinsDataType.DM.WordCode)
			{
				dBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			else if (operateResult.Content.WordCode == OmronFinsDataType.CIO.WordCode)
			{
				cioBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			else if (operateResult.Content.WordCode == OmronFinsDataType.WR.WordCode)
			{
				wBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			else if (operateResult.Content.WordCode == OmronFinsDataType.HR.WordCode)
			{
				hBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			else if (operateResult.Content.WordCode == OmronFinsDataType.AR.WordCode)
			{
				arBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			else
			{
				emBuffer.SetBytes(value, operateResult.Content.AddressStart / 16 * 2);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.DM.BitCode)
			{
				return OperateResult.CreateSuccessResult(dBuffer.GetBool(operateResult.Content.AddressStart, length));
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.CIO.BitCode)
			{
				return OperateResult.CreateSuccessResult(cioBuffer.GetBool(operateResult.Content.AddressStart, length));
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.WR.BitCode)
			{
				return OperateResult.CreateSuccessResult(wBuffer.GetBool(operateResult.Content.AddressStart, length));
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.HR.BitCode)
			{
				return OperateResult.CreateSuccessResult(hBuffer.GetBool(operateResult.Content.AddressStart, length));
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.AR.BitCode)
			{
				return OperateResult.CreateSuccessResult(arBuffer.GetBool(operateResult.Content.AddressStart, length));
			}
			return OperateResult.CreateSuccessResult(emBuffer.GetBool(operateResult.Content.AddressStart, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<OmronFinsAddress> operateResult = OmronFinsAddress.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content.BitCode == OmronFinsDataType.DM.BitCode)
			{
				dBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			else if (operateResult.Content.BitCode == OmronFinsDataType.CIO.BitCode)
			{
				cioBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			else if (operateResult.Content.BitCode == OmronFinsDataType.WR.BitCode)
			{
				wBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			else if (operateResult.Content.BitCode == OmronFinsDataType.HR.BitCode)
			{
				hBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			else if (operateResult.Content.BitCode == OmronFinsDataType.AR.BitCode)
			{
				arBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			else
			{
				emBuffer.SetBool(value, operateResult.Content.AddressStart);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			FinsMessage netMessage = new FinsMessage();
			OperateResult<byte[]> operateResult = ReceiveByMessage(socket, 5000, netMessage);
			if (!operateResult.IsSuccess)
			{
				return;
			}
			OperateResult operateResult2 = Send(socket, SoftBasic.HexStringToBytes("46 49 4E 53 00 00 00 10 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 01"));
			if (operateResult2.IsSuccess)
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
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new FinsMessage());
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
				byte[] back = ReadFromFinsCore(read1.Content.RemoveBegin(26));
				if (back != null)
				{
					session.WorkSocket.Send(back);
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString(' ')}");
					session.UpdateHeartTime();
					RaiseDataReceived(session, read1.Content);
					session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				}
				else
				{
					RemoveClient(session);
				}
			}
			catch
			{
				RemoveClient(session);
			}
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。<br />
		/// The method that should be triggered when a message of the mc protocol is received is allowed to be inherited and rewritten to achieve a custom return or data monitoring.
		/// </summary>
		/// <param name="finsCore">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromFinsCore(byte[] finsCore)
		{
			if (finsCore[0] == 1 && finsCore[1] == 1)
			{
				byte[] array = ReadByCommand(finsCore);
				return PackCommand((array == null) ? 2 : 0, finsCore, array);
			}
			if (finsCore[0] == 1 && finsCore[1] == 2)
			{
				if (!base.EnableWrite)
				{
					return PackCommand(3, finsCore, null);
				}
				return PackCommand(0, finsCore, WriteByMessage(finsCore));
			}
			if (finsCore[0] == 4 && finsCore[1] == 1)
			{
				return PackCommand(0, finsCore, null);
			}
			if (finsCore[0] == 4 && finsCore[1] == 2)
			{
				return PackCommand(0, finsCore, null);
			}
			return null;
		}

		/// <summary>
		/// 将核心报文打包的方法，追加报文头<br />
		/// The method of packing the core message, adding the message header
		/// </summary>
		/// <param name="status">错误码</param>
		/// <param name="finsCore">Fins的核心报文</param>
		/// <param name="data">核心的内容</param>
		/// <returns>完整的报文信息</returns>
		protected virtual byte[] PackCommand(int status, byte[] finsCore, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[30 + data.Length];
			SoftBasic.HexStringToBytes("46 49 4E 53 00 00 00 0000 00 00 00 00 00 00 0000 00 00 00 00 00 00 00 00 00 00 00 00 00").CopyTo(array, 0);
			if (data.Length != 0)
			{
				data.CopyTo(array, 30);
			}
			array[26] = finsCore[0];
			array[27] = finsCore[1];
			BitConverter.GetBytes(array.Length - 8).ReverseNew().CopyTo(array, 4);
			BitConverter.GetBytes(status).ReverseNew().CopyTo(array, 12);
			return array;
		}

		private byte[] ReadByCommand(byte[] command)
		{
			if (command[2] == OmronFinsDataType.DM.BitCode || command[2] == OmronFinsDataType.CIO.BitCode || command[2] == OmronFinsDataType.WR.BitCode || command[2] == OmronFinsDataType.HR.BitCode || command[2] == OmronFinsDataType.AR.BitCode || (32 <= command[2] && command[2] < 48) || (208 <= command[2] && command[2] < 224))
			{
				ushort length = (ushort)(command[6] * 256 + command[7]);
				int destIndex = (command[3] * 256 + command[4]) * 16 + command[5];
				if (command[2] == OmronFinsDataType.DM.BitCode)
				{
					return (from m in dBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				if (command[2] == OmronFinsDataType.CIO.BitCode)
				{
					return (from m in cioBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				if (command[2] == OmronFinsDataType.WR.BitCode)
				{
					return (from m in wBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				if (command[2] == OmronFinsDataType.HR.BitCode)
				{
					return (from m in hBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				if (command[2] == OmronFinsDataType.AR.BitCode)
				{
					return (from m in arBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				if ((32 <= command[2] && command[2] < 48) || (208 <= command[2] && command[2] < 224))
				{
					return (from m in emBuffer.GetBool(destIndex, length)
						select (byte)(m ? 1 : 0)).ToArray();
				}
				throw new Exception(StringResources.Language.NotSupportedDataType);
			}
			if (command[2] == OmronFinsDataType.DM.WordCode || command[2] == OmronFinsDataType.CIO.WordCode || command[2] == OmronFinsDataType.WR.WordCode || command[2] == OmronFinsDataType.HR.WordCode || command[2] == OmronFinsDataType.AR.WordCode || (160 <= command[2] && command[2] < 176) || (80 <= command[2] && command[2] < 96))
			{
				ushort num = (ushort)(command[6] * 256 + command[7]);
				int num2 = command[3] * 256 + command[4];
				if (num > 999)
				{
					return null;
				}
				if (command[2] == OmronFinsDataType.DM.WordCode)
				{
					return dBuffer.GetBytes(num2 * 2, num * 2);
				}
				if (command[2] == OmronFinsDataType.CIO.WordCode)
				{
					return cioBuffer.GetBytes(num2 * 2, num * 2);
				}
				if (command[2] == OmronFinsDataType.WR.WordCode)
				{
					return wBuffer.GetBytes(num2 * 2, num * 2);
				}
				if (command[2] == OmronFinsDataType.HR.WordCode)
				{
					return hBuffer.GetBytes(num2 * 2, num * 2);
				}
				if (command[2] == OmronFinsDataType.AR.WordCode)
				{
					return arBuffer.GetBytes(num2 * 2, num * 2);
				}
				if ((160 <= command[2] && command[2] < 176) || (80 <= command[2] && command[2] < 96))
				{
					return emBuffer.GetBytes(num2 * 2, num * 2);
				}
				throw new Exception(StringResources.Language.NotSupportedDataType);
			}
			return new byte[0];
		}

		private byte[] WriteByMessage(byte[] command)
		{
			if (command[2] == OmronFinsDataType.DM.BitCode || command[2] == OmronFinsDataType.CIO.BitCode || command[2] == OmronFinsDataType.WR.BitCode || command[2] == OmronFinsDataType.HR.BitCode || command[2] == OmronFinsDataType.AR.BitCode || (32 <= command[2] && command[2] < 48) || (208 <= command[2] && command[2] < 224))
			{
				ushort num = (ushort)(command[6] * 256 + command[7]);
				int destIndex = (command[3] * 256 + command[4]) * 16 + command[5];
				bool[] value = (from m in SoftBasic.ArrayRemoveBegin(command, 8)
					select m == 1).ToArray();
				if (command[2] == OmronFinsDataType.DM.BitCode)
				{
					dBuffer.SetBool(value, destIndex);
				}
				else if (command[2] == OmronFinsDataType.CIO.BitCode)
				{
					cioBuffer.SetBool(value, destIndex);
				}
				else if (command[2] == OmronFinsDataType.WR.BitCode)
				{
					wBuffer.SetBool(value, destIndex);
				}
				else if (command[2] == OmronFinsDataType.HR.BitCode)
				{
					hBuffer.SetBool(value, destIndex);
				}
				else if (command[2] == OmronFinsDataType.AR.BitCode)
				{
					arBuffer.SetBool(value, destIndex);
				}
				else
				{
					if ((32 > command[2] || command[2] >= 48) && (208 > command[2] || command[2] >= 224))
					{
						throw new Exception(StringResources.Language.NotSupportedDataType);
					}
					emBuffer.SetBool(value, destIndex);
				}
				return new byte[0];
			}
			ushort num2 = (ushort)(command[6] * 256 + command[7]);
			int num3 = command[3] * 256 + command[4];
			byte[] data = SoftBasic.ArrayRemoveBegin(command, 8);
			if (command[2] == OmronFinsDataType.DM.WordCode)
			{
				dBuffer.SetBytes(data, num3 * 2);
			}
			else if (command[2] == OmronFinsDataType.CIO.WordCode)
			{
				cioBuffer.SetBytes(data, num3 * 2);
			}
			else if (command[2] == OmronFinsDataType.WR.WordCode)
			{
				wBuffer.SetBytes(data, num3 * 2);
			}
			else if (command[2] == OmronFinsDataType.HR.WordCode)
			{
				hBuffer.SetBytes(data, num3 * 2);
			}
			else if (command[2] == OmronFinsDataType.AR.WordCode)
			{
				arBuffer.SetBytes(data, num3 * 2);
			}
			else
			{
				if ((160 > command[2] || command[2] >= 176) && (80 > command[2] || command[2] >= 96))
				{
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
				emBuffer.SetBytes(data, num3 * 2);
			}
			return new byte[0];
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 786432)
			{
				throw new Exception("File is not correct");
			}
			dBuffer.SetBytes(content, 0, 0, 131072);
			cioBuffer.SetBytes(content, 131072, 0, 131072);
			wBuffer.SetBytes(content, 262144, 0, 131072);
			hBuffer.SetBytes(content, 393216, 0, 131072);
			arBuffer.SetBytes(content, 524288, 0, 131072);
			emBuffer.SetBytes(content, 655360, 0, 131072);
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[786432];
			Array.Copy(dBuffer.GetBytes(), 0, array, 0, 131072);
			Array.Copy(cioBuffer.GetBytes(), 0, array, 131072, 131072);
			Array.Copy(wBuffer.GetBytes(), 0, array, 262144, 131072);
			Array.Copy(hBuffer.GetBytes(), 0, array, 393216, 131072);
			Array.Copy(arBuffer.GetBytes(), 0, array, 524288, 131072);
			Array.Copy(emBuffer.GetBytes(), 0, array, 655360, 131072);
			return array;
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				dBuffer?.Dispose();
				cioBuffer?.Dispose();
				wBuffer?.Dispose();
				hBuffer?.Dispose();
				arBuffer?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronFinsServer[{base.Port}]";
		}
	}
}

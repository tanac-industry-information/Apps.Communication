using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Keyence
{
	/// <summary>
	/// 基恩士的上位链路协议的虚拟服务器
	/// </summary>
	public class KeyenceNanoServer : NetworkDataServerBase
	{
		private SoftBuffer rBuffer;

		private SoftBuffer bBuffer;

		private SoftBuffer mrBuffer;

		private SoftBuffer lrBuffer;

		private SoftBuffer crBuffer;

		private SoftBuffer vbBuffer;

		private SoftBuffer dmBuffer;

		private SoftBuffer emBuffer;

		private SoftBuffer wBuffer;

		private SoftBuffer atBuffer;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 实例化一个基于上位链路协议的虚拟的基恩士PLC对象，可以用来和<see cref="T:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp" />进行通信测试。
		/// </summary>
		public KeyenceNanoServer()
		{
			rBuffer = new SoftBuffer(65536);
			bBuffer = new SoftBuffer(65536);
			mrBuffer = new SoftBuffer(65536);
			lrBuffer = new SoftBuffer(65536);
			crBuffer = new SoftBuffer(65536);
			vbBuffer = new SoftBuffer(65536);
			dmBuffer = new SoftBuffer(131072);
			emBuffer = new SoftBuffer(131072);
			wBuffer = new SoftBuffer(131072);
			atBuffer = new SoftBuffer(65536);
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[851968];
			rBuffer.GetBytes().CopyTo(array, 0);
			bBuffer.GetBytes().CopyTo(array, 65536);
			mrBuffer.GetBytes().CopyTo(array, 131072);
			lrBuffer.GetBytes().CopyTo(array, 196608);
			crBuffer.GetBytes().CopyTo(array, 262144);
			vbBuffer.GetBytes().CopyTo(array, 327680);
			dmBuffer.GetBytes().CopyTo(array, 393216);
			emBuffer.GetBytes().CopyTo(array, 524288);
			wBuffer.GetBytes().CopyTo(array, 655360);
			atBuffer.GetBytes().CopyTo(array, 786432);
			return array;
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 851968)
			{
				throw new Exception("File is not correct");
			}
			rBuffer.SetBytes(content, 0, 65536);
			bBuffer.SetBytes(content, 65536, 65536);
			mrBuffer.SetBytes(content, 131072, 65536);
			lrBuffer.SetBytes(content, 196608, 65536);
			crBuffer.SetBytes(content, 262144, 65536);
			vbBuffer.SetBytes(content, 327680, 65536);
			dmBuffer.SetBytes(content, 393216, 131072);
			emBuffer.SetBytes(content, 524288, 131072);
			wBuffer.SetBytes(content, 655360, 131072);
			atBuffer.SetBytes(content, 786432, 65536);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			try
			{
				if (address.StartsWith("DM"))
				{
					return OperateResult.CreateSuccessResult(dmBuffer.GetBytes(int.Parse(address.Substring(2)) * 2, length * 2));
				}
				if (address.StartsWith("EM"))
				{
					return OperateResult.CreateSuccessResult(emBuffer.GetBytes(int.Parse(address.Substring(2)) * 2, length * 2));
				}
				if (address.StartsWith("W"))
				{
					return OperateResult.CreateSuccessResult(wBuffer.GetBytes(int.Parse(address.Substring(1)) * 2, length * 2));
				}
				if (address.StartsWith("AT"))
				{
					return OperateResult.CreateSuccessResult(atBuffer.GetBytes(int.Parse(address.Substring(2)) * 4, length * 4));
				}
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType + " Reason:" + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Keyence.KeyenceNanoSerialOverTcp.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			try
			{
				if (address.StartsWith("DM"))
				{
					dmBuffer.SetBytes(value, int.Parse(address.Substring(2)) * 2);
				}
				else if (address.StartsWith("EM"))
				{
					emBuffer.SetBytes(value, int.Parse(address.Substring(2)) * 2);
				}
				else if (address.StartsWith("W"))
				{
					wBuffer.SetBytes(value, int.Parse(address.Substring(1)) * 2);
				}
				else
				{
					if (!address.StartsWith("AT"))
					{
						return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
					}
					atBuffer.SetBytes(value, int.Parse(address.Substring(2)) * 4);
				}
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType + " Reason:" + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			try
			{
				if (address.StartsWith("R"))
				{
					return OperateResult.CreateSuccessResult((from m in rBuffer.GetBytes(int.Parse(address.Substring(1)), length)
						select m != 0).ToArray());
				}
				if (address.StartsWith("B"))
				{
					return OperateResult.CreateSuccessResult((from m in bBuffer.GetBytes(int.Parse(address.Substring(1)), length)
						select m != 0).ToArray());
				}
				if (address.StartsWith("MR"))
				{
					return OperateResult.CreateSuccessResult((from m in mrBuffer.GetBytes(int.Parse(address.Substring(2)), length)
						select m != 0).ToArray());
				}
				if (address.StartsWith("LR"))
				{
					return OperateResult.CreateSuccessResult((from m in lrBuffer.GetBytes(int.Parse(address.Substring(2)), length)
						select m != 0).ToArray());
				}
				if (address.StartsWith("CR"))
				{
					return OperateResult.CreateSuccessResult((from m in crBuffer.GetBytes(int.Parse(address.Substring(2)), length)
						select m != 0).ToArray());
				}
				if (address.StartsWith("VB"))
				{
					return OperateResult.CreateSuccessResult((from m in vbBuffer.GetBytes(int.Parse(address.Substring(2)), length)
						select m != 0).ToArray());
				}
				return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType + " Reason:" + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			try
			{
				byte[] data = value.Select((bool m) => (byte)(m ? 1 : 0)).ToArray();
				if (address.StartsWith("R"))
				{
					rBuffer.SetBytes(data, int.Parse(address.Substring(1)));
				}
				else if (address.StartsWith("B"))
				{
					bBuffer.SetBytes(data, int.Parse(address.Substring(1)));
				}
				else if (address.StartsWith("MR"))
				{
					mrBuffer.SetBytes(data, int.Parse(address.Substring(2)));
				}
				else if (address.StartsWith("LR"))
				{
					lrBuffer.SetBytes(data, int.Parse(address.Substring(2)));
				}
				else if (address.StartsWith("CR"))
				{
					crBuffer.SetBytes(data, int.Parse(address.Substring(2)));
				}
				else
				{
					if (!address.StartsWith("VB"))
					{
						return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
					}
					vbBuffer.SetBytes(data, int.Parse(address.Substring(2)));
				}
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType + " Reason:" + ex.Message);
			}
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			OperateResult<byte[]> operateResult = ReceiveCommandLineFromSocket(socket, 13, 5000);
			if (!operateResult.IsSuccess)
			{
				socket?.Close();
				return;
			}
			if (!Encoding.ASCII.GetString(operateResult.Content).StartsWith("CR"))
			{
				socket?.Close();
				return;
			}
			OperateResult operateResult2 = Send(socket, Encoding.ASCII.GetBytes("CC\r\n"));
			if (operateResult2.IsSuccess)
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
			OperateResult<byte[]> read1 = await ReceiveCommandLineFromSocketAsync(session.WorkSocket, 13, 5000);
			if (!read1.IsSuccess)
			{
				RemoveClient(session);
				return;
			}
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{SoftBasic.GetAsciiStringRender(read1.Content)}");
			byte[] back = ReadFromNanoCore(read1.Content);
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
			base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{SoftBasic.GetAsciiStringRender(back)}");
			session.UpdateHeartTime();
			RaiseDataReceived(session, read1.Content);
			if (!session.WorkSocket.BeginReceiveResult(SocketAsyncCallBack, session).IsSuccess)
			{
				RemoveClient(session);
			}
		}

		private byte[] GetBoolResponseData(byte[] data)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
			{
				stringBuilder.Append(data[i]);
				if (i != data.Length - 1)
				{
					stringBuilder.Append(" ");
				}
			}
			stringBuilder.Append("\r\n");
			return Encoding.ASCII.GetBytes(stringBuilder.ToString());
		}

		private byte[] GetWordResponseData(byte[] data)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < data.Length / 2; i++)
			{
				stringBuilder.Append(BitConverter.ToUInt16(data, i * 2));
				if (i != data.Length / 2 - 1)
				{
					stringBuilder.Append(" ");
				}
			}
			stringBuilder.Append("\r\n");
			return Encoding.ASCII.GetBytes(stringBuilder.ToString());
		}

		private byte[] GetDoubleWordResponseData(byte[] data)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < data.Length / 4; i++)
			{
				stringBuilder.Append(BitConverter.ToUInt32(data, i * 4));
				if (i != data.Length / 4 - 1)
				{
					stringBuilder.Append(" ");
				}
			}
			stringBuilder.Append("\r\n");
			return Encoding.ASCII.GetBytes(stringBuilder.ToString());
		}

		private byte[] ReadFromNanoCore(byte[] receive)
		{
			string[] array = Encoding.ASCII.GetString(receive).Trim('\r', '\n').Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (array[0] == "ER")
			{
				return Encoding.ASCII.GetBytes("OK\r\n");
			}
			if (array[0] == "RD" || array[0] == "RDS")
			{
				return ReadByCommand(array);
			}
			if (array[0] == "WR" || array[0] == "WRS")
			{
				return WriteByCommand(array);
			}
			if (array[0] == "ST")
			{
				return WriteByCommand(new string[4]
				{
					"WRS",
					array[1],
					"1",
					"1"
				});
			}
			if (array[0] == "RS")
			{
				return WriteByCommand(new string[4]
				{
					"WRS",
					array[1],
					"1",
					"0"
				});
			}
			if (array[0] == "?K")
			{
				return Encoding.ASCII.GetBytes("53\r\n");
			}
			if (array[0] == "?M")
			{
				return Encoding.ASCII.GetBytes("1\r\n");
			}
			return Encoding.ASCII.GetBytes("E0\r\n");
		}

		private byte[] ReadByCommand(string[] command)
		{
			try
			{
				if (command[1].EndsWith(".U") || command[1].EndsWith(".S") || command[1].EndsWith(".D") || command[1].EndsWith(".L") || command[1].EndsWith(".H"))
				{
					command[1] = command[1].Remove(command[1].Length - 2);
				}
				int num = ((command.Length <= 2) ? 1 : int.Parse(command[2]));
				if (num > 256)
				{
					return Encoding.ASCII.GetBytes("E0\r\n");
				}
				if (Regex.IsMatch(command[1], "^[0-9]+$"))
				{
					command[1] = "R" + command[1];
				}
				if (command[1].StartsWith("R"))
				{
					return GetBoolResponseData(rBuffer.GetBytes(int.Parse(command[1].Substring(1)), num));
				}
				if (command[1].StartsWith("B"))
				{
					return GetBoolResponseData(bBuffer.GetBytes(int.Parse(command[1].Substring(1)), num));
				}
				if (command[1].StartsWith("MR"))
				{
					return GetBoolResponseData(mrBuffer.GetBytes(int.Parse(command[1].Substring(2)), num));
				}
				if (command[1].StartsWith("LR"))
				{
					return GetBoolResponseData(lrBuffer.GetBytes(int.Parse(command[1].Substring(2)), num));
				}
				if (command[1].StartsWith("CR"))
				{
					return GetBoolResponseData(crBuffer.GetBytes(int.Parse(command[1].Substring(2)), num));
				}
				if (command[1].StartsWith("VB"))
				{
					return GetBoolResponseData(vbBuffer.GetBytes(int.Parse(command[1].Substring(2)), num));
				}
				if (command[1].StartsWith("DM"))
				{
					return GetWordResponseData(dmBuffer.GetBytes(int.Parse(command[1].Substring(2)) * 2, num * 2));
				}
				if (command[1].StartsWith("EM"))
				{
					return GetWordResponseData(emBuffer.GetBytes(int.Parse(command[1].Substring(2)) * 2, num * 2));
				}
				if (command[1].StartsWith("W"))
				{
					return GetWordResponseData(wBuffer.GetBytes(int.Parse(command[1].Substring(1)) * 2, num * 2));
				}
				if (command[1].StartsWith("AT"))
				{
					return GetDoubleWordResponseData(atBuffer.GetBytes(int.Parse(command[1].Substring(2)) * 4, num * 4));
				}
				return Encoding.ASCII.GetBytes("E0\r\n");
			}
			catch
			{
				return Encoding.ASCII.GetBytes("E1\r\n");
			}
		}

		private byte[] WriteByCommand(string[] command)
		{
			if (!base.EnableWrite)
			{
				return Encoding.ASCII.GetBytes("E4\r\n");
			}
			try
			{
				if (command[1].EndsWith(".U") || command[1].EndsWith(".S") || command[1].EndsWith(".D") || command[1].EndsWith(".L") || command[1].EndsWith(".H"))
				{
					command[1] = command[1].Remove(command[1].Length - 2);
				}
				int num = ((!(command[0] == "WRS")) ? 1 : int.Parse(command[2]));
				if (num > 256)
				{
					return Encoding.ASCII.GetBytes("E0\r\n");
				}
				if (Regex.IsMatch(command[1], "^[0-9]+$"))
				{
					command[1] = "R" + command[1];
				}
				if (command[1].StartsWith("R") || command[1].StartsWith("B") || command[1].StartsWith("MR") || command[1].StartsWith("LR") || command[1].StartsWith("CR") || command[1].StartsWith("VB"))
				{
					byte[] data = (from m in command.RemoveBegin((command[0] == "WRS") ? 3 : 2)
						select byte.Parse(m)).ToArray();
					if (command[1].StartsWith("R"))
					{
						rBuffer.SetBytes(data, int.Parse(command[1].Substring(1)));
					}
					else if (command[1].StartsWith("B"))
					{
						bBuffer.SetBytes(data, int.Parse(command[1].Substring(1)));
					}
					else if (command[1].StartsWith("MR"))
					{
						mrBuffer.SetBytes(data, int.Parse(command[1].Substring(2)));
					}
					else if (command[1].StartsWith("LR"))
					{
						lrBuffer.SetBytes(data, int.Parse(command[1].Substring(2)));
					}
					else if (command[1].StartsWith("CR"))
					{
						crBuffer.SetBytes(data, int.Parse(command[1].Substring(2)));
					}
					else
					{
						if (!command[1].StartsWith("VB"))
						{
							return Encoding.ASCII.GetBytes("E0\r\n");
						}
						vbBuffer.SetBytes(data, int.Parse(command[1].Substring(2)));
					}
				}
				else
				{
					byte[] data2 = base.ByteTransform.TransByte((from m in command.RemoveBegin((command[0] == "WRS") ? 3 : 2)
						select ushort.Parse(m)).ToArray());
					if (command[1].StartsWith("DM"))
					{
						dmBuffer.SetBytes(data2, int.Parse(command[1].Substring(2)) * 2);
					}
					else if (command[1].StartsWith("EM"))
					{
						emBuffer.SetBytes(data2, int.Parse(command[1].Substring(2)) * 2);
					}
					else if (command[1].StartsWith("W"))
					{
						wBuffer.SetBytes(data2, int.Parse(command[1].Substring(1)) * 2);
					}
					else
					{
						if (!command[1].StartsWith("AT"))
						{
							return Encoding.ASCII.GetBytes("E0\r\n");
						}
						atBuffer.SetBytes(data2, int.Parse(command[1].Substring(2)) * 4);
					}
				}
				return Encoding.ASCII.GetBytes("OK\r\n");
			}
			catch
			{
				return Encoding.ASCII.GetBytes("E1\r\n");
			}
		}

		/// <inheritdoc />
		protected override bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			return buffer[receivedLength - 1] == 13;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Receive + "：" + SoftBasic.GetAsciiStringRender(data));
			byte[] array = ReadFromNanoCore(data);
			base.LogNet?.WriteDebug(ToString(), "[" + GetSerialPort().PortName + "] " + StringResources.Language.Send + "：" + SoftBasic.GetAsciiStringRender(array));
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"KeyenceNanoServer[{base.Port}]";
		}
	}
}

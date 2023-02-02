using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Siemens
{
	/// <summary>
	/// <b>[商业授权]</b> 西门子S7协议的虚拟服务器，支持TCP协议，模拟的是1200的PLC进行通信，在客户端进行操作操作的时候，最好是选择1200的客户端对象进行通信。<br />
	/// <b>[Authorization]</b> The virtual server of Siemens S7 protocol supports TCP protocol. It simulates 1200 PLC for communication. When the client is operating, it is best to select the 1200 client object for communication.
	/// </summary>
	/// <remarks>
	/// 本西门子的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// <note type="important">对于200smartPLC的V区，就是DB1.X，例如，V100=DB1.100</note>
	/// </remarks>
	/// <example>
	/// 地址支持的列表如下：
	/// <list type="table">
	///   <listheader>
	///     <term>地址名称</term>
	///     <term>地址代号</term>
	///     <term>示例</term>
	///     <term>地址进制</term>
	///     <term>字操作</term>
	///     <term>位操作</term>
	///     <term>备注</term>
	///   </listheader>
	///   <item>
	///     <term>中间寄存器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入寄存器</term>
	///     <term>I</term>
	///     <term>I100,I200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出寄存器</term>
	///     <term>Q</term>
	///     <term>Q100,Q200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>DB块寄存器</term>
	///     <term>DB</term>
	///     <term>DB1.100,DB1.200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>V寄存器</term>
	///     <term>V</term>
	///     <term>V100,V200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>V寄存器本质就是DB块1</term>
	///   </item>
	///   <item>
	///     <term>定时器的值</term>
	///     <term>T</term>
	///     <term>T100,T200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>未测试通过</term>
	///   </item>
	///   <item>
	///     <term>计数器的值</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term>未测试通过</term>
	///   </item>
	/// </list>
	/// 你可以很快速并且简单的创建一个虚拟的s7服务器
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="UseExample1" title="简单的创建服务器" />
	/// 当然如果需要高级的服务器，指定日志，限制客户端的IP地址，获取客户端发送的信息，在服务器初始化的时候就要参照下面的代码：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="UseExample4" title="定制服务器" />
	/// 服务器创建好之后，我们就可以对服务器进行一些读写的操作了，下面的代码是基础的BCL类型的读写操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="ReadWriteExample" title="基础的读写示例" />
	/// 高级的对于byte数组类型的数据进行批量化的读写操作如下：   
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Profinet\SiemensS7ServerExample.cs" region="BytesReadWrite" title="字节的读写示例" />
	/// 更高级操作请参见源代码。
	/// </example>
	public class SiemensS7Server : NetworkDataServerBase
	{
		private SoftBuffer inputBuffer;

		private SoftBuffer outputBuffer;

		private SoftBuffer memeryBuffer;

		private SoftBuffer countBuffer;

		private SoftBuffer timerBuffer;

		private SoftBuffer db1BlockBuffer;

		private SoftBuffer db2BlockBuffer;

		private SoftBuffer db3BlockBuffer;

		private SoftBuffer dbOtherBlockBuffer;

		private SoftBuffer aiBuffer;

		private SoftBuffer aqBuffer;

		private const int DataPoolLength = 65536;

		/// <summary>
		/// 实例化一个S7协议的服务器，支持I，Q，M，DB1.X, DB2.X, DB3.X 数据区块的读写操作<br />
		/// Instantiate a server with S7 protocol, support I, Q, M, DB1.X data block read and write operations
		/// </summary>
		public SiemensS7Server()
		{
			inputBuffer = new SoftBuffer(65536);
			outputBuffer = new SoftBuffer(65536);
			memeryBuffer = new SoftBuffer(65536);
			db1BlockBuffer = new SoftBuffer(65536);
			db2BlockBuffer = new SoftBuffer(65536);
			db3BlockBuffer = new SoftBuffer(65536);
			dbOtherBlockBuffer = new SoftBuffer(65536);
			countBuffer = new SoftBuffer(131072);
			timerBuffer = new SoftBuffer(131072);
			aiBuffer = new SoftBuffer(65536);
			aqBuffer = new SoftBuffer(65536);
			base.WordLength = 2;
			base.ByteTransform = new ReverseBytesTransform();
		}

		private OperateResult<SoftBuffer> GetDataAreaFromS7Address(S7AddressData s7Address)
		{
			switch (s7Address.DataCode)
			{
			case 129:
				return OperateResult.CreateSuccessResult(inputBuffer);
			case 130:
				return OperateResult.CreateSuccessResult(outputBuffer);
			case 131:
				return OperateResult.CreateSuccessResult(memeryBuffer);
			case 132:
				if (s7Address.DbBlock == 1)
				{
					return OperateResult.CreateSuccessResult(db1BlockBuffer);
				}
				if (s7Address.DbBlock == 2)
				{
					return OperateResult.CreateSuccessResult(db2BlockBuffer);
				}
				if (s7Address.DbBlock == 3)
				{
					return OperateResult.CreateSuccessResult(db3BlockBuffer);
				}
				return OperateResult.CreateSuccessResult(dbOtherBlockBuffer);
			case 30:
				return OperateResult.CreateSuccessResult(countBuffer);
			case 31:
				return OperateResult.CreateSuccessResult(timerBuffer);
			case 6:
				return OperateResult.CreateSuccessResult(aiBuffer);
			case 7:
				return OperateResult.CreateSuccessResult(aqBuffer);
			default:
				return new OperateResult<SoftBuffer>(StringResources.Language.NotSupportedDataType);
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<S7AddressData> operateResult = S7AddressData.ParseFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(operateResult.Content);
			if (!dataAreaFromS7Address.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataAreaFromS7Address);
			}
			if (operateResult.Content.DataCode == 30 || operateResult.Content.DataCode == 31)
			{
				return OperateResult.CreateSuccessResult(dataAreaFromS7Address.Content.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			}
			return OperateResult.CreateSuccessResult(dataAreaFromS7Address.Content.GetBytes(operateResult.Content.AddressStart / 8, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<S7AddressData> operateResult = S7AddressData.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(operateResult.Content);
			if (!dataAreaFromS7Address.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataAreaFromS7Address);
			}
			if (operateResult.Content.DataCode == 30 || operateResult.Content.DataCode == 31)
			{
				dataAreaFromS7Address.Content.SetBytes(value, operateResult.Content.AddressStart * 2);
			}
			else
			{
				dataAreaFromS7Address.Content.SetBytes(value, operateResult.Content.AddressStart / 8);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.ReadByte(System.String)" />
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.Write(System.String,System.Byte)" />
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.ReadBool(System.String)" />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			OperateResult<S7AddressData> operateResult = S7AddressData.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(operateResult.Content);
			if (!dataAreaFromS7Address.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(dataAreaFromS7Address);
			}
			return OperateResult.CreateSuccessResult(dataAreaFromS7Address.Content.GetBool(operateResult.Content.AddressStart));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.SiemensS7Net.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			OperateResult<S7AddressData> operateResult = S7AddressData.ParseFrom(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(operateResult.Content);
			if (!dataAreaFromS7Address.IsSuccess)
			{
				return dataAreaFromS7Address;
			}
			dataAreaFromS7Address.Content.SetBool(value, operateResult.Content.AddressStart);
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			S7Message netMessage = new S7Message();
			OperateResult<byte[]> operateResult = ReceiveByMessage(socket, 5000, netMessage);
			if (!operateResult.IsSuccess)
			{
				return;
			}
			OperateResult operateResult2 = Send(socket, SoftBasic.HexStringToBytes("03 00 00 16 02 D0 80 32 01 00 00 02 00 00 08 00 00 f0 00 00 01 00"));
			if (!operateResult2.IsSuccess)
			{
				return;
			}
			OperateResult<byte[]> operateResult3 = ReceiveByMessage(socket, 5000, netMessage);
			if (!operateResult3.IsSuccess)
			{
				return;
			}
			OperateResult operateResult4 = Send(socket, SoftBasic.HexStringToBytes("03 00 00 1B 02 f0 80 32 01 00 00 02 00 00 08 00 00 00 00 00 01 00 01 00 f0 00 f0"));
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
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new S7Message());
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
				byte[] receive = read1.Content;
				byte[] back;
				if (receive[17] == 4)
				{
					back = ReadByMessage(receive);
					goto IL_0251;
				}
				if (receive[17] == 5)
				{
					back = WriteByMessage(receive);
					goto IL_0251;
				}
				if (receive[17] == 0)
				{
					back = SoftBasic.HexStringToBytes("03 00 00 7D 02 F0 80 32 07 00 00 00 01 00 0C 00 60 00 01 12 08 12 84 01 01 00 00 00 00 FF 09 00 5C 00 11 00 00 00 1C 00 03 00 01 36 45 53 37 20 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 00 06 20 20 00 06 36 45 53 37 20 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 00 06 20 20 00 07 36 45 53 37 20 32 31 35 2D 31 41 47 34 30 2D 30 58 42 30 20 00 00 56 04 02 01");
					goto IL_0251;
				}
				RemoveClient(session);
				goto end_IL_0040;
				IL_0251:
				session.WorkSocket.Send(back);
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString(' ')}");
				session.UpdateHeartTime();
				RaiseDataReceived(session, receive);
				session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				end_IL_0040:;
			}
			catch
			{
				RemoveClient(session);
			}
		}

		private byte[] ReadByMessage(byte[] packCommand)
		{
			List<byte> list = new List<byte>();
			int num = packCommand[18];
			int num2 = 19;
			for (int i = 0; i < num; i++)
			{
				byte b = packCommand[num2 + 1];
				byte[] command = packCommand.SelectMiddle(num2, b + 2);
				num2 += b + 2;
				list.AddRange(ReadByCommand(command));
			}
			byte[] array = new byte[21 + list.Count];
			SoftBasic.HexStringToBytes("03 00 00 1A 02 F0 80 32 03 00 00 00 01 00 02 00 05 00 00 04 01").CopyTo(array, 0);
			array[2] = (byte)(array.Length / 256);
			array[3] = (byte)(array.Length % 256);
			array[15] = (byte)(list.Count / 256);
			array[16] = (byte)(list.Count % 256);
			array[20] = packCommand[18];
			list.CopyTo(array, 21);
			return array;
		}

		private byte[] ReadByCommand(byte[] command)
		{
			if (command[3] == 1)
			{
				int num = command[9] * 65536 + command[10] * 256 + command[11];
				ushort dbBlock = base.ByteTransform.TransUInt16(command, 6);
				OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(new S7AddressData
				{
					AddressStart = num,
					DataCode = command[8],
					DbBlock = dbBlock,
					Length = 1
				});
				if (!dataAreaFromS7Address.IsSuccess)
				{
					throw new Exception(dataAreaFromS7Address.Message);
				}
				return PackReadBitCommandBack(dataAreaFromS7Address.Content.GetBool(num));
			}
			if (command[3] == 30 || command[3] == 31)
			{
				ushort num2 = base.ByteTransform.TransUInt16(command, 4);
				int num3 = command[9] * 65536 + command[10] * 256 + command[11];
				OperateResult<SoftBuffer> dataAreaFromS7Address2 = GetDataAreaFromS7Address(new S7AddressData
				{
					AddressStart = num3,
					DataCode = command[8],
					DbBlock = 0,
					Length = num2
				});
				if (!dataAreaFromS7Address2.IsSuccess)
				{
					throw new Exception(dataAreaFromS7Address2.Message);
				}
				return PackReadCTCommandBack(dataAreaFromS7Address2.Content.GetBytes(num3 * 2, num2 * 2), (command[3] == 30) ? 3 : 5);
			}
			ushort num4 = base.ByteTransform.TransUInt16(command, 4);
			if (command[3] == 4)
			{
				num4 = (ushort)(num4 * 2);
			}
			ushort dbBlock2 = base.ByteTransform.TransUInt16(command, 6);
			int num5 = (command[9] * 65536 + command[10] * 256 + command[11]) / 8;
			OperateResult<SoftBuffer> dataAreaFromS7Address3 = GetDataAreaFromS7Address(new S7AddressData
			{
				AddressStart = num5,
				DataCode = command[8],
				DbBlock = dbBlock2,
				Length = num4
			});
			if (!dataAreaFromS7Address3.IsSuccess)
			{
				throw new Exception(dataAreaFromS7Address3.Message);
			}
			return PackReadWordCommandBack(dataAreaFromS7Address3.Content.GetBytes(num5, num4));
		}

		private byte[] PackReadWordCommandBack(byte[] result)
		{
			byte[] array = new byte[4 + result.Length];
			array[0] = byte.MaxValue;
			array[1] = 4;
			base.ByteTransform.TransByte((ushort)result.Length).CopyTo(array, 2);
			result.CopyTo(array, 4);
			return array;
		}

		private byte[] PackReadCTCommandBack(byte[] result, int dataLength)
		{
			byte[] array = new byte[4 + result.Length * dataLength / 2];
			array[0] = byte.MaxValue;
			array[1] = 9;
			base.ByteTransform.TransByte((ushort)(array.Length - 4)).CopyTo(array, 2);
			for (int i = 0; i < result.Length / 2; i++)
			{
				result.SelectMiddle(i * 2, 2).CopyTo(array, 4 + dataLength - 2 + i * dataLength);
			}
			return array;
		}

		private byte[] PackReadBitCommandBack(bool value)
		{
			return new byte[5]
			{
				255,
				3,
				0,
				1,
				(byte)(value ? 1u : 0u)
			};
		}

		private byte[] WriteByMessage(byte[] packCommand)
		{
			if (!base.EnableWrite)
			{
				return SoftBasic.HexStringToBytes("03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 04");
			}
			if (packCommand[22] == 2 || packCommand[22] == 4)
			{
				ushort dbBlock = base.ByteTransform.TransUInt16(packCommand, 25);
				int num = base.ByteTransform.TransInt16(packCommand, 23);
				if (packCommand[22] == 4)
				{
					num *= 2;
				}
				int destIndex = (packCommand[28] * 65536 + packCommand[29] * 256 + packCommand[30]) / 8;
				byte[] data = base.ByteTransform.TransByte(packCommand, 35, num);
				OperateResult<SoftBuffer> dataAreaFromS7Address = GetDataAreaFromS7Address(new S7AddressData
				{
					DataCode = packCommand[27],
					DbBlock = dbBlock,
					Length = 1
				});
				if (!dataAreaFromS7Address.IsSuccess)
				{
					throw new Exception(dataAreaFromS7Address.Message);
				}
				dataAreaFromS7Address.Content.SetBytes(data, destIndex);
				return SoftBasic.HexStringToBytes("03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 FF");
			}
			ushort dbBlock2 = base.ByteTransform.TransUInt16(packCommand, 25);
			int destIndex2 = packCommand[28] * 65536 + packCommand[29] * 256 + packCommand[30];
			bool value = packCommand[35] != 0;
			OperateResult<SoftBuffer> dataAreaFromS7Address2 = GetDataAreaFromS7Address(new S7AddressData
			{
				DataCode = packCommand[27],
				DbBlock = dbBlock2,
				Length = 1
			});
			if (!dataAreaFromS7Address2.IsSuccess)
			{
				throw new Exception(dataAreaFromS7Address2.Message);
			}
			dataAreaFromS7Address2.Content.SetBool(value, destIndex2);
			return SoftBasic.HexStringToBytes("03 00 00 16 02 F0 80 32 03 00 00 00 01 00 02 00 01 00 00 05 01 FF");
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 458752)
			{
				throw new Exception("File is not correct");
			}
			inputBuffer.SetBytes(content, 0, 0, 65536);
			outputBuffer.SetBytes(content, 65536, 0, 65536);
			memeryBuffer.SetBytes(content, 131072, 0, 65536);
			db1BlockBuffer.SetBytes(content, 196608, 0, 65536);
			db2BlockBuffer.SetBytes(content, 262144, 0, 65536);
			db3BlockBuffer.SetBytes(content, 327680, 0, 65536);
			dbOtherBlockBuffer.SetBytes(content, 393216, 0, 65536);
			if (content.Length >= 720896)
			{
				countBuffer.SetBytes(content, 458752, 0, 131072);
				timerBuffer.SetBytes(content, 589824, 0, 131072);
			}
		}

		/// <inheritdoc />
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[720896];
			Array.Copy(inputBuffer.GetBytes(), 0, array, 0, 65536);
			Array.Copy(outputBuffer.GetBytes(), 0, array, 65536, 65536);
			Array.Copy(memeryBuffer.GetBytes(), 0, array, 131072, 65536);
			Array.Copy(db1BlockBuffer.GetBytes(), 0, array, 196608, 65536);
			Array.Copy(db2BlockBuffer.GetBytes(), 0, array, 262144, 65536);
			Array.Copy(db3BlockBuffer.GetBytes(), 0, array, 327680, 65536);
			Array.Copy(dbOtherBlockBuffer.GetBytes(), 0, array, 393216, 65536);
			Array.Copy(countBuffer.GetBytes(), 0, array, 458752, 131072);
			Array.Copy(timerBuffer.GetBytes(), 0, array, 589824, 131072);
			return array;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				inputBuffer?.Dispose();
				outputBuffer?.Dispose();
				memeryBuffer?.Dispose();
				db1BlockBuffer?.Dispose();
				db2BlockBuffer?.Dispose();
				db3BlockBuffer?.Dispose();
				dbOtherBlockBuffer?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"SiemensS7Server[{base.Port}]";
		}
	}
}

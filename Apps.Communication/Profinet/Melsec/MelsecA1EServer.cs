using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Net;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// <b>[商业授权]</b> 三菱MC-A1E协议的虚拟服务器，支持M,X,Y,D,W的数据池读写操作，支持二进制及ASCII格式进行读写操作，需要在实例化的时候指定。<br />
	/// <b>[Authorization]</b> The Mitsubishi MC-A1E protocol virtual server supports M, X, Y, D, W data pool read and write operations, 
	/// and supports binary and ASCII format read and write operations, which need to be specified during instantiation.
	/// </summary>
	/// <remarks>
	/// 本三菱的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// 如果你没有可以测试的三菱PLC，想要测试自己开发的上位机软件，或是想要在本机实现虚拟PLC，然后进行IO的输入输出练习，都可以使用本类来实现，先来说明下地址信息
	/// <br />
	/// 地址的输入的格式说明如下：
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
	///     <term>内部继电器</term>
	///     <term>M</term>
	///     <term>M100,M200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输入继电器</term>
	///     <term>X</term>
	///     <term>X100,X1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>输出继电器</term>
	///     <term>Y</term>
	///     <term>Y100,Y1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>数据寄存器</term>
	///     <term>D</term>
	///     <term>D1000,D2000</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>链接寄存器</term>
	///     <term>W</term>
	///     <term>W100,W1A0</term>
	///     <term>16</term>
	///     <term>√</term>
	///     <term>×</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	public class MelsecA1EServer : MelsecMcServer
	{
		/// <summary>
		/// 实例化一个默认参数的mc协议的服务器<br />
		/// Instantiate a mc protocol server with default parameters
		/// </summary>
		/// <param name="isBinary">是否是二进制，默认是二进制，否则是ASCII格式</param>
		public MelsecA1EServer(bool isBinary = true)
			: base(isBinary)
		{
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
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
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, null);
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}
				byte[] back = ((!base.IsBinary) ? ReadFromMcAsciiCore(read1.Content) : ReadFromMcCore(read1.Content));
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{(base.IsBinary ? read1.Content.ToHexString(' ') : SoftBasic.GetAsciiStringRender(read1.Content))}");
				if (back != null)
				{
					session.WorkSocket.Send(back);
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{(base.IsBinary ? back.ToHexString(' ') : SoftBasic.GetAsciiStringRender(back))}");
					session.UpdateHeartTime();
					RaiseDataReceived(session, read1.Content);
					session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				}
				else
				{
					RemoveClient(session);
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				RemoveClient(session, "SocketAsyncCallBack -> " + ex.Message);
			}
		}

		private byte[] PackResponseCommand(byte[] mcCore, byte err, byte code, byte[] data)
		{
			byte[] array = new byte[2]
			{
				(byte)(mcCore[0] + 128),
				err
			};
			switch (err)
			{
			case 91:
				return SoftBasic.SpliceArray<byte>(array, new byte[1] { code });
			default:
				return array;
			case 0:
				if (data == null)
				{
					return array;
				}
				return SoftBasic.SpliceArray<byte>(array, data);
			}
		}

		private byte[] PackResponseCommand(byte[] mcCore, byte err, byte code, bool[] data)
		{
			byte[] array = new byte[2]
			{
				(byte)(mcCore[0] + 128),
				err
			};
			switch (err)
			{
			case 91:
				return SoftBasic.SpliceArray<byte>(array, new byte[1] { code });
			default:
				return array;
			case 0:
				if (data == null)
				{
					return array;
				}
				return SoftBasic.SpliceArray<byte>(array, MelsecHelper.TransBoolArrayToByteData(data));
			}
		}

		private string GetAddressFromDataCode(ushort dataCode, int address)
		{
			if (dataCode == MelsecA1EDataType.M.DataCode)
			{
				return "M" + address;
			}
			if (dataCode == MelsecA1EDataType.X.DataCode)
			{
				return "X" + address.ToString("X");
			}
			if (dataCode == MelsecA1EDataType.Y.DataCode)
			{
				return "Y" + address.ToString("X");
			}
			if (dataCode == MelsecA1EDataType.S.DataCode)
			{
				return "S" + address;
			}
			if (dataCode == MelsecA1EDataType.F.DataCode)
			{
				return "F" + address;
			}
			if (dataCode == MelsecA1EDataType.B.DataCode)
			{
				return "B" + address.ToString("X");
			}
			if (dataCode == MelsecA1EDataType.D.DataCode)
			{
				return "D" + address;
			}
			if (dataCode == MelsecA1EDataType.R.DataCode)
			{
				return "R" + address;
			}
			if (dataCode == MelsecA1EDataType.W.DataCode)
			{
				return "W" + address.ToString("X");
			}
			return string.Empty;
		}

		/// <inheritdoc />
		protected override byte[] ReadFromMcCore(byte[] mcCore)
		{
			try
			{
				int address = BitConverter.ToInt32(mcCore, 4);
				ushort num = BitConverter.ToUInt16(mcCore, 8);
				ushort length = BitConverter.ToUInt16(mcCore, 10);
				string addressFromDataCode = GetAddressFromDataCode(num, address);
				if (mcCore[0] == 0)
				{
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode)
					{
						OperateResult<bool[]> operateResult = ReadBool(addressFromDataCode, length);
						if (!operateResult.IsSuccess)
						{
							return PackResponseCommand(mcCore, 16, 0, new bool[0]);
						}
						return PackResponseCommand(mcCore, 0, 0, operateResult.Content);
					}
				}
				else if (mcCore[0] == 1)
				{
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode || num == MelsecA1EDataType.D.DataCode || num == MelsecA1EDataType.R.DataCode || num == MelsecA1EDataType.W.DataCode)
					{
						OperateResult<byte[]> operateResult2 = Read(addressFromDataCode, length);
						if (!operateResult2.IsSuccess)
						{
							return PackResponseCommand(mcCore, 16, 0, new bool[0]);
						}
						return PackResponseCommand(mcCore, 0, 0, operateResult2.Content);
					}
				}
				else if (mcCore[0] == 2)
				{
					bool[] value = MelsecHelper.TransByteArrayToBoolData(mcCore, 12, length);
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode)
					{
						OperateResult operateResult3 = Write(addressFromDataCode, value);
						if (!operateResult3.IsSuccess)
						{
							return PackResponseCommand(mcCore, 16, 0, new byte[0]);
						}
						return PackResponseCommand(mcCore, 0, 0, new byte[0]);
					}
				}
				else if (mcCore[0] == 3)
				{
					byte[] value2 = mcCore.RemoveBegin(12);
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode || num == MelsecA1EDataType.D.DataCode || num == MelsecA1EDataType.R.DataCode || num == MelsecA1EDataType.W.DataCode)
					{
						OperateResult operateResult4 = Write(addressFromDataCode, value2);
						if (!operateResult4.IsSuccess)
						{
							return PackResponseCommand(mcCore, 16, 0, new byte[0]);
						}
						return PackResponseCommand(mcCore, 0, 0, new byte[0]);
					}
				}
				return null;
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), ex);
				return null;
			}
		}

		private byte[] PackAsciiResponseCommand(byte[] mcCore, byte[] data)
		{
			byte[] array = new byte[4]
			{
				(byte)(mcCore[0] + 8),
				mcCore[1],
				48,
				48
			};
			if (data == null)
			{
				return array;
			}
			return SoftBasic.SpliceArray<byte>(array, MelsecHelper.TransByteArrayToAsciiByteArray(data));
		}

		private byte[] PackAsciiResponseCommand(byte[] mcCore, bool[] data)
		{
			byte[] array = new byte[4]
			{
				(byte)(mcCore[0] + 8),
				mcCore[1],
				48,
				48
			};
			if (data == null)
			{
				return array;
			}
			if (data.Length % 2 == 1)
			{
				data = SoftBasic.ArrayExpandToLength(data, data.Length + 1);
			}
			return SoftBasic.SpliceArray<byte>(array, data.Select((bool m) => (byte)(m ? 49 : 48)).ToArray());
		}

		/// <inheritdoc />
		protected override byte[] ReadFromMcAsciiCore(byte[] mcCore)
		{
			try
			{
				byte b = Convert.ToByte(Encoding.ASCII.GetString(mcCore, 0, 2), 16);
				int address = Convert.ToInt32(Encoding.ASCII.GetString(mcCore, 12, 8), 16);
				ushort num = Convert.ToUInt16(Encoding.ASCII.GetString(mcCore, 8, 4), 16);
				ushort length = Convert.ToUInt16(Encoding.ASCII.GetString(mcCore, 20, 2), 16);
				string addressFromDataCode = GetAddressFromDataCode(num, address);
				switch (b)
				{
				case 0:
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode)
					{
						return PackAsciiResponseCommand(mcCore, ReadBool(addressFromDataCode, length).Content);
					}
					break;
				case 1:
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode || num == MelsecA1EDataType.D.DataCode || num == MelsecA1EDataType.R.DataCode || num == MelsecA1EDataType.W.DataCode)
					{
						return PackAsciiResponseCommand(mcCore, Read(addressFromDataCode, length).Content);
					}
					break;
				case 2:
				{
					bool[] value2 = (from m in mcCore.SelectMiddle(24, length)
						select m == 49).ToArray();
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode)
					{
						Write(addressFromDataCode, value2);
						return PackAsciiResponseCommand(mcCore, new byte[0]);
					}
					break;
				}
				case 3:
				{
					byte[] value = MelsecHelper.TransAsciiByteArrayToByteArray(mcCore.RemoveBegin(24));
					if (num == MelsecA1EDataType.M.DataCode || num == MelsecA1EDataType.X.DataCode || num == MelsecA1EDataType.Y.DataCode || num == MelsecA1EDataType.S.DataCode || num == MelsecA1EDataType.F.DataCode || num == MelsecA1EDataType.B.DataCode || num == MelsecA1EDataType.D.DataCode || num == MelsecA1EDataType.R.DataCode || num == MelsecA1EDataType.W.DataCode)
					{
						Write(addressFromDataCode, value);
						return PackAsciiResponseCommand(mcCore, new byte[0]);
					}
					break;
				}
				}
				return null;
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), ex);
				return null;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecA1EServer[{base.Port}]";
		}
	}
}

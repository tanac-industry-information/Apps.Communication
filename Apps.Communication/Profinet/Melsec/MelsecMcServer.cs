using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Melsec.Helper;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// <b>[商业授权]</b> 三菱MC协议的虚拟服务器，支持M,X,Y,D,W的数据池读写操作，支持二进制及ASCII格式进行读写操作，需要在实例化的时候指定。<br />
	/// <b>[Authorization]</b> The Mitsubishi MC protocol virtual server supports M, X, Y, D, W data pool read and write operations, 
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
	public class MelsecMcServer : NetworkDataServerBase
	{
		private SoftBuffer xBuffer;

		private SoftBuffer yBuffer;

		private SoftBuffer mBuffer;

		private SoftBuffer dBuffer;

		private SoftBuffer wBuffer;

		private SoftBuffer bBuffer;

		private SoftBuffer rBuffer;

		private SoftBuffer zrBuffer;

		private const int DataPoolLength = 65536;

		private bool isBinary = true;

		/// <summary>
		/// 获取或设置当前的通信格式是否是二进制<br />
		/// Get or set whether the current communication format is binary
		/// </summary>
		public bool IsBinary
		{
			get
			{
				return isBinary;
			}
			set
			{
				isBinary = value;
			}
		}

		/// <summary>
		/// 实例化一个默认参数的mc协议的服务器<br />
		/// Instantiate a mc protocol server with default parameters
		/// </summary>
		/// <param name="isBinary">是否是二进制，默认是二进制，否则是ASCII格式</param>
		public MelsecMcServer(bool isBinary = true)
		{
			xBuffer = new SoftBuffer(65536);
			yBuffer = new SoftBuffer(65536);
			mBuffer = new SoftBuffer(65536);
			dBuffer = new SoftBuffer(131072);
			wBuffer = new SoftBuffer(131072);
			bBuffer = new SoftBuffer(65536);
			rBuffer = new SoftBuffer(131072);
			zrBuffer = new SoftBuffer(131072);
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
			this.isBinary = isBinary;
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode)
			{
				bool[] array = (from m in mBuffer.GetBytes(operateResult.Content.AddressStart, length * 16)
					select m != 0).ToArray();
				return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(array));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode)
			{
				bool[] array2 = (from m in xBuffer.GetBytes(operateResult.Content.AddressStart, length * 16)
					select m != 0).ToArray();
				return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(array2));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode)
			{
				bool[] array3 = (from m in yBuffer.GetBytes(operateResult.Content.AddressStart, length * 16)
					select m != 0).ToArray();
				return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(array3));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.B.DataCode)
			{
				bool[] array4 = (from m in bBuffer.GetBytes(operateResult.Content.AddressStart, length * 16)
					select m != 0).ToArray();
				return OperateResult.CreateSuccessResult(SoftBasic.BoolArrayToByte(array4));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.D.DataCode)
			{
				return OperateResult.CreateSuccessResult(dBuffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.W.DataCode)
			{
				return OperateResult.CreateSuccessResult(wBuffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.R.DataCode)
			{
				return OperateResult.CreateSuccessResult(rBuffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.ZR.DataCode)
			{
				return OperateResult.CreateSuccessResult(zrBuffer.GetBytes(operateResult.Content.AddressStart * 2, length * 2));
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode)
			{
				byte[] data = (from m in SoftBasic.ByteToBoolArray(value)
					select (byte)(m ? 1 : 0)).ToArray();
				mBuffer.SetBytes(data, operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode)
			{
				byte[] data2 = (from m in SoftBasic.ByteToBoolArray(value)
					select (byte)(m ? 1 : 0)).ToArray();
				xBuffer.SetBytes(data2, operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode)
			{
				byte[] data3 = (from m in SoftBasic.ByteToBoolArray(value)
					select (byte)(m ? 1 : 0)).ToArray();
				yBuffer.SetBytes(data3, operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.B.DataCode)
			{
				byte[] data4 = (from m in SoftBasic.ByteToBoolArray(value)
					select (byte)(m ? 1 : 0)).ToArray();
				bBuffer.SetBytes(data4, operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.D.DataCode)
			{
				dBuffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.W.DataCode)
			{
				wBuffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.R.DataCode)
			{
				rBuffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.ZR.DataCode)
			{
				zrBuffer.SetBytes(value, operateResult.Content.AddressStart * 2);
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content.McDataType.DataType == 0)
			{
				return new OperateResult<bool[]>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode)
			{
				return OperateResult.CreateSuccessResult((from m in mBuffer.GetBytes(operateResult.Content.AddressStart, length)
					select m != 0).ToArray());
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode)
			{
				return OperateResult.CreateSuccessResult((from m in xBuffer.GetBytes(operateResult.Content.AddressStart, length)
					select m != 0).ToArray());
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode)
			{
				return OperateResult.CreateSuccessResult((from m in yBuffer.GetBytes(operateResult.Content.AddressStart, length)
					select m != 0).ToArray());
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.B.DataCode)
			{
				return OperateResult.CreateSuccessResult((from m in bBuffer.GetBytes(operateResult.Content.AddressStart, length)
					select m != 0).ToArray());
			}
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<McAddressData> operateResult = McAddressData.ParseMelsecFrom(address, 0);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content.McDataType.DataType == 0)
			{
				return new OperateResult<bool[]>(StringResources.Language.MelsecCurrentTypeNotSupportedWordOperate);
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.M.DataCode)
			{
				mBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.X.DataCode)
			{
				xBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.Y.DataCode)
			{
				yBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			if (operateResult.Content.McDataType.DataCode == MelsecMcDataType.B.DataCode)
			{
				bBuffer.SetBytes(value.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), operateResult.Content.AddressStart);
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedDataType);
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
				OperateResult<byte[]> read1;
				byte[] back;
				if (isBinary)
				{
					read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new MelsecQnA3EBinaryMessage());
					if (read1.IsSuccess)
					{
						back = ReadFromMcCore(read1.Content.RemoveBegin(11));
						goto IL_0292;
					}
					RemoveClient(session);
				}
				else
				{
					read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new MelsecQnA3EAsciiMessage());
					if (read1.IsSuccess)
					{
						back = ReadFromMcAsciiCore(read1.Content.RemoveBegin(22));
						goto IL_0292;
					}
					RemoveClient(session);
				}
				goto end_IL_004f;
				IL_0292:
				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{(isBinary ? read1.Content.ToHexString(' ') : SoftBasic.GetAsciiStringRender(read1.Content))}");
				if (back != null)
				{
					session.WorkSocket.Send(back);
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{(isBinary ? back.ToHexString(' ') : SoftBasic.GetAsciiStringRender(back))}");
					session.UpdateHeartTime();
					RaiseDataReceived(session, read1.Content);
					session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				}
				else
				{
					RemoveClient(session);
				}
				end_IL_004f:;
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				RemoveClient(session, "SocketAsyncCallBack -> " + ex.Message);
			}
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。<br />
		/// The method that should be triggered when a message of the mc protocol is received, 
		/// allowing inheritance to be rewritten to implement custom return or data monitoring.
		/// </summary>
		/// <param name="mcCore">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromMcCore(byte[] mcCore)
		{
			if (mcCore[0] == 1 && mcCore[1] == 4)
			{
				return ReadByCommand(mcCore);
			}
			if (mcCore[0] == 1 && mcCore[1] == 20)
			{
				if (!base.EnableWrite)
				{
					return PackCommand(49250, null);
				}
				return PackCommand(0, WriteByMessage(mcCore));
			}
			return null;
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。<br />
		/// The method that should be triggered when a message of the mc protocol is received, 
		/// allowing inheritance to be rewritten to implement custom return or data monitoring.
		/// </summary>
		/// <param name="mcCore">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromMcAsciiCore(byte[] mcCore)
		{
			if (mcCore[0] == 48 && mcCore[1] == 52 && mcCore[2] == 48 && mcCore[3] == 49)
			{
				return ReadAsciiByCommand(mcCore);
			}
			if (mcCore[0] == 49 && mcCore[1] == 52 && mcCore[2] == 48 && mcCore[3] == 49)
			{
				if (!base.EnableWrite)
				{
					return PackCommand(49250, null);
				}
				return PackCommand(0, WriteAsciiByMessage(mcCore));
			}
			return null;
		}

		/// <summary>
		/// 将状态码，数据打包成一个完成的回复报文信息
		/// </summary>
		/// <param name="status">状态信息</param>
		/// <param name="data">数据</param>
		/// <returns>状态信息</returns>
		protected virtual byte[] PackCommand(ushort status, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			if (isBinary)
			{
				byte[] array = new byte[11 + data.Length];
				SoftBasic.HexStringToBytes("D0 00 00 FF FF 03 00 00 00 00 00").CopyTo(array, 0);
				if (data.Length != 0)
				{
					data.CopyTo(array, 11);
				}
				BitConverter.GetBytes((short)(data.Length + 2)).CopyTo(array, 7);
				BitConverter.GetBytes(status).CopyTo(array, 9);
				return array;
			}
			byte[] array2 = new byte[22 + data.Length];
			Encoding.ASCII.GetBytes("D00000FF03FF0000000000").CopyTo(array2, 0);
			if (data.Length != 0)
			{
				data.CopyTo(array2, 22);
			}
			Encoding.ASCII.GetBytes((data.Length + 4).ToString("X4")).CopyTo(array2, 14);
			Encoding.ASCII.GetBytes(status.ToString("X4")).CopyTo(array2, 18);
			return array2;
		}

		private byte[] ReadByCommand(byte[] command)
		{
			ushort num = base.ByteTransform.TransUInt16(command, 8);
			int num2 = command[6] * 65536 + command[5] * 256 + command[4];
			if (command[2] == 1)
			{
				if (num > 7168)
				{
					return PackCommand(49233, null);
				}
				if (command[7] == MelsecMcDataType.M.DataCode)
				{
					return PackCommand(0, MelsecHelper.TransBoolArrayToByteData(mBuffer.GetBytes(num2, num)));
				}
				if (command[7] == MelsecMcDataType.X.DataCode)
				{
					return PackCommand(0, MelsecHelper.TransBoolArrayToByteData(xBuffer.GetBytes(num2, num)));
				}
				if (command[7] == MelsecMcDataType.Y.DataCode)
				{
					return PackCommand(0, MelsecHelper.TransBoolArrayToByteData(yBuffer.GetBytes(num2, num)));
				}
				if (command[7] == MelsecMcDataType.B.DataCode)
				{
					return PackCommand(0, MelsecHelper.TransBoolArrayToByteData(bBuffer.GetBytes(num2, num)));
				}
				return PackCommand(49242, null);
			}
			if (num > 960)
			{
				return PackCommand(49233, null);
			}
			if (command[7] == MelsecMcDataType.M.DataCode)
			{
				return PackCommand(0, (from m in mBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray().ToByteArray());
			}
			if (command[7] == MelsecMcDataType.X.DataCode)
			{
				return PackCommand(0, (from m in xBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray().ToByteArray());
			}
			if (command[7] == MelsecMcDataType.Y.DataCode)
			{
				return PackCommand(0, (from m in yBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray().ToByteArray());
			}
			if (command[7] == MelsecMcDataType.B.DataCode)
			{
				return PackCommand(0, (from m in bBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray().ToByteArray());
			}
			if (command[7] == MelsecMcDataType.D.DataCode)
			{
				return PackCommand(0, dBuffer.GetBytes(num2 * 2, num * 2));
			}
			if (command[7] == MelsecMcDataType.W.DataCode)
			{
				return PackCommand(0, wBuffer.GetBytes(num2 * 2, num * 2));
			}
			if (command[7] == MelsecMcDataType.R.DataCode)
			{
				return PackCommand(0, rBuffer.GetBytes(num2 * 2, num * 2));
			}
			if (command[7] == MelsecMcDataType.ZR.DataCode)
			{
				return PackCommand(0, zrBuffer.GetBytes(num2 * 2, num * 2));
			}
			return PackCommand(49242, null);
		}

		private byte[] ReadAsciiByCommand(byte[] command)
		{
			ushort num = Convert.ToUInt16(Encoding.ASCII.GetString(command, 16, 4), 16);
			string @string = Encoding.ASCII.GetString(command, 8, 2);
			int num2 = 0;
			num2 = ((!(@string == MelsecMcDataType.X.AsciiCode) && !(@string == MelsecMcDataType.Y.AsciiCode) && !(@string == MelsecMcDataType.W.AsciiCode) && !(@string == MelsecMcDataType.B.AsciiCode)) ? Convert.ToInt32(Encoding.ASCII.GetString(command, 10, 6)) : Convert.ToInt32(Encoding.ASCII.GetString(command, 10, 6), 16));
			if (command[7] == 49)
			{
				if (num > 3584)
				{
					return PackCommand(49233, null);
				}
				if (@string == MelsecMcDataType.M.AsciiCode)
				{
					return PackCommand(0, (from m in mBuffer.GetBytes(num2, num)
						select (byte)((m != 0) ? 49 : 48)).ToArray());
				}
				if (@string == MelsecMcDataType.X.AsciiCode)
				{
					return PackCommand(0, (from m in xBuffer.GetBytes(num2, num)
						select (byte)((m != 0) ? 49 : 48)).ToArray());
				}
				if (@string == MelsecMcDataType.Y.AsciiCode)
				{
					return PackCommand(0, (from m in yBuffer.GetBytes(num2, num)
						select (byte)((m != 0) ? 49 : 48)).ToArray());
				}
				if (@string == MelsecMcDataType.B.AsciiCode)
				{
					return PackCommand(0, (from m in bBuffer.GetBytes(num2, num)
						select (byte)((m != 0) ? 49 : 48)).ToArray());
				}
				return PackCommand(49242, null);
			}
			if (num > 960)
			{
				return PackCommand(49233, null);
			}
			if (@string == MelsecMcDataType.M.AsciiCode)
			{
				bool[] array = (from m in mBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray();
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(SoftBasic.BoolArrayToByte(array)));
			}
			if (@string == MelsecMcDataType.X.AsciiCode)
			{
				bool[] array2 = (from m in xBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray();
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(SoftBasic.BoolArrayToByte(array2)));
			}
			if (@string == MelsecMcDataType.Y.AsciiCode)
			{
				bool[] array3 = (from m in yBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray();
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(SoftBasic.BoolArrayToByte(array3)));
			}
			if (@string == MelsecMcDataType.B.AsciiCode)
			{
				bool[] array4 = (from m in bBuffer.GetBytes(num2, num * 16)
					select m != 0).ToArray();
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(SoftBasic.BoolArrayToByte(array4)));
			}
			if (@string == MelsecMcDataType.D.AsciiCode)
			{
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(dBuffer.GetBytes(num2 * 2, num * 2)));
			}
			if (@string == MelsecMcDataType.W.AsciiCode)
			{
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(wBuffer.GetBytes(num2 * 2, num * 2)));
			}
			if (@string == MelsecMcDataType.R.AsciiCode)
			{
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(rBuffer.GetBytes(num2 * 2, num * 2)));
			}
			if (@string == MelsecMcDataType.ZR.AsciiCode)
			{
				return PackCommand(0, MelsecHelper.TransByteArrayToAsciiByteArray(zrBuffer.GetBytes(num2 * 2, num * 2)));
			}
			return PackCommand(49242, null);
		}

		private byte[] WriteByMessage(byte[] command)
		{
			if (!base.EnableWrite)
			{
				return null;
			}
			ushort count = base.ByteTransform.TransUInt16(command, 8);
			int num = command[6] * 65536 + command[5] * 256 + command[4];
			if (command[2] == 1)
			{
				byte[] source = McBinaryHelper.ExtractActualDataHelper(command.RemoveBegin(10), isBit: true);
				if (command[7] == MelsecMcDataType.M.DataCode)
				{
					mBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else if (command[7] == MelsecMcDataType.X.DataCode)
				{
					xBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else if (command[7] == MelsecMcDataType.Y.DataCode)
				{
					yBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else
				{
					if (command[7] != MelsecMcDataType.B.DataCode)
					{
						throw new Exception(StringResources.Language.NotSupportedDataType);
					}
					bBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.M.DataCode)
			{
				byte[] data = (from m in SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(command, 10))
					select (byte)(m ? 1 : 0)).ToArray();
				mBuffer.SetBytes(data, num);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.X.DataCode)
			{
				byte[] data2 = (from m in SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(command, 10))
					select (byte)(m ? 1 : 0)).ToArray();
				xBuffer.SetBytes(data2, num);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.Y.DataCode)
			{
				byte[] data3 = (from m in SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(command, 10))
					select (byte)(m ? 1 : 0)).ToArray();
				yBuffer.SetBytes(data3, num);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.B.DataCode)
			{
				byte[] data4 = (from m in SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(command, 10))
					select (byte)(m ? 1 : 0)).ToArray();
				bBuffer.SetBytes(data4, num);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.D.DataCode)
			{
				dBuffer.SetBytes(SoftBasic.ArrayRemoveBegin(command, 10), num * 2);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.W.DataCode)
			{
				wBuffer.SetBytes(SoftBasic.ArrayRemoveBegin(command, 10), num * 2);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.R.DataCode)
			{
				rBuffer.SetBytes(SoftBasic.ArrayRemoveBegin(command, 10), num * 2);
				return new byte[0];
			}
			if (command[7] == MelsecMcDataType.ZR.DataCode)
			{
				zrBuffer.SetBytes(SoftBasic.ArrayRemoveBegin(command, 10), num * 2);
				return new byte[0];
			}
			throw new Exception(StringResources.Language.NotSupportedDataType);
		}

		private byte[] WriteAsciiByMessage(byte[] command)
		{
			ushort count = Convert.ToUInt16(Encoding.ASCII.GetString(command, 16, 4), 16);
			string @string = Encoding.ASCII.GetString(command, 8, 2);
			int num = 0;
			num = ((!(@string == MelsecMcDataType.X.AsciiCode) && !(@string == MelsecMcDataType.Y.AsciiCode) && !(@string == MelsecMcDataType.W.AsciiCode) && !(@string == MelsecMcDataType.B.AsciiCode)) ? Convert.ToInt32(Encoding.ASCII.GetString(command, 10, 6)) : Convert.ToInt32(Encoding.ASCII.GetString(command, 10, 6), 16));
			if (command[7] == 49)
			{
				byte[] source = (from m in command.RemoveBegin(20)
					select (byte)((m == 49) ? 1 : 0)).ToArray();
				if (@string == MelsecMcDataType.M.AsciiCode)
				{
					mBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else if (@string == MelsecMcDataType.X.AsciiCode)
				{
					xBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else if (@string == MelsecMcDataType.Y.AsciiCode)
				{
					yBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				else
				{
					if (!(@string == MelsecMcDataType.B.AsciiCode))
					{
						throw new Exception(StringResources.Language.NotSupportedDataType);
					}
					bBuffer.SetBytes(source.Take(count).ToArray(), num);
				}
				return new byte[0];
			}
			if (@string == MelsecMcDataType.M.AsciiCode)
			{
				byte[] data = (from m in SoftBasic.ByteToBoolArray(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)))
					select (byte)(m ? 1 : 0)).ToArray();
				mBuffer.SetBytes(data, num);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.X.AsciiCode)
			{
				byte[] data2 = (from m in SoftBasic.ByteToBoolArray(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)))
					select (byte)(m ? 1 : 0)).ToArray();
				xBuffer.SetBytes(data2, num);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.Y.AsciiCode)
			{
				byte[] data3 = (from m in SoftBasic.ByteToBoolArray(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)))
					select (byte)(m ? 1 : 0)).ToArray();
				yBuffer.SetBytes(data3, num);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.B.AsciiCode)
			{
				byte[] data4 = (from m in SoftBasic.ByteToBoolArray(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)))
					select (byte)(m ? 1 : 0)).ToArray();
				bBuffer.SetBytes(data4, num);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.D.AsciiCode)
			{
				dBuffer.SetBytes(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)), num * 2);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.W.AsciiCode)
			{
				wBuffer.SetBytes(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)), num * 2);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.R.AsciiCode)
			{
				rBuffer.SetBytes(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)), num * 2);
				return new byte[0];
			}
			if (@string == MelsecMcDataType.ZR.AsciiCode)
			{
				zrBuffer.SetBytes(MelsecHelper.TransAsciiByteArrayToByteArray(command.RemoveBegin(20)), num * 2);
				return new byte[0];
			}
			throw new Exception(StringResources.Language.NotSupportedDataType);
		}

		/// <inheritdoc />
		protected override void LoadFromBytes(byte[] content)
		{
			if (content.Length < 458752)
			{
				throw new Exception("File is not correct");
			}
			mBuffer.SetBytes(content, 0, 0, 65536);
			xBuffer.SetBytes(content, 65536, 0, 65536);
			yBuffer.SetBytes(content, 131072, 0, 65536);
			dBuffer.SetBytes(content, 196608, 0, 131072);
			wBuffer.SetBytes(content, 327680, 0, 131072);
			if (content.Length >= 12)
			{
				bBuffer.SetBytes(content, 458752, 0, 65536);
				rBuffer.SetBytes(content, 524288, 0, 131072);
				zrBuffer.SetBytes(content, 655360, 0, 131072);
			}
		}

		/// <inheritdoc />
		[HslMqttApi]
		protected override byte[] SaveToBytes()
		{
			byte[] array = new byte[786432];
			Array.Copy(mBuffer.GetBytes(), 0, array, 0, 65536);
			Array.Copy(xBuffer.GetBytes(), 0, array, 65536, 65536);
			Array.Copy(yBuffer.GetBytes(), 0, array, 131072, 65536);
			Array.Copy(dBuffer.GetBytes(), 0, array, 196608, 131072);
			Array.Copy(wBuffer.GetBytes(), 0, array, 327680, 131072);
			Array.Copy(bBuffer.GetBytes(), 0, array, 458752, 65536);
			Array.Copy(rBuffer.GetBytes(), 0, array, 524288, 131072);
			Array.Copy(zrBuffer.GetBytes(), 0, array, 655360, 131072);
			return array;
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing">是否托管对象</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				xBuffer?.Dispose();
				yBuffer?.Dispose();
				mBuffer?.Dispose();
				dBuffer?.Dispose();
				wBuffer?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecMcServer[{base.Port}]";
		}
	}
}

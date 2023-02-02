using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Omron.Helper;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLink协议的实现，基于Tcp实现，地址支持示例 DM区:D100; CIO区:C100; Work区:W100; Holding区:H100; Auxiliary区: A100<br />
	/// Implementation of Omron's HostLink protocol, based on tcp protocol, address support example DM area: D100; CIO area: C100; Work area: W100; Holding area: H100; Auxiliary area: A100
	/// </summary>
	/// <remarks>
	/// 感谢 深圳～拾忆 的测试，地址可以携带站号信息，例如 s=2;D100 
	/// <br />
	/// <note type="important">
	/// 如果发现串口线和usb同时打开才能通信的情况，需要按照如下的操作：<br />
	/// 串口线不是标准的串口线，电脑的串口线的235引脚分别接PLC的329引脚，45线短接，就可以通讯，感谢 深圳-小君(QQ932507362)提供的解决方案。
	/// </note>
	/// </remarks>
	/// <example>
	/// 欧姆龙的地址参考如下：
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
	///     <term>DM Area</term>
	///     <term>D</term>
	///     <term>D100,D200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>CIO Area</term>
	///     <term>C</term>
	///     <term>C100,C200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Work Area</term>
	///     <term>W</term>
	///     <term>W100,W200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Holding Bit Area</term>
	///     <term>H</term>
	///     <term>H100,H200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>Auxiliary Bit Area</term>
	///     <term>A</term>
	///     <term>A100,A200</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </example>
	public class OmronHostLinkOverTcp : NetworkDeviceBase
	{
		/// <summary>
		/// Specifies whether or not there are network relays. Set “80” (ASCII: 38,30) 
		/// when sending an FINS command to a CPU Unit on a network.Set “00” (ASCII: 30,30) 
		/// when sending to a CPU Unit connected directly to the host computer.
		/// </summary>
		public byte ICF { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.DA2" />
		public byte DA2 { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SA2" />
		public byte SA2 { get; set; }

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SID" />
		public byte SID { get; set; } = 0;


		/// <summary>
		/// The response wait time sets the time from when the CPU Unit receives a command block until it starts 
		/// to return a response.It can be set from 0 to F in hexadecimal, in units of 10 ms.
		/// If F(15) is set, the response will begin to be returned 150 ms (15 × 10 ms) after the command block was received.
		/// </summary>
		public byte ResponseWaitTime { get; set; } = 48;


		/// <summary>
		/// PLC设备的站号信息<br />
		/// PLC device station number information
		/// </summary>
		public byte UnitNumber { get; set; }

		/// <summary>
		/// 进行字读取的时候对于超长的情况按照本属性进行切割，默认260。<br />
		/// When reading words, it is cut according to this attribute for the case of overlength. The default is 260.
		/// </summary>
		public int ReadSplits { get; set; } = 260;


		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.#ctor" />
		public OmronHostLinkOverTcp()
		{
			base.ByteTransform = new ReverseWordTransform();
			base.WordLength = 1;
			base.ByteTransform.DataFormat = DataFormat.CDAB;
			LogMsgFormatBinary = false;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.#ctor(System.String,System.Int32)" />
		public OmronHostLinkOverTcp(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return OmronHostLinkHelper.ResponseValidAnalysis(send, response);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> ReceiveByMessage(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			return ReceiveCommandLineFromSocket(socket, 13, timeOut);
		}

		/// <inheritdoc />
		protected override Task<OperateResult<byte[]>> ReceiveByMessageAsync(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			return ReceiveCommandLineFromSocketAsync(socket, 13, timeOut);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<List<byte[]>> operateResult = OmronFinsNetHelper.BuildReadCommand(address, length, isBit: false, ReadSplits);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			List<byte> list = new List<byte>();
			for (int i = 0; i < operateResult.Content.Count; i++)
			{
				OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(station, operateResult.Content[i]));
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult2);
				}
				list.AddRange(operateResult2.Content);
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<byte[]> operateResult = OmronFinsNetHelper.BuildWriteWordCommand(address, value, isBit: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(station, operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<List<byte[]>> command = OmronFinsNetHelper.BuildReadCommand(address, length, isBit: false, ReadSplits);
			if (!command.IsSuccess)
			{
				return command.ConvertFailed<byte[]>();
			}
			List<byte> contentArray = new List<byte>();
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(station, command.Content[i]));
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(read);
				}
				contentArray.AddRange(read.Content);
			}
			return OperateResult.CreateSuccessResult(contentArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<byte[]> command = OmronFinsNetHelper.BuildWriteWordCommand(address, value, isBit: false);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(station, command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<List<byte[]>> operateResult = OmronFinsNetHelper.BuildReadCommand(address, length, isBit: true);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			List<bool> list = new List<bool>();
			for (int i = 0; i < operateResult.Content.Count; i++)
			{
				OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(station, operateResult.Content[i]));
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult2);
				}
				if (operateResult2.Content.Length == 0)
				{
					return new OperateResult<bool[]>("Data is empty.");
				}
				list.AddRange(operateResult2.Content.Select((byte m) => m != 0));
			}
			return OperateResult.CreateSuccessResult(list.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<byte[]> operateResult = OmronFinsNetHelper.BuildWriteWordCommand(address, values.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), isBit: true);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(station, operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<List<byte[]>> command = OmronFinsNetHelper.BuildReadCommand(address, length, isBit: true);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(command);
			}
			List<bool> contentArray = new List<bool>();
			for (int i = 0; i < command.Content.Count; i++)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(station, command.Content[i]));
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(read);
				}
				contentArray.AddRange(read.Content.Select((byte m) => m != 0));
			}
			return OperateResult.CreateSuccessResult(contentArray.ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", UnitNumber);
			OperateResult<byte[]> command = OmronFinsNetHelper.BuildWriteWordCommand(address, values.Select((bool m) => (byte)(m ? 1 : 0)).ToArray(), isBit: true);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(station, command.Content));
			if (!read.IsSuccess)
			{
				return read;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronHostLinkOverTcp[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 将普通的指令打包成完整的指令
		/// </summary>
		/// <param name="station">PLC的站号信息</param>
		/// <param name="cmd">fins指令</param>
		/// <returns>完整的质量</returns>
		private byte[] PackCommand(byte station, byte[] cmd)
		{
			cmd = SoftBasic.BytesToAsciiBytes(cmd);
			byte[] array = new byte[18 + cmd.Length];
			array[0] = 64;
			array[1] = SoftBasic.BuildAsciiBytesFrom(station)[0];
			array[2] = SoftBasic.BuildAsciiBytesFrom(station)[1];
			array[3] = 70;
			array[4] = 65;
			array[5] = ResponseWaitTime;
			array[6] = SoftBasic.BuildAsciiBytesFrom(ICF)[0];
			array[7] = SoftBasic.BuildAsciiBytesFrom(ICF)[1];
			array[8] = SoftBasic.BuildAsciiBytesFrom(DA2)[0];
			array[9] = SoftBasic.BuildAsciiBytesFrom(DA2)[1];
			array[10] = SoftBasic.BuildAsciiBytesFrom(SA2)[0];
			array[11] = SoftBasic.BuildAsciiBytesFrom(SA2)[1];
			array[12] = SoftBasic.BuildAsciiBytesFrom(SID)[0];
			array[13] = SoftBasic.BuildAsciiBytesFrom(SID)[1];
			array[array.Length - 2] = 42;
			array[array.Length - 1] = 13;
			cmd.CopyTo(array, 14);
			int num = array[0];
			for (int i = 1; i < array.Length - 4; i++)
			{
				num ^= array[i];
			}
			array[array.Length - 4] = SoftBasic.BuildAsciiBytesFrom((byte)num)[0];
			array[array.Length - 3] = SoftBasic.BuildAsciiBytesFrom((byte)num)[1];
			return array;
		}
	}
}

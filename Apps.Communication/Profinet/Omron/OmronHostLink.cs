using System.Collections.Generic;
using System.IO;
using System.Linq;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Profinet.Omron.Helper;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;
using RJCP.IO.Ports;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的HostLink协议的实现，地址支持示例 DM区:D100; CIO区:C100; Work区:W100; Holding区:H100; Auxiliary区: A100<br />
	/// Implementation of Omron's HostLink protocol, address support example DM area: D100; CIO area: C100; Work area: W100; Holding area: H100; Auxiliary area: A100
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
	/// <inheritdoc cref="T:Communication.Profinet.Omron.OmronHostLinkOverTcp" path="example" />
	/// </example>
	public class OmronHostLink : SerialDeviceBase
	{
		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.ICF" />
		public byte ICF { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.DA2" />
		public byte DA2 { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.SA2" />
		public byte SA2 { get; set; }

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.SID" />
		public byte SID { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.ResponseWaitTime" />
		public byte ResponseWaitTime { get; set; } = 48;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.UnitNumber" />
		public byte UnitNumber { get; set; }

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronHostLinkOverTcp.ReadSplits" />
		public int ReadSplits { get; set; } = 260;


		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.#ctor" />
		public OmronHostLink()
		{
			base.ByteTransform = new ReverseWordTransform();
			base.WordLength = 1;
			base.ByteTransform.DataFormat = DataFormat.CDAB;
			base.ByteTransform.IsStringReverseByteWord = true;
			LogMsgFormatBinary = false;
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return OmronHostLinkHelper.ResponseValidAnalysis(send, response);
		}

		/// <inheritdoc />
		protected override bool CheckReceiveDataComplete(MemoryStream ms)
		{
			byte[] array = ms.ToArray();
			if (array.Length > 1)
			{
				return array[array.Length - 1] == 13;
			}
			return false;
		}

		/// <summary>
		/// 初始化串口信息，9600波特率，7位数据位，1位停止位，偶校验<br />
		/// Initial serial port information, 9600 baud rate, 7 data bits, 1 stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		public override void SerialPortInni(string portName)
		{
			base.SerialPortInni(portName);
		}

		/// <summary>
		/// 初始化串口信息，波特率，7位数据位，1位停止位，偶校验<br />
		/// Initializes serial port information, baud rate, 7-bit data bit, 1-bit stop bit, even parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		public override void SerialPortInni(string portName, int baudRate)
		{
			base.SerialPortInni(portName, baudRate, 7, StopBits.One, Parity.Even);
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
				list.AddRange(operateResult2.Content.Select((byte m) => (m != 0) ? true : false));
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

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronHostLink[{base.PortName}:{base.BaudRate}]";
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

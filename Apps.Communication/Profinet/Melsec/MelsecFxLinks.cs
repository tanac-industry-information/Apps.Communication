using System;
using System.Linq;
using System.Text;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 三菱计算机链接协议，适用FX3U系列，FX3G，FX3S等等系列，通常在PLC侧连接的是485的接线口<br />
	/// Mitsubishi Computer Link Protocol, suitable for FX3U series, FX3G, FX3S, etc., usually the 485 connection port is connected on the PLC side
	/// </summary>
	/// <remarks>
	/// 关于在PLC侧的配置信息，协议：专用协议  传送控制步骤：格式一  站号设置：0
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.Melsec.MelsecFxLinksOverTcp" path="example" />
	/// </example>
	public class MelsecFxLinks : SerialDeviceBase
	{
		private byte station = 0;

		private byte watiingTime = 0;

		private bool sumCheck = true;

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.Station" />
		public byte Station
		{
			get
			{
				return station;
			}
			set
			{
				station = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.WaittingTime" />
		public byte WaittingTime
		{
			get
			{
				return watiingTime;
			}
			set
			{
				if (watiingTime > 15)
				{
					watiingTime = 15;
				}
				else
				{
					watiingTime = value;
				}
			}
		}

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.SumCheck" />
		public bool SumCheck
		{
			get
			{
				return sumCheck;
			}
			set
			{
				sumCheck = value;
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.#ctor" />
		public MelsecFxLinks()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 1;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildReadCommand(b, address, length, isBool: false, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			if (operateResult2.Content[0] != 2)
			{
				return new OperateResult<byte[]>(operateResult2.Content[0], "Read Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			byte[] array = new byte[length * 2];
			for (int i = 0; i < array.Length / 2; i++)
			{
				ushort value = Convert.ToUInt16(Encoding.ASCII.GetString(operateResult2.Content, i * 4 + 5, 4), 16);
				BitConverter.GetBytes(value).CopyTo(array, i * 2);
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildWriteByteCommand(b, address, value, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content[0] != 6)
			{
				return new OperateResult(operateResult2.Content[0], "Write Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildReadCommand(b, address, length, isBool: true, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			if (operateResult2.Content[0] != 2)
			{
				return new OperateResult<bool[]>(operateResult2.Content[0], "Read Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			byte[] array = new byte[length];
			Array.Copy(operateResult2.Content, 5, array, 0, length);
			return OperateResult.CreateSuccessResult(array.Select((byte m) => m == 49).ToArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			byte b = (byte)HslHelper.ExtractParameter(ref address, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildWriteBoolCommand(b, address, value, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content[0] != 6)
			{
				return new OperateResult(operateResult2.Content[0], "Write Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.StartPLC(System.String)" />
		[HslMqttApi(Description = "Start the PLC operation, you can carry additional parameter information and specify the station number. Example: s=2; Note: The semicolon is required.")]
		public OperateResult StartPLC(string parameter = "")
		{
			byte b = (byte)HslHelper.ExtractParameter(ref parameter, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildStart(b, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content[0] != 6)
			{
				return new OperateResult(operateResult2.Content[0], "Start Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.StopPLC(System.String)" />
		[HslMqttApi(Description = "Stop PLC operation, you can carry additional parameter information and specify the station number. Example: s=2; Note: The semicolon is required.")]
		public OperateResult StopPLC(string parameter = "")
		{
			byte b = (byte)HslHelper.ExtractParameter(ref parameter, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildStop(b, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			if (operateResult2.Content[0] != 6)
			{
				return new OperateResult(operateResult2.Content[0], "Stop Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecFxLinksOverTcp.ReadPlcType(System.String)" />
		[HslMqttApi(Description = "Read the PLC model information, you can carry additional parameter information, and specify the station number. Example: s=2; Note: The semicolon is required.")]
		public OperateResult<string> ReadPlcType(string parameter = "")
		{
			byte b = (byte)HslHelper.ExtractParameter(ref parameter, "s", station);
			OperateResult<byte[]> operateResult = MelsecFxLinksOverTcp.BuildReadPlcType(b, sumCheck, watiingTime);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult2);
			}
			if (operateResult2.Content[0] != 6)
			{
				return new OperateResult<string>(operateResult2.Content[0], "ReadPlcType Faild:" + SoftBasic.ByteToHexString(operateResult2.Content, ' '));
			}
			return MelsecFxLinksOverTcp.GetPlcTypeFromCode(Encoding.ASCII.GetString(operateResult2.Content, 5, 2));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecFxLinks[{base.PortName}:{base.BaudRate}]";
		}
	}
}

using System.IO;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Panasonic
{
	/// <summary>
	/// 松下PLC的数据交互协议，采用Mewtocol协议通讯，支持的地址列表参考api文档<br />
	/// The data exchange protocol of Panasonic PLC adopts Mewtocol protocol for communication. For the list of supported addresses, refer to the api document.
	/// </summary>
	/// <remarks>
	/// 地址支持携带站号的访问方式，例如：s=2;D100
	/// </remarks>
	/// <example>
	/// <inheritdoc cref="T:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp" path="example" />
	/// </example>
	public class PanasonicMewtocol : SerialDeviceBase
	{
		/// <inheritdoc cref="P:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Station" />
		public byte Station { get; set; }

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.#ctor(System.Byte)" />
		public PanasonicMewtocol(byte station = 238)
		{
			base.ByteTransform = new RegularByteTransform();
			Station = station;
			base.ByteTransform.DataFormat = DataFormat.DCBA;
		}

		/// <inheritdoc />
		protected override bool CheckReceiveDataComplete(MemoryStream ms)
		{
			byte[] array = ms.ToArray();
			if (array.Length > 5)
			{
				return array[array.Length - 1] == 13;
			}
			return false;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildWriteCommand(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> operateResult = PanasonicHelper.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = PanasonicHelper.BuildReadCommand(station, address, length);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			OperateResult<byte[]> operateResult4 = PanasonicHelper.ExtraActualData(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult4.Content).SelectMiddle(operateResult.Content2 % 16, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.ReadBool(System.String)" />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildReadOneCoil(station, address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult2);
			}
			return PanasonicHelper.ExtraActualBool(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<string, int> operateResult = PanasonicHelper.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			if (operateResult.Content2 % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicAddressBitStartMulti16);
			}
			if (values.Length % 16 != 0)
			{
				return new OperateResult(StringResources.Language.PanasonicBoolLengthMulti16);
			}
			byte[] values2 = SoftBasic.BoolArrayToByte(values);
			OperateResult<byte[]> operateResult2 = PanasonicHelper.BuildWriteCommand(station, address, values2);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return PanasonicHelper.ExtraActualData(operateResult3.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocolOverTcp.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = PanasonicHelper.BuildWriteOneCoil(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return PanasonicHelper.ExtraActualData(operateResult2.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.ReadBool(System.String)" />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			return await Task.Run(() => ReadBool(address));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Panasonic.PanasonicMewtocol.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await Task.Run(() => Write(address, value));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Panasonic Mewtocol[{base.PortName}:{base.BaudRate}]";
		}
	}
}

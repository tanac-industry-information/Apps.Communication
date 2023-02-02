using System;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.Address;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Melsec.Helper;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Melsec
{
	/// <summary>
	/// 三菱的R系列的MC协议，支持的地址类型和 <see cref="T:Communication.Profinet.Melsec.MelsecMcNet" /> 有区别，详细请查看对应的API文档说明
	/// </summary>
	public class MelsecMcRNet : NetworkDeviceBase, IReadWriteMc, IReadWriteDevice, IReadWriteNet
	{
		/// <inheritdoc cref="P:Communication.Profinet.Melsec.Helper.IReadWriteMc.McType" />
		public McType McType => McType.McBinary;

		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecMcNet.NetworkNumber" />
		public byte NetworkNumber { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Melsec.MelsecMcNet.NetworkStationNumber" />
		public byte NetworkStationNumber { get; set; } = 0;


		/// <summary>
		/// 实例化三菱R系列的Qna兼容3E帧协议的通讯对象<br />
		/// Instantiate the communication object of Mitsubishi's Qna compatible 3E frame protocol
		/// </summary>
		public MelsecMcRNet()
		{
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 指定ip地址和端口号来实例化一个默认的对象<br />
		/// Specify the IP address and port number to instantiate a default object
		/// </summary>
		/// <param name="ipAddress">PLC的Ip地址</param>
		/// <param name="port">PLC的端口</param>
		public MelsecMcRNet(string ipAddress, int port)
		{
			base.WordLength = 1;
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new MelsecQnA3EBinaryMessage();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.IReadWriteMc.McAnalysisAddress(System.String,System.UInt16)" />
		public virtual OperateResult<McAddressData> McAnalysisAddress(string address, ushort length)
		{
			return McAddressData.ParseMelsecRFrom(address, length);
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return McBinaryHelper.PackMcCommand(command, NetworkNumber, NetworkStationNumber);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			OperateResult operateResult = McBinaryHelper.CheckResponseContentHelper(response);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(response.RemoveBegin(11));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.IReadWriteMc.ExtractActualData(System.Byte[],System.Boolean)" />
		public byte[] ExtractActualData(byte[] response, bool isBit)
		{
			return McBinaryHelper.ExtractActualDataHelper(response, isBit);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return McHelper.Read(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return McHelper.Write(this, address, value);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await McHelper.ReadAsync(this, address, length);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await McHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandom(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[])" />
		[HslMqttApi("随机读取PLC的数据信息，可以跨地址，跨类型组合，但是每个地址只能读取一个word，也就是2个字节的内容。收到结果后，需要自行解析数据")]
		public OperateResult<byte[]> ReadRandom(string[] address)
		{
			return McHelper.ReadRandom(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandom(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[],System.UInt16[])" />
		[HslMqttApi(ApiTopic = "ReadRandoms", Description = "随机读取PLC的数据信息，可以跨地址，跨类型组合，每个地址是任意的长度。收到结果后，需要自行解析数据，目前只支持字地址，比如D区，W区，R区，不支持X，Y，M，B，L等等")]
		public OperateResult<byte[]> ReadRandom(string[] address, ushort[] length)
		{
			return McHelper.ReadRandom(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandomInt16(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[])" />
		public OperateResult<short[]> ReadRandomInt16(string[] address)
		{
			return McHelper.ReadRandomInt16(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.Helper.McHelper.ReadRandomUInt16(Communication.Profinet.Melsec.Helper.IReadWriteMc,System.String[])" />
		public OperateResult<ushort[]> ReadRandomUInt16(string[] address)
		{
			return McHelper.ReadRandomUInt16(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.ReadRandom(System.String[])" />
		public async Task<OperateResult<byte[]>> ReadRandomAsync(string[] address)
		{
			return await McHelper.ReadRandomAsync(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.ReadRandom(System.String[],System.UInt16[])" />
		public async Task<OperateResult<byte[]>> ReadRandomAsync(string[] address, ushort[] length)
		{
			return await McHelper.ReadRandomAsync(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.ReadRandomInt16(System.String[])" />
		public async Task<OperateResult<short[]>> ReadRandomInt16Async(string[] address)
		{
			return await McHelper.ReadRandomInt16Async(this, address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.ReadRandomUInt16(System.String[])" />
		public async Task<OperateResult<ushort[]>> ReadRandomUInt16Async(string[] address)
		{
			return await McHelper.ReadRandomUInt16Async(this, address);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return McHelper.ReadBool(this, address, length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			return McHelper.Write(this, address, values);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await McHelper.ReadBoolAsync(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Melsec.MelsecMcRNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			return await McHelper.WriteAsync(this, address, values);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"MelsecMcRNet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 分析三菱R系列的地址，并返回解析后的数据对象
		/// </summary>
		/// <param name="address">字符串地址</param>
		/// <returns>是否解析成功</returns>
		public static OperateResult<MelsecMcDataType, int> AnalysisAddress(string address)
		{
			try
			{
				if (address.StartsWith("LSTS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LSTS, Convert.ToInt32(address.Substring(4), MelsecMcDataType.R_LSTS.FromBase));
				}
				if (address.StartsWith("LSTC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LSTC, Convert.ToInt32(address.Substring(4), MelsecMcDataType.R_LSTC.FromBase));
				}
				if (address.StartsWith("LSTN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LSTN, Convert.ToInt32(address.Substring(4), MelsecMcDataType.R_LSTN.FromBase));
				}
				if (address.StartsWith("STS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_STS, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_STS.FromBase));
				}
				if (address.StartsWith("STC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_STC, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_STC.FromBase));
				}
				if (address.StartsWith("STN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_STN, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_STN.FromBase));
				}
				if (address.StartsWith("LTS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LTS, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LTS.FromBase));
				}
				if (address.StartsWith("LTC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LTC, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LTC.FromBase));
				}
				if (address.StartsWith("LTN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LTN, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LTN.FromBase));
				}
				if (address.StartsWith("LCS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LCS, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LCS.FromBase));
				}
				if (address.StartsWith("LCC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LCC, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LCC.FromBase));
				}
				if (address.StartsWith("LCN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_LCN, Convert.ToInt32(address.Substring(3), MelsecMcDataType.R_LCN.FromBase));
				}
				if (address.StartsWith("TS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_TS, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_TS.FromBase));
				}
				if (address.StartsWith("TC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_TC, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_TC.FromBase));
				}
				if (address.StartsWith("TN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_TN, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_TN.FromBase));
				}
				if (address.StartsWith("CS"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_CS, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_CS.FromBase));
				}
				if (address.StartsWith("CC"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_CC, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_CC.FromBase));
				}
				if (address.StartsWith("CN"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_CN, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_CN.FromBase));
				}
				if (address.StartsWith("SM"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_SM, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_SM.FromBase));
				}
				if (address.StartsWith("SB"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_SB, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_SB.FromBase));
				}
				if (address.StartsWith("DX"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_DX, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_DX.FromBase));
				}
				if (address.StartsWith("DY"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_DY, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_DY.FromBase));
				}
				if (address.StartsWith("SD"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_SD, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_SD.FromBase));
				}
				if (address.StartsWith("SW"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_SW, Convert.ToInt32(address.Substring(2), MelsecMcDataType.R_SW.FromBase));
				}
				if (address.StartsWith("X"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_X, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_X.FromBase));
				}
				if (address.StartsWith("Y"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_Y, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_Y.FromBase));
				}
				if (address.StartsWith("M"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_M, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_M.FromBase));
				}
				if (address.StartsWith("L"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_L, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_L.FromBase));
				}
				if (address.StartsWith("F"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_F, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_F.FromBase));
				}
				if (address.StartsWith("V"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_V, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_V.FromBase));
				}
				if (address.StartsWith("S"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_S, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_S.FromBase));
				}
				if (address.StartsWith("B"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_B, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_B.FromBase));
				}
				if (address.StartsWith("D"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_D, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_D.FromBase));
				}
				if (address.StartsWith("W"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_W, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_W.FromBase));
				}
				if (address.StartsWith("R"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_R, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_R.FromBase));
				}
				if (address.StartsWith("Z"))
				{
					return OperateResult.CreateSuccessResult(MelsecMcDataType.R_Z, Convert.ToInt32(address.Substring(1), MelsecMcDataType.R_Z.FromBase));
				}
				return new OperateResult<MelsecMcDataType, int>(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<MelsecMcDataType, int>(ex.Message);
			}
		}

		/// <summary>
		/// 从三菱地址，是否位读取进行创建读取的MC的核心报文
		/// </summary>
		/// <param name="address">地址数据</param>
		/// <param name="isBit">是否进行了位读取操作</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildReadMcCoreCommand(McAddressData address, bool isBit)
		{
			return new byte[12]
			{
				1,
				4,
				(byte)(isBit ? 1 : 0),
				0,
				BitConverter.GetBytes(address.AddressStart)[0],
				BitConverter.GetBytes(address.AddressStart)[1],
				BitConverter.GetBytes(address.AddressStart)[2],
				BitConverter.GetBytes(address.AddressStart)[3],
				BitConverter.GetBytes(address.McDataType.DataCode)[0],
				BitConverter.GetBytes(address.McDataType.DataCode)[1],
				(byte)((int)address.Length % 256),
				(byte)((int)address.Length / 256)
			};
		}

		/// <summary>
		/// 以字为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="address">三菱的数据地址</param>
		/// <param name="value">实际的原始数据信息</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteWordCoreCommand(McAddressData address, byte[] value)
		{
			if (value == null)
			{
				value = new byte[0];
			}
			byte[] array = new byte[12 + value.Length];
			array[0] = 1;
			array[1] = 20;
			array[2] = 0;
			array[3] = 0;
			array[4] = BitConverter.GetBytes(address.AddressStart)[0];
			array[5] = BitConverter.GetBytes(address.AddressStart)[1];
			array[6] = BitConverter.GetBytes(address.AddressStart)[2];
			array[7] = BitConverter.GetBytes(address.AddressStart)[3];
			array[8] = BitConverter.GetBytes(address.McDataType.DataCode)[0];
			array[9] = BitConverter.GetBytes(address.McDataType.DataCode)[1];
			array[10] = (byte)(value.Length / 2 % 256);
			array[11] = (byte)(value.Length / 2 / 256);
			value.CopyTo(array, 12);
			return array;
		}

		/// <summary>
		/// 以位为单位，创建数据写入的核心报文
		/// </summary>
		/// <param name="address">三菱的地址信息</param>
		/// <param name="value">原始的bool数组数据</param>
		/// <returns>带有成功标识的报文对象</returns>
		public static byte[] BuildWriteBitCoreCommand(McAddressData address, bool[] value)
		{
			if (value == null)
			{
				value = new bool[0];
			}
			byte[] array = MelsecHelper.TransBoolArrayToByteData(value);
			byte[] array2 = new byte[12 + array.Length];
			array2[0] = 1;
			array2[1] = 20;
			array2[2] = 1;
			array2[3] = 0;
			array2[4] = BitConverter.GetBytes(address.AddressStart)[0];
			array2[5] = BitConverter.GetBytes(address.AddressStart)[1];
			array2[6] = BitConverter.GetBytes(address.AddressStart)[2];
			array2[7] = BitConverter.GetBytes(address.AddressStart)[3];
			array2[8] = BitConverter.GetBytes(address.McDataType.DataCode)[0];
			array2[9] = BitConverter.GetBytes(address.McDataType.DataCode)[1];
			array2[10] = (byte)(value.Length % 256);
			array2[11] = (byte)(value.Length / 256);
			array.CopyTo(array2, 12);
			return array2;
		}
	}
}

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.LSIS
{
	/// <summary>
	/// XGB Fast Enet I/F module supports open Ethernet. It provides network configuration that is to connect LSIS and other company PLC, PC on network
	/// </summary>
	/// <remarks>
	/// Address example likes the follow
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
	///     <term>*</term>
	///     <term>P</term>
	///     <term>PX100,PB100,PW100,PD100,PL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>M</term>
	///     <term>MX100,MB100,MW100,MD100,ML100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>L</term>
	///     <term>LX100,LB100,LW100,LD100,LL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>K</term>
	///     <term>KX100,KB100,KW100,KD100,KL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term>*</term>
	///     <term>F</term>
	///     <term>FX100,FB100,FW100,FD100,FL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>T</term>
	///     <term>TX100,TB100,TW100,TD100,TL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>C</term>
	///     <term>CX100,CB100,CW100,CD100,CL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>D</term>
	///     <term>DX100,DB100,DW100,DD100,DL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>S</term>
	///     <term>SX100,SB100,SW100,SD100,SL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Q</term>
	///     <term>QX100,QB100,QW100,QD100,QL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>I</term>
	///     <term>IX100,IB100,IW100,ID100,IL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>N</term>
	///     <term>NX100,NB100,NW100,ND100,NL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>U</term>
	///     <term>UX100,UB100,UW100,UD100,UL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>Z</term>
	///     <term>ZX100,ZB100,ZW100,ZD100,ZL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	///   <item>
	///     <term></term>
	///     <term>R</term>
	///     <term>RX100,RB100,RW100,RD100,RL100</term>
	///     <term>10</term>
	///     <term>√</term>
	///     <term>√</term>
	///     <term></term>
	///   </item>
	/// </list>
	/// </remarks>
	public class XGBFastEnet : NetworkDeviceBase
	{
		private string CompanyID1 = "LSIS-XGT";

		private LSCpuInfo cpuInfo = LSCpuInfo.XGK;

		private byte baseNo = 0;

		private byte slotNo = 3;

		/// <summary>
		/// 所有支持的地址信息
		/// </summary>
		public static string AddressTypes = "PMLKFTCDSQINUZR";

		/// <summary>
		/// set plc
		/// </summary>
		public string SetCpuType { get; set; }

		/// <summary>
		/// CPU TYPE
		/// </summary>
		public string CpuType { get; private set; }

		/// <summary>
		/// Cpu is error
		/// </summary>
		public bool CpuError { get; private set; }

		/// <summary>
		/// RUN, STOP, ERROR, DEBUG
		/// </summary>
		public LSCpuStatus LSCpuStatus { get; private set; }

		/// <summary>
		/// FEnet I/F module’s Base No.
		/// </summary>
		public byte BaseNo
		{
			get
			{
				return baseNo;
			}
			set
			{
				baseNo = value;
			}
		}

		/// <summary>
		/// FEnet I/F module’s Slot No.
		/// </summary>
		public byte SlotNo
		{
			get
			{
				return slotNo;
			}
			set
			{
				slotNo = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public LSCpuInfo CpuInfo
		{
			get
			{
				return cpuInfo;
			}
			set
			{
				cpuInfo = value;
			}
		}

		/// <summary>
		///
		/// </summary>
		public string CompanyID
		{
			get
			{
				return CompanyID1;
			}
			set
			{
				CompanyID1 = value;
			}
		}

		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGBFastEnet()
		{
			base.WordLength = 2;
			IpAddress = "127.0.0.1";
			Port = 2004;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// Instantiate a object by ipaddress and port
		/// </summary>
		/// <param name="ipAddress">the ip address of the plc</param>
		/// <param name="port">the port of the plc, default is 2004</param>
		public XGBFastEnet(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <summary>
		/// Instantiate a object by ipaddress, port, cpuType, slotNo
		/// </summary>
		/// <param name="CpuType">CpuType</param>
		/// <param name="ipAddress">the ip address of the plc</param>
		/// <param name="port">he port of the plc, default is 2004</param>
		/// <param name="slotNo">slot number</param>
		public XGBFastEnet(string CpuType, string ipAddress, int port, byte slotNo)
			: this(ipAddress, port)
		{
			SetCpuType = CpuType;
			this.slotNo = slotNo;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new LsisFastEnetMessage();
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadByteCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return ExtractActualData(operateResult2.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = BuildWriteByteCommand(address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return ExtractActualData(operateResult2.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<byte[]> coreResult = BuildReadByteCommand(address, length);
			if (!coreResult.IsSuccess)
			{
				return coreResult;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(coreResult.Content));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return ExtractActualData(read.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<byte[]> coreResult = BuildWriteByteCommand(address, value);
			if (!coreResult.IsSuccess)
			{
				return coreResult;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(coreResult.Content));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return ExtractActualData(read.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = BuildReadByteCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ExtractActualData(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult3.Content, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			OperateResult<byte[]> operateResult = BuildReadIndividualCommand(0, address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(PackCommand(operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ExtractActualData(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult3.Content, 1)[0]);
		}

		/// <summary>
		/// ReadCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>Whether to read the successful</returns>
		public OperateResult<bool> ReadCoil(string address)
		{
			return ReadBool(address);
		}

		/// <summary>
		/// ReadCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="length">read address length</param>
		/// <returns>Whether to read the successful</returns>
		public OperateResult<bool[]> ReadCoil(string address, ushort length)
		{
			return ReadBool(address, length);
		}

		/// <summary>
		/// Read single byte value from plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <summary>
		/// Write single byte value to plc
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">value</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <summary>
		/// WriteCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">bool value</param>
		/// <returns>Whether to write the successful</returns>
		public OperateResult WriteCoil(string address, bool value)
		{
			return Write(address, new byte[2]
			{
				(byte)(value ? 1u : 0u),
				0
			});
		}

		/// <summary>
		/// WriteCoil
		/// </summary>
		/// <param name="address">Start address</param>
		/// <param name="value">bool value</param>
		/// <returns>Whether to write the successful</returns>
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return WriteCoil(address, value);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			OperateResult<byte[]> coreResult = BuildReadByteCommand(address, length);
			if (!coreResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(coreResult);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(coreResult.Content));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			OperateResult<byte[]> extract = ExtractActualData(read.Content);
			if (!extract.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(extract);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			OperateResult<byte[]> coreResult = BuildReadIndividualCommand(0, address);
			if (!coreResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(coreResult);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(PackCommand(coreResult.Content));
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			OperateResult<byte[]> extract = ExtractActualData(read.Content);
			if (!extract.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(extract);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content, 1)[0]);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.ReadCoil(System.String)" />
		public async Task<OperateResult<bool>> ReadCoilAsync(string address)
		{
			return await ReadBoolAsync(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.ReadCoil(System.String,System.UInt16)" />
		public async Task<OperateResult<bool[]>> ReadCoilAsync(string address, ushort length)
		{
			return await ReadBoolAsync(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.ReadByte(System.String)" />
		public async Task<OperateResult<byte>> ReadByteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.Write(System.String,System.Byte)" />
		public async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteAsync(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.WriteCoil(System.String,System.Boolean)" />
		public async Task<OperateResult> WriteCoilAsync(string address, bool value)
		{
			return await WriteAsync(address, new byte[2]
			{
				(byte)(value ? 1u : 0u),
				0
			});
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await WriteCoilAsync(address, value);
		}

		private byte[] PackCommand(byte[] coreCommand)
		{
			byte[] array = new byte[coreCommand.Length + 20];
			Encoding.ASCII.GetBytes(CompanyID).CopyTo(array, 0);
			switch (cpuInfo)
			{
			case LSCpuInfo.XGK:
				array[12] = 160;
				break;
			case LSCpuInfo.XGI:
				array[12] = 164;
				break;
			case LSCpuInfo.XGR:
				array[12] = 168;
				break;
			case LSCpuInfo.XGB_MK:
				array[12] = 176;
				break;
			case LSCpuInfo.XGB_IEC:
				array[12] = 180;
				break;
			}
			array[13] = 51;
			BitConverter.GetBytes((short)coreCommand.Length).CopyTo(array, 16);
			array[18] = (byte)(baseNo * 16 + slotNo);
			int num = 0;
			for (int i = 0; i < 19; i++)
			{
				num += array[i];
			}
			array[19] = (byte)num;
			coreCommand.CopyTo(array, 20);
			string text = SoftBasic.ByteToHexString(array, ' ');
			return array;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"XGBFastEnet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 需要传入 MX100.2 的 100.2 部分，返回的是
		/// AnalysisAddress IX0.0.0 QX0.0.0  MW1.0  MB1.0
		/// </summary>
		/// <param name="address">start address</param>
		/// <param name="QI">is Q or I data</param>
		/// <returns>int address</returns>
		public static int CalculateAddressStarted(string address, bool QI = false)
		{
			if (address.IndexOf('.') < 0)
			{
				return Convert.ToInt32(address);
			}
			string[] array = address.Split('.');
			if (!QI)
			{
				return Convert.ToInt32(array[0]);
			}
			if (array.Length >= 4)
			{
				return Convert.ToInt32(array[3]);
			}
			return Convert.ToInt32(array[2]);
		}

		/// <summary>
		/// NumberStyles HexNumber
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static bool IsHex(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return false;
			}
			bool result = false;
			for (int i = 0; i < value.Length; i++)
			{
				switch (value[i])
				{
				case 'A':
				case 'B':
				case 'C':
				case 'D':
				case 'E':
				case 'F':
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case 'f':
					result = true;
					break;
				}
			}
			return result;
		}

		/// <summary>
		/// AnalysisAddress
		/// </summary>
		/// <param name="address">start address</param>
		/// <param name="IsReadWrite">is read or write operate</param>
		/// <returns>analysis result</returns>
		public static OperateResult<string> AnalysisAddress(string address, bool IsReadWrite)
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				stringBuilder.Append("%");
				bool flag = false;
				if (IsReadWrite)
				{
					for (int i = 0; i < AddressTypes.Length; i++)
					{
						if (AddressTypes[i] != address[0])
						{
							continue;
						}
						stringBuilder.Append(AddressTypes[i]);
						char c = address[1];
						char c2 = c;
						if (c2 == 'X')
						{
							stringBuilder.Append("X");
							if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
							{
								stringBuilder.Append(CalculateAddressStarted(address.Substring(2), QI: true));
							}
							else if (IsHex(address.Substring(2)))
							{
								stringBuilder.Append(address.Substring(2));
							}
							else
							{
								stringBuilder.Append(CalculateAddressStarted(address.Substring(2)));
							}
						}
						else
						{
							stringBuilder.Append("B");
							int num = 0;
							if (address[1] == 'B')
							{
								num = ((address[0] != 'I' && address[0] != 'Q' && address[0] != 'U') ? CalculateAddressStarted(address.Substring(2)) : CalculateAddressStarted(address.Substring(2), QI: true));
								stringBuilder.Append((num == 0) ? num : (num *= 2));
							}
							else if (address[1] == 'W')
							{
								num = ((address[0] != 'I' && address[0] != 'Q' && address[0] != 'U') ? CalculateAddressStarted(address.Substring(2)) : CalculateAddressStarted(address.Substring(2), QI: true));
								stringBuilder.Append((num == 0) ? num : (num *= 2));
							}
							else if (address[1] == 'D')
							{
								num = CalculateAddressStarted(address.Substring(2));
								stringBuilder.Append((num == 0) ? num : (num *= 4));
							}
							else if (address[1] == 'L')
							{
								num = CalculateAddressStarted(address.Substring(2));
								stringBuilder.Append((num == 0) ? num : (num *= 8));
							}
							else if (address[0] == 'I' || address[0] == 'Q' || address[0] == 'U')
							{
								stringBuilder.Append(CalculateAddressStarted(address.Substring(1), QI: true));
							}
							else if (IsHex(address.Substring(1)))
							{
								stringBuilder.Append(address.Substring(1));
							}
							else
							{
								stringBuilder.Append(CalculateAddressStarted(address.Substring(1)));
							}
						}
						flag = true;
						break;
					}
				}
				else
				{
					stringBuilder.Append(address);
					flag = true;
				}
				if (!flag)
				{
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(stringBuilder.ToString());
		}

		/// <summary>
		/// Get DataType to Address
		/// </summary>
		/// <param name="address">address</param>
		/// <returns>dataType</returns>
		public static OperateResult<string> GetDataTypeToAddress(string address)
		{
			string value = string.Empty;
			try
			{
				char[] array = new char[12]
				{
					'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q',
					'I', 'R'
				};
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == address[0])
					{
						switch (address[1])
						{
						case 'X':
							value = "Bit";
							break;
						case 'W':
							value = "Word";
							break;
						case 'D':
							value = "DWord";
							break;
						case 'L':
							value = "LWord";
							break;
						case 'B':
							value = "Continuous";
							break;
						default:
							value = "Continuous";
							break;
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(value);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBFastEnet.BuildReadIndividualCommand(System.Byte,System.String[])" />
		public static OperateResult<byte[]> BuildReadIndividualCommand(byte dataType, string address)
		{
			return BuildReadIndividualCommand(dataType, new string[1] { address });
		}

		/// <summary>
		/// Multi reading address Type of Read Individual
		/// </summary>
		/// <param name="dataType">dataType bit:0x04, byte:0x01, word:0x02, dword:0x03, lword:0x04, continuous:0x14</param>
		/// <param name="addresses">address, for example: MX100, PX100</param>
		/// <returns>Read Individual Command</returns>
		public static OperateResult<byte[]> BuildReadIndividualCommand(byte dataType, string[] addresses)
		{
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.WriteByte(84);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(dataType);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte((byte)addresses.Length);
			memoryStream.WriteByte(0);
			foreach (string address in addresses)
			{
				OperateResult<string> operateResult = AnalysisAddress(address, IsReadWrite: true);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult);
				}
				memoryStream.WriteByte((byte)operateResult.Content.Length);
				memoryStream.WriteByte(0);
				byte[] bytes = Encoding.ASCII.GetBytes(operateResult.Content);
				memoryStream.Write(bytes, 0, bytes.Length);
			}
			return OperateResult.CreateSuccessResult(memoryStream.ToArray());
		}

		private static OperateResult<byte[]> BuildReadByteCommand(string address, ushort length)
		{
			OperateResult<string> operateResult = AnalysisAddress(address, IsReadWrite: true);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<string> dataTypeToAddress = GetDataTypeToAddress(address);
			if (!dataTypeToAddress.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress);
			}
			byte[] array = new byte[12 + operateResult.Content.Length];
			switch (dataTypeToAddress.Content)
			{
			case "Bit":
				array[2] = 0;
				break;
			case "Word":
			case "DWord":
			case "LWord":
			case "Continuous":
				array[2] = 20;
				break;
			}
			array[0] = 84;
			array[1] = 0;
			array[2] = 0;
			array[3] = 0;
			array[4] = 0;
			array[5] = 0;
			array[6] = 1;
			array[7] = 0;
			array[8] = (byte)operateResult.Content.Length;
			array[9] = 0;
			Encoding.ASCII.GetBytes(operateResult.Content).CopyTo(array, 10);
			BitConverter.GetBytes(length).CopyTo(array, array.Length - 2);
			return OperateResult.CreateSuccessResult(array);
		}

		private OperateResult<byte[]> BuildWriteByteCommand(string address, byte[] data)
		{
			string setCpuType = SetCpuType;
			string text = setCpuType;
			OperateResult<string> operateResult = ((text == "XGK") ? AnalysisAddress(address, IsReadWrite: true) : ((!(text == "XGB")) ? AnalysisAddress(address, IsReadWrite: true) : AnalysisAddress(address, IsReadWrite: false)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<string> dataTypeToAddress = GetDataTypeToAddress(address);
			if (!dataTypeToAddress.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress);
			}
			byte[] array = new byte[12 + operateResult.Content.Length + data.Length];
			switch (dataTypeToAddress.Content)
			{
			case "Bit":
			case "Byte":
				array[2] = 1;
				break;
			case "Word":
				array[2] = 2;
				break;
			case "DWord":
				array[2] = 3;
				break;
			case "LWord":
				array[2] = 4;
				break;
			case "Continuous":
				array[2] = 20;
				break;
			}
			array[0] = 88;
			array[1] = 0;
			array[3] = 0;
			array[4] = 0;
			array[5] = 0;
			array[6] = 1;
			array[7] = 0;
			array[8] = (byte)operateResult.Content.Length;
			array[9] = 0;
			Encoding.ASCII.GetBytes(operateResult.Content).CopyTo(array, 10);
			BitConverter.GetBytes(data.Length).CopyTo(array, array.Length - 2 - data.Length);
			data.CopyTo(array, array.Length - data.Length);
			return OperateResult.CreateSuccessResult(array);
		}

		/// <summary>
		/// Returns true data content, supports read and write returns
		/// </summary>
		/// <param name="response">response data</param>
		/// <returns>real data</returns>
		public OperateResult<byte[]> ExtractActualData(byte[] response)
		{
			if (response.Length < 20)
			{
				return new OperateResult<byte[]>("Length is less than 20:" + SoftBasic.ByteToHexString(response));
			}
			ushort num = BitConverter.ToUInt16(response, 10);
			BitArray bitArray = new BitArray(BitConverter.GetBytes(num));
			int num2 = (int)num % 32;
			switch ((int)num % 32)
			{
			case 1:
				CpuType = "XGK/R-CPUH";
				break;
			case 2:
				CpuType = "XGK-CPUS";
				break;
			case 4:
				CpuType = "XGK-CPUE";
				break;
			case 5:
				CpuType = "XGK/R-CPUH";
				break;
			case 6:
				CpuType = "XGB/XBCU";
				break;
			}
			CpuError = bitArray[7];
			if (bitArray[8])
			{
				LSCpuStatus = LSCpuStatus.RUN;
			}
			if (bitArray[9])
			{
				LSCpuStatus = LSCpuStatus.STOP;
			}
			if (bitArray[10])
			{
				LSCpuStatus = LSCpuStatus.ERROR;
			}
			if (bitArray[11])
			{
				LSCpuStatus = LSCpuStatus.DEBUG;
			}
			if (response.Length < 28)
			{
				return new OperateResult<byte[]>("Length is less than 28:" + SoftBasic.ByteToHexString(response));
			}
			ushort num3 = BitConverter.ToUInt16(response, 26);
			if (num3 > 0)
			{
				return new OperateResult<byte[]>(response[28], "Error:" + GetErrorDesciption(response[28]));
			}
			if (response[20] == 89)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (response[20] == 85)
			{
				try
				{
					ushort num4 = BitConverter.ToUInt16(response, 30);
					byte[] array = new byte[num4];
					Array.Copy(response, 32, array, 0, num4);
					return OperateResult.CreateSuccessResult(array);
				}
				catch (Exception ex)
				{
					return new OperateResult<byte[]>(ex.Message);
				}
			}
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <summary>
		/// get the description of the error code meanning
		/// </summary>
		/// <param name="code">code value</param>
		/// <returns>string information</returns>
		public static string GetErrorDesciption(byte code)
		{
			switch (code)
			{
			case 0:
				return "Normal";
			case 1:
				return "Physical layer error (TX, RX unavailable)";
			case 3:
				return "There is no identifier of Function Block to receive in communication channel";
			case 4:
				return "Mismatch of data type";
			case 5:
				return "Reset is received from partner station";
			case 6:
				return "Communication instruction of partner station is not ready status";
			case 7:
				return "Device status of remote station is not desirable status";
			case 8:
				return "Access to some target is not available";
			case 9:
				return "Can’ t deal with communication instruction of partner station by too many reception";
			case 10:
				return "Time Out error";
			case 11:
				return "Structure error";
			case 12:
				return "Abort";
			case 13:
				return "Reject(local/remote)";
			case 14:
				return "Communication channel establishment error (Connect/Disconnect)";
			case 15:
				return "High speed communication and connection service error";
			case 33:
				return "Can’t find variable identifier";
			case 34:
				return "Address error";
			case 50:
				return "Response error";
			case 113:
				return "Object Access Unsupported";
			case 187:
				return "Unknown error code (communication code of other company) is received";
			default:
				return "Unknown error";
			}
		}
	}
}

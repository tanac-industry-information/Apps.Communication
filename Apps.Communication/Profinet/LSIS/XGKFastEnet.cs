using System;
using System.Collections;
using System.Collections.Generic;
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
	/// XGK Fast Enet I/F module supports open Ethernet. It provides network configuration that is to connect LSIS and other company PLC, PC on network
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
	public class XGKFastEnet : NetworkDeviceBase
	{
		private string CompanyID1 = "LSIS-XGT";

		private LSCpuInfo cpuInfo = LSCpuInfo.XGK;

		private byte baseNo = 0;

		private byte slotNo = 3;

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
		public XGKFastEnet()
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
		public XGKFastEnet(string ipAddress, int port)
		{
			base.WordLength = 2;
			IpAddress = ipAddress;
			Port = port;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// Instantiate a object by ipaddress, port, cpuType, slotNo
		/// </summary>
		/// <param name="CpuType">CpuType</param>
		/// <param name="ipAddress">the ip address of the plc</param>
		/// <param name="port">he port of the plc, default is 2004</param>
		/// <param name="slotNo">slot number</param>
		public XGKFastEnet(string CpuType, string ipAddress, int port, byte slotNo)
		{
			SetCpuType = CpuType;
			base.WordLength = 2;
			IpAddress = ipAddress;
			Port = port;
			this.slotNo = slotNo;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new LsisFastEnetMessage();
		}

		/// <inheritdoc />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			List<XGTAddressData> lstAdress = new List<XGTAddressData>();
			string[] ArrayAdress = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array = ArrayAdress;
			foreach (string item in array)
			{
				XGTAddressData addrData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> DataTypeResult2 = GetDataTypeToAddress(item);
				if (DataTypeResult2.Content2)
				{
					addrData.Address = item.Substring(1);
				}
				else
				{
					addrData.Address = item.Substring(2);
				}
				lstAdress.Add(addrData);
			}
			OperateResult<XGT_MemoryType> analysisResult = AnalysisAddress(ArrayAdress[0]);
			if (!analysisResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(analysisResult);
			}
			OperateResult<XGT_DataType, bool> DataTypeResult = GetDataTypeToAddress(ArrayAdress[0]);
			if (!DataTypeResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(DataTypeResult);
			}
			OperateResult<byte[]> coreResult = ((DataTypeResult.Content1 != XGT_DataType.Continue) ? Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1) : Read(DataTypeResult.Content1, lstAdress, analysisResult.Content, 1, length));
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(coreResult.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			if (lstAdress.Count > 1)
			{
				OperateResult<bool[]> extract2 = ExtractActualDataBool(read.Content);
				if (!extract2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(extract2);
				}
				return OperateResult.CreateSuccessResult(extract2.Content);
			}
			OperateResult<byte[]> extract = ExtractActualData(read.Content);
			if (!extract.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(extract);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(extract.Content));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.ReadCoil(System.String)" />
		public async Task<OperateResult<bool>> ReadCoilAsync(string address)
		{
			return await ReadBoolAsync(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.ReadCoil(System.String,System.UInt16)" />
		public async Task<OperateResult<bool[]>> ReadCoilAsync(string address, ushort length)
		{
			return await ReadBoolAsync(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.ReadByte(System.String)" />
		public async Task<OperateResult<byte>> ReadByteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.Write(System.String,System.Byte)" />
		public async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteAsync(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.WriteCoil(System.String,System.Boolean)" />
		public async Task<OperateResult> WriteCoilAsync(string address, bool value)
		{
			return await WriteAsync(address, new byte[2]
			{
				(byte)(value ? 1u : 0u),
				0
			});
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKFastEnet.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await WriteCoilAsync(address, value);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = null;
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = GetDataTypeToAddress(text);
				if (dataTypeToAddress.Content2)
				{
					xGTAddressData.Address = text.Substring(1);
				}
				else
				{
					xGTAddressData.Address = text.Substring(2);
				}
				list.Add(xGTAddressData);
			}
			OperateResult<XGT_MemoryType> operateResult2 = AnalysisAddress(array[0]);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = GetDataTypeToAddress(array[0]);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress2);
			}
			operateResult = ((dataTypeToAddress2.Content1 != XGT_DataType.Continue) ? Read(dataTypeToAddress2.Content1, list, operateResult2.Content, 1) : Read(dataTypeToAddress2.Content1, list, operateResult2.Content, 1, length));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			if (list.Count > 1)
			{
				return ExtractActualDatabyte(operateResult3.Content);
			}
			return ExtractActualData(operateResult3.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<byte[]> operateResult = null;
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = GetDataTypeToAddress(text);
				if (dataTypeToAddress.Content2)
				{
					xGTAddressData.Address = text.Substring(1);
				}
				else
				{
					xGTAddressData.Address = text.Substring(2);
				}
				xGTAddressData.DataByteArray = value;
				list.Add(xGTAddressData);
			}
			OperateResult<XGT_MemoryType> operateResult2 = AnalysisAddress(address);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = GetDataTypeToAddress(address);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress2);
			}
			operateResult = ((dataTypeToAddress2.Content1 != XGT_DataType.Continue) ? Write(dataTypeToAddress2.Content1, list, operateResult2.Content, 1) : Write(dataTypeToAddress2.Content1, list, operateResult2.Content, 1, value.Length));
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			return ExtractActualData(operateResult3.Content);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = null;
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = GetDataTypeToAddress(text);
				if (dataTypeToAddress.Content2)
				{
					xGTAddressData.Address = text.Substring(1);
				}
				else
				{
					xGTAddressData.Address = text.Substring(2);
				}
				list.Add(xGTAddressData);
			}
			OperateResult<XGT_MemoryType> operateResult2 = AnalysisAddress(array[0]);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = GetDataTypeToAddress(array[0]);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(dataTypeToAddress2);
			}
			operateResult = ((dataTypeToAddress2.Content1 != XGT_DataType.Continue) ? Read(dataTypeToAddress2.Content1, list, operateResult2.Content, 1) : Read(dataTypeToAddress2.Content1, list, operateResult2.Content, 1, length));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			if (list.Count > 1)
			{
				OperateResult<bool[]> operateResult4 = ExtractActualDataBool(operateResult3.Content);
				if (!operateResult4.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool[]>(operateResult4);
				}
				return OperateResult.CreateSuccessResult(operateResult4.Content);
			}
			OperateResult<byte[]> operateResult5 = ExtractActualData(operateResult3.Content);
			if (!operateResult5.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult5);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult5.Content));
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

		/// <summary>
		/// Read
		/// </summary>
		/// <param name="pDataType"></param>
		/// <param name="pAddress"></param>
		/// <param name="pMemtype"></param>
		/// <param name="pInvokeID"></param>
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Read(XGT_DataType pDataType, List<XGTAddressData> pAddress, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
		{
			if (pAddress.Count > 16)
			{
				return new OperateResult<byte[]>("You cannot read more than 16 pieces.");
			}
			try
			{
				byte[] array = CreateReadDataFormat(XGT_Request_Func.Read, pDataType, pAddress, pMemtype, pDataCount);
				byte[] array2 = CreateHeader(pInvokeID, array.Length);
				byte[] header = new byte[array2.Length + array.Length];
				int idx = 0;
				AddByte(array2, ref idx, ref header);
				AddByte(array, ref idx, ref header);
				return OperateResult.CreateSuccessResult(header);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
			}
		}

		/// <summary>
		/// Write
		/// </summary>
		/// <param name="pDataType"></param>
		/// <param name="pAddressList"></param>
		/// <param name="pMemtype"></param>
		/// <param name="pInvokeID"></param>
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Write(XGT_DataType pDataType, List<XGTAddressData> pAddressList, XGT_MemoryType pMemtype, int pInvokeID, int pDataCount = 0)
		{
			try
			{
				byte[] array = CreateWriteDataFormat(XGT_Request_Func.Write, pDataType, pAddressList, pMemtype, pDataCount);
				byte[] array2 = CreateHeader(pInvokeID, array.Length);
				byte[] header = new byte[array2.Length + array.Length];
				int idx = 0;
				AddByte(array2, ref idx, ref header);
				AddByte(array, ref idx, ref header);
				return OperateResult.CreateSuccessResult(header);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
			}
		}

		/// <summary>
		/// CreateHeader
		/// </summary>
		/// <param name="pInvokeID"></param>
		/// <param name="pDataByteLenth"></param>
		/// <returns></returns>
		public byte[] CreateHeader(int pInvokeID, int pDataByteLenth)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(CompanyID);
			byte[] bytes2 = BitConverter.GetBytes((short)0);
			byte[] bytes3 = BitConverter.GetBytes((short)0);
			byte[] array = new byte[1];
			switch (cpuInfo)
			{
			case LSCpuInfo.XGK:
				array[0] = 160;
				break;
			case LSCpuInfo.XGI:
				array[0] = 164;
				break;
			case LSCpuInfo.XGR:
				array[0] = 168;
				break;
			case LSCpuInfo.XGB_MK:
				array[0] = 176;
				break;
			case LSCpuInfo.XGB_IEC:
				array[0] = 180;
				break;
			}
			byte[] array2 = new byte[1] { 51 };
			byte[] bytes4 = BitConverter.GetBytes((short)pInvokeID);
			byte[] bytes5 = BitConverter.GetBytes((short)pDataByteLenth);
			byte[] array3 = new byte[1] { (byte)(baseNo * 16 + slotNo) };
			byte[] array4 = new byte[1] { 0 };
			int num = bytes.Length + bytes2.Length + bytes3.Length + array.Length + array2.Length + bytes4.Length + bytes5.Length + array3.Length + array4.Length;
			byte[] header = new byte[num];
			int idx = 0;
			AddByte(bytes, ref idx, ref header);
			AddByte(bytes2, ref idx, ref header);
			AddByte(bytes3, ref idx, ref header);
			AddByte(array, ref idx, ref header);
			AddByte(array2, ref idx, ref header);
			AddByte(bytes4, ref idx, ref header);
			AddByte(bytes5, ref idx, ref header);
			AddByte(array3, ref idx, ref header);
			AddByte(array4, ref idx, ref header);
			return header;
		}

		private byte[] CreateReadDataFormat(XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			int num = 0;
			byte[] bytes = BitConverter.GetBytes((short)emFunc);
			byte[] bytes2 = BitConverter.GetBytes((short)emDatatype);
			byte[] bytes3 = BitConverter.GetBytes((short)0);
			byte[] bytes4 = BitConverter.GetBytes((short)pAddressList.Count);
			num = bytes.Length + bytes2.Length + bytes3.Length + bytes4.Length;
			foreach (XGTAddressData pAddress in pAddressList)
			{
				string addressString = CreateValueName(emDatatype, emMemtype, pAddress.Address);
				XGTAddressData xGTAddressData = new XGTAddressData();
				xGTAddressData.AddressString = addressString;
				list.Add(xGTAddressData);
				num += xGTAddressData.AddressByteArray.Length + xGTAddressData.LengthByteArray.Length;
			}
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
			{
				num += 2;
			}
			byte[] header = new byte[num];
			int idx = 0;
			AddByte(bytes, ref idx, ref header);
			AddByte(bytes2, ref idx, ref header);
			AddByte(bytes3, ref idx, ref header);
			AddByte(bytes4, ref idx, ref header);
			foreach (XGTAddressData item in list)
			{
				AddByte(item.LengthByteArray, ref idx, ref header);
				AddByte(item.AddressByteArray, ref idx, ref header);
			}
			if (XGT_DataType.Continue == emDatatype)
			{
				byte[] bytes5 = BitConverter.GetBytes((short)pDataCount);
				AddByte(bytes5, ref idx, ref header);
			}
			return header;
		}

		private byte[] CreateWriteDataFormat(XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			int num = 0;
			byte[] bytes = BitConverter.GetBytes((short)emFunc);
			byte[] bytes2 = BitConverter.GetBytes((short)emDatatype);
			byte[] bytes3 = BitConverter.GetBytes((short)0);
			byte[] bytes4 = BitConverter.GetBytes((short)pAddressList.Count);
			num = bytes.Length + bytes2.Length + bytes3.Length + bytes4.Length;
			List<XGTAddressData> list = new List<XGTAddressData>();
			foreach (XGTAddressData pAddress in pAddressList)
			{
				string text2 = (pAddress.AddressString = CreateValueName(emDatatype, emMemtype, pAddress.Address));
				int num2 = 0;
				num2 = pAddress.DataByteArray.Length;
				num += pAddress.AddressByteArray.Length + pAddress.LengthByteArray.Length + 2 + num2;
				list.Add(pAddress);
			}
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
			{
				num += 2;
			}
			byte[] header = new byte[num];
			int idx = 0;
			AddByte(bytes, ref idx, ref header);
			AddByte(bytes2, ref idx, ref header);
			AddByte(bytes3, ref idx, ref header);
			AddByte(bytes4, ref idx, ref header);
			foreach (XGTAddressData item in list)
			{
				AddByte(item.LengthByteArray, ref idx, ref header);
				AddByte(item.AddressByteArray, ref idx, ref header);
			}
			foreach (XGTAddressData item2 in list)
			{
				byte[] bytes5 = BitConverter.GetBytes((short)item2.DataByteArray.Length);
				AddByte(bytes5, ref idx, ref header);
				AddByte(item2.DataByteArray, ref idx, ref header);
			}
			return header;
		}

		/// <summary>
		///             Create a memory address variable name.
		/// </summary>
		/// <param name="dataType">데이터타입</param>
		/// <param name="memType">메모리타입</param>
		/// <param name="pAddress">주소번지</param>
		/// <returns></returns>
		public string CreateValueName(XGT_DataType dataType, XGT_MemoryType memType, string pAddress)
		{
			string empty = string.Empty;
			string memTypeChar = GetMemTypeChar(memType);
			string typeChar = GetTypeChar(dataType);
			if (dataType == XGT_DataType.Continue)
			{
				pAddress = (Convert.ToInt32(pAddress) * 2).ToString();
			}
			if (dataType == XGT_DataType.Bit)
			{
				int num = 0;
				string value = pAddress.Substring(0, pAddress.Length - 1);
				string value2 = pAddress.Substring(pAddress.Length - 1);
				num = Convert.ToInt32(value2, 16);
				pAddress = ((!string.IsNullOrEmpty(value)) ? (Convert.ToInt32(value) * 16 + num).ToString() : num.ToString());
			}
			return "%" + memTypeChar + typeChar + pAddress;
		}

		/// <summary>
		/// Char return according to data type
		/// </summary>
		/// <param name="type">데이터타입</param>
		/// <returns></returns>
		private string GetTypeChar(XGT_DataType type)
		{
			string empty = string.Empty;
			switch (type)
			{
			case XGT_DataType.Bit:
				return "X";
			case XGT_DataType.Byte:
				return "B";
			case XGT_DataType.Word:
				return "W";
			case XGT_DataType.DWord:
				return "D";
			case XGT_DataType.LWord:
				return "L";
			case XGT_DataType.Continue:
				return "B";
			default:
				return "X";
			}
		}

		/// <summary>
		/// Char return according to memory type
		/// </summary>
		/// <param name="type">메모리타입</param>
		/// <returns></returns>
		private string GetMemTypeChar(XGT_MemoryType type)
		{
			string result = string.Empty;
			switch (type)
			{
			case XGT_MemoryType.IO:
				result = "P";
				break;
			case XGT_MemoryType.SubRelay:
				result = "M";
				break;
			case XGT_MemoryType.LinkRelay:
				result = "L";
				break;
			case XGT_MemoryType.KeepRelay:
				result = "K";
				break;
			case XGT_MemoryType.EtcRelay:
				result = "F";
				break;
			case XGT_MemoryType.Timer:
				result = "T";
				break;
			case XGT_MemoryType.DataRegister:
				result = "D";
				break;
			case XGT_MemoryType.Counter:
				result = "C";
				break;
			case XGT_MemoryType.ComDataRegister:
				result = "N";
				break;
			case XGT_MemoryType.FileDataRegister:
				result = "R";
				break;
			case XGT_MemoryType.StepRelay:
				result = "S";
				break;
			case XGT_MemoryType.SpecialRegister:
				result = "U";
				break;
			}
			return result;
		}

		/// <summary>
		/// 바이트 합치기
		/// </summary>
		/// <param name="item">개별바이트</param>
		/// <param name="idx">전체바이트에 개별바이트를 합칠 인덱스</param>
		/// <param name="header">전체바이트</param>
		/// <returns>전체 바이트 </returns>
		private byte[] AddByte(byte[] item, ref int idx, ref byte[] header)
		{
			Array.Copy(item, 0, header, idx, item.Length);
			idx += item.Length;
			return header;
		}

		/// <summary>
		/// AnalysisAddress XGT_MemoryType
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static OperateResult<XGT_MemoryType> AnalysisAddress(string address)
		{
			XGT_MemoryType value = XGT_MemoryType.IO;
			try
			{
				char[] array = new char[15]
				{
					'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q',
					'I', 'N', 'U', 'Z', 'R'
				};
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == address[0])
					{
						switch (address[0])
						{
						case 'P':
							value = XGT_MemoryType.IO;
							break;
						case 'M':
							value = XGT_MemoryType.SubRelay;
							break;
						case 'L':
							value = XGT_MemoryType.LinkRelay;
							break;
						case 'K':
							value = XGT_MemoryType.KeepRelay;
							break;
						case 'F':
							value = XGT_MemoryType.EtcRelay;
							break;
						case 'T':
							value = XGT_MemoryType.Timer;
							break;
						case 'C':
							value = XGT_MemoryType.Counter;
							break;
						case 'D':
							value = XGT_MemoryType.DataRegister;
							break;
						case 'N':
							value = XGT_MemoryType.ComDataRegister;
							break;
						case 'R':
							value = XGT_MemoryType.FileDataRegister;
							break;
						case 'S':
							value = XGT_MemoryType.StepRelay;
							break;
						case 'U':
							value = XGT_MemoryType.SpecialRegister;
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
				return new OperateResult<XGT_MemoryType>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(value);
		}

		/// <summary>
		/// GetDataTypeToAddress
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static OperateResult<XGT_DataType, bool> GetDataTypeToAddress(string address)
		{
			XGT_DataType value = XGT_DataType.Bit;
			bool value2 = false;
			try
			{
				char[] array = new char[15]
				{
					'P', 'M', 'L', 'K', 'F', 'T', 'C', 'D', 'S', 'Q',
					'I', 'N', 'U', 'Z', 'R'
				};
				bool flag = false;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == address[0])
					{
						switch (address[1])
						{
						case 'X':
							value = XGT_DataType.Bit;
							break;
						case 'W':
							value = XGT_DataType.Word;
							break;
						case 'D':
							value = XGT_DataType.DWord;
							break;
						case 'L':
							value = XGT_DataType.LWord;
							break;
						case 'B':
							value = XGT_DataType.Byte;
							break;
						case 'C':
							value = XGT_DataType.Continue;
							break;
						default:
							value2 = true;
							value = XGT_DataType.Continue;
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
				return new OperateResult<XGT_DataType, bool>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(value, value2);
		}

		/// <summary>
		/// Returns true data content, supports read and write returns
		/// </summary>
		/// <param name="response">response data</param>
		/// <returns>real data</returns>
		public OperateResult<byte[]> ExtractActualData(byte[] response)
		{
			OperateResult<bool> cpuTypeToPLC = GetCpuTypeToPLC(response);
			if (!cpuTypeToPLC.IsSuccess)
			{
				return new OperateResult<byte[]>(cpuTypeToPLC.Message);
			}
			if (response[20] == 89)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (response[20] == 85)
			{
				try
				{
					ushort num = BitConverter.ToUInt16(response, 30);
					byte[] array = new byte[num];
					Array.Copy(response, 32, array, 0, num);
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
		/// SetCpuType
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public OperateResult<bool> GetCpuTypeToPLC(byte[] response)
		{
			try
			{
				if (response.Length < 20)
				{
					return new OperateResult<bool>("Length is less than 20:" + SoftBasic.ByteToHexString(response));
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
					return new OperateResult<bool>("Length is less than 28:" + SoftBasic.ByteToHexString(response));
				}
				ushort num3 = BitConverter.ToUInt16(response, 26);
				if (num3 > 0)
				{
					return new OperateResult<bool>(response[28], "Error:" + GetErrorDesciption(response[28]));
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<bool>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(value: true);
		}

		/// <summary>
		/// Returns true data content, supports read and write returns
		/// </summary>
		/// <param name="response">response data</param>
		/// <returns>real data</returns>
		public OperateResult<bool[]> ExtractActualDataBool(byte[] response)
		{
			OperateResult<bool> cpuTypeToPLC = GetCpuTypeToPLC(response);
			if (!cpuTypeToPLC.IsSuccess)
			{
				return new OperateResult<bool[]>(cpuTypeToPLC.Message);
			}
			if (response[20] == 89)
			{
				return OperateResult.CreateSuccessResult(new bool[0]);
			}
			if (response[20] == 85)
			{
				int num = 28;
				byte[] array = new byte[2];
				byte[] array2 = new byte[2];
				byte[] array3 = new byte[2];
				Array.Copy(response, num, array, 0, 2);
				int num2 = BitConverter.ToInt16(array, 0);
				List<bool> list = new List<bool>();
				num += 2;
				try
				{
					for (int i = 0; i < num2; i++)
					{
						Array.Copy(response, num, array2, 0, 2);
						int num3 = BitConverter.ToInt16(array2, 0);
						num += 2;
						array3 = new byte[num3];
						Array.Copy(response, num, array3, 0, num3);
						num += num3;
						list.Add(BitConverter.ToBoolean(array3, 0));
					}
					return OperateResult.CreateSuccessResult(list.ToArray());
				}
				catch (Exception ex)
				{
					return new OperateResult<bool[]>(ex.Message);
				}
			}
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <summary>
		/// Returns true data content, supports read and write returns
		/// </summary>
		/// <param name="response">response data</param>
		/// <returns>real data</returns>
		public OperateResult<byte[]> ExtractActualDatabyte(byte[] response)
		{
			OperateResult<bool> cpuTypeToPLC = GetCpuTypeToPLC(response);
			if (!cpuTypeToPLC.IsSuccess)
			{
				return new OperateResult<byte[]>(cpuTypeToPLC.Message);
			}
			if (response[20] == 89)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			if (response[20] == 85)
			{
				int num = 28;
				byte[] array = new byte[2];
				byte[] array2 = new byte[2];
				byte[] array3 = new byte[2];
				Array.Copy(response, num, array, 0, 2);
				int num2 = BitConverter.ToInt16(array, 0);
				List<byte> list = new List<byte>();
				num += 2;
				try
				{
					for (int i = 0; i < num2; i++)
					{
						Array.Copy(response, num, array2, 0, 2);
						int num3 = BitConverter.ToInt16(array2, 0);
						num += 2;
						Array.Copy(response, num, array3, 0, num3);
						num += num3;
						list.AddRange(array3);
					}
					return OperateResult.CreateSuccessResult(list.ToArray());
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

		/// <inheritdoc />
		public override string ToString()
		{
			return $"XGkFastEnet[{IpAddress}:{Port}]";
		}
	}
}

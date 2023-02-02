using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.LSIS
{
	/// <summary>
	/// XGk Cnet I/F module supports Serial Port.
	/// </summary>
	/// <remarks>
	/// XGB 主机的通道 0 仅支持 1:1 通信。 对于具有主从格式的 1:N 系统，在连接 XGL-C41A 模块的通道 1 或 XGB 主机中使用 RS-485 通信。 XGL-C41A 模块支持 RS-422/485 协议。
	/// </remarks>
	public class XGKCnet : SerialDeviceBase
	{
		/// <inheritdoc cref="P:Communication.Profinet.LSIS.XGBCnetOverTcp.Station" />
		public byte Station { get; set; } = 5;


		/// <summary>
		/// Instantiate a Default object
		/// </summary>
		public XGKCnet()
		{
			base.ByteTransform = new RegularByteTransform();
			base.WordLength = 2;
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadByte(System.String)" />
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 2));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.Write(System.String,System.Byte)" />
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = XGKFastEnet.GetDataTypeToAddress(text);
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
			OperateResult<XGT_MemoryType> operateResult = XGKFastEnet.AnalysisAddress(array[0]);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = XGKFastEnet.GetDataTypeToAddress(array[0]);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(dataTypeToAddress2);
			}
			OperateResult<byte[]> operateResult2 = Read(dataTypeToAddress2.Content1, list, operateResult.Content, 1);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(ExtractActualData(operateResult3.Content, isRead: true).Content, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadCoil(System.String)" />
		public OperateResult<bool> ReadCoil(string address)
		{
			return ReadBool(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGBCnetOverTcp.ReadCoil(System.String,System.UInt16)" />
		public OperateResult<bool[]> ReadCoil(string address, ushort length)
		{
			return ReadBool(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.LSIS.XGKCnet.WriteCoil(System.String,System.Boolean)" />
		public OperateResult WriteCoil(string address, bool value)
		{
			return Write(address, value);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return Write(address, new byte[1] { (byte)(value ? 1u : 0u) });
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await WriteAsync(address, new byte[1] { (byte)(value ? 1u : 0u) });
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = XGKFastEnet.GetDataTypeToAddress(text);
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
			OperateResult<XGT_MemoryType> operateResult = XGKFastEnet.AnalysisAddress(array[0]);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = XGKFastEnet.GetDataTypeToAddress(array[0]);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress2);
			}
			OperateResult<byte[]> operateResult2 = Read(dataTypeToAddress2.Content1, list, operateResult.Content, length);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return ExtractActualData(operateResult3.Content, isRead: true);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			string[] array = address.Split(new string[2] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				XGTAddressData xGTAddressData = new XGTAddressData();
				OperateResult<XGT_DataType, bool> dataTypeToAddress = XGKFastEnet.GetDataTypeToAddress(text);
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
			OperateResult<XGT_MemoryType> operateResult = XGKFastEnet.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			OperateResult<XGT_DataType, bool> dataTypeToAddress2 = XGKFastEnet.GetDataTypeToAddress(address);
			if (!dataTypeToAddress2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(dataTypeToAddress2);
			}
			OperateResult<byte[]> operateResult2 = Write(dataTypeToAddress2.Content1, list, operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return ExtractActualData(operateResult3.Content, isRead: false);
		}

		/// <summary>
		/// Read
		/// </summary>
		/// <param name="pDataType"></param>
		/// <param name="pAddress"></param>
		/// <param name="pMemtype"></param>
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Read(XGT_DataType pDataType, List<XGTAddressData> pAddress, XGT_MemoryType pMemtype, int pDataCount = 0)
		{
			if (pAddress.Count > 16)
			{
				return new OperateResult<byte[]>("You cannot read more than 16 pieces.");
			}
			try
			{
				byte[] value = CreateReadDataFormat(Station, XGT_Request_Func.Read, pDataType, pAddress, pMemtype, pDataCount);
				return OperateResult.CreateSuccessResult(value);
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
		/// <param name="pDataCount"></param>
		/// <returns></returns>
		public OperateResult<byte[]> Write(XGT_DataType pDataType, List<XGTAddressData> pAddressList, XGT_MemoryType pMemtype, int pDataCount = 0)
		{
			try
			{
				byte[] value = CreateWriteDataFormat(Station, XGT_Request_Func.Write, pDataType, pAddressList, pMemtype, pDataCount);
				return OperateResult.CreateSuccessResult(value);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("ERROR:" + ex.Message.ToString());
			}
		}

		private byte[] CreateReadDataFormat(byte station, XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			foreach (XGTAddressData pAddress in pAddressList)
			{
				string addressString = new XGKFastEnet().CreateValueName(emDatatype, emMemtype, pAddress.Address);
				XGTAddressData xGTAddressData = new XGTAddressData();
				xGTAddressData.AddressString = addressString;
				list.Add(xGTAddressData);
			}
			List<byte> list2 = new List<byte>();
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Read == emFunc)
			{
				list2.Add(5);
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				list2.Add(114);
				list2.Add(83);
				list2.Add(66);
				foreach (XGTAddressData item in list)
				{
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)item.AddressString.Length));
					list2.AddRange(Encoding.ASCII.GetBytes(item.AddressString));
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)pDataCount));
				}
				list2.Add(4);
				int num = 0;
				for (int i = 0; i < list2.Count; i++)
				{
					num += list2[i];
				}
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			}
			else
			{
				list2.Add(5);
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				list2.Add(114);
				list2.Add(83);
				list2.Add(83);
				list2.Add(48);
				list2.Add(49);
				foreach (XGTAddressData item2 in list)
				{
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)item2.AddressString.Length));
					list2.AddRange(Encoding.ASCII.GetBytes(item2.AddressString));
				}
				list2.Add(4);
				int num2 = 0;
				for (int j = 0; j < list2.Count; j++)
				{
					num2 += list2[j];
				}
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num2));
			}
			return list2.ToArray();
		}

		private byte[] CreateWriteDataFormat(byte station, XGT_Request_Func emFunc, XGT_DataType emDatatype, List<XGTAddressData> pAddressList, XGT_MemoryType emMemtype, int pDataCount)
		{
			List<XGTAddressData> list = new List<XGTAddressData>();
			foreach (XGTAddressData pAddress in pAddressList)
			{
				string text2 = (pAddress.AddressString = new XGKFastEnet().CreateValueName(emDatatype, emMemtype, pAddress.Address));
				list.Add(pAddress);
			}
			List<byte> list2 = new List<byte>();
			if (XGT_DataType.Continue == emDatatype && XGT_Request_Func.Write == emFunc)
			{
				list2.Add(5);
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				list2.Add(119);
				list2.Add(83);
				list2.Add(66);
				foreach (XGTAddressData item in list)
				{
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)item.AddressString.Length));
					list2.AddRange(Encoding.ASCII.GetBytes(item.AddressString));
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)item.AddressByteArray.Length));
					list2.AddRange(SoftBasic.BytesToAsciiBytes(item.AddressByteArray));
				}
				list2.Add(4);
				int num = 0;
				for (int i = 0; i < list2.Count; i++)
				{
					num += list2[i];
				}
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num));
			}
			else
			{
				list2.Add(5);
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom(station));
				list2.Add(119);
				list2.Add(83);
				list2.Add(83);
				list2.Add(48);
				list2.Add(49);
				foreach (XGTAddressData item2 in list)
				{
					list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)item2.AddressString.Length));
					list2.AddRange(Encoding.ASCII.GetBytes(item2.AddressString));
					list2.AddRange(SoftBasic.BytesToAsciiBytes(item2.AddressByteArray));
				}
				list2.Add(4);
				int num2 = 0;
				for (int j = 0; j < list2.Count; j++)
				{
					num2 += list2[j];
				}
				list2.AddRange(SoftBasic.BuildAsciiBytesFrom((byte)num2));
			}
			return list2.ToArray();
		}

		/// <summary>
		/// Extract actual data form plc response
		/// </summary>
		/// <param name="response">response data</param>
		/// <param name="isRead">read</param>
		/// <returns>result</returns>
		public static OperateResult<byte[]> ExtractActualData(byte[] response, bool isRead)
		{
			try
			{
				if (isRead)
				{
					if (response[0] == 6)
					{
						byte[] array = new byte[response.Length - 13];
						Array.Copy(response, 10, array, 0, array.Length);
						return OperateResult.CreateSuccessResult(SoftBasic.AsciiBytesToBytes(array));
					}
					byte[] array2 = new byte[response.Length - 9];
					Array.Copy(response, 6, array2, 0, array2.Length);
					return new OperateResult<byte[]>(BitConverter.ToUInt16(SoftBasic.AsciiBytesToBytes(array2), 0), "Data:" + SoftBasic.ByteToHexString(response));
				}
				if (response[0] == 6)
				{
					return OperateResult.CreateSuccessResult(new byte[0]);
				}
				byte[] array3 = new byte[response.Length - 9];
				Array.Copy(response, 6, array3, 0, array3.Length);
				return new OperateResult<byte[]>(BitConverter.ToUInt16(SoftBasic.AsciiBytesToBytes(array3), 0), "Data:" + SoftBasic.ByteToHexString(response));
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"XGKCnet[{base.PortName}:{base.BaudRate}]";
		}
	}
}

using System;

namespace Apps.Communication.Core.Address
{
	/// <summary>
	/// 西门子的地址数据信息，主要包含数据代码，DB块，偏移地址（偏移地址对于不是CT类型而已，是位为单位的），当处于写入时，Length无效<br />
	/// Address data information of Siemens, mainly including data code, DB block, offset address, when writing, Length is invalid
	/// </summary>
	public class S7AddressData : DeviceAddressDataBase
	{
		/// <summary>
		/// 获取或设置等待读取的数据的代码<br />
		/// Get or set the code of the data waiting to be read
		/// </summary>
		public byte DataCode { get; set; }

		/// <summary>
		/// 获取或设置PLC的DB块数据信息<br />
		/// Get or set PLC DB data information
		/// </summary>
		public ushort DbBlock { get; set; }

		/// <summary>
		/// 从指定的地址信息解析成真正的设备地址信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		public override void Parse(string address, ushort length)
		{
			OperateResult<S7AddressData> operateResult = ParseFrom(address, length);
			if (operateResult.IsSuccess)
			{
				base.AddressStart = operateResult.Content.AddressStart;
				base.Length = operateResult.Content.Length;
				DataCode = operateResult.Content.DataCode;
				DbBlock = operateResult.Content.DbBlock;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			if (DataCode == 31)
			{
				return "T" + base.AddressStart;
			}
			if (DataCode == 30)
			{
				return "C" + base.AddressStart;
			}
			if (DataCode == 6)
			{
				return "AI" + GetActualStringAddress(base.AddressStart);
			}
			if (DataCode == 7)
			{
				return "AQ" + GetActualStringAddress(base.AddressStart);
			}
			if (DataCode == 129)
			{
				return "I" + GetActualStringAddress(base.AddressStart);
			}
			if (DataCode == 130)
			{
				return "Q" + GetActualStringAddress(base.AddressStart);
			}
			if (DataCode == 131)
			{
				return "M" + GetActualStringAddress(base.AddressStart);
			}
			if (DataCode == 132)
			{
				return "DB" + DbBlock + "." + GetActualStringAddress(base.AddressStart);
			}
			return base.AddressStart.ToString();
		}

		private static string GetActualStringAddress(int addressStart)
		{
			if (addressStart % 8 == 0)
			{
				return (addressStart / 8).ToString();
			}
			return $"{addressStart / 8}.{addressStart % 8}";
		}

		/// <summary>
		/// 计算特殊的地址信息<br />
		/// Calculate Special Address information
		/// </summary>
		/// <param name="address">字符串地址 -&gt; String address</param>
		/// <param name="isCT">是否是定时器和计数器的地址</param>
		/// <returns>实际值 -&gt; Actual value</returns>
		public static int CalculateAddressStarted(string address, bool isCT = false)
		{
			if (address.IndexOf('.') < 0)
			{
				if (isCT)
				{
					return Convert.ToInt32(address);
				}
				return Convert.ToInt32(address) * 8;
			}
			string[] array = address.Split('.');
			return Convert.ToInt32(array[0]) * 8 + Convert.ToInt32(array[1]);
		}

		/// <summary>
		/// 从实际的西门子的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Siemens address
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<S7AddressData> ParseFrom(string address)
		{
			return ParseFrom(address, 0);
		}

		/// <summary>
		/// 从实际的西门子的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Siemens address
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<S7AddressData> ParseFrom(string address, ushort length)
		{
			S7AddressData s7AddressData = new S7AddressData();
			try
			{
				s7AddressData.Length = length;
				s7AddressData.DbBlock = 0;
				if (address.StartsWith("AI") || address.StartsWith("ai"))
				{
					s7AddressData.DataCode = 6;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(2));
				}
				else if (address.StartsWith("AQ") || address.StartsWith("aq"))
				{
					s7AddressData.DataCode = 7;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(2));
				}
				else if (address[0] == 'I')
				{
					s7AddressData.DataCode = 129;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'Q')
				{
					s7AddressData.DataCode = 130;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'M')
				{
					s7AddressData.DataCode = 131;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1));
				}
				else if (address[0] == 'D' || address.Substring(0, 2) == "DB")
				{
					s7AddressData.DataCode = 132;
					string[] array = address.Split('.');
					if (address[1] == 'B')
					{
						s7AddressData.DbBlock = Convert.ToUInt16(array[0].Substring(2));
					}
					else
					{
						s7AddressData.DbBlock = Convert.ToUInt16(array[0].Substring(1));
					}
					string text = address.Substring(address.IndexOf('.') + 1);
					if (text.StartsWith("DBX") || text.StartsWith("DBB") || text.StartsWith("DBW") || text.StartsWith("DBD"))
					{
						text = text.Substring(3);
					}
					s7AddressData.AddressStart = CalculateAddressStarted(text);
				}
				else if (address[0] == 'T')
				{
					s7AddressData.DataCode = 31;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1), isCT: true);
				}
				else if (address[0] == 'C')
				{
					s7AddressData.DataCode = 30;
					s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1), isCT: true);
				}
				else
				{
					if (address[0] != 'V')
					{
						return new OperateResult<S7AddressData>(StringResources.Language.NotSupportedDataType);
					}
					s7AddressData.DataCode = 132;
					s7AddressData.DbBlock = 1;
					if (address.StartsWith("VB") || address.StartsWith("VW") || address.StartsWith("VD") || address.StartsWith("VX"))
					{
						s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(2));
					}
					else
					{
						s7AddressData.AddressStart = CalculateAddressStarted(address.Substring(1));
					}
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<S7AddressData>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(s7AddressData);
		}
	}
}

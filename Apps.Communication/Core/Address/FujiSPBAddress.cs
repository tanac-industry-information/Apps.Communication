using System;
using Apps.Communication.Profinet.Fuji;

namespace Apps.Communication.Core.Address
{
	/// <summary>
	/// FujiSPB的地址信息，可以携带数据类型，起始地址操作
	/// </summary>
	public class FujiSPBAddress : DeviceAddressDataBase
	{
		/// <summary>
		/// 数据的类型代码
		/// </summary>
		public string TypeCode { get; set; }

		/// <summary>
		/// 当是位地址的时候，用于标记的信息
		/// </summary>
		public int BitIndex { get; set; }

		/// <summary>
		/// 获取读写字数据的时候的地址信息内容
		/// </summary>
		/// <returns>报文信息</returns>
		public string GetWordAddress()
		{
			return TypeCode + FujiSPBHelper.AnalysisIntegerAddress(base.AddressStart);
		}

		/// <summary>
		/// 获取命令，写入字地址的某一位的命令内容
		/// </summary>
		/// <returns>报文信息</returns>
		public string GetWriteBoolAddress()
		{
			int num = base.AddressStart * 2;
			int num2 = BitIndex;
			if (num2 >= 8)
			{
				num++;
				num2 -= 8;
			}
			return $"{TypeCode}{FujiSPBHelper.AnalysisIntegerAddress(num)}{num2:X2}";
		}

		/// <summary>
		/// 按照位为单位获取相关的索引信息
		/// </summary>
		/// <returns>位数据信息</returns>
		public int GetBitIndex()
		{
			return base.AddressStart * 16 + BitIndex;
		}

		/// <summary>
		/// 从实际的Fuji的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Fuji address
		/// </summary>
		/// <param name="address">富士的地址数据信息</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<FujiSPBAddress> ParseFrom(string address)
		{
			return ParseFrom(address, 0);
		}

		/// <summary>
		/// 从实际的Fuji的地址里面解析出地址对象<br />
		/// Resolve the address object from the actual Fuji address
		/// </summary>
		/// <param name="address">富士的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<FujiSPBAddress> ParseFrom(string address, ushort length)
		{
			FujiSPBAddress fujiSPBAddress = new FujiSPBAddress();
			fujiSPBAddress.Length = length;
			try
			{
				fujiSPBAddress.BitIndex = HslHelper.GetBitIndexInformation(ref address);
				switch (address[0])
				{
				case 'X':
				case 'x':
					fujiSPBAddress.TypeCode = "01";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'Y':
				case 'y':
					fujiSPBAddress.TypeCode = "00";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'M':
				case 'm':
					fujiSPBAddress.TypeCode = "02";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'L':
				case 'l':
					fujiSPBAddress.TypeCode = "03";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'T':
				case 't':
					if (address[1] == 'N' || address[1] == 'n')
					{
						fujiSPBAddress.TypeCode = "0A";
						fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(2), 10);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						fujiSPBAddress.TypeCode = "04";
						fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(2), 10);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'C':
				case 'c':
					if (address[1] == 'N' || address[1] == 'n')
					{
						fujiSPBAddress.TypeCode = "0B";
						fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(2), 10);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						fujiSPBAddress.TypeCode = "05";
						fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(2), 10);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'D':
				case 'd':
					fujiSPBAddress.TypeCode = "0C";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'R':
				case 'r':
					fujiSPBAddress.TypeCode = "0D";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				case 'W':
				case 'w':
					fujiSPBAddress.TypeCode = "0E";
					fujiSPBAddress.AddressStart = Convert.ToUInt16(address.Substring(1), 10);
					break;
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<FujiSPBAddress>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(fujiSPBAddress);
		}
	}
}

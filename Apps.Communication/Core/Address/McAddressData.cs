using System;
using Apps.Communication.Profinet.Melsec;
using Apps.Communication.Profinet.Panasonic;

namespace Apps.Communication.Core.Address
{
	/// <summary>
	/// 三菱的数据地址表示形式<br />
	/// Mitsubishi's data address representation
	/// </summary>
	public class McAddressData : DeviceAddressDataBase
	{
		/// <summary>
		/// 三菱的数据类型及地址信息
		/// </summary>
		public MelsecMcDataType McDataType { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public McAddressData()
		{
			McDataType = MelsecMcDataType.D;
		}

		/// <summary>
		/// 从指定的地址信息解析成真正的设备地址信息，默认是三菱的地址
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		public override void Parse(string address, ushort length)
		{
			OperateResult<McAddressData> operateResult = ParseMelsecFrom(address, length);
			if (operateResult.IsSuccess)
			{
				base.AddressStart = operateResult.Content.AddressStart;
				base.Length = operateResult.Content.Length;
				McDataType = operateResult.Content.McDataType;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return McDataType.AsciiCode.Replace("*", "") + Convert.ToString(base.AddressStart, McDataType.FromBase);
		}

		/// <summary>
		/// 从实际三菱的地址里面解析出我们需要的地址类型<br />
		/// Resolve the type of address we need from the actual Mitsubishi address
		/// </summary>
		/// <param name="address">三菱的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<McAddressData> ParseMelsecFrom(string address, ushort length)
		{
			McAddressData mcAddressData = new McAddressData();
			mcAddressData.Length = length;
			try
			{
				switch (address[0])
				{
				case 'M':
				case 'm':
					mcAddressData.McDataType = MelsecMcDataType.M;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.M.FromBase);
					break;
				case 'X':
				case 'x':
					mcAddressData.McDataType = MelsecMcDataType.X;
					address = address.Substring(1);
					if (address.StartsWith("0"))
					{
						mcAddressData.AddressStart = Convert.ToInt32(address, 8);
					}
					else
					{
						mcAddressData.AddressStart = Convert.ToInt32(address, MelsecMcDataType.X.FromBase);
					}
					break;
				case 'Y':
				case 'y':
					mcAddressData.McDataType = MelsecMcDataType.Y;
					address = address.Substring(1);
					if (address.StartsWith("0"))
					{
						mcAddressData.AddressStart = Convert.ToInt32(address, 8);
					}
					else
					{
						mcAddressData.AddressStart = Convert.ToInt32(address, MelsecMcDataType.Y.FromBase);
					}
					break;
				case 'D':
				case 'd':
					if (address[1] == 'X' || address[1] == 'x')
					{
						mcAddressData.McDataType = MelsecMcDataType.DX;
						address = address.Substring(2);
						if (address.StartsWith("0"))
						{
							mcAddressData.AddressStart = Convert.ToInt32(address, 8);
						}
						else
						{
							mcAddressData.AddressStart = Convert.ToInt32(address, MelsecMcDataType.DX.FromBase);
						}
					}
					else if (address[1] == 'Y' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.DY;
						address = address.Substring(2);
						if (address.StartsWith("0"))
						{
							mcAddressData.AddressStart = Convert.ToInt32(address, 8);
						}
						else
						{
							mcAddressData.AddressStart = Convert.ToInt32(address, MelsecMcDataType.DY.FromBase);
						}
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.D;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.D.FromBase);
					}
					break;
				case 'W':
				case 'w':
					mcAddressData.McDataType = MelsecMcDataType.W;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.W.FromBase);
					break;
				case 'L':
				case 'l':
					mcAddressData.McDataType = MelsecMcDataType.L;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.L.FromBase);
					break;
				case 'F':
				case 'f':
					mcAddressData.McDataType = MelsecMcDataType.F;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.F.FromBase);
					break;
				case 'V':
				case 'v':
					mcAddressData.McDataType = MelsecMcDataType.V;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.V.FromBase);
					break;
				case 'B':
				case 'b':
					mcAddressData.McDataType = MelsecMcDataType.B;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.B.FromBase);
					break;
				case 'R':
				case 'r':
					mcAddressData.McDataType = MelsecMcDataType.R;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.R.FromBase);
					break;
				case 'S':
				case 's':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.SN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SN.FromBase);
					}
					else if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.SS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SS.FromBase);
					}
					else if (address[1] == 'C' || address[1] == 'c')
					{
						mcAddressData.McDataType = MelsecMcDataType.SC;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SC.FromBase);
					}
					else if (address[1] == 'M' || address[1] == 'm')
					{
						mcAddressData.McDataType = MelsecMcDataType.SM;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SM.FromBase);
					}
					else if (address[1] == 'D' || address[1] == 'd')
					{
						mcAddressData.McDataType = MelsecMcDataType.SD;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SD.FromBase);
					}
					else if (address[1] == 'B' || address[1] == 'b')
					{
						mcAddressData.McDataType = MelsecMcDataType.SB;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SB.FromBase);
					}
					else if (address[1] == 'W' || address[1] == 'w')
					{
						mcAddressData.McDataType = MelsecMcDataType.SW;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.SW.FromBase);
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.S;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.S.FromBase);
					}
					break;
				case 'Z':
				case 'z':
					if (address.StartsWith("ZR") || address.StartsWith("zr"))
					{
						mcAddressData.McDataType = MelsecMcDataType.ZR;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.ZR.FromBase);
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.Z;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Z.FromBase);
					}
					break;
				case 'T':
				case 't':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.TN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.TN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.TS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.TS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						mcAddressData.McDataType = MelsecMcDataType.TC;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.TC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'C':
				case 'c':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.CN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.CN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.CS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.CS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						mcAddressData.McDataType = MelsecMcDataType.CC;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.CC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<McAddressData>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(mcAddressData);
		}

		/// <inheritdoc cref="M:Communication.Core.Address.McAddressData.ParseMelsecFrom(System.String,System.UInt16)" />
		public static OperateResult<McAddressData> ParseMelsecRFrom(string address, ushort length)
		{
			OperateResult<MelsecMcDataType, int> operateResult = MelsecMcRNet.AnalysisAddress(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<McAddressData>(operateResult);
			}
			return OperateResult.CreateSuccessResult(new McAddressData
			{
				McDataType = operateResult.Content1,
				AddressStart = operateResult.Content2,
				Length = length
			});
		}

		/// <summary>
		/// 从实际基恩士的地址里面解析出我们需要的地址信息<br />
		/// Resolve the address information we need from the actual Keyence address
		/// </summary>
		/// <param name="address">基恩士的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<McAddressData> ParseKeyenceFrom(string address, ushort length)
		{
			McAddressData mcAddressData = new McAddressData();
			mcAddressData.Length = length;
			try
			{
				switch (address[0])
				{
				case 'M':
				case 'm':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_M;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_M.FromBase);
					break;
				case 'X':
				case 'x':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_X;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_X.FromBase);
					break;
				case 'Y':
				case 'y':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_Y;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_Y.FromBase);
					break;
				case 'B':
				case 'b':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_B;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_B.FromBase);
					break;
				case 'L':
				case 'l':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_L;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_L.FromBase);
					break;
				case 'S':
				case 's':
					if (address[1] == 'M' || address[1] == 'm')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_SM;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_SM.FromBase);
						break;
					}
					if (address[1] == 'D' || address[1] == 'd')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_SD;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_SD.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'D':
				case 'd':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_D;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_D.FromBase);
					break;
				case 'R':
				case 'r':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_R;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_R.FromBase);
					break;
				case 'Z':
				case 'z':
					if (address[1] == 'R' || address[1] == 'r')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_ZR;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_ZR.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'W':
				case 'w':
					mcAddressData.McDataType = MelsecMcDataType.Keyence_W;
					mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1), MelsecMcDataType.Keyence_W.FromBase);
					break;
				case 'T':
				case 't':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_TN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_TN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_TS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_TS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_TC;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_TC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'C':
				case 'c':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_CN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_CN.FromBase);
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_CS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_CS.FromBase);
						break;
					}
					if (address[1] == 'C' || address[1] == 'c')
					{
						mcAddressData.McDataType = MelsecMcDataType.Keyence_CC;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2), MelsecMcDataType.Keyence_CC.FromBase);
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<McAddressData>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(mcAddressData);
		}

		/// <summary>
		/// 从实际松下的地址里面解析出
		/// </summary>
		/// <param name="address">松下的地址数据信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>是否成功的结果对象</returns>
		public static OperateResult<McAddressData> ParsePanasonicFrom(string address, ushort length)
		{
			McAddressData mcAddressData = new McAddressData();
			mcAddressData.Length = length;
			try
			{
				switch (address[0])
				{
				case 'R':
				case 'r':
				{
					int num = PanasonicHelper.CalculateComplexAddress(address.Substring(1));
					if (num < 14400)
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_R;
						mcAddressData.AddressStart = num;
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_SM;
						mcAddressData.AddressStart = num - 14400;
					}
					break;
				}
				case 'X':
				case 'x':
					mcAddressData.McDataType = MelsecMcDataType.Panasonic_X;
					mcAddressData.AddressStart = PanasonicHelper.CalculateComplexAddress(address.Substring(1));
					break;
				case 'Y':
				case 'y':
					mcAddressData.McDataType = MelsecMcDataType.Panasonic_Y;
					mcAddressData.AddressStart = PanasonicHelper.CalculateComplexAddress(address.Substring(1));
					break;
				case 'L':
				case 'l':
					if (address[1] == 'D' || address[1] == 'd')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_LD;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_L;
						mcAddressData.AddressStart = PanasonicHelper.CalculateComplexAddress(address.Substring(1));
					}
					break;
				case 'D':
				case 'd':
				{
					int num2 = Convert.ToInt32(address.Substring(1));
					if (num2 < 90000)
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_DT;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1));
					}
					else
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_SD;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(1)) - 90000;
					}
					break;
				}
				case 'T':
				case 't':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_TN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_TS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'C':
				case 'c':
					if (address[1] == 'N' || address[1] == 'n')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_CN;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
						break;
					}
					if (address[1] == 'S' || address[1] == 's')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_CS;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				case 'S':
				case 's':
					if (address[1] == 'D' || address[1] == 'd')
					{
						mcAddressData.McDataType = MelsecMcDataType.Panasonic_SD;
						mcAddressData.AddressStart = Convert.ToInt32(address.Substring(2));
						break;
					}
					throw new Exception(StringResources.Language.NotSupportedDataType);
				default:
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<McAddressData>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(mcAddressData);
		}
	}
}

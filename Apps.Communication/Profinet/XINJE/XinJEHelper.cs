using System;
using Apps.Communication.Core;

namespace Apps.Communication.Profinet.XINJE
{
	/// <summary>
	/// 信捷PLC的相关辅助类
	/// </summary>
	public class XinJEHelper
	{
		private static int CalculateXinJEStartAddress(string address)
		{
			if (address.IndexOf('.') < 0)
			{
				return Convert.ToInt32(address, 8);
			}
			string[] array = address.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			return Convert.ToInt32(array[0], 8) * 8 + int.Parse(array[1]);
		}

		/// <summary>
		/// 根据信捷PLC的地址，解析出转换后的modbus协议信息
		/// </summary>
		/// <param name="series">PLC的系列信息</param>
		/// <param name="address">汇川plc的地址信息</param>
		/// <param name="modbusCode">原始的对应的modbus信息</param>
		/// <returns>还原后的modbus地址</returns>
		public static OperateResult<string> PraseXinJEAddress(XinJESeries series, string address, byte modbusCode)
		{
			if (series == XinJESeries.XC)
			{
				return PraseXinJEXCAddress(address, modbusCode);
			}
			return PraseXinJEXD1XD2XD3XL1XL3Address(address, modbusCode);
		}

		/// <summary>
		/// 根据信捷PLC的地址，解析出转换后的modbus协议信息，适用XC系列
		/// </summary>
		/// <param name="address">信捷plc的地址信息</param>
		/// <param name="modbusCode">原始的对应的modbus信息</param>
		/// <returns>还原后的modbus地址</returns>
		public static OperateResult<string> PraseXinJEXCAddress(string address, byte modbusCode)
		{
			try
			{
				string text = string.Empty;
				OperateResult<int> operateResult = HslHelper.ExtractParameter(ref address, "s");
				if (operateResult.IsSuccess)
				{
					text = $"s={operateResult.Content};";
				}
				if (modbusCode == 1 || modbusCode == 15 || modbusCode == 5)
				{
					if (address.StartsWith("X") || address.StartsWith("x"))
					{
						return OperateResult.CreateSuccessResult(text + (CalculateXinJEStartAddress(address.Substring(1)) + 16384));
					}
					if (address.StartsWith("Y") || address.StartsWith("y"))
					{
						return OperateResult.CreateSuccessResult(text + (CalculateXinJEStartAddress(address.Substring(1)) + 18432));
					}
					if (address.StartsWith("S") || address.StartsWith("s"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 20480));
					}
					if (address.StartsWith("T") || address.StartsWith("t"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 25600));
					}
					if (address.StartsWith("C") || address.StartsWith("c"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 27648));
					}
					if (address.StartsWith("M") || address.StartsWith("m"))
					{
						int num = Convert.ToInt32(address.Substring(1));
						if (num >= 8000)
						{
							return OperateResult.CreateSuccessResult(text + (num - 8000 + 24576));
						}
						return OperateResult.CreateSuccessResult(text + num);
					}
				}
				else
				{
					if (address.StartsWith("D") || address.StartsWith("d"))
					{
						int num2 = Convert.ToInt32(address.Substring(1));
						if (num2 >= 8000)
						{
							return OperateResult.CreateSuccessResult(text + (num2 - 8000 + 16384));
						}
						return OperateResult.CreateSuccessResult(text + num2);
					}
					if (address.StartsWith("F") || address.StartsWith("f"))
					{
						int num3 = Convert.ToInt32(address.Substring(1));
						if (num3 >= 8000)
						{
							return OperateResult.CreateSuccessResult(text + (num3 - 8000 + 26624));
						}
						return OperateResult.CreateSuccessResult(text + (num3 + 18432));
					}
					if (address.StartsWith("E") || address.StartsWith("e"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 28672));
					}
					if (address.StartsWith("T") || address.StartsWith("t"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 12288));
					}
					if (address.StartsWith("C") || address.StartsWith("c"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 14336));
					}
				}
				return new OperateResult<string>(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}
		}

		/// <summary>
		/// 解析信捷的XD1,XD2,XD3,XL1,XL3系列的PLC的Modbus地址和内部软元件的对照
		/// </summary>
		/// <remarks>适用 XD1、XD2、XD3、XL1、XL3、XD5、XDM、XDC、XD5E、XDME、XL5、XL5E、XLME, XDH 只是支持的地址范围不一样而已</remarks>
		/// <param name="address">PLC内部的软元件的地址</param>
		/// <param name="modbusCode">默认的Modbus功能码</param>
		/// <returns>解析后的Modbus地址</returns>
		public static OperateResult<string> PraseXinJEXD1XD2XD3XL1XL3Address(string address, byte modbusCode)
		{
			try
			{
				string text = string.Empty;
				OperateResult<int> operateResult = HslHelper.ExtractParameter(ref address, "s");
				if (operateResult.IsSuccess)
				{
					text = $"s={operateResult.Content};";
				}
				if (modbusCode == 1 || modbusCode == 15 || modbusCode == 5)
				{
					if (address.StartsWith("X") || address.StartsWith("x"))
					{
						int num = CalculateXinJEStartAddress(address.Substring(1));
						if (num < 4096)
						{
							return OperateResult.CreateSuccessResult(text + (num + 20480));
						}
						if (num < 8192)
						{
							return OperateResult.CreateSuccessResult(text + (num - 4096 + 20736));
						}
						if (num < 12288)
						{
							return OperateResult.CreateSuccessResult(text + (num - 8192 + 22736));
						}
						return OperateResult.CreateSuccessResult(text + (num - 12288 + 23536));
					}
					if (address.StartsWith("Y") || address.StartsWith("y"))
					{
						int num2 = CalculateXinJEStartAddress(address.Substring(1));
						if (num2 < 4096)
						{
							return OperateResult.CreateSuccessResult(text + (num2 + 24576));
						}
						if (num2 < 8192)
						{
							return OperateResult.CreateSuccessResult(text + (num2 - 4096 + 24832));
						}
						if (num2 < 12288)
						{
							return OperateResult.CreateSuccessResult(text + (num2 - 8192 + 26832));
						}
						return OperateResult.CreateSuccessResult(text + (num2 - 12288 + 27632));
					}
					if (address.StartsWith("SEM") || address.StartsWith("sem"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 49280));
					}
					if (address.StartsWith("HSC") || address.StartsWith("hsc"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 59648));
					}
					if (address.StartsWith("SM") || address.StartsWith("sm"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 36864));
					}
					if (address.StartsWith("ET") || address.StartsWith("et"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 49152));
					}
					if (address.StartsWith("HM") || address.StartsWith("hm"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 49408));
					}
					if (address.StartsWith("HS") || address.StartsWith("hs"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 55552));
					}
					if (address.StartsWith("HT") || address.StartsWith("ht"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 57600));
					}
					if (address.StartsWith("HC") || address.StartsWith("hc"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 58624));
					}
					if (address.StartsWith("S") || address.StartsWith("s"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 28672));
					}
					if (address.StartsWith("T") || address.StartsWith("t"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 40960));
					}
					if (address.StartsWith("C") || address.StartsWith("c"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(1)) + 45056));
					}
					if (address.StartsWith("M") || address.StartsWith("m"))
					{
						return OperateResult.CreateSuccessResult(text + Convert.ToInt32(address.Substring(1)));
					}
				}
				else
				{
					if (address.StartsWith("ID") || address.StartsWith("id"))
					{
						int num3 = Convert.ToInt32(address.Substring(2));
						if (num3 < 10000)
						{
							return OperateResult.CreateSuccessResult(text + (num3 + 20480));
						}
						if (num3 < 20000)
						{
							return OperateResult.CreateSuccessResult(text + (num3 - 10000 + 20736));
						}
						if (num3 < 30000)
						{
							return OperateResult.CreateSuccessResult(text + (num3 - 20000 + 22736));
						}
						return OperateResult.CreateSuccessResult(text + (num3 - 30000 + 23536));
					}
					if (address.StartsWith("QD") || address.StartsWith("qd"))
					{
						int num4 = Convert.ToInt32(address.Substring(2));
						if (num4 < 10000)
						{
							return OperateResult.CreateSuccessResult(text + (num4 + 24576));
						}
						if (num4 < 20000)
						{
							return OperateResult.CreateSuccessResult(text + (num4 - 10000 + 24832));
						}
						if (num4 < 30000)
						{
							return OperateResult.CreateSuccessResult(text + (num4 - 20000 + 26832));
						}
						return OperateResult.CreateSuccessResult(text + (num4 - 30000 + 27632));
					}
					if (address.StartsWith("HSCD") || address.StartsWith("hscd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(4)) + 50304));
					}
					if (address.StartsWith("ETD") || address.StartsWith("etd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 40960));
					}
					if (address.StartsWith("HSD") || address.StartsWith("hsd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 47232));
					}
					if (address.StartsWith("HTD") || address.StartsWith("htd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 48256));
					}
					if (address.StartsWith("HCD") || address.StartsWith("hcd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 49280));
					}
					if (address.StartsWith("SFD") || address.StartsWith("sfd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(3)) + 58560));
					}
					if (address.StartsWith("SD") || address.StartsWith("sd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 28672));
					}
					if (address.StartsWith("TD") || address.StartsWith("td"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 32768));
					}
					if (address.StartsWith("CD") || address.StartsWith("cd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 36864));
					}
					if (address.StartsWith("HD") || address.StartsWith("hd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 41088));
					}
					if (address.StartsWith("FD") || address.StartsWith("fd"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 50368));
					}
					if (address.StartsWith("FS") || address.StartsWith("fs"))
					{
						return OperateResult.CreateSuccessResult(text + (Convert.ToInt32(address.Substring(2)) + 62656));
					}
					if (address.StartsWith("D") || address.StartsWith("d"))
					{
						return OperateResult.CreateSuccessResult(text + Convert.ToInt32(address.Substring(1)));
					}
				}
				return new OperateResult<string>(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message);
			}
		}
	}
}

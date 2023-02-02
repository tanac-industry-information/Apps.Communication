using System;
using System.Linq;
using System.Text;

namespace Apps.Communication.Instrument.DLT
{
	/// <summary>
	/// DTL数据转换
	/// </summary>
	public class DLTTransform
	{
		/// <summary>
		/// Byte[]转ToHexString
		/// </summary>
		/// <param name="content">原始的字节内容</param>
		/// <param name="length">长度信息</param>
		/// <returns></returns>
		public static OperateResult<string> TransStringFromDLt(byte[] content, ushort length)
		{
			try
			{
				string empty = string.Empty;
				byte[] array = content.SelectBegin(length).Reverse().ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (byte)(array[i] - 51);
				}
				return OperateResult.CreateSuccessResult(Encoding.ASCII.GetString(array));
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message + " Reason: " + content.ToHexString(' '));
			}
		}

		/// <summary>
		/// Byte[]转Dlt double[]
		/// </summary>
		/// <param name="content">原始的字节数据</param>
		/// <param name="length">需要转换的数据长度</param>
		/// <param name="format">当前数据的解析格式</param>
		/// <returns>结果内容</returns>
		public static OperateResult<double[]> TransDoubleFromDLt(byte[] content, ushort length, string format = "XXXXXX.XX")
		{
			try
			{
				format = format.ToUpper();
				int num = format.Count((char m) => m == 'X') / 2;
				int num2 = ((format.IndexOf('.') >= 0) ? (format.Length - format.IndexOf('.') - 1) : 0);
				double[] array = new double[length];
				for (int i = 0; i < array.Length; i++)
				{
					byte[] array2 = content.SelectMiddle(i * num, num).Reverse().ToArray();
					for (int j = 0; j < array2.Length; j++)
					{
						array2[j] = (byte)(array2[j] - 51);
					}
					array[i] = Convert.ToDouble(array2.ToHexString()) / Math.Pow(10.0, num2);
				}
				return OperateResult.CreateSuccessResult(array);
			}
			catch (Exception ex)
			{
				return new OperateResult<double[]>(ex.Message);
			}
		}
	}
}

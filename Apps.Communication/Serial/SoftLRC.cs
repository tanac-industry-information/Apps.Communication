using System;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Serial
{
	/// <summary>
	/// 用于LRC验证的类，提供了标准的验证方法<br />
	/// The class used for LRC verification provides a standard verification method
	/// </summary>
	public class SoftLRC
	{
		/// <summary>
		/// 获取对应的数据的LRC校验码<br />
		/// Class for LRC validation that provides a standard validation method
		/// </summary>
		/// <param name="value">需要校验的数据，不包含LRC字节</param>
		/// <returns>返回带LRC校验码的字节数组，可用于串口发送</returns>
		public static byte[] LRC(byte[] value)
		{
			if (value == null)
			{
				return null;
			}
			int num = 0;
			for (int i = 0; i < value.Length; i++)
			{
				num += value[i];
			}
			num %= 256;
			num = 256 - num;
			byte[] array = new byte[1] { (byte)num };
			return SoftBasic.SpliceArray<byte>(value, array);
		}

		/// <summary>
		/// 检查数据是否符合LRC的验证<br />
		/// Check data for compliance with LRC validation
		/// </summary>
		/// <param name="value">等待校验的数据，是否正确</param>
		/// <returns>是否校验成功</returns>
		public static bool CheckLRC(byte[] value)
		{
			if (value == null)
			{
				return false;
			}
			int num = value.Length;
			byte[] array = new byte[num - 1];
			Array.Copy(value, 0, array, 0, array.Length);
			byte[] array2 = LRC(array);
			if (array2[num - 1] == value[num - 1])
			{
				return true;
			}
			return false;
		}
	}
}

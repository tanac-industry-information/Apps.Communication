using System;
using System.Text;

namespace Apps.Communication.Profinet.LSIS
{
	public class XGTAddressData
	{
		public string Address { get; set; }

		public string Data { get; set; }

		public byte[] DataByteArray { get; set; }

		/// <summary>
		/// 주소 문자열 표현, EX) %DW1100
		/// </summary>
		public string AddressString { get; set; }

		/// <summary>
		/// AddressString 을 바이트 배열로 변환
		/// </summary>
		public byte[] AddressByteArray => Encoding.ASCII.GetBytes(AddressString);

		/// <summary>
		/// AddressByteArray 바이트 배열의 수(2byte)
		/// </summary>
		public byte[] LengthByteArray => BitConverter.GetBytes((short)AddressByteArray.Length);
	}
}

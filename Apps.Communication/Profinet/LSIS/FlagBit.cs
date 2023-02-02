namespace Apps.Communication.Profinet.LSIS
{
	/// <summary>
	/// using FlagBit in Marker for Byte<br />
	/// M0.0=1;M0.1=2;M0.2=4;M0.3=8;==========================&gt;M0.7=128
	/// </summary>
	public enum FlagBit
	{
		Flag1 = 1,
		Flag2 = 2,
		Flag4 = 4,
		Flag8 = 8,
		Flag16 = 0x10,
		Flag32 = 0x20,
		Flag64 = 0x40,
		Flag128 = 0x80,
		Flag256 = 0x100
	}
}

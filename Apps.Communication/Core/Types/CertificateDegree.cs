namespace Apps.Communication.Core.Types
{
	/// <summary>
	/// 证书等级
	/// </summary>
	public enum CertificateDegree
	{
		/// <summary>
		/// 只允许读取数据的等级
		/// </summary>
		Read = 1,
		/// <summary>
		/// 允许同时读写数据的等级
		/// </summary>
		ReadWrite
	}
}

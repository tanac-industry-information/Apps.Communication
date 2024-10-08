namespace Apps.Communication.ModBus
{
	/// <summary>
	/// 监视使用的数据缓存
	/// </summary>
	internal struct MonitorAddress
	{
		/// <summary>
		/// 地址
		/// </summary>
		public ushort Address;

		/// <summary>
		/// 原有的值
		/// </summary>
		public short ValueOrigin;

		/// <summary>
		/// 新的值
		/// </summary>
		public short ValueNew;
	}
}

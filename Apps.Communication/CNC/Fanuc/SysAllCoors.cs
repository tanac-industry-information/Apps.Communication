namespace Apps.Communication.CNC.Fanuc
{
	/// <summary>
	/// 系统的坐标信息
	/// </summary>
	public class SysAllCoors
	{
		/// <summary>
		/// 绝对坐标
		/// </summary>
		public double[] Absolute { get; set; }

		/// <summary>
		/// 机械坐标
		/// </summary>
		public double[] Machine { get; set; }

		/// <summary>
		/// 相对坐标
		/// </summary>
		public double[] Relative { get; set; }
	}
}

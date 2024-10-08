namespace Apps.Communication.Profinet.Geniitek
{
	/// <summary>
	/// 振动传感器的加速度值
	/// </summary>
	public struct VibrationSensorActualValue
	{
		/// <summary>
		/// X轴的实时加速度
		/// </summary>
		public float AcceleratedSpeedX { get; set; }

		/// <summary>
		/// Y轴的实时加速度
		/// </summary>
		public float AcceleratedSpeedY { get; set; }

		/// <summary>
		/// Z轴的实时加速度
		/// </summary>
		public float AcceleratedSpeedZ { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ActualValue[{AcceleratedSpeedX},{AcceleratedSpeedY},{AcceleratedSpeedZ}]";
		}
	}
}

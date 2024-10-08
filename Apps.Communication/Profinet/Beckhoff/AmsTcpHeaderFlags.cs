namespace Apps.Communication.Profinet.Beckhoff
{
	/// <summary>
	/// AMS消息的命令号
	/// </summary>
	public enum AmsTcpHeaderFlags : ushort
	{
		/// <summary>
		/// AmsCommand (AMS_TCP_PORT_AMS_CMD, 0x0000)
		/// </summary>
		Command = 0,
		/// <summary>
		/// Port Close command (AMS_TCP_PORT_CLOSE, 0x0001)
		/// </summary>
		PortClose = 1,
		/// <summary>
		/// Port connect command (AMS_TCP_PORT_CONNECT, 0x1000)
		/// </summary>
		PortConnect = 0x1000,
		/// <summary>
		/// Router Notification (AMS_TCP_PORT_ROUTER_NOTE, 0x1001)
		/// </summary>
		RouterNotification = 4097,
		/// <summary>
		/// Get LocalNetId header
		/// </summary>
		GetLocalNetId = 4098
	}
}

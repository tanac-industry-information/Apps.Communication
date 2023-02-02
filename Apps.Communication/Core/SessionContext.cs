namespace Apps.Communication.Core
{
	/// <inheritdoc cref="T:Communication.Core.ISessionContext" />
	public class SessionContext : ISessionContext
	{
		/// <inheritdoc cref="P:Communication.Core.ISessionContext.UserName" />
		public string UserName { get; set; }

		/// <inheritdoc cref="P:Communication.Core.ISessionContext.ClientId" />
		public string ClientId { get; set; }
	}
}

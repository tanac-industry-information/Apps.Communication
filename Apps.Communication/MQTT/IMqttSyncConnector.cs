using System;
using Apps.Communication.Algorithms.ConnectPool;

namespace Apps.Communication.MQTT
{
	/// <summary>
	/// 关于MqttSyncClient实现的接口<see cref="T:Communication.Algorithms.ConnectPool.IConnector" />，从而实现了数据连接池的操作信息
	/// </summary>
	public class IMqttSyncConnector : IConnector
	{
		private MqttConnectionOptions connectionOptions;

		/// <inheritdoc cref="P:Communication.Algorithms.ConnectPool.IConnector.IsConnectUsing" />
		public bool IsConnectUsing { get; set; }

		/// <inheritdoc cref="P:Communication.Algorithms.ConnectPool.IConnector.GuidToken" />
		public string GuidToken { get; set; }

		/// <inheritdoc cref="P:Communication.Algorithms.ConnectPool.IConnector.LastUseTime" />
		public DateTime LastUseTime { get; set; }

		/// <summary>
		/// MQTT的连接对象
		/// </summary>
		public MqttSyncClient SyncClient { get; set; }

		/// <summary>
		/// 根据连接的MQTT参数，实例化一个默认的对象<br />
		/// According to the connected MQTT parameters, instantiate a default object
		/// </summary>
		/// <param name="options">连接的参数信息</param>
		public IMqttSyncConnector(MqttConnectionOptions options)
		{
			connectionOptions = options;
			SyncClient = new MqttSyncClient(options);
		}

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public IMqttSyncConnector()
		{
		}

		/// <inheritdoc cref="M:Communication.Algorithms.ConnectPool.IConnector.Close" />
		public void Close()
		{
			SyncClient?.ConnectClose();
		}

		/// <inheritdoc cref="M:Communication.Algorithms.ConnectPool.IConnector.Open" />
		public void Open()
		{
			SyncClient?.SetPersistentConnection();
		}
	}
}

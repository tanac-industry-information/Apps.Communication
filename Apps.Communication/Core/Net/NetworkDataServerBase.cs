using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Reflection;
using RJCP.IO.Ports;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 所有虚拟的数据服务器的基类，提供了基本的数据读写，存储加载的功能方法，具体的字节读写需要继承重写。<br />
	/// The base class of all virtual data servers provides basic methods for reading and writing data and storing and loading. 
	/// Specific byte reads and writes need to be inherited and override.
	/// </summary>
	public class NetworkDataServerBase : NetworkAuthenticationServerBase, IDisposable, IReadWriteNet
	{
		/// <summary>
		/// 当接收到来自客户的数据信息时触发的对象，该数据可能来自tcp或是串口<br />
		/// The object that is triggered when receiving data information from the customer, the data may come from tcp or serial port
		/// </summary>
		/// <param name="sender">触发的服务器对象</param>
		/// <param name="source">消息的来源对象</param>
		/// <param name="data">实际的数据信息</param>
		public delegate void DataReceivedDelegate(object sender, object source, byte[] data);

		/// <summary>
		/// 数据发送的时候委托<br />
		/// Show DataSend To PLC
		/// </summary>
		/// <param name="sender">数据发送对象</param>
		/// <param name="data">数据内容</param>
		public delegate void DataSendDelegate(object sender, byte[] data);

		/// <summary>
		/// 表示客户端状态变化的委托信息<br />
		/// Delegate information representing the state change of the client
		/// </summary>
		/// <param name="server">当前的服务器对象信息</param>
		/// <param name="session">当前的客户端会话信息</param>
		public delegate void OnClientStatusChangeDelegate(NetworkDataServerBase server, AppSession session);

		private List<string> TrustedClients = null;

		private bool IsTrustedClientsOnly = false;

		private SimpleHybirdLock lock_trusted_clients;

		private List<AppSession> listsOnlineClient;

		private object lockOnlineClient;

		private int onlineCount = 0;

		/// <summary>
		/// 接收一次的数据的最少时间，当重写了报文结束的检查代码时，可以适当的将本值设置的大一点。<br />
		/// The minimum time to receive the data once, when the check code of the end of the message is rewritten, this value can be appropriately set larger.
		/// </summary>
		protected int receiveAtleastTime = 20;

		private Timer timerHeart;

		private SerialPortStream serialPort;

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ByteTransform" />
		public IByteTransform ByteTransform { get; set; }

		/// <inheritdoc cref="P:Communication.Core.IReadWriteNet.ConnectionId" />
		public string ConnectionId { get; set; }

		/// <summary>
		/// 获取或设置当前的服务器是否允许远程客户端进行写入数据操作，默认为<c>True</c><br />
		/// Gets or sets whether the current server allows remote clients to write data, the default is <c>True</c>
		/// </summary>
		/// <remarks>
		/// 如果设置为<c>False</c>，那么所有远程客户端的操作都会失败，直接返回错误码或是关闭连接。
		/// </remarks>
		public bool EnableWrite { get; set; } = true;


		/// <summary>
		/// 获取或设置两次数据交互时的最小时间间隔，默认为24小时。<br />
		/// Get or set the minimum time interval between two data interactions, the default is 24 hours.
		/// </summary>
		public TimeSpan ActiveTimeSpan { get; set; }

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDeviceBase.WordLength" />
		protected ushort WordLength { get; set; } = 1;


		/// <summary>
		/// 获取在线的客户端的数量<br />
		/// Get the number of clients online
		/// </summary>
		public int OnlineCount => onlineCount;

		/// <summary>
		/// 获取当前所有在线的客户端信息，包括IP地址和端口号信息<br />
		/// Get all current online client information, including IP address and port number information
		/// </summary>
		public AppSession[] GetOnlineSessions
		{
			get
			{
				lock (lockOnlineClient)
				{
					return listsOnlineClient.ToArray();
				}
			}
		}

		/// <summary>
		/// 接收到数据的时候就触发的事件，示例详细参考API文档信息<br />
		/// An event that is triggered when data is received
		/// </summary>
		/// <remarks>
		/// 事件共有三个参数，sender指服务器本地的对象，例如 <see cref="T:Communication.ModBus.ModbusTcpServer" /> 对象，source 指会话对象，网口对象为 <see cref="T:Communication.Core.Net.AppSession" />，
		/// 串口为<see cref="T:System.IO.Ports.SerialPort" /> 对象，需要根据实际判断，data 为收到的原始数据 byte[] 对象
		/// </remarks>
		/// <example>
		/// 我们以Modbus的Server为例子，其他的虚拟服务器同理，因为都集成自本服务器对象
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDataServerBaseSample.cs" region="OnDataReceivedSample" title="数据接收触发的示例" />
		/// </example>
		public event DataReceivedDelegate OnDataReceived;

		/// <summary>
		/// 数据发送的时候就触发的事件<br />
		/// Events that are triggered when data is sent
		/// </summary>
		public event DataSendDelegate OnDataSend;

		/// <summary>
		/// 当客户端上线时候的触发的事件<br />
		/// Event triggered when the client goes online
		/// </summary>
		public event OnClientStatusChangeDelegate OnClientOnline;

		/// <summary>
		/// 当客户端下线时候的触发的事件<br />
		/// Event triggered when the client goes offline
		/// </summary>
		public event OnClientStatusChangeDelegate OnClientOffline;

		/// <summary>
		/// 实例化一个默认的数据服务器的对象<br />
		/// Instantiate an object of the default data server
		/// </summary>
		public NetworkDataServerBase()
		{
			ActiveTimeSpan = TimeSpan.FromHours(24.0);
			lock_trusted_clients = new SimpleHybirdLock();
			ConnectionId = SoftBasic.GetUniqueStringByGuidAndRandom();
			lockOnlineClient = new object();
			listsOnlineClient = new List<AppSession>();
			timerHeart = new Timer(ThreadTimerHeartCheck, null, 2000, 10000);
			serialPort = new SerialPortStream();
		}

		/// <summary>
		/// 将本系统的数据池数据存储到指定的文件<br />
		/// Store the data pool data of this system to the specified file
		/// </summary>
		/// <param name="path">指定文件的路径</param>
		/// <exception cref="T:System.ArgumentException"></exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.IO.PathTooLongException"></exception>
		/// <exception cref="T:System.IO.DirectoryNotFoundException"></exception>
		/// <exception cref="T:System.IO.IOException"></exception>
		/// <exception cref="T:System.UnauthorizedAccessException"></exception>
		/// <exception cref="T:System.NotSupportedException"></exception>
		/// <exception cref="T:System.Security.SecurityException"></exception>
		public void SaveDataPool(string path)
		{
			byte[] bytes = SaveToBytes();
			File.WriteAllBytes(path, bytes);
		}

		/// <summary>
		/// 从文件加载数据池信息<br />
		/// Load datapool information from a file
		/// </summary>
		/// <param name="path">文件路径</param>
		/// <exception cref="T:System.ArgumentException"></exception>
		/// <exception cref="T:System.ArgumentNullException"></exception>
		/// <exception cref="T:System.IO.PathTooLongException"></exception>
		/// <exception cref="T:System.IO.DirectoryNotFoundException"></exception>
		/// <exception cref="T:System.IO.IOException"></exception>
		/// <exception cref="T:System.UnauthorizedAccessException"></exception>
		/// <exception cref="T:System.NotSupportedException"></exception>
		/// <exception cref="T:System.Security.SecurityException"></exception>
		public void LoadDataPool(string path)
		{
			if (File.Exists(path))
			{
				byte[] content = File.ReadAllBytes(path);
				LoadFromBytes(content);
			}
		}

		/// <summary>
		/// 从字节数据加载数据信息，需要进行重写方法<br />
		/// Loading data information from byte data requires rewriting method
		/// </summary>
		/// <param name="content">字节数据</param>
		protected virtual void LoadFromBytes(byte[] content)
		{
		}

		/// <summary>
		/// 将数据信息存储到字节数组去，需要进行重写方法<br />
		/// To store data information into a byte array, a rewrite method is required
		/// </summary>
		/// <returns>所有的内容</returns>
		protected virtual byte[] SaveToBytes()
		{
			return new byte[0];
		}

		/// <summary>
		/// 触发一个数据接收的事件信息<br />
		/// Event information that triggers a data reception
		/// </summary>
		/// <param name="source">数据的发送方</param>
		/// <param name="receive">接收数据信息</param>
		protected void RaiseDataReceived(object source, byte[] receive)
		{
			this.OnDataReceived?.Invoke(this, source, receive);
		}

		/// <summary>
		/// 触发一个数据发送的事件信息<br />
		/// Event information that triggers a data transmission
		/// </summary>
		/// <param name="send">数据内容</param>
		protected void RaiseDataSend(byte[] send)
		{
			this.OnDataSend?.Invoke(this, send);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDeviceBase.GetWordLength(System.String,System.Int32,System.Int32)" />
		protected virtual ushort GetWordLength(string address, int length, int dataTypeLength)
		{
			if (WordLength == 0)
			{
				int num = length * dataTypeLength * 2 / 4;
				return (ushort)((num == 0) ? 1 : ((ushort)num));
			}
			return (ushort)(WordLength * length * dataTypeLength);
		}

		/// <summary>
		/// 当客户端登录后，在Ip信息的过滤后，然后触发本方法，进行后续的数据接收，处理，并返回相关的数据信息<br />
		/// When the client logs in, after filtering the IP information, this method is then triggered to perform subsequent data reception, 
		/// processing, and return related data information
		/// </summary>
		/// <param name="socket">网络套接字</param>
		/// <param name="endPoint">终端节点</param>
		protected virtual void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
		}

		/// <summary>
		/// 当接收到了新的请求的时候执行的操作，此处进行账户的安全验证<br />
		/// The operation performed when a new request is received, and the account security verification is performed here
		/// </summary>
		/// <param name="socket">异步对象</param>
		/// <param name="endPoint">终结点</param>
		protected override void ThreadPoolLogin(Socket socket, IPEndPoint endPoint)
		{
			string ipAddress = endPoint.Address.ToString();
			if (IsTrustedClientsOnly && !CheckIpAddressTrusted(ipAddress))
			{
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientDisableLogin, endPoint));
				socket.Close();
				return;
			}
			if (!base.IsUseAccountCertificate)
			{
				base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOnlineInfo, endPoint));
			}
			ThreadPoolLoginAfterClientCheck(socket, endPoint);
		}

		/// <summary>
		/// 设置并启动受信任的客户端登录并读写，如果为null，将关闭对客户端的ip验证<br />
		/// Set and start the trusted client login and read and write, if it is null, the client's IP verification will be turned off
		/// </summary>
		/// <param name="clients">受信任的客户端列表</param>
		public void SetTrustedIpAddress(List<string> clients)
		{
			lock_trusted_clients.Enter();
			if (clients != null)
			{
				TrustedClients = clients.Select(delegate(string m)
				{
					IPAddress iPAddress = IPAddress.Parse(m);
					return iPAddress.ToString();
				}).ToList();
				IsTrustedClientsOnly = true;
			}
			else
			{
				TrustedClients = new List<string>();
				IsTrustedClientsOnly = false;
			}
			lock_trusted_clients.Leave();
		}

		/// <summary>
		/// 检查该Ip地址是否是受信任的<br />
		/// Check if the IP address is trusted
		/// </summary>
		/// <param name="ipAddress">Ip地址信息</param>
		/// <returns>是受信任的返回<c>True</c>，否则返回<c>False</c></returns>
		private bool CheckIpAddressTrusted(string ipAddress)
		{
			if (IsTrustedClientsOnly)
			{
				bool result = false;
				lock_trusted_clients.Enter();
				for (int i = 0; i < TrustedClients.Count; i++)
				{
					if (TrustedClients[i] == ipAddress)
					{
						result = true;
						break;
					}
				}
				lock_trusted_clients.Leave();
				return result;
			}
			return false;
		}

		/// <summary>
		/// 获取受信任的客户端列表<br />
		/// Get a list of trusted clients
		/// </summary>
		/// <returns>字符串数据信息</returns>
		public string[] GetTrustedClients()
		{
			string[] result = new string[0];
			lock_trusted_clients.Enter();
			if (TrustedClients != null)
			{
				result = TrustedClients.ToArray();
			}
			lock_trusted_clients.Leave();
			return result;
		}

		/// <summary>
		/// 新增一个在线的客户端信息<br />
		/// Add an online client information
		/// </summary>
		/// <param name="session">会话内容</param>
		protected void AddClient(AppSession session)
		{
			lock (lockOnlineClient)
			{
				listsOnlineClient.Add(session);
				onlineCount++;
			}
			this.OnClientOnline?.Invoke(this, session);
		}

		/// <summary>
		/// 移除一个在线的客户端信息<br />
		/// Remove an online client message
		/// </summary>
		/// <param name="session">会话内容</param>
		/// <param name="reason">下线的原因</param>
		protected void RemoveClient(AppSession session, string reason = "")
		{
			bool flag = false;
			lock (lockOnlineClient)
			{
				if (listsOnlineClient.Remove(session))
				{
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, session.IpEndPoint) + " " + reason);
					session.WorkSocket?.Close();
					onlineCount--;
					flag = true;
				}
			}
			if (flag)
			{
				this.OnClientOffline?.Invoke(this, session);
			}
		}

		/// <summary>
		/// 关闭之后进行的操作
		/// </summary>
		protected override void CloseAction()
		{
			base.CloseAction();
			lock (lockOnlineClient)
			{
				for (int i = 0; i < listsOnlineClient.Count; i++)
				{
					listsOnlineClient[i]?.WorkSocket?.Close();
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, listsOnlineClient[i].IpEndPoint));
				}
				listsOnlineClient.Clear();
				onlineCount = 0;
			}
		}

		private void ThreadTimerHeartCheck(object obj)
		{
			AppSession[] array = null;
			lock (lockOnlineClient)
			{
				array = listsOnlineClient.ToArray();
			}
			if (array == null || array.Length == 0)
			{
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (DateTime.Now - array[i].HeartTime > ActiveTimeSpan)
				{
					RemoveClient(array[i]);
				}
			}
		}

		/// <summary>
		/// 启动串口的从机服务，使用默认的参数进行初始化串口，9600波特率，8位数据位，无奇偶校验，1位停止位<br />
		/// Start the slave service of serial, initialize the serial port with default parameters, 9600 baud rate, 8 data bits, no parity, 1 stop bit
		/// </summary>
		/// <param name="com">串口信息</param>
		public void StartSerialSlave(string com)
		{
			StartSerialSlave(com, 9600);
		}

		/// <summary>
		/// 启动串口的从机服务，使用默认的参数进行初始化串口，8位数据位，无奇偶校验，1位停止位<br />
		/// Start the slave service of serial, initialize the serial port with default parameters, 8 data bits, no parity, 1 stop bit
		/// </summary>
		/// <param name="com">串口信息</param>
		/// <param name="baudRate">波特率</param>
		public void StartSerialSlave(string com, int baudRate)
		{
			StartSerialSlave(delegate(SerialPortStream sp)
			{
				sp.PortName = com;
				sp.BaudRate = baudRate;
				sp.DataBits = 8;
				sp.Parity = Parity.None;
				sp.StopBits = StopBits.One;
			});
		}

		/// <summary>
		/// 启动串口的从机服务，使用指定的参数进行初始化串口，指定数据位，指定奇偶校验，指定停止位<br />
		/// </summary>
		/// <param name="com">串口信息</param>
		/// <param name="baudRate">波特率</param>
		/// <param name="dataBits">数据位</param>
		/// <param name="parity">奇偶校验</param>
		/// <param name="stopBits">停止位</param>
		public void StartSerialSlave(string com, int baudRate, int dataBits, Parity parity, StopBits stopBits)
		{
			StartSerialSlave(delegate(SerialPortStream sp)
			{
				sp.PortName = com;
				sp.BaudRate = baudRate;
				sp.DataBits = dataBits;
				sp.Parity = parity;
				sp.StopBits = stopBits;
			});
		}

		/// <summary>
		/// 启动串口的从机服务，使用自定义的初始化方法初始化串口的参数<br />
		/// Start the slave service of serial and initialize the parameters of the serial port using a custom initialization method
		/// </summary>
		/// <param name="inni">初始化信息的委托</param>
		public void StartSerialSlave(Action<SerialPortStream> inni)
		{
			if (!serialPort.IsOpen)
			{
				inni?.Invoke(serialPort);
				serialPort.ReadBufferSize = 1024;
				serialPort.ReceivedBytesThreshold = 1;
				serialPort.Open();
				serialPort.DataReceived += SerialPort_DataReceived;
			}
		}

		/// <summary>
		/// 关闭提供从机服务的串口对象<br />
		/// Close the serial port object that provides slave services
		/// </summary>
		public void CloseSerialSlave()
		{
			if (serialPort.IsOpen)
			{
				serialPort.Close();
			}
		}

		/// <summary>
		/// 接收到串口数据的时候触发
		/// </summary>
		/// <param name="sender">串口对象</param>
		/// <param name="e">消息</param>
		private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{

			int num = 0;
			int num2 = 0;
			byte[] array = new byte[1024];
			DateTime now = DateTime.Now;
			while (true)
			{
				int num3 = serialPort.Read(array, num, serialPort.BytesToRead);
				if (num3 == 0 && num2 != 0 && (DateTime.Now - now).TotalMilliseconds >= (double)receiveAtleastTime)
				{
					break;
				}
				num += num3;
				num2++;
				if (CheckSerialReceiveDataComplete(array, num))
				{
					break;
				}
				Thread.Sleep(20);
			}
			if (num == 0)
			{
				return;
			}
			try
			{
				OperateResult<byte[]> operateResult = DealWithSerialReceivedData(array.SelectBegin(num));
				if (operateResult.IsSuccess)
				{
					if (operateResult.Content != null)
					{
						serialPort.Write(operateResult.Content, 0, operateResult.Content.Length);
						if (base.IsStarted)
						{
							RaiseDataReceived(sender, operateResult.Content);
						}
					}
				}
				else
				{
					base.LogNet?.WriteError(ToString(), operateResult.Message);
				}
			}
			catch (Exception ex)
			{
				base.LogNet?.WriteException(ToString(), ex);
			}
		}

		/// <summary>
		/// 检查串口接收的数据是否完成的方法，如果接收完成，则返回<c>True</c>
		/// </summary>
		/// <param name="buffer">缓存的数据信息</param>
		/// <param name="receivedLength">当前已经接收的数据长度信息</param>
		/// <returns>是否接收完成</returns>
		protected virtual bool CheckSerialReceiveDataComplete(byte[] buffer, int receivedLength)
		{
			return false;
		}

		/// <summary>
		/// 处理串口接收数据的功能方法，需要在继承类中进行相关的重写操作
		/// </summary>
		/// <param name="data">串口接收到的原始字节数据</param>
		protected virtual OperateResult<byte[]> DealWithSerialReceivedData(byte[] data)
		{
			return new OperateResult<byte[]>();
		}

		/// <summary>
		/// 获取当前的串口对象信息
		/// </summary>
		/// <returns>串口对象</returns>
		protected SerialPortStream GetSerialPort()
		{
			return serialPort;
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing">是否托管对象</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock_trusted_clients?.Dispose();
				this.OnDataSend = null;
				this.OnDataReceived = null;
				serialPort?.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"NetworkDataServerBase[{base.Port}]";
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public virtual OperateResult<byte[]> Read(string address, ushort length)
		{
			return new OperateResult<byte[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public virtual OperateResult Write(string address, byte[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public virtual OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBool(System.String)" />
		[HslMqttApi("ReadBool", "")]
		public virtual OperateResult<bool> ReadBool(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadBool(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public virtual OperateResult Write(string address, bool[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public virtual OperateResult Write(string address, bool value)
		{
			return Write(address, new bool[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String)" />
		public OperateResult<T> ReadCustomer<T>(string address) where T : IDataTransfer, new()
		{
			return ReadWriteNetHelper.ReadCustomer<T>(this, address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String,``0)" />
		public OperateResult<T> ReadCustomer<T>(string address, T obj) where T : IDataTransfer, new()
		{
			return ReadWriteNetHelper.ReadCustomer(this, address, obj);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteCustomer``1(System.String,``0)" />
		public OperateResult WriteCustomer<T>(string address, T data) where T : IDataTransfer, new()
		{
			return ReadWriteNetHelper.WriteCustomer(this, address, data);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Read``1" />
		public virtual OperateResult<T> Read<T>() where T : class, new()
		{
			return HslReflectionHelper.Read<T>(this);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write``1(``0)" />
		public virtual OperateResult Write<T>(T data) where T : class, new()
		{
			return HslReflectionHelper.Write(data, this);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadStruct``1(System.String,System.UInt16)" />
		public virtual OperateResult<T> ReadStruct<T>(string address, ushort length) where T : class, new()
		{
			return ReadWriteNetHelper.ReadStruct<T>(this, address, length, ByteTransform);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt16(System.String)" />
		[HslMqttApi("ReadInt16", "")]
		public OperateResult<short> ReadInt16(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadInt16(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt16(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt16Array", "")]
		public virtual OperateResult<short[]> ReadInt16(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 1)), (byte[] m) => ByteTransform.TransInt16(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt16(System.String)" />
		[HslMqttApi("ReadUInt16", "")]
		public OperateResult<ushort> ReadUInt16(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadUInt16(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt16(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt16Array", "")]
		public virtual OperateResult<ushort[]> ReadUInt16(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 1)), (byte[] m) => ByteTransform.TransUInt16(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32(System.String)" />
		[HslMqttApi("ReadInt32", "")]
		public OperateResult<int> ReadInt32(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadInt32(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt32Array", "")]
		public virtual OperateResult<int[]> ReadInt32(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32(System.String)" />
		[HslMqttApi("ReadUInt32", "")]
		public OperateResult<uint> ReadUInt32(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadUInt32(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt32Array", "")]
		public virtual OperateResult<uint[]> ReadUInt32(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloat(System.String)" />
		[HslMqttApi("ReadFloat", "")]
		public OperateResult<float> ReadFloat(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadFloat(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloat(System.String,System.UInt16)" />
		[HslMqttApi("ReadFloatArray", "")]
		public virtual OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64(System.String)" />
		[HslMqttApi("ReadInt64", "")]
		public OperateResult<long> ReadInt64(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadInt64(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt64Array", "")]
		public virtual OperateResult<long[]> ReadInt64(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64(System.String)" />
		[HslMqttApi("ReadUInt64", "")]
		public OperateResult<ulong> ReadUInt64(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadUInt64(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt64Array", "")]
		public virtual OperateResult<ulong[]> ReadUInt64(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDouble(System.String)" />
		[HslMqttApi("ReadDouble", "")]
		public OperateResult<double> ReadDouble(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadDouble(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDouble(System.String,System.UInt16)" />
		[HslMqttApi("ReadDoubleArray", "")]
		public virtual OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadString(System.String,System.UInt16)" />
		[HslMqttApi("ReadString", "")]
		public virtual OperateResult<string> ReadString(string address, ushort length)
		{
			return ReadString(address, length, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadString(System.String,System.UInt16,System.Text.Encoding)" />
		public virtual OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => ByteTransform.TransString(m, 0, m.Length, encoding));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int16[])" />
		[HslMqttApi("WriteInt16Array", "")]
		public virtual OperateResult Write(string address, short[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int16)" />
		[HslMqttApi("WriteInt16", "")]
		public virtual OperateResult Write(string address, short value)
		{
			return Write(address, new short[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt16[])" />
		[HslMqttApi("WriteUInt16Array", "")]
		public virtual OperateResult Write(string address, ushort[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt16)" />
		[HslMqttApi("WriteUInt16", "")]
		public virtual OperateResult Write(string address, ushort value)
		{
			return Write(address, new ushort[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int32[])" />
		[HslMqttApi("WriteInt32Array", "")]
		public virtual OperateResult Write(string address, int[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int32)" />
		[HslMqttApi("WriteInt32", "")]
		public OperateResult Write(string address, int value)
		{
			return Write(address, new int[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt32[])" />
		[HslMqttApi("WriteUInt32Array", "")]
		public virtual OperateResult Write(string address, uint[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt32)" />
		[HslMqttApi("WriteUInt32", "")]
		public OperateResult Write(string address, uint value)
		{
			return Write(address, new uint[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Single[])" />
		[HslMqttApi("WriteFloatArray", "")]
		public virtual OperateResult Write(string address, float[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Single)" />
		[HslMqttApi("WriteFloat", "")]
		public OperateResult Write(string address, float value)
		{
			return Write(address, new float[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int64[])" />
		[HslMqttApi("WriteInt64Array", "")]
		public virtual OperateResult Write(string address, long[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int64)" />
		[HslMqttApi("WriteInt64", "")]
		public OperateResult Write(string address, long value)
		{
			return Write(address, new long[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt64[])" />
		[HslMqttApi("WriteUInt64Array", "")]
		public virtual OperateResult Write(string address, ulong[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt64)" />
		[HslMqttApi("WriteUInt64", "")]
		public OperateResult Write(string address, ulong value)
		{
			return Write(address, new ulong[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Double[])" />
		[HslMqttApi("WriteDoubleArray", "")]
		public virtual OperateResult Write(string address, double[] values)
		{
			return Write(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Double)" />
		[HslMqttApi("WriteDouble", "")]
		public OperateResult Write(string address, double value)
		{
			return Write(address, new double[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.String)" />
		[HslMqttApi("WriteString", "")]
		public virtual OperateResult Write(string address, string value)
		{
			return Write(address, value, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.String,System.Int32)" />
		public virtual OperateResult Write(string address, string value, int length)
		{
			return Write(address, value, length, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.String,System.Text.Encoding)" />
		public virtual OperateResult Write(string address, string value, Encoding encoding)
		{
			byte[] array = ByteTransform.TransByte(value, encoding);
			if (WordLength == 1)
			{
				array = SoftBasic.ArrayExpandToLengthEven(array);
			}
			return Write(address, array);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.String,System.Int32,System.Text.Encoding)" />
		public virtual OperateResult Write(string address, string value, int length, Encoding encoding)
		{
			byte[] data = ByteTransform.TransByte(value, encoding);
			if (WordLength == 1)
			{
				data = SoftBasic.ArrayExpandToLengthEven(data);
			}
			data = SoftBasic.ArrayExpandToLength(data, length);
			return Write(address, data);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Boolean,System.Int32,System.Int32)" />
		[HslMqttApi("WaitBool", "")]
		public OperateResult<TimeSpan> Wait(string address, bool waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int16,System.Int32,System.Int32)" />
		[HslMqttApi("WaitInt16", "")]
		public OperateResult<TimeSpan> Wait(string address, short waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt16,System.Int32,System.Int32)" />
		[HslMqttApi("WaitUInt16", "")]
		public OperateResult<TimeSpan> Wait(string address, ushort waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int32,System.Int32,System.Int32)" />
		[HslMqttApi("WaitInt32", "")]
		public OperateResult<TimeSpan> Wait(string address, int waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt32,System.Int32,System.Int32)" />
		[HslMqttApi("WaitUInt32", "")]
		public OperateResult<TimeSpan> Wait(string address, uint waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int64,System.Int32,System.Int32)" />
		[HslMqttApi("WaitInt64", "")]
		public OperateResult<TimeSpan> Wait(string address, long waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt64,System.Int32,System.Int32)" />
		[HslMqttApi("WaitUInt64", "")]
		public OperateResult<TimeSpan> Wait(string address, ulong waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return ReadWriteNetHelper.Wait(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Boolean,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, bool waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int16,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, short waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt16,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, ushort waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int32,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, int waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt32,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, uint waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int64,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, long waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt64,System.Int32,System.Int32)" />
		public async Task<OperateResult<TimeSpan>> WaitAsync(string address, ulong waitValue, int readInterval = 100, int waitTimeout = -1)
		{
			return await ReadWriteNetHelper.WaitAsync(this, address, waitValue, readInterval, waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await Task.Run(() => Read(address, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Byte[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await Task.Run(() => Write(address, value));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBoolAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await Task.Run(() => ReadBool(address, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadBoolAsync(System.String)" />
		public virtual async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadBoolAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Boolean[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, bool[] value)
		{
			return await Task.Run(() => Write(address, value));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Boolean)" />
		public virtual async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await WriteAsync(address, new bool[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomerAsync``1(System.String)" />
		public async Task<OperateResult<T>> ReadCustomerAsync<T>(string address) where T : IDataTransfer, new()
		{
			return await ReadWriteNetHelper.ReadCustomerAsync<T>(this, address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomerAsync``1(System.String,``0)" />
		public async Task<OperateResult<T>> ReadCustomerAsync<T>(string address, T obj) where T : IDataTransfer, new()
		{
			return await ReadWriteNetHelper.ReadCustomerAsync(this, address, obj);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteCustomerAsync``1(System.String,``0)" />
		public async Task<OperateResult> WriteCustomerAsync<T>(string address, T data) where T : IDataTransfer, new()
		{
			return await ReadWriteNetHelper.WriteCustomerAsync(this, address, data);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadAsync``1" />
		public virtual async Task<OperateResult<T>> ReadAsync<T>() where T : class, new()
		{
			return await HslReflectionHelper.ReadAsync<T>(this);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync``1(``0)" />
		public virtual async Task<OperateResult> WriteAsync<T>(T data) where T : class, new()
		{
			return await HslReflectionHelper.WriteAsync(data, this);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadStruct``1(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<T>> ReadStructAsync<T>(string address, ushort length) where T : class, new()
		{
			return await ReadWriteNetHelper.ReadStructAsync<T>(this, address, length, ByteTransform);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt16Async(System.String)" />
		public async Task<OperateResult<short>> ReadInt16Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadInt16Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt16Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 1)), (byte[] m) => ByteTransform.TransInt16(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt16Async(System.String)" />
		public async Task<OperateResult<ushort>> ReadUInt16Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadUInt16Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt16Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 1)), (byte[] m) => ByteTransform.TransUInt16(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32Async(System.String)" />
		public async Task<OperateResult<int>> ReadInt32Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadInt32Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32Async(System.String)" />
		public async Task<OperateResult<uint>> ReadUInt32Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadUInt32Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloatAsync(System.String)" />
		public async Task<OperateResult<float>> ReadFloatAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadFloatAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloatAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64Async(System.String)" />
		public async Task<OperateResult<long>> ReadInt64Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadInt64Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64Async(System.String)" />
		public async Task<OperateResult<ulong>> ReadUInt64Async(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadUInt64Async(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64Async(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDoubleAsync(System.String)" />
		public async Task<OperateResult<double>> ReadDoubleAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadDoubleAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDoubleAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => ByteTransform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadStringAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult<string>> ReadStringAsync(string address, ushort length)
		{
			return await ReadStringAsync(address, length, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadStringAsync(System.String,System.UInt16,System.Text.Encoding)" />
		public virtual async Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => ByteTransform.TransString(m, 0, m.Length, encoding));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int16[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, short[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int16)" />
		public virtual async Task<OperateResult> WriteAsync(string address, short value)
		{
			return await WriteAsync(address, new short[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt16[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, ushort[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt16)" />
		public virtual async Task<OperateResult> WriteAsync(string address, ushort value)
		{
			return await WriteAsync(address, new ushort[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int32[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, int[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int32)" />
		public async Task<OperateResult> WriteAsync(string address, int value)
		{
			return await WriteAsync(address, new int[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt32[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, uint[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt32)" />
		public async Task<OperateResult> WriteAsync(string address, uint value)
		{
			return await WriteAsync(address, new uint[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Single[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, float[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Single)" />
		public async Task<OperateResult> WriteAsync(string address, float value)
		{
			return await WriteAsync(address, new float[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int64[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, long[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int64)" />
		public async Task<OperateResult> WriteAsync(string address, long value)
		{
			return await WriteAsync(address, new long[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt64[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, ulong[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt64)" />
		public async Task<OperateResult> WriteAsync(string address, ulong value)
		{
			return await WriteAsync(address, new ulong[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Double[])" />
		public virtual async Task<OperateResult> WriteAsync(string address, double[] values)
		{
			return await WriteAsync(address, ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Double)" />
		public async Task<OperateResult> WriteAsync(string address, double value)
		{
			return await WriteAsync(address, new double[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.String)" />
		public virtual async Task<OperateResult> WriteAsync(string address, string value)
		{
			return await WriteAsync(address, value, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.String,System.Text.Encoding)" />
		public virtual async Task<OperateResult> WriteAsync(string address, string value, Encoding encoding)
		{
			byte[] temp = ByteTransform.TransByte(value, encoding);
			if (WordLength == 1)
			{
				temp = SoftBasic.ArrayExpandToLengthEven(temp);
			}
			return await WriteAsync(address, temp);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.String,System.Int32)" />
		public virtual async Task<OperateResult> WriteAsync(string address, string value, int length)
		{
			return await WriteAsync(address, value, length, Encoding.ASCII);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.String,System.Int32,System.Text.Encoding)" />
		public virtual async Task<OperateResult> WriteAsync(string address, string value, int length, Encoding encoding)
		{
			byte[] temp2 = ByteTransform.TransByte(value, encoding);
			if (WordLength == 1)
			{
				temp2 = SoftBasic.ArrayExpandToLengthEven(temp2);
			}
			temp2 = SoftBasic.ArrayExpandToLength(temp2, length);
			return await WriteAsync(address, temp2);
		}
	}
}

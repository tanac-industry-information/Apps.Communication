using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Beckhoff
{
	/// <summary>
	/// 倍福的ADS协议，支持读取倍福的地址数据，关于端口号的选择，TwinCAT2，端口号801；TwinCAT3，端口号为851<br />
	/// Beckhoff ’s ADS protocol supports reading Beckhoff ’s address data. For the choice of port number, TwinCAT2, port number 801; TwinCAT3, port number 851
	/// </summary>
	/// <remarks>
	/// 支持的地址格式分三种，第一种是绝对的地址表示，比如M100，I100，Q100；第二种是字符串地址，采用s=aaaa;的表示方式；第三种是绝对内存地址采用i=1000000;的表示方式
	/// <br />
	/// <note type="important">
	/// 在实际的测试中，由于打开了VS软件对倍福PLC进行编程操作，会导致HslCommunicationDemo读取PLC发生间歇性读写失败的问题，此时需要关闭Visual Studio软件对倍福的
	/// 连接，之后HslCommunicationDemo就会读写成功，感谢QQ：1813782515 提供的解决思路。
	/// </note>
	/// </remarks>
	public class BeckhoffAdsNet : NetworkDeviceBase
	{
		private byte[] targetAMSNetId = new byte[8];

		private byte[] sourceAMSNetId = new byte[8];

		private string senderAMSNetId = string.Empty;

		private bool useTagCache = false;

		private readonly Dictionary<string, uint> tagCaches = new Dictionary<string, uint>();

		private readonly object tagLock = new object();

		private readonly SoftIncrementCount incrementCount = new SoftIncrementCount(2147483647L, 1L);

		/// <inheritdoc />
		[HslMqttApi(HttpMethod = "GET", Description = "Get or set the IP address of the remote server. If it is a local test, then it needs to be set to 127.0.0.1")]
		public override string IpAddress
		{
			get
			{
				return base.IpAddress;
			}
			set
			{
				base.IpAddress = value;
				string[] array = base.IpAddress.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					targetAMSNetId[i] = byte.Parse(array[i]);
				}
			}
		}

		/// <summary>
		/// 是否使用标签的名称缓存功能，默认为 <c>False</c><br />
		/// Whether to use tag name caching. The default is <c>False</c>
		/// </summary>
		public bool UseTagCache
		{
			get
			{
				return useTagCache;
			}
			set
			{
				useTagCache = value;
			}
		}

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public BeckhoffAdsNet()
		{
			base.WordLength = 2;
			targetAMSNetId[4] = 1;
			targetAMSNetId[5] = 1;
			targetAMSNetId[6] = 33;
			targetAMSNetId[7] = 3;
			sourceAMSNetId[4] = 1;
			sourceAMSNetId[5] = 1;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// 通过指定的ip地址以及端口号实例化一个默认的对象<br />
		/// Instantiate a default object with the specified IP address and port number
		/// </summary>
		/// <param name="ipAddress">IP地址信息</param>
		/// <param name="port">端口号</param>
		public BeckhoffAdsNet(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new AdsNetMessage();
		}

		/// <summary>
		/// 目标的地址，举例 192.168.0.1.1.1；也可以是带端口号 192.168.0.1.1.1:801<br />
		/// The address of the destination, for example 192.168.0.1.1.1; it can also be the port number 192.168.0.1.1.1: 801
		/// </summary>
		/// <remarks>
		/// Port：1: AMS Router; 2: AMS Debugger; 800: Ring 0 TC2 PLC; 801: TC2 PLC Runtime System 1; 811: TC2 PLC Runtime System 2; <br />
		/// 821: TC2 PLC Runtime System 3; 831: TC2 PLC Runtime System 4; 850: Ring 0 TC3 PLC; 851: TC3 PLC Runtime System 1<br />
		/// 852: TC3 PLC Runtime System 2; 853: TC3 PLC Runtime System 3; 854: TC3 PLC Runtime System 4; ...
		/// </remarks>
		/// <param name="amsNetId">AMSNet Id地址</param>
		public void SetTargetAMSNetId(string amsNetId)
		{
			if (!string.IsNullOrEmpty(amsNetId))
			{
				StrToAMSNetId(amsNetId).CopyTo(targetAMSNetId, 0);
			}
		}

		/// <summary>
		/// 设置原目标地址 举例 192.168.0.100.1.1；也可以是带端口号 192.168.0.100.1.1:34567<br />
		/// Set the original destination address Example: 192.168.0.100.1.1; it can also be the port number 192.168.0.100.1.1: 34567
		/// </summary>
		/// <param name="amsNetId">原地址</param>
		public void SetSenderAMSNetId(string amsNetId)
		{
			if (!string.IsNullOrEmpty(amsNetId))
			{
				StrToAMSNetId(amsNetId).CopyTo(sourceAMSNetId, 0);
				senderAMSNetId = amsNetId;
			}
		}

		/// <summary>
		/// 获取当前发送的AMS的网络ID信息
		/// </summary>
		/// <returns></returns>
		public string GetSenderAMSNetId()
		{
			return GetAmsNetIdString(sourceAMSNetId, 0);
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			if (string.IsNullOrEmpty(senderAMSNetId))
			{
				IPEndPoint iPEndPoint = (IPEndPoint)socket.LocalEndPoint;
				sourceAMSNetId[6] = BitConverter.GetBytes(iPEndPoint.Port)[0];
				sourceAMSNetId[7] = BitConverter.GetBytes(iPEndPoint.Port)[1];
				iPEndPoint.Address.GetAddressBytes().CopyTo(sourceAMSNetId, 0);
			}
			return base.InitializationOnConnect(socket);
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			if (string.IsNullOrEmpty(senderAMSNetId))
			{
				IPEndPoint iPEndPoint = (IPEndPoint)socket.LocalEndPoint;
				sourceAMSNetId[6] = BitConverter.GetBytes(iPEndPoint.Port)[0];
				sourceAMSNetId[7] = BitConverter.GetBytes(iPEndPoint.Port)[1];
				iPEndPoint.Address.GetAddressBytes().CopyTo(sourceAMSNetId, 0);
			}
			return await base.InitializationOnConnectAsync(socket);
		}

		/// <summary>
		/// 根据当前标签的地址获取到内存偏移地址<br />
		/// Get the memory offset address based on the address of the current label
		/// </summary>
		/// <param name="address">带标签的地址信息，例如s=A,那么标签就是A</param>
		/// <returns>内存偏移地址</returns>
		public OperateResult<uint> ReadValueHandle(string address)
		{
			if (!address.StartsWith("s="))
			{
				return new OperateResult<uint>(StringResources.Language.SAMAddressStartWrong);
			}
			OperateResult<byte[]> operateResult = BuildReadWriteCommand(address, 4, isBit: false, StrToAdsBytes(address.Substring(2)));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToUInt32(operateResult2.Content, 46));
		}

		/// <summary>
		/// 将字符串的地址转换为内存的地址，其他地址则不操作<br />
		/// Converts the address of a string to the address of a memory, other addresses do not operate
		/// </summary>
		/// <param name="address">地址信息，s=A的地址转换为i=100000的形式</param>
		/// <returns>地址</returns>
		public OperateResult<string> TransValueHandle(string address)
		{
			if (address.StartsWith("s="))
			{
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (tagCaches.ContainsKey(address))
						{
							return OperateResult.CreateSuccessResult($"i={tagCaches[address]}");
						}
					}
				}
				OperateResult<uint> operateResult = ReadValueHandle(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(operateResult);
				}
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (!tagCaches.ContainsKey(address))
						{
							tagCaches.Add(address, operateResult.Content);
						}
					}
				}
				return OperateResult.CreateSuccessResult($"i={operateResult.Content}");
			}
			return OperateResult.CreateSuccessResult(address);
		}

		/// <summary>
		/// 读取Ads设备的设备信息。主要是版本号，设备名称<br />
		/// Read the device information of the Ads device. Mainly version number, device name
		/// </summary>
		/// <returns>设备信息</returns>
		[HslMqttApi("ReadAdsDeviceInfo", "读取Ads设备的设备信息。主要是版本号，设备名称")]
		public OperateResult<AdsDeviceInfo> ReadAdsDeviceInfo()
		{
			OperateResult<byte[]> operateResult = BuildReadDeviceInfoCommand();
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(new AdsDeviceInfo(operateResult2.Content.RemoveBegin(42)));
		}

		/// <summary>
		/// 读取Ads设备的状态信息，其中<see cref="P:Communication.OperateResult`2.Content1" />是Ads State，<see cref="P:Communication.OperateResult`2.Content2" />是Device State<br />
		/// Read the status information of the Ads device, where <see cref="P:Communication.OperateResult`2.Content1" /> is the Ads State, and <see cref="P:Communication.OperateResult`2.Content2" /> is the Device State
		/// </summary>
		/// <returns>设备状态信息</returns>
		[HslMqttApi("ReadAdsState", "读取Ads设备的状态信息")]
		public OperateResult<ushort, ushort> ReadAdsState()
		{
			OperateResult<byte[]> operateResult = BuildReadStateCommand();
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(operateResult2);
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(operateResult3);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToUInt16(operateResult2.Content, 42), BitConverter.ToUInt16(operateResult2.Content, 44));
		}

		/// <summary>
		/// 写入Ads的状态，可以携带数据信息，数据可以为空<br />
		/// Write the status of Ads, can carry data information, and the data can be empty
		/// </summary>
		/// <param name="state">ads state</param>
		/// <param name="deviceState">device state</param>
		/// <param name="data">数据信息</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteAdsState", "写入Ads的状态，可以携带数据信息，数据可以为空")]
		public OperateResult WriteAdsState(short state, short deviceState, byte[] data)
		{
			OperateResult<byte[]> operateResult = BuildWriteControlCommand(state, deviceState, data);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 释放当前的系统句柄，该句柄是通过<see cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadValueHandle(System.String)" />获取的
		/// </summary>
		/// <param name="handle">句柄</param>
		/// <returns>是否释放成功</returns>
		public OperateResult ReleaseSystemHandle(uint handle)
		{
			OperateResult<byte[]> operateResult = BuildReleaseSystemHandle(handle);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadValueHandle(System.String)" />
		public async Task<OperateResult<uint>> ReadValueHandleAsync(string address)
		{
			if (!address.StartsWith("s="))
			{
				return new OperateResult<uint>(StringResources.Language.SAMAddressStartWrong);
			}
			OperateResult<byte[]> build = BuildReadWriteCommand(address, 4, isBit: false, StrToAdsBytes(address.Substring(2)));
			if (!build.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(build);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(read);
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<uint>(check);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToUInt32(read.Content, 46));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.TransValueHandle(System.String)" />
		public async Task<OperateResult<string>> TransValueHandleAsync(string address)
		{
			if (address.StartsWith("s="))
			{
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (tagCaches.ContainsKey(address))
						{
							return OperateResult.CreateSuccessResult($"i={tagCaches[address]}");
						}
					}
				}
				OperateResult<uint> read = await ReadValueHandleAsync(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<string>(read);
				}
				if (useTagCache)
				{
					lock (tagLock)
					{
						if (!tagCaches.ContainsKey(address))
						{
							tagCaches.Add(address, read.Content);
						}
					}
				}
				return OperateResult.CreateSuccessResult($"i={read.Content}");
			}
			return OperateResult.CreateSuccessResult(address);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadAdsDeviceInfo" />
		public async Task<OperateResult<AdsDeviceInfo>> ReadAdsDeviceInfoAsync()
		{
			OperateResult<byte[]> build = BuildReadDeviceInfoCommand();
			if (!build.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(build);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(read);
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AdsDeviceInfo>(check);
			}
			return OperateResult.CreateSuccessResult(new AdsDeviceInfo(read.Content.RemoveBegin(42)));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadAdsState" />
		public async Task<OperateResult<ushort, ushort>> ReadAdsStateAsync()
		{
			OperateResult<byte[]> build = BuildReadStateCommand();
			if (!build.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(build);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(read);
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, ushort>(check);
			}
			return OperateResult.CreateSuccessResult(BitConverter.ToUInt16(read.Content, 42), BitConverter.ToUInt16(read.Content, 44));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.WriteAdsState(System.Int16,System.Int16,System.Byte[])" />
		public async Task<OperateResult> WriteAdsStateAsync(short state, short deviceState, byte[] data)
		{
			OperateResult<byte[]> build = BuildWriteControlCommand(state, deviceState, data);
			if (!build.IsSuccess)
			{
				return build;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReleaseSystemHandle(System.UInt32)" />
		public async Task<OperateResult> ReleaseSystemHandleAsync(uint handle)
		{
			OperateResult<byte[]> build = BuildReleaseSystemHandle(handle);
			if (!build.IsSuccess)
			{
				return build;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A</param>
		/// <param name="length">长度</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<string> operateResult = TransValueHandle(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			address = operateResult.Content;
			OperateResult<byte[]> operateResult2 = BuildReadCommand(address, length, isBit: false);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			OperateResult operateResult4 = CheckResponse(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ArrayRemoveBegin(operateResult3.Content, 46));
		}

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			OperateResult<string> operateResult = TransValueHandle(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			address = operateResult.Content;
			OperateResult<byte[]> operateResult2 = BuildWriteCommand(address, value, isBit: false);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			OperateResult operateResult4 = CheckResponse(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return operateResult4;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">PLC的地址信息，例如 M10</param>
		/// <param name="length">数据长度</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<string> operateResult = TransValueHandle(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			address = operateResult.Content;
			OperateResult<byte[]> operateResult2 = BuildReadCommand(address, length, isBit: true);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult2);
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult3);
			}
			OperateResult operateResult4 = CheckResponse(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult4);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(operateResult3.Content, 46)));
		}

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			OperateResult<string> operateResult = TransValueHandle(address);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			address = operateResult.Content;
			OperateResult<byte[]> operateResult2 = BuildWriteCommand(address, value, isBit: true);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			OperateResult operateResult4 = CheckResponse(operateResult3.Content);
			if (!operateResult4.IsSuccess)
			{
				return operateResult4;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 读取PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// Read PLC data, there are three formats of address, one: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <returns>包含是否成功的结果对象</returns>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <summary>
		/// 写入PLC的数据，地址共有三种格式，一：I,Q,M数据信息，举例M0,M100；二：内存地址，i=100000；三：标签地址，s=A<br />
		/// There are three formats for the data written into the PLC. One: I, Q, M data information, such as M0, M100; two: memory address, i = 100000; three: tag address, s = A
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync(address);
			if (!addressCheck.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(addressCheck);
			}
			address = addressCheck.Content;
			OperateResult<byte[]> build = BuildReadCommand(address, length, isBit: false);
			if (!build.IsSuccess)
			{
				return build;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ArrayRemoveBegin(read.Content, 46));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync(address);
			if (!addressCheck.IsSuccess)
			{
				return addressCheck;
			}
			address = addressCheck.Content;
			OperateResult<byte[]> build = BuildWriteCommand(address, value, isBit: false);
			if (!build.IsSuccess)
			{
				return build;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadBool(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync(address);
			if (!addressCheck.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(addressCheck);
			}
			address = addressCheck.Content;
			OperateResult<byte[]> build = BuildReadCommand(address, length, isBit: true);
			if (!build.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(build);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(check);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(SoftBasic.ArrayRemoveBegin(read.Content, 46)));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Write(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] value)
		{
			OperateResult<string> addressCheck = await TransValueHandleAsync(address);
			if (!addressCheck.IsSuccess)
			{
				return addressCheck;
			}
			address = addressCheck.Content;
			OperateResult<byte[]> build = BuildWriteCommand(address, value, isBit: true);
			if (!build.IsSuccess)
			{
				return build;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(build.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.ReadByte(System.String)" />
		public async Task<OperateResult<byte>> ReadByteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Beckhoff.BeckhoffAdsNet.Write(System.String,System.Byte)" />
		public async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteAsync(address, new byte[1] { value });
		}

		/// <summary>
		/// 根据命令码ID，消息ID，数据信息组成AMS的命令码
		/// </summary>
		/// <param name="commandId">命令码ID</param>
		/// <param name="data">数据内容</param>
		/// <returns>打包之后的数据信息，没有填写AMSNetId的Target和Source内容</returns>
		public byte[] BuildAmsHeaderCommand(ushort commandId, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			uint value = (uint)incrementCount.GetCurrentValue();
			byte[] array = new byte[32 + data.Length];
			targetAMSNetId.CopyTo(array, 0);
			sourceAMSNetId.CopyTo(array, 8);
			array[16] = BitConverter.GetBytes(commandId)[0];
			array[17] = BitConverter.GetBytes(commandId)[1];
			array[18] = 4;
			array[19] = 0;
			array[20] = BitConverter.GetBytes(data.Length)[0];
			array[21] = BitConverter.GetBytes(data.Length)[1];
			array[22] = BitConverter.GetBytes(data.Length)[2];
			array[23] = BitConverter.GetBytes(data.Length)[3];
			array[24] = 0;
			array[25] = 0;
			array[26] = 0;
			array[27] = 0;
			array[28] = BitConverter.GetBytes(value)[0];
			array[29] = BitConverter.GetBytes(value)[1];
			array[30] = BitConverter.GetBytes(value)[2];
			array[31] = BitConverter.GetBytes(value)[3];
			data.CopyTo(array, 32);
			return PackAmsTcpHelper(AmsTcpHeaderFlags.Command, array);
		}

		/// <summary>
		/// 构建读取设备信息的命令报文
		/// </summary>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildReadDeviceInfoCommand()
		{
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(1, null));
		}

		/// <summary>
		/// 构建读取状态的命令报文
		/// </summary>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildReadStateCommand()
		{
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(4, null));
		}

		/// <summary>
		/// 构建写入状态的命令报文
		/// </summary>
		/// <param name="state">Ads state</param>
		/// <param name="deviceState">Device state</param>
		/// <param name="data">Data</param>
		/// <returns>报文信息</returns>
		public OperateResult<byte[]> BuildWriteControlCommand(short state, short deviceState, byte[] data)
		{
			if (data == null)
			{
				data = new byte[0];
			}
			byte[] array = new byte[8 + data.Length];
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(5, SoftBasic.SpliceArray<byte>(BitConverter.GetBytes(state), BitConverter.GetBytes(deviceState), BitConverter.GetBytes(data.Length), data)));
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildReadCommand(string address, int length, bool isBit)
		{
			OperateResult<uint, uint> operateResult = AnalysisAddress(address, isBit);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[12];
			BitConverter.GetBytes(operateResult.Content1).CopyTo(array, 0);
			BitConverter.GetBytes(operateResult.Content2).CopyTo(array, 4);
			BitConverter.GetBytes(length).CopyTo(array, 8);
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(2, array));
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="length">数据长度</param>
		/// <param name="isBit">是否是位信息</param>
		/// <param name="value">写入的数值</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildReadWriteCommand(string address, int length, bool isBit, byte[] value)
		{
			OperateResult<uint, uint> operateResult = AnalysisAddress(address, isBit);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[16 + value.Length];
			BitConverter.GetBytes(operateResult.Content1).CopyTo(array, 0);
			BitConverter.GetBytes(operateResult.Content2).CopyTo(array, 4);
			BitConverter.GetBytes(length).CopyTo(array, 8);
			BitConverter.GetBytes(value.Length).CopyTo(array, 12);
			value.CopyTo(array, 16);
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(9, array));
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildWriteCommand(string address, byte[] value, bool isBit)
		{
			OperateResult<uint, uint> operateResult = AnalysisAddress(address, isBit);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = new byte[12 + value.Length];
			BitConverter.GetBytes(operateResult.Content1).CopyTo(array, 0);
			BitConverter.GetBytes(operateResult.Content2).CopyTo(array, 4);
			BitConverter.GetBytes(value.Length).CopyTo(array, 8);
			value.CopyTo(array, 12);
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(3, array));
		}

		/// <summary>
		/// 构建写入的指令信息
		/// </summary>
		/// <param name="address">地址信息</param>
		/// <param name="value">数据</param>
		/// <param name="isBit">是否是位信息</param>
		/// <returns>结果内容</returns>
		public OperateResult<byte[]> BuildWriteCommand(string address, bool[] value, bool isBit)
		{
			OperateResult<uint, uint> operateResult = AnalysisAddress(address, isBit);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			byte[] array = SoftBasic.BoolArrayToByte(value);
			byte[] array2 = new byte[12 + array.Length];
			BitConverter.GetBytes(operateResult.Content1).CopyTo(array2, 0);
			BitConverter.GetBytes(operateResult.Content2).CopyTo(array2, 4);
			BitConverter.GetBytes(array.Length).CopyTo(array2, 8);
			array.CopyTo(array2, 12);
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(3, array2));
		}

		/// <summary>
		/// 构建释放句柄的报文信息，当获取了变量的句柄后，这个句柄就被释放
		/// </summary>
		/// <param name="handle">句柄信息</param>
		/// <returns>报文的结果内容</returns>
		public OperateResult<byte[]> BuildReleaseSystemHandle(uint handle)
		{
			byte[] array = new byte[16];
			BitConverter.GetBytes(61446).CopyTo(array, 0);
			BitConverter.GetBytes(4).CopyTo(array, 8);
			BitConverter.GetBytes(handle).CopyTo(array, 12);
			return OperateResult.CreateSuccessResult(BuildAmsHeaderCommand(3, array));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"BeckhoffAdsNet[{IpAddress}:{Port}]";
		}

		/// <summary>
		/// 检查从PLC的反馈的数据报文是否正确
		/// </summary>
		/// <param name="response">反馈报文</param>
		/// <returns>检查结果</returns>
		public static OperateResult CheckResponse(byte[] response)
		{
			try
			{
				int num = BitConverter.ToInt32(response, 30);
				if (num > 0)
				{
					return new OperateResult(num, GetErrorCodeText(num) + Environment.NewLine + "Source:" + response.ToHexString(' '));
				}
				int num2 = BitConverter.ToInt32(response, 38);
				if (num2 != 0)
				{
					return new OperateResult(num2, StringResources.Language.UnknownError + " Source:" + response.ToHexString(' '));
				}
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message + " Source:" + response.ToHexString(' '));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 将实际的包含AMS头报文和数据报文的命令，打包成实际可发送的命令
		/// </summary>
		/// <param name="headerFlags">命令头信息</param>
		/// <param name="command">命令信息</param>
		/// <returns>结果信息</returns>
		public static byte[] PackAmsTcpHelper(AmsTcpHeaderFlags headerFlags, byte[] command)
		{
			byte[] array = new byte[6 + command.Length];
			BitConverter.GetBytes((ushort)headerFlags).CopyTo(array, 0);
			BitConverter.GetBytes(command.Length).CopyTo(array, 2);
			command.CopyTo(array, 6);
			return array;
		}

		/// <summary>
		/// 分析当前的地址信息，根据结果信息进行解析出真实的偏移地址
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="isBit">是否位访问</param>
		/// <returns>结果内容</returns>
		public static OperateResult<uint, uint> AnalysisAddress(string address, bool isBit)
		{
			OperateResult<uint, uint> operateResult = new OperateResult<uint, uint>();
			try
			{
				if (address.StartsWith("i="))
				{
					operateResult.Content1 = 61445u;
					operateResult.Content2 = uint.Parse(address.Substring(2));
				}
				else if (address.StartsWith("s="))
				{
					operateResult.Content1 = 61443u;
					operateResult.Content2 = 0u;
				}
				else
				{
					switch (address[0])
					{
					case 'M':
					case 'm':
						if (isBit)
						{
							operateResult.Content1 = 16417u;
						}
						else
						{
							operateResult.Content1 = 16416u;
						}
						break;
					case 'I':
					case 'i':
						if (isBit)
						{
							operateResult.Content1 = 61473u;
						}
						else
						{
							operateResult.Content1 = 61472u;
						}
						break;
					case 'Q':
					case 'q':
						if (isBit)
						{
							operateResult.Content1 = 61489u;
						}
						else
						{
							operateResult.Content1 = 61488u;
						}
						break;
					default:
						throw new Exception(StringResources.Language.NotSupportedDataType);
					}
					operateResult.Content2 = uint.Parse(address.Substring(1));
				}
			}
			catch (Exception ex)
			{
				operateResult.Message = ex.Message;
				return operateResult;
			}
			operateResult.IsSuccess = true;
			operateResult.Message = StringResources.Language.SuccessText;
			return operateResult;
		}

		/// <summary>
		/// 将字符串名称转变为ADS协议可识别的字节数组
		/// </summary>
		/// <param name="value">值</param>
		/// <returns>字节数组</returns>
		public static byte[] StrToAdsBytes(string value)
		{
			return SoftBasic.SpliceArray<byte>(Encoding.ASCII.GetBytes(value), new byte[1]);
		}

		/// <summary>
		/// 将字符串的信息转换为AMS目标的地址
		/// </summary>
		/// <param name="amsNetId">目标信息</param>
		/// <returns>字节数组</returns>
		public static byte[] StrToAMSNetId(string amsNetId)
		{
			string text = amsNetId;
			byte[] array;
			if (amsNetId.IndexOf(':') > 0)
			{
				array = new byte[8];
				string[] array2 = amsNetId.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				text = array2[0];
				array[6] = BitConverter.GetBytes(int.Parse(array2[1]))[0];
				array[7] = BitConverter.GetBytes(int.Parse(array2[1]))[1];
			}
			else
			{
				array = new byte[6];
			}
			string[] array3 = text.Split(new char[1] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array3.Length; i++)
			{
				array[i] = byte.Parse(array3[i]);
			}
			return array;
		}

		/// <summary>
		/// 根据byte数组信息提取出字符串格式的AMSNetId数据信息，方便日志查看
		/// </summary>
		/// <param name="data">原始的报文数据信息</param>
		/// <param name="index">起始的节点信息</param>
		/// <returns>Ams节点号信息</returns>
		public static string GetAmsNetIdString(byte[] data, int index)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(data[index]);
			stringBuilder.Append(".");
			stringBuilder.Append(data[index + 1]);
			stringBuilder.Append(".");
			stringBuilder.Append(data[index + 2]);
			stringBuilder.Append(".");
			stringBuilder.Append(data[index + 3]);
			stringBuilder.Append(".");
			stringBuilder.Append(data[index + 4]);
			stringBuilder.Append(".");
			stringBuilder.Append(data[index + 5]);
			stringBuilder.Append(":");
			stringBuilder.Append(BitConverter.ToUInt16(data, index + 6));
			return stringBuilder.ToString();
		}

		/// <summary>
		/// 根据AMS的错误号，获取到错误信息，错误信息来源于 wirshake 源代码文件 "..\wireshark\plugins\epan\ethercat\packet-ams.c"
		/// </summary>
		/// <param name="error">错误号</param>
		/// <returns>错误的描述信息</returns>
		public static string GetErrorCodeText(int error)
		{
			switch (error)
			{
			case 0:
				return "NO ERROR";
			case 1:
				return "INTERNAL";
			case 2:
				return "NO RTIME";
			case 3:
				return "ALLOC LOCKED MEM";
			case 4:
				return "INSERT MAILBOX";
			case 5:
				return "WRONGRECEIVEHMSG";
			case 6:
				return "TARGET PORT NOT FOUND";
			case 7:
				return "TARGET MACHINE NOT FOUND";
			case 8:
				return "UNKNOWN CMDID";
			case 9:
				return "BAD TASKID";
			case 10:
				return "NOIO";
			case 11:
				return "UNKNOWN AMSCMD";
			case 12:
				return "WIN32 ERROR";
			case 13:
				return "PORT NOT CONNECTED";
			case 14:
				return "INVALID AMS LENGTH";
			case 15:
				return "INVALID AMS NETID";
			case 16:
				return "LOW INST LEVEL";
			case 17:
				return "NO DEBUG INT AVAILABLE";
			case 18:
				return "PORT DISABLED";
			case 19:
				return "PORT ALREADY CONNECTED";
			case 20:
				return "AMSSYNC_W32ERROR";
			case 21:
				return "AMSSYNC_TIMEOUT";
			case 22:
				return "AMSSYNC_AMSERROR";
			case 23:
				return "AMSSYNC_NOINDEXINMAP";
			case 24:
				return "INVALID AMSPORT";
			case 25:
				return "NO MEMORY";
			case 26:
				return "TCP SEND";
			case 27:
				return "HOST UNREACHABLE";
			default:
				return StringResources.Language.UnknownError;
			}
		}
	}
}

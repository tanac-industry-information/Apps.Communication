using Apps.Communication.Core;
using Apps.Communication.Profinet.Siemens.Helper;
using Apps.Communication.Reflection;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Siemens
{
	/// <summary>
	/// 西门子的PPI协议，适用于s7-200plc，注意，由于本类库的每次通讯分成2次操作，内部增加了一个同步锁，所以单次通信时间比较久，另外，地址支持携带站号，例如：s=2;M100<br />
	/// Siemens' PPI protocol is suitable for s7-200plc. Note that since each communication of this class library is divided into two operations, 
	/// and a synchronization lock is added inside, the single communication time is relatively long. In addition, 
	/// the address supports carrying the station number, for example : S=2;M100
	/// </summary>
	/// <remarks>
	/// 适用于西门子200的通信，非常感谢 合肥-加劲 的测试，让本类库圆满完成。注意：M地址范围有限 0-31地址<br />
	/// 在本类的<see cref="T:Communication.Profinet.Siemens.SiemensPPIOverTcp" />实现类里，如果使用了Async的异步方法，没有增加同步锁，多线程调用可能会引发数据错乱的情况。<br />
	/// In the <see cref="T:Communication.Profinet.Siemens.SiemensPPIOverTcp" /> implementation class of this class, if the asynchronous method of Async is used, 
	/// the synchronization lock is not added, and multi-threaded calls may cause data disorder.
	/// </remarks>
	public class SiemensPPI : SerialDeviceBase
	{
		private byte station = 2;

		private object communicationLock;

		/// <summary>
		/// 西门子PLC的站号信息<br />
		/// Siemens PLC station number information
		/// </summary>
		[HslMqttApi]
		public byte Station
		{
			get
			{
				return station;
			}
			set
			{
				station = value;
			}
		}

		/// <summary>
		/// 实例化一个西门子的PPI协议对象<br />
		/// Instantiate a Siemens PPI protocol object
		/// </summary>
		public SiemensPPI()
		{
			base.ByteTransform = new ReverseBytesTransform();
			base.WordLength = 2;
			communicationLock = new object();
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.Read(Communication.Core.IReadWriteDevice,System.String,System.UInt16,System.Byte,System.Object)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return SiemensPPIHelper.Read(this, address, length, Station, communicationLock);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.ReadBool(Communication.Core.IReadWriteDevice,System.String,System.UInt16,System.Byte,System.Object)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return SiemensPPIHelper.ReadBool(this, address, length, Station, communicationLock);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Byte[],System.Byte,System.Object)" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return SiemensPPIHelper.Write(this, address, value, Station, communicationLock);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.Write(Communication.Core.IReadWriteDevice,System.String,System.Boolean[],System.Byte,System.Object)" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return SiemensPPIHelper.Write(this, address, value, Station, communicationLock);
		}

		/// <summary>
		/// 从西门子的PLC中读取byte数据信息，地址为"M100","AI100","I0","Q0","V100","S100"等，详细请参照API文档<br />
		/// Read byte data information from Siemens PLC. The addresses are "M100", "AI100", "I0", "Q0", "V100", "S100", etc. Please refer to the API documentation for details.
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <returns>带返回结果的结果对象</returns>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <summary>
		/// 向西门子的PLC中写入byte数据，地址为"M100","AI100","I0","Q0","V100","S100"等，详细请参照API文档<br />
		/// Write byte data from Siemens PLC with addresses "M100", "AI100", "I0", "Q0", "V100", "S100", etc. For details, please refer to the API documentation
		/// </summary>
		/// <param name="address">西门子的地址数据信息</param>
		/// <param name="value">数据长度</param>
		/// <returns>带返回结果的结果对象</returns>
		[HslMqttApi("WriteByte", "向西门子的PLC中写入byte数据，地址为\"M100\",\"AI100\",\"I0\",\"Q0\",\"V100\",\"S100\"等，详细请参照API文档")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.Start(Communication.Core.IReadWriteDevice,System.String,System.Byte,System.Object)" />
		[HslMqttApi]
		public OperateResult Start(string parameter = "")
		{
			return SiemensPPIHelper.Start(this, parameter, Station, communicationLock);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Siemens.Helper.SiemensPPIHelper.Stop(Communication.Core.IReadWriteDevice,System.String,System.Byte,System.Object)" />
		[HslMqttApi]
		public OperateResult Stop(string parameter = "")
		{
			return SiemensPPIHelper.Stop(this, parameter, Station, communicationLock);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"SiemensPPI[{base.PortName}:{base.BaudRate}]";
		}
	}
}

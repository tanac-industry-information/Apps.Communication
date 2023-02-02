using System;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Serial
{
	/// <summary>
	/// 串口设备交互类的基类，实现了<see cref="T:Communication.Core.IReadWriteDevice" />接口的基础方法方法，需要使用继承重写来实现字节读写，bool读写操作。<br />
	/// The base class of the serial device interaction class, which implements the basic methods of the <see cref="T:Communication.Core.IReadWriteDevice" /> interface, 
	/// requires inheritance rewriting to implement byte read and write, and bool read and write operations.
	/// </summary>
	/// <remarks>
	/// 本类实现了不同的数据类型的读写交互的api，继承自本类，重写下面的四个方法将可以实现你自己的设备通信对象
	/// <list type="number">
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.Read(System.String,System.UInt16)" /> 方法，读取字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.Write(System.String,System.Byte[])" /> 方法，写入字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.ReadBool(System.String,System.UInt16)" /> 方法，读取bool数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.Write(System.String,System.Boolean[])" /> 方法，写入bool数组的方法。
	/// </item>
	/// </list>
	/// 如果需要实现异步的方法。那就需要重写下面的四个方法。
	/// <list type="number">
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.ReadAsync(System.String,System.UInt16)" /> 方法，读取字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.WriteAsync(System.String,System.Byte[])" /> 方法，写入字节数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.ReadBoolAsync(System.String,System.UInt16)" /> 方法，读取bool数组的方法。
	/// </item>
	/// <item>
	/// <see cref="M:Communication.Serial.SerialDeviceBase.WriteAsync(System.String,System.Boolean[])" /> 方法，写入bool数组的方法。
	/// </item>
	/// </list>
	/// </remarks>
	public class SerialDeviceBase : SerialBase, IReadWriteDevice, IReadWriteNet
	{
		private IByteTransform byteTransform;

		private string connectionId = string.Empty;

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ByteTransform" />
		public IByteTransform ByteTransform
		{
			get
			{
				return byteTransform;
			}
			set
			{
				byteTransform = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDoubleBase.ConnectionId" />
		public string ConnectionId
		{
			get
			{
				return connectionId;
			}
			set
			{
				connectionId = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkDeviceBase.WordLength" />
		protected ushort WordLength { get; set; } = 1;


		/// <summary>
		/// 默认的构造方法实现的设备信息
		/// </summary>
		public SerialDeviceBase()
		{
			connectionId = SoftBasic.GetUniqueStringByGuidAndRandom();
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

		/// <inheritdoc />
		public override string ToString()
		{
			return $"SerialDeviceBase<{byteTransform.GetType()}>";
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

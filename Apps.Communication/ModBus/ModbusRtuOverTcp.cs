using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.ModBus
{
	/// <inheritdoc cref="T:Communication.ModBus.ModbusRtu" />
	public class ModbusRtuOverTcp : NetworkDeviceBase, IModbus, IReadWriteDevice, IReadWriteNet
	{
		private byte station = 1;

		private bool isAddressStartWithZero = true;

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.AddressStartWithZero" />
		public bool AddressStartWithZero
		{
			get
			{
				return isAddressStartWithZero;
			}
			set
			{
				isAddressStartWithZero = value;
			}
		}

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.Station" />
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

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.DataFormat" />
		public DataFormat DataFormat
		{
			get
			{
				return base.ByteTransform.DataFormat;
			}
			set
			{
				base.ByteTransform.DataFormat = value;
			}
		}

		/// <inheritdoc cref="P:Communication.ModBus.ModbusTcpNet.IsStringReverse" />
		public bool IsStringReverse
		{
			get
			{
				return base.ByteTransform.IsStringReverseByteWord;
			}
			set
			{
				base.ByteTransform.IsStringReverseByteWord = value;
			}
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtu.#ctor" />
		public ModbusRtuOverTcp()
		{
			base.ByteTransform = new ReverseWordTransform();
			base.WordLength = 1;
			base.SleepTime = 20;
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.#ctor(System.String,System.Int32,System.Byte)" />
		public ModbusRtuOverTcp(string ipAddress, int port = 502, byte station = 1)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
			this.station = station;
		}

		/// <inheritdoc cref="M:Communication.ModBus.IModbus.TranslateToModbusAddress(System.String,System.Byte)" />
		public virtual OperateResult<string> TranslateToModbusAddress(string address, byte modbusCode)
		{
			return OperateResult.CreateSuccessResult(address);
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return ModbusInfo.PackCommandToRtu(command);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return ModbusHelper.ExtraRtuResponseContent(send, response);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadCoil(System.String)" />
		public OperateResult<bool> ReadCoil(string address)
		{
			return ReadBool(address);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadCoil(System.String,System.UInt16)" />
		public OperateResult<bool[]> ReadCoil(string address, ushort length)
		{
			return ReadBool(address, length);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadDiscrete(System.String)" />
		public OperateResult<bool> ReadDiscrete(string address)
		{
			return ByteTransformHelper.GetResultFromArray(ReadDiscrete(address, 1));
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadDiscrete(System.String,System.UInt16)" />
		public OperateResult<bool[]> ReadDiscrete(string address, ushort length)
		{
			return ModbusHelper.ReadBoolHelper(this, address, length, 2);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return ModbusHelper.Read(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return ModbusHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Int16)" />
		[HslMqttApi("WriteInt16", "")]
		public override OperateResult Write(string address, short value)
		{
			return ModbusHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.UInt16)" />
		[HslMqttApi("WriteUInt16", "")]
		public override OperateResult Write(string address, ushort value)
		{
			return ModbusHelper.Write(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteMask(System.String,System.UInt16,System.UInt16)" />
		[HslMqttApi("WriteMask", "")]
		public OperateResult WriteMask(string address, ushort andMask, ushort orMask)
		{
			return ModbusHelper.WriteMask(this, address, andMask, orMask);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtuOverTcp.Write(System.String,System.Int16)" />
		public OperateResult WriteOneRegister(string address, short value)
		{
			return Write(address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtuOverTcp.Write(System.String,System.UInt16)" />
		public OperateResult WriteOneRegister(string address, ushort value)
		{
			return Write(address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadCoilAsync(System.String)" />
		public async Task<OperateResult<bool>> ReadCoilAsync(string address)
		{
			return await ReadBoolAsync(address);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadCoilAsync(System.String,System.UInt16)" />
		public async Task<OperateResult<bool[]>> ReadCoilAsync(string address, ushort length)
		{
			return await ReadBoolAsync(address, length);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadDiscreteAsync(System.String)" />
		public async Task<OperateResult<bool>> ReadDiscreteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadDiscreteAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadDiscreteAsync(System.String,System.UInt16)" />
		public async Task<OperateResult<bool[]>> ReadDiscreteAsync(string address, ushort length)
		{
			return await ReadBoolHelperAsync(address, length, 2);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			return await ModbusHelper.ReadAsync(this, address, length);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteAsync(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await ModbusHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtuOverTcp.Write(System.String,System.Int16)" />
		public override async Task<OperateResult> WriteAsync(string address, short value)
		{
			return await ModbusHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtuOverTcp.Write(System.String,System.UInt16)" />
		public override async Task<OperateResult> WriteAsync(string address, ushort value)
		{
			return await ModbusHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteOneRegisterAsync(System.String,System.Int16)" />
		public async Task<OperateResult> WriteOneRegisterAsync(string address, short value)
		{
			return await WriteAsync(address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteOneRegisterAsync(System.String,System.UInt16)" />
		public async Task<OperateResult> WriteOneRegisterAsync(string address, ushort value)
		{
			return await WriteAsync(address, value);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtuOverTcp.WriteMask(System.String,System.UInt16,System.UInt16)" />
		public async Task<OperateResult> WriteMaskAsync(string address, ushort andMask, ushort orMask)
		{
			return await Task.Run(() => WriteMask(address, andMask, orMask));
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return ModbusHelper.ReadBoolHelper(this, address, length, 1);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			return ModbusHelper.Write(this, address, values);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.Write(System.String,System.Boolean)" />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			return ModbusHelper.Write(this, address, value);
		}

		private async Task<OperateResult<bool[]>> ReadBoolHelperAsync(string address, ushort length, byte function)
		{
			return await ModbusHelper.ReadBoolHelperAsync(this, address, length, function);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.ReadBoolAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			return await ReadBoolHelperAsync(address, length, 1);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteAsync(System.String,System.Boolean[])" />
		public override async Task<OperateResult> WriteAsync(string address, bool[] values)
		{
			return await ModbusHelper.WriteAsync(this, address, values);
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusTcpNet.WriteAsync(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			return await ModbusHelper.WriteAsync(this, address, value);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt32Array", "")]
		public override OperateResult<int[]> ReadInt32(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt32Array", "")]
		public override OperateResult<uint[]> ReadUInt32(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloat(System.String,System.UInt16)" />
		[HslMqttApi("ReadFloatArray", "")]
		public override OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadInt64Array", "")]
		public override OperateResult<long[]> ReadInt64(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64(System.String,System.UInt16)" />
		[HslMqttApi("ReadUInt64Array", "")]
		public override OperateResult<ulong[]> ReadUInt64(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDouble(System.String,System.UInt16)" />
		[HslMqttApi("ReadDoubleArray", "")]
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(Read(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int32[])" />
		[HslMqttApi("WriteInt32Array", "")]
		public override OperateResult Write(string address, int[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt32[])" />
		[HslMqttApi("WriteUInt32Array", "")]
		public override OperateResult Write(string address, uint[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Single[])" />
		[HslMqttApi("WriteFloatArray", "")]
		public override OperateResult Write(string address, float[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Int64[])" />
		[HslMqttApi("WriteInt64Array", "")]
		public override OperateResult Write(string address, long[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.UInt64[])" />
		[HslMqttApi("WriteUInt64Array", "")]
		public override OperateResult Write(string address, ulong[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Write(System.String,System.Double[])" />
		[HslMqttApi("WriteDoubleArray", "")]
		public override OperateResult Write(string address, double[] values)
		{
			IByteTransform byteTransform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return Write(address, byteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt32Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt32Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadFloatAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 2)), (byte[] m) => transform.TransSingle(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadInt64Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadUInt64Async(System.String,System.UInt16)" />
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadDoubleAsync(System.String,System.UInt16)" />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			IByteTransform transform = HslHelper.ExtractTransformParameter(ref address, base.ByteTransform);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, GetWordLength(address, length, 4)), (byte[] m) => transform.TransDouble(m, 0, length));
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int32[])" />
		public override async Task<OperateResult> WriteAsync(string address, int[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt32[])" />
		public override async Task<OperateResult> WriteAsync(string address, uint[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Single[])" />
		public override async Task<OperateResult> WriteAsync(string address, float[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Int64[])" />
		public override async Task<OperateResult> WriteAsync(string address, long[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.UInt64[])" />
		public override async Task<OperateResult> WriteAsync(string address, ulong[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteAsync(System.String,System.Double[])" />
		public override async Task<OperateResult> WriteAsync(string address, double[] values)
		{
			return await WriteAsync(value: HslHelper.ExtractTransformParameter(ref address, base.ByteTransform).TransByte(values), address: address);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ModbusRtuOverTcp[{IpAddress}:{Port}]";
		}
	}
}

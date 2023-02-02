using System;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.AllenBradley;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙PLC的CIP协议的类，支持NJ,NX,NY系列PLC，支持tag名的方式读写数据，假设你读取的是局部变量，那么使用 Program:MainProgram.变量名<br />
	/// Omron PLC's CIP protocol class, support NJ, NX, NY series PLC, support tag name read and write data, assuming you read local variables, then use Program: MainProgram. Variable name
	/// </summary>
	public class OmronCipNet : AllenBradleyNet
	{
		/// <summary>
		/// Instantiate a communication object for a OmronCipNet PLC protocol
		/// </summary>
		public OmronCipNet()
		{
		}

		/// <summary>
		/// Specify the IP address and port to instantiate a communication object for a OmronCipNet PLC protocol
		/// </summary>
		/// <param name="ipAddress">PLC IpAddress</param>
		/// <param name="port">PLC Port</param>
		public OmronCipNet(string ipAddress, int port = 44818)
			: base(ipAddress, port)
		{
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			if (length > 1)
			{
				return Read(new string[1] { address }, new int[1] { 1 });
			}
			return Read(new string[1] { address }, new int[1] { length });
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt16Array", "")]
		public override OperateResult<short[]> ReadInt16(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt16(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt16(m, (startIndex >= 0) ? (startIndex * 2) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt16Array", "")]
		public override OperateResult<ushort[]> ReadUInt16(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt16(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt16(m, (startIndex >= 0) ? (startIndex * 2) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt32Array", "")]
		public override OperateResult<int[]> ReadInt32(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt32(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt32(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt32Array", "")]
		public override OperateResult<uint[]> ReadUInt32(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt32(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt32(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadFloatArray", "")]
		public override OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransSingle(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt64Array", "")]
		public override OperateResult<long[]> ReadInt64(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt64(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransInt64(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt64Array", "")]
		public override OperateResult<ulong[]> ReadUInt64(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt64(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransUInt64(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadDoubleArray", "")]
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(Read(address, 1), (byte[] m) => base.ByteTransform.TransDouble(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		public override OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> operateResult = Read(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			int count = base.ByteTransform.TransUInt16(operateResult.Content, 0);
			return OperateResult.CreateSuccessResult(encoding.GetString(operateResult.Content, 2, count));
		}

		/// <inheritdoc />
		public override OperateResult<T> ReadStruct<T>(string address, ushort length)
		{
			return ReadWriteNetHelper.ReadStruct<T>(this, address, length, base.ByteTransform, 2);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt16Array", "")]
		public override OperateResult Write(string address, short[] values)
		{
			return WriteTag(address, 195, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt16Array", "")]
		public override OperateResult Write(string address, ushort[] values)
		{
			return WriteTag(address, 199, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt32Array", "")]
		public override OperateResult Write(string address, int[] values)
		{
			return WriteTag(address, 196, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt32Array", "")]
		public override OperateResult Write(string address, uint[] values)
		{
			return WriteTag(address, 200, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteFloatArray", "")]
		public override OperateResult Write(string address, float[] values)
		{
			return WriteTag(address, 202, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt64Array", "")]
		public override OperateResult Write(string address, long[] values)
		{
			return WriteTag(address, 197, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt64Array", "")]
		public override OperateResult Write(string address, ulong[] values)
		{
			return WriteTag(address, 201, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		[HslMqttApi("WriteDoubleArray", "")]
		public override OperateResult Write(string address, double[] values)
		{
			return WriteTag(address, 203, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, string value, Encoding encoding)
		{
			if (string.IsNullOrEmpty(value))
			{
				value = string.Empty;
			}
			byte[] array = SoftBasic.SpliceArray<byte>(new byte[2], SoftBasic.ArrayExpandToLengthEven(encoding.GetBytes(value)));
			array[0] = BitConverter.GetBytes(array.Length - 2)[0];
			array[1] = BitConverter.GetBytes(array.Length - 2)[1];
			return base.WriteTag(address, 208, array);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByte", "")]
		public override OperateResult Write(string address, byte value)
		{
			return WriteTag(address, 209, new byte[2] { value, 0 });
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			if (length > 1)
			{
				return await ReadAsync(new string[1] { address }, new int[1] { 1 });
			}
			return await ReadAsync(new string[1] { address }, new int[1] { length });
		}

		/// <inheritdoc />
		public override async Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt16(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt16(m, (startIndex >= 0) ? (startIndex * 2) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt16(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt16(m, (startIndex >= 0) ? (startIndex * 2) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt32(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt32(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt32(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt32(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransSingle(m, (startIndex >= 0) ? (startIndex * 4) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt64(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransInt64(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt64(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransUInt64(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			if (length == 1)
			{
				return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
			}
			int startIndex = HslHelper.ExtractStartIndex(ref address);
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, 1), (byte[] m) => base.ByteTransform.TransDouble(m, (startIndex >= 0) ? (startIndex * 8) : 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> read = await ReadAsync(address, length);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			return OperateResult.CreateSuccessResult(encoding.GetString(count: base.ByteTransform.TransUInt16(read.Content, 0), bytes: read.Content, index: 2));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Int16[])" />
		public override async Task<OperateResult> WriteAsync(string address, short[] values)
		{
			return await WriteTagAsync(address, 195, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.UInt16[])" />
		public override async Task<OperateResult> WriteAsync(string address, ushort[] values)
		{
			return await WriteTagAsync(address, 199, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Int32[])" />
		public override async Task<OperateResult> WriteAsync(string address, int[] values)
		{
			return await WriteTagAsync(address, 196, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.UInt32[])" />
		public override async Task<OperateResult> WriteAsync(string address, uint[] values)
		{
			return await WriteTagAsync(address, 200, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Single[])" />
		public override async Task<OperateResult> WriteAsync(string address, float[] values)
		{
			return await WriteTagAsync(address, 202, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Int64[])" />
		public override async Task<OperateResult> WriteAsync(string address, long[] values)
		{
			return await WriteTagAsync(address, 197, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.UInt64[])" />
		public override async Task<OperateResult> WriteAsync(string address, ulong[] values)
		{
			return await WriteTagAsync(address, 201, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Double[])" />
		public override async Task<OperateResult> WriteAsync(string address, double[] values)
		{
			return await WriteTagAsync(address, 203, base.ByteTransform.TransByte(values));
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, string value, Encoding encoding)
		{
			if (string.IsNullOrEmpty(value))
			{
				value = string.Empty;
			}
			byte[] data = SoftBasic.SpliceArray<byte>(new byte[2], SoftBasic.ArrayExpandToLengthEven(Encoding.ASCII.GetBytes(value)));
			data[0] = BitConverter.GetBytes(data.Length - 2)[0];
			data[1] = BitConverter.GetBytes(data.Length - 2)[1];
			return await WriteTagAsync(address, 208, data);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronCipNet.Write(System.String,System.Byte)" />
		public override async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteTagAsync(address, 209, new byte[2] { value, 0 });
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronCipNet[{IpAddress}:{Port}]";
		}
	}
}

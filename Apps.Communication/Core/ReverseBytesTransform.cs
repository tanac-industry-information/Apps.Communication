using System;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 字节倒序的转换类，字节的顺序和C#的原生字节的顺序是完全相反的，高字节在前，低字节在后。<br />
	/// In the reverse byte order conversion class, the byte order is completely opposite to the native byte order of C#, 
	/// with the high byte first and the low byte following.
	/// </summary>
	/// <remarks>
	/// 适用西门子PLC的S7协议的数据转换
	/// </remarks>
	public class ReverseBytesTransform : ByteTransformBase
	{
		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor" />
		public ReverseBytesTransform()
		{
			base.DataFormat = DataFormat.ABCD;
		}

		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor(Communication.Core.DataFormat)" />
		public ReverseBytesTransform(DataFormat dataFormat)
			: base(dataFormat)
		{
			base.DataFormat = DataFormat.ABCD;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransInt16(System.Byte[],System.Int32)" />
		public override short TransInt16(byte[] buffer, int index)
		{
			return BitConverter.ToInt16(new byte[2]
			{
				buffer[1 + index],
				buffer[index]
			}, 0);
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransUInt16(System.Byte[],System.Int32)" />
		public override ushort TransUInt16(byte[] buffer, int index)
		{
			return BitConverter.ToUInt16(new byte[2]
			{
				buffer[1 + index],
				buffer[index]
			}, 0);
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransByte(System.Int16[])" />
		public override byte[] TransByte(short[] values)
		{
			if (values == null)
			{
				return null;
			}
			byte[] array = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] bytes = BitConverter.GetBytes(values[i]);
				Array.Reverse(bytes);
				bytes.CopyTo(array, 2 * i);
			}
			return array;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransByte(System.UInt16[])" />
		public override byte[] TransByte(ushort[] values)
		{
			if (values == null)
			{
				return null;
			}
			byte[] array = new byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				byte[] bytes = BitConverter.GetBytes(values[i]);
				Array.Reverse(bytes);
				bytes.CopyTo(array, 2 * i);
			}
			return array;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.CreateByDateFormat(Communication.Core.DataFormat)" />
		public override IByteTransform CreateByDateFormat(DataFormat dataFormat)
		{
			return new ReverseBytesTransform(dataFormat)
			{
				IsStringReverseByteWord = base.IsStringReverseByteWord
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ReverseBytesTransform[{base.DataFormat}]";
		}
	}
}

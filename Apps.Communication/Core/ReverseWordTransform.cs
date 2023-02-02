using System;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 按照字节错位的数据转换类<br />
	/// Data conversion class according to byte misalignment
	/// </summary>
	public class ReverseWordTransform : ByteTransformBase
	{
		/// <summary>
		/// 获取或设置双字节的整数是否进行翻转操作，主要针对的类型为 <see cref="T:System.Int16" /> 和 <see cref="T:System.UInt16" /><br />
		/// Get or set whether the double-byte integer is to be flipped, the main types are <see cref="T:System.Int16" /> and <see cref="T:System.UInt16" />
		/// </summary>
		/// <remarks>
		/// 默认为 <c>True</c>，即发生数据翻转，当修改为 <c>False</c> 时，和C#的字节顺序一致
		/// </remarks>
		public bool IsInteger16Reverse { get; set; }

		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor" />
		public ReverseWordTransform()
		{
			base.DataFormat = DataFormat.ABCD;
			IsInteger16Reverse = true;
		}

		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor(Communication.Core.DataFormat)" />
		public ReverseWordTransform(DataFormat dataFormat)
			: base(dataFormat)
		{
			IsInteger16Reverse = true;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransInt16(System.Byte[],System.Int32)" />
		public override short TransInt16(byte[] buffer, int index)
		{
			if (IsInteger16Reverse)
			{
				return BitConverter.ToInt16(new byte[2]
				{
					buffer[index + 1],
					buffer[index]
				}, 0);
			}
			return base.TransInt16(buffer, index);
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransUInt16(System.Byte[],System.Int32)" />
		public override ushort TransUInt16(byte[] buffer, int index)
		{
			if (IsInteger16Reverse)
			{
				return BitConverter.ToUInt16(new byte[2]
				{
					buffer[index + 1],
					buffer[index]
				}, 0);
			}
			return base.TransUInt16(buffer, index);
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransByte(System.Int16[])" />
		public override byte[] TransByte(short[] values)
		{
			byte[] array = base.TransByte(values);
			return IsInteger16Reverse ? SoftBasic.BytesReverseByWord(array) : array;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.TransByte(System.UInt16[])" />
		public override byte[] TransByte(ushort[] values)
		{
			byte[] array = base.TransByte(values);
			return IsInteger16Reverse ? SoftBasic.BytesReverseByWord(array) : array;
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.CreateByDateFormat(Communication.Core.DataFormat)" />
		public override IByteTransform CreateByDateFormat(DataFormat dataFormat)
		{
			return new ReverseWordTransform(dataFormat)
			{
				IsStringReverseByteWord = base.IsStringReverseByteWord,
				IsInteger16Reverse = IsInteger16Reverse
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ReverseWordTransform[{base.DataFormat}]";
		}
	}
}

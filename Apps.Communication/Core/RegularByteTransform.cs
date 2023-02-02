namespace Apps.Communication.Core
{
	/// <summary>
	/// 常规的字节转换类<br />
	/// Regular byte conversion class
	/// </summary>
	public class RegularByteTransform : ByteTransformBase
	{
		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor" />
		public RegularByteTransform()
		{
		}

		/// <inheritdoc cref="M:Communication.Core.ByteTransformBase.#ctor(Communication.Core.DataFormat)" />
		public RegularByteTransform(DataFormat dataFormat)
			: base(dataFormat)
		{
		}

		/// <inheritdoc cref="M:Communication.Core.IByteTransform.CreateByDateFormat(Communication.Core.DataFormat)" />
		public override IByteTransform CreateByDateFormat(DataFormat dataFormat)
		{
			return new RegularByteTransform(dataFormat)
			{
				IsStringReverseByteWord = base.IsStringReverseByteWord
			};
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"RegularByteTransform[{base.DataFormat}]";
		}
	}
}

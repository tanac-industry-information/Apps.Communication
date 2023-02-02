using System.IO;
using Apps.Communication.Core;
using Apps.Communication.Profinet.Yamatake.Helper;
using Apps.Communication.Serial;

namespace Apps.Communication.Profinet.Yamatake
{
	/// <summary>
	/// 日本山武的数字指示调节器，目前适配SDC40B
	/// </summary>
	public class DigitronCPL : SerialDeviceBase
	{
		/// <summary>
		/// 获取或设置当前的站号信息
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public DigitronCPL()
		{
			Station = 1;
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override bool CheckReceiveDataComplete(MemoryStream ms)
		{
			byte[] array = ms.ToArray();
			if (array.Length > 5)
			{
				return array[array.Length - 2] == 13 && array[array.Length - 1] == 10;
			}
			return base.CheckReceiveDataComplete(ms);
		}

		/// <inheritdoc />
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = DigitronCPLHelper.BuildReadCommand(station, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return DigitronCPLHelper.ExtraActualResponse(operateResult2.Content);
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = DigitronCPLHelper.BuildWriteCommand(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return DigitronCPLHelper.ExtraActualResponse(operateResult2.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DigitronCPL[{base.PortName}:{base.BaudRate}]";
		}
	}
}

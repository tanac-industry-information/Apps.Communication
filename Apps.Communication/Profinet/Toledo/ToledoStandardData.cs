using System.Text;
using Apps.Communication.BasicFramework;
using Newtonsoft.Json;

namespace Apps.Communication.Profinet.Toledo
{
	/// <summary>
	/// 托利多标准格式的数据类对象
	/// </summary>
	public class ToledoStandardData
	{
		/// <summary>
		/// 为 True 则是净重，为 False 则为毛重
		/// </summary>
		public bool Suttle { get; set; }

		/// <summary>
		/// 为 True 则是正，为 False 则为负
		/// </summary>
		public bool Symbol { get; set; }

		/// <summary>
		/// 是否在范围之外
		/// </summary>
		public bool BeyondScope { get; set; }

		/// <summary>
		/// 是否为动态，为 True 则是动态，为 False 则为稳态
		/// </summary>
		public bool DynamicState { get; set; }

		/// <summary>
		/// 单位
		/// </summary>
		public string Unit { get; set; }

		/// <summary>
		/// 是否打印
		/// </summary>
		public bool IsPrint { get; set; }

		/// <summary>
		/// 是否10被扩展
		/// </summary>
		public bool IsTenExtend { get; set; }

		/// <summary>
		/// 重量
		/// </summary>
		public float Weight { get; set; }

		/// <summary>
		/// 皮重
		/// </summary>
		public float Tare { get; set; }

		/// <summary>
		/// 解析数据的原始字节
		/// </summary>
		[JsonIgnore]
		public byte[] SourceData { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ToledoStandardData()
		{
		}

		/// <summary>
		/// 从缓存里加载一个标准格式的对象
		/// </summary>
		/// <param name="buffer">缓存</param>
		public ToledoStandardData(byte[] buffer)
		{
			Weight = float.Parse(Encoding.ASCII.GetString(buffer, 4, 6));
			Tare = float.Parse(Encoding.ASCII.GetString(buffer, 10, 6));
			switch (buffer[1] & 7)
			{
			case 0:
				Weight *= 100f;
				Tare *= 100f;
				break;
			case 1:
				Weight *= 10f;
				Tare *= 10f;
				break;
			case 3:
				Weight /= 10f;
				Tare /= 10f;
				break;
			case 4:
				Weight /= 100f;
				Tare /= 100f;
				break;
			case 5:
				Weight /= 1000f;
				Tare /= 1000f;
				break;
			case 6:
				Weight /= 10000f;
				Tare /= 10000f;
				break;
			case 7:
				Weight /= 100000f;
				Tare /= 100000f;
				break;
			}
			Suttle = SoftBasic.BoolOnByteIndex(buffer[2], 0);
			Symbol = SoftBasic.BoolOnByteIndex(buffer[2], 1);
			BeyondScope = SoftBasic.BoolOnByteIndex(buffer[2], 2);
			DynamicState = SoftBasic.BoolOnByteIndex(buffer[2], 3);
			switch (buffer[3] & 7)
			{
			case 0:
				Unit = (SoftBasic.BoolOnByteIndex(buffer[2], 4) ? "kg" : "lb");
				break;
			case 1:
				Unit = "g";
				break;
			case 2:
				Unit = "t";
				break;
			case 3:
				Unit = "oz";
				break;
			case 4:
				Unit = "ozt";
				break;
			case 5:
				Unit = "dwt";
				break;
			case 6:
				Unit = "ton";
				break;
			case 7:
				Unit = "newton";
				break;
			}
			IsPrint = SoftBasic.BoolOnByteIndex(buffer[3], 3);
			IsTenExtend = SoftBasic.BoolOnByteIndex(buffer[3], 4);
			SourceData = buffer;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ToledoStandardData[{Weight}]";
		}
	}
}

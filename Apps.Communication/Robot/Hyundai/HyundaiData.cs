using System;
using System.Text;
using Apps.Communication.BasicFramework;

namespace Apps.Communication.Robot.Hyundai
{
	/// <summary>
	/// Hyundai的数据类对象
	/// </summary>
	public class HyundaiData
	{
		/// <summary>
		/// 命令码，从控制器发数据到PC和PC到控制器，两者的命令不一样
		/// </summary>
		public char Command { get; set; }

		/// <summary>
		/// 虚标记
		/// </summary>
		public string CharDummy { get; set; }

		/// <summary>
		/// 状态码
		/// </summary>
		public int State { get; set; }

		/// <summary>
		/// 标记数据，从PLC发送给机器人的数据，原封不动的返回
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// 虚标记
		/// </summary>
		public int IntDummy { get; set; }

		/// <summary>
		/// 关节坐标数据，包含X,Y,Z,W,P,R，三个位置数据，三个角度数据。
		/// </summary>
		public double[] Data { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public HyundaiData()
		{
			Data = new double[6];
		}

		/// <summary>
		/// 通过缓存对象实例化一个
		/// </summary>
		/// <param name="buffer"></param>
		public HyundaiData(byte[] buffer)
		{
			LoadBy(buffer);
		}

		/// <summary>
		/// 从字节数组的指定索引开始加载现在机器人的数据
		/// </summary>
		/// <param name="buffer">原始的字节数据</param>
		/// <param name="index">起始的索引信息</param>
		public void LoadBy(byte[] buffer, int index = 0)
		{
			Command = (char)buffer[index];
			CharDummy = Encoding.ASCII.GetString(buffer, index + 1, 3);
			State = BitConverter.ToInt32(buffer, index + 4);
			Count = BitConverter.ToInt32(buffer, index + 8);
			IntDummy = BitConverter.ToInt32(buffer, index + 12);
			Data = new double[6];
			for (int i = 0; i < Data.Length; i++)
			{
				if (i < 3)
				{
					Data[i] = BitConverter.ToDouble(buffer, index + 16 + 8 * i) * 1000.0;
				}
				else
				{
					Data[i] = BitConverter.ToDouble(buffer, index + 16 + 8 * i) * 180.0 / Math.PI;
				}
			}
		}

		/// <summary>
		/// 将现代机器人的数据转换为字节数组
		/// </summary>
		/// <returns>字节数组</returns>
		public byte[] ToBytes()
		{
			byte[] array = new byte[64];
			array[0] = (byte)Command;
			if (!string.IsNullOrEmpty(CharDummy))
			{
				Encoding.ASCII.GetBytes(CharDummy).CopyTo(array, 1);
			}
			BitConverter.GetBytes(State).CopyTo(array, 4);
			BitConverter.GetBytes(Count).CopyTo(array, 8);
			BitConverter.GetBytes(IntDummy).CopyTo(array, 12);
			for (int i = 0; i < Data.Length; i++)
			{
				if (i < 3)
				{
					BitConverter.GetBytes(Data[i] / 1000.0).CopyTo(array, 16 + 8 * i);
				}
				else
				{
					BitConverter.GetBytes(Data[i] * Math.PI / 180.0).CopyTo(array, 16 + 8 * i);
				}
			}
			return array;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"HyundaiData:Cmd[{Command},{CharDummy},{State},{Count},{IntDummy}] Data:{SoftBasic.ArrayFormat(Data)}";
		}
	}
}

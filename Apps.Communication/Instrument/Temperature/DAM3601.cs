using System.Linq;
using Apps.Communication.ModBus;

namespace Apps.Communication.Instrument.Temperature
{
	/// <summary>
	/// 阿尔泰科技发展有限公司的DAM3601温度采集模块，基于ModbusRtu开发完成。
	/// </summary>
	/// <remarks>
	/// 该温度采集模块是基于modbus-rtu，但不是标准的modbus协议，存在一些小误差，需要重写实现，并且提供了基础的数据转换
	/// </remarks>
	public class DAM3601 : ModbusRtu
	{
		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public DAM3601()
		{
			base.SleepTime = 200;
		}

		/// <summary>
		/// 使用站号实例化默认的对象
		/// </summary>
		/// <param name="station">站号信息</param>
		public DAM3601(byte station)
			: base(station)
		{
			base.SleepTime = 200;
		}

		/// <summary>
		/// 读取所有的温度数据，并转化成相关的信息
		/// </summary>
		/// <returns>结果数据对象</returns>
		public OperateResult<float[]> ReadAllTemperature()
		{
			string address = "x=4;1";
			if (base.AddressStartWithZero)
			{
				address = "x=4;0";
			}
			OperateResult<short[]> operateResult = ReadInt16(address, 128);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<float[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content.Select((short m) => TransformValue(m)).ToArray());
		}

		/// <summary>
		/// 数据转换方法，将读取的值，
		/// </summary>
		/// <param name="value">读取的值</param>
		/// <returns>转换后的值</returns>
		private float TransformValue(short value)
		{
			if ((value & 0x800) > 0)
			{
				return (float)(((value & 0xFFF) ^ 0xFFF) + 1) * -0.0625f;
			}
			return (float)(value & 0x7FF) * 0.0625f;
		}

		/// <summary>
		/// 从Modbus服务器批量读取寄存器的信息，需要指定起始地址，读取长度
		/// </summary>
		/// <param name="address">起始地址，格式为"1234"，或者是带功能码格式x=3;1234</param>
		/// <param name="length">读取的数量</param>
		/// <returns>带有成功标志的字节信息</returns>
		/// <example>
		/// 此处演示批量读取的示例
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\Modbus.cs" region="ReadExample2" title="Read示例" />
		/// </example>
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			OperateResult<byte[][]> operateResult = ModbusInfo.BuildReadModbusCommand(address, length, base.Station, base.AddressStartWithZero, 16);
			if (!operateResult.IsSuccess)
			{
				return operateResult.ConvertFailed<byte[]>();
			}
			return ReadFromCoreServer(operateResult.Content[0]);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DAM3601[{base.PortName}:{base.BaudRate}]";
		}
	}
}

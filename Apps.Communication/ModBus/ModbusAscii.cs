using System.IO;

namespace Apps.Communication.ModBus
{
	/// <summary>
	/// Modbus-Ascii通讯协议的类库，基于rtu类库完善过来，支持标准的功能码，也支持扩展的功能码实现，地址采用富文本的形式，详细见备注说明<br />
	/// The client communication class of Modbus-Ascii protocol is convenient for data interaction with the server. It supports standard function codes and also supports extended function codes. 
	/// The address is in rich text. For details, see the remarks.
	/// </summary>
	/// <remarks>
	/// 本客户端支持的标准的modbus协议，Modbus-Tcp及Modbus-Udp内置的消息号会进行自增，地址支持富文本格式，具体参考示例代码。<br />
	/// 读取线圈，输入线圈，寄存器，输入寄存器的方法中的读取长度对商业授权用户不限制，内部自动切割读取，结果合并。
	/// </remarks>
	/// <example>
	/// 基本的用法请参照下面的代码示例，初始化部分的代码省略
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\ModbusAsciiExample.cs" region="Example" title="Modbus示例" />
	/// 复杂的读取数据的代码示例如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\ModbusAsciiExample.cs" region="ReadExample" title="read示例" />
	/// 写入数据的代码如下：
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\ModbusAsciiExample.cs" region="WriteExample" title="write示例" />
	/// </example>
	public class ModbusAscii : ModbusRtu
	{
		/// <summary>
		/// 实例化一个Modbus-ascii协议的客户端对象<br />
		/// Instantiate a client object of the Modbus-ascii protocol
		/// </summary>
		public ModbusAscii()
		{
			LogMsgFormatBinary = false;
		}

		/// <inheritdoc cref="M:Communication.ModBus.ModbusRtu.#ctor(System.Byte)" />
		public ModbusAscii(byte station = 1)
			: base(station)
		{
			LogMsgFormatBinary = false;
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return ModbusInfo.TransModbusCoreToAsciiPackCommand(command);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			OperateResult<byte[]> operateResult = ModbusInfo.TransAsciiPackCommandToCore(response);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			if (send[1] + 128 == operateResult.Content[1])
			{
				return new OperateResult<byte[]>(operateResult.Content[2], ModbusInfo.GetDescriptionByErrorCode(operateResult.Content[2]));
			}
			return ModbusInfo.ExtractActualData(operateResult.Content);
		}

		/// <inheritdoc />
		protected override bool CheckReceiveDataComplete(MemoryStream ms)
		{
			return ModbusInfo.CheckAsciiReceiveDataComplete(ms.ToArray());
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"ModbusAscii[{base.PortName}:{base.BaudRate}]";
		}
	}
}

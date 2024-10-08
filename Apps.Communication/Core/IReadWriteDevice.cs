using System.Threading.Tasks;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 用于读写的设备接口，相较于<see cref="T:Communication.Core.IReadWriteNet" />，增加了<see cref="M:Communication.Core.IReadWriteDevice.ReadFromCoreServer(System.Byte[])" />相关的方法，可以用来和设备进行额外的交互。<br />
	/// The device interface used for reading and writing. Compared with <see cref="T:Communication.Core.IReadWriteNet" />, 
	/// a method related to <see cref="M:Communication.Core.IReadWriteDevice.ReadFromCoreServer(System.Byte[])" /> is added, which can be used for additional interaction with the device .
	/// </summary>
	public interface IReadWriteDevice : IReadWriteNet
	{
		/// <summary>
		/// 将当前的数据报文发送到设备去，具体使用什么通信方式取决于设备信息，然后从设备接收数据回来，并返回给调用者。<br />
		/// Send the current data message to the device, the specific communication method used depends on the device information, and then receive the data back from the device and return it to the caller.
		/// </summary>
		/// <param name="send">发送的完整的报文信息</param>
		/// <returns>接收的完整的报文信息</returns>
		/// <remarks>
		/// 本方法用于实现本组件还未实现的一些报文功能，例如有些modbus服务器会有一些特殊的功能码支持，需要收发特殊的报文，详细请看示例
		/// </remarks>
		OperateResult<byte[]> ReadFromCoreServer(byte[] send);

		/// <inheritdoc cref="M:Communication.Core.IReadWriteDevice.ReadFromCoreServer(System.Byte[])" />
		Task<OperateResult<byte[]>> ReadFromCoreServerAsync(byte[] send);
	}
}

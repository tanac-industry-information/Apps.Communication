using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.Pipe;
using Apps.Communication.LogNet;
using Apps.Communication.Reflection;

namespace Apps.Communication.Serial
{
	/// <summary>
	/// 所有串行通信类的基类，提供了一些基础的服务，核心的通信实现<br />
	/// The base class of all serial communication classes provides some basic services for the core communication implementation
	/// </summary>
	public class SerialBase : IDisposable
	{
		/// <inheritdoc cref="F:Communication.Core.Net.NetworkDoubleBase.LogMsgFormatBinary" />
		protected bool LogMsgFormatBinary = true;

		private bool disposedValue = false;

		/// <summary>
		/// 串口交互的核心
		/// </summary>
		protected PipeSerial pipeSerial = null;

		/// <summary>
		/// 从串口中至少接收的字节长度信息
		/// </summary>
		protected int AtLeastReceiveLength = 1;

		private ILogNet logNet;

		private int receiveTimeout = 5000;

		private int sleepTime = 20;

		private bool isClearCacheBeforeRead = false;

		private int connectErrorCount = 0;

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkBase.LogNet" />
		public ILogNet LogNet
		{
			get
			{

		
				return logNet;
			}
			set
			{
				logNet = value;
			}
		}

		/// <inheritdoc cref="P:Communication.Core.Pipe.PipeSerial.RtsEnable" />
		[HslMqttApi(Description = "Gets or sets a value indicating whether the request sending (RTS) signal is enabled in serial communication.")]
		public bool RtsEnable
		{
			get
			{
				return pipeSerial.RtsEnable;
			}
			set
			{
				pipeSerial.RtsEnable = value;
			}
		}

		/// <summary>
		/// 接收数据的超时时间，默认5000ms<br />
		/// Timeout for receiving data, default is 5000ms
		/// </summary>
		[HslMqttApi(Description = "Timeout for receiving data, default is 5000ms")]
		public int ReceiveTimeout
		{
			get
			{
				return receiveTimeout;
			}
			set
			{
				receiveTimeout = value;
			}
		}

		/// <summary>
		/// 连续串口缓冲数据检测的间隔时间，默认20ms，该值越小，通信速度越快，但是越不稳定。<br />
		/// Continuous serial port buffer data detection interval, the default 20ms, the smaller the value, the faster the communication, but the more unstable.
		/// </summary>
		[HslMqttApi(Description = "Continuous serial port buffer data detection interval, the default 20ms, the smaller the value, the faster the communication, but the more unstable.")]
		public int SleepTime
		{
			get
			{
				return sleepTime;
			}
			set
			{
				if (value > 0)
				{
					sleepTime = value;
				}
			}
		}

		/// <summary>
		/// 是否在发送数据前清空缓冲数据，默认是false<br />
		/// Whether to empty the buffer before sending data, the default is false
		/// </summary>
		[HslMqttApi(Description = "Whether to empty the buffer before sending data, the default is false")]
		public bool IsClearCacheBeforeRead
		{
			get
			{
				return isClearCacheBeforeRead;
			}
			set
			{
				isClearCacheBeforeRead = value;
			}
		}

		/// <summary>
		/// 当前连接串口信息的端口号名称<br />
		/// The port name of the current connection serial port information
		/// </summary>
		[HslMqttApi(Description = "The port name of the current connection serial port information")]
		public string PortName { get; private set; }

		/// <summary>
		/// 当前连接串口信息的波特率<br />
		/// Baud rate of current connection serial port information
		/// </summary>
		[HslMqttApi(Description = "Baud rate of current connection serial port information")]
		public int BaudRate { get; private set; }

		/// <summary>
		/// 实例化一个无参的构造方法<br />
		/// Instantiate a parameterless constructor
		/// </summary>
		public SerialBase()
		{
			pipeSerial = new PipeSerial();
		}

		/// <summary>
		/// 设置一个新的串口管道，一般来说不需要调用本方法，当多个串口设备共用一个COM口时才需要使用本方法进行设置共享的管道。<br />
		/// To set a new serial port pipe, generally speaking, you do not need to call this method. 
		/// This method is only needed to set the shared pipe when multiple serial devices share the same COM port.
		/// </summary>
		/// <remarks>
		/// 如果需要设置共享的串口管道的话，需要是设备类对象实例化之后立即进行设置，如果在串口的初始化之后再设置操作，串口的初始化可能会失效。<br />
		/// If you need to set a shared serial port pipeline, you need to set it immediately after the device class object is instantiated. 
		/// If you set the operation after the initialization of the serial port, the initialization of the serial port may fail.
		/// </remarks>
		/// <param name="pipeSerial">共享的串口管道信息</param>
		public void SetPipeSerial(PipeSerial pipeSerial)
		{
			if (pipeSerial != null)
			{
				this.pipeSerial = pipeSerial;
			}
		}

		/// <summary>
		/// 初始化串口信息，9600波特率，8位数据位，1位停止位，无奇偶校验<br />
		/// Initial serial port information, 9600 baud rate, 8 data bits, 1 stop bit, no parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		public virtual void SerialPortInni(string portName)
		{
			SerialPortInni(portName, 9600);
		}

		/// <summary>
		/// 初始化串口信息，波特率，8位数据位，1位停止位，无奇偶校验<br />
		/// Initializes serial port information, baud rate, 8-bit data bit, 1-bit stop bit, no parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		public virtual void SerialPortInni(string portName, int baudRate)
		{
			SerialPortInni(portName, baudRate, 8, StopBits.One, Parity.None);
		}

		/// <summary>
		/// 初始化串口信息，波特率，数据位，停止位，奇偶校验需要全部自己来指定<br />
		/// Start serial port information, baud rate, data bit, stop bit, parity all need to be specified
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		/// <param name="dataBits">数据位</param>
		/// <param name="stopBits">停止位</param>
		/// <param name="parity">奇偶校验</param>
		public virtual void SerialPortInni(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
		{
			pipeSerial.SerialPortInni(portName, baudRate, dataBits, stopBits, parity);
			PortName = portName;
			BaudRate = baudRate;
		}

		/// <summary>
		/// 根据自定义初始化方法进行初始化串口信息<br />
		/// Initialize the serial port information according to the custom initialization method
		/// </summary>
		/// <param name="initi">初始化的委托方法</param>
		public void SerialPortInni(Action<SerialPort> initi)
		{
			pipeSerial.SerialPortInni(initi);
			PortName = pipeSerial.GetPipe().PortName;
			BaudRate = pipeSerial.GetPipe().BaudRate;
		}

		/// <summary>
		/// 打开一个新的串行端口连接<br />
		/// Open a new serial port connection
		/// </summary>
		public OperateResult Open()
		{
			OperateResult operateResult = pipeSerial.Open();
			if (!operateResult.IsSuccess)
			{
				if (connectErrorCount < 100000000)
				{
					connectErrorCount++;
				}
				return new OperateResult(-connectErrorCount, operateResult.Message);
			}
			return InitializationOnOpen(pipeSerial.GetPipe());
		}

		/// <summary>
		/// 获取一个值，指示串口是否处于打开状态<br />
		/// Gets a value indicating whether the serial port is open
		/// </summary>
		/// <returns>是或否</returns>
		public bool IsOpen()
		{
			return pipeSerial.GetPipe().IsOpen;
		}

		/// <summary>
		/// 关闭当前的串口连接<br />
		/// Close the current serial connection
		/// </summary>
		public void Close()
		{
			pipeSerial.Close(ExtraOnClose);
		}

		/// <summary>
		/// 将原始的字节数据发送到串口，然后从串口接收一条数据。<br />
		/// The raw byte data is sent to the serial port, and then a piece of data is received from the serial port.
		/// </summary>
		/// <param name="send">发送的原始字节数据</param>
		/// <returns>带接收字节的结果对象</returns>
		[HslMqttApi(Description = "The raw byte data is sent to the serial port, and then a piece of data is received from the serial port.")]
		public virtual OperateResult<byte[]> ReadFromCoreServer(byte[] send)
		{
			return ReadFromCoreServer(send, hasResponseData: true);
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.PackCommandWithHeader(System.Byte[])" />
		protected virtual byte[] PackCommandWithHeader(byte[] command)
		{
			return command;
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.UnpackResponseContent(System.Byte[],System.Byte[])" />
		protected virtual OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return OperateResult.CreateSuccessResult(response);
		}

		/// <summary>
		/// 将原始的字节数据发送到串口，然后从串口接收一条数据。<br />
		/// The raw byte data is sent to the serial port, and then a piece of data is received from the serial port.
		/// </summary>
		/// <param name="send">发送的原始字节数据</param>
		/// <param name="hasResponseData">是否有数据相应，如果为true, 需要等待数据返回，如果为false, 不需要等待数据返回</param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="M:Communication.Serial.SerialBase.PackCommandWithHeader(System.Byte[])" />方法后才会有影响</param>
		/// <returns>带接收字节的结果对象</returns>
		public OperateResult<byte[]> ReadFromCoreServer(byte[] send, bool hasResponseData, bool usePackAndUnpack = true)
		{
			pipeSerial.PipeLockEnter();
			try
			{
				OperateResult operateResult = Open();
				if (!operateResult.IsSuccess)
				{
					pipeSerial.PipeLockLeave();
					return OperateResult.CreateFailedResult<byte[]>(operateResult);
				}
				OperateResult<byte[]> result = ReadFromCoreServer(pipeSerial.GetPipe(), send, hasResponseData, usePackAndUnpack);
				pipeSerial.PipeLockLeave();
				return result;
			}
			catch
			{
				pipeSerial.PipeLockLeave();
				throw;
			}
		}

		/// <summary>
		/// 将数据发送到当前的串口通道上去，并且从串口通道接收一串原始的字节报文，默认对方必须返回数据，也可以手动修改不返回数据信息。<br />
		/// Send data to the current serial channel, and receive a string of original byte messages from the serial channel. By default, the other party must return data, or you can manually modify it to not return data information.
		/// </summary>
		/// <param name="sp">指定的串口通信对象，最终将使用该串口进行数据的收发</param>
		/// <param name="send">发送到串口的报文数据信息，如果<paramref name="usePackAndUnpack" />为<c>True</c>，那么就使用<see cref="M:Communication.Serial.SerialBase.PackCommandWithHeader(System.Byte[])" />方法打包发送的报文信息。</param>
		/// <param name="hasResponseData">是否等待数据的返回，默认为 <c>True</c></param>
		/// <param name="usePackAndUnpack">是否需要对命令重新打包，在重写<see cref="M:Communication.Serial.SerialBase.PackCommandWithHeader(System.Byte[])" />方法后才会有影响</param>
		/// <returns>接收的完整的报文信息</returns>
		public virtual OperateResult<byte[]> ReadFromCoreServer(SerialPort sp, byte[] send, bool hasResponseData = true, bool usePackAndUnpack = true)
		{
			byte[] array = (usePackAndUnpack ? PackCommandWithHeader(send) : send);
			LogNet?.WriteDebug(ToString(), StringResources.Language.Send + " : " + (LogMsgFormatBinary ? array.ToHexString(' ') : Encoding.ASCII.GetString(array)));
			if (IsClearCacheBeforeRead)
			{
				ClearSerialCache();
			}
			OperateResult operateResult = SPSend(sp, array);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			if (!hasResponseData)
			{
				return OperateResult.CreateSuccessResult(new byte[0]);
			}
			OperateResult<byte[]> operateResult2 = SPReceived(sp, awaitData: true);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			LogNet?.WriteDebug(ToString(), StringResources.Language.Receive + " : " + (LogMsgFormatBinary ? operateResult2.Content.ToHexString(' ') : SoftBasic.GetAsciiStringRender(operateResult2.Content)));
			return usePackAndUnpack ? UnpackResponseContent(array, operateResult2.Content) : operateResult2;
		}

		/// <summary>
		/// 清除串口缓冲区的数据，并返回该数据，如果缓冲区没有数据，返回的字节数组长度为0<br />
		/// The number sent clears the data in the serial port buffer and returns that data, or if there is no data in the buffer, the length of the byte array returned is 0
		/// </summary>
		/// <returns>是否操作成功的方法</returns>
		public OperateResult<byte[]> ClearSerialCache()
		{
			return SPReceived(pipeSerial.GetPipe(), awaitData: false);
		}

		/// <inheritdoc cref="M:Communication.Serial.SerialBase.ReadFromCoreServer(System.Byte[])" />
		public virtual async Task<OperateResult<byte[]>> ReadFromCoreServerAsync(byte[] value)
		{
			return await Task.Run(() => ReadFromCoreServer(value));
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.InitializationOnConnect(System.Net.Sockets.Socket)" />
		protected virtual OperateResult InitializationOnOpen(SerialPort sp)
		{
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Core.Net.NetworkDoubleBase.ExtraOnDisconnect(System.Net.Sockets.Socket)" />
		protected virtual OperateResult ExtraOnClose(SerialPort sp)
		{
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 发送数据到串口去。<br />
		/// Send data to serial port.
		/// </summary>
		/// <param name="serialPort">串口对象</param>
		/// <param name="data">字节数据</param>
		/// <returns>是否发送成功</returns>
		protected virtual OperateResult SPSend(SerialPort serialPort, byte[] data)
		{
			if (data != null && data.Length != 0)
			{
				try
				{
					serialPort.Write(data, 0, data.Length);
					return OperateResult.CreateSuccessResult();
				}
				catch (Exception ex)
				{
					if (connectErrorCount < 100000000)
					{
						connectErrorCount++;
					}
					return new OperateResult(-connectErrorCount, ex.Message);
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 检查当前从串口接收的数据是否是完整的，如果是完整的，则需要返回 <c>True</c>，串口数据接收立即完成，默认返回 <c>False</c><br />
		/// Check whether the data currently received from the serial port is complete. If it is complete, you need to return <c>True</c>. 
		/// The serial port data reception is completed immediately, and the default returns <c>False</c>
		/// </summary>
		/// <remarks>
		/// 在默认情况下，串口在接收数据之后，需要再等一个 <see cref="P:Communication.Serial.SerialBase.SleepTime" /> 的时间，再没有接收到数据，才真的表明数据接收完成了，
		/// 但是在某些情况下，可以判断是否接收完成，然后直接返回，不需要在等一个 <see cref="P:Communication.Serial.SerialBase.SleepTime" /> 的时间，从而提高一倍的通信性能。<br />
		/// By default, after the serial port receives data, it needs to wait another <see cref="P:Communication.Serial.SerialBase.SleepTime" /> time, and no more data is received, 
		/// it really indicates that the data reception is complete, but in some cases, you can Judge whether the reception is complete, 
		/// and then return directly. There is no need to wait for a <see cref="P:Communication.Serial.SerialBase.SleepTime" /> time, 
		/// thereby doubling the communication performance.
		/// </remarks>
		/// <param name="ms">目前已经接收到数据流</param>
		/// <returns>如果数据接收完成，则返回True, 否则返回False</returns>
		protected virtual bool CheckReceiveDataComplete(MemoryStream ms)
		{
			return false;
		}

		/// <summary>
		/// 从串口接收一串字节数据信息，直到没有数据为止，如果参数awaitData为false, 第一轮接收没有数据则返回<br />
		/// Receives a string of bytes of data information from the serial port until there is no data, and returns if the parameter awaitData is false
		/// </summary>
		/// <param name="serialPort">串口对象</param>
		/// <param name="awaitData">是否必须要等待数据返回</param>
		/// <returns>结果数据对象</returns>
		protected virtual OperateResult<byte[]> SPReceived(SerialPort serialPort, bool awaitData)
		{
			byte[] array = new byte[1024];
			MemoryStream memoryStream = new MemoryStream();
			DateTime now = DateTime.Now;
			while (true)
			{
				Thread.Sleep(sleepTime);
				try
				{
					if (serialPort.BytesToRead < 1)
					{
						if ((DateTime.Now - now).TotalMilliseconds > (double)ReceiveTimeout)
						{
							memoryStream.Dispose();
							if (connectErrorCount < 100000000)
							{
								connectErrorCount++;
							}
							return new OperateResult<byte[]>(-connectErrorCount, $"Time out: {ReceiveTimeout}");
						}
						if (memoryStream.Length < AtLeastReceiveLength && awaitData)
						{
							continue;
						}
					}
					else
					{
						int num = serialPort.Read(array, 0, array.Length);
						if (num > 0)
						{
							memoryStream.Write(array, 0, num);
						}
						if (!CheckReceiveDataComplete(memoryStream))
						{
							continue;
						}
					}
				}
				catch (Exception ex)
				{
					memoryStream.Dispose();
					if (connectErrorCount < 100000000)
					{
						connectErrorCount++;
					}
					return new OperateResult<byte[]>(-connectErrorCount, ex.Message);
				}
				break;
			}
			connectErrorCount = 0;
			return OperateResult.CreateSuccessResult(memoryStream.ToArray());
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		/// <param name="disposing">是否在</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					pipeSerial?.Dispose();
				}
				disposedValue = true;
			}
		}

		/// <summary>
		/// 释放当前的对象
		/// </summary>
		public void Dispose()
		{
			Dispose(disposing: true);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"SerialBase{pipeSerial}";
		}
	}
}

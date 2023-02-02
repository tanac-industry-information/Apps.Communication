using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using Apps.Communication.LogNet;

namespace Apps.Communication.Profinet.Toledo
{
	/// <summary>
	/// 托利多电子秤的串口服务器对象
	/// </summary>
	public class ToledoSerial
	{
		/// <summary>
		/// 托利多数据接收时的委托
		/// </summary>
		/// <param name="sender">数据发送对象</param>
		/// <param name="toledoStandardData">数据对象</param>
		public delegate void ToledoStandardDataReceivedDelegate(object sender, ToledoStandardData toledoStandardData);

		private SerialPort serialPort;

		private ILogNet logNet;

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

		/// <summary>
		/// 获取或设置一个值，该值指示在串行通信中是否启用请求发送 (RTS) 信号。<br />
		/// Gets or sets a value indicating whether the request sending (RTS) signal is enabled in serial communication.
		/// </summary>
		public bool RtsEnable
		{
			get
			{
				return serialPort.RtsEnable;
			}
			set
			{
				serialPort.RtsEnable = value;
			}
		}

		/// <summary>
		/// 当前连接串口信息的端口号名称<br />
		/// The port name of the current connection serial port information
		/// </summary>
		public string PortName { get; private set; }

		/// <summary>
		/// 当前连接串口信息的波特率<br />
		/// Baud rate of current connection serial port information
		/// </summary>
		public int BaudRate { get; private set; }

		/// <summary>
		/// 当接收到一条新的托利多的数据的时候触发
		/// </summary>
		public event ToledoStandardDataReceivedDelegate OnToledoStandardDataReceived;

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public ToledoSerial()
		{
			serialPort = new SerialPort();
			serialPort.RtsEnable = true;
			serialPort.DataReceived += SerialPort_DataReceived;
		}

		private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			List<byte> list = new List<byte>();
			byte[] array = new byte[1024];
			while (true)
			{
				Thread.Sleep(20);
				if (serialPort.BytesToRead < 1)
				{
					break;
				}
				try
				{
					int num = serialPort.Read(array, 0, Math.Min(serialPort.BytesToRead, array.Length));
					byte[] array2 = new byte[num];
					Array.Copy(array, 0, array2, 0, num);
					list.AddRange(array2);
				}
				catch (Exception ex)
				{
					logNet?.WriteException(ToString(), "SerialPort_DataReceived", ex);
					return;
				}
			}
			if (list.Count != 0)
			{
				this.OnToledoStandardDataReceived?.Invoke(this, new ToledoStandardData(list.ToArray()));
			}
		}

		/// <summary>
		/// 初始化串口信息，9600波特率，8位数据位，1位停止位，无奇偶校验<br />
		/// Initial serial port information, 9600 baud rate, 8 data bits, 1 stop bit, no parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		public void SerialPortInni(string portName)
		{
			SerialPortInni(portName, 9600);
		}

		/// <summary>
		/// 初始化串口信息，波特率，8位数据位，1位停止位，无奇偶校验<br />
		/// Initializes serial port information, baud rate, 8-bit data bit, 1-bit stop bit, no parity
		/// </summary>
		/// <param name="portName">端口号信息，例如"COM3"</param>
		/// <param name="baudRate">波特率</param>
		public void SerialPortInni(string portName, int baudRate)
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
		public void SerialPortInni(string portName, int baudRate, int dataBits, StopBits stopBits, Parity parity)
		{
			if (!serialPort.IsOpen)
			{
				serialPort.PortName = portName;
				serialPort.BaudRate = baudRate;
				serialPort.DataBits = dataBits;
				serialPort.StopBits = stopBits;
				serialPort.Parity = parity;
				PortName = serialPort.PortName;
				BaudRate = serialPort.BaudRate;
			}
		}

		/// <summary>
		/// 根据自定义初始化方法进行初始化串口信息<br />
		/// Initialize the serial port information according to the custom initialization method
		/// </summary>
		/// <param name="initi">初始化的委托方法</param>
		public void SerialPortInni(Action<SerialPort> initi)
		{
			if (!serialPort.IsOpen)
			{
				serialPort.PortName = "COM5";
				serialPort.BaudRate = 9600;
				serialPort.DataBits = 8;
				serialPort.StopBits = StopBits.One;
				serialPort.Parity = Parity.None;
				initi(serialPort);
				PortName = serialPort.PortName;
				BaudRate = serialPort.BaudRate;
			}
		}

		/// <summary>
		/// 打开一个新的串行端口连接<br />
		/// Open a new serial port connection
		/// </summary>
		public void Open()
		{
			if (!serialPort.IsOpen)
			{
				serialPort.Open();
			}
		}

		/// <summary>
		/// 获取一个值，指示串口是否处于打开状态<br />
		/// Gets a value indicating whether the serial port is open
		/// </summary>
		/// <returns>是或否</returns>
		public bool IsOpen()
		{
			return serialPort.IsOpen;
		}

		/// <summary>
		/// 关闭当前的串口连接<br />
		/// Close the current serial connection
		/// </summary>
		public void Close()
		{
			if (serialPort.IsOpen)
			{
				serialPort.Close();
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString();
		}
	}
}

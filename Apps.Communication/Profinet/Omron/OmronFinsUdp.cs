using System;
using System.Text;
using Apps.Communication.Core;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.Omron
{
	/// <summary>
	/// 欧姆龙的Udp协议的实现类，地址类型和Fins-TCP一致，无连接的实现，可靠性不如<see cref="T:Communication.Profinet.Omron.OmronFinsNet" /><br />
	/// Omron's Udp protocol implementation class, the address type is the same as Fins-TCP, 
	/// and the connectionless implementation is not as reliable as <see cref="T:Communication.Profinet.Omron.OmronFinsNet" />
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="T:Communication.Profinet.Omron.OmronFinsNet" path="remarks" />
	/// </remarks>
	public class OmronFinsUdp : NetworkUdpDeviceBase
	{
		/// <inheritdoc />
		public override string IpAddress
		{
			get
			{
				return base.IpAddress;
			}
			set
			{
				base.IpAddress = value;
				DA1 = Convert.ToByte(base.IpAddress.Substring(base.IpAddress.LastIndexOf(".") + 1));
			}
		}

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.ICF" />
		public byte ICF { get; set; } = 128;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.RSV" />
		public byte RSV { get; private set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.GCT" />
		public byte GCT { get; set; } = 2;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.DNA" />
		public byte DNA { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.DA1" />
		public byte DA1 { get; set; } = 19;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.DA2" />
		public byte DA2 { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SNA" />
		public byte SNA { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SA1" />
		public byte SA1 { get; set; } = 13;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SA2" />
		public byte SA2 { get; set; }

		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.SID" />
		public byte SID { get; set; } = 0;


		/// <inheritdoc cref="P:Communication.Profinet.Omron.OmronFinsNet.ReadSplits" />
		public int ReadSplits { get; set; } = 500;


		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.#ctor(System.String,System.Int32)" />
		public OmronFinsUdp(string ipAddress, int port)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.#ctor" />
		public OmronFinsUdp()
		{
			base.WordLength = 1;
			base.ByteTransform = new ReverseWordTransform();
			base.ByteTransform.DataFormat = DataFormat.CDAB;
			base.ByteTransform.IsStringReverseByteWord = true;
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.PackCommand(System.Byte[])" />
		private byte[] PackCommand(byte[] cmd)
		{
			byte[] array = new byte[10 + cmd.Length];
			array[0] = ICF;
			array[1] = RSV;
			array[2] = GCT;
			array[3] = DNA;
			array[4] = DA1;
			array[5] = DA2;
			array[6] = SNA;
			array[7] = SA1;
			array[8] = SA2;
			array[9] = SID;
			cmd.CopyTo(array, 10);
			return array;
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return PackCommand(command);
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> UnpackResponseContent(byte[] send, byte[] response)
		{
			return OmronFinsNetHelper.UdpResponseValidAnalysis(response);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Read(System.String,System.UInt16)" />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			return OmronFinsNetHelper.Read(this, address, length, ReadSplits);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Byte[])" />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return OmronFinsNetHelper.Write(this, address, value);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadString", "")]
		public override OperateResult<string> ReadString(string address, ushort length)
		{
			return base.ReadString(address, length, Encoding.UTF8);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteString", "")]
		public override OperateResult Write(string address, string value)
		{
			return base.Write(address, value, Encoding.UTF8);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.ReadBool(System.String,System.UInt16)" />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return OmronFinsNetHelper.ReadBool(this, address, length, ReadSplits);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNet.Write(System.String,System.Boolean[])" />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] values)
		{
			return OmronFinsNetHelper.Write(this, address, values);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Run(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi(ApiTopic = "Run", Description = "将CPU单元的操作模式更改为RUN，从而使PLC能够执行其程序。")]
		public OperateResult Run()
		{
			return OmronFinsNetHelper.Run(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.Stop(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi(ApiTopic = "Stop", Description = "将CPU单元的操作模式更改为PROGRAM，停止程序执行。")]
		public OperateResult Stop()
		{
			return OmronFinsNetHelper.Stop(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.ReadCpuUnitData(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi(ApiTopic = "ReadCpuUnitData", Description = "读取CPU的一些数据信息，主要包含型号，版本，一些数据块的大小。")]
		public OperateResult<OmronCpuUnitData> ReadCpuUnitData()
		{
			return OmronFinsNetHelper.ReadCpuUnitData(this);
		}

		/// <inheritdoc cref="M:Communication.Profinet.Omron.OmronFinsNetHelper.ReadCpuUnitStatus(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi(ApiTopic = "ReadCpuUnitStatus", Description = "读取CPU单元的一些操作状态数据，主要包含运行状态，工作模式，错误信息等。")]
		public OperateResult<OmronCpuUnitStatus> ReadCpuUnitStatus()
		{
			return OmronFinsNetHelper.ReadCpuUnitStatus(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"OmronFinsUdp[{IpAddress}:{Port}]";
		}
	}
}

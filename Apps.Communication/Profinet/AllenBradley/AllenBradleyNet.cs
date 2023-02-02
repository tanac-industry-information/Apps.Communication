using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.AllenBradley
{
	/// <summary>
	/// AB PLC的数据通信类，使用CIP协议实现，适用1756，1769等型号，支持使用标签的形式进行读写操作，支持标量数据，一维数组，二维数组，三维数组等等。如果是局部变量，那么使用 Program:MainProgram.[变量名]。<br />
	/// The data communication class of AB PLC is implemented using the CIP protocol. It is suitable for 1756, 1769 and other models. 
	/// It supports reading and writing in the form of tags, scalar data, one-dimensional array, two-dimensional array, 
	/// three-dimensional array, and so on. If it is a local variable, use the Program:MainProgram.[Variable name].
	/// </summary>
	/// <remarks>
	/// thanks 江阴-  ∮溪风-⊙_⌒ help test the dll
	/// <br />
	/// thanks 上海-null 测试了这个dll
	/// <br />
	/// <br />
	/// 默认的地址就是PLC里的TAG名字，比如A，B，C；如果你需要读取的数据是一个数组，那么A就是默认的A[0]，如果想要读取偏移量为10的数据，那么地址为A[10]，
	/// 多维数组同理，使用A[10,10,10]的操作。
	/// <br />
	/// <br />
	/// 假设你读取的是局部变量，那么使用 Program:MainProgram.变量名<br />
	/// 目前适用的系列为1756 ControlLogix, 1756 GuardLogix, 1769 CompactLogix, 1769 Compact GuardLogix, 1789SoftLogix, 5069 CompactLogix, 5069 Compact GuardLogix, Studio 5000 Logix Emulate
	/// <br />
	/// <br />
	/// 如果你有个Bool数组要读取，变量名为 A, 那么读第0个位，可以通过 ReadBool("A")，但是第二个位需要使用<br />
	/// ReadBoolArray("A[0]")   // 返回32个bool长度，0-31的索引，如果我想读取32-63的位索引，就需要 ReadBoolArray("A[1]") ，以此类推。
	/// <br />
	/// <br />
	/// 地址可以携带站号信息，只要在前面加上slot=2;即可，这就是访问站号2的数据了，例如 slot=2;AAA
	/// </remarks>
	public class AllenBradleyNet : NetworkDeviceBase
	{
		/// <summary>
		/// The current session handle, which is determined by the PLC when communicating with the PLC handshake
		/// </summary>
		public uint SessionHandle { get; protected set; }

		/// <summary>
		/// Gets or sets the slot number information for the current plc, which should be set before connections
		/// </summary>
		public byte Slot { get; set; } = 0;


		/// <summary>
		/// port and slot information
		/// </summary>
		public byte[] PortSlot { get; set; }

		/// <summary>
		/// 获取或设置整个交互指令的控制码，默认为0x6F，通常不需要修改<br />
		/// Gets or sets the control code of the entire interactive instruction. The default is 0x6F, and usually does not need to be modified.
		/// </summary>
		public ushort CipCommand { get; set; } = 111;


		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		public AllenBradleyNet()
		{
			base.WordLength = 2;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <summary>
		/// Instantiate a communication object for a Allenbradley PLC protocol
		/// </summary>
		/// <param name="ipAddress">PLC IpAddress</param>
		/// <param name="port">PLC Port</param>
		public AllenBradleyNet(string ipAddress, int port = 44818)
			: this()
		{
			IpAddress = ipAddress;
			Port = port;
		}

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new AllenBradleyMessage();
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return AllenBradleyHelper.PackRequestHeader(CipCommand, SessionHandle, command);
		}

		/// <inheritdoc />
		protected override OperateResult InitializationOnConnect(Socket socket)
		{
			OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, AllenBradleyHelper.RegisterSessionHandle(), hasResponseData: true, usePackAndUnpack: false);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult operateResult2 = AllenBradleyHelper.CheckResponse(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			SessionHandle = base.ByteTransform.TransUInt32(operateResult.Content, 4);
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override OperateResult ExtraOnDisconnect(Socket socket)
		{
			if (socket != null)
			{
				OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, AllenBradleyHelper.UnRegisterSessionHandle(SessionHandle), hasResponseData: true, usePackAndUnpack: false);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.RegisterSessionHandle(), hasResponseData: true, usePackAndUnpack: false);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			SessionHandle = base.ByteTransform.TransUInt32(read.Content, 4);
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> ExtraOnDisconnectAsync(Socket socket)
		{
			if (socket != null)
			{
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.UnRegisterSessionHandle(SessionHandle), hasResponseData: true, usePackAndUnpack: false);
				if (!read.IsSuccess)
				{
					return read;
				}
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 创建一个读取标签的报文指定，标签地址可以手动动态指定slot编号，例如 slot=2;AAA<br />
		/// Build a read command bytes, The label address can manually specify the slot number dynamically, for example slot=2;AAA
		/// </summary>
		/// <param name="address">the address of the tag name</param>
		/// <param name="length">Array information, if not arrays, is 1 </param>
		/// <returns>Message information that contains the result object </returns>
		public virtual OperateResult<byte[]> BuildReadCommand(string[] address, int[] length)
		{
			if (address == null || length == null)
			{
				return new OperateResult<byte[]>("address or length is null");
			}
			if (address.Length != length.Length)
			{
				return new OperateResult<byte[]>("address and length is not same array");
			}
			try
			{
				byte b = Slot;
				List<byte[]> list = new List<byte[]>();
				for (int i = 0; i < address.Length; i++)
				{
					b = (byte)HslHelper.ExtractParameter(ref address[i], "slot", Slot);
					list.Add(AllenBradleyHelper.PackRequsetRead(address[i], length[i]));
				}
				byte[] value = AllenBradleyHelper.PackCommandSpecificData(new byte[4], PackCommandService(PortSlot ?? new byte[2] { 1, b }, list.ToArray()));
				return OperateResult.CreateSuccessResult(value);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
			}
		}

		/// <summary>
		/// 创建一个读取多标签的报文<br />
		/// Build a read command bytes
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <returns>Message information that contains the result object </returns>
		public OperateResult<byte[]> BuildReadCommand(string[] address)
		{
			if (address == null)
			{
				return new OperateResult<byte[]>("address or length is null");
			}
			int[] array = new int[address.Length];
			for (int i = 0; i < address.Length; i++)
			{
				array[i] = 1;
			}
			return BuildReadCommand(address, array);
		}

		/// <summary>
		/// Create a written message instruction
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <param name="typeCode">Data type</param>
		/// <param name="data">Source Data </param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Message information that contains the result object</returns>
		protected virtual OperateResult<byte[]> BuildWriteCommand(string address, ushort typeCode, byte[] data, int length = 1)
		{
			try
			{
				byte b = (byte)HslHelper.ExtractParameter(ref address, "slot", Slot);
				byte[] array = AllenBradleyHelper.PackRequestWrite(address, typeCode, data, length);
				byte[] value = AllenBradleyHelper.PackCommandSpecificData(new byte[4], PackCommandService(PortSlot ?? new byte[2] { 1, b }, array));
				return OperateResult.CreateSuccessResult(value);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
			}
		}

		/// <summary>
		/// Create a written message instruction
		/// </summary>
		/// <param name="address">The address of the tag name </param>
		/// <param name="data">Bool Data </param>
		/// <returns>Message information that contains the result object</returns>
		public OperateResult<byte[]> BuildWriteCommand(string address, bool data)
		{
			try
			{
				byte b = (byte)HslHelper.ExtractParameter(ref address, "slot", Slot);
				byte[] array = AllenBradleyHelper.PackRequestWrite(address, data);
				byte[] value = AllenBradleyHelper.PackCommandSpecificData(new byte[4], PackCommandService(PortSlot ?? new byte[2] { 1, b }, array));
				return OperateResult.CreateSuccessResult(value);
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
			}
		}

		/// <summary>
		/// Read data information, data length for read array length information
		/// </summary>
		/// <param name="address">Address format of the node</param>
		/// <param name="length">In the case of arrays, the length of the array </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			if (length > 1)
			{
				return ReadSegment(address, 0, length);
			}
			return Read(new string[1] { address }, new int[1] { length });
		}

		/// <summary>
		/// Bulk read Data information
		/// </summary>
		/// <param name="address">Name of the node </param>
		/// <returns>Result data with result object </returns>
		[HslMqttApi("ReadAddress", "")]
		public OperateResult<byte[]> Read(string[] address)
		{
			if (address == null)
			{
				return new OperateResult<byte[]>("address can not be null");
			}
			int[] array = new int[address.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = 1;
			}
			return Read(address, array);
		}

		/// <summary>
		/// <b>[商业授权]</b> 批量读取多地址的数据信息，例如我可以读取两个标签的数据 "A","B[0]"， 长度为 [1, 5]，返回的是一整个的字节数组，需要自行解析<br />
		/// <b>[Authorization]</b> Read the data information of multiple addresses in batches. For example, I can read the data "A", "B[0]" of two tags, 
		/// the length is [1, 5], and the return is an entire byte array, and I need to do it myself Parsing
		/// </summary>
		/// <param name="address">节点的名称 -&gt; Name of the node </param>
		/// <param name="length">如果是数组，就为数组长度 -&gt; In the case of arrays, the length of the array </param>
		/// <returns>带有结果对象的结果数据 -&gt; Result data with result object </returns>
		public OperateResult<byte[]> Read(string[] address, int[] length)
		{
			if (address != null && address.Length > 1)
			{
				return new OperateResult<byte[]>(StringResources.Language.InsufficientPrivileges);
			}
			OperateResult<byte[], ushort, bool> operateResult = ReadWithType(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content1);
		}

		private OperateResult<byte[], ushort, bool> ReadWithType(string[] address, int[] length)
		{
			OperateResult<byte[]> operateResult = BuildReadCommand(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(operateResult2);
			}
			OperateResult operateResult3 = AllenBradleyHelper.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(operateResult3);
			}
			return AllenBradleyHelper.ExtractActualData(operateResult2.Content, isRead: true);
		}

		/// <summary>
		/// Read Segment Data Array form plc, use address tag name
		/// </summary>
		/// <param name="address">Tag name in plc</param>
		/// <param name="startIndex">array start index, uint byte index</param>
		/// <param name="length">array length, data item length</param>
		/// <returns>Results Bytes</returns>
		[HslMqttApi("ReadSegment", "")]
		public OperateResult<byte[]> ReadSegment(string address, int startIndex, int length)
		{
			try
			{
				List<byte> list = new List<byte>();
				OperateResult<byte[], ushort, bool> operateResult2;
				do
				{
					OperateResult<byte[]> operateResult = ReadCipFromServer(AllenBradleyHelper.PackRequestReadSegment(address, startIndex, length));
					if (!operateResult.IsSuccess)
					{
						return operateResult;
					}
					operateResult2 = AllenBradleyHelper.ExtractActualData(operateResult.Content, isRead: true);
					if (!operateResult2.IsSuccess)
					{
						return OperateResult.CreateFailedResult<byte[]>(operateResult2);
					}
					startIndex += operateResult2.Content1.Length;
					list.AddRange(operateResult2.Content1);
				}
				while (operateResult2.Content3);
				return OperateResult.CreateSuccessResult(list.ToArray());
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
			}
		}

		private OperateResult<byte[]> ReadByCips(params byte[][] cips)
		{
			OperateResult<byte[]> operateResult = ReadCipFromServer(cips);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[], ushort, bool> operateResult2 = AllenBradleyHelper.ExtractActualData(operateResult.Content, isRead: true);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(operateResult2.Content1);
		}

		/// <summary>
		/// 使用CIP报文和服务器进行核心的数据交换
		/// </summary>
		/// <param name="cips">Cip commands</param>
		/// <returns>Results Bytes</returns>
		public OperateResult<byte[]> ReadCipFromServer(params byte[][] cips)
		{
			byte[] send = AllenBradleyHelper.PackCommandSpecificData(new byte[4], PackCommandService(PortSlot ?? new byte[2] { 1, Slot }, cips.ToArray()));
			OperateResult<byte[]> operateResult = ReadFromCoreServer(send);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult operateResult2 = AllenBradleyHelper.CheckResponse(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content);
		}

		/// <summary>
		/// 使用EIP报文和服务器进行核心的数据交换
		/// </summary>
		/// <param name="eip">eip commands</param>
		/// <returns>Results Bytes</returns>
		public OperateResult<byte[]> ReadEipFromServer(params byte[][] eip)
		{
			byte[] send = AllenBradleyHelper.PackCommandSpecificData(eip);
			OperateResult<byte[]> operateResult = ReadFromCoreServer(send);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult operateResult2 = AllenBradleyHelper.CheckResponse(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content);
		}

		/// <summary>
		/// 读取单个的bool数据信息，如果读取的是单bool变量，就直接写变量名，如果是由int组成的bool数组的一个值，一律带"i="开头访问，例如"i=A[0]" <br />
		/// Read a single bool data information, if it is a single bool variable, write the variable name directly, 
		/// if it is a value of a bool array composed of int, it is always accessed with "i=" at the beginning, for example, "i=A[0]"
		/// </summary>
		/// <param name="address">节点的名称 -&gt; Name of the node </param>
		/// <returns>带有结果对象的结果数据 -&gt; Result data with result info </returns>
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			if (address.StartsWith("i="))
			{
				address = address.Substring(2);
				address = AllenBradleyHelper.AnalysisArrayIndex(address, out var arrayIndex);
				string text = ((arrayIndex / 32 == 0) ? "" : $"[{arrayIndex / 32}]");
				OperateResult<bool[]> operateResult = ReadBoolArray(address + text);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool>(operateResult);
				}
				return OperateResult.CreateSuccessResult(operateResult.Content[arrayIndex % 32]);
			}
			OperateResult<byte[]> operateResult2 = Read(address, 1);
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(operateResult2);
			}
			return OperateResult.CreateSuccessResult(base.ByteTransform.TransBool(operateResult2.Content, 0));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			OperateResult<byte[]> operateResult = Read(address, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(operateResult.Content, length));
		}

		/// <summary>
		/// 批量读取的bool数组信息，如果你有个Bool数组变量名为 A, 那么读第0个位，可以通过 ReadBool("A")，但是第二个位需要使用 
		/// ReadBoolArray("A[0]")   // 返回32个bool长度，0-31的索引，如果我想读取32-63的位索引，就需要 ReadBoolArray("A[1]") ，以此类推。<br />
		/// For batch read bool array information, if you have a Bool array variable named A, then you can read the 0th bit through ReadBool("A"), 
		/// but the second bit needs to use ReadBoolArray("A[0]" ) // Returns the length of 32 bools, the index is 0-31, 
		/// if I want to read the bit index of 32-63, I need ReadBoolArray("A[1]"), and so on.
		/// </summary>
		/// <param name="address">节点的名称 -&gt; Name of the node </param>
		/// <returns>带有结果对象的结果数据 -&gt; Result data with result info </returns>
		[HslMqttApi("ReadBoolArrayAddress", "")]
		public OperateResult<bool[]> ReadBoolArray(string address)
		{
			OperateResult<byte[]> operateResult = Read(address, 1);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content.ToBoolArray());
		}

		/// <summary>
		/// 读取PLC的byte类型的数据<br />
		/// Read the byte type of PLC data
		/// </summary>
		/// <param name="address">节点的名称 -&gt; Name of the node </param>
		/// <returns>带有结果对象的结果数据 -&gt; Result data with result info </returns>
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <summary>
		/// 从PLC里读取一个指定标签名的原始数据信息及其数据类型信息<br />
		/// Read the original data information of a specified tag name and its data type information from the PLC
		/// </summary>
		/// <remarks>
		/// 数据类型的定义，可以参考 <see cref="T:Communication.Profinet.AllenBradley.AllenBradleyHelper" /> 的常量资源信息。
		/// </remarks>
		/// <param name="address">PLC的标签地址信息</param>
		/// <param name="length">读取的数据长度</param>
		/// <returns>包含原始数据信息及数据类型的结果对象</returns>
		public OperateResult<ushort, byte[]> ReadTag(string address, int length = 1)
		{
			OperateResult<byte[], ushort, bool> operateResult = ReadWithType(new string[1] { address }, new int[1] { length });
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, byte[]>(operateResult);
			}
			return OperateResult.CreateSuccessResult(operateResult.Content2, operateResult.Content1);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Read(System.String,System.UInt16)" />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			if (length > 1)
			{
				return await ReadSegmentAsync(address, 0, length);
			}
			return await ReadAsync(new string[1] { address }, new int[1] { length });
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Read(System.String[])" />
		public async Task<OperateResult<byte[]>> ReadAsync(string[] address)
		{
			if (address == null)
			{
				return new OperateResult<byte[]>("address can not be null");
			}
			int[] length = new int[address.Length];
			for (int i = 0; i < length.Length; i++)
			{
				length[i] = 1;
			}
			return await ReadAsync(address, length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Read(System.String[],System.Int32[])" />
		public async Task<OperateResult<byte[]>> ReadAsync(string[] address, int[] length)
		{
			if (address != null && address.Length > 1)
			{
				return new OperateResult<byte[]>(StringResources.Language.InsufficientPrivileges);
			}
			OperateResult<byte[], ushort, bool> read = await ReadWithTypeAsync(address, length);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content1);
		}

		private async Task<OperateResult<byte[], ushort, bool>> ReadWithTypeAsync(string[] address, int[] length)
		{
			OperateResult<byte[]> command = BuildReadCommand(address, length);
			if (!command.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(command);
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(read);
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[], ushort, bool>(check);
			}
			return AllenBradleyHelper.ExtractActualData(read.Content, isRead: true);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadSegment(System.String,System.Int32,System.Int32)" />
		public async Task<OperateResult<byte[]>> ReadSegmentAsync(string address, int startIndex, int length)
		{
			try
			{
				List<byte> bytesContent = new List<byte>();
				OperateResult<byte[], ushort, bool> analysis;
				do
				{
					OperateResult<byte[]> read = await ReadCipFromServerAsync(AllenBradleyHelper.PackRequestReadSegment(address, startIndex, length));
					if (!read.IsSuccess)
					{
						return read;
					}
					analysis = AllenBradleyHelper.ExtractActualData(read.Content, isRead: true);
					if (!analysis.IsSuccess)
					{
						return OperateResult.CreateFailedResult<byte[]>(analysis);
					}
					startIndex += analysis.Content1.Length;
					bytesContent.AddRange(analysis.Content1);
				}
				while (analysis.Content3);
				return OperateResult.CreateSuccessResult(bytesContent.ToArray());
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				return new OperateResult<byte[]>("Address Wrong:" + ex.Message);
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadCipFromServer(System.Byte[][])" />
		public async Task<OperateResult<byte[]>> ReadCipFromServerAsync(params byte[][] cips)
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData(new byte[4], PackCommandService(PortSlot ?? new byte[2] { 1, Slot }, cips.ToArray()));
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(commandSpecificData);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return OperateResult.CreateSuccessResult(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadEipFromServer(System.Byte[][])" />
		public async Task<OperateResult<byte[]>> ReadEipFromServerAsync(params byte[][] eip)
		{
			byte[] commandSpecificData = AllenBradleyHelper.PackCommandSpecificData(eip);
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(commandSpecificData);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return OperateResult.CreateSuccessResult(read.Content);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadBool(System.String)" />
		public override async Task<OperateResult<bool>> ReadBoolAsync(string address)
		{
			if (address.StartsWith("i="))
			{
				address = address.Substring(2);
				address = AllenBradleyHelper.AnalysisArrayIndex(address, out var bitIndex);
				string uintIndex = ((bitIndex / 32 == 0) ? "" : $"[{bitIndex / 32}]");
				OperateResult<bool[]> read2 = await ReadBoolArrayAsync(address + uintIndex);
				if (!read2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<bool>(read2);
				}
				return OperateResult.CreateSuccessResult(read2.Content[bitIndex % 32]);
			}
			OperateResult<byte[]> read = await ReadAsync(address, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool>(read);
			}
			return OperateResult.CreateSuccessResult(base.ByteTransform.TransBool(read.Content, 0));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<bool[]>> ReadBoolAsync(string address, ushort length)
		{
			OperateResult<byte[]> read = await ReadAsync(address, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			return OperateResult.CreateSuccessResult(SoftBasic.ByteToBoolArray(read.Content, length));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadBoolArray(System.String)" />
		public async Task<OperateResult<bool[]>> ReadBoolArrayAsync(string address)
		{
			OperateResult<byte[]> read = await ReadAsync(address, 1);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<bool[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content.ToBoolArray());
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadByte(System.String)" />
		public async Task<OperateResult<byte>> ReadByteAsync(string address)
		{
			return ByteTransformHelper.GetResultFromArray(await ReadAsync(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadTag(System.String,System.Int32)" />
		public async Task<OperateResult<ushort, byte[]>> ReadTagAsync(string address, int length = 1)
		{
			OperateResult<byte[], ushort, bool> read = await ReadWithTypeAsync(new string[1] { address }, new int[1] { length });
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<ushort, byte[]>(read);
			}
			return OperateResult.CreateSuccessResult(read.Content2, read.Content1);
		}

		/// <summary>
		/// 枚举当前的所有的变量名字，包含结构体信息，除去系统自带的名称数据信息<br />
		/// Enumerate all the current variable names, including structure information, except the name data information that comes with the system
		/// </summary>
		/// <returns>结果对象</returns>
		public OperateResult<AbTagItem[]> TagEnumerator()
		{
			List<AbTagItem> list = new List<AbTagItem>();
			ushort startInstance = 0;
			while (true)
			{
				OperateResult<byte[]> operateResult = ReadCipFromServer(AllenBradleyHelper.GetEnumeratorCommand(startInstance));
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<AbTagItem[]>(operateResult);
				}
				OperateResult<byte[], ushort, bool> operateResult2 = AllenBradleyHelper.ExtractActualData(operateResult.Content, isRead: true);
				if (!operateResult2.IsSuccess)
				{
					return OperateResult.CreateFailedResult<AbTagItem[]>(operateResult2);
				}
				if (operateResult.Content.Length < 43 || BitConverter.ToUInt16(operateResult.Content, 40) != 213)
				{
					break;
				}
				int num = 44;
				while (num < operateResult.Content.Length)
				{
					AbTagItem abTagItem = new AbTagItem();
					abTagItem.InstanceID = BitConverter.ToUInt32(operateResult.Content, num);
					startInstance = (ushort)(abTagItem.InstanceID + 1);
					num += 4;
					ushort num2 = BitConverter.ToUInt16(operateResult.Content, num);
					num += 2;
					abTagItem.Name = Encoding.ASCII.GetString(operateResult.Content, num, num2);
					num += num2;
					abTagItem.SymbolType = BitConverter.ToUInt16(operateResult.Content, num);
					num += 2;
					if ((abTagItem.SymbolType & 0x1000) != 4096 && !abTagItem.Name.StartsWith("__"))
					{
						list.Add(abTagItem);
					}
				}
				if (!operateResult2.Content3)
				{
					return OperateResult.CreateSuccessResult(list.ToArray());
				}
			}
			return new OperateResult<AbTagItem[]>(StringResources.Language.UnknownError);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.TagEnumerator" />
		public async Task<OperateResult<AbTagItem[]>> TagEnumeratorAsync()
		{
			List<AbTagItem> lists = new List<AbTagItem>();
			ushort instansAddress = 0;
			while (true)
			{
				OperateResult<byte[]> readCip = await ReadCipFromServerAsync(AllenBradleyHelper.GetEnumeratorCommand(instansAddress));
				if (!readCip.IsSuccess)
				{
					return OperateResult.CreateFailedResult<AbTagItem[]>(readCip);
				}
				OperateResult<byte[], ushort, bool> analysis = AllenBradleyHelper.ExtractActualData(readCip.Content, isRead: true);
				if (!analysis.IsSuccess)
				{
					return OperateResult.CreateFailedResult<AbTagItem[]>(analysis);
				}
				if (readCip.Content.Length < 43 || BitConverter.ToUInt16(readCip.Content, 40) != 213)
				{
					break;
				}
				int index4 = 44;
				while (index4 < readCip.Content.Length)
				{
					AbTagItem td = new AbTagItem
					{
						InstanceID = BitConverter.ToUInt32(readCip.Content, index4)
					};
					instansAddress = (ushort)(td.InstanceID + 1);
					index4 += 4;
					ushort nameLen = BitConverter.ToUInt16(readCip.Content, index4);
					index4 += 2;
					td.Name = Encoding.ASCII.GetString(readCip.Content, index4, nameLen);
					index4 += nameLen;
					td.SymbolType = BitConverter.ToUInt16(readCip.Content, index4);
					index4 += 2;
					if ((td.SymbolType & 0x1000) != 4096 && !td.Name.StartsWith("__"))
					{
						lists.Add(td);
					}
				}
				if (!analysis.Content3)
				{
					return OperateResult.CreateSuccessResult(lists.ToArray());
				}
			}
			return new OperateResult<AbTagItem[]>(StringResources.Language.UnknownError);
		}

		/// <summary>
		/// 枚举结构体的方法
		/// </summary>
		/// <param name="structTag">结构体的标签</param>
		/// <returns>是否成功</returns>
		[Obsolete("未测试通过")]
		public OperateResult<AbTagItem[]> StructTagEnumerator(AbTagItem structTag)
		{
			OperateResult<AbStructHandle> operateResult = ReadTagStructHandle(structTag);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AbTagItem[]>(operateResult);
			}
			OperateResult<byte[]> operateResult2 = ReadCipFromServer(AllenBradleyHelper.GetStructItemNameType(structTag.SymbolType, operateResult.Content));
			if (!operateResult2.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AbTagItem[]>(operateResult2);
			}
			if (operateResult2.Content.Length >= 43 && operateResult2.Content[40] == 204 && operateResult2.Content[41] == 0)
			{
				byte[] bytes = BitConverter.GetBytes(structTag.SymbolType);
				bytes[1] = (byte)(bytes[1] & 0xFu);
				if (bytes[1] >= 15)
				{
					return OperateResult.CreateSuccessResult(EnumSysStructItemType(operateResult2.Content, operateResult.Content).ToArray());
				}
				return OperateResult.CreateSuccessResult(EnumUserStructItemType(operateResult2.Content, operateResult.Content).ToArray());
			}
			return new OperateResult<AbTagItem[]>(StringResources.Language.UnknownError);
		}

		private OperateResult<AbStructHandle> ReadTagStructHandle(AbTagItem structTag)
		{
			OperateResult<byte[]> operateResult = ReadByCips(AllenBradleyHelper.GetStructHandleCommand(structTag.SymbolType));
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<AbStructHandle>(operateResult);
			}
			if (operateResult.Content.Length >= 43 && BitConverter.ToInt32(operateResult.Content, 40) == 131)
			{
				AbStructHandle abStructHandle = new AbStructHandle();
				abStructHandle.Count = BitConverter.ToUInt16(operateResult.Content, 44);
				abStructHandle.TemplateObjectDefinitionSize = BitConverter.ToUInt32(operateResult.Content, 50);
				abStructHandle.TemplateStructureSize = BitConverter.ToUInt32(operateResult.Content, 58);
				abStructHandle.MemberCount = BitConverter.ToUInt16(operateResult.Content, 66);
				abStructHandle.StructureHandle = BitConverter.ToUInt16(operateResult.Content, 72);
				return OperateResult.CreateSuccessResult(abStructHandle);
			}
			return new OperateResult<AbStructHandle>(StringResources.Language.UnknownError);
		}

		private List<AbTagItem> EnumSysStructItemType(byte[] Struct_Item_Type_buff, AbStructHandle structHandle)
		{
			List<AbTagItem> list = new List<AbTagItem>();
			if (Struct_Item_Type_buff.Length > 41 && Struct_Item_Type_buff[40] == 204 && Struct_Item_Type_buff[41] == 0 && Struct_Item_Type_buff[42] == 0)
			{
				int num = Struct_Item_Type_buff.Length - 40;
				byte[] array = new byte[num - 4];
				Array.Copy(Struct_Item_Type_buff, 44, array, 0, num - 4);
				byte[] array2 = new byte[structHandle.MemberCount * 8];
				Array.Copy(array, 0, array2, 0, structHandle.MemberCount * 8);
				byte[] array3 = new byte[array.Length - array2.Length + 1];
				Array.Copy(array, array2.Length - 1, array3, 0, array.Length - array2.Length + 1);
				ushort memberCount = structHandle.MemberCount;
				for (int i = 0; i < memberCount; i++)
				{
					AbTagItem abTagItem = new AbTagItem();
					int num2;
					abTagItem.SymbolType = BitConverter.ToUInt16(array2, num2 = 8 * i + 2);
					list.Add(abTagItem);
				}
				List<int> list2 = new List<int>();
				for (int j = 0; j < array3.Length; j++)
				{
					if (array3[j] == 0)
					{
						list2.Add(j);
					}
				}
				list2.Add(array3.Length);
				for (int k = 0; k < list2.Count; k++)
				{
					if (k != 0)
					{
						int num3 = 0;
						num3 = ((k + 1 < list2.Count) ? (list2[k + 1] - list2[k] - 1) : 0);
						if (num3 > 0)
						{
							list[k - 1].Name = Encoding.ASCII.GetString(array3, list2[k] + 1, num3);
						}
					}
				}
			}
			return list;
		}

		private List<AbTagItem> EnumUserStructItemType(byte[] Struct_Item_Type_buff, AbStructHandle structHandle)
		{
			List<AbTagItem> list = new List<AbTagItem>();
			bool flag = false;
			int num = 0;
			if ((Struct_Item_Type_buff.Length > 41) & (Struct_Item_Type_buff[40] == 204) & (Struct_Item_Type_buff[41] == 0) & (Struct_Item_Type_buff[42] == 0))
			{
				int num2 = Struct_Item_Type_buff.Length - 40;
				byte[] array = new byte[num2 - 4];
				Array.ConstrainedCopy(Struct_Item_Type_buff, 44, array, 0, num2 - 4);
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == 0 && !flag)
					{
						num = i;
					}
					if (array[i] != 59 || array[i + 1] != 110)
					{
						continue;
					}
					flag = true;
					int num3 = i - num - 1;
					byte[] destinationArray = new byte[num3];
					Array.Copy(array, num + 1, destinationArray, 0, num3);
					byte[] array2 = new byte[i + 1];
					Array.Copy(array, 0, array2, 0, i + 1);
					byte[] array3 = new byte[array.Length - i - 1];
					Array.Copy(array, i + 1, array3, 0, array.Length - i - 1);
					if ((num + 1) % 8 != 0)
					{
						break;
					}
					int num4 = (num + 1) / 8 - 1;
					for (int j = 0; j <= num4; j++)
					{
						AbTagItem abTagItem = new AbTagItem();
						int num5;
						abTagItem.SymbolType = BitConverter.ToUInt16(array2, num5 = 8 * j + 2);
						list.Add(abTagItem);
					}
					List<int> list2 = new List<int>();
					for (int k = 0; k < array3.Length; k++)
					{
						if (array3[k] == 0)
						{
							list2.Add(k);
						}
					}
					list2.Add(array3.Length);
					for (int l = 0; l < list2.Count; l++)
					{
						int num6 = 0;
						num6 = ((l + 1 < list2.Count) ? (list2[l + 1] - list2[l] - 1) : 0);
						if (num6 > 0)
						{
							list[l].Name = Encoding.ASCII.GetString(array3, list2[l] + 1, num6);
						}
					}
					break;
				}
			}
			return list;
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt16Array", "")]
		public override OperateResult<short[]> ReadInt16(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransInt16(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt16Array", "")]
		public override OperateResult<ushort[]> ReadUInt16(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransUInt16(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt32Array", "")]
		public override OperateResult<int[]> ReadInt32(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransInt32(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt32Array", "")]
		public override OperateResult<uint[]> ReadUInt32(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadFloatArray", "")]
		public override OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadInt64Array", "")]
		public override OperateResult<long[]> ReadInt64(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransInt64(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadUInt64Array", "")]
		public override OperateResult<ulong[]> ReadUInt64(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadDoubleArray", "")]
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
		}

		/// <inheritdoc />
		public OperateResult<string> ReadString(string address)
		{
			return ReadString(address, 1);
		}

		/// <summary>
		/// 读取字符串数据，默认为<see cref="P:System.Text.Encoding.UTF8" />编码<br />
		/// Read string data, default is the <see cref="P:System.Text.Encoding.UTF8" /> encoding
		/// </summary>
		/// <param name="address">起始地址</param>
		/// <param name="length">数据长度</param>
		/// <returns>带有成功标识的string数据</returns>
		/// <example>
		/// 以下为三菱的连接对象示例，其他的设备读写情况参照下面的代码：
		/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetworkDeviceBase.cs" region="ReadString" title="String类型示例" />
		/// </example>
		public override OperateResult<string> ReadString(string address, ushort length)
		{
			return base.ReadString(address, length, Encoding.UTF8);
		}

		/// <inheritdoc />
		public override OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> operateResult = Read(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			try
			{
				if (operateResult.Content.Length >= 6)
				{
					int count = base.ByteTransform.TransInt32(operateResult.Content, 2);
					return OperateResult.CreateSuccessResult(encoding.GetString(operateResult.Content, 6, count));
				}
				return OperateResult.CreateSuccessResult(encoding.GetString(operateResult.Content));
			}
			catch (Exception ex)
			{
				return new OperateResult<string>(ex.Message + " Source: " + operateResult.Content.ToHexString(' '));
			}
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyHelper.ReadPlcType(Communication.Core.IReadWriteDevice)" />
		[HslMqttApi(Description = "获取PLC的型号信息")]
		public OperateResult<string> ReadPlcType()
		{
			return AllenBradleyHelper.ReadPlcType(this);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<short[]>> ReadInt16Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransInt16(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<ushort[]>> ReadUInt16Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransUInt16(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<int[]>> ReadInt32Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransInt32(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<uint[]>> ReadUInt32Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransUInt32(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<long[]>> ReadInt64Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransInt64(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<ulong[]>> ReadUInt64Async(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransUInt64(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
		}

		/// <inheritdoc />
		public async Task<OperateResult<string>> ReadStringAsync(string address)
		{
			return await ReadStringAsync(address, 1);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadString(System.String,System.UInt16)" />
		public override async Task<OperateResult<string>> ReadStringAsync(string address, ushort length)
		{
			return await ReadStringAsync(address, length, Encoding.UTF8);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<string>> ReadStringAsync(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> read = await ReadAsync(address, length);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(read);
			}
			if (read.Content.Length >= 6)
			{
				return OperateResult.CreateSuccessResult(encoding.GetString(count: base.ByteTransform.TransInt32(read.Content, 2), bytes: read.Content, index: 6));
			}
			return OperateResult.CreateSuccessResult(encoding.GetString(read.Content));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyHelper.ReadPlcType(Communication.Core.IReadWriteDevice)" />
		public async Task<OperateResult<string>> ReadPlcTypeAsync()
		{
			return await AllenBradleyHelper.ReadPlcTypeAsync(this);
		}

		/// <summary>
		/// 当前的PLC不支持该功能，需要调用 <see cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTag(System.String,System.UInt16,System.Byte[],System.Int32)" /> 方法来实现。<br />
		/// The current PLC does not support this function, you need to call the <see cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTag(System.String,System.UInt16,System.Byte[],System.Int32)" /> method to achieve it.
		/// </summary>
		/// <param name="address">地址</param>
		/// <param name="value">值</param>
		/// <returns>写入结果值</returns>
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction + " Please refer to use WriteTag instead ");
		}

		/// <summary>
		/// 使用指定的类型写入指定的节点数据<br />
		/// Writes the specified node data with the specified type
		/// </summary>
		/// <param name="address">节点的名称 -&gt; Name of the node </param>
		/// <param name="typeCode">类型代码，详细参见<see cref="T:Communication.Profinet.AllenBradley.AllenBradleyHelper" />上的常用字段 -&gt;  Type code, see the commonly used Fields section on the <see cref="T:Communication.Profinet.AllenBradley.AllenBradleyHelper" /> in detail</param>
		/// <param name="value">实际的数据值 -&gt; The actual data value </param>
		/// <param name="length">如果节点是数组，就是数组长度 -&gt; If the node is an array, it is the array length </param>
		/// <returns>是否写入成功 -&gt; Whether to write successfully</returns>
		public virtual OperateResult WriteTag(string address, ushort typeCode, byte[] value, int length = 1)
		{
			OperateResult<byte[]> operateResult = BuildWriteCommand(address, typeCode, value, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			OperateResult operateResult3 = AllenBradleyHelper.CheckResponse(operateResult2.Content);
			if (!operateResult3.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(operateResult3);
			}
			return AllenBradleyHelper.ExtractActualData(operateResult2.Content, isRead: false);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Byte[])" />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			return await Task.Run(() => Write(address, value));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTag(System.String,System.UInt16,System.Byte[],System.Int32)" />
		public virtual async Task<OperateResult> WriteTagAsync(string address, ushort typeCode, byte[] value, int length = 1)
		{
			OperateResult<byte[]> command = BuildWriteCommand(address, typeCode, value, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
			if (!check.IsSuccess)
			{
				return OperateResult.CreateFailedResult<byte[]>(check);
			}
			return AllenBradleyHelper.ExtractActualData(read.Content, isRead: false);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt16Array", "")]
		public override OperateResult Write(string address, short[] values)
		{
			return WriteTag(address, 195, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt16Array", "")]
		public override OperateResult Write(string address, ushort[] values)
		{
			return WriteTag(address, 199, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt32Array", "")]
		public override OperateResult Write(string address, int[] values)
		{
			return WriteTag(address, 196, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt32Array", "")]
		public override OperateResult Write(string address, uint[] values)
		{
			return WriteTag(address, 200, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteFloatArray", "")]
		public override OperateResult Write(string address, float[] values)
		{
			return WriteTag(address, 202, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteInt64Array", "")]
		public override OperateResult Write(string address, long[] values)
		{
			return WriteTag(address, 197, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteUInt64Array", "")]
		public override OperateResult Write(string address, ulong[] values)
		{
			return WriteTag(address, 201, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteDoubleArray", "")]
		public override OperateResult Write(string address, double[] values)
		{
			return WriteTag(address, 203, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, string value, Encoding encoding)
		{
			if (string.IsNullOrEmpty(value))
			{
				value = string.Empty;
			}
			byte[] bytes = encoding.GetBytes(value);
			OperateResult operateResult = Write(address + ".LEN", bytes.Length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			byte[] value2 = SoftBasic.ArrayExpandToLengthEven(bytes);
			return WriteTag(address + ".DATA[0]", 194, value2, bytes.Length);
		}

		/// <summary>
		/// 写入单个Bool的数据信息。如果读取的是单bool变量，就直接写变量名，如果是bool数组的一个值，一律带下标访问，例如a[0]<br />
		/// Write the data information of a single Bool. If the read is a single bool variable, write the variable name directly, 
		/// if it is a value of the bool array, it will always be accessed with a subscript, such as a[0]
		/// </summary>
		/// <param name="address">标签的地址数据</param>
		/// <param name="value">bool数据值</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			if (Regex.IsMatch(address, "\\[[0-9]+\\]$"))
			{
				OperateResult<byte[]> operateResult = BuildWriteCommand(address, value);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
				OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
				if (!operateResult2.IsSuccess)
				{
					return operateResult2;
				}
				OperateResult operateResult3 = AllenBradleyHelper.CheckResponse(operateResult2.Content);
				if (!operateResult3.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(operateResult3);
				}
				return AllenBradleyHelper.ExtractActualData(operateResult2.Content, isRead: false);
			}
			return WriteTag(address, 193, (!value) ? new byte[2] : new byte[2] { 255, 255 });
		}

		/// <summary>
		/// 写入Byte数据，返回是否写入成功<br />
		/// Write Byte data and return whether the writing is successful
		/// </summary>
		/// <param name="address">标签的地址数据</param>
		/// <param name="value">Byte数据</param>
		/// <returns>是否写入成功</returns>
		[HslMqttApi("WriteByte", "")]
		public virtual OperateResult Write(string address, byte value)
		{
			return WriteTag(address, 194, new byte[2] { value, 0 });
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Int16[])" />
		public override async Task<OperateResult> WriteAsync(string address, short[] values)
		{
			return await WriteTagAsync(address, 195, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.UInt16[])" />
		public override async Task<OperateResult> WriteAsync(string address, ushort[] values)
		{
			return await WriteTagAsync(address, 199, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Int32[])" />
		public override async Task<OperateResult> WriteAsync(string address, int[] values)
		{
			return await WriteTagAsync(address, 196, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.UInt32[])" />
		public override async Task<OperateResult> WriteAsync(string address, uint[] values)
		{
			return await WriteTagAsync(address, 200, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Single[])" />
		public override async Task<OperateResult> WriteAsync(string address, float[] values)
		{
			return await WriteTagAsync(address, 202, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Int64[])" />
		public override async Task<OperateResult> WriteAsync(string address, long[] values)
		{
			return await WriteTagAsync(address, 197, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.UInt64[])" />
		public override async Task<OperateResult> WriteAsync(string address, ulong[] values)
		{
			return await WriteTagAsync(address, 201, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Double[])" />
		public override async Task<OperateResult> WriteAsync(string address, double[] values)
		{
			return await WriteTagAsync(address, 203, base.ByteTransform.TransByte(values), values.Length);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, string value, Encoding encoding)
		{
			if (string.IsNullOrEmpty(value))
			{
				value = string.Empty;
			}
			byte[] data = encoding.GetBytes(value);
			OperateResult write = await WriteAsync(address + ".LEN", data.Length);
			if (!write.IsSuccess)
			{
				return write;
			}
			return await WriteTagAsync(value: SoftBasic.ArrayExpandToLengthEven(data), address: address + ".DATA[0]", typeCode: 194, length: data.Length);
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Boolean)" />
		public override async Task<OperateResult> WriteAsync(string address, bool value)
		{
			if (Regex.IsMatch(address, "\\[[0-9]+\\]$"))
			{
				OperateResult<byte[]> command = BuildWriteCommand(address, value);
				if (!command.IsSuccess)
				{
					return command;
				}
				OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
				if (!read.IsSuccess)
				{
					return read;
				}
				OperateResult check = AllenBradleyHelper.CheckResponse(read.Content);
				if (!check.IsSuccess)
				{
					return OperateResult.CreateFailedResult<byte[]>(check);
				}
				return AllenBradleyHelper.ExtractActualData(read.Content, isRead: false);
			}
			return await WriteTagAsync(address, 193, (!value) ? new byte[2] : new byte[2] { 255, 255 });
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Byte)" />
		public virtual async Task<OperateResult> WriteAsync(string address, byte value)
		{
			return await WriteTagAsync(address, 194, new byte[2] { value, 0 });
		}

		/// <summary>
		/// 读取指定地址的日期数据，最小日期为 1970年1月1日，当PLC的变量类型为 "Date" 和 "TimeAndDate" 时，都可以用本方法读取。<br />
		/// Read the date data of the specified address. The minimum date is January 1, 1970. When the PLC variable type is "Date" and "TimeAndDate", this method can be used to read.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <returns>日期结果对象</returns>
		public OperateResult<DateTime> ReadDate(string address)
		{
			OperateResult<long> operateResult = ReadInt64(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(operateResult);
			}
			long value = operateResult.Content / 100;
			return OperateResult.CreateSuccessResult(new DateTime(1970, 1, 1).AddTicks(value));
		}

		/// <summary>
		/// 使用日期格式（Date）将指定的数据写入到指定的地址里，PLC的地址类型变量必须为 "Date"，否则写入失败。<br />
		/// Use the date format (Date) to write the specified data to the specified address. The PLC address type variable must be "Date", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="date">时间信息</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteDate(string address, DateTime date)
		{
			long value = (date.Date - new DateTime(1970, 1, 1)).Ticks * 100;
			return WriteTag(address, 8, base.ByteTransform.TransByte(value));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteDate(System.String,System.DateTime)" />
		public OperateResult WriteTimeAndDate(string address, DateTime date)
		{
			long value = (date - new DateTime(1970, 1, 1)).Ticks * 100;
			return WriteTag(address, 10, base.ByteTransform.TransByte(value));
		}

		/// <summary>
		/// 读取指定地址的时间数据，最小时间为 0，如果获取秒，可以访问 <see cref="P:System.TimeSpan.TotalSeconds" />，当PLC的变量类型为 "Time" 和 "TimeOfDate" 时，都可以用本方法读取。<br />
		/// Read the time data of the specified address. The minimum time is 0. If you get seconds, you can access <see cref="P:System.TimeSpan.TotalSeconds" />. 
		/// When the PLC variable type is "Time" and "TimeOfDate", you can use this Method to read.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <returns>时间的结果对象</returns>
		public OperateResult<TimeSpan> ReadTime(string address)
		{
			OperateResult<long> operateResult = ReadInt64(address);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
			}
			long value = operateResult.Content / 100;
			return OperateResult.CreateSuccessResult(TimeSpan.FromTicks(value));
		}

		/// <summary>
		/// 使用时间格式（TIME）将时间数据写入到PLC中指定的地址里去，PLC的地址类型变量必须为 "TIME"，否则写入失败。<br />
		/// Use the time format (TIME) to write the time data to the address specified in the PLC. The PLC address type variable must be "TIME", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="time">时间参数变量</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteTime(string address, TimeSpan time)
		{
			return WriteTag(address, 9, base.ByteTransform.TransByte(time.Ticks * 100));
		}

		/// <summary>
		/// 使用时间格式（TimeOfDate）将时间数据写入到PLC中指定的地址里去，PLC的地址类型变量必须为 "TimeOfDate"，否则写入失败。<br />
		/// Use the time format (TimeOfDate) to write the time data to the address specified in the PLC. The PLC address type variable must be "TimeOfDate", otherwise the writing will fail.
		/// </summary>
		/// <param name="address">PLC里变量的地址</param>
		/// <param name="timeOfDate">时间参数变量</param>
		/// <returns>是否写入成功</returns>
		public OperateResult WriteTimeOfDate(string address, TimeSpan timeOfDate)
		{
			return WriteTag(address, 11, base.ByteTransform.TransByte(timeOfDate.Ticks * 100));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadDate(System.String)" />
		public async Task<OperateResult<DateTime>> ReadDateAsync(string address)
		{
			OperateResult<long> read = await ReadInt64Async(address);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<DateTime>(read);
			}
			long tick = read.Content / 100;
			return OperateResult.CreateSuccessResult(new DateTime(1970, 1, 1).AddTicks(tick));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteDate(System.String,System.DateTime)" />
		public async Task<OperateResult> WriteDateAsync(string address, DateTime date)
		{
			long tick = (date.Date - new DateTime(1970, 1, 1)).Ticks * 100;
			return await WriteTagAsync(address, 8, base.ByteTransform.TransByte(tick));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTimeAndDate(System.String,System.DateTime)" />
		public async Task<OperateResult> WriteTimeAndDateAsync(string address, DateTime date)
		{
			long tick = (date - new DateTime(1970, 1, 1)).Ticks * 100;
			return await WriteTagAsync(address, 10, base.ByteTransform.TransByte(tick));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadTime(System.String)" />
		public async Task<OperateResult<TimeSpan>> ReadTimeAsync(string address)
		{
			OperateResult<long> read = await ReadInt64Async(address);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<TimeSpan>(read);
			}
			long tick = read.Content / 100;
			return OperateResult.CreateSuccessResult(TimeSpan.FromTicks(tick));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTime(System.String,System.TimeSpan)" />
		public async Task<OperateResult> WriteTimeAsync(string address, TimeSpan time)
		{
			return await WriteTagAsync(address, 9, base.ByteTransform.TransByte(time.Ticks * 100));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.WriteTimeOfDate(System.String,System.TimeSpan)" />
		public async Task<OperateResult> WriteTimeOfDateAsync(string address, TimeSpan timeOfDate)
		{
			return await WriteTagAsync(address, 11, base.ByteTransform.TransByte(timeOfDate.Ticks * 100));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyHelper.PackCommandService(System.Byte[],System.Byte[][])" />
		protected virtual byte[] PackCommandService(byte[] portSlot, params byte[][] cips)
		{
			return AllenBradleyHelper.PackCommandService(portSlot, cips);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"AllenBradleyNet[{IpAddress}:{Port}]";
		}
	}
}

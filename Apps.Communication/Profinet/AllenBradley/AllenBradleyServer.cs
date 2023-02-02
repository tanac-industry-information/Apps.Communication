using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Reflection;

namespace Apps.Communication.Profinet.AllenBradley
{
	/// <summary>
	/// <b>[商业授权]</b> AB PLC的虚拟服务器，仅支持和HSL组件的完美通信，可以手动添加一些节点。<br />
	/// <b>[Authorization]</b> AB PLC's virtual server only supports perfect communication with HSL components. You can manually add some nodes.
	/// </summary>
	/// <remarks>
	/// 本AB的虚拟PLC仅限商业授权用户使用，感谢支持。
	/// </remarks>
	public class AllenBradleyServer : NetworkDataServerBase
	{
		private const int DataPoolLength = 65536;

		private Dictionary<string, AllenBradleyItemValue> abValues;

		private SimpleHybirdLock simpleHybird;

		private Random random = new Random();

		/// <summary>
		/// 获取或设置当前的服务器的数据字节排序情况<br />
		/// Gets or sets the data byte ordering of the current server
		/// </summary>
		public DataFormat DataFormat
		{
			get
			{
				return base.ByteTransform.DataFormat;
			}
			set
			{
				base.ByteTransform.DataFormat = value;
			}
		}

		/// <summary>
		/// 实例化一个AB PLC协议的服务器<br />
		/// Instantiate an AB PLC protocol server
		/// </summary>
		public AllenBradleyServer()
		{
			base.WordLength = 2;
			base.ByteTransform = new RegularByteTransform();
			base.Port = 44818;
			simpleHybird = new SimpleHybirdLock();
			abValues = new Dictionary<string, AllenBradleyItemValue>();
		}

		/// <summary>
		/// 向服务器新增一个新的Tag值<br />
		/// Add a new tag value to the server
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值</param>
		public void AddTagValue(string key, AllenBradleyItemValue value)
		{
			simpleHybird.Enter();
			if (abValues.ContainsKey(key))
			{
				abValues[key] = value;
			}
			else
			{
				abValues.Add(key, value);
			}
			simpleHybird.Leave();
		}

		/// <summary>
		/// 向服务器新增一个新的bool类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type bool to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, bool value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = ((!value) ? new byte[2] : new byte[2] { 255, 255 }),
				TypeLength = 2
			});
		}

		/// <summary>
		/// 向服务器新增一个新的short类型的Tag值，并赋予初始化的值<br />
		/// Add a new short tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, short value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 2
			});
		}

		/// <summary>
		/// 向服务器新增一个新的short数组的Tag值，并赋予初始化的值<br />
		/// Add a new short array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, short[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 2
			});
		}

		/// <summary>
		/// 向服务器新增一个新的ushort类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of ushort type to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, ushort value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 2
			});
		}

		/// <summary>
		/// 向服务器新增一个新的ushort数组的Tag值，并赋予初始化的值<br />
		/// Add a new ushort array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, ushort[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 2
			});
		}

		/// <summary>
		/// 向服务器新增一个新的int类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of type int to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, int value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的int数组的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of the int array to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, int[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的uint类型的Tag值，并赋予初始化的值<br />
		/// Add a new uint tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, uint value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的uint数组的Tag值，并赋予初始化的值<br />
		/// Add a new uint array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, uint[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的long类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of type long to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, long value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的long数组的Tag值，并赋予初始化的值<br />
		/// Add a new Long array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, long[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的ulong类型的Tag值，并赋予初始化的值<br />
		/// Add a new Ulong type Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, ulong value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的ulong数组的Tag值，并赋予初始化的值<br />
		/// Add a new Ulong array Tag value to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, ulong[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的float类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type float to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, float value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的float数组的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of the float array to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, float[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 4
			});
		}

		/// <summary>
		/// 向服务器新增一个新的double类型的Tag值，并赋予初始化的值<br />
		/// Add a new tag value of type double to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, double value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的double数组的Tag值，并赋予初始化的值<br />
		/// Add a new double array Tag value to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		public void AddTagValue(string key, double[] value)
		{
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = base.ByteTransform.TransByte(value),
				TypeLength = 8
			});
		}

		/// <summary>
		/// 向服务器新增一个新的string类型的Tag值，并赋予初始化的值<br />
		/// Add a new Tag value of string type to the server and assign the initial value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		/// <param name="maxLength">字符串的最长值</param>
		public void AddTagValue(string key, string value, int maxLength)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = false,
				Buffer = SoftBasic.ArrayExpandToLength(SoftBasic.SpliceArray<byte>(new byte[2], BitConverter.GetBytes(bytes.Length), Encoding.UTF8.GetBytes(value)), maxLength),
				TypeLength = maxLength
			});
		}

		/// <summary>
		/// 向服务器新增一个新的string数组的Tag值，并赋予初始化的值<br />
		/// Add a new String array Tag value to the server and assign the initialized value
		/// </summary>
		/// <param name="key">Tag名称</param>
		/// <param name="value">值信息</param>
		/// <param name="maxLength">字符串的最长值</param>
		public void AddTagValue(string key, string[] value, int maxLength)
		{
			byte[] array = new byte[maxLength * value.Length];
			for (int i = 0; i < value.Length; i++)
			{
				byte[] bytes = Encoding.UTF8.GetBytes(value[i]);
				BitConverter.GetBytes(bytes.Length).CopyTo(array, maxLength * i + 2);
				bytes.CopyTo(array, maxLength * i + 6);
			}
			AddTagValue(key, new AllenBradleyItemValue
			{
				IsArray = true,
				Buffer = array,
				TypeLength = maxLength
			});
		}

		/// <inheritdoc />
		[HslMqttApi("ReadByteArray", "")]
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			int num = 0;
			try
			{
				int num2 = address.IndexOf('[');
				int num3 = address.IndexOf(']');
				if (num2 > 0 && num3 > 0 && num3 > num2)
				{
					num = int.Parse(address.Substring(num2 + 1, num3 - num2 - 1));
					address = address.Substring(0, num2);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(ex.Message);
			}
			byte[] array = null;
			simpleHybird.Enter();
			if (abValues.ContainsKey(address))
			{
				AllenBradleyItemValue allenBradleyItemValue = abValues[address];
				if (!allenBradleyItemValue.IsArray)
				{
					array = new byte[allenBradleyItemValue.Buffer.Length];
					allenBradleyItemValue.Buffer.CopyTo(array, 0);
				}
				else if (num * allenBradleyItemValue.TypeLength + length * allenBradleyItemValue.TypeLength <= allenBradleyItemValue.Buffer.Length)
				{
					array = new byte[length * allenBradleyItemValue.TypeLength];
					Array.Copy(allenBradleyItemValue.Buffer, num * allenBradleyItemValue.TypeLength, array, 0, array.Length);
				}
			}
			simpleHybird.Leave();
			if (array == null)
			{
				return new OperateResult<byte[]>(StringResources.Language.AllenBradley04);
			}
			return OperateResult.CreateSuccessResult(array);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteByteArray", "")]
		public override OperateResult Write(string address, byte[] value)
		{
			int num = 0;
			try
			{
				int num2 = address.IndexOf('[');
				int num3 = address.IndexOf(']');
				if (num2 > 0 && num3 > 0 && num3 > num2)
				{
					num = int.Parse(address.Substring(num2 + 1, num3 - num2 - 1));
					address = address.Substring(0, num2);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
			bool flag = false;
			simpleHybird.Enter();
			if (abValues.ContainsKey(address))
			{
				AllenBradleyItemValue allenBradleyItemValue = abValues[address];
				if (!allenBradleyItemValue.IsArray)
				{
					if (allenBradleyItemValue.Buffer.Length == value.Length)
					{
						allenBradleyItemValue.Buffer = value;
						flag = true;
					}
				}
				else if (num * allenBradleyItemValue.TypeLength + value.Length <= allenBradleyItemValue.Buffer.Length)
				{
					Array.Copy(value, 0, allenBradleyItemValue.Buffer, num * allenBradleyItemValue.TypeLength, value.Length);
					flag = true;
				}
			}
			simpleHybird.Leave();
			if (!flag)
			{
				return new OperateResult(StringResources.Language.AllenBradley04);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.ReadByte(System.String)" />
		[HslMqttApi("ReadByte", "")]
		public OperateResult<byte> ReadByte(string address)
		{
			return ByteTransformHelper.GetResultFromArray(Read(address, 1));
		}

		/// <inheritdoc cref="M:Communication.Profinet.AllenBradley.AllenBradleyNet.Write(System.String,System.Byte)" />
		[HslMqttApi("WriteByte", "")]
		public OperateResult Write(string address, byte value)
		{
			return Write(address, new byte[1] { value });
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBoolArray", "")]
		public override OperateResult<bool[]> ReadBool(string address, ushort length)
		{
			return new OperateResult<bool[]>(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBoolArray", "")]
		public override OperateResult Write(string address, bool[] value)
		{
			return new OperateResult(StringResources.Language.NotSupportedFunction);
		}

		/// <inheritdoc />
		[HslMqttApi("ReadBool", "")]
		public override OperateResult<bool> ReadBool(string address)
		{
			bool flag = false;
			bool value = false;
			simpleHybird.Enter();
			if (abValues.ContainsKey(address))
			{
				flag = true;
				AllenBradleyItemValue allenBradleyItemValue = abValues[address];
				byte[] buffer = allenBradleyItemValue.Buffer;
				if (buffer != null && buffer.Length != 0)
				{
					value = allenBradleyItemValue.Buffer[0] != 0;
				}
			}
			simpleHybird.Leave();
			if (!flag)
			{
				return new OperateResult<bool>(StringResources.Language.AllenBradley04);
			}
			return OperateResult.CreateSuccessResult(value);
		}

		/// <inheritdoc />
		[HslMqttApi("WriteBool", "")]
		public override OperateResult Write(string address, bool value)
		{
			bool flag = false;
			simpleHybird.Enter();
			if (abValues.ContainsKey(address))
			{
				flag = true;
				AllenBradleyItemValue allenBradleyItemValue = abValues[address];
				byte[] buffer = allenBradleyItemValue.Buffer;
				if (buffer != null && buffer.Length != 0)
				{
					allenBradleyItemValue.Buffer[0] = (byte)(value ? byte.MaxValue : 0);
				}
				byte[] buffer2 = allenBradleyItemValue.Buffer;
				if (buffer2 != null && buffer2.Length > 1)
				{
					allenBradleyItemValue.Buffer[1] = (byte)(value ? byte.MaxValue : 0);
				}
			}
			simpleHybird.Leave();
			if (!flag)
			{
				return new OperateResult(StringResources.Language.AllenBradley04);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		public override OperateResult<string> ReadString(string address, ushort length, Encoding encoding)
		{
			OperateResult<byte[]> operateResult = Read(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(operateResult);
			}
			if (operateResult.Content.Length >= 6)
			{
				int count = BitConverter.ToInt32(operateResult.Content, 2);
				return OperateResult.CreateSuccessResult(encoding.GetString(operateResult.Content, 6, count));
			}
			return OperateResult.CreateSuccessResult(encoding.GetString(operateResult.Content));
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, string value, Encoding encoding)
		{
			bool flag = false;
			int num = 0;
			int num2 = address.IndexOf('[');
			int num3 = address.IndexOf(']');
			if (num2 > 0 && num3 > 0 && num3 > num2)
			{
				num = int.Parse(address.Substring(num2 + 1, num3 - num2 - 1));
				address = address.Substring(0, num2);
			}
			simpleHybird.Enter();
			if (abValues.ContainsKey(address))
			{
				flag = true;
				AllenBradleyItemValue allenBradleyItemValue = abValues[address];
				byte[] buffer = allenBradleyItemValue.Buffer;
				if (buffer != null && buffer.Length >= 6)
				{
					byte[] bytes = encoding.GetBytes(value);
					BitConverter.GetBytes(bytes.Length).CopyTo(allenBradleyItemValue.Buffer, 2 + num * allenBradleyItemValue.TypeLength);
					if (bytes.Length != 0)
					{
						Array.Copy(bytes, 0, allenBradleyItemValue.Buffer, 6 + num * allenBradleyItemValue.TypeLength, Math.Min(bytes.Length, allenBradleyItemValue.Buffer.Length - 6));
					}
				}
			}
			simpleHybird.Leave();
			if (!flag)
			{
				return new OperateResult<bool>(StringResources.Language.AllenBradley04);
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override void ThreadPoolLoginAfterClientCheck(Socket socket, IPEndPoint endPoint)
		{
			OperateResult<byte[]> operateResult = ReceiveByMessage(socket, 5000, new AllenBradleyMessage());
			if (!operateResult.IsSuccess)
			{
				return;
			}
			byte[] buffer = new byte[4];
			random.NextBytes(buffer);
			OperateResult operateResult2 = Send(socket, AllenBradleyHelper.PackRequestHeader(101, base.ByteTransform.TransUInt32(buffer, 0), new byte[0]));
			if (operateResult2.IsSuccess)
			{
				AppSession appSession = new AppSession(socket);
				appSession.LoginAlias = base.ByteTransform.TransUInt32(buffer, 0).ToString();
				try
				{
					socket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, appSession);
					AddClient(appSession);
				}
				catch
				{
					socket.Close();
					base.LogNet?.WriteDebug(ToString(), string.Format(StringResources.Language.ClientOfflineInfo, endPoint));
				}
			}
		}

		private async void SocketAsyncCallBack(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			AppSession session = asyncState as AppSession;
			if (session == null)
			{
				return;
			}
			try
			{
				session.WorkSocket.EndReceive(ar);
				OperateResult<byte[]> read1 = await ReceiveByMessageAsync(session.WorkSocket, 5000, new AllenBradleyMessage());
				if (!read1.IsSuccess)
				{
					RemoveClient(session);
					return;
				}

				base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Receive}：{read1.Content.ToHexString(' ')}");
				string sessionID = base.ByteTransform.TransUInt32(read1.Content, 4).ToString();
				if (sessionID != session.LoginAlias)
				{
					base.LogNet?.WriteDebug(ToString(), "SessionID 不一致的请求，要求ID：" + session.LoginAlias + " 实际ID：" + sessionID);
					Send(session.WorkSocket, AllenBradleyHelper.PackRequestHeader(102, 100u, base.ByteTransform.TransUInt32(read1.Content, 4), new byte[0]));
					RemoveClient(session);
					return;
				}
				byte[] back = ReadFromCipCore(read1.Content);
				Array.Copy(read1.Content, 4, back, 4, 4);
				if (back != null)
				{
					session.WorkSocket.Send(back);
					base.LogNet?.WriteDebug(ToString(), $"[{session.IpEndPoint}] Tcp {StringResources.Language.Send}：{back.ToHexString(' ')}");
					session.UpdateHeartTime();
					RaiseDataReceived(session, read1.Content);
					session.WorkSocket.BeginReceive(new byte[0], 0, 0, SocketFlags.None, SocketAsyncCallBack, session);
				}
				else
				{
					RemoveClient(session);
				}
			}
			catch
			{
				RemoveClient(session);
			}
		}

		/// <summary>
		/// 当收到mc协议的报文的时候应该触发的方法，允许继承重写，来实现自定义的返回，或是数据监听。
		/// </summary>
		/// <param name="cipAll">mc报文</param>
		/// <returns>返回的报文信息</returns>
		protected virtual byte[] ReadFromCipCore(byte[] cipAll)
		{
			if (BitConverter.ToInt16(cipAll, 2) == 102)
			{
				return AllenBradleyHelper.PackCommandResponse(new byte[0], isRead: true);
			}
			byte[] array = SoftBasic.ArrayRemoveBegin(cipAll, 24);
			if (array[26] == 10 && array[27] == 2 && array[28] == 32 && array[29] == 2 && array[30] == 36 && array[31] == 1)
			{
				return null;
			}
			byte[] array2 = base.ByteTransform.TransByte(array, 26, BitConverter.ToInt16(array, 24));
			if (array2[0] == 76 || array2[0] == 82)
			{
				return AllenBradleyHelper.PackRequestHeader(102, 16u, AllenBradleyHelper.PackCommandSpecificData(new byte[4], AllenBradleyHelper.PackCommandSingleService(ReadByCommand(array2), 178)));
			}
			if (array2[0] == 77)
			{
				return AllenBradleyHelper.PackRequestHeader(102, 16u, AllenBradleyHelper.PackCommandSpecificData(new byte[4], AllenBradleyHelper.PackCommandSingleService(WriteByMessage(array2), 178)));
			}
			if (array2[0] == 85)
			{
				return AllenBradleyHelper.PackRequestHeader(111, 16u, AllenBradleyHelper.PackCommandSpecificData(new byte[4], AllenBradleyHelper.PackCommandSingleService(ReadList(array2), 178)));
			}
			return null;
		}

		private byte[] ReadList(byte[] cipCore)
		{
			switch (BitConverter.ToUInt16(cipCore, 6))
			{
			case 0:
				return SoftBasic.HexStringToBytes("\r\nd5 00\r\n06 00 77 00 00 00 15 00 43 4f 4d 4d 5f 33 5f 31\r\n5f 50 4c 43 5f 31 5f 42 75 66 66 65 72 c4 00 8e\r\n01 00 00 0e 00 4b 65 79 73 77 69 74 63 68 41 6c\r\n61 72 6d c4 00 1f 02 00 00 0e 00 44 61 74 65 54\r\n69 6d 65 53 79 6e 63 68 31 c4 20 7a 0c 00 00 11\r\n00 4d 53 47 5f 33 5f 31 5f 50 4c 43 5f 31 5f 54\r\n4d 52 83 8f 9a 10 00 00 11 00 4d 53 47 5f 33 5f\r\n31 5f 50 4c 43 5f 31 5f 43 54 4c ff 8f f2 17 00\r\n00 10 00 4c 5f 43 50 55 5f 4d 65 6d 55 73 65 44\r\n61 74 61 c3 20 c4 1a 00 00 11 00 4d 53 47 5f 33\r\n5f 34 5f 50 4c 43 5f 31 5f 43 54 4c ff 8f 99 1b\r\n00 00 0e 00 4c 69 6e 6b 53 74 61 74 75 73 57 6f\r\n72 64 c4 20 fa 1b 00 00 13 00 50 72 6f 67 72 61\r\n6d 3a 4d 61 69 6e 50 72 6f 67 72 61 6d 68 10 20\r\n24 00 00 09 00 4d 61 70 3a 4c 6f 63 61 6c 69 10\r\n8d 2a 00 00 13 00 4c 5f 43 50 55 5f 4d 73 67 47\r\n65 74 43 6f 6e 6e 55 73 65 ff 8f fc 2b 00 00 12\r\n00 4c 5f 43 50 55 5f 4d 73 67 47 65 74 4d 65 6d\r\n55 73 65 ff 8f 21 2c 00 00 17 00 4c 5f 43 50 55\r\n5f 4d 73 67 47 65 74 54 72 65 6e 64 4f 62 6a 55\r\n73 65 ff 8f 6f 30 00 00 12 00 4c 5f 43 50 55 5f\r\n54 61 73 6b 54 69 6d 65 44 61 74 61 c4 20 68 31\r\n00 00 07 00 54 61 73 6b 3a 54 32 70 10 53 32 00\r\n00 15 00 44 61 74 65 54 69 6d 65 53 79 6e 63 68\r\n52 65 71 75 65 73 74 31 c1 00 9e 32 00 00 07 00\r\n4d 53 47 5f 54 4d 52 83 8f 81 36 00 00 0d 00 4d\r\n65 73 73 61 67 65 5f 54 69 6d 65 72 83 8f ae 37\r\n00 00 13 00 53 74 73 5f 53 43 50 55 5f 52 65 64\r\n75 6e 64 61 6e 63 79 c3 00 2b 38 00 00 15 00 43\r\n4f 4d 4d 5f 33 5f 34 5f 50 4c 43 5f 31 5f 42 75\r\n66 66 65 72 c4 00 3d 38 00 00 03 00 58 58 58 23\r\n82\r\n");
			case 14398:
				return SoftBasic.HexStringToBytes("\r\nd5 00\r\n06 00 55 3e 00 00 11 00 4d 53 47 5f 33 5f 33 5f\r\n50 4c 43 5f 31 5f 54 4d 52 83 8f 67 41 00 00 0f\r\n00 53 74 73 5f 4e 61 6d 65 43 68 61 73 73 69 73\r\nc3 00 a3 41 00 00 15 00 4d 6f 64 75 6c 65 52 65\r\n64 75 6e 64 61 6e 63 79 53 74 61 74 65 c3 00 8a\r\n44 00 00 0b 00 53 74 73 5f 43 50 55 54 69 6d 65\r\nc4 20 f5 45 00 00 11 00 4d 53 47 5f 33 5f 33 5f\r\n50 4c 43 5f 31 5f 43 54 4c ff 8f 43 4c 00 00 12\r\n00 4c 5f 43 50 55 5f 4d 73 67 53 65 74 57 69 6e\r\n64 6f 77 ff 8f ec 50 00 00 04 00 78 78 78 32 fa\r\n8e 7b 55 00 00 09 00 4c 6f 63 61 6c 5f 4d 53 47\r\nce af 24 59 00 00 11 00 5f 5f 44 45 46 56 41 4c\r\n5f 30 30 30 30 30 39 32 30 20 89 78 5f 00 00 0c\r\n00 53 59 53 5f 53 65 74 5f 74 69 6d 65 c4 20 85\r\n60 00 00 09 00 44 4c 52 5f 52 41 5f 4f 4b c1 00\r\n02 66 00 00 0b 00 50 61 72 74 6e 65 72 4d 6f 64\r\n65 c4 00 6a 68 00 00 09 00 44 41 54 45 5f 54 49\r\n4d 45 c4 20 34 6a 00 00 12 00 53 74 73 5f 4d 52\r\n65 64 75 6e 64 61 6e 63 79 5f 4f 4b c1 00 a6 6a\r\n00 00 17 00 51 75 61 6c 69 66 69 63 61 74 69 6f\r\n6e 49 6e 50 72 6f 67 72 65 73 73 c3 00 10 6d 00\r\n00 14 00 44 61 74 65 54 69 6d 65 53 79 6e 63 68\r\n52 65 71 75 65 73 74 c1 00 e8 6d 00 00 10 00 50\r\n61 72 74 6e 65 72 4b 65 79 73 77 69 74 63 68 c4\r\n00 51 6f 00 00 0f 00 52 65 64 75 6e 64 61 6e 63\r\n79 53 74 61 74 65 c3 00 96 6f 00 00 10 00 4c 5f\r\n43 50 55 5f 57 69 6e 64 6f 77 54 69 6d 65 c4 00\r\n69 72 00 00 1d 00 50 61 72 74 6e 65 72 43 68 61\r\n73 73 69 73 52 65 64 75 6e 64 61 6e 63 79 53 74\r\n61 74 65 c3 00 4b 7c 00 00 0c 00 50 72 6f 67 72\r\n61 6d 3a 54 45 53 54 68 10\r\n");
			case 31820:
				return SoftBasic.HexStringToBytes("\r\nd5 00\r\n06 00 b1 81 00 00 18 00 44 61 74 65 54 69 6d 65\r\n53 79 6e 63 68 48 6f 75 72 42 75 66 66 65 72 31\r\nc4 00 84 90 00 00 06 00 4d 61 70 3a 73 64 69 10\r\na0 90 00 00 0f 00 4d 65 73 73 61 67 65 5f 54 69\r\n6d 65 72 5f 31 83 af cb 96 00 00 12 00 50 61 72\r\n74 6e 65 72 4d 69 6e 6f 72 46 61 75 6c 74 73 c4\r\n00 b0 9b 00 00 04 00 78 78 78 78 23 82 17 9d 00\r\n00 19 00 4d 53 47 5f 46 5a 5f 35 5f 31 5f 46 4d\r\n43 53 5f 50 4c 43 5f 31 5f 54 4d 52 c4 00 7e a4\r\n00 00 12 00 43 4f 4d 4d 5f 33 5f 31 5f 50 4c 43\r\n5f 31 5f 54 4d 52 83 8f 14 a5 00 00 0a 00 44 4c\r\n52 5f 53 74 61 74 75 73 c2 20 ed a6 00 00 12 00\r\n4c 5f 43 50 55 5f 50 6f 72 74 43 61 70 79 44 61\r\n74 61 c3 20 30 a8 00 00 0c 00 52 69 6e 67 5f 41\r\n5f 46 61 75 6c 74 c1 00 c0 b3 00 00 19 00 4c 5f\r\n43 50 55 5f 4d 73 67 47 65 74 55 73 65 72 54 61\r\n73 6b 54 69 6d 65 73 ff 8f 6c b5 00 00 11 00 50\r\n68 79 73 69 63 61 6c 43 68 61 73 73 69 73 49 44\r\nc3 00 23 b7 00 00 0d 00 54 61 73 6b 3a 4d 61 69\r\n6e 54 61 73 6b 70 10 3b b8 00 00 12 00 4c 5f 43\r\n50 55 5f 54 72 65 6e 64 4f 62 6a 44 61 74 61 c3\r\n20 0f b9 00 00 17 00 4c 5f 43 50 55 5f 4d 73 67\r\n47 65 74 4f 53 54 61 73 6b 54 69 6d 65 73 ff 8f\r\nb6 b9 00 00 14 00 4c 5f 43 50 55 5f 4d 73 67 47\r\n65 74 53 63 61 6e 54 69 6d 65 ff 8f d5 bf 00 00\r\n0b 00 55 44 49 3a 53 45 46 45 54 59 32 38 13 26\r\nc2 00 00 03 00 54 54 54 83 af bf c2 00 00 0d 00\r\n50 72 69 6d 61 72 79 53 74 61 74 75 73 c3 00 bd\r\nc6 00 00 1c 00 50 61 72 74 6e 65 72 4d 6f 64 75\r\n6c 65 52 65 64 75 6e 64 61 6e 63 79 53 74 61 74\r\n65 c3 00\r\n");
			case 50878:
				return SoftBasic.HexStringToBytes("\r\nd5 00\r\n00 00 12 c8 00 00 0f 00 43 48 31 5f 4d 53 47 5f\r\n50 56 5f 31 5f 44 49 ff 8f 86 c8 00 00 17 00 44\r\n61 74 65 54 69 6d 65 53 79 6e 63 68 48 6f 75 72\r\n42 75 66 66 65 72 c4 00 c9 c8 00 00 15 00 43 4f\r\n4d 4d 5f 33 5f 33 5f 50 4c 43 5f 31 5f 42 75 66\r\n66 65 72 c4 00 b7 c9 00 00 08 00 4d 53 47 5f 52\r\n69 6e 67 ff 8f 1d cb 00 00 14 00 43 6f 6d 70 61\r\n74 69 62 69 6c 69 74 79 52 65 73 75 6c 74 73 c3\r\n00 39 cd 00 00 0d 00 53 59 53 5f 52 65 61 64 5f\r\n74 69 6d 65 c4 20 c7 dc 00 00 0e 00 44 50 5f 31\r\n5f 32 5f 52 48 57 5f 31 5f 31 ca 00 e2 dd 00 00\r\n12 00 43 4f 4d 4d 5f 33 5f 34 5f 50 4c 43 5f 31\r\n5f 54 4d 52 83 8f 25 e0 00 00 12 00 53 74 73 5f\r\n4d 43 50 55 5f 52 65 64 75 6e 64 61 63 79 c3 00\r\n4c e4 00 00 0d 00 44 61 74 65 54 69 6d 65 53 79\r\n6e 63 68 c4 20 c8 e6 00 00 12 00 43 4f 4d 4d 5f\r\n33 5f 33 5f 50 4c 43 5f 31 5f 54 4d 52 83 8f a4\r\ned 00 00 11 00 4d 53 47 5f 33 5f 34 5f 50 4c 43\r\n5f 31 5f 54 4d 52 83 8f 58 f0 00 00 13 00 4c 69\r\n6e 6b 53 74 61 74 75 73 57 6f 72 64 5f 54 45 53\r\n54 c2 20 dc f2 00 00 12 00 4c 5f 43 50 55 5f 53\r\n63 61 6e 54 69 6d 65 44 61 74 61 c3 20 f3 f4 00\r\n00 0d 00 53 52 4d 53 6c 6f 74 4e 75 6d 62 65 72\r\nc3 00 1b fc 00 00 04 00 54 53 45 54 c4 00\r\n");
			default:
				return null;
			}
		}

		private byte[] ReadByCommand(byte[] cipCore)
		{
			byte[] array = base.ByteTransform.TransByte(cipCore, 2, cipCore[1] * 2);
			string address = AllenBradleyHelper.ParseRequestPathCommand(array);
			ushort length = BitConverter.ToUInt16(cipCore, 2 + array.Length);
			return AllenBradleyHelper.PackCommandResponse(Read(address, length).Content, isRead: true);
		}

		private byte[] WriteByMessage(byte[] cipCore)
		{
			if (!base.EnableWrite)
			{
				return AllenBradleyHelper.PackCommandResponse(null, isRead: false);
			}
			byte[] array = base.ByteTransform.TransByte(cipCore, 2, cipCore[1] * 2);
			string text = AllenBradleyHelper.ParseRequestPathCommand(array);
			if (text.EndsWith(".LEN"))
			{
				return AllenBradleyHelper.PackCommandResponse(new byte[0], isRead: false);
			}
			if (text.EndsWith(".DATA[0]"))
			{
				text = text.Replace(".DATA[0]", "");
				byte[] bytes = base.ByteTransform.TransByte(cipCore, 6 + array.Length, cipCore.Length - 6 - array.Length);
				if (Write(text, Encoding.ASCII.GetString(bytes).TrimEnd(default(char))).IsSuccess)
				{
					return AllenBradleyHelper.PackCommandResponse(new byte[0], isRead: false);
				}
				return AllenBradleyHelper.PackCommandResponse(null, isRead: false);
			}
			ushort num = BitConverter.ToUInt16(cipCore, 2 + array.Length);
			ushort num2 = BitConverter.ToUInt16(cipCore, 4 + array.Length);
			byte[] value = base.ByteTransform.TransByte(cipCore, 6 + array.Length, cipCore.Length - 6 - array.Length);
			if (Write(text, value).IsSuccess)
			{
				return AllenBradleyHelper.PackCommandResponse(new byte[0], isRead: false);
			}
			return AllenBradleyHelper.PackCommandResponse(null, isRead: false);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				simpleHybird.Dispose();
			}
			base.Dispose(disposing);
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
		[HslMqttApi("ReadFloatArray", "")]
		public override OperateResult<float[]> ReadFloat(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc />
		[HslMqttApi("ReadDoubleArray", "")]
		public override OperateResult<double[]> ReadDouble(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(Read(address, length), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
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
		public override async Task<OperateResult<float[]>> ReadFloatAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransSingle(m, 0, length));
		}

		/// <inheritdoc />
		public override async Task<OperateResult<double[]>> ReadDoubleAsync(string address, ushort length)
		{
			return ByteTransformHelper.GetResultFromBytes(await ReadAsync(address, length), (byte[] m) => base.ByteTransform.TransDouble(m, 0, length));
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"AllenBradleyServer[{base.Port}]";
		}
	}
}

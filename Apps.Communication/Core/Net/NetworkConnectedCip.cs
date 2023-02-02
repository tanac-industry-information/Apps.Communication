using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Profinet.AllenBradley;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 基于连接的CIP协议的基类
	/// </summary>
	public class NetworkConnectedCip : NetworkDeviceBase
	{
		/// <summary>
		/// O -&gt; T Network Connection ID
		/// </summary>
		protected uint OTConnectionId = 0u;

		/// <summary>
		/// T -&gt; O Network Connection ID
		/// </summary>
		protected uint TOConnectionId = 0u;

		private SoftIncrementCount incrementCount = new SoftIncrementCount(65535L, 3L, 2);

		/// <inheritdoc cref="P:Communication.Profinet.AllenBradley.AllenBradleyNet.SessionHandle" />
		public uint SessionHandle { get; protected set; }

		/// <inheritdoc />
		protected override INetMessage GetNewNetMessage()
		{
			return new AllenBradleyMessage();
		}

		/// <inheritdoc />
		protected override byte[] PackCommandWithHeader(byte[] command)
		{
			return AllenBradleyHelper.PackRequestHeader(112, SessionHandle, AllenBradleyHelper.PackCommandSpecificData(GetOTConnectionIdService(), command));
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
			OperateResult<byte[]> operateResult3 = ReadFromCoreServer(socket, AllenBradleyHelper.PackRequestHeader(111, SessionHandle, GetLargeForwardOpen()), hasResponseData: true, usePackAndUnpack: false);
			if (!operateResult3.IsSuccess)
			{
				return operateResult3;
			}
			try
			{
				if (operateResult3.Content.Length >= 46 && operateResult3.Content[42] != 0)
				{
					if (base.ByteTransform.TransUInt16(operateResult3.Content, 44) == 256)
					{
						return new OperateResult("Connection in use or duplicate Forward Open");
					}
					return new OperateResult("Forward Open failed, Code: " + base.ByteTransform.TransUInt16(operateResult3.Content, 44));
				}
				OTConnectionId = base.ByteTransform.TransUInt32(operateResult3.Content, 44);
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message + Environment.NewLine + "Source: " + operateResult3.Content.ToHexString(' '));
			}
			incrementCount.ResetCurrentValue();
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override OperateResult ExtraOnDisconnect(Socket socket)
		{
			byte[] largeForwardClose = GetLargeForwardClose();
			if (largeForwardClose != null)
			{
				OperateResult<byte[]> operateResult = ReadFromCoreServer(socket, AllenBradleyHelper.PackRequestHeader(111, SessionHandle, largeForwardClose), hasResponseData: true, usePackAndUnpack: false);
				if (!operateResult.IsSuccess)
				{
					return operateResult;
				}
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(socket, AllenBradleyHelper.UnRegisterSessionHandle(SessionHandle), hasResponseData: true, usePackAndUnpack: false);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> InitializationOnConnectAsync(Socket socket)
		{
			OperateResult<byte[]> read1 = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.RegisterSessionHandle(), hasResponseData: true, usePackAndUnpack: false);
			if (!read1.IsSuccess)
			{
				return read1;
			}
			OperateResult check = AllenBradleyHelper.CheckResponse(read1.Content);
			if (!check.IsSuccess)
			{
				return check;
			}
			SessionHandle = base.ByteTransform.TransUInt32(read1.Content, 4);
			OperateResult<byte[]> read2 = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.PackRequestHeader(111, SessionHandle, GetLargeForwardOpen()), hasResponseData: true, usePackAndUnpack: false);
			if (!read2.IsSuccess)
			{
				return read2;
			}
			try
			{
				if (read2.Content.Length >= 46 && read2.Content[42] != 0)
				{
					if (base.ByteTransform.TransUInt16(read2.Content, 44) == 256)
					{
						return new OperateResult("Connection in use or duplicate Forward Open");
					}
					return new OperateResult("Forward Open failed, Code: " + base.ByteTransform.TransUInt16(read2.Content, 44));
				}
				OTConnectionId = base.ByteTransform.TransUInt32(read2.Content, 44);
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message + Environment.NewLine + "Source: " + read2.Content.ToHexString(' '));
			}
			incrementCount.ResetCurrentValue();
			return OperateResult.CreateSuccessResult();
		}

		/// <inheritdoc />
		protected override async Task<OperateResult> ExtraOnDisconnectAsync(Socket socket)
		{
			byte[] forwardClose = GetLargeForwardClose();
			if (forwardClose != null)
			{
				OperateResult<byte[]> close = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.PackRequestHeader(111, SessionHandle, forwardClose), hasResponseData: true, usePackAndUnpack: false);
				if (!close.IsSuccess)
				{
					return close;
				}
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(socket, AllenBradleyHelper.UnRegisterSessionHandle(SessionHandle), hasResponseData: true, usePackAndUnpack: false);
			if (!read.IsSuccess)
			{
				return read;
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 将多个的CIP命令打包成一个服务的命令
		/// </summary>
		/// <param name="cip">CIP命令列表</param>
		/// <returns>服务命令</returns>
		protected byte[] PackCommandService(params byte[][] cip)
		{
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.WriteByte(177);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			memoryStream.WriteByte(0);
			long currentValue = incrementCount.GetCurrentValue();
			memoryStream.WriteByte(BitConverter.GetBytes(currentValue)[0]);
			memoryStream.WriteByte(BitConverter.GetBytes(currentValue)[1]);
			if (cip.Length == 1)
			{
				memoryStream.Write(cip[0], 0, cip[0].Length);
			}
			else
			{
				memoryStream.Write(new byte[6] { 10, 2, 32, 2, 36, 1 }, 0, 6);
				memoryStream.WriteByte(BitConverter.GetBytes(cip.Length)[0]);
				memoryStream.WriteByte(BitConverter.GetBytes(cip.Length)[1]);
				int num = 2 + cip.Length * 2;
				for (int i = 0; i < cip.Length; i++)
				{
					memoryStream.WriteByte(BitConverter.GetBytes(num)[0]);
					memoryStream.WriteByte(BitConverter.GetBytes(num)[1]);
					num += cip[i].Length;
				}
				for (int j = 0; j < cip.Length; j++)
				{
					memoryStream.Write(cip[j], 0, cip[j].Length);
				}
			}
			byte[] array = memoryStream.ToArray();
			memoryStream.Dispose();
			BitConverter.GetBytes((ushort)(array.Length - 4)).CopyTo(array, 2);
			return array;
		}

		/// <summary>
		/// 获取数据通信的前置打开命令，不同的PLC的信息不一样。
		/// </summary>
		/// <returns>原始命令数据</returns>
		protected virtual byte[] GetLargeForwardOpen()
		{
			return "\r\n00 00 00 00 00 00 02 00 00 00 00 00 b2 00 34 00\r\n5b 02 20 06 24 01 06 9c 02 00 00 80 01 00 fe 80\r\n02 00 1b 05 30 a7 2b 03 02 00 00 00 80 84 1e 00\r\ncc 07 00 42 80 84 1e 00 cc 07 00 42 a3 03 20 02\r\n24 01 2c 01".ToHexBytes();
		}

		/// <summary>
		/// 获取数据通信的后置关闭命令，不同的PLC的信息不一样。
		/// </summary>
		/// <returns>原始命令数据</returns>
		protected virtual byte[] GetLargeForwardClose()
		{
			return null;
		}

		private byte[] GetOTConnectionIdService()
		{
			byte[] array = new byte[8] { 161, 0, 4, 0, 0, 0, 0, 0 };
			base.ByteTransform.TransByte(OTConnectionId).CopyTo(array, 4);
			return array;
		}

		/// <summary>
		/// 从PLC反馈的数据解析出真实的数据内容
		/// </summary>
		/// <param name="response">PLC的反馈数据</param>
		/// <param name="isRead">是否是返回的操作</param>
		/// <returns>带有结果标识的最终数据</returns>
		public static OperateResult<byte[], ushort, bool> ExtractActualData(byte[] response, bool isRead)
		{
			List<byte> list = new List<byte>();
			int num = 42;
			bool value = false;
			ushort value2 = 0;
			ushort num2 = BitConverter.ToUInt16(response, num);
			if (BitConverter.ToInt32(response, 46) == 138)
			{
				num = 50;
				int num3 = BitConverter.ToUInt16(response, num);
				for (int i = 0; i < num3; i++)
				{
					int num4 = BitConverter.ToUInt16(response, num + 2 + i * 2) + num;
					int num5 = ((i == num3 - 1) ? response.Length : (BitConverter.ToUInt16(response, num + 4 + i * 2) + num));
					ushort num6 = BitConverter.ToUInt16(response, num4 + 2);
					switch (num6)
					{
					case 4:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley04
						};
					case 5:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley05
						};
					case 6:
						if (response[num + 2] == 210 || response[num + 2] == 204)
						{
							return new OperateResult<byte[], ushort, bool>
							{
								ErrorCode = num6,
								Message = StringResources.Language.AllenBradley06
							};
						}
						break;
					case 10:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley0A
						};
					case 19:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley13
						};
					case 28:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley1C
						};
					case 30:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley1E
						};
					case 38:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.AllenBradley26
						};
					default:
						return new OperateResult<byte[], ushort, bool>
						{
							ErrorCode = num6,
							Message = StringResources.Language.UnknownError
						};
					case 0:
						break;
					}
					if (isRead)
					{
						for (int j = num4 + 6; j < num5; j++)
						{
							list.Add(response[j]);
						}
					}
				}
			}
			else
			{
				byte b = response[num + 6];
				switch (b)
				{
				case 4:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley04
					};
				case 5:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley05
					};
				case 6:
					value = true;
					break;
				case 10:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley0A
					};
				case 19:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley13
					};
				case 28:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley1C
					};
				case 30:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley1E
					};
				case 38:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.AllenBradley26
					};
				default:
					return new OperateResult<byte[], ushort, bool>
					{
						ErrorCode = b,
						Message = StringResources.Language.UnknownError
					};
				case 0:
					break;
				}
				if (response[num + 4] == 205 || response[num + 4] == 211)
				{
					return OperateResult.CreateSuccessResult(list.ToArray(), value2, value);
				}
				if (response[num + 4] == 204 || response[num + 4] == 210)
				{
					for (int k = num + 10; k < num + 2 + num2; k++)
					{
						list.Add(response[k]);
					}
					value2 = BitConverter.ToUInt16(response, num + 8);
				}
				else if (response[num + 4] == 213)
				{
					for (int l = num + 8; l < num + 2 + num2; l++)
					{
						list.Add(response[l]);
					}
				}
				else if (response[num + 4] == 203)
				{
					if (response[58] != 0)
					{
						return new OperateResult<byte[], ushort, bool>(response[58], AllenBradleyDF1Serial.GetExtStatusDescription(response[58]) + Environment.NewLine + "Source: " + response.RemoveBegin(57).ToHexString(' '));
					}
					if (!isRead)
					{
						return OperateResult.CreateSuccessResult(list.ToArray(), value2, value);
					}
					return OperateResult.CreateSuccessResult(response.RemoveBegin(61), value2, value);
				}
			}
			return OperateResult.CreateSuccessResult(list.ToArray(), value2, value);
		}
	}
}

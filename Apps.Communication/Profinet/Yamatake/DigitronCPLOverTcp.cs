using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Apps.Communication.Core;
using Apps.Communication.Core.IMessage;
using Apps.Communication.Core.Net;
using Apps.Communication.Profinet.Yamatake.Helper;

namespace Apps.Communication.Profinet.Yamatake
{
	/// <summary>
	/// 山武的数字指定调节器的通信协议，基于CPL转网口的实现，测试型号 SDC40B
	/// </summary>
	public class DigitronCPLOverTcp : NetworkDeviceBase
	{
		/// <summary>
		/// 获取或设置当前的站号信息
		/// </summary>
		public byte Station { get; set; }

		/// <summary>
		/// 实例化一个默认的对象
		/// </summary>
		public DigitronCPLOverTcp()
		{
			Station = 1;
			base.WordLength = 1;
			base.ByteTransform = new RegularByteTransform();
		}

		/// <inheritdoc />
		protected override OperateResult<byte[]> ReceiveByMessage(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			return ReceiveCommandLineFromSocket(socket, 13, 10, timeOut);
		}

		/// <inheritdoc />
		protected override async Task<OperateResult<byte[]>> ReceiveByMessageAsync(Socket socket, int timeOut, INetMessage netMessage, Action<long, long> reportProgress = null)
		{
			return await ReceiveCommandLineFromSocketAsync(socket, 13, 10, timeOut);
		}

		/// <inheritdoc />
		public override OperateResult<byte[]> Read(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = DigitronCPLHelper.BuildReadCommand(station, address, length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return DigitronCPLHelper.ExtraActualResponse(operateResult2.Content);
		}

		/// <inheritdoc />
		public override OperateResult Write(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> operateResult = DigitronCPLHelper.BuildWriteCommand(station, address, value);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			OperateResult<byte[]> operateResult2 = ReadFromCoreServer(operateResult.Content);
			if (!operateResult2.IsSuccess)
			{
				return operateResult2;
			}
			return DigitronCPLHelper.ExtraActualResponse(operateResult2.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult<byte[]>> ReadAsync(string address, ushort length)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = DigitronCPLHelper.BuildReadCommand(station, address, length);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return DigitronCPLHelper.ExtraActualResponse(read.Content);
		}

		/// <inheritdoc />
		public override async Task<OperateResult> WriteAsync(string address, byte[] value)
		{
			byte station = (byte)HslHelper.ExtractParameter(ref address, "s", Station);
			OperateResult<byte[]> command = DigitronCPLHelper.BuildWriteCommand(station, address, value);
			if (!command.IsSuccess)
			{
				return command;
			}
			OperateResult<byte[]> read = await ReadFromCoreServerAsync(command.Content);
			if (!read.IsSuccess)
			{
				return read;
			}
			return DigitronCPLHelper.ExtraActualResponse(read.Content);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"DigitronCPLOverTcp[{IpAddress}:{Port}]";
		}
	}
}

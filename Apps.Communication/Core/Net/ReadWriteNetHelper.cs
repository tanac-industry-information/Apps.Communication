using System;
using System.Threading;
using System.Threading.Tasks;
using Apps.Communication.Reflection;

namespace Apps.Communication.Core.Net
{
	/// <summary>
	/// 读写网络的辅助类
	/// </summary>
	public class ReadWriteNetHelper
	{
		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Boolean,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, bool waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<bool> operateResult = readWriteNet.ReadBool(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int16,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, short waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<short> operateResult = readWriteNet.ReadInt16(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt16,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, ushort waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<ushort> operateResult = readWriteNet.ReadUInt16(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int32,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, int waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<int> operateResult = readWriteNet.ReadInt32(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt32,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, uint waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<uint> operateResult = readWriteNet.ReadUInt32(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int64,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, long waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<long> operateResult = readWriteNet.ReadInt64(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt64,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static OperateResult<TimeSpan> Wait(IReadWriteNet readWriteNet, string address, ulong waitValue, int readInterval, int waitTimeout)
		{
			DateTime now = DateTime.Now;
			while (true)
			{
				OperateResult<ulong> operateResult = readWriteNet.ReadUInt64(address);
				if (!operateResult.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(operateResult);
				}
				if (operateResult.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - now);
				}
				if (waitTimeout > 0 && (DateTime.Now - now).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				Thread.Sleep(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String)" />
		public static OperateResult<T> ReadCustomer<T>(IReadWriteNet readWriteNet, string address) where T : IDataTransfer, new()
		{
			T obj = new T();
			return ReadCustomer(readWriteNet, address, obj);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String,``0)" />
		public static OperateResult<T> ReadCustomer<T>(IReadWriteNet readWriteNet, string address, T obj) where T : IDataTransfer, new()
		{
			OperateResult<byte[]> operateResult = readWriteNet.Read(address, obj.ReadCount);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<T>(operateResult);
			}
			obj.ParseSource(operateResult.Content);
			return OperateResult.CreateSuccessResult(obj);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteCustomer``1(System.String,``0)" />
		public static OperateResult WriteCustomer<T>(IReadWriteNet readWriteNet, string address, T data) where T : IDataTransfer, new()
		{
			return readWriteNet.Write(address, data.ToSource());
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String)" />
		public static async Task<OperateResult<T>> ReadCustomerAsync<T>(IReadWriteNet readWriteNet, string address) where T : IDataTransfer, new()
		{
			T Content = new T();
			return await ReadCustomerAsync(readWriteNet, address, Content);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadCustomer``1(System.String,``0)" />
		public static async Task<OperateResult<T>> ReadCustomerAsync<T>(IReadWriteNet readWriteNet, string address, T obj) where T : IDataTransfer, new()
		{
			OperateResult<byte[]> read = await readWriteNet.ReadAsync(address, obj.ReadCount);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<T>(read);
			}
			obj.ParseSource(read.Content);
			return OperateResult.CreateSuccessResult(obj);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.WriteCustomerAsync``1(System.String,``0)" />
		public static async Task<OperateResult> WriteCustomerAsync<T>(IReadWriteNet readWriteNet, string address, T data) where T : IDataTransfer, new()
		{
			return await readWriteNet.WriteAsync(address, data.ToSource());
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.ReadStruct``1(System.String,System.UInt16)" />
		public static OperateResult<T> ReadStruct<T>(IReadWriteNet readWriteNet, string address, ushort length, IByteTransform byteTransform, int startIndex = 0) where T : class, new()
		{
			OperateResult<byte[]> operateResult = readWriteNet.Read(address, length);
			if (!operateResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<T>(operateResult);
			}
			try
			{
				return OperateResult.CreateSuccessResult(HslReflectionHelper.PraseStructContent<T>(operateResult.Content, startIndex, byteTransform));
			}
			catch (Exception ex)
			{
				return new OperateResult<T>("Prase struct faild: " + ex.Message + Environment.NewLine + "Source Data: " + operateResult.Content.ToHexString(' '));
			}
		}

		/// <inheritdoc cref="M:Communication.Core.Net.ReadWriteNetHelper.ReadStruct``1(Communication.Core.IReadWriteNet,System.String,System.UInt16,Communication.Core.IByteTransform,System.Int32)" />
		public static async Task<OperateResult<T>> ReadStructAsync<T>(IReadWriteNet readWriteNet, string address, ushort length, IByteTransform byteTransform, int startIndex = 0) where T : class, new()
		{
			OperateResult<byte[]> read = await readWriteNet.ReadAsync(address, length);
			if (!read.IsSuccess)
			{
				return OperateResult.CreateFailedResult<T>(read);
			}
			try
			{
				return OperateResult.CreateSuccessResult(HslReflectionHelper.PraseStructContent<T>(read.Content, startIndex, byteTransform));
			}
			catch (Exception ex)
			{
				return new OperateResult<T>("Prase struct faild: " + ex.Message + Environment.NewLine + "Source Data: " + read.Content.ToHexString(' '));
			}
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Boolean,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, bool waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<bool> read = await readWriteNet.ReadBoolAsync(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int16,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, short waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<short> read = await readWriteNet.ReadInt16Async(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt16,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, ushort waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<ushort> read = await readWriteNet.ReadUInt16Async(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int32,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, int waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<int> read = await readWriteNet.ReadInt32Async(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt32,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, uint waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<uint> read = readWriteNet.ReadUInt32(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.Int64,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, long waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<long> read = readWriteNet.ReadInt64(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}

		/// <inheritdoc cref="M:Communication.Core.IReadWriteNet.Wait(System.String,System.UInt64,System.Int32,System.Int32)" />
		/// <param name="readWriteNet">通信对象</param>
		public static async Task<OperateResult<TimeSpan>> WaitAsync(IReadWriteNet readWriteNet, string address, ulong waitValue, int readInterval, int waitTimeout)
		{
			DateTime start = DateTime.Now;
			while (true)
			{
				OperateResult<ulong> read = readWriteNet.ReadUInt64(address);
				if (!read.IsSuccess)
				{
					return OperateResult.CreateFailedResult<TimeSpan>(read);
				}
				if (read.Content == waitValue)
				{
					return OperateResult.CreateSuccessResult(DateTime.Now - start);
				}
				if (waitTimeout > 0 && (DateTime.Now - start).TotalMilliseconds > (double)waitTimeout)
				{
					break;
				}
				await Task.Delay(readInterval);
			}
			return new OperateResult<TimeSpan>(StringResources.Language.CheckDataTimeout + waitTimeout);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Apps.Communication.Core;
using Apps.Communication.Reflection;

namespace Apps.Communication.LogNet
{
	/// <summary>
	/// 日志存储类的基类，提供一些基础的服务
	/// </summary>
	/// <remarks>
	/// 基于此类可以实现任意的规则的日志存储规则，欢迎大家补充实现，本组件实现了3个日志类
	/// <list type="number">
	/// <item>单文件日志类 <see cref="T:Communication.LogNet.LogNetSingle" /></item>
	/// <item>根据文件大小的类 <see cref="T:Communication.LogNet.LogNetFileSize" /></item>
	/// <item>根据时间进行存储的类 <see cref="T:Communication.LogNet.LogNetDateTime" /></item>
	/// </list>
	/// </remarks>
	public abstract class LogNetBase : IDisposable
	{
		/// <summary>
		/// 文件存储的锁
		/// </summary>
		protected SimpleHybirdLock m_fileSaveLock;

		private HslMessageDegree m_messageDegree = HslMessageDegree.DEBUG;

		private Queue<HslMessageItem> m_WaitForSave;

		private SimpleHybirdLock m_simpleHybirdLock;

		private int m_SaveStatus = 0;

		private List<string> filtrateKeyword;

		private SimpleHybirdLock filtrateLock;

		private bool disposedValue = false;

		/// <inheritdoc cref="P:Communication.LogNet.ILogNet.LogSaveMode" />
		public LogSaveMode LogSaveMode { get; protected set; }

		/// <inheritdoc cref="P:Communication.LogNet.ILogNet.LogNetStatistics" />
		public LogStatistics LogNetStatistics { get; set; }

		/// <inheritdoc cref="P:Communication.LogNet.ILogNet.ConsoleOutput" />
		public bool ConsoleOutput { get; set; }

		/// <inheritdoc cref="E:Communication.LogNet.ILogNet.BeforeSaveToFile" />
		public event EventHandler<HslEventArgs> BeforeSaveToFile = null;

		/// <summary>
		/// 实例化一个日志对象<br />
		/// Instantiate a log object
		/// </summary>
		public LogNetBase()
		{
			m_fileSaveLock = new SimpleHybirdLock();
			m_simpleHybirdLock = new SimpleHybirdLock();
			m_WaitForSave = new Queue<HslMessageItem>();
			filtrateKeyword = new List<string>();
			filtrateLock = new SimpleHybirdLock();
		}

		private void OnBeforeSaveToFile(HslEventArgs args)
		{
			this.BeforeSaveToFile?.Invoke(this, args);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteDebug(System.String)" />
		[HslMqttApi]
		public void WriteDebug(string text)
		{
			WriteDebug(string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteDebug(System.String,System.String)" />
		[HslMqttApi(ApiTopic = "WriteDebugKeyWord")]
		public void WriteDebug(string keyWord, string text)
		{
			RecordMessage(HslMessageDegree.DEBUG, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteInfo(System.String)" />
		[HslMqttApi]
		public void WriteInfo(string text)
		{
			WriteInfo(string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteInfo(System.String,System.String)" />
		[HslMqttApi(ApiTopic = "WriteInfoKeyWord")]
		public void WriteInfo(string keyWord, string text)
		{
			RecordMessage(HslMessageDegree.INFO, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteWarn(System.String)" />
		[HslMqttApi]
		public void WriteWarn(string text)
		{
			WriteWarn(string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteWarn(System.String,System.String)" />
		[HslMqttApi(ApiTopic = "WriteWarnKeyWord")]
		public void WriteWarn(string keyWord, string text)
		{
			RecordMessage(HslMessageDegree.WARN, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteError(System.String)" />
		[HslMqttApi]
		public void WriteError(string text)
		{
			WriteError(string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteError(System.String,System.String)" />
		[HslMqttApi(ApiTopic = "WriteErrorKeyWord")]
		public void WriteError(string keyWord, string text)
		{
			RecordMessage(HslMessageDegree.ERROR, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteFatal(System.String)" />
		[HslMqttApi]
		public void WriteFatal(string text)
		{
			WriteFatal(string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteFatal(System.String,System.String)" />
		[HslMqttApi(ApiTopic = "WriteFatalKeyWord")]
		public void WriteFatal(string keyWord, string text)
		{
			RecordMessage(HslMessageDegree.FATAL, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteException(System.String,System.Exception)" />
		public void WriteException(string keyWord, Exception ex)
		{
			WriteException(keyWord, string.Empty, ex);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteException(System.String,System.String,System.Exception)" />
		public void WriteException(string keyWord, string text, Exception ex)
		{
			RecordMessage(HslMessageDegree.FATAL, keyWord, LogNetManagment.GetSaveStringFromException(text, ex));
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.RecordMessage(Communication.LogNet.HslMessageDegree,System.String,System.String)" />
		public void RecordMessage(HslMessageDegree degree, string keyWord, string text)
		{
			WriteToFile(degree, keyWord, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteDescrition(System.String)" />
		[HslMqttApi]
		public void WriteDescrition(string description)
		{
			if (string.IsNullOrEmpty(description))
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder("\u0002");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("\u0002/");
			int num = 118 - CalculateStringOccupyLength(description);
			if (num >= 8)
			{
				int num2 = (num - 8) / 2;
				AppendCharToStringBuilder(stringBuilder, '*', num2);
				stringBuilder.Append("   ");
				stringBuilder.Append(description);
				stringBuilder.Append("   ");
				if (num % 2 == 0)
				{
					AppendCharToStringBuilder(stringBuilder, '*', num2);
				}
				else
				{
					AppendCharToStringBuilder(stringBuilder, '*', num2 + 1);
				}
			}
			else if (num >= 2)
			{
				int num3 = (num - 2) / 2;
				AppendCharToStringBuilder(stringBuilder, '*', num3);
				stringBuilder.Append(description);
				if (num % 2 == 0)
				{
					AppendCharToStringBuilder(stringBuilder, '*', num3);
				}
				else
				{
					AppendCharToStringBuilder(stringBuilder, '*', num3 + 1);
				}
			}
			else
			{
				stringBuilder.Append(description);
			}
			stringBuilder.Append("/");
			stringBuilder.Append(Environment.NewLine);
			RecordMessage(HslMessageDegree.None, string.Empty, stringBuilder.ToString());
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteAnyString(System.String)" />
		[HslMqttApi]
		public void WriteAnyString(string text)
		{
			RecordMessage(HslMessageDegree.None, string.Empty, text);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.WriteNewLine" />
		[HslMqttApi]
		public void WriteNewLine()
		{
			RecordMessage(HslMessageDegree.None, string.Empty, "\u0002" + Environment.NewLine);
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.SetMessageDegree(Communication.LogNet.HslMessageDegree)" />
		public void SetMessageDegree(HslMessageDegree degree)
		{
			m_messageDegree = degree;
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.FiltrateKeyword(System.String)" />
		[HslMqttApi]
		public void FiltrateKeyword(string keyword)
		{
			filtrateLock.Enter();
			if (!filtrateKeyword.Contains(keyword))
			{
				filtrateKeyword.Add(keyword);
			}
			filtrateLock.Leave();
		}

		/// <inheritdoc cref="M:Communication.LogNet.ILogNet.RemoveFiltrate(System.String)" />
		[HslMqttApi]
		public void RemoveFiltrate(string keyword)
		{
			filtrateLock.Enter();
			if (filtrateKeyword.Contains(keyword))
			{
				filtrateKeyword.Remove(keyword);
			}
			filtrateLock.Leave();
		}

		private void WriteToFile(HslMessageDegree degree, string keyword, string text)
		{
			if (degree <= m_messageDegree)
			{
				HslMessageItem hslMessageItem = GetHslMessageItem(degree, keyword, text);
				AddItemToCache(hslMessageItem);
			}
		}

		private void AddItemToCache(HslMessageItem item)
		{
			m_simpleHybirdLock.Enter();
			m_WaitForSave.Enqueue(item);
			m_simpleHybirdLock.Leave();
			StartSaveFile();
		}

		private void StartSaveFile()
		{
			if (Interlocked.CompareExchange(ref m_SaveStatus, 1, 0) == 0)
			{
				ThreadPool.QueueUserWorkItem(ThreadPoolSaveFile, null);
			}
		}

		private HslMessageItem GetAndRemoveLogItem()
		{
			HslMessageItem hslMessageItem = null;
			m_simpleHybirdLock.Enter();
			hslMessageItem = ((m_WaitForSave.Count > 0) ? m_WaitForSave.Dequeue() : null);
			m_simpleHybirdLock.Leave();
			return hslMessageItem;
		}

		private void ConsoleWriteLog(HslMessageItem log)
		{
			if (log.Degree == HslMessageDegree.DEBUG)
			{
				Console.ForegroundColor = ConsoleColor.DarkGray;
			}
			else if (log.Degree == HslMessageDegree.INFO)
			{
				Console.ForegroundColor = ConsoleColor.White;
			}
			else if (log.Degree == HslMessageDegree.WARN)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
			}
			else if (log.Degree == HslMessageDegree.ERROR)
			{
				Console.ForegroundColor = ConsoleColor.Red;
			}
			else if (log.Degree == HslMessageDegree.FATAL)
			{
				Console.ForegroundColor = ConsoleColor.DarkRed;
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.White;
			}
			Console.WriteLine(log.ToString());
		}

		private void ThreadPoolSaveFile(object obj)
		{
			HslMessageItem andRemoveLogItem = GetAndRemoveLogItem();
			m_fileSaveLock.Enter();
			string fileSaveName = GetFileSaveName();
			if (!string.IsNullOrEmpty(fileSaveName))
			{
				StreamWriter streamWriter = null;
				try
				{
					streamWriter = new StreamWriter(fileSaveName, append: true, Encoding.UTF8);
					while (andRemoveLogItem != null)
					{
						if (ConsoleOutput)
						{
							ConsoleWriteLog(andRemoveLogItem);
						}
						OnBeforeSaveToFile(new HslEventArgs
						{
							HslMessage = andRemoveLogItem
						});
						LogNetStatistics?.StatisticsAdd(1L);
						bool flag = true;
						filtrateLock.Enter();
						flag = !filtrateKeyword.Contains(andRemoveLogItem.KeyWord);
						filtrateLock.Leave();
						if (andRemoveLogItem.Cancel)
						{
							flag = false;
						}
						if (flag)
						{
							streamWriter.Write(HslMessageFormate(andRemoveLogItem));
							streamWriter.Write(Environment.NewLine);
							streamWriter.Flush();
						}
						andRemoveLogItem = GetAndRemoveLogItem();
					}
				}
				catch (Exception ex)
				{
					AddItemToCache(andRemoveLogItem);
					AddItemToCache(new HslMessageItem
					{
						Degree = HslMessageDegree.FATAL,
						Text = LogNetManagment.GetSaveStringFromException("LogNetSelf", ex)
					});
				}
				finally
				{
					streamWriter?.Dispose();
				}
			}
			else
			{
				while (andRemoveLogItem != null)
				{
					if (ConsoleOutput)
					{
						ConsoleWriteLog(andRemoveLogItem);
					}
					OnBeforeSaveToFile(new HslEventArgs
					{
						HslMessage = andRemoveLogItem
					});
					andRemoveLogItem = GetAndRemoveLogItem();
				}
			}
			m_fileSaveLock.Leave();
			Interlocked.Exchange(ref m_SaveStatus, 0);
			OnWriteCompleted();
			if (m_WaitForSave.Count > 0)
			{
				StartSaveFile();
			}
		}

		private string HslMessageFormate(HslMessageItem hslMessage)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (hslMessage.Degree != HslMessageDegree.None)
			{
				stringBuilder.Append("\u0002");
				stringBuilder.Append("[");
				stringBuilder.Append(LogNetManagment.GetDegreeDescription(hslMessage.Degree));
				stringBuilder.Append("] ");
				stringBuilder.Append(hslMessage.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
				stringBuilder.Append(" thread:[");
				stringBuilder.Append(hslMessage.ThreadId.ToString("D3"));
				stringBuilder.Append("] ");
				if (!string.IsNullOrEmpty(hslMessage.KeyWord))
				{
					stringBuilder.Append(hslMessage.KeyWord);
					stringBuilder.Append(" : ");
				}
			}
			stringBuilder.Append(hslMessage.Text);
			return stringBuilder.ToString();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"LogNetBase[{LogSaveMode}]";
		}

		/// <inheritdoc />
		protected virtual string GetFileSaveName()
		{
			return string.Empty;
		}

		/// <summary>
		/// 当写入文件完成的时候触发，这时候已经释放了文件的句柄了。<br />
		/// Triggered when writing to the file is complete, and the file handle has been released.
		/// </summary>
		protected virtual void OnWriteCompleted()
		{
		}

		private HslMessageItem GetHslMessageItem(HslMessageDegree degree, string keyWord, string text)
		{
			return new HslMessageItem
			{
				KeyWord = keyWord,
				Degree = degree,
				Text = text,
				ThreadId = Thread.CurrentThread.ManagedThreadId
			};
		}

		private int CalculateStringOccupyLength(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < str.Length; i++)
			{
				num = ((str[i] < '一' || str[i] > '龻') ? (num + 1) : (num + 2));
			}
			return num;
		}

		private void AppendCharToStringBuilder(StringBuilder sb, char c, int count)
		{
			for (int i = 0; i < count; i++)
			{
				sb.Append(c);
			}
		}

		/// <summary>
		/// 释放资源
		/// </summary>
		/// <param name="disposing">是否初次调用</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					this.BeforeSaveToFile = null;
					m_simpleHybirdLock.Dispose();
					m_WaitForSave.Clear();
					m_fileSaveLock.Dispose();
				}
				disposedValue = true;
			}
		}

		/// <inheritdoc cref="M:System.IDisposable.Dispose" />
		public void Dispose()
		{
			Dispose(disposing: true);
		}
	}
}

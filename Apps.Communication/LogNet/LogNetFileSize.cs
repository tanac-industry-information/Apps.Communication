using System;
using System.IO;

namespace Apps.Communication.LogNet
{
	/// <summary>
	/// 根据文件的大小来存储日志信息，当前的文件大小增长超过设定值，就会创建新的文件来存储，新的文件命名为当前时间。<br />
	/// Log information is stored according to the size of the file. If the current file size exceeds the set value, a new file is created for storage, and the new file is named the current time.
	/// </summary>
	/// <remarks>
	/// 此日志的实例是根据文件的大小储存，例如设置了2M，每隔2M，系统将生成一个新的日志文件。当然你也可以指定允许存在多少个日志文件，
	/// 比如我允许存在最多10个文件，那总量就是20M，旧的文件会被删除掉。
	/// </remarks>
	/// <example>
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example2" title="限制文件大小实例化" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example4" title="基本的使用" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example5" title="所有日志不存储" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example6" title="仅存储ERROR等级" />
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\LogNet\LogNetSingle.cs" region="Example7" title="不指定路径" />
	/// </example>
	public class LogNetFileSize : LogPathBase, ILogNet, IDisposable
	{
		private int fileMaxSize = 2097152;

		private int currentFileSize = 0;

		/// <summary>
		/// 实例化一个根据文件大小生成新文件的，默认是2M大的文件<br />
		/// Instantiate a new file based on the file size. The default is 2M.
		/// </summary>
		/// <param name="filePath">日志文件的保存路径</param>
		/// <param name="fileMaxSize">每个日志文件的最大大小，默认2M</param>
		/// <param name="fileQuantity">指定当前的日志文件数量上限，如果小于0，则不限制，文件一直增加，如果设置为10，就限制最多10个文件，会删除最近时间的10个文件之外的文件。</param>
		public LogNetFileSize(string filePath, int fileMaxSize = 2097152, int fileQuantity = -1)
		{
			base.filePath = filePath;
			this.fileMaxSize = fileMaxSize;
			controlFileQuantity = fileQuantity;
			base.LogSaveMode = LogSaveMode.FileFixedSize;
			if (!string.IsNullOrEmpty(filePath) && !Directory.Exists(filePath))
			{
				Directory.CreateDirectory(filePath);
			}
		}

		/// <inheritdoc />
		protected override string GetFileSaveName()
		{
			if (string.IsNullOrEmpty(filePath))
			{
				return string.Empty;
			}
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = GetLastAccessFileName();
			}
			if (File.Exists(fileName))
			{
				FileInfo fileInfo = new FileInfo(fileName);
				if (fileInfo.Length > fileMaxSize)
				{
					fileName = GetDefaultFileName();
				}
				else
				{
					currentFileSize = (int)fileInfo.Length;
				}
			}
			return fileName;
		}

		/// <summary>
		/// 获取之前保存的日志文件，如果文件大小超过了设定值，将会生成新的文件名称<br />
		/// Obtain the previously saved log file. If the file size exceeds the set value, a new file name will be generated
		/// </summary>
		/// <returns>文件名称</returns>
		private string GetLastAccessFileName()
		{
			string[] existLogFileNames = GetExistLogFileNames();
			foreach (string result in existLogFileNames)
			{
				FileInfo fileInfo = new FileInfo(result);
				if (fileInfo.Length < fileMaxSize)
				{
					currentFileSize = (int)fileInfo.Length;
					return result;
				}
			}
			return GetDefaultFileName();
		}

		/// <summary>
		/// 获取一个新的默认的文件名称<br />
		/// Get a new default file name
		/// </summary>
		/// <returns>完整的文件名</returns>
		private string GetDefaultFileName()
		{
			return Path.Combine(filePath, "Logs_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt");
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"LogNetFileSize[{fileMaxSize}]";
		}
	}
}

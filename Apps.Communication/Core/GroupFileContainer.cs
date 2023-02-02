using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Apps.Communication.LogNet;
using Newtonsoft.Json.Linq;

namespace Apps.Communication.Core
{
	/// <summary>
	/// 文件集容器，绑定一个文件夹的文件信息组，提供了文件夹的文件信息的获取，更新接口<br />
	/// File set container, which binds the file information group of a folder, provides the file information acquisition and update interface of the folder
	/// </summary>
	public class GroupFileContainer
	{
		/// <summary>
		/// 文件数量变化的委托信息<br />
		/// Order information for changes in the number of files
		/// </summary>
		/// <param name="container">文件列表容器</param>
		/// <param name="fileCount">文件的数量</param>
		public delegate void FileCountChangedDelegate(GroupFileContainer container, int fileCount);

		private string dirPath = string.Empty;

		private const string FileListResources = "list.txt";

		private ILogNet LogNet;

		private string jsonArrayContent = "[]";

		private int filesCount = 0;

		private object hybirdLock = new object();

		private HslAsyncCoordinator coordinatorCacheJsonArray;

		private List<GroupFileItem> groupFileItems;

		private string fileFolderPath;

		private string fileFullPath;

		private long totalFileSize = 0L;

		private DateTime lastModifyTime = DateTime.MinValue;

		/// <summary>
		/// 包含所有文件列表信息的json文本缓存<br />
		/// JSON text cache containing all file list information
		/// </summary>
		public string JsonArrayContent => jsonArrayContent;

		/// <summary>
		/// 获取文件的数量<br />
		/// Get the number of files
		/// </summary>
		public int FileCount => filesCount;

		/// <summary>
		/// 当前的目录信息<br />
		/// Current catalog information
		/// </summary>
		public string DirectoryPath => dirPath;

		/// <summary>
		/// 当文件数量发生变化的时候触发的事件<br />
		/// Event triggered when the number of files changes
		/// </summary>
		public event FileCountChangedDelegate FileCountChanged;

		/// <summary>
		/// 实例化一个新的指定目录的文件管理容器<br />
		/// Instantiates a new file management container for the specified directory
		/// </summary>
		/// <param name="logNet">日志记录对象，可以为空</param>
		/// <param name="path">文件的路径</param>
		public GroupFileContainer(ILogNet logNet, string path)
		{
			LogNet = logNet;
			dirPath = path;
			if (!string.IsNullOrEmpty(path))
			{
				LoadByPath(path);
			}
		}

		/// <summary>
		/// 获取当前目录所有文件的大小之和<br />
		/// Get the sum of the size of all files in the current directory
		/// </summary>
		public GroupFileInfo GetGroupFileInfo()
		{
			GroupFileInfo groupFileInfo = new GroupFileInfo();
			lock (hybirdLock)
			{
				groupFileInfo.FileCount = filesCount;
				groupFileInfo.FileTotalSize = totalFileSize;
				groupFileInfo.LastModifyTime = lastModifyTime;
			}
			return groupFileInfo;
		}

		/// <summary>
		/// 下载文件时调用，根据当前的文件名称，例如 123.txt 获取到在文件服务器里映射的文件名称，例如返回 b35a11ec533147ca80c7f7d1713f015b7909<br />
		/// Called when downloading a file. Get the file name mapped in the file server according to the current file name, such as 123.txt. 
		/// For example, return b35a11ec533147ca80c7f7d1713f015b7909.
		/// </summary>
		/// <param name="fileName">文件的实际名称</param>
		/// <returns>文件名映射过去的实际的文件名字</returns>
		public string GetCurrentFileMappingName(string fileName)
		{
			string result = string.Empty;
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					if (groupFileItems[i].FileName == fileName)
					{
						result = groupFileItems[i].MappingName;
						groupFileItems[i].DownloadTimes++;
					}
				}
			}
			coordinatorCacheJsonArray.StartOperaterInfomation();
			return result;
		}

		/// <summary>
		/// 上传文件时掉用，通过比对现有的文件列表，如果没有，就重新创建列表信息<br />
		/// Used when uploading files, by comparing existing file lists, if not, re-creating list information
		/// </summary>
		/// <param name="fileName">文件名，带后缀，不带任何的路径</param>
		/// <param name="fileSize">文件的大小</param>
		/// <param name="mappingName">文件映射名称</param>
		/// <param name="owner">文件的拥有者</param>
		/// <param name="description">文件的额外描述</param>
		/// <returns>映射的文件名称</returns>
		public string UpdateFileMappingName(string fileName, long fileSize, string mappingName, string owner, string description)
		{
			string text = string.Empty;
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					if (groupFileItems[i].FileName == fileName)
					{
						totalFileSize -= groupFileItems[i].FileSize;
						text = groupFileItems[i].MappingName;
						groupFileItems[i].MappingName = mappingName;
						groupFileItems[i].Description = description;
						groupFileItems[i].FileSize = fileSize;
						groupFileItems[i].Owner = owner;
						groupFileItems[i].UploadTime = DateTime.Now;
						totalFileSize += fileSize;
						if (lastModifyTime < groupFileItems[i].UploadTime)
						{
							lastModifyTime = groupFileItems[i].UploadTime;
						}
						break;
					}
				}
				if (string.IsNullOrEmpty(text))
				{
					GroupFileItem groupFileItem = new GroupFileItem
					{
						FileName = fileName,
						FileSize = fileSize,
						DownloadTimes = 0L,
						Description = description,
						Owner = owner,
						MappingName = mappingName,
						UploadTime = DateTime.Now
					};
					groupFileItems.Add(groupFileItem);
					filesCount = groupFileItems.Count;
					totalFileSize += fileSize;
					if (lastModifyTime < groupFileItem.UploadTime)
					{
						lastModifyTime = groupFileItem.UploadTime;
					}
				}
			}
			coordinatorCacheJsonArray.StartOperaterInfomation();
			return text;
		}

		/// <summary>
		/// 删除一个文件信息，传入文件实际的名称，例如 123.txt 返回被删除的文件的guid名称，例如返回 b35a11ec533147ca80c7f7d1713f015b7909   此方法存在同名文件删除的风险<br />
		/// Delete a file information. Pass in the actual name of the file. For example, 123.txt returns the guid name of the deleted file. For example, it returns b35a11ec533147ca80c7f7d1713f015b7909. There is a risk of deleting the file with the same name
		/// </summary>
		/// <param name="fileName">实际的文件名称，如果 123.txt</param>
		/// <returns>映射之后的文件名，例如 b35a11ec533147ca80c7f7d1713f015b7909</returns>
		public string DeleteFile(string fileName)
		{
			string result = string.Empty;
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					if (groupFileItems[i].FileName == fileName)
					{
						result = groupFileItems[i].MappingName;
						groupFileItems.RemoveAt(i);
						break;
					}
				}
				UpdatePathInfomation();
			}
			coordinatorCacheJsonArray.StartOperaterInfomation();
			return result;
		}

		/// <summary>
		/// 判断当前的文件名是否在文件的列表里，传入文件实际的名称，例如 123.txt，如果文件存在，返回 true, 如果不存在，返回 false<br />
		/// Determine whether the current file name is in the file list, and pass in the actual file name, such as 123.txt, 
		/// if it exists, return true, if it does not exist, return false
		/// </summary>
		/// <param name="fileName">实际的文件名称，如果 123.txt</param>
		/// <returns>如果文件存在，返回 true, 如果不存在，返回 false</returns>
		public bool FileExists(string fileName)
		{
			bool result = false;
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					if (groupFileItems[i].FileName == fileName)
					{
						result = true;
						if (!File.Exists(Path.Combine(dirPath, groupFileItems[i].MappingName)))
						{
							result = false;
							LogNet?.WriteError("File Check exist failed, find file in list, but mapping file not found");
						}
						break;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// 删除一个文件信息，传入文件唯一的guid的名称，例如 b35a11ec533147ca80c7f7d1713f015b7909 返回被删除的文件的guid名称<br />
		/// Delete a file information, pass in the unique GUID name of the file, for example b35a11ec533147ca80c7f7d1713f015b7909 return the GUID name of the deleted file
		/// </summary>
		/// <param name="guidName">实际的文件名称，如果 b35a11ec533147ca80c7f7d1713f015b7909</param>
		/// <returns>映射之后的文件名，例如 b35a11ec533147ca80c7f7d1713f015b7909</returns>
		public string DeleteFileByGuid(string guidName)
		{
			string result = string.Empty;
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					if (groupFileItems[i].MappingName == guidName)
					{
						result = groupFileItems[i].MappingName;
						groupFileItems.RemoveAt(i);
						break;
					}
				}
				UpdatePathInfomation();
			}
			coordinatorCacheJsonArray.StartOperaterInfomation();
			return result;
		}

		/// <summary>
		/// 删除当前目录下所有的文件信息，返回等待被删除的文件列表，是映射文件名：b35a11ec533147ca80c7f7d1713f015b7909<br />
		/// Delete all file information in the current directory, and return to the list of files waiting to be deleted, 
		/// which is the mapping file name: b35a11ec533147ca80c7f7d1713f015b7909
		/// </summary>
		/// <returns>映射之后的文件列表，例如 b35a11ec533147ca80c7f7d1713f015b7909</returns>
		public List<string> ClearAllFiles()
		{
			List<string> list = new List<string>();
			lock (hybirdLock)
			{
				for (int i = 0; i < groupFileItems.Count; i++)
				{
					list.Add(groupFileItems[i].MappingName);
				}
				groupFileItems.Clear();
				UpdatePathInfomation();
			}
			coordinatorCacheJsonArray.StartOperaterInfomation();
			return list;
		}

		private void UpdatePathInfomation()
		{
			filesCount = groupFileItems.Count;
			long num = 0L;
			lastModifyTime = DateTime.MinValue;
			for (int i = 0; i < groupFileItems.Count; i++)
			{
				num += groupFileItems[i].FileSize;
				if (lastModifyTime < groupFileItems[i].UploadTime)
				{
					lastModifyTime = groupFileItems[i].UploadTime;
				}
			}
			totalFileSize = num;
		}

		/// <summary>
		/// 缓存JSON文本的方法，该机制使用乐观并发模型完成<br />
		/// Method for caching JSON text, which is done using an optimistic concurrency model
		/// </summary>
		private void CacheJsonArrayContent()
		{
			lock (hybirdLock)
			{
				try
				{
					jsonArrayContent = JArray.FromObject(groupFileItems).ToString();
					using (StreamWriter streamWriter = new StreamWriter(fileFullPath, append: false, Encoding.UTF8))
					{
						streamWriter.Write(jsonArrayContent);
						streamWriter.Flush();
					}
				}
				catch (Exception ex)
				{
					LogNet?.WriteException("CacheJsonArrayContent", ex);
				}
			}
			this.FileCountChanged?.Invoke(this, filesCount);
		}

		/// <summary>
		/// 从目录进行加载数据，必须实例化的时候加载，加载失败会导致系统异常，旧的文件丢失<br />
		/// Load data from the directory, it must be loaded when instantiating. Failure to load will cause system exceptions and old files will be lost
		/// </summary>
		/// <param name="path">当前的文件夹路径信息</param>
		private void LoadByPath(string path)
		{
			fileFolderPath = path;
			fileFullPath = Path.Combine(path, "list.txt");
			if (!Directory.Exists(fileFolderPath))
			{
				Directory.CreateDirectory(fileFolderPath);
			}
			if (File.Exists(fileFullPath))
			{
				try
				{
					using (StreamReader streamReader = new StreamReader(fileFullPath, Encoding.UTF8))
					{
						groupFileItems = JArray.Parse(streamReader.ReadToEnd()).ToObject<List<GroupFileItem>>();
					}
				}
				catch (Exception ex)
				{
					LogNet?.WriteException("GroupFileContainer", "Load files txt failed,", ex);
				}
			}
			if (groupFileItems == null)
			{
				groupFileItems = new List<GroupFileItem>();
			}
			UpdatePathInfomation();
			coordinatorCacheJsonArray = new HslAsyncCoordinator(CacheJsonArrayContent);
			CacheJsonArrayContent();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "GroupFileContainer[" + dirPath + "]";
		}
	}
}

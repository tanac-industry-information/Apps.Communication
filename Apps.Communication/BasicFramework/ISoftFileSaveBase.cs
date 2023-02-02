namespace Apps.Communication.BasicFramework
{
	/// <summary>
	/// 支持字符串信息加载存储的接口，定义了几个通用的方法
	/// </summary>
	public interface ISoftFileSaveBase
	{
		/// <summary>
		/// 文件路径的存储
		/// </summary>
		string FileSavePath { get; set; }

		/// <summary>
		/// 获取需要保存的数据，需要重写实现
		/// </summary>
		/// <returns>需要存储的信息</returns>
		string ToSaveString();

		/// <summary>
		/// 从字符串加载数据，需要重写实现
		/// </summary>
		/// <param name="content">字符串数据</param>
		void LoadByString(string content);

		/// <summary>
		/// 不使用解密方法从文件读取数据
		/// </summary>
		void LoadByFile();

		/// <summary>
		/// 不使用加密方法保存数据到文件
		/// </summary>
		void SaveToFile();
	}
}

namespace Apps.Communication.Core.Security
{
	/// <summary>
	/// 加密解密的数据接口
	/// </summary>
	public interface ICryptography
	{
		/// <summary>
		/// 当前加密的密钥信息
		/// </summary>
		string Key { get; }

		/// <summary>
		/// 对原始的数据进行加密的操作，返回加密之后的二进制原始数据
		/// </summary>
		/// <param name="data">等待加密的数据</param>
		/// <returns>加密之后的二进制数据</returns>
		byte[] Encrypt(byte[] data);

		/// <summary>
		/// 对原始的数据进行解密的操作，返回解密之后的二进制原始数据
		/// </summary>
		/// <param name="data">等待解密的数据</param>
		/// <returns>解密之后的原始二进制数据</returns>
		byte[] Decrypt(byte[] data);
	}
}

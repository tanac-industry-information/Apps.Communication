using System.Security.Cryptography;
using System.Text;

namespace Apps.Communication.Core.Security
{
	/// <summary>
	/// DES加密解密的对象
	/// </summary>
	public class DesCryptography : ICryptography
	{
		private ICryptoTransform encryptTransform;

		private ICryptoTransform decryptTransform;

		private DESCryptoServiceProvider des;

		private string key;

		/// <inheritdoc cref="P:Communication.Core.Security.ICryptography.Key" />
		public string Key => key;

		/// <summary>
		/// 使用指定的密钥来实例化一个加密对象，该密钥右8位的字符和数字组成，例如 12345678
		/// </summary>
		/// <param name="key">密钥</param>
		public DesCryptography(string key)
		{
			this.key = key;
			des = new DESCryptoServiceProvider();
			des.Key = Encoding.ASCII.GetBytes(key);
			des.IV = Encoding.ASCII.GetBytes(key);
			encryptTransform = des.CreateEncryptor();
			decryptTransform = des.CreateDecryptor();
		}

		/// <inheritdoc cref="M:Communication.Core.Security.ICryptography.Encrypt(System.Byte[])" />
		public byte[] Encrypt(byte[] data)
		{
			if (data == null)
			{
				return null;
			}
			return encryptTransform.TransformFinalBlock(data, 0, data.Length);
		}

		/// <inheritdoc cref="M:Communication.Core.Security.ICryptography.Decrypt(System.Byte[])" />
		public byte[] Decrypt(byte[] data)
		{
			if (data == null)
			{
				return null;
			}
			return decryptTransform.TransformFinalBlock(data, 0, data.Length);
		}
	}
}

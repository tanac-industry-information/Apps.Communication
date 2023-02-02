using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Apps.Communication.WebSocket
{
	/// <summary>
	/// websocket的相关辅助的方法
	/// </summary>
	public class WebSocketHelper
	{
		/// <summary>
		/// 计算websocket返回得令牌
		/// </summary>
		/// <param name="webSocketKey">请求的令牌</param>
		/// <returns>返回的令牌</returns>
		public static string CalculateWebscoketSha1(string webSocketKey)
		{
			SHA1 sHA = new SHA1CryptoServiceProvider();
			byte[] inArray = sHA.ComputeHash(Encoding.UTF8.GetBytes(webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
			sHA.Dispose();
			return Convert.ToBase64String(inArray);
		}

		/// <summary>
		/// 根据http网页的信息，计算出返回的安全令牌
		/// </summary>
		/// <param name="httpGet">网页信息</param>
		/// <returns>返回的安全令牌</returns>
		public static string GetSecKeyAccetp(string httpGet)
		{
			string webSocketKey = string.Empty;
			Regex regex = new Regex("Sec\\-WebSocket\\-Key:(.*?)\\r\\n");
			Match match = regex.Match(httpGet);
			if (match.Success)
			{
				webSocketKey = Regex.Replace(match.Value, "Sec\\-WebSocket\\-Key:(.*?)\\r\\n", "$1").Trim();
			}
			return CalculateWebscoketSha1(webSocketKey);
		}

		/// <summary>
		/// 检测当前的反馈对象是否是标准的websocket请求
		/// </summary>
		/// <param name="httpGet">http的请求内容</param>
		/// <returns>是否验证成功</returns>
		public static OperateResult CheckWebSocketLegality(string httpGet)
		{
			if (Regex.IsMatch(httpGet, "Connection:[ ]*Upgrade", RegexOptions.IgnoreCase) && Regex.IsMatch(httpGet, "Upgrade:[ ]*websocket", RegexOptions.IgnoreCase))
			{
				return OperateResult.CreateSuccessResult();
			}
			return new OperateResult("Can't find Connection: Upgrade or Upgrade: websocket");
		}

		/// <summary>
		/// 从当前的websocket的HTTP请求头里，分析出订阅的主题内容
		/// </summary>
		/// <param name="httpGet">http的请求内容</param>
		/// <returns>是否验证成功</returns>
		public static string[] GetWebSocketSubscribes(string httpGet)
		{
			Regex regex = new Regex("HslSubscribes:[^\\r\\n]+");
			Match match = regex.Match(httpGet);
			if (!match.Success)
			{
				return null;
			}
			return match.Value.Substring(14).Trim().Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// 从当前的Websocket的Url里，分析出订阅的主题内容
		/// </summary>
		/// <param name="url">URL内容，例如 ws://127.0.0.1:1883/HslSubscribes=A,B</param>
		/// <returns>消息主题</returns>
		public static string[] GetWebSocketSubscribesFromUrl(string url)
		{
			Regex regex = new Regex("HslSubscribes=[\\s\\S]+$");
			Match match = regex.Match(url);
			if (!match.Success)
			{
				return null;
			}
			return match.Value.Substring(14).Trim().Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		/// <summary>
		/// 获取初步握手的时候的完整返回的数据信息
		/// </summary>
		/// <param name="httpGet">请求的网页信息</param>
		/// <returns>完整的返回信息</returns>
		public static OperateResult<byte[]> GetResponse(string httpGet)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("HTTP/1.1 101 Switching Protocols" + Environment.NewLine);
				stringBuilder.Append("Connection: Upgrade" + Environment.NewLine);
				stringBuilder.Append("Upgrade: websocket" + Environment.NewLine);
				stringBuilder.Append("Server:hsl websocket server" + Environment.NewLine);
				stringBuilder.Append("Access-Control-Allow-Credentials:true" + Environment.NewLine);
				stringBuilder.Append("Access-Control-Allow-Headers:content-type" + Environment.NewLine);
				stringBuilder.Append("Sec-WebSocket-Accept: " + GetSecKeyAccetp(httpGet) + Environment.NewLine + Environment.NewLine);
				return OperateResult.CreateSuccessResult(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
			}
			catch (Exception ex)
			{
				return new OperateResult<byte[]>(ex.Message);
			}
		}

		/// <summary>
		/// 创建连接服务器的http请求，输入订阅的主题信息
		/// </summary>
		/// <param name="ipAddress">远程服务器的ip地址</param>
		/// <param name="port">远程服务器的端口号</param>
		/// <param name="url">参数信息</param>
		/// <param name="subscribes">通知hsl的服务器，需要订阅的topic信息</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsSubRequest(string ipAddress, int port, string url, string[] subscribes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (subscribes != null)
			{
				stringBuilder.Append("HslSubscribes: ");
				for (int i = 0; i < subscribes.Length; i++)
				{
					stringBuilder.Append(subscribes[i]);
					if (i != subscribes.Length - 1)
					{
						stringBuilder.Append(",");
					}
				}
			}
			return BuildWsRequest(ipAddress, port, url, stringBuilder.ToString());
		}

		/// <summary>
		/// 创建连接服务器的http请求，采用问答的机制
		/// </summary>
		/// <param name="ipAddress">远程服务器的ip地址</param>
		/// <param name="port">远程服务器的端口号</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsQARequest(string ipAddress, int port)
		{
			return BuildWsRequest(ipAddress, port, string.Empty, "HslRequestAndAnswer: true");
		}

		/// <summary>
		/// 根据额外的参数信息，创建新的websocket的请求信息
		/// </summary>
		/// <param name="ipAddress">ip地址</param>
		/// <param name="port">端口号</param>
		/// <param name="url">跟在端口号后面的额外的参数信息</param>
		/// <param name="extra">额外的参数信息</param>
		/// <returns>报文信息</returns>
		public static byte[] BuildWsRequest(string ipAddress, int port, string url, string extra)
		{
			if (!url.StartsWith("/"))
			{
				url = "/" + url;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append($"GET ws://{ipAddress}:{port}{url} HTTP/1.1");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append($"Host: {ipAddress}:{port}");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Connection: Upgrade");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Pragma: no-cache");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Cache-Control: no-cache");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Upgrade: websocket");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append($"Origin: http://{ipAddress}:{port}");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Sec-WebSocket-Version: 13");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3314.0 Safari/537.36 SE 2.X MetaSr 1.0");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Accept-Encoding: gzip, deflate, br");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Accept-Language: zh-CN,zh;q=0.9");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Sec-WebSocket-Key: ia36apzXapB4YVxRfVyTuw==");
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.Append("Sec-WebSocket-Extensions: permessage-deflate; client_max_window_bits");
			stringBuilder.Append(Environment.NewLine);
			if (!string.IsNullOrEmpty(extra))
			{
				stringBuilder.Append(extra);
				stringBuilder.Append(Environment.NewLine);
			}
			stringBuilder.Append(Environment.NewLine);
			return Encoding.UTF8.GetBytes(stringBuilder.ToString());
		}

		/// <summary>
		/// 将普通的文本信息转换成websocket的报文
		/// </summary>
		/// <param name="opCode">操作信息码</param>
		/// <param name="isMask">是否使用掩码</param>
		/// <param name="message">等待转换的数据信息</param>
		/// <returns>数据包</returns>
		public static byte[] WebScoketPackData(int opCode, bool isMask, string message)
		{
			return WebScoketPackData(opCode, isMask, string.IsNullOrEmpty(message) ? new byte[0] : Encoding.UTF8.GetBytes(message));
		}

		/// <summary>
		/// 将普通的文本信息转换成websocket的报文
		/// </summary>
		/// <param name="opCode">操作信息码</param>
		/// <param name="isMask">是否使用掩码</param>
		/// <param name="payload">等待转换的数据信息</param>
		/// <returns>数据包</returns>
		public static byte[] WebScoketPackData(int opCode, bool isMask, byte[] payload)
		{
			if (payload == null)
			{
				payload = new byte[0];
			}
			byte[] array = payload.CopyArray();
			MemoryStream memoryStream = new MemoryStream();
			byte[] array2 = new byte[4] { 155, 3, 161, 168 };
			if (isMask)
			{
				Random random = new Random();
				random.NextBytes(array2);
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = (byte)(array[i] ^ array2[i % 4]);
				}
			}
			memoryStream.WriteByte((byte)(0x80u | (uint)opCode));
			if (array.Length < 126)
			{
				memoryStream.WriteByte((byte)(array.Length + (isMask ? 128 : 0)));
			}
			else if (array.Length <= 65535)
			{
				memoryStream.WriteByte((byte)(126 + (isMask ? 128 : 0)));
				byte[] bytes = BitConverter.GetBytes((ushort)array.Length);
				Array.Reverse(bytes);
				memoryStream.Write(bytes, 0, bytes.Length);
			}
			else
			{
				memoryStream.WriteByte((byte)(127 + (isMask ? 128 : 0)));
				byte[] bytes2 = BitConverter.GetBytes((ulong)array.Length);
				Array.Reverse(bytes2);
				memoryStream.Write(bytes2, 0, bytes2.Length);
			}
			if (isMask)
			{
				memoryStream.Write(array2, 0, array2.Length);
			}
			memoryStream.Write(array, 0, array.Length);
			byte[] result = memoryStream.ToArray();
			memoryStream.Dispose();
			return result;
		}
	}
}

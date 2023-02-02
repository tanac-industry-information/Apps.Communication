using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.Core.Security;
using Apps.Communication.Reflection;
using Newtonsoft.Json.Linq;

namespace Apps.Communication.MQTT
{
	/// <summary>
	/// Mqtt协议的辅助类，提供了一些协议相关的基础方法，方便客户端和服务器端一起调用。<br />
	/// The auxiliary class of the Mqtt protocol provides some protocol-related basic methods for the client and server to call together.
	/// </summary>
	public class MqttHelper
	{
		/// <summary>
		/// 根据数据的总长度，计算出剩余的数据长度信息<br />
		/// According to the total length of the data, calculate the remaining data length information
		/// </summary>
		/// <param name="length">数据的总长度</param>
		/// <returns>计算结果</returns>
		public static OperateResult<byte[]> CalculateLengthToMqttLength(int length)
		{
			if (length > 268435455)
			{
				return new OperateResult<byte[]>(StringResources.Language.MQTTDataTooLong);
			}
			if (length < 128)
			{
				return OperateResult.CreateSuccessResult(new byte[1] { (byte)length });
			}
			if (length < 16384)
			{
				return OperateResult.CreateSuccessResult(new byte[2]
				{
					(byte)(length % 128 + 128),
					(byte)(length / 128)
				});
			}
			if (length < 2097152)
			{
				return OperateResult.CreateSuccessResult(new byte[3]
				{
					(byte)(length % 128 + 128),
					(byte)(length / 128 % 128 + 128),
					(byte)(length / 128 / 128)
				});
			}
			return OperateResult.CreateSuccessResult(new byte[4]
			{
				(byte)(length % 128 + 128),
				(byte)(length / 128 % 128 + 128),
				(byte)(length / 128 / 128 % 128 + 128),
				(byte)(length / 128 / 128 / 128)
			});
		}

		/// <summary>
		/// 将一个数据打包成一个mqtt协议的内容<br />
		/// Pack a piece of data into a mqtt protocol
		/// </summary>
		/// <param name="control">控制码</param>
		/// <param name="flags">标记</param>
		/// <param name="variableHeader">可变头的字节内容</param>
		/// <param name="payLoad">负载数据</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildMqttCommand(byte control, byte flags, byte[] variableHeader, byte[] payLoad, AesCryptography aesCryptography = null)
		{
			control = (byte)(control << 4);
			byte head = (byte)(control | flags);
			return BuildMqttCommand(head, variableHeader, payLoad, aesCryptography);
		}

		/// <summary>
		/// 将一个数据打包成一个mqtt协议的内容<br />
		/// Pack a piece of data into a mqtt protocol
		/// </summary>
		/// <param name="head">控制码加标记码</param>
		/// <param name="variableHeader">可变头的字节内容</param>
		/// <param name="payLoad">负载数据</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>带有是否成功的结果对象</returns>
		public static OperateResult<byte[]> BuildMqttCommand(byte head, byte[] variableHeader, byte[] payLoad, AesCryptography aesCryptography = null)
		{
			if (variableHeader == null)
			{
				variableHeader = new byte[0];
			}
			if (payLoad == null)
			{
				payLoad = new byte[0];
			}
			if (aesCryptography != null)
			{
				payLoad = aesCryptography.Encrypt(payLoad);
			}
			OperateResult<byte[]> operateResult = CalculateLengthToMqttLength(variableHeader.Length + payLoad.Length);
			if (!operateResult.IsSuccess)
			{
				return operateResult;
			}
			MemoryStream memoryStream = new MemoryStream();
			memoryStream.WriteByte(head);
			memoryStream.Write(operateResult.Content, 0, operateResult.Content.Length);
			if (variableHeader.Length != 0)
			{
				memoryStream.Write(variableHeader, 0, variableHeader.Length);
			}
			if (payLoad.Length != 0)
			{
				memoryStream.Write(payLoad, 0, payLoad.Length);
			}
			return OperateResult.CreateSuccessResult(memoryStream.ToArray());
		}

		/// <summary>
		/// 将字符串打包成utf8编码，并且带有2个字节的表示长度的信息<br />
		/// Pack the string into utf8 encoding, and with 2 bytes of length information
		/// </summary>
		/// <param name="message">文本消息</param>
		/// <returns>打包之后的信息</returns>
		public static byte[] BuildSegCommandByString(string message)
		{
			byte[] array = (string.IsNullOrEmpty(message) ? new byte[0] : Encoding.UTF8.GetBytes(message));
			byte[] array2 = new byte[array.Length + 2];
			array.CopyTo(array2, 2);
			array2[0] = (byte)(array.Length / 256);
			array2[1] = (byte)(array.Length % 256);
			return array2;
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取文本信息<br />
		/// Extract text information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static string ExtraMsgFromBytes(byte[] buffer, ref int index)
		{
			int num = index;
			int num2 = buffer[index] * 256 + buffer[index + 1];
			index = index + 2 + num2;
			return Encoding.UTF8.GetString(buffer, num + 2, num2);
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取文本信息<br />
		/// Extract text information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static string ExtraSubscribeMsgFromBytes(byte[] buffer, ref int index)
		{
			int num = index;
			int num2 = buffer[index] * 256 + buffer[index + 1];
			index = index + 3 + num2;
			return Encoding.UTF8.GetString(buffer, num + 2, num2);
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取长度信息<br />
		/// Extract length information from MQTT cache information
		/// </summary>
		/// <param name="buffer">Mqtt的报文</param>
		/// <param name="index">索引</param>
		/// <returns>值</returns>
		public static int ExtraIntFromBytes(byte[] buffer, ref int index)
		{
			int result = buffer[index] * 256 + buffer[index + 1];
			index += 2;
			return result;
		}

		/// <summary>
		/// 从MQTT的缓存信息里，提取长度信息<br />
		/// Extract length information from MQTT cache information
		/// </summary>
		/// <param name="data">数据信息</param>
		/// <returns>值</returns>
		public static byte[] BuildIntBytes(int data)
		{
			return new byte[2]
			{
				BitConverter.GetBytes(data)[1],
				BitConverter.GetBytes(data)[0]
			};
		}

		/// <summary>
		/// 创建MQTT连接服务器的报文信息<br />
		/// Create MQTT connection server message information
		/// </summary>
		/// <param name="connectionOptions">连接配置</param>
		/// <param name="protocol">协议的内容</param>
		/// <param name="rsa">数据加密对象</param>
		/// <returns>返回是否成功的信息</returns>
		public static OperateResult<byte[]> BuildConnectMqttCommand(MqttConnectionOptions connectionOptions, string protocol = "MQTT", RSACryptoServiceProvider rsa = null)
		{
			List<byte> list = new List<byte>();
			list.AddRange(new byte[2] { 0, 4 });
			list.AddRange(Encoding.ASCII.GetBytes(protocol));
			list.Add(4);
			byte b = 0;
			if (connectionOptions.Credentials != null)
			{
				b = (byte)(b | 0x80u);
				b = (byte)(b | 0x40u);
			}
			if (connectionOptions.CleanSession)
			{
				b = (byte)(b | 2u);
			}
			list.Add(b);
			if (connectionOptions.KeepAlivePeriod.TotalSeconds < 1.0)
			{
				connectionOptions.KeepAlivePeriod = TimeSpan.FromSeconds(1.0);
			}
			byte[] bytes = BitConverter.GetBytes((int)connectionOptions.KeepAlivePeriod.TotalSeconds);
			list.Add(bytes[1]);
			list.Add(bytes[0]);
			List<byte> list2 = new List<byte>();
			list2.AddRange(BuildSegCommandByString(connectionOptions.ClientId));
			if (connectionOptions.Credentials != null)
			{
				list2.AddRange(BuildSegCommandByString(connectionOptions.Credentials.UserName));
				list2.AddRange(BuildSegCommandByString(connectionOptions.Credentials.Password));
			}
			if (rsa == null)
			{
				return BuildMqttCommand(1, 0, list.ToArray(), list2.ToArray());
			}
			return BuildMqttCommand(1, 0, rsa.EncryptLargeData(list.ToArray()), rsa.EncryptLargeData(list2.ToArray()));
		}

		/// <summary>
		/// 根据服务器返回的信息判断当前的连接是否是可用的<br />
		/// According to the information returned by the server to determine whether the current connection is available
		/// </summary>
		/// <param name="code">功能码</param>
		/// <param name="data">数据内容</param>
		/// <returns>是否可用的连接</returns>
		public static OperateResult CheckConnectBack(byte code, byte[] data)
		{
			if (code >> 4 != 2)
			{
				return new OperateResult("MQTT Connection Back Is Wrong: " + code);
			}
			if (data.Length < 2)
			{
				return new OperateResult("MQTT Connection Data Is Short: " + SoftBasic.ByteToHexString(data, ' '));
			}
			int num = data[1];
			int num2 = data[0];
			if (num > 0)
			{
				return new OperateResult(num, GetMqttCodeText(num));
			}
			return OperateResult.CreateSuccessResult();
		}

		/// <summary>
		/// 获取当前的错误的描述信息<br />
		/// Get a description of the current error
		/// </summary>
		/// <param name="status">状态信息</param>
		/// <returns>描述信息</returns>
		public static string GetMqttCodeText(int status)
		{
			switch (status)
			{
			case 1:
				return StringResources.Language.MQTTStatus01;
			case 2:
				return StringResources.Language.MQTTStatus02;
			case 3:
				return StringResources.Language.MQTTStatus03;
			case 4:
				return StringResources.Language.MQTTStatus04;
			case 5:
				return StringResources.Language.MQTTStatus05;
			default:
				return StringResources.Language.UnknownError;
			}
		}

		/// <summary>
		/// 创建Mqtt发送消息的命令<br />
		/// Create Mqtt command to send messages
		/// </summary>
		/// <param name="message">封装后的消息内容</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildPublishMqttCommand(MqttPublishMessage message, AesCryptography aesCryptography = null)
		{
			byte b = 0;
			if (!message.IsSendFirstTime)
			{
				b = (byte)(b | 8u);
			}
			if (message.Message.Retain)
			{
				b = (byte)(b | 1u);
			}
			if (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
			{
				b = (byte)(b | 2u);
			}
			else if (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
			{
				b = (byte)(b | 4u);
			}
			else if (message.Message.QualityOfServiceLevel == MqttQualityOfServiceLevel.OnlyTransfer)
			{
				b = (byte)(b | 6u);
			}
			List<byte> list = new List<byte>();
			list.AddRange(BuildSegCommandByString(message.Message.Topic));
			if (message.Message.QualityOfServiceLevel != 0)
			{
				list.Add(BitConverter.GetBytes(message.Identifier)[1]);
				list.Add(BitConverter.GetBytes(message.Identifier)[0]);
			}
			return BuildMqttCommand(3, b, list.ToArray(), message.Message.Payload, aesCryptography);
		}

		/// <summary>
		/// 创建Mqtt发送消息的命令<br />
		/// Create Mqtt command to send messages
		/// </summary>
		/// <param name="topic">主题消息内容</param>
		/// <param name="payload">数据负载</param>
		/// <param name="aesCryptography">AES数据加密对象</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildPublishMqttCommand(string topic, byte[] payload, AesCryptography aesCryptography = null)
		{
			return BuildMqttCommand(3, 0, BuildSegCommandByString(topic), payload, aesCryptography);
		}

		/// <summary>
		/// 创建Mqtt订阅消息的命令<br />
		/// Command to create Mqtt subscription message
		/// </summary>
		/// <param name="message">订阅的主题</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildSubscribeMqttCommand(MqttSubscribeMessage message)
		{
			List<byte> list = new List<byte>();
			List<byte> list2 = new List<byte>();
			list.Add(BitConverter.GetBytes(message.Identifier)[1]);
			list.Add(BitConverter.GetBytes(message.Identifier)[0]);
			for (int i = 0; i < message.Topics.Length; i++)
			{
				list2.AddRange(BuildSegCommandByString(message.Topics[i]));
				if (message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
				{
					list2.AddRange(new byte[1]);
				}
				else if (message.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
				{
					list2.AddRange(new byte[1] { 1 });
				}
				else
				{
					list2.AddRange(new byte[1] { 2 });
				}
			}
			return BuildMqttCommand(8, 2, list.ToArray(), list2.ToArray());
		}

		/// <summary>
		/// 创建Mqtt取消订阅消息的命令<br />
		/// Create Mqtt unsubscribe message command
		/// </summary>
		/// <param name="message">订阅的主题</param>
		/// <returns>结果内容</returns>
		public static OperateResult<byte[]> BuildUnSubscribeMqttCommand(MqttSubscribeMessage message)
		{
			List<byte> list = new List<byte>();
			List<byte> list2 = new List<byte>();
			list.Add(BitConverter.GetBytes(message.Identifier)[1]);
			list.Add(BitConverter.GetBytes(message.Identifier)[0]);
			for (int i = 0; i < message.Topics.Length; i++)
			{
				list2.AddRange(BuildSegCommandByString(message.Topics[i]));
			}
			return BuildMqttCommand(10, 2, list.ToArray(), list2.ToArray());
		}

		internal static OperateResult<MqttClientApplicationMessage> ParseMqttClientApplicationMessage(MqttSession session, byte code, byte[] data, AesCryptography aesCryptography = null)
		{
			bool flag = (code & 8) == 8;
			int num = (((code & 4) == 4) ? 2 : 0) + (((code & 2) == 2) ? 1 : 0);
			MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce;
			switch (num)
			{
			case 1:
				qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce;
				break;
			case 2:
				qualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce;
				break;
			case 3:
				qualityOfServiceLevel = MqttQualityOfServiceLevel.OnlyTransfer;
				break;
			}
			bool retain = (code & 1) == 1;
			int msgID = 0;
			int index = 0;
			string topic = ExtraMsgFromBytes(data, ref index);
			if (num > 0)
			{
				msgID = ExtraIntFromBytes(data, ref index);
			}
			byte[] array = SoftBasic.ArrayRemoveBegin(data, index);
			if (session.AesCryptography)
			{
				try
				{
					if (array.Length != 0)
					{
						array = aesCryptography.Decrypt(array);
					}
				}
				catch (Exception ex)
				{
					return new OperateResult<MqttClientApplicationMessage>("AES Decrypt failed: " + ex.Message);
				}
			}
			MqttClientApplicationMessage value = new MqttClientApplicationMessage
			{
				ClientId = session.ClientId,
				QualityOfServiceLevel = qualityOfServiceLevel,
				Retain = retain,
				Topic = topic,
				UserName = session.UserName,
				Payload = array,
				MsgID = msgID
			};
			return OperateResult.CreateSuccessResult(value);
		}

		/// <summary>
		/// 解析从MQTT接受的客户端信息，解析成实际的Topic数据及Payload数据<br />
		/// Parse the client information received from MQTT and parse it into actual Topic data and Payload data
		/// </summary>
		/// <param name="mqttCode">MQTT的命令码</param>
		/// <param name="data">接收的MQTT原始的消息内容</param>
		/// <param name="aesCryptography">AES数据加密信息</param>
		/// <returns>解析的数据结果信息</returns>
		public static OperateResult<string, byte[]> ExtraMqttReceiveData(byte mqttCode, byte[] data, AesCryptography aesCryptography = null)
		{
			if (data.Length < 2)
			{
				return new OperateResult<string, byte[]>(StringResources.Language.ReceiveDataLengthTooShort + data.Length);
			}
			int num = data[0] * 256 + data[1];
			if (data.Length < 2 + num)
			{
				return new OperateResult<string, byte[]>($"Code[{mqttCode:X2}] ExtraMqttReceiveData Error: {SoftBasic.ByteToHexString(data, ' ')}");
			}
			string value = ((num > 0) ? Encoding.UTF8.GetString(data, 2, num) : string.Empty);
			byte[] array = new byte[data.Length - num - 2];
			Array.Copy(data, num + 2, array, 0, array.Length);
			if (aesCryptography != null)
			{
				try
				{
					array = aesCryptography.Decrypt(array);
				}
				catch (Exception ex)
				{
					return new OperateResult<string, byte[]>("AES Decrypt failed: " + ex.Message);
				}
			}
			return OperateResult.CreateSuccessResult(value, array);
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="mqttSession">当前的对话状态</param>
		/// <param name="message">当前传入的消息内容</param>
		/// <param name="obj">等待解析的api解析的对象</param>
		/// <returns>等待返回客户的结果</returns>
		public static async Task<OperateResult<string>> HandleObjectMethod(MqttSession mqttSession, MqttClientApplicationMessage message, object obj)
		{
			string method = message.Topic;
			if (method.LastIndexOf('/') >= 0)
			{
				method = method.Substring(method.LastIndexOf('/') + 1);
			}
			MethodInfo methodInfo = obj.GetType().GetMethod(method);
			if (methodInfo == null)
			{
				return new OperateResult<string>("Current MqttSync Api ：[" + method + "] not exsist");
			}
			OperateResult<MqttRpcApiInfo> apiResult = GetMqttSyncServicesApiFromMethod("", methodInfo, obj);
			if (!apiResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(apiResult);
			}
			return await HandleObjectMethod(mqttSession, message, apiResult.Content);
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="mqttSession">当前的对话状态</param>
		/// <param name="message">当前传入的消息内容</param>
		/// <param name="apiInformation">当前已经解析好的Api内容对象</param>
		/// <returns>等待返回客户的结果</returns>
		public static async Task<OperateResult<string>> HandleObjectMethod(MqttSession mqttSession, MqttClientApplicationMessage message, MqttRpcApiInfo apiInformation)
		{
			object retObject = null;
			if (apiInformation.PermissionAttribute != null)
			{

				if (!apiInformation.PermissionAttribute.CheckClientID(mqttSession.ClientId))
				{
					return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] Check ClientID[" + mqttSession.ClientId + "] failed, access not permission");
				}
				if (!apiInformation.PermissionAttribute.CheckUserName(mqttSession.UserName))
				{
					return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] Check Username[" + mqttSession.UserName + "] failed, access not permission");
				}
			}
			try
			{
				if (apiInformation.Method != null)
				{
					string json = Encoding.UTF8.GetString(message.Payload);
					if (!string.IsNullOrEmpty(json))
					{
						JObject.Parse(json);
					}
					else
					{
						new JObject();
					}
					object[] paras = HslReflectionHelper.GetParametersFromJson(mqttSession, apiInformation.Method.GetParameters(), json);
					object obj = apiInformation.Method.Invoke(apiInformation.SourceObject, paras);
					Task task = obj as Task;
					if (task != null)
					{
						await task;
						retObject = task.GetType().GetProperty("Result")?.GetValue(task, null);
					}
					else
					{
						retObject = obj;
					}
				}
				else if (apiInformation.Property != null)
				{
					retObject = apiInformation.Property.GetValue(apiInformation.SourceObject, null);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] Wrong，Reason：" + ex.Message);
			}
			return HslReflectionHelper.GetOperateResultJsonFromObj(retObject);
		}

		/// <inheritdoc cref="M:Communication.MQTT.MqttHelper.GetSyncServicesApiInformationFromObject(System.String,System.Object,Communication.Reflection.HslMqttPermissionAttribute)" />
		public static List<MqttRpcApiInfo> GetSyncServicesApiInformationFromObject(object obj)
		{
			Type type = obj as Type;
			if ((object)type != null)
			{
				return GetSyncServicesApiInformationFromObject(type.Name, type);
			}
			return GetSyncServicesApiInformationFromObject(obj.GetType().Name, obj);
		}

		/// <summary>
		/// 根据当前的对象定义的方法信息，获取到所有支持ApiTopic的方法列表信息，包含API名称，示例参数数据，描述信息。<br />
		/// According to the method information defined by the current object, the list information of all methods that support ApiTopic is obtained, 
		/// including the API name, sample parameter data, and description information.
		/// </summary>
		/// <param name="api">指定的ApiTopic的前缀，可以理解为控制器，如果为空，就不携带控制器。</param>
		/// <param name="obj">实际的等待解析的对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回所有API说明的列表，类型为<see cref="T:Communication.MQTT.MqttRpcApiInfo" /></returns>
		public static List<MqttRpcApiInfo> GetSyncServicesApiInformationFromObject(string api, object obj, HslMqttPermissionAttribute permissionAttribute = null)
		{
			Type type = null;
			Type type2 = obj as Type;
			if ((object)type2 != null)
			{
				type = type2;
				obj = null;
			}
			else
			{
				type = obj.GetType();
			}
			MethodInfo[] methods = type.GetMethods();
			List<MqttRpcApiInfo> list = new List<MqttRpcApiInfo>();
			MethodInfo[] array = methods;
			foreach (MethodInfo method in array)
			{
				OperateResult<MqttRpcApiInfo> mqttSyncServicesApiFromMethod = GetMqttSyncServicesApiFromMethod(api, method, obj, permissionAttribute);
				if (mqttSyncServicesApiFromMethod.IsSuccess)
				{
					list.Add(mqttSyncServicesApiFromMethod.Content);
				}
			}
			PropertyInfo[] properties = type.GetProperties();
			PropertyInfo[] array2 = properties;
			foreach (PropertyInfo propertyInfo in array2)
			{
				OperateResult<HslMqttApiAttribute, MqttRpcApiInfo> mqttSyncServicesApiFromProperty = GetMqttSyncServicesApiFromProperty(api, propertyInfo, obj, permissionAttribute);
				if (mqttSyncServicesApiFromProperty.IsSuccess)
				{
					if (!mqttSyncServicesApiFromProperty.Content1.PropertyUnfold)
					{
						list.Add(mqttSyncServicesApiFromProperty.Content2);
					}
					else if (propertyInfo.GetValue(obj, null) != null)
					{
						List<MqttRpcApiInfo> syncServicesApiInformationFromObject = GetSyncServicesApiInformationFromObject(mqttSyncServicesApiFromProperty.Content2.ApiTopic, propertyInfo.GetValue(obj, null), permissionAttribute);
						list.AddRange(syncServicesApiInformationFromObject);
					}
				}
			}
			return list;
		}

		private static string GetReturnTypeDescription(Type returnType)
		{
			if (returnType.IsSubclassOf(typeof(OperateResult)))
			{
				if (returnType == typeof(OperateResult))
				{
					return returnType.Name;
				}
				if (returnType.GetProperty("Content") != null)
				{
					return "OperateResult<" + returnType.GetProperty("Content").PropertyType.Name + ">";
				}
				StringBuilder stringBuilder = new StringBuilder("OperateResult<");
				for (int i = 1; i <= 10; i++)
				{
					if (!(returnType.GetProperty("Content" + i) != null))
					{
						break;
					}
					if (i != 1)
					{
						stringBuilder.Append(",");
					}
					stringBuilder.Append(returnType.GetProperty("Content" + i).PropertyType.Name);
				}
				stringBuilder.Append(">");
				return stringBuilder.ToString();
			}
			return returnType.Name;
		}

		/// <summary>
		/// 根据当前的方法的委托信息和类对象，生成<see cref="T:Communication.MQTT.MqttRpcApiInfo" />的API对象信息。
		/// </summary>
		/// <param name="api">Api头信息</param>
		/// <param name="method">方法的委托</param>
		/// <param name="obj">当前注册的API的源对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<MqttRpcApiInfo> GetMqttSyncServicesApiFromMethod(string api, MethodInfo method, object obj, HslMqttPermissionAttribute permissionAttribute = null)
		{
			object[] customAttributes = method.GetCustomAttributes(typeof(HslMqttApiAttribute), inherit: false);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return new OperateResult<MqttRpcApiInfo>($"Current Api ：[{method}] not support Api attribute");
			}
			HslMqttApiAttribute hslMqttApiAttribute = (HslMqttApiAttribute)customAttributes[0];
			MqttRpcApiInfo mqttRpcApiInfo = new MqttRpcApiInfo();
			mqttRpcApiInfo.SourceObject = obj;
			mqttRpcApiInfo.Method = method;
			mqttRpcApiInfo.Description = hslMqttApiAttribute.Description;
			mqttRpcApiInfo.HttpMethod = hslMqttApiAttribute.HttpMethod.ToUpper();
			if (string.IsNullOrEmpty(hslMqttApiAttribute.ApiTopic))
			{
				hslMqttApiAttribute.ApiTopic = method.Name;
			}
			if (permissionAttribute == null)
			{
				customAttributes = method.GetCustomAttributes(typeof(HslMqttPermissionAttribute), inherit: false);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					mqttRpcApiInfo.PermissionAttribute = (HslMqttPermissionAttribute)customAttributes[0];
				}
			}
			else
			{
				mqttRpcApiInfo.PermissionAttribute = permissionAttribute;
			}
			if (string.IsNullOrEmpty(api))
			{
				mqttRpcApiInfo.ApiTopic = hslMqttApiAttribute.ApiTopic;
			}
			else
			{
				mqttRpcApiInfo.ApiTopic = api + "/" + hslMqttApiAttribute.ApiTopic;
			}
			ParameterInfo[] parameters = method.GetParameters();
			StringBuilder stringBuilder = new StringBuilder();
			if (method.ReturnType.IsSubclassOf(typeof(Task)))
			{
				stringBuilder.Append("Task<" + GetReturnTypeDescription(method.ReturnType.GetProperty("Result").PropertyType) + ">");
			}
			else
			{
				stringBuilder.Append(GetReturnTypeDescription(method.ReturnType));
			}
			stringBuilder.Append(" ");
			stringBuilder.Append(mqttRpcApiInfo.ApiTopic);
			stringBuilder.Append("(");
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType != typeof(ISessionContext))
				{
					stringBuilder.Append(parameters[i].ParameterType.Name);
					stringBuilder.Append(" ");
					stringBuilder.Append(parameters[i].Name);
					if (i != parameters.Length - 1)
					{
						stringBuilder.Append(",");
					}
				}
			}
			stringBuilder.Append(")");
			mqttRpcApiInfo.MethodSignature = stringBuilder.ToString();
			mqttRpcApiInfo.ExamplePayload = HslReflectionHelper.GetParametersFromJson(method, parameters).ToString();
			return OperateResult.CreateSuccessResult(mqttRpcApiInfo);
		}

		/// <summary>
		/// 根据当前的方法的委托信息和类对象，生成<see cref="T:Communication.MQTT.MqttRpcApiInfo" />的API对象信息。
		/// </summary>
		/// <param name="api">Api头信息</param>
		/// <param name="property">方法的委托</param>
		/// <param name="obj">当前注册的API的源对象</param>
		/// <param name="permissionAttribute">默认的权限特性</param>
		/// <returns>返回是否成功的结果对象</returns>
		public static OperateResult<HslMqttApiAttribute, MqttRpcApiInfo> GetMqttSyncServicesApiFromProperty(string api, PropertyInfo property, object obj, HslMqttPermissionAttribute permissionAttribute = null)
		{
			object[] customAttributes = property.GetCustomAttributes(typeof(HslMqttApiAttribute), inherit: false);
			if (customAttributes == null || customAttributes.Length == 0)
			{
				return new OperateResult<HslMqttApiAttribute, MqttRpcApiInfo>($"Current Api ：[{property}] not support Api attribute");
			}
			HslMqttApiAttribute hslMqttApiAttribute = (HslMqttApiAttribute)customAttributes[0];
			MqttRpcApiInfo mqttRpcApiInfo = new MqttRpcApiInfo();
			mqttRpcApiInfo.SourceObject = obj;
			mqttRpcApiInfo.Property = property;
			mqttRpcApiInfo.Description = hslMqttApiAttribute.Description;
			mqttRpcApiInfo.HttpMethod = hslMqttApiAttribute.HttpMethod.ToUpper();
			if (string.IsNullOrEmpty(hslMqttApiAttribute.ApiTopic))
			{
				hslMqttApiAttribute.ApiTopic = property.Name;
			}
			if (permissionAttribute == null)
			{
				customAttributes = property.GetCustomAttributes(typeof(HslMqttPermissionAttribute), inherit: false);
				if (customAttributes != null && customAttributes.Length != 0)
				{
					mqttRpcApiInfo.PermissionAttribute = (HslMqttPermissionAttribute)customAttributes[0];
				}
			}
			else
			{
				mqttRpcApiInfo.PermissionAttribute = permissionAttribute;
			}
			if (string.IsNullOrEmpty(api))
			{
				mqttRpcApiInfo.ApiTopic = hslMqttApiAttribute.ApiTopic;
			}
			else
			{
				mqttRpcApiInfo.ApiTopic = api + "/" + hslMqttApiAttribute.ApiTopic;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(GetReturnTypeDescription(property.PropertyType));
			stringBuilder.Append(" ");
			stringBuilder.Append(mqttRpcApiInfo.ApiTopic);
			stringBuilder.Append(" { ");
			if (property.CanRead)
			{
				stringBuilder.Append("get; ");
			}
			if (property.CanWrite)
			{
				stringBuilder.Append("set; ");
			}
			stringBuilder.Append("}");
			mqttRpcApiInfo.MethodSignature = stringBuilder.ToString();
			mqttRpcApiInfo.ExamplePayload = string.Empty;
			return OperateResult.CreateSuccessResult(hslMqttApiAttribute, mqttRpcApiInfo);
		}

		/// <summary>
		/// 判断当前服务器的实际的 topic 的主题，是否满足通配符格式的订阅主题 subTopic
		/// </summary>
		/// <param name="topic">服务器的实际的主题信息</param>
		/// <param name="subTopic">客户端订阅的基于通配符的格式</param>
		/// <returns>如果返回True, 说明当前匹配成功，应该发送订阅操作</returns>
		public static bool CheckMqttTopicWildcards(string topic, string subTopic)
		{
			if (subTopic == "#")
			{
				return true;
			}
			if (subTopic.EndsWith("/#"))
			{
				if (subTopic.Contains("/+/"))
				{
					subTopic = subTopic.Replace("[", "\\[");
					subTopic = subTopic.Replace("]", "\\]");
					subTopic = subTopic.Replace(".", "\\.");
					subTopic = subTopic.Replace("*", "\\*");
					subTopic = subTopic.Replace("{", "\\{");
					subTopic = subTopic.Replace("}", "\\}");
					subTopic = subTopic.Replace("?", "\\?");
					subTopic = subTopic.Replace("$", "\\$");
					subTopic = subTopic.Replace("/+", "/[^/]+");
					subTopic = subTopic.RemoveLast(2);
					subTopic += "(/[\\S\\s]+$|$)";
					return Regex.IsMatch(topic, subTopic);
				}
				if (subTopic.Length == 2)
				{
					return false;
				}
				if (topic == subTopic.RemoveLast(2))
				{
					return true;
				}
				if (topic.StartsWith(subTopic.RemoveLast(1)))
				{
					return true;
				}
				return false;
			}
			if (subTopic == "+")
			{
				return !topic.Contains("/");
			}
			if (subTopic.EndsWith("/+"))
			{
				if (subTopic.Length == 2)
				{
					return false;
				}
				if (!topic.StartsWith(subTopic.RemoveLast(1)))
				{
					return false;
				}
				if (topic.Length == subTopic.Length - 1)
				{
					return false;
				}
				if (topic.Substring(subTopic.Length - 1).Contains("/"))
				{
					return false;
				}
				return true;
			}
			if (subTopic.Contains("/+/"))
			{
				subTopic = subTopic.Replace("[", "\\[");
				subTopic = subTopic.Replace("]", "\\]");
				subTopic = subTopic.Replace(".", "\\.");
				subTopic = subTopic.Replace("*", "\\*");
				subTopic = subTopic.Replace("{", "\\{");
				subTopic = subTopic.Replace("}", "\\}");
				subTopic = subTopic.Replace("?", "\\?");
				subTopic = subTopic.Replace("$", "\\$");
				subTopic = subTopic.Replace("/+", "/[^/]+");
				return Regex.IsMatch(topic, subTopic);
			}
			return topic == subTopic;
		}
	}
}

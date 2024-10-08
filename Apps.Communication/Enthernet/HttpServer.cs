using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Apps.Communication.BasicFramework;
using Apps.Communication.Core;
using Apps.Communication.LogNet;
using Apps.Communication.MQTT;
using Apps.Communication.Reflection;

namespace Apps.Communication.Enthernet
{
	/// <summary>
	/// 一个支持完全自定义的Http服务器，支持返回任意的数据信息，方便调试信息，详细的案例请查看API文档信息<br />
	/// A Http server that supports fully customized, supports returning arbitrary data information, which is convenient for debugging information. For detailed cases, please refer to the API documentation information
	/// </summary>
	/// <example>
	/// 我们先来看看一个最简单的例子，如何进行实例化的操作。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\HttpServerSample.cs" region="Sample1" title="基本的实例化" />
	/// 通常来说，基本的实例化，返回固定的数据并不能满足我们的需求，我们需要返回自定义的数据，有一个委托，我们需要自己指定方法.
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\HttpServerSample.cs" region="Sample2" title="自定义返回" />
	/// 我们实际的需求可能会更加的复杂，不同的网址会返回不同的数据，所以接下来我们需要对网址信息进行判断。
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\HttpServerSample.cs" region="Sample3" title="区分网址" />
	/// 如果我们想增加安全性的验证功能，比如我们的api接口需要增加用户名和密码的功能，那么我们也可以实现
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\HttpServerSample.cs" region="Sample4" title="安全实现" />
	/// 当然了，如果我们想反回一个完整的html网页，也是可以实现的，甚至添加一些js的脚本，下面的例子就简单的说明了如何操作
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Enthernet\HttpServerSample.cs" region="Sample5" title="返回html" />
	/// 如果需要实现跨域的操作，可以将属性<see cref="P:Communication.Enthernet.HttpServer.IsCrossDomain" /> 设置为<c>True</c>
	/// </example>
	public class HttpServer
	{
		private Dictionary<string, MqttRpcApiInfo> apiTopicServiceDict;

		private object rpcApiLock;

		private int receiveBufferSize = 2048;

		private int port = 80;

		private HttpListener listener;

		private ILogNet logNet;

		private Encoding encoding = Encoding.UTF8;

		private Func<HttpListenerRequest, HttpListenerResponse, string, string> handleRequestFunc;

		private LogStatisticsDict statisticsDict;

		private bool loginAccess = false;

		private MqttCredential[] loginCredentials;

		/// <summary>
		/// 获取当前的日志统计信息，可以获取到每个API的每天的调度次数信息，缓存60天数据，如果需要存储本地，需要调用<see cref="M:Communication.LogNet.LogStatisticsDict.SaveToFile(System.String)" />方法。<br />
		/// Get the current log statistics, you can get the daily scheduling times information of each API, and cache 60-day data. 
		/// If you need to store it locally, you need to call the <see cref="M:Communication.LogNet.LogStatisticsDict.SaveToFile(System.String)" /> method.
		/// </summary>
		public LogStatisticsDict LogStatistics => statisticsDict;

		/// <inheritdoc cref="P:Communication.Core.Net.NetworkBase.LogNet" />
		public ILogNet LogNet
		{
			get
			{
				return logNet;
			}
			set
			{
				logNet = value;
			}
		}

		/// <summary>
		/// 获取或设置当前服务器的编码信息，默认为UTF8编码<br />
		/// Get or set the encoding information of the current server, the default is UTF8 encoding
		/// </summary>
		public Encoding ServerEncoding
		{
			get
			{
				return encoding;
			}
			set
			{
				encoding = value;
			}
		}

		/// <summary>
		/// 获取或设置是否支持跨域操作<br />
		/// Get or set whether to support cross-domain operations
		/// </summary>
		public bool IsCrossDomain { get; set; }

		/// <summary>
		/// 获取或设置当前的自定义的处理信息，如果不想继承实现方法，可以使用本属性来关联你自定义的方法。<br />
		/// Get or set the current custom processing information. If you don't want to inherit the implementation method, you can use this attribute to associate your custom method.
		/// </summary>
		public Func<HttpListenerRequest, HttpListenerResponse, string, string> HandleRequestFunc
		{
			get
			{
				return handleRequestFunc;
			}
			set
			{
				handleRequestFunc = value;
			}
		}

		/// <summary>
		/// 获取当前的端口号信息<br />
		/// Get current port number information
		/// </summary>
		public int Port => port;

		/// <summary>
		/// 实例化一个默认的对象，当前的运行，需要使用管理员的模式运行<br />
		/// Instantiate a default object, the current operation, you need to use the administrator mode to run
		/// </summary>
		public HttpServer()
		{
			statisticsDict = new LogStatisticsDict(GenerateMode.ByEveryDay, 60);
			apiTopicServiceDict = new Dictionary<string, MqttRpcApiInfo>();
			rpcApiLock = new object();
		}

		/// <summary>
		/// 启动服务器，正常调用该方法时，应该使用try...catch...来捕获错误信息<br />
		/// Start the server and use try...catch... to capture the error message when calling this method normally
		/// </summary>
		/// <param name="port">端口号信息</param>
		/// <exception cref="T:System.Net.HttpListenerException"></exception>
		/// <exception cref="T:System.ObjectDisposedException"></exception>
		public void Start(int port)
		{
			this.port = port;
			listener = new HttpListener();
			listener.Prefixes.Add($"http://+:{port}/");
			listener.Start();
			listener.BeginGetContext(GetConnectCallBack, listener);
			logNet?.WriteDebug(ToString(), "Server Started, wait for connections");
		}

		/// <summary>
		/// 关闭服务器<br />
		/// Shut down the server
		/// </summary>
		public void Close()
		{
			listener?.Close();
		}

		private async void GetConnectCallBack(IAsyncResult ar)
		{
			object asyncState = ar.AsyncState;
			HttpListener listener = asyncState as HttpListener;
			if (listener == null)
			{
				return;
			}
			HttpListenerContext context = null;
			try
			{
				context = listener.EndGetContext(ar);
			}
			catch (Exception ex5)
			{
				Exception ex2 = ex5;
				logNet?.WriteException(ToString(), ex2);
			}
			int restartcount = 0;
			while (true)
			{
				try
				{
					listener.BeginGetContext(GetConnectCallBack, listener);
				}
				catch (Exception ex5)
				{
					Exception ex4 = ex5;
					logNet?.WriteException(ToString(), ex4);
					restartcount++;
					if (restartcount >= 3)
					{
						logNet?.WriteError(ToString(), "ReGet Content Failed!");
						return;
					}
					Thread.Sleep(1000);
					continue;
				}
				break;
			}
			if (context == null)
			{
				return;
			}
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			if (response != null)
			{
				try
				{
					if (IsCrossDomain)
					{
						context.Response.AppendHeader("Access-Control-Allow-Origin", request.Headers["Origin"]);
						context.Response.AppendHeader("Access-Control-Allow-Headers", "*");
						context.Response.AppendHeader("Access-Control-Allow-Method", "POST,GET,PUT,OPTIONS,DELETE");
						context.Response.AppendHeader("Access-Control-Allow-Credentials", "true");
						context.Response.AppendHeader("Access-Control-Max-Age", "3600");
					}
					context.Response.AddHeader("Content-type", "text/html; charset=utf-8");
				}
				catch (Exception ex5)
				{
					Exception ex3 = ex5;
					logNet?.WriteError(ToString(), ex3.Message);
				}
			}
			string data = await GetDataFromRequestAsync(request);
			response.StatusCode = 200;
			try
			{
				string ret = await HandleRequest(request, response, data);
				using (Stream stream = response.OutputStream)
				{
					if (string.IsNullOrEmpty(ret))
					{
						await stream.WriteAsync(new byte[0], 0, 0);
						return;
					}
					byte[] buffer = encoding.GetBytes(ret);
					await stream.WriteAsync(buffer, 0, buffer.Length);
				}
			}
			catch (Exception ex)
			{
				logNet?.WriteException(ToString(), "Handle Request[" + request.HttpMethod + "], " + request.RawUrl, ex);
			}
		}

		private string GetDataFromRequest(HttpListenerRequest request)
		{
			try
			{
				List<byte> list = new List<byte>();
				byte[] array = new byte[receiveBufferSize];
				int num = 0;
				int num2 = 0;
				do
				{
					num = request.InputStream.Read(array, 0, array.Length);
					num2 += num;
					list.AddRange(SoftBasic.ArraySelectBegin(array, num));
				}
				while (num != 0);
				return encoding.GetString(list.ToArray(), 0, num2);
			}
			catch
			{
				return string.Empty;
			}
		}

		private async Task<string> GetDataFromRequestAsync(HttpListenerRequest request)
		{
			try
			{
				List<byte> byteList = new List<byte>();
				byte[] byteArr = new byte[receiveBufferSize];
				int len = 0;
				int readLen;
				do
				{
					readLen = await request.InputStream.ReadAsync(byteArr, 0, byteArr.Length);
					len += readLen;
					byteList.AddRange(SoftBasic.ArraySelectBegin(byteArr, readLen));
				}
				while (readLen != 0);
				return encoding.GetString(byteList.ToArray(), 0, len);
			}
			catch
			{
				return string.Empty;
			}
		}

		/// <summary>
		/// 根据客户端的请求进行处理的核心方法，可以返回自定义的数据内容，只需要集成重写即可。<br />
		/// The core method of processing according to the client's request can return custom data content, and only needs to be integrated and rewritten.
		/// </summary>
		/// <param name="request">请求</param>
		/// <param name="response">回应</param>
		/// <param name="data">Body数据</param>
		/// <returns>返回的内容</returns>
		protected virtual async Task<string> HandleRequest(HttpListenerRequest request, HttpListenerResponse response, string data)
		{
			if (request.HttpMethod == "OPTIONS")
			{
				return "OK";
			}
			if (loginAccess)
			{
				string[] values = request.Headers.GetValues("Authorization");
				if (values == null || values.Length < 1 || string.IsNullOrEmpty(values[0]))
				{
					response.StatusCode = 401;
					response.AddHeader("WWW-Authenticate", "Basic realm=\"Secure Area\"");
					return "";
				}
				try
				{
					string base64String = values[0].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
					string accountString = Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
					string[] account = accountString.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					if (account.Length < 2)
					{
						response.StatusCode = 401;
						response.AddHeader("WWW-Authenticate", "Basic realm=\"Secure Area\"");
						return "";
					}
					MqttCredential[] credentials = loginCredentials;
					bool loginEnable = false;
					for (int i = 0; i < credentials.Length; i++)
					{
						if (account[0] == credentials[i].UserName && account[1] == credentials[i].Password)
						{
							loginEnable = true;
							break;
						}
					}
					if (!loginEnable)
					{
						response.StatusCode = 401;
						response.AddHeader("WWW-Authenticate", "Basic realm=\"Secure Area\"");
						return "";
					}
				}
				catch
				{
					response.StatusCode = 401;
					response.AddHeader("WWW-Authenticate", "Basic realm=\"Secure Area\"");
					return "";
				}
			}
			if (request.HttpMethod == "HSL")
			{
				if (request.RawUrl.StartsWith("/Apis"))
				{
					response.AddHeader("Content-type", "application/json; charset=utf-8");
					return GetAllRpcApiInfo().ToJsonString();
				}
				if (request.RawUrl.StartsWith("/Logs"))
				{
					response.AddHeader("Content-type", "application/json; charset=utf-8");
					if (request.RawUrl == "/Logs" || request.RawUrl == "/Logs/")
					{
						return LogStatistics.LogStat.GetStatisticsSnapshot().ToJsonString();
					}
					return LogStatistics.GetStatisticsSnapshot(request.RawUrl.Substring(6)).ToJsonString();
				}
				response.AddHeader("Content-type", "application/json; charset=utf-8");
				return GetAllRpcApiInfo().ToJsonString();
			}
			if (request.HttpMethod == "OPTIONS")
			{
				return "OK";
			}
			MqttRpcApiInfo apiInformation = GetMqttRpcApiInfo(GetMethodName(HttpUtility.UrlDecode(request.RawUrl)));
			if (apiInformation == null)
			{
				if (HandleRequestFunc != null)
				{
					return HandleRequestFunc(request, response, data);
				}
				return "This is HslWebServer, Thank you for use!";
			}
			response.AddHeader("Content-type", "application/json; charset=utf-8");
			DateTime dateTime = DateTime.Now;
			string result = await HandleObjectMethod(request, HttpUtility.UrlDecode(request.RawUrl), data, apiInformation);
			double timeSpend = Math.Round((DateTime.Now - dateTime).TotalSeconds, 5);
			apiInformation.CalledCountAddOne((long)(timeSpend * 100000.0));
			statisticsDict.StatisticsAdd(apiInformation.ApiTopic, 1L);
			LogNet?.WriteDebug(ToString(), $"[{request.RemoteEndPoint}] HttpRpc request:[{apiInformation.ApiTopic}] Spend:[{timeSpend * 1000.0:F2} ms] Count:[{apiInformation.CalledCount}]");
			return result;
		}

		private MqttRpcApiInfo GetMqttRpcApiInfo(string apiTopic)
		{
			MqttRpcApiInfo result = null;
			lock (rpcApiLock)
			{
				if (apiTopicServiceDict.ContainsKey(apiTopic))
				{
					result = apiTopicServiceDict[apiTopic];
				}
			}
			return result;
		}

		/// <summary>
		/// 获取当前所有注册的RPC接口信息，将返回一个数据列表。<br />
		/// Get all currently registered RPC interface information, and a data list will be returned.
		/// </summary>
		/// <returns>信息列表</returns>
		public MqttRpcApiInfo[] GetAllRpcApiInfo()
		{
			MqttRpcApiInfo[] result = null;
			lock (rpcApiLock)
			{
				result = apiTopicServiceDict.Values.ToArray();
			}
			return result;
		}

		/// <summary>
		/// 注册一个RPC的服务接口，可以指定当前的控制器名称，以及提供RPC服务的原始对象<br />
		/// Register an RPC service interface, you can specify the current controller name, 
		/// and the original object that provides the RPC service
		/// </summary>
		/// <param name="api">前置的接口信息，可以理解为MVC模式的控制器</param>
		/// <param name="obj">原始对象信息</param>
		public void RegisterHttpRpcApi(string api, object obj)
		{
			lock (rpcApiLock)
			{
				foreach (MqttRpcApiInfo item in MqttHelper.GetSyncServicesApiInformationFromObject(api, obj))
				{
					apiTopicServiceDict.Add(item.ApiTopic, item);
				}
			}
		}

		/// <inheritdoc cref="M:Communication.Enthernet.HttpServer.RegisterHttpRpcApi(System.String,System.Object)" />
		public void RegisterHttpRpcApi(object obj)
		{
			lock (rpcApiLock)
			{
				foreach (MqttRpcApiInfo item in MqttHelper.GetSyncServicesApiInformationFromObject(obj))
				{
					apiTopicServiceDict.Add(item.ApiTopic, item);
				}
			}
		}

		/// <summary>
		/// 设置登录的账户信息，如果需要自己控制，可以自己实现委托<see cref="P:Communication.Enthernet.HttpServer.HandleRequestFunc" /><br />
		/// Set the login account information, if you need to control by yourself, you can implement the delegation by yourself<see cref="P:Communication.Enthernet.HttpServer.HandleRequestFunc" />
		/// </summary>
		/// <param name="credentials">用户名的列表信息</param>
		public void SetLoginAccessControl(MqttCredential[] credentials)
		{
			if (credentials == null)
			{
				loginAccess = false;
				return;
			}
			if (credentials.Length == 0)
			{
				loginAccess = false;
				return;
			}
			loginAccess = true;
			loginCredentials = credentials;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"HttpServer[{port}]";
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="request">当前的请求信息</param>
		/// <param name="deceodeUrl">已经解码过的Url地址信息</param>
		/// <param name="json">json格式的参数信息</param>
		/// <param name="obj">等待解析的api解析的对象</param>
		/// <returns>等待返回客户的结果</returns>
		public static async Task<string> HandleObjectMethod(HttpListenerRequest request, string deceodeUrl, string json, object obj)
		{
			string method = GetMethodName(deceodeUrl);
			if (method.LastIndexOf('/') >= 0)
			{
				method = method.Substring(method.LastIndexOf('/') + 1);
			}
			MethodInfo methodInfo = obj.GetType().GetMethod(method);
			if (methodInfo == null)
			{
				return new OperateResult<string>("Current MqttSync Api ：[" + method + "] not exsist").ToJsonString();
			}
			OperateResult<MqttRpcApiInfo> apiResult = MqttHelper.GetMqttSyncServicesApiFromMethod("", methodInfo, obj);
			if (!apiResult.IsSuccess)
			{
				return OperateResult.CreateFailedResult<string>(apiResult).ToJsonString();
			}
			return await HandleObjectMethod(request, deceodeUrl, json, apiResult.Content);
		}

		/// <summary>
		/// 根据完整的地址获取当前的url地址信息
		/// </summary>
		/// <param name="url">地址信息</param>
		/// <returns>方法名称</returns>
		public static string GetMethodName(string url)
		{
			string empty = string.Empty;
			empty = ((url.IndexOf('?') <= 0) ? url : url.Substring(0, url.IndexOf('?')));
			if (empty.EndsWith("/") || empty.StartsWith("/"))
			{
				empty = empty.Trim('/');
			}
			return empty;
		}

		private static ISessionContext GetSessionContextFromHeaders(HttpListenerRequest request)
		{
			try
			{
				string[] values = request.Headers.GetValues("Authorization");
				if (values == null || values.Length < 1 || string.IsNullOrEmpty(values[0]))
				{
					return null;
				}
				string s = values[0].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
				string @string = Encoding.UTF8.GetString(Convert.FromBase64String(s));
				string[] array = @string.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length < 1)
				{
					return null;
				}
				return new SessionContext
				{
					UserName = array[0]
				};
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// 使用指定的对象来返回网络的API接口，前提是传入的数据为json参数，返回的数据为json数据，详细参照说明<br />
		/// Use the specified object to return the API interface of the network, 
		/// provided that the incoming data is json parameters and the returned data is json data, 
		/// please refer to the description for details
		/// </summary>
		/// <param name="request">当前的请求信息</param>
		/// <param name="deceodeUrl">已经解码过的Url地址信息</param>
		/// <param name="json">json格式的参数信息</param>
		/// <param name="apiInformation">等待解析的api解析的对象</param>
		/// <returns>等待返回客户的结果</returns>
		public static async Task<string> HandleObjectMethod(HttpListenerRequest request, string deceodeUrl, string json, MqttRpcApiInfo apiInformation)
		{
			ISessionContext context = GetSessionContextFromHeaders(request);
			if (apiInformation.PermissionAttribute != null)
			{
				try
				{
					string[] values = request.Headers.GetValues("Authorization");
					if (values == null || values.Length < 1 || string.IsNullOrEmpty(values[0]))
					{
						return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] has none Authorization information, access not permission").ToJsonString();
					}
					string base64String = values[0].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[1];
					string accountString = Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
					string[] account = accountString.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					if (account.Length < 1)
					{
						return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] has none Username information, access not permission").ToJsonString();
					}
					if (!apiInformation.PermissionAttribute.CheckUserName(account[0]))
					{
						return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] Check Username[" + account[0] + "] failed, access not permission").ToJsonString();
					}
				}
				catch (Exception ex2)
				{
					return new OperateResult<string>("Mqtt RPC Api ：[" + apiInformation.ApiTopic + "] Check Username failed, access not permission, reason:" + ex2.Message).ToJsonString();
				}
			}
			try
			{
				if (apiInformation.Method != null)
				{
					MethodInfo methodInfo = apiInformation.Method;
					string apiName2 = apiInformation.ApiTopic;
					if (request.HttpMethod != apiInformation.HttpMethod)
					{
						return new OperateResult("Current Api ：" + apiName2 + " not support diffrent httpMethod").ToJsonString();
					}
					object[] paras;
					if (request.HttpMethod == "POST")
					{
						paras = HslReflectionHelper.GetParametersFromJson(context, methodInfo.GetParameters(), json);
					}
					else
					{
						if (!(request.HttpMethod == "GET"))
						{
							return new OperateResult("Current Api ：" + apiName2 + " not support GET or POST").ToJsonString();
						}
						paras = ((deceodeUrl.IndexOf('?') <= 0) ? HslReflectionHelper.GetParametersFromJson(context, methodInfo.GetParameters(), json) : HslReflectionHelper.GetParametersFromUrl(context, methodInfo.GetParameters(), deceodeUrl));
					}
					object obj = methodInfo.Invoke(apiInformation.SourceObject, paras);
					Task task = obj as Task;
					if (task != null)
					{
						await task;
						return task.GetType().GetProperty("Result").GetValue(task, null)
							.ToJsonString();
					}
					return obj.ToJsonString();
				}
				if (apiInformation.Property != null)
				{
					string apiName = apiInformation.ApiTopic;
					if (request.HttpMethod != apiInformation.HttpMethod)
					{
						return new OperateResult("Current Api ：" + apiName + " not support diffrent httpMethod").ToJsonString();
					}
					if (request.HttpMethod != "GET")
					{
						return new OperateResult("Current Api ：" + apiName + " not support POST").ToJsonString();
					}
					return apiInformation.Property.GetValue(apiInformation.SourceObject, null).ToJsonString();
				}
				return new OperateResult("Current Api ：" + deceodeUrl + " not supported").ToJsonString();
			}
			catch (Exception ex)
			{
				return new OperateResult("Current Api ：" + deceodeUrl + " Wrong，Reason：" + ex.Message).ToJsonString();
			}
		}
	}
}

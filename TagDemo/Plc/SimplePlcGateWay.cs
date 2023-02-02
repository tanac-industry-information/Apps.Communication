using Apps.Communication.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Apps.Communication.Profinet.Melsec;
using Apps.Communication.Profinet.Keyence;
using Apps.Communication.Profinet.Siemens;
using Apps.Communication.Profinet.Omron;
using Apps.Communication.ModBus;
using Apps.Communication;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

namespace AppFramework.Common.Plc
{
	public class GateWayStatusEventArgs
	{
		/// <summary>
		/// 是否异常
		/// </summary>
		public bool Error { get; set; }

		/// <summary>
		/// 时间
		/// </summary>
		public DateTime Time { get; set; }

		/// <summary>
		/// 文本
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// 转化为字符串
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return (Error ? "[异常]" : "[正常]") + Time.ToString("  yyyy-MM-dd HH:mm:ss  ") + Text;
		}
	}
	public class PDUArea
	{
        public int ID;
        /// <summary>
        /// 起始地址
        /// </summary>
        public string StartAddress;
        /// <summary>
        /// 读取字节数据的长度
        /// </summary>
        public ushort Length;
        public byte[] Cache;
        public PDUArea(int id, string start, ushort len)
        {
            ID = id;
            StartAddress = start;
            Length = len;
            Cache = new byte[Length];
        }

    }

	/// <summary>
	/// 一个二次封装了的简易PLC标签变量网关，用于不支持变量标签读取的PLC，实现利用标签读写，订阅，历史数据读取(没空搞)，方法调用操作。
	/// </summary>
	public class SimplePlcGateWay : BindableBase
    {
        private const string DEFAULT_PATH = "./Settings/DataValues.json";

		private EventHandler<GateWayStatusEventArgs> m_PlcStatusChange;

		public event EventHandler<GateWayStatusEventArgs> PlcStatusChange
		{
			add { m_PlcStatusChange += value; }
			remove { m_PlcStatusChange -= value; }
		}

		/// <summary>
		/// 一个字单位的数据表示的地址长度，西门子为1，三菱，欧姆龙，modbusTcp就为2
		/// </summary>
		protected ushort wordLength = 2;

        protected List<PDUArea> _rangeList = new List<PDUArea>();

        private int pdu_length = 920;

        private CancellationTokenSource _cts;

        private IReadWriteNet _readwriter;

        private IByteTransform _transform;

        private PlcType CurrentPlc = PlcType.ModbusTcpNet;
        public int PDULength => pdu_length;
        public string IpAddress { get; set; }
        public int Port { get; set; }
		public bool IsRefreshTagValue { get; set; } = true;

		private bool isConnected;//是否已经连接过
		public bool IsConnected
        {
            get { return isConnected; }
            private set { SetProperty(ref isConnected, value);  }
        }

		private bool isError;//是否已经错误状态
		public bool IsError
		{
			get { return isError; }
		    private	set { SetProperty(ref isError, value); }
		}
		public List<SimpleTag> Tags { get; set; } = new List<SimpleTag>();

        public SimplePlcGateWay(PlcType plcType,string ipAddress, int port)
        {
            Initialization(plcType, ipAddress, port);
        }

        private void Initialization(PlcType plc, string ipAddress, int port)
        {
            IpAddress = ipAddress;
            Port = port;
            CurrentPlc = plc;
            switch (plc)
            {
                case PlcType.ModbusTcpNet:
                    var modbusTcp =new ModbusTcpNet(ipAddress,port);
					modbusTcp.SetPersistentConnection();
                    _readwriter = modbusTcp;
                    _transform =modbusTcp.ByteTransform;
                    wordLength = 2;
                    break;
                case PlcType.SiemensS7Net:
                    var s7=new SiemensS7Net(SiemensPLCS.S1200, ipAddress);
					s7.SetPersistentConnection();
					_readwriter = s7; 
                    _transform =s7.ByteTransform;
                    wordLength = 1;
                    break;
                case PlcType.MelsecMcNet:
                    var mc= new MelsecMcNet(ipAddress,port);
					mc.SetPersistentConnection();
					_readwriter = mc;
                    _transform = mc.ByteTransform;
                    wordLength = 2;
                    break;
				case PlcType.KeyenceMcNet:
					var kv = new KeyenceMcNet(ipAddress, port);
					kv.SetPersistentConnection();
					_readwriter = kv;
					_transform = kv.ByteTransform;
					wordLength = 2;
					break;
				case PlcType.OmronFinsNet:
                    var fins = new OmronFinsNet(ipAddress, port);
					fins.SetPersistentConnection();
					_readwriter = fins;
                    _transform = fins.ByteTransform;
                    wordLength = 2;
                    break;
            }
        }

		private void UpdateStatus(bool error, DateTime time, string status, params object[] args)
		{
			m_PlcStatusChange?.Invoke(this, new GateWayStatusEventArgs()
			{
				Error = error,
				Time = time.ToLocalTime(),
				Text = String.Format(status, args),
			});
		}

		public OperateResult Connect()
        {
			if (!isConnected)
			{
				_cts?.Cancel();
				_cts = new CancellationTokenSource();
				try
				{
					Tags.TrimExcess();
					Tags.Sort();
					var res = UpdatePDUArea();
					if (res.IsSuccess)
						Task.Factory.StartNew(() =>
						{
							IsConnected = true;
							UpdateStatus(false, DateTime.UtcNow, "StartRead");
							while (!_cts.IsCancellationRequested)
							{
								var res2 = ReadToCahce();
								if (res2.IsSuccess)
								{
									if (isError)
									{
										isError = false;
										UpdateStatus(false, DateTime.UtcNow, "ReadSuccess");
									}
									RefreshTags();
								}
								else
								{
									if (!isError)
									{
										UpdateStatus(true, DateTime.UtcNow, "ReadError:" + res.Message);
										IsError = true;
									}
								}
								Thread.Sleep(100);
							}
							if (_cts.IsCancellationRequested)
							{
								UpdateStatus(false, DateTime.UtcNow, "StopRead");
								IsConnected = false;
								IsError = false;
								return;
							}
						}, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
					else
						new OperateResult(res.Message);
				}
				catch (Exception ex)
				{
					return new OperateResult(ex.Message);
				}
				return OperateResult.CreateSuccessResult();
			}
			else
				return new OperateResult("连接失败,请先断开连接");
        }

        public void DisConnect()
        {
            _cts?.Cancel();          
        }

        protected OperateResult UpdatePDUArea()
        {
            try
            {
                _rangeList.Clear();
                int count = Tags.Count;
                if (count > 0)
                {
					DeviceAddress _start = Tags[0].Address;//起始地址;
                    if (count > 1)
                    {
                        ushort startIndex = 0;
						DeviceAddress segmentEnd = new DeviceAddress();//分段结束地址
						DeviceAddress tagAddress = new DeviceAddress();//当前地址 
						DeviceAddress segmentStart = _start;//分段开始地址
                        for (int j = 1, i = 1; i < count; i++, j++)
                        {
							tagAddress = Tags[i].Address;//当前变量地址 
                            ushort offset1 = GetTagAddressOffset(tagAddress, segmentStart);//计算与起始地址之间间隔
                            if (offset1 > pdu_length - tagAddress.DataSize)
                            {
                                //超出读取最大长度
                                segmentEnd = Tags[i - 1].Address;//最大
                                ushort len = GetTagAddressOffset(segmentEnd, segmentStart);//计算间隔
                                len += segmentEnd.DataSize <= wordLength ? wordLength : segmentEnd.DataSize;
								var res = ParseFromTag(segmentStart);
								if(!res.IsSuccess)
									throw new Exception(res.Message);
								_rangeList.Add(new PDUArea(startIndex, res.Content, len));
                                startIndex++;//缓存区块号+1
                                tagAddress.CacheID = startIndex;
                                tagAddress.CacheIndex = 0;
                                Tags[i].Address = tagAddress;
                                segmentStart = tagAddress;//更新数据片段的起始地址
                            }
                            else
                            {
								//没有超出读取最大长度
								tagAddress.CacheID = startIndex;
                                tagAddress.CacheIndex = offset1;
                                Tags[i].Address = tagAddress;

                            }
                            if (i == count - 1)
                            {
                                //最后一个
                                segmentEnd = Tags[i].Address;
                                int segmentLength = GetTagAddressOffset(segmentEnd, segmentStart);
                                if (segmentLength > pdu_length - tagAddress.DataSize)
                                {
                                    segmentEnd = Tags[i - 1].Address;
                                    segmentLength = segmentEnd.DataSize <= wordLength ? wordLength : segmentEnd.DataSize;
                                }
                                tagAddress.CacheID = startIndex;
                                tagAddress.CacheIndex = (ushort)segmentLength;
                                Tags[i].Address = tagAddress;
                                segmentLength += segmentEnd.DataSize <= wordLength ? wordLength : segmentEnd.DataSize;
								var res = ParseFromTag(segmentStart);
								if (!res.IsSuccess)
									throw new Exception(res.Message);
								_rangeList.Add(new PDUArea(startIndex, res.Content, (ushort)segmentLength));
                            }

                        }

                    }
                    else
                    {
                        //只有单个标签的情况
                        _start.CacheID = 0;//缓存区块ID为0
                        _start.CacheIndex = 0;//在缓存区内的偏移量地址为0
						var res = ParseFromTag(_start);
						if (!res.IsSuccess)
							throw new Exception(res.Message);
                        ushort lenght = _start.DataSize <= wordLength ? wordLength : _start.DataSize;//读取长度
                        _rangeList.Add(new PDUArea(0, res.Content, lenght));
                    }
                    return OperateResult.CreateSuccessResult();
                }
                else
                    return new OperateResult("PduArea计算失败:当前标签变量数量为0");
            }
            catch (Exception ex)
            {
                return new OperateResult("PduArea计算失败:" + ex.Message);
            }
        }

        private ushort GetTagAddressOffset(DeviceAddress start, DeviceAddress end)
		{
			
			return start.DataCode == end.DataCode && start.DbBlock == end.DbBlock ? (ushort)(start.AddressStart - end.AddressStart) : ushort.MaxValue;
		}

		protected OperateResult ReadToCahce()
        {
            try
            {
                for (int i = 0; i < _rangeList.Count; i++)
                {
                    var area = _rangeList[i];
                    OperateResult<byte[]> operateResult = _readwriter.Read(area.StartAddress, (ushort)(area.Length / 2));//从PLC读取数据
                    if (operateResult.IsSuccess && operateResult.Content != null)
                    {
                        //读取成功
                        Buffer.BlockCopy(operateResult.Content, 0, area.Cache, 0, operateResult.Content.Length);
                        //刷新
                    }
                    else
                    {
                        //读取失败                     
                        return operateResult;
                    }
                }
                return OperateResult.CreateSuccessResult();
            }
            catch (Exception ex)
            {
                return new OperateResult(ex.Message);
            }
        }

        protected void RefreshTags()
        {
			if (IsRefreshTagValue)
			{
				foreach (var tag in Tags)
				{
					var res = getValue(tag);
					if (res.IsSuccess)
					{
						tag.Refresh(res.Content);
					}
				}
			}
		}		

		private OperateResult<object> getValue(SimpleTag tag, DataSource source = DataSource.Cache)
		{
			try
			{
				var address = tag.Address;
				if (tag.Type==DataType.BOOL)
				{
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						int _index = address.CacheIndex * 8 + address.Bit;
						object res= _transform.TransBool(_rbs, _index);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
                        var read = _readwriter.ReadBool(tag.PlcAddress);
                        if (read.IsSuccess)
                        {
                            return OperateResult.CreateSuccessResult((object)read.Content);
                        }
                        else
                        {
                            return new OperateResult<object>(read.Message);
                        }
                    }

                }
				else if (tag.Type == DataType.WORD)
				{
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res=_transform.TransUInt16(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadUInt16(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if(tag.Type == DataType.SHORT)
                {
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res=_transform.TransInt16(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadInt16(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if (tag.Type == DataType.DWORD)
				{
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res = _transform.TransUInt32(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadUInt32(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if (tag.Type == DataType.INT)
                {
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res=_transform.TransInt32(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadInt32(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if(tag.Type == DataType.FLOAT)
                {
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res=_transform.TransSingle(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadFloat(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if (tag.Type == DataType.DOUBLE)
				{
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						object res = _transform.TransDouble(_rbs, address.CacheIndex);
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadDouble(tag.PlcAddress);
						if (read.IsSuccess)
						{
							return OperateResult.CreateSuccessResult((object)read.Content);
						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else if(tag.Type == DataType.STRING)
                {
					if (source == DataSource.Cache)
					{
						byte[] _rbs = _rangeList[address.CacheID].Cache;
						string[] strArray = _transform.TransString(_rbs, address.CacheIndex, address.DataSize, Encoding.ASCII).Split('\0');
						string[] strArray2 = strArray[0].Split('\r');
						object res = strArray2[0].TrimEnd(' ').TrimEnd('\r');
						return OperateResult.CreateSuccessResult(res);
					}
					else
					{
						var read = _readwriter.ReadString(tag.PlcAddress,address.DataSize);
						if (read.IsSuccess)
						{
							string[] strArray2 = read.Content.Split('\r');
							object res =strArray2[0].TrimEnd(' ').TrimEnd('\r');
							return OperateResult.CreateSuccessResult(res);

						}
						else
						{
							return new OperateResult<object>(read.Message);
						}
					}
				}
				else
                {
					return new OperateResult<object>("变量标签的数据类型未知");
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<object>(ex.Message);
			}
		}

		private OperateResult setValue(SimpleTag tag, object value)
		{
			try
			{
                var address = tag.Address;
                if (tag.Type == DataType.BOOL)
                {
                    var res = _readwriter.Write(tag.PlcAddress,(bool)value);
					return res;
                }
                else if (tag.Type == DataType.WORD)
				{
					var res = _readwriter.Write(tag.PlcAddress, (ushort)value);
					return res;
				}
				else if (tag.Type == DataType.SHORT)
				{
					var res = _readwriter.Write(tag.PlcAddress, (short)value);
					return res;
				}
				else if (tag.Type == DataType.DWORD)
				{
					var res = _readwriter.Write(tag.PlcAddress, (UInt32)value);
					return res;
				}
				else if (tag.Type == DataType.INT)
				{
					var res = _readwriter.Write(tag.PlcAddress, (Int32)value);
					return res;
				}
				else if (tag.Type == DataType.FLOAT)
				{
					var res = _readwriter.Write(tag.PlcAddress, (Single)value);
					return res;
				}
				else if (tag.Type == DataType.DOUBLE)
				{
					var res = _readwriter.Write(tag.PlcAddress, (Double)value);
					return res;
				}
				else if (tag.Type == DataType.STRING)
				{
					string writeValue = (string)value;
					if (writeValue.Length <= tag.StrLength)
					{
						byte [] array = new byte [tag.Address.DataSize];
						byte[] values = _transform.TransByte(writeValue,Encoding.ASCII);
						values.CopyTo(array,0);
						var res = _readwriter.Write(tag.PlcAddress, array);
						return res;
					}
					else
						return new OperateResult("写入的字符串长度超过设定长度");
				}
				else
				{
					return new OperateResult("变量标签的数据类型未知");
				}
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
		}

		public OperateResult<T> GetValue<T>(string tagName, DataSource source = DataSource.Cache)
        {
            try
            {
				if (Tags.Any(x => x.Name == tagName))
				{
					var tag= Tags.First(x => x.Name == tagName);
				    var res=getValue(tag, source);
					if(res.IsSuccess)
                    {
						var value = (T)res.Content;
						return OperateResult.CreateSuccessResult(value);
                    }
					else
                    {
						return new OperateResult<T>(res.Message);
					}
				}
				else
					return new OperateResult<T>($"不存在标签变量<{tagName}>");
			}
            catch (Exception ex)
            {
                return new OperateResult<T>(ex.Message);
            }
        }

		public OperateResult SetValue(string tagName,object value)
        {
			try
			{
				if (Tags.Any(x => x.Name == tagName))
				{
					var tag = Tags.First(x => x.Name == tagName);
					var res = setValue(tag,value);
					return res;
				}
				else
					return new OperateResult($"不存在标签变量<{tagName}>");
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
		}
		
		public OperateResult AddTag(string tagName,DataType dataType,string plcAddress,ushort len)
		{
			try
            {
				if (isConnected)
					return new OperateResult("连接状态下无法添加标签变量");
				if (Tags.Any(x => x.Name == tagName))
					return new OperateResult("无法添加具有相同名称的标签变量");
				ushort dataSize = 0;
				switch (dataType)
				{
					case DataType.BOOL:
					case DataType.BYTE:
						dataSize = 1;				
						break;
					case DataType.WORD:
					case DataType.SHORT:
						dataSize = 2;
						break;
					case DataType.DWORD:
					case DataType.INT:
					case DataType.FLOAT:
						dataSize = 4;
						break;
					case DataType.DOUBLE:
						dataSize = 8;
						break;
					case DataType.STRING:
						if(len % wordLength>0)
							dataSize = (ushort)(len + 1);
						else
							dataSize = len;
						break;
				}
				var tag = new SimpleTag();
				tag.Name= tagName;
				tag.PlcAddress = plcAddress;
				tag.Type= dataType;
				tag.StrLength= len;
				OperateResult<DeviceAddress> res;
				switch (CurrentPlc)
                {
					case PlcType.OmronFinsNet:
						 res = ParseFromFins(plcAddress, dataSize);
						break;
					case PlcType.SiemensS7Net:
						res = ParseFromS7(plcAddress, dataSize);
						break;
					case PlcType.MelsecMcNet:
						res = ParseFromMc(plcAddress, dataSize);
						break;
					case PlcType.KeyenceMcNet:
						res = ParseFromKvMc(plcAddress, dataSize);
						break;
					default:
						res = new OperateResult<DeviceAddress>($"{CurrentPlc}暂不支持");
						break;
				}
				if (res.IsSuccess)
				{
					tag.Address = res.Content;
					Tags.Add(tag);
					return OperateResult.CreateSuccessResult();
				}
				else
				{
					return new OperateResult("地址解析失败:"+res.Message);
				}
			}
			catch(Exception ex)
            {
				return new OperateResult(ex.Message);
			}
            
		}

		public OperateResult RemoveTag(SimpleTag tag)
		{
			try
			{
				if (isConnected)
					return new OperateResult("连接状态下无法删除标签变量");
				Tags.Remove(tag);
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
		}

		public void Save(string FilePath = DEFAULT_PATH)
		{
			string path = Path.GetDirectoryName(FilePath);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			var v = Tags.Select(x => new
			{
				x.Name,
				x.Type,
				x.PlcAddress,
				x.StrLength
			});
			File.WriteAllText(FilePath, JsonConvert.SerializeObject(v, Formatting.Indented));
		}

		public OperateResult Load(string FilePath = DEFAULT_PATH)
		{
			try
			{
				var str = File.ReadAllText(FilePath);
				var obj = JsonConvert.DeserializeObject<IEnumerable<SimpleTag>>(str);
				if (isConnected)
					return new OperateResult("连接状态下无法载入配置文件");
				Tags.Clear();
				foreach (var a in obj)
				{
				  var res=AddTag(a.Name, a.Type, a.PlcAddress, a.StrLength);
				  if(!res.IsSuccess)
						return new OperateResult("载入配置文件失败:"+res.Message);
				}
				return OperateResult.CreateSuccessResult();
			}
			catch (Exception ex)
			{
				return new OperateResult(ex.Message);
			}
        }

		public string this[string Name]
        {
            get
            {
                if (Tags.Any(x => x.Name == Name))
                {
					object v =Tags.First(x => x.Name == Name).Value;
                    if(v!=null)
						return v.ToString();
                }
				return null;
			}
        }



		public static OperateResult<string> ParseFromTag(DeviceAddress address)
        {
			string plcAddress = "";
			try
            {
				switch (address.DataCode)
                {

					case DataCode.Keyence_DM:
						plcAddress = $"D{address.AddressStart / 2}";
						break;
					case DataCode.Keyence_EM:
						plcAddress = $"D{address.AddressStart/ 2+ 100000}";
						break;
					case DataCode.Keyence_MR:
						plcAddress = $"M{address.AddressStart * 8}";
						break;
					case DataCode.MelsecMc_M:
						plcAddress = $"M{address.AddressStart*8}";
						break;
					case DataCode.MelsecMc_D:
						plcAddress = $"D{address.AddressStart / 2}";
						break;
					case DataCode.MelsecMc_W:
						plcAddress = $"W{address.AddressStart / 2}";
						break;
					case DataCode.OmronFins_DM:
						plcAddress = $"D{address.AddressStart / 2}";
						break;
					case DataCode.OmronFins_CIO:
						plcAddress = $"C{address.AddressStart / 2}";
						break;
					case DataCode.OmronFins_WR:
						plcAddress = $"W{address.AddressStart / 2}";
						break;
					case DataCode.SiemensS7_DB:
						plcAddress = $"DB{address.DbBlock}.{address.AddressStart}";
						break;
				    
				}
			}
			catch(Exception ex)
            {
				return new OperateResult<string>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(plcAddress);
		}

		public static OperateResult<DeviceAddress> ParseFromFins(string address, ushort length)
		{
			DeviceAddress omronFinsAddress = new DeviceAddress();
			try
			{
				omronFinsAddress.DataSize = length;
				switch (address[0])
				{
					case 'D':
					case 'd':
						omronFinsAddress.DataCode = DataCode.OmronFins_DM;
						break;
					case 'C':
					case 'c':
						omronFinsAddress.DataCode = DataCode.OmronFins_CIO;
						break;
					case 'W':
					case 'w':
						omronFinsAddress.DataCode = DataCode.OmronFins_WR;
						break;
					default:
						throw new Exception(StringResources.Language.NotSupportedDataType);
				}
				string[] array3 = address.Substring(1).SplitDot();
				int num3 = ushort.Parse(array3[0]) * 2;
				if (array3.Length > 1)
				{
					int bitStartIndex = HslHelper.CalculateBitStartIndex(array3[1]);
					omronFinsAddress.AddressStart = num3 + 1 - bitStartIndex / 8;
					omronFinsAddress.Bit = (byte)(bitStartIndex % 8);
				}
				else
				{
					omronFinsAddress.AddressStart = num3;
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<DeviceAddress>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(omronFinsAddress);
		}

		public static OperateResult<DeviceAddress> ParseFromS7(string address, ushort length)
		{
			DeviceAddress s7AddressData = new DeviceAddress();
			try
			{
				s7AddressData.DataSize = length;
				s7AddressData.DbBlock = 0;
				if (address[0] == 'D' || address.Substring(0, 2) == "DB")
				{
					s7AddressData.DataCode = DataCode.SiemensS7_DB;
					string[] array = address.Split('.');
					if (address[1] == 'B')
					{
						s7AddressData.DbBlock = Convert.ToUInt16(array[0].Substring(2));
					}
					else
					{
						s7AddressData.DbBlock = Convert.ToUInt16(array[0].Substring(1));
					}
					string text = address.Substring(address.IndexOf('.') + 1);
					if (text.StartsWith("DBX") || text.StartsWith("DBB") || text.StartsWith("DBW") || text.StartsWith("DBD"))
					{
						text = text.Substring(3);
					}
					int address2 = S7CalculateAddressStarted(text);
					s7AddressData.AddressStart = address2 / 8;
					s7AddressData.Bit = (byte)(address2 % 8);
				}
				else
				{
					throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<DeviceAddress>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(s7AddressData);
		}

		public static OperateResult<DeviceAddress> ParseFromMc(string address, ushort length)
		{
			DeviceAddress mcAddressData = new DeviceAddress();
			mcAddressData.DataSize = length;
			try
			{
				switch (address[0])
				{
					case 'M':
					case 'm':
						{
							mcAddressData.DataCode = DataCode.MelsecMc_M;
							string[] array3 = address.Substring(1).SplitDot();
							int s = ushort.Parse(array3[0]);
							mcAddressData.Bit = (byte)(s % 8);
							mcAddressData.AddressStart = s / 8;
						}
						break;
					case 'D':
					case 'd':
						{
							mcAddressData.DataCode = DataCode.MelsecMc_D;
							string[] array3 = address.Substring(1).SplitDot();
							int num3 = ushort.Parse(array3[0]) * 2;
							if (array3.Length > 1)
							{
								int bitStartIndex = HslHelper.CalculateBitStartIndex(array3[1]);
								mcAddressData.AddressStart = num3 + 1 - bitStartIndex / 8;
								mcAddressData.Bit = (byte)(bitStartIndex % 8);
							}
							else
							{
								mcAddressData.AddressStart = num3;
							}
						}
						break;
					case 'W':
					case 'w':
						{
							mcAddressData.DataCode = DataCode.MelsecMc_W;
							string[] array3 = address.Substring(1).SplitDot();
							int num3 = ushort.Parse(array3[0]) * 2;
							if (array3.Length > 1)
							{
								int bitStartIndex = HslHelper.CalculateBitStartIndex(array3[1]);
								mcAddressData.AddressStart = num3 + 1 - bitStartIndex / 8;
								mcAddressData.Bit = (byte)(bitStartIndex % 8);
							}
							else
							{
								mcAddressData.AddressStart = num3;
							}
						}
						break;
					default:
						throw new Exception(StringResources.Language.NotSupportedDataType);
				}
			}
			catch (Exception ex)
			{
				return new OperateResult<DeviceAddress>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(mcAddressData);
		}

		public static OperateResult<DeviceAddress> ParseFromKvMc(string address, ushort length)
		{
			DeviceAddress mcAddressData = new DeviceAddress();
			mcAddressData.DataSize = length;
			try
			{
				if (address.ToUpper().StartsWith("MR"))
				{
					var bit = int.TryParse(address.Substring(address.Length - 2), out var v0);
					var r = int.TryParse(address.Substring(2, address.Length - 4), out var v1);
					if (bit && r)
					{
						int s = v1 * 16 + v0;
						mcAddressData.DataCode = DataCode.Keyence_MR;
						mcAddressData.Bit = (byte)(s % 8);
						mcAddressData.AddressStart = s / 8;
					}
					else
					{
						throw new Exception(StringResources.Language.NotSupportedDataType);
					}
				}
				else if (address.ToUpper().StartsWith("EM"))
				{
					mcAddressData.DataCode = DataCode.Keyence_EM;
					string[] array3 = address.Substring(2).SplitDot();
					int num3 = ushort.Parse(array3[0]) * 2;
					if (array3.Length > 1)
					{
						int bitStartIndex = HslHelper.CalculateBitStartIndex(array3[1]);
						mcAddressData.AddressStart = num3 + 1 - bitStartIndex / 8;
						mcAddressData.Bit = (byte)(bitStartIndex % 8);
					}
					else
					{
						mcAddressData.AddressStart = num3;
					}
				}
				else if (address.ToUpper().StartsWith("DM"))
				{
					mcAddressData.DataCode = DataCode.Keyence_DM;
					string[] array3 = address.Substring(2).SplitDot();
					int num3 = ushort.Parse(array3[0]) * 2;
					if (array3.Length > 1)
					{
						int bitStartIndex = HslHelper.CalculateBitStartIndex(array3[1]);
						mcAddressData.AddressStart = num3 + 1 - bitStartIndex / 8;
						mcAddressData.Bit = (byte)(bitStartIndex % 8);
					}
					else
					{
						mcAddressData.AddressStart = num3;
					}

				}
				else
					throw new Exception(StringResources.Language.NotSupportedDataType);
			}
			catch (Exception ex)
			{
				return new OperateResult<DeviceAddress>(ex.Message);
			}
			return OperateResult.CreateSuccessResult(mcAddressData);
		}

		public static int S7CalculateAddressStarted(string address, bool isCT = false)
		{
			if (address.IndexOf('.') < 0)
			{
				if (isCT)
				{
					return Convert.ToInt32(address);
				}
				return Convert.ToInt32(address) * 8;
			}
			string[] array = address.Split('.');
			return Convert.ToInt32(array[0]) * 8 + Convert.ToInt32(array[1]);
		}

    }
}

using System.Text;

namespace Apps.Communication.Core.Address
{
	/// <summary>
	/// Modbus协议地址格式，可以携带站号，功能码，地址信息<br />
	/// Modbus protocol address format, can carry station number, function code, address information
	/// </summary>
	public class ModbusAddress : DeviceAddressBase
	{
		/// <summary>
		/// 获取或设置当前地址的站号信息<br />
		/// Get or set the station number information of the current address
		/// </summary>
		public int Station { get; set; }

		/// <summary>
		/// 获取或设置当前地址携带的功能码<br />
		/// Get or set the function code carried by the current address
		/// </summary>
		public int Function { get; set; }

		/// <summary>
		/// 实例化一个默认的对象<br />
		/// Instantiate a default object
		/// </summary>
		public ModbusAddress()
		{
			Station = -1;
			Function = -1;
			base.Address = 0;
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址初始化<br />
		/// Instantiate an object, initialize with the specified address
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		public ModbusAddress(string address)
		{
			Station = -1;
			Function = -1;
			base.Address = 0;
			Parse(address);
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址及功能码初始化<br />
		/// Instantiate an object and initialize it with the specified address and function code
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		/// <param name="function">默认的功能码信息</param>
		public ModbusAddress(string address, byte function)
		{
			Station = -1;
			Function = function;
			base.Address = 0;
			Parse(address);
		}

		/// <summary>
		/// 实例化一个对象，使用指定的地址，站号，功能码来初始化<br />
		/// Instantiate an object, use the specified address, station number, function code to initialize
		/// </summary>
		/// <param name="address">传入的地址信息，支持富地址，例如s=2;x=3;100</param>
		/// <param name="station">站号信息</param>
		/// <param name="function">默认的功能码信息</param>
		public ModbusAddress(string address, byte station, byte function)
		{
			Station = -1;
			Function = function;
			Station = station;
			base.Address = 0;
			Parse(address);
		}

		/// <inheritdoc />
		public override void Parse(string address)
		{
			if (address.IndexOf(';') < 0)
			{
				base.Address = ushort.Parse(address);
				return;
			}
			string[] array = address.Split(';');
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i][0] == 's' || array[i][0] == 'S')
				{
					Station = byte.Parse(array[i].Substring(2));
				}
				else if (array[i][0] == 'x' || array[i][0] == 'X')
				{
					Function = byte.Parse(array[i].Substring(2));
				}
				else
				{
					base.Address = ushort.Parse(array[i]);
				}
			}
		}

		/// <summary>
		/// 地址偏移指定的位置，返回一个新的地址对象<br />
		/// The address is offset by the specified position and a new address object is returned
		/// </summary>
		/// <param name="value">数据值信息</param>
		/// <returns>新增后的地址信息</returns>
		public ModbusAddress AddressAdd(int value)
		{
			return new ModbusAddress
			{
				Station = Station,
				Function = Function,
				Address = (ushort)(base.Address + value)
			};
		}

		/// <summary>
		/// 地址偏移1，返回一个新的地址对象<br />
		/// The address is offset by 1 and a new address object is returned
		/// </summary>
		/// <returns>新增后的地址信息</returns>
		public ModbusAddress AddressAdd()
		{
			return AddressAdd(1);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Station >= 0)
			{
				stringBuilder.Append("s=" + Station + ";");
			}
			if (Function == 2 || Function == 4)
			{
				stringBuilder.Append("x=" + Function + ";");
			}
			stringBuilder.Append(base.Address.ToString());
			return stringBuilder.ToString();
		}
	}
}

using System.Runtime.InteropServices;

namespace Apps.Communication
{
	/// <summary>
	/// 用于网络传递的信息头，使用上等同于int
	/// </summary>
	/// <remarks>
	/// 通常用于<see cref="T:Communication.Enthernet.NetComplexServer" />和<see cref="T:Communication.Enthernet.NetComplexClient" />之间的通信，以及<see cref="T:Communication.Enthernet.NetSimplifyServer" />和<see cref="T:Communication.Enthernet.NetSimplifyClient" />通讯
	/// </remarks>
	/// <example>
	/// 使用上等同于int，只是本结构体允许将4字节的int拆分成3部分单独访问
	/// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Core\NetHandle.cs" region="NetHandleExample" title="NetHandle示例" />
	/// </example>
	[StructLayout(LayoutKind.Explicit)]
	public struct NetHandle
	{
		/// <summary>
		/// 完整的暗号值
		/// </summary>
		[FieldOffset(0)]
		private int m_CodeValue;

		/// <summary>
		/// 主暗号分类0-255
		/// </summary>
		[FieldOffset(3)]
		private byte m_CodeMajor;

		/// <summary>
		/// 次要的暗号分类0-255
		/// </summary>
		[FieldOffset(2)]
		private byte m_CodeMinor;

		/// <summary>
		/// 暗号的编号分类0-65535
		/// </summary>
		[FieldOffset(0)]
		private ushort m_CodeIdentifier;

		/// <summary>
		/// 完整的暗号值
		/// </summary>
		public int CodeValue
		{
			get
			{
				return m_CodeValue;
			}
			set
			{
				m_CodeValue = value;
			}
		}

		/// <summary>
		/// 主暗号分类0-255
		/// </summary>
		public byte CodeMajor
		{
			get
			{
				return m_CodeMajor;
			}
			private set
			{
				m_CodeMajor = value;
			}
		}

		/// <summary>
		/// 次要的暗号分类0-255
		/// </summary>
		public byte CodeMinor
		{
			get
			{
				return m_CodeMinor;
			}
			private set
			{
				m_CodeMinor = value;
			}
		}

		/// <summary>
		/// 暗号的编号分类0-65535
		/// </summary>
		public ushort CodeIdentifier
		{
			get
			{
				return m_CodeIdentifier;
			}
			private set
			{
				m_CodeIdentifier = value;
			}
		}

		/// <summary>
		/// 赋值操作，可以直接赋值int数据
		/// </summary>
		/// <param name="value">int数值</param>
		/// <returns>等值的消息对象</returns>
		public static implicit operator NetHandle(int value)
		{
			return new NetHandle(value);
		}

		/// <summary>
		/// 也可以赋值给int数据
		/// </summary>
		/// <param name="netHandle">netHandle对象</param>
		/// <returns>等值的消息对象</returns>
		public static implicit operator int(NetHandle netHandle)
		{
			return netHandle.m_CodeValue;
		}

		/// <summary>
		/// 判断是否相等
		/// </summary>
		/// <param name="netHandle1">第一个数</param>
		/// <param name="netHandle2">第二个数</param>
		/// <returns>等于返回<c>True</c>，否则<c>False</c></returns>
		public static bool operator ==(NetHandle netHandle1, NetHandle netHandle2)
		{
			return netHandle1.CodeValue == netHandle2.CodeValue;
		}

		/// <summary>
		/// 判断是否不相等
		/// </summary>
		/// <param name="netHandle1">第一个对象</param>
		/// <param name="netHandle2">第二个对象</param>
		/// <returns>等于返回<c>False</c>，否则<c>True</c></returns>
		public static bool operator !=(NetHandle netHandle1, NetHandle netHandle2)
		{
			return netHandle1.CodeValue != netHandle2.CodeValue;
		}

		/// <summary>
		/// 两个数值相加
		/// </summary>
		/// <param name="netHandle1">第一个对象</param>
		/// <param name="netHandle2">第二个对象</param>
		/// <returns>返回两个指令的和</returns>
		public static NetHandle operator +(NetHandle netHandle1, NetHandle netHandle2)
		{
			return new NetHandle(netHandle1.CodeValue + netHandle2.CodeValue);
		}

		/// <summary>
		/// 两个数值相减
		/// </summary>
		/// <param name="netHandle1">第一个对象</param>
		/// <param name="netHandle2">第二个对象</param>
		/// <returns>返回两个指令的差</returns>
		public static NetHandle operator -(NetHandle netHandle1, NetHandle netHandle2)
		{
			return new NetHandle(netHandle1.CodeValue - netHandle2.CodeValue);
		}

		/// <summary>
		/// 判断是否小于另一个数值
		/// </summary>
		/// <param name="netHandle1">第一个对象</param>
		/// <param name="netHandle2">第二个对象</param>
		/// <returns>小于则返回<c>True</c>，否则返回<c>False</c></returns>
		public static bool operator <(NetHandle netHandle1, NetHandle netHandle2)
		{
			return netHandle1.CodeValue < netHandle2.CodeValue;
		}

		/// <summary>
		/// 判断是否大于另一个数值
		/// </summary>
		/// <param name="netHandle1">第一个对象</param>
		/// <param name="netHandle2">第二个对象</param>
		/// <returns>大于则返回<c>True</c>，否则返回<c>False</c></returns>
		public static bool operator >(NetHandle netHandle1, NetHandle netHandle2)
		{
			return netHandle1.CodeValue > netHandle2.CodeValue;
		}

		/// <summary>
		/// 初始化一个暗号对象
		/// </summary>
		/// <param name="value">使用一个默认的数值进行初始化</param>
		public NetHandle(int value)
		{
			m_CodeMajor = 0;
			m_CodeMinor = 0;
			m_CodeIdentifier = 0;
			m_CodeValue = value;
		}

		/// <summary>
		/// 根据三个值来初始化暗号对象
		/// </summary>
		/// <param name="major">主暗号</param>
		/// <param name="minor">次暗号</param>
		/// <param name="identifier">暗号编号</param>
		public NetHandle(byte major, byte minor, ushort identifier)
		{
			m_CodeValue = 0;
			m_CodeMajor = major;
			m_CodeMinor = minor;
			m_CodeIdentifier = identifier;
		}

		/// <summary>
		/// 获取完整的暗号数据
		/// </summary>
		/// <returns>返回暗号的字符串表示形式</returns>
		public override string ToString()
		{
			return m_CodeValue.ToString();
		}

		/// <summary>
		/// 判断两个实例是否相同
		/// </summary>
		/// <param name="obj">对比的对象</param>
		/// <returns>相同返回<c>True</c>，否则返回<c>False</c></returns>
		public override bool Equals(object obj)
		{
			NetHandle netHandle = default(NetHandle);
			int num;
			if (obj is NetHandle)
			{
				netHandle = (NetHandle)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				return CodeValue.Equals(netHandle.CodeValue);
			}
			return false;
		}

		/// <summary>
		/// 获取哈希值
		/// </summary>
		/// <returns>返回当前对象的哈希值</returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}

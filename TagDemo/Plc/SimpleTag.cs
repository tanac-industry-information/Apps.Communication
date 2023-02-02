using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AppFramework.Common.Plc
{
    public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs e);
    public class ValueChangedEventArgs : EventArgs
    {
        public ValueChangedEventArgs(object value, object oldvalue)
        {
            this.Value = value;
            this.OldValue = oldvalue;
        }

        public object Value;
        public object OldValue;
    }
    public class SimpleTag:BindableBase,IComparable<SimpleTag>
    {
        private EventHandler<ValueChangedEventArgs> m_ValueChanged;

        public event EventHandler<ValueChangedEventArgs> ValueChanged
        {
            add { m_ValueChanged += value;  }
            remove { m_ValueChanged -= value; }
        }

        private DateTime timeStamp;

        private object _value;
        public string Name { get; set; }
		public string PlcAddress { get; set; }
        public ushort StrLength { get; set; }
        public DeviceAddress Address { get; set; }
        public DataType Type { get; set; }
        public DateTime TimeStamp
        {
            get{ return timeStamp; }
            private set { SetProperty(ref timeStamp, value); }
        }
        public object Value 
        {
            get{ return _value; } 
            private set {SetProperty(ref _value, value); }
        }
        public void Refresh(object value)
        {
            try
            {               
                if (!value.Equals(_value))
                {
                    OnValueChanged(new ValueChangedEventArgs(_value, value));
                    Value =value;
                    TimeStamp = DateTime.Now;
                }
            }
            catch{}
        }
        protected void OnValueChanged(ValueChangedEventArgs args)
        {      
             m_ValueChanged?.Invoke(this, args);
        }
        public int CompareTo(SimpleTag other)
        {
            return Address.CompareTo(other.Address);
        }
    }
    public class DeviceAddress : IComparable<DeviceAddress>
    {
        public ushort CacheID { get; set; }
        public ushort CacheIndex { get;set; }
        public int AddressStart { get; set; }
        public byte Bit { get; set; }
        public DataCode DataCode { get; set; }
        public ushort DbBlock { get; set; }
        public ushort DataSize { get; set; }
        public int CompareTo(DeviceAddress other)
        {
            return DataCode > other.DataCode ? 1 :
                   DataCode < other.DataCode ? -1 :
                   DbBlock > other.DbBlock ? 1 :
                   DbBlock < other.DbBlock ? -1 :
                   AddressStart + DataSize > other.AddressStart + other.DataSize ? 1 :
                   AddressStart + DataSize < other.AddressStart + other.DataSize ? -1 :
                   Bit > other.Bit ? 1 :
                   Bit < other.Bit ? -1 : 0;
        }
    }
}

using AppFramework.Common.Plc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Apps.Communication.Profinet.Melsec;

namespace TagDemo
{
    public partial class Form1 : Form
    {
        private MelsecMcServer m_SimulationPlc;

        private SimplePlcGateWay m_SimplePlcGateWay;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m_SimulationPlc=new MelsecMcServer();
            m_SimulationPlc.Port = 6000;
            m_SimulationPlc.ServerStart();


            m_SimplePlcGateWay = new SimplePlcGateWay(PlcType.MelsecMcNet, "127.0.0.1", 6000);
            m_SimplePlcGateWay.PlcStatusChange += PlcStatusChange;
            m_SimplePlcGateWay.Load();//读取变量地址配置文件,目前josn，简易改成csv;
            m_SimplePlcGateWay.AddTag("变量1",DataType.STRING,"D500",20); //加上配置文件，一共2006个标签
            var tag=m_SimplePlcGateWay.Tags.LastOrDefault();
            tag.ValueChanged += Tag_ValueChanged;
            m_SimplePlcGateWay.Connect();//连接
        }

        private void Tag_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            var tag=sender as SimpleTag;
            MessageBox.Show($"<{tag.Name}>的值发生变换:<{e.OldValue}>变成了<{e.Value}>");
        }

        private void PlcStatusChange(object sender, GateWayStatusEventArgs e)
        {
            MessageBox.Show("网关当前状态:"+e.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
           var res=m_SimplePlcGateWay.GetValue<string>("变量1");
           string str = m_SimplePlcGateWay["变量1"];
            if (res.IsSuccess)
                MessageBox.Show("<变量1>:"+res.Content);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string value = DateTime.Now.ToString("HH:mm:ss");
            var res = m_SimplePlcGateWay.SetValue("变量1", value);
        }
    }
}

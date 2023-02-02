using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppFramework.Common.Plc
{
    public enum DataType
    {
        NONE = 0,
        BOOL = 1,
        BYTE = 2,
        SHORT = 3,
        WORD = 4,
        INT = 5,
        DWORD=6,
        FLOAT = 7,
        DOUBLE = 8,
        STRING = 9
    }
    public enum DataSource
    {
        Cache = 1,
        Device = 2
    }
	public enum PlcType
	{
        ModbusTcpNet,
        SiemensS7Net,
        MelsecMcNet,
        KeyenceMcNet,
        OmronFinsNet
    }
    public enum DataCode
    {
        Keyence_DM,
        Keyence_EM,
        Keyence_MR,
        MelsecMc_M,
        MelsecMc_D,
        MelsecMc_W,
        OmronFins_DM,
        OmronFins_CIO,
        OmronFins_WR,
        SiemensS7_DB
    }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace GeneratePIL
{
    public enum MemoryType
    {
        Unknown,
        UserRAM,
        UserROM,
        SystemROM,
        SystemRAM,
        FixRAM,
        MEMCARD,
        DRAM,
    };

    public enum ModuleType
    {
        /* the order of module types defines the order in which the modules will be downloaded */

        #region sysconf 
        PLCConfig,
        IOConfig,
        #endregion

        HWD_FW,

        #region OS tasks
        SystemTask,
        Error,
        StartUp,
        SystemLibrary,
        CommConfig,
        OSEXE,
        Boot,
        HWLibrary,
        CommLibrary,
        InstTrapLibrary,
        MathLibrary,
        FUBAVT,
        #endregion

        Unknown,

        NCUpdate,
        OSUpdate,
        ACP10,

        #region libraries
        DeviceDriver,
        Library,
        #endregion

        #region data modules
        Data,
        #endregion

        Logger,
        IOMap,

        #region tasks
        ExceptionTask,
        SSTask,
        SPSTask,
        #endregion

        Table,
        ProfilerDef,
        ProfilerData,
        TracerDef,
        TracerData
    };

    public class ModuleInfo
    {
        string name;
        string version;
        MemoryType memory;
        ModuleType type;

        public ModuleInfo(string name, string version, string memory, string type)
        {
            this.name = name;
            this.version = version;
            this.memory = GetMemoryType(memory);
            this.type = GetModuleType(type);
        }

        MemoryType GetMemoryType(string memory)
        {
            MemoryType mt = MemoryType.Unknown;

            for (int i = 0; i < Enum.GetValues(typeof(MemoryType)).Length; i++)
            {
                string enumString = ((MemoryType)i).ToString();
                if (enumString == memory)
                {
                    mt = (MemoryType)i;
                }
            }
            if (mt == MemoryType.Unknown)
            {
                Console.WriteLine("Internal Error: Module \"{0}\" has unknown memory \"{1}\"", name, memory);
            }

            return mt;
        }

        ModuleType GetModuleType(string type)
        {
            ModuleType mt = ModuleType.Unknown;

            type = type.Replace('/', '_');

            for (int i = 0; i < Enum.GetValues(typeof(ModuleType)).Length; i++)
            {
                string enumString = ((ModuleType)i).ToString();
                if (enumString == type)
                {
                    mt = (ModuleType)i;
                }
            }
            if (mt == ModuleType.Unknown)
            {
                Console.WriteLine("Warning: Module \"{0}\" is of unknown type \"{1}\"", name, type);
            }

            return mt;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
        }

        public MemoryType Memory
        {
            get
            {
                return memory;
            }
        }

        public string Memory4Download
        {
            get
            {
                switch (memory)
                {
                    case MemoryType.SystemRAM:
                        return "SYSRAM";

                    case MemoryType.UserROM:
                        return "ROM";

                    case MemoryType.UserRAM:
                        return "RAM";

                    case MemoryType.SystemROM:
                        return "SYSROM";

                    case MemoryType.FixRAM:
                        return "FIXRAM";

                    case MemoryType.MEMCARD:
                        return "MEMCARD";

                    case MemoryType.DRAM:
                        return "DRAM";
                }

                return "UNKNOWN";
            }
        }

        public ModuleType Type
        {
            get
            {
                return type;
            }
        }

        public bool IsPartOfOperatingSystem
        {
            get
            {
                if (memory == MemoryType.SystemROM || memory == MemoryType.SystemRAM)
                {
                    if (type == ModuleType.Boot) return true;
                    if (type == ModuleType.CommConfig) return true;
                    if (type == ModuleType.CommLibrary) return true;
                    if (type == ModuleType.DeviceDriver) return true;
                    if (type == ModuleType.Error) return true;
                    if (type == ModuleType.HWLibrary) return true;
                    if (type == ModuleType.InstTrapLibrary) return true;
                    if (type == ModuleType.MathLibrary) return true;
                    if (type == ModuleType.OSEXE) return true;
                    if (type == ModuleType.StartUp) return true;
                    if (type == ModuleType.SystemLibrary) return true;
                    if (type == ModuleType.SystemTask)
                    {
                        if (name.StartsWith("FF.")) return true;
                        if (name.StartsWith("$")) return true;
                    }
                }

                //otherwise this is not a system module
                return false; 
            }
        }

        public string Path
        {
        	get
        	{
        		return name.Replace("::","\\");
        	}
        }
        
    }

    public class ModuleInfoComparer : IComparer<ModuleInfo>
    {
        public int Compare(ModuleInfo x, ModuleInfo y)
        {
            if (x.Type > y.Type) return 1;
            if (x.Type < y.Type) return -1;
            //return 0;

           /* if (x.Name.StartsWith("$$")) return -1; // x < y
            if (y.Name.StartsWith("$$")) return 1;  // x > y

            if (x.Name.StartsWith("$")) return -1; // x < y
            if (y.Name.StartsWith("$")) return 1;  // x > y

            if (x.Name.StartsWith("FF.")) return -1; // x < y
            if (y.Name.StartsWith("FF.")) return 1;  // x > y
            */
            return x.Name.CompareTo(y.Name);
        }
    }


}

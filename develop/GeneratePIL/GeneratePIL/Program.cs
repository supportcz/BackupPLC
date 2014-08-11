using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace GeneratePIL
{
    static class Program
    {
        static List<ModuleInfo> moduleList;
        static string moduleDirectory = "Modules";
        static string backupFile = "Backup.pil";
        static string restoreFile = "Restore.pil";
        static string generateFile = "GeneratePIL.pil";
        static string AR_downloadFile = "DownloadAR.pil";

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Generates \"" + backupFile + "\" and \"" + restoreFile + "\" from ModuleList.");
                Console.WriteLine();
                Console.WriteLine("GeneratePIL modulelist [/S]");
                Console.WriteLine();
                Console.WriteLine("  modulelist       Specifies ModuleList file");
                Console.WriteLine("  /A               Include all modules");
                Console.WriteLine("  /S               Include system modules");
                goto L1;
            }

            string path = "";
            bool includeAllModules = false;
            bool includeSystemModules = false;

            // parse arguments
            foreach (string argument in args)
            {
                Regex regex = new Regex(@"\w+.txt");
                Match match = regex.Match(argument);

                if (match.Success)
                {
                    path = argument;
                    continue;
                }

                regex = new Regex(@"/[sS]");
                match = regex.Match(argument);

                if (match.Success)
                {
                    includeSystemModules = true;
                    continue;
                }

                regex = new Regex(@"/[aA]");
                match = regex.Match(argument);

                if (match.Success)
                {
                    includeAllModules = true;
                    continue;
                }
            }

            if (ReadModuleList(path) != 0) goto L1;

            //sort list
            ModuleInfoComparer mic = new ModuleInfoComparer();
            moduleList.Sort(mic);
            
            //try to read connection from GeneratePIL.pil file
            string connection;
            ReadConnection(generateFile, out connection);
            
            if (CreateBackupFile(backupFile, connection, includeAllModules, includeSystemModules) != 0) goto L1;

            if (CreateRestoreFile(restoreFile, connection, includeAllModules, includeSystemModules) != 0) goto L1;

            if (CreateAR_DownloadFile(AR_downloadFile, connection) != 0) goto L1;

            if (!System.Diagnostics.Debugger.IsAttached) return 0;

L1:
            Console.WriteLine();
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey();
            return 1;
        }

        static int ReadModuleList(string path)
        {
            try
            {
                StreamReader sr1 = new StreamReader(path);

                /* check first line */
                string line = sr1.ReadLine();

                Regex regex = new Regex(@"^\s*\[(ModuleList|Modulliste)\]");
                Match match = regex.Match(line);

                if (!match.Success)
                {
                    Console.WriteLine("Error in file \"{0}\", line 1", path);
                    return 1;
                }

                /* now decode all lines and fill list with info about modules */
                moduleList = new List<ModuleInfo>();
                regex = new Regex(@"(\S+)\s*(\S+),\s*(\S+),\s*(\S+),");

                while ((line = sr1.ReadLine()) != null)
                {
                    match = regex.Match(line);
                    if (match.Success)
                    {
                        moduleList.Add(new ModuleInfo(match.Groups[1].Value, match.Groups[2].Value,
                            match.Groups[3].Value, match.Groups[4].Value));
                    }
                    else
                    {
                        Console.WriteLine("Error in file \"{0}\", line {1}", path, moduleList.Count + 2);
                        return 1;
                    }
                }
                sr1.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }

            return 0;
        }

        static int CreateBackupFile(string path, string connection, bool incudeAllModules, bool incudeSystemModules)
        {
            try
            {
                StreamWriter sw1 = new StreamWriter(path);

                /* connect to PLC */
                sw1.WriteLine("Connection " + connection);

                /* backup modules */
                foreach (ModuleInfo module in moduleList)
                {
                    string line = "Upload \"" + module.Name + "\", \".\\" + moduleDirectory + "\\" + module.Path + ".br\"";

                    /* do not include AR files */
                    if (module.IsPartOfOperatingSystem)
                    {
                        if (incudeAllModules || incudeSystemModules)
                        {
                            line = "Remark \"" + line + "\"";
                        }
                        else
                        {
                            continue;
                        }
                    }
                    sw1.WriteLine(line);
                }
                
                /* backup variables */
                sw1.WriteLine("VariableListGlobal \".\\" + moduleDirectory + "\\VariableList.txt\"");

                sw1.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            //file created succesfully
            Console.WriteLine("File \"" + path + "\" successfully created.");

            return 0;
        }

        static int CreateRestoreFile(string path, string connection, bool incudeAllModules, bool incudeSystemModules)
        {
            try
            {
                StreamWriter sw1 = new StreamWriter(path);
                bool plcStopped = false;

                sw1.WriteLine("Connection " + connection);
                sw1.WriteLine("Diagnose");
                sw1.WriteLine("DeleteMemory ROM");
                sw1.WriteLine("Coldstart");

                foreach (ModuleInfo module in moduleList)
                {
                    // before task download go to service
                    if (module.Type >= ModuleType.ExceptionTask && !plcStopped)
                    {
                        sw1.WriteLine("Stop");
                        plcStopped = true;
                    }

                    string line = "Download \"" + ".\\" + moduleDirectory + "\\" + module.Path + ".br\", \"" + module.Memory4Download + "\"";

                    /* do not include AR files */
                    if (module.IsPartOfOperatingSystem)
                    {
                        if (incudeAllModules || incudeSystemModules)
                        {
                            line = "Remark \"" + line + "\"";
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (module.Type == ModuleType.Logger || module.Type == ModuleType.ProfilerData)
                    {
                        if (incudeAllModules)
                        {
                            line = "Remark \"" + line + "\"";
                        }
                        else
                        {
                            continue;
                        }
                    }

                    sw1.WriteLine(line);

                    //add coldstart after sysconf
                    if (module.Type == ModuleType.PLCConfig)
                    {
                        sw1.WriteLine("Coldstart");
                    }
                }

                /* backup variables */
                sw1.WriteLine("Stop");
                sw1.WriteLine("WriteVariableListToPLC \".\\" + moduleDirectory + "\\VariableList.txt\"");

                sw1.WriteLine("Warmstart");
                sw1.WriteLine("ModuleList \".\\ModuleListRestored.txt\"");

                sw1.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }

            //file created succesfully
            Console.WriteLine("File \"" + path + "\" successfully created.");
            return 0;
        }
        
        static int ReadConnection(string path, out string connection)
        {
       		try 
       		{
	        	StreamReader sr1 = new StreamReader(path);
	            Regex regex = new Regex(@"^\s*Connection\s*(.*)");
	            string line;
	            
	            while ((line = sr1.ReadLine()) != null)
	            {
	    	        Match match = regex.Match(line);
	
	    	        if (match.Success)
	    	        {
	    	        	connection = match.Groups[1].Value;
	    	        	return 0;
	    	        }
	            }
       		}
       		catch{
       			
       		}

   		    connection = "\"/IF=COM6 /BD=57600 /PA=2 /IT=1" + "\", \"\", \"WT=30\"";

   		    Console.WriteLine("Unable to read Connection from \"" + path + "\".");
       		Console.WriteLine("Default Connection will be used:"+connection);
       		return 1;
        }
        
        static int CreateAR_DownloadFile(string path, string connection)
        {
        	string sFile = "CP474V22.S17";
        	string brFile = "ARUpdate.br";
        	
            try
            {
                StreamWriter sw1 = new StreamWriter(path);

                sw1.WriteLine("ARUpdateFileGenerate \".\\" + moduleDirectory + "\\" + sFile + "\", \".\\" + moduleDirectory + "\\" + brFile
                             + "\", \"\"");
                sw1.WriteLine("Connection " + connection);
                sw1.WriteLine("Diagnose");
                sw1.WriteLine("DeleteMemory ROM");
                sw1.WriteLine("Coldstart");
                sw1.WriteLine("Download \".\\" + moduleDirectory + "\\" + brFile + "\", \"ROM\"");

                sw1.WriteLine("Coldstart");
                sw1.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }

            //file created succesfully
            Console.WriteLine("File \"" + path + "\" successfully created.");
            return 0;
        	
        }
    }
}

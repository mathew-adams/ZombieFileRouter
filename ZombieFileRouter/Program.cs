using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NDesk.Options;
using System.Diagnostics;
using System.Diagnostics;

namespace ZombieFileRouter
{
    class Program
    {
        private static bool log;
        private static bool Log
        {
            get { return log; }
            set { log = value; }
        }
        private static string logPath;
        private static string LogPath
        {
            get { return logPath; }
            set { logPath = value; }
        }

        static void Main(string[] args)
        {
            //Get torrent parameters
            string name = "";
            string category = "";
            string tags = "";
            string contentPath = "";
            string rootPath = "";
            string savePath = "";
            int fileCount = 0;
            Int64 size = 0;
            string hash = "";
            string logFile = "";
            Dictionary<string, string> newLocations = new Dictionary<string, string>();
            StringBuilder sbLog = new StringBuilder();
            string[] spl = {"="};

            //Get parameters
            OptionSet options = new OptionSet
            {
                //"=" is for required
                //":" is for optional
                {"n=|name", n =>{name = n;}},
                {"c:|category", c =>{category = c.ToLower();}},
                {"t:|tags", t =>{tags = t;}},
                {"cp=|contentpath", cp =>{contentPath = cp;}},
                {"rp=|rootpath", rp =>{rootPath = rp;}},
                {"sp=|savepath", sp =>{savePath = sp;}},
                {"fc:|filecount", fc =>{fileCount = int.Parse(fc);}},
                {"s:|size", s =>{size = Int64.Parse(s);}},
                {"h:|hash", h =>{hash = h;}},
                {"np=|newpath", np =>{newLocations.Add(np.Split(spl,StringSplitOptions.RemoveEmptyEntries)[0].ToLower(),
                                                       np.Split(spl,StringSplitOptions.RemoveEmptyEntries)[1].ToLower());}},
                {"lf:|log", lf =>{ logFile = lf;}}
            };
            
            try
            {
                options.Parse(args);
            }
            catch (Exception ex)
            {
                if (!String.IsNullOrEmpty(logFile))
                {
                    Log = true;
                    logPath = logFile;
                }

                if (Log)
                {
                    sbLog.Append(ex);
                    sbLog.AppendLine(Environment.NewLine);
                    WriteLog(sbLog);
                }
                Environment.Exit(-200);
            }

            if (!String.IsNullOrEmpty(logFile))
            {
                Log = true;
                logPath = logFile;
            }
            
            if (Log)
            {
                sbLog.AppendLine("------------------------------------------------------------------------------------------------------");
                sbLog.AppendLine("Command:..." + Environment.CommandLine);
                sbLog.AppendLine($"Timestamp:{DateTime.Now.ToString()}");
                sbLog.AppendLine($"name:{name}");
                sbLog.AppendLine($"category:{category}");
                sbLog.AppendLine($"contentpath:{contentPath}");
                sbLog.AppendLine($"rootpath:{rootPath}");
                sbLog.AppendLine($"savepath:{savePath}");
                sbLog.AppendLine($"filecount:{fileCount}");
                sbLog.AppendLine($"size:{size}");
                sbLog.AppendLine($"hash:{hash}");
                sbLog.AppendLine($"newpaths:{string.Join(",", newLocations.Select(pair => string.Format("{0}={1}", pair.Key.ToString(), pair.Value.ToString())).ToArray())}");
                sbLog.AppendLine($"logfile:{logFile}");
            }

            var rootFolder = new DirectoryInfo(rootPath);

            if (newLocations.ContainsKey(category))
            {
                if (Log)
                    sbLog.AppendLine("Category location found:" + category + "...");
                try
                {
                    string newFolder = Path.Combine(newLocations[category], rootFolder.Name);
                    if (!Directory.Exists(newFolder))
                    {
                        if (Log)
                            sbLog.AppendLine("Movies...Directory Move");
                        try
                        {
                            Directory.Move(rootPath, newFolder);
                            sbLog.AppendLine("Movies...directory move success.... to: " + newFolder);
                        }
                        catch
                        {
                            sbLog.AppendLine("Movies...directory move failed.... to: " + newFolder);
                            CopyAndDeleteFiles(Log, sbLog, rootPath, newFolder);
                        }
                    }
                    else
                    {
                        if(Directory.Exists(rootPath))
                        {
                            CopyAndDeleteFiles(Log, sbLog, rootPath, newFolder);
                        }
                        else
                        {
                            //source directory doesn't exist, log it and move on
                            if (Log)
                                sbLog.AppendLine("Source directory doesn't exist.... " + rootPath);
                        }
                    }
                    //Rename Directories and folders
                    if (category.Equals("movies", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //Start the file rename process
                        string quote = @"""";
                        string renameArgs = "--directory=" + quote + newFolder + quote + " --moveafterrename=" + quote + @"E:\Movies" + quote + " --log=" + quote + @"C:\temp\zombiefilerename\log.txt" + quote;
                        if (Log)
                            sbLog.AppendLine("File renaming program arguments.... " + renameArgs);
                        string executingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                        string renameExecutable = Path.Combine(executingDirectory, "ZombieFileRename.exe");
                        if (Log)
                            sbLog.AppendLine("File renaming program located.... " + renameExecutable);

                        if (Log)
                            sbLog.AppendLine("Invoke file renaming program.... " + newFolder);
                        Process process = new Process();
                        process.StartInfo.FileName = renameExecutable;
                        process.StartInfo.Arguments = renameArgs;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.Start();
                        process.WaitForExit();

                        if (Log)
                            sbLog.AppendLine("Invoke file renaming program success.... " + newFolder);
                    }
                }
                catch (Exception fex)
                {
                    if(Log)
                    {
                        sbLog.Append(fex);
                        sbLog.AppendLine(Environment.NewLine);
                        WriteLog(sbLog);
                    }
                    Environment.Exit(-300);
                }
            }
            else
            {
                //No directory to route to, nothing to do. Log it and move on
                if(Log)
                    sbLog.AppendLine("Category directory not found.... " + category);
            }

            //Write log
            if(Log && sbLog.Length > 0 )
            {
                WriteLog(sbLog);
            }
        }
        #region "Private functions"
        private static void WriteLog(StringBuilder sb)
        {
            var logFile = new FileInfo(LogPath);

            if (!logFile.Directory.Exists)
            {
                logFile.Directory.Create();
            }
            using (StreamWriter swrt = new StreamWriter(LogPath, true))
            {
                swrt.Write(sb.ToString());
            }
        }
        private static void CopyAndDeleteFiles(bool log,
                                               StringBuilder sbLog,
                                               string source,
                                               string dest)
        {
            if (Directory.Exists(source))
            {
                //Create destination folder
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                    if (Log)
                        sbLog.AppendLine("Created new destination folder..." + dest);
                }
                
                //source directory exists, move files
                foreach (string fileName in Directory.GetFiles(source))
                {
                    if (Log)
                        sbLog.AppendLine("Files..." + fileName);

                    var file = new FileInfo(fileName);

                    string moveTo = Path.Combine(dest, file.Name);
                    if (!File.Exists(moveTo))
                    {
                        if (Log)
                            sbLog.AppendLine("Copy File from...." + fileName);

                        File.Move(fileName, moveTo);

                        if (Log)
                            sbLog.AppendLine("File copied to new folder..." + moveTo);
                    }
                    else
                    {
                        //File already exists in destination
                        if (Log)
                            sbLog.AppendLine("File exists.... " + moveTo);
                    }
                    //after copy, delete from source
                    File.Delete(fileName);
                    if (Log)
                        sbLog.AppendLine("File deleted.... " + fileName);
                }

                //Move folders 
                foreach (string dirName in Directory.GetDirectories(source))
                {
                    var directoryInfo = new DirectoryInfo(dirName);
                    string moveTo = Path.Combine(dest, directoryInfo.Name);
                    try
                    {
                        if (log)
                            sbLog.AppendLine("Move subdirectory.... " + moveTo);
                        Directory.Move(dirName, moveTo);
                    }
                    catch
                    {
                        if (log)
                            sbLog.AppendLine("Subdirectory move failed.....recursive CopyAndDeleteFiles.... " + moveTo);
                        //Recursively CopyAndDeleteFiles
                        CopyAndDeleteFiles(log, sbLog, dirName, moveTo);
                    }
                }

                if (Log)
                    sbLog.AppendLine("Begin delete source folder.... " + source);

                //after files have been moved, perform cleanup on source directory (all files need to be cleaned up)
                try
                {
                    Directory.Delete(source);
                }
                catch (Exception fex)
                {
                    sbLog.Append(fex);
                    sbLog.AppendLine(Environment.NewLine);
                    WriteLog(sbLog);
                    Environment.Exit(-400);
                }
                if (Log)
                    sbLog.AppendLine("End delete source folder.... " + source);
            }
        }
        #endregion
    }
}

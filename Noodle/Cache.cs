using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Noodle
{
    public static class Cache
    {
        #region Enumerations
        public enum CacheTypes { FileContent, FileName, ColumnName, TableName, ProcedureContent, ProcedureName, FunctionContent, FunctionName, ViewContent, ViewName };
        public enum FileTypes { All, DotNet, SQL, VB, Text }
        #endregion

        #region Properties and Fields
        private static bool _refresh = true;

        public static int CachedFileItemCount;
        public static int CachedDatabaseItemCount;
        public static string RootFolder;
        public static string Server;
        public static string Database;
        public static string UserName;
        public static string Password;
        public static FileTypes FileType = FileTypes.All;

        private static List<string> fileExtensionsToIgnore
        {
            get
            {
                var ignoreExtensions = new List<string>();
                ignoreExtensions.Add("._");
                ignoreExtensions.Add(".application");
                ignoreExtensions.Add(".avi");
                ignoreExtensions.Add(".baml");
                ignoreExtensions.Add(".cache");
                ignoreExtensions.Add(".cdf-ms");
                ignoreExtensions.Add(".copycomplete");
                ignoreExtensions.Add(".csproj");
                ignoreExtensions.Add(".csv");
                ignoreExtensions.Add(".dll");
                ignoreExtensions.Add(".exe");
                ignoreExtensions.Add(".gif");
                ignoreExtensions.Add(".ico");
                ignoreExtensions.Add(".ide");
                ignoreExtensions.Add(".jpeg");
                ignoreExtensions.Add(".jpg");
                ignoreExtensions.Add(".json");
                ignoreExtensions.Add(".lref");
                ignoreExtensions.Add(".maverick");
                ignoreExtensions.Add(".maverickdev");
                ignoreExtensions.Add(".maverickpublictest");
                ignoreExtensions.Add(".maverickuat");
                ignoreExtensions.Add(".m4v");
                ignoreExtensions.Add(".md");
                ignoreExtensions.Add(".mp3");
                ignoreExtensions.Add(".mp4");
                ignoreExtensions.Add(".mpeg");
                ignoreExtensions.Add(".mpg");
                ignoreExtensions.Add(".msi");
                ignoreExtensions.Add(".nupkg");
                ignoreExtensions.Add(".ogg");
                ignoreExtensions.Add(".ogv");
                ignoreExtensions.Add(".pack");
                ignoreExtensions.Add(".p7s");
                ignoreExtensions.Add(".pdb");
                ignoreExtensions.Add(".pdf");
                ignoreExtensions.Add(".pfx");
                ignoreExtensions.Add(".png");
                ignoreExtensions.Add(".ps1");
                ignoreExtensions.Add(".resources");
                ignoreExtensions.Add(".sln");
                ignoreExtensions.Add(".suo");
                ignoreExtensions.Add(".svg");
                ignoreExtensions.Add(".targets");
                ignoreExtensions.Add(".user");
                ignoreExtensions.Add(".webm");
                ignoreExtensions.Add(".webp");
                ignoreExtensions.Add(".wmv");
                ignoreExtensions.Add(".xcf");
                ignoreExtensions.Add(".zip");
                ignoreExtensions.Add("");
                return ignoreExtensions;
            }
        }

        private static string connection_string
        {
            get
            {
                //return $"Server={Server};Database={Database};User ID={UserName}; Password={Password};Trusted_Connection=False";
                Server = "localhost";
                Database = "IASDev";

                return $"Server=localhost;Database=IASDev;Trusted_Connection=True";
            }
        }

        public static Dictionary<FileTypes, List<string>> FileExtensions = new Dictionary<FileTypes, List<string>>();

        private static List<CacheEntry> _file_content_cache = new List<CacheEntry>();
        public static List<CacheEntry> FileContentCache
        {
            get
            {
                _file_content_cache = GetCache(CacheTypes.FileContent, _file_content_cache, _refresh);
                return _file_content_cache;
            }
        }

        private static List<CacheEntry> _file_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> FileNameCache
        {
            get
            {
                _file_name_cache = GetCache(CacheTypes.FileName, _file_name_cache, _refresh);
                return _file_name_cache;
            }
        }

        private static List<CacheEntry> _table_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> TableNameCache
        {
            get
            {
                _table_name_cache = GetCache(CacheTypes.TableName, _table_name_cache, _refresh);
                return _table_name_cache;
            }
        }

        private static List<CacheEntry> _column_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> ColumnNameCache
        {
            get
            {
                _column_name_cache = GetCache(CacheTypes.ColumnName, _column_name_cache, _refresh);
                return _column_name_cache;
            }
        }

        private static List<CacheEntry> _procedure_content_cache = new List<CacheEntry>();
        public static List<CacheEntry> ProcedureContentCache
        {
            get
            {
                _procedure_content_cache = GetCache(CacheTypes.ProcedureContent, _procedure_content_cache, _refresh);
                return _procedure_content_cache;
            }
        }

        private static List<CacheEntry> _procedure_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> ProcedureNameCache
        {
            get
            {
                _procedure_name_cache = GetCache(CacheTypes.ProcedureName, _procedure_name_cache, _refresh);
                return _procedure_name_cache;
            }
        }

        private static List<CacheEntry> _function_content_cache = new List<CacheEntry>();
        public static List<CacheEntry> FunctionContentCache
        {
            get
            {
                _function_content_cache = GetCache(CacheTypes.FunctionContent, _function_content_cache, _refresh);
                return _function_content_cache;
            }
        }

        private static List<CacheEntry> _function_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> FunctionNameCache
        {
            get
            {
                _function_name_cache = GetCache(CacheTypes.FunctionName, _function_name_cache, _refresh);
                return _function_name_cache;
            }
        }

        private static List<CacheEntry> _view_content_cache = new List<CacheEntry>();
        public static List<CacheEntry> ViewContentCache
        {
            get
            {
                _view_content_cache = GetCache(CacheTypes.ViewContent, _view_content_cache, _refresh);
                return _view_content_cache;
            }
        }

        private static List<CacheEntry> _view_name_cache = new List<CacheEntry>();
        public static List<CacheEntry> ViewNameCache
        {
            get
            {
                _view_name_cache = GetCache(CacheTypes.ViewName, _view_name_cache, _refresh);
                return _view_name_cache;
            }
        }

        private static List<CacheEntry> _all_cache = new List<CacheEntry>();
        public static List<CacheEntry> AllCache
        {
            get
            {
                return _all_cache;
            }
        }
        #endregion

        public static void UpdateFileCache(CacheTypes cacheType)
        {
            _refresh = true;
            if (!string.IsNullOrWhiteSpace(RootFolder))
            {
                CachedFileItemCount = 0;
                if (cacheType == CacheTypes.FileName)
                {
                    AllCache.AddRange(FileNameCache); 
                }
                else if (cacheType == CacheTypes.FileContent)
                {
                    AllCache.AddRange(FileContentCache); 
                }
            }
        }

        public static void UpdateDatabaseCache(CacheTypes cacheType)
        {
            _refresh = true;
            if (!string.IsNullOrWhiteSpace(Server))
            {
                CachedDatabaseItemCount = 0;
                switch (cacheType)
                {
                    case CacheTypes.TableName:
                        AllCache.AddRange(TableNameCache);
                        break;
                    case CacheTypes.ColumnName:
                        AllCache.AddRange(ColumnNameCache);
                        break;
                    case CacheTypes.ProcedureName:
                        AllCache.AddRange(ProcedureNameCache);
                        break;
                    case CacheTypes.ProcedureContent:
                        AllCache.AddRange(ProcedureContentCache);
                        break;
                    case CacheTypes.FunctionName:
                        AllCache.AddRange(FunctionNameCache);
                        break;
                    case CacheTypes.FunctionContent:
                        AllCache.AddRange(FunctionContentCache);
                        break;
                    case CacheTypes.ViewName:
                        AllCache.AddRange(ViewNameCache);
                        break;
                    case CacheTypes.ViewContent:
                        AllCache.AddRange(ViewContentCache);
                        break;
                }
            }
        }

        private static List<CacheEntry> GetCache(CacheTypes cacheType, List<CacheEntry> searchCache, bool forceRefresh, FileTypes fileType = FileTypes.All)
        {
            if (forceRefresh || !searchCache.Any())
            {
                searchCache.Clear();
                //if (cacheType == CacheTypes.FileContent || cacheType == CacheTypes.FileName)
                //{
                switch (cacheType)
                {
                    case CacheTypes.FileContent:
                        searchCache = CacheFileContents(fileType);
                        break;
                    case CacheTypes.FileName:
                        searchCache = CacheFileNames(fileType);
                        break;
                    case CacheTypes.ColumnName:
                        searchCache = CacheColumnNames();
                        break;
                    case CacheTypes.TableName:
                        searchCache = CacheTableNames();
                        break;
                    case CacheTypes.ProcedureContent:
                        searchCache = CacheProcedureContents();
                        break;
                    case CacheTypes.ProcedureName:
                        searchCache = CacheProcedureNames();
                        break;
                    case CacheTypes.FunctionContent:
                        searchCache = CacheFunctionContents();
                        break;
                    case CacheTypes.FunctionName:
                        searchCache = CacheFunctionNames();
                        break;
                    case CacheTypes.ViewContent:
                        searchCache = CacheViewContents();
                        break;
                    case CacheTypes.ViewName:
                        searchCache = CacheViewNames();
                        break;
                }
                //}
            }
            _refresh = false;
            return searchCache;
        }

        /// <summary>
        /// You must include the dot such as .cs or .txt or .config
        /// </summary>
        /// <param name="fileType"></param>
        /// <param name="extension"></param>
        public static void AddExtension(FileTypes fileType, string extension)
        {
            FileExtensions.Where(x => x.Key == FileTypes.All).Select(x => x.Value).FirstOrDefault().Add(extension);
            FileExtensions.Where(x => x.Key == fileType).Select(x => x.Value).FirstOrDefault().Add(extension);
        }

        private static List<CacheEntry> CacheFileNames(FileTypes fileType)
        {
            var results = new List<CacheEntry>();
            try
            {
                var directoryNames = Directory.GetDirectories(RootFolder).ToList().Where(x => !x.StartsWith(".")); //dir starting with "." are usually hidden folders
                foreach (var name in directoryNames)
                {
                    try
                    {
                        var directory = new DirectoryInfo(name);
                        results.Add(new CacheEntry
                        {
                            Location = directory.Parent.FullName,
                            Name = directory.Name,
                            Value = directory.FullName,
                            LineNumber = null,
                            CacheType = CacheTypes.FileName
                        });
                        CachedFileItemCount++;
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        CachedFileItemCount--;
                        Debug.WriteLine(uae.Message);
                        Debug.WriteLine(uae.StackTrace);
                    }
                }

                var pattern = "*.*";
                if (fileType != FileTypes.All)
                {
                    pattern = string.Join(",", FileExtensions.Where(x => x.Key == fileType).Select(x => x.Value));
                    pattern = $"*{pattern.Replace(',', '*')}";
                }
                var fileNames = Directory.GetFiles(RootFolder, pattern, SearchOption.AllDirectories).ToList();

                var extList = new List<string>();
                foreach (var file in fileNames)
                {
                    if (!extList.Contains(Path.GetExtension(file)))
                    {
                        extList.Add(Path.GetExtension(file));
                        Debug.WriteLine(Path.GetExtension(file));
                    }
                }
                foreach (var name in fileNames)
                {
                    try
                    {
                        var file = new FileInfo(name);
                        if (fileExtensionsToIgnore.Contains(file.Extension.ToLower())) { continue; }

                        results.Add(new CacheEntry
                        {
                            Location = file.DirectoryName,
                            Name = file.Name,
                            Value = file.FullName,
                            LineNumber = null,
                            CacheType = CacheTypes.FileName
                        });
                        CachedFileItemCount++;
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        CachedFileItemCount--;
                        Debug.WriteLine(uae.Message);
                        Debug.WriteLine(uae.StackTrace);
                    }
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.WriteLine(uae.Message);
                Debug.WriteLine(uae.StackTrace);
            }
            return results;
        }

        /// <summary>
        /// This is very slow
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns></returns>
        private static List<CacheEntry> CacheFileContentsToFile(FileTypes fileType)
        {
            var tempDelimiter = "ZZ_TEMP_ZZ";
            var tempDelimitedFilePath = $@"C:\CODE MINE\CURRENT\Noodle\File Content Cache.tab";
            var results = new List<CacheEntry>();
            var pattern = "*.*";
            if (fileType != FileTypes.All)
            {
                pattern = string.Join(",", FileExtensions.Where(x => x.Key == fileType).Select(x => x.Value));
                pattern = $"*{pattern.Replace(',', '*')}";
            }
            var fileSystemObjects = new DirectoryInfo(RootFolder)
                .EnumerateFiles(pattern, SearchOption.AllDirectories)
                .Where(x => !x.Name.StartsWith(".") 
                    && !fileExtensionsToIgnore.Contains(x.Extension.ToLowerInvariant())
                    && x.Length.BytesToMB() < Convert.ToInt32(ConfigurationManager.AppSettings["MaxFileSizeInMB"]));  // Skip hidden files
            var isLocked = false;
            List<Process> lstProcs = new List<Process>();            
            for (int i = 0; i < fileSystemObjects.Count(); i++)
            {
                lstProcs = ProcessHandler.WhoIsLocking(fileSystemObjects.ElementAt(i).FullName);
                foreach (Process p in lstProcs)
                {
                    if (p.ProcessName != "Noodle")
                    {
                        var s = p.ProcessName;
                        isLocked = true;
                        continue;
                    }
                }
                if (isLocked) { continue; }

                var currentCacheEntry = new CacheEntry();
                using (var fs = new StreamWriter(tempDelimitedFilePath))
                using (var reader = new StreamReader(fileSystemObjects.ElementAt(i).FullName))
                {
                    var lineNumber = 0;
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            lineNumber++;
                            line = reader.ReadLine();
                            continue;
                        }
                        try
                        {
                            currentCacheEntry = new CacheEntry
                            {
                                Location = fileSystemObjects.ElementAt(i).DirectoryName,
                                Name = fileSystemObjects.ElementAt(i).Name,
                                Value = line.Trim(),
                                LineNumber = lineNumber,
                                CacheType = CacheTypes.FileContent
                            };
                            fs.WriteLine($@"{currentCacheEntry.ToDelimitedString(tempDelimiter)}");
                            CachedFileItemCount++;
                            lineNumber++;
                            line = reader.ReadLine();
                        }
                        catch (UnauthorizedAccessException uae)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(uae.Message);
                            Debug.WriteLine(uae.StackTrace);
                        }
                        catch (IOException ioe)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(ioe.Message);
                            Debug.WriteLine(ioe.StackTrace);
                        }
                        catch (Exception ex)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }

            using(var reader = new StreamReader(tempDelimitedFilePath))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    results.Add(CacheEntry.FromDelimitedString(line, tempDelimiter));
                    line = reader.ReadLine();
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheFileContents(FileTypes fileType)
        {
            var results = new List<CacheEntry>();
            //var pattern = "*.*";
            //if (fileType != FileTypes.All)
            //{
            //    pattern = string.Join(",", FileExtensions.Where(x => x.Key == fileType).Select(x => x.Value));
            //    pattern = $"*{pattern.Replace(',', '*')}";
            //}
            var dir = new DirectoryInfo(RootFolder);
            //var tempFiles = dir.GetFiles("*", SearchOption.AllDirectories);//.Where(x => !x.Name.StartsWith(".")).ToList();
            //var tempFiles = new List<FileInfo>();
            //dirs.ForEach(x =>
            //{
            //    tempFiles.AddRange(x.GetFiles("*.*").ToList<FileInfo>());
            //});
            var files = dir.GetTextFiles();
            //foreach(var file in tempFiles)
            //{
            //    if (string.IsNullOrWhiteSpace(file.Extension)) { continue; }
            //    if (file.Name.StartsWith(".")) { continue; }
            //    if (file.Directory.Name.StartsWith(".")) { continue; }
            //    if (fileExtensionsToIgnore.Contains(file.Extension.ToLower())) { continue; }
            //    if (file.Length.BytesToMB() > Convert.ToInt32(ConfigurationManager.AppSettings["MaxFileSizeInMB"])) { continue; }
            //    files.Add(file);
            //}
            //var files = dir.GetFiles(pattern, SearchOption.AllDirectories).ToList().Where(x => !x.Name.StartsWith(".")); //dir starting with "." are usually hidden folders
            var isLocked = false;
            var fileCount = files.Count();
            List<Process> lstProcs = new List<Process>();
            for(int i = 0; i < fileCount; i++)
            {                
                lstProcs = ProcessHandler.WhoIsLocking(files[i].FullName);
                foreach (Process p in lstProcs)
                {
                    if (p.ProcessName != "Noodle")
                    {
                        var s = p.ProcessName;
                        isLocked = true;
                        continue;
                    }
                }
                if (isLocked) { continue; }
                using (var fs = File.Open(files[i].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    var lineNumber = 1;
                    string line = null;
                    line = reader.ReadLine();
                    while (line != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            lineNumber++;
                            line = reader.ReadLine();
                            continue;
                        }
                        try
                        {
                            results.Add(new CacheEntry
                            {
                                Location = files[i].DirectoryName,
                                Name = files[i].Name,
                                Value = line.Trim(),
                                LineNumber = lineNumber,
                                CacheType = CacheTypes.FileContent
                            });
                            CachedFileItemCount++;
                            lineNumber++;
                            line = reader.ReadLine();
                        }
                        catch (UnauthorizedAccessException uae)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(uae.Message);
                            Debug.WriteLine(uae.StackTrace);
                        }
                        catch (IOException ioe)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(ioe.Message);
                            Debug.WriteLine(ioe.StackTrace);
                        }
                        catch (Exception ex)
                        {
                            CachedFileItemCount--;
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
                if(i.TriggerLogic(100, fileCount)) { Debug.WriteLine(i.ThisOfThat(fileCount)); }
                //Debug.WriteLine($"{i.ToString("000000")}/{fileCount.ToString("000000")}\t{files[i].FullName}");
            }
            return results;
        }

        private static List<CacheEntry> CacheTableNames()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    try
                    {
                        results.Add(new CacheEntry
                        {
                            Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                            Name = reader.GetValue(2).ToString(),
                            Value = reader.GetValue(2).ToString(),
                            LineNumber = null,
                            CacheType = CacheTypes.TableName
                        });
                        CachedDatabaseItemCount++;
                    }
                    catch (Exception ex)
                    {
                        CachedDatabaseItemCount--;
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheColumnNames()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT t.TABLE_CATALOG, t.TABLE_SCHEMA, t.TABLE_NAME, c.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLES t JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME WHERE t.TABLE_TYPE = 'BASE TABLE' ORDER BY c.TABLE_CATALOG, c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    try
                    {
                        results.Add(new CacheEntry
                        {
                            Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}.{reader.GetValue(2).ToString()}",
                            Name = reader.GetValue(3).ToString(),
                            Value = reader.GetValue(3).ToString(),
                            LineNumber = null,
                            CacheType = CacheTypes.ColumnName
                        });
                        CachedDatabaseItemCount++;
                    }
                    catch (Exception ex)
                    {
                        CachedDatabaseItemCount--;
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheProcedureNames()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    try
                    {
                        results.Add(new CacheEntry
                        {
                            Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                            Name = reader.GetValue(2).ToString(),
                            Value = reader.GetValue(2).ToString(),
                            LineNumber = null,
                            CacheType = CacheTypes.ProcedureName
                        });
                        CachedDatabaseItemCount++;
                    }
                    catch (Exception ex)
                    {
                        CachedDatabaseItemCount--;
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheProcedureContents()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE' ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    var lineNumber = 1;
                    var procedureName = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}.{reader.GetValue(2).ToString()}";
                    var contentQuery = $"sp_helptext '{procedureName}'";
                    using (var contentCommand = new SqlCommand(contentQuery))
                    using (contentCommand.Connection = new SqlConnection(connection_string))
                    {
                        contentCommand.CommandType = CommandType.Text;
                        contentCommand.CommandTimeout = 900000;
                        contentCommand.Connection.Open();
                        SqlDataReader contentReader = null;
                        try
                        {
                            contentReader = contentCommand.ExecuteReader(CommandBehavior.CloseConnection);
                        
                            while (contentReader.Read())
                            {
                                if (string.IsNullOrWhiteSpace(contentReader.GetValue(0).ToString()))
                                {
                                    lineNumber++;
                                    continue;
                                }
                                try
                                {
                                    results.Add(new CacheEntry
                                    {
                                        Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                                        Name = reader.GetValue(2).ToString(),
                                        Value = contentReader.GetValue(0).ToString().Trim(),
                                        LineNumber = lineNumber,
                                        CacheType = CacheTypes.ProcedureContent
                                    });
                                    lineNumber++;
                                    CachedDatabaseItemCount++;
                                }
                                catch (Exception ex)
                                {
                                    CachedDatabaseItemCount--;
                                    Debug.WriteLine(ex.Message);
                                    Debug.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        catch (Exception readEx)
                        {
                            Debug.WriteLine(readEx.Message);
                            Debug.WriteLine(contentCommand.CommandText);
                        }
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheFunctionNames()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'FUNCTION' ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    try
                    {
                        results.Add(new CacheEntry
                        {
                            Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                            Name = reader.GetValue(2).ToString(),
                            Value = reader.GetValue(2).ToString(),
                            LineNumber = null,
                            CacheType = CacheTypes.FunctionName
                        });
                        CachedDatabaseItemCount++;
                    }
                    catch (Exception ex)
                    {
                        CachedDatabaseItemCount--;
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheFunctionContents()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'FUNCTION' ORDER BY SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    var lineNumber = 1;
                    var functionName = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}.{reader.GetValue(2).ToString()}";
                    var contentQuery = $"sp_helptext '{functionName}'";
                    using (var contentCommand = new SqlCommand(contentQuery))
                    using (contentCommand.Connection = new SqlConnection(connection_string))
                    {
                        try
                        {
                            contentCommand.CommandType = CommandType.Text;
                            contentCommand.CommandTimeout = 900000;
                            contentCommand.Connection.Open();
                            var contentReader = contentCommand.ExecuteReader(CommandBehavior.CloseConnection);
                            while (contentReader.Read())
                            {
                                if (string.IsNullOrWhiteSpace(contentReader.GetValue(0).ToString()))
                                {
                                    lineNumber++;
                                    continue;
                                }
                                try
                                {
                                    results.Add(new CacheEntry
                                    {
                                        Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                                        Name = reader.GetValue(2).ToString(),
                                        Value = contentReader.GetValue(0).ToString().Trim(),
                                        LineNumber = lineNumber,
                                        CacheType = CacheTypes.FunctionContent
                                    });
                                    lineNumber++;
                                    CachedDatabaseItemCount++;
                                }
                                catch (Exception ex)
                                {
                                    CachedDatabaseItemCount--;
                                    Debug.WriteLine(ex.Message);
                                    Debug.WriteLine(ex.StackTrace);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine($"Error reading function {functionName}");
                            Debug.WriteLine(e.Message);
                        }
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheViewNames()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'VIEW' ORDER BY TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    try
                    {
                        results.Add(new CacheEntry
                        {
                            Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                            Name = reader.GetValue(2).ToString(),
                            Value = reader.GetValue(2).ToString(),
                            LineNumber = null,
                            CacheType = CacheTypes.ViewName
                        });
                        CachedDatabaseItemCount++;
                    }
                    catch (Exception ex)
                    {
                        CachedDatabaseItemCount--;
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
            return results;
        }

        private static List<CacheEntry> CacheViewContents()
        {
            var results = new List<CacheEntry>();
            var query = "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'VIEW' ORDER BY TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME";
            using (var command = new SqlCommand(query))
            using (command.Connection = new SqlConnection(connection_string))
            {
                command.CommandType = CommandType.Text;
                command.CommandTimeout = 900000;
                command.Connection.Open();
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                while (reader.Read())
                {
                    var lineNumber = 1;
                    var viewName = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}.{reader.GetValue(2).ToString()}";
                    var contentQuery = $"sp_helptext '{viewName}'";
                    using (var contentCommand = new SqlCommand(contentQuery))
                    using (contentCommand.Connection = new SqlConnection(connection_string))
                    {
                        contentCommand.CommandType = CommandType.Text;
                        contentCommand.CommandTimeout = 900000;
                        contentCommand.Connection.Open();
                        var contentReader = contentCommand.ExecuteReader(CommandBehavior.CloseConnection);
                        while (contentReader.Read())
                        {
                            if (string.IsNullOrWhiteSpace(contentReader.GetValue(0).ToString())) { continue; }
                            try
                            {
                                results.Add(new CacheEntry
                                {
                                    Location = $"{reader.GetValue(0).ToString()}.{reader.GetValue(1).ToString()}",
                                    Name = reader.GetValue(2).ToString(),
                                    Value = contentReader.GetValue(0).ToString().Trim(),
                                    LineNumber = lineNumber,
                                    CacheType = CacheTypes.ViewContent
                                });
                                lineNumber++;
                                CachedDatabaseItemCount++;
                            }
                            catch (Exception ex)
                            {
                                CachedDatabaseItemCount--;
                                Debug.WriteLine(ex.Message);
                                Debug.WriteLine(ex.StackTrace);
                            }
                        }
                    }
                }
            }
            return results;
        }
    }
}

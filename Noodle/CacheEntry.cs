using System;
using System.Diagnostics;
using System.IO;
using static Noodle.Cache;

namespace Noodle
{
    public class CacheEntry
    {        
        public CacheTypes CacheType { get; set; }
        public string Location { get; set; }  // c:\sds\asdfsd, ias-sqlsss5.dbo
        public string Name { get; set; }  // file, proc, view, function, table, column name
        public int? LineNumber { get; set; }
        public string Value { get; set; }  // the text of the line, null if not content

        public CacheEntry() { }

        public void Launch(string server, string database, string username, string password)
        {
            var scriptFile = new FileInfo($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Temp{Path.DirectorySeparatorChar}{DateTime.Now.ToString("hhMMddmmss")}.sql");
            Directory.CreateDirectory(scriptFile.Directory.FullName);
            var createStream = File.Create(scriptFile.FullName);
            createStream.Dispose();
            var launch = new ProcessStartInfo();
            switch (CacheType)
            {
                case CacheTypes.FileName:
                case CacheTypes.FileContent:
                    launch = new ProcessStartInfo($"{Location}{Path.DirectorySeparatorChar}{Name}");
                    break;
                case CacheTypes.TableName:
                case CacheTypes.ProcedureContent:
                case CacheTypes.FunctionContent:
                case CacheTypes.ViewContent:
                    launch = new ProcessStartInfo($"ssms.exe", $" -U {username} -P {password} -S {server} -d {database}");
                    break;          
                case CacheTypes.ColumnName:                   
                    File.WriteAllText(scriptFile.FullName, $"SELECT TOP 10 {Value} FROM {Location};");
                    launch = new ProcessStartInfo($"ssms.exe", $"{scriptFile.FullName} -U {username} -P {password} -S {server} -d {database}");
                    break;
                case CacheTypes.ViewName: 
                    File.WriteAllText(scriptFile.FullName, $"SELECT TOP 10 * FROM {Value};");
                    launch = new ProcessStartInfo($"ssms.exe", $"{scriptFile.FullName} -U {username} -P {password} -S {server} -d {database}");
                    break;
                case CacheTypes.ProcedureName:
                    File.WriteAllText(scriptFile.FullName, $"EXEC {Value};"); 
                    launch = new ProcessStartInfo($"ssms.exe", $"{scriptFile.FullName} -U {username} -P {password} -S {server} -d {database}");
                    break;
                case CacheTypes.FunctionName:
                    File.WriteAllText(scriptFile.FullName, $"SELECT * FROM {Location}();");
                    launch = new ProcessStartInfo($"ssms.exe", $"{scriptFile.FullName} -U");
                    break;
            }            
            Process.Start(launch);
        }

        public override string ToString()
        {
            return $"{Location} {Name} {LineNumber} {Value}";
        }

        public static string TabStringHeader()
        {
            return "Result Type,Location,Name,Line Number,Content/Value";
        }

        public string ToDelimitedString(string delimiter)
        {
            return $"{CacheType}{delimiter}{Location}{delimiter}{Name}{delimiter}{LineNumber}{delimiter}{Value}";
        }

        public static CacheEntry FromDelimitedString(string cacheString, string delimiter)
        {
            var properties = cacheString.Split(new string[] {delimiter}, StringSplitOptions.None);
            int.TryParse(properties[3], out int linenumber);
            var entry = new CacheEntry
            {
                CacheType = (CacheTypes)Enum.Parse(typeof(CacheTypes), properties[0], true),
                Location = properties[1],
                Name = properties[2],
                LineNumber = linenumber,
                Value = properties[4].Trim()
            };
            return entry;
        }

        public static string GetCacheTypeName(CacheTypes cacheType)
        {
            var result = "";
            switch (cacheType)
            {
                //case CacheTypes.AllData:
                //    result = "Database";
                //    break;
                //case CacheTypes.AllFile:
                //    result = "File";
                //    break;
                case CacheTypes.ColumnName:
                    result = "Table Column Name";
                    break;
                case CacheTypes.FileContent:
                    result = "File Content";
                    break;
                case CacheTypes.FileName:
                    result = "File Name";
                    break;
                case CacheTypes.FunctionContent:
                    result = "Function Content";
                    break;
                case CacheTypes.FunctionName:
                    result = "Function Name";
                    break;
                case CacheTypes.ProcedureContent:
                    result = "Procedure Content";
                    break;
                case CacheTypes.ProcedureName:
                    result = "Procedure Name";
                    break;
                case CacheTypes.TableName:
                    result = "Table Name";
                    break;
                case CacheTypes.ViewName:
                    result = "View Name";
                    break;
                case CacheTypes.ViewContent:
                    result = "View Name";
                    break;
            }
            return result;
        }
    }
}

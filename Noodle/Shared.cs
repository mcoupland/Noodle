using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Noodle
{
    public static class Shared
    {
        public static bool TriggerLogic(this int value, int splitIndex, int maxIndex)
        {
            if (value == 0) { return false; }
            if (value % splitIndex == 0) { return true; }
            if (value >= maxIndex - 1) { return true; }
            return false;
        }

        public static string ThisOfThat(this int value, int max)
        {
            return $"{value + 1}/{max}";
        }

        public static List<FileInfo> GetTextFiles(this DirectoryInfo value, bool ignoreHidden = true)
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

            return value.EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(
                    x => !x.FullName.Contains(@"\.") &&
                    !x.Name.StartsWith(".") &&
                    !string.IsNullOrWhiteSpace(x.Extension) &&
                    !ignoreExtensions.Contains(x.Extension)
                )
                .ToList();
        }

        public static int MaxCellContentLength = 250; // Excel max is 500 per cell, 32767 total, but that is ridiculous for our purposes;

        public static long BytesToMB(this long byteCount)
        {
            return byteCount / 1048576;
        }

        public static string GetNewPath(string backupFile, string appendText = "")
        {
            return $"{Path.GetDirectoryName(backupFile)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(backupFile)}{appendText}{Path.GetExtension(backupFile)}";
        }

        public static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("AssurantUploaderLogFile");

        public static string TabString = "   ";

        public static string TrimPath(string value)
        {
            return value.TrimEnd("\\".ToCharArray());
        }
        
        public static string SingularPlural(int value, string text)
        {
            return value == 1 ? $"{value} {text}" : $"{value} {text}s";
        }

        public static DateTime GetRecommendedStartDate()
        {
            var year = DateTime.Now.Month != 1 ? DateTime.Now.Year : DateTime.Now.AddYears(-1).Year;
            var month = DateTime.Now.AddMonths(-1).Month;
            return new DateTime(year, month, 1);
        }

        public static DateTime GetRecommendedStopDate()
        {
            var year = DateTime.Now.Month != 1 ? DateTime.Now.Year : DateTime.Now.AddYears(-1).Year;
            var month = DateTime.Now.AddMonths(-1).Month;
            return new DateTime(year, month, DateTime.DaysInMonth(year, month));
        }

        /// <summary>
        /// Timestamp HH.mm.ss 03.05.09
        /// </summary>
        /// <param name="date">Null for today otherwise requested time</param>
        /// <returns>HH.mm.ss</returns>
        public static string GetTimeStampMilitary()
        {
            return Shared.GetTimeStampMilitary(DateTime.Now);
        }

        /// <summary>
        /// Timestamp HH.mm.ss 03.05.09
        /// </summary>
        /// <param name="date">Null for today otherwise requested time</param>
        /// <returns>HH.mm.ss</returns>
        public static string GetTimeStampMilitary(DateTime? date)
        {
            var response = "";
            try
            {
                var dateTime = date.HasValue ? date.Value : DateTime.Now;
                response = DateTime.Now.ToString("HH.mm.ss");
            }
            catch (ArgumentOutOfRangeException)
            {
                response = "Invalid Date";
            }
            return response;
        }

        /// <summary>
        /// Safe date for database parameter
        /// </summary>
        /// <param name="date">if null or invalid, returns DBNull.Value</param>
        /// <returns>yyyy-MM-dd HH:mm:ss.fff</returns>
        public static object GetDateOrDBNull(DateTime? date)
        {
            object response;
            if (!date.HasValue)
            {
                response = DBNull.Value;
            }
            else 
            {
                try
                {
                    response = date.Value.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                catch(ArgumentOutOfRangeException)
                {
                    response = DBNull.Value;
                }
            }
            return response;
        }

        /// <summary>
        /// Datestamp yyyyMM 201903
        /// </summary>
        /// <param name="month">Requested month 1 - 12</param>
        /// <param name="year">Requested year</param>
        /// <returns>yyyyMM</returns>
        public static string GetYearMonth(int year, int month)
        {
            var response = "";
            try
            {
                response = new DateTime(year, month, 1).ToString("yyyyMM");
            }
            catch(ArgumentOutOfRangeException)
            {
                response = "Invalid Date";
            }
            return response;
        }
        
        /// <summary>
        /// Datestamp yyyy-MM-dd 2019-04-01
        /// </summary>
        /// <returns>yyyy-MM-dd</returns>
        public static string GetYearMonthDay(int year, int month, int day)
        {
            var response = "";
            try
            {
                var date = new DateTime(year, month, day);
                response = Shared.GetYearMonthDay(date);
            }
            catch (ArgumentOutOfRangeException)
            {
                response = "Invalid Date";
            }
            return response;
        }
        
        /// <summary>
        /// Datestamp yyyy-MM-dd 2019-04-01
        /// </summary>
        /// <returns>yyyy-MM-dd</returns>
        public static string GetYearMonthDay(DateTime date)
        {
            var response = "";
            try
            {
                response = date.ToString("yyyy-MM-dd");
            }
            catch (ArgumentOutOfRangeException)
            {
                response = "Invalid Date";
            }
            return response;
        }

        /// <summary>
        /// Datestamp d MMM yyyy (1 Jan, 2019)
        /// </summary>
        /// <param name="date"></param>
        /// <returns>d MMM yyyy</returns>
        public static string GetDayMonthNameYear(DateTime date)
        {
            return date.ToString("d MMM yyyy");
        }

        /// <summary>
        /// DateTime stamp d MMM yyyy h:mm tt (1 Jan, 2019 3:00PM)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string GetDayMonthNameYearTime(DateTime date)
        {
            return date.ToString("d MMM yyyy h:mmtt");
        }

        /// <summary>
        /// Datestamp today MMM-dd  (Nov-23)
        /// </summary>
        /// <returns>MMM-dd</returns>
        public static string GetMonthDay()
        {
            return Shared.GetMonthDay(DateTime.Now.Month, DateTime.Now.Day);
        }

        /// <summary>
        /// Datestamp MMM-dd  (Nov-23)
        /// </summary>
        /// <param name="month">Requested month 1 - 12</param>
        /// <param name="day">Requested day</param>
        /// <returns>MMM-dd</returns>
        public static string GetMonthDay(int month, int day)
        {            
            var response = "";
            try
            {
                response = new DateTime(1900, month, day).ToString("MMM-dd");
            }
            catch(ArgumentOutOfRangeException)
            {
                response = "Invalid Date";
            }
            return response;
        }

        /// <summary>
        /// Datetime yyyy-MM-dd h:mm tt
        /// </summary>
        /// <param name="date"></param>
        /// <returns>yyyy-MM-dd h:mm tt</returns>
        public static string GetDateTimeStampAMPM(DateTime date)
        {
            return date.ToString("yyyy-MM-dd h:mm tt");
        }

        /// <summary>
        /// Datetime "1 Hour 13 Minutes 2 Seconds". "s" is only added if value is 1.
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns>XX Hour XX Minutes XX Seconds</returns>
        public static string GetTimespanSentence(TimeSpan timespan)
        {
            var response = Shared.SingularPlural(timespan.Seconds, "Second");
            if (timespan.Minutes > 0)
            {
                response = $"{Shared.SingularPlural(timespan.Minutes, "Minute")} {response}";
            }
            if (timespan.Hours > 0)
            {
                response = $"{Shared.SingularPlural(timespan.Hours, "Hour")} {response}";
            }
            return response;
        }

        /// <summary>
        /// Timestamp 00:00:00.0000 (01:16:32.2081)
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns>00:00:00.0000</returns>
        public static string GetTimespanLong(TimeSpan timespan)
        {
            return $"{timespan.Hours.ToString("00")}:{timespan.Minutes.ToString("00")}.{timespan.Seconds.ToString("00")}.{timespan.Milliseconds.ToString("0000")}";
        }

        /// <summary>
        /// Timestamp 00:00:00 (01:16:32.2081)
        /// </summary>
        /// <param name="timespan"></param>
        /// <returns>00:00:00</returns>
        public static string GetTimespan(TimeSpan timespan)
        {
            return $"{timespan.Hours.ToString("00")}:{timespan.Minutes.ToString("00")}.{timespan.Seconds.ToString("00")}";
        }

        public static string GetFileLength(long size)
        {
            return String.Format("{0:0,0}", size) + " bytes.";
        }
    }
}

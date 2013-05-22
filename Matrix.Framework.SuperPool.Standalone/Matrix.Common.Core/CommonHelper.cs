// -----
// Copyright 2010 Deyan Timnev
// This file is part of the Matrix Platform (www.matrixplatform.com).
// The Matrix Platform is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version. The Matrix Platform is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with the Matrix Platform. If not, see http://www.gnu.org/licenses/lgpl.html
// -----
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;

namespace Matrix.Common.Core
{
    /// <summary>
    /// Contains common core helper items.
    /// A general helper class, contains all kinds of routines that help the general operation of an application.
    /// </summary>
    public class CommonHelper
    {
        public delegate void DefaultDelegate();
        public delegate object DefaultReturnDelegate();
        public delegate void GenericDelegate<TParameterOne>(TParameterOne parameter1);
        public delegate void GenericDelegate<TParameterOne, TParameterTwo>(TParameterOne parameter1, TParameterTwo parameter2);
        public delegate void GenericDelegate<TParameterOne, TParameterTwo, TParameterThree>(TParameterOne parameter1, TParameterTwo parameter2, TParameterThree parameter3);

        public delegate TReturnType GenericReturnDelegate<TReturnType>();
        public delegate TReturnType GenericReturnDelegate<TReturnType, TParameterOne>(TParameterOne parameter1);
        public delegate TReturnType GenericReturnDelegate<TReturnType, TParameterOne, TParameterTwo>(TParameterOne parameter1, TParameterTwo parameter2);


        static System.Text.ASCIIEncoding _encoding = new ASCIIEncoding();

        /// <summary>
        /// Lock when accessing. Watch out for potential dead locks, use in atomic operations only.
        /// </summary>
        static Random _random = new Random();

        static string _startupExecutingDirectory = string.Empty;

        /// <summary>
        /// Only works automatically where entry assembly is available (NA in ApplicationDomain).
        /// </summary>
        public static string StartupExecutingDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_startupExecutingDirectory))
                {
                    Assembly entryAssembly = System.Reflection.Assembly.GetEntryAssembly();

                    if (entryAssembly == null)
                    {
                        _startupExecutingDirectory = string.Empty;
                    }
                    else
                    {
                        string assemblyLaunch = entryAssembly.Location;
                        _startupExecutingDirectory = Path.GetDirectoryName(assemblyLaunch);
                    }
                }

                return _startupExecutingDirectory;
            }

            set
            {
                _startupExecutingDirectory = value;
            }
        }

        /// <summary>
        /// How many CPUs, or Cores the CPU has in total.
        /// </summary>
        public static int ProcessorCount
        {
            get
            {
                return Environment.ProcessorCount;
            }
        }

        /// <summary>
        /// An array of time zones
        /// (e.g. new string[] {"PST", "-0700", "(US) Pacific
        /// Standard"}).
        /// </summary>
        protected static string[][] TimeZones = new string[][] {
            new string[] {"ACDT", "+1030", "Australian Central Daylight"},
            new string[] {"ACST", "+0930", "Australian Central Standard"},
            new string[] {"ADT", "-0300", "(US) Atlantic Daylight"},
            new string[] {"AEDT", "+1100", "Australian East Daylight"},
            new string[] {"AEST", "+1000", "Australian East Standard"},
            new string[] {"AHDT", "-0900", ""},
            new string[] {"AHST", "-1000", ""},
            new string[] {"AST", "-0400", "(US) Atlantic Standard"},
            new string[] {"AT", "-0200", "Azores"},
            new string[] {"AWDT", "+0900", "Australian West Daylight"},
            new string[] {"AWST", "+0800", "Australian West Standard"},
            new string[] {"BAT", "+0300", "Bhagdad"},
            new string[] {"BDST", "+0200", "British Double Summer"},
            new string[] {"BET", "-1100", "Bering Standard"},
            new string[] {"BST", "-0300", "Brazil Standard"},
            new string[] {"BT", "+0300", "Baghdad"},
            new string[] {"BZT2", "-0300", "Brazil Zone 2"},
            new string[] {"CADT", "+1030", "Central Australian Daylight"},
            new string[] {"CAST", "+0930", "Central Australian Standard"},
            new string[] {"CAT", "-1000", "Central Alaska"},
            new string[] {"CCT", "+0800", "China Coast"},
            new string[] {"CDT", "-0500", "(US) Central Daylight"},
            new string[] {"CED", "+0200", "Central European Daylight"},
            new string[] {"CET", "+0100", "Central European"},
            new string[] {"CST", "-0600", "(US) Central Standard"},
            new string[] {"CENTRAL", "-0600", "(US) Central Standard"},
            new string[] {"EAST", "+1000", "Eastern Australian Standard"},
            new string[] {"EDT", "-0400", "(US) Eastern Daylight"},
            new string[] {"EED", "+0300", "Eastern European Daylight"},
            new string[] {"EET", "+0200", "Eastern Europe"},
            new string[] {"EEST", "+0300", "Eastern Europe Summer"},
            new string[] {"EST", "-0500", "(US) Eastern Standard"},
            new string[] {"EASTERN", "-0500", "(US) Eastern Standard"},
            new string[] {"FST", "+0200", "French Summer"},
            new string[] {"FWT", "+0100", "French Winter"},
            new string[] {"GMT", "-0000", "Greenwich Mean"},
            new string[] {"ETC", "-0000", "ETC"},
            new string[] {"GST", "+1000", "Guam Standard"},
            new string[] {"HDT", "-0900", "Hawaii Daylight"},
            new string[] {"HST", "-1000", "Hawaii Standard"},
            new string[] {"IDLE", "+1200", "Internation Date Line East"},
            new string[] {"IDLW", "-1200", "Internation Date Line West"},
            new string[] {"IST", "+0530", "Indian Standard"},
            new string[] {"IT", "+0330", "Iran"},
            new string[] {"JST", "+0900", "Japan Standard"},
            new string[] {"JT", "+0700", "Java"},
            new string[] {"MDT", "-0600", "(US) Mountain Daylight"},
            new string[] {"MED", "+0200", "Middle European Daylight"},
            new string[] {"MET", "+0100", "Middle European"},
            new string[] {"MEST", "+0200", "Middle European Summer"},
            new string[] {"MEWT", "+0100", "Middle European Winter"},
            new string[] {"MST", "-0700", "(US) Mountain Standard"},
            new string[] {"MOUNTAIN", "-0700", "(US) Mountain Standard"},
            new string[] {"MT", "+0800", "Moluccas"},
            new string[] {"NDT", "-0230", "Newfoundland Daylight"},
            new string[] {"NFT", "-0330", "Newfoundland"},
            new string[] {"NT", "-1100", "Nome"},
            new string[] {"NST", "+0630", "North Sumatra"},
            new string[] {"NZ", "+1100", "New Zealand "},
            new string[] {"NZST", "+1200", "New Zealand Standard"},
            new string[] {"NZDT", "+1300", "New Zealand Daylight "},
            new string[] {"NZT", "+1200", "New Zealand"},
            new string[] {"PDT", "-0700", "(US) Pacific Daylight"},
            new string[] {"PST", "-0800", "(US) Pacific Standard"},
            new string[] {"PACIFIC", "-0800", "(US) Pacific Standard"},
            new string[] {"ROK", "+0900", "Republic of Korea"},
            new string[] {"SAD", "+1000", "South Australia Daylight"},
            new string[] {"SAST", "+0900", "South Australia Standard"},
            new string[] {"SAT", "+0900", "South Australia Standard"},
            new string[] {"SDT", "+1000", "South Australia Daylight"},
            new string[] {"SST", "+0200", "Swedish Summer"},
            new string[] {"SWT", "+0100", "Swedish Winter"},
            new string[] {"USZ3", "+0400", "USSR Zone 3"},
            new string[] {"USZ4", "+0500", "USSR Zone 4"},
            new string[] {"USZ5", "+0600", "USSR Zone 5"},
            new string[] {"USZ6", "+0700", "USSR Zone 6"},
            new string[] {"UT", "-0000", "Universal Coordinated"},
            new string[] {"UTC", "-0000", "Universal Coordinated"},
            new string[] {"UZ10", "+1100", "USSR Zone 10"},
            new string[] {"WAT", "-0100", "West Africa"},
            new string[] {"WET", "-0000", "West European"},
            new string[] {"WST", "+0800", "West Australian Standard"},
            new string[] {"YDT", "-0800", "Yukon Daylight"},
            new string[] {"YST", "-0900", "Yukon Standard"},
            new string[] {"ZP4", "+0400", "USSR Zone 3"},
            new string[] {"ZP5", "+0500", "USSR Zone 4"},
            new string[] {"ZP6", "+0600", "USSR Zone 5"}
        };

        /// <summary>
        /// Is the current OS Windows.
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningOnWindows()
        {
            OperatingSystem osInfo = Environment.OSVersion;
            return osInfo.Platform == PlatformID.Win32NT || osInfo.Platform == PlatformID.Win32S
                || osInfo.Platform == PlatformID.Win32Windows;
        }

        /// <summary>
        /// Check to see if you are running on a 32b or a 64b OS.
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningOn64bOS()
        {
            return IntPtr.Size == 8;
        }

        /// <summary>
        /// This is related to comparing IComparable inheritors, that may be also null.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        public static int CompareNullable(IComparable item1, IComparable item2)
        {
            if (item1 != null && item2 == null)
            {
                return 1;
            }
            else if (item1 == null && item2 != null)
            {
                return -1;
            }
            else if (item1 == null && item2 == null)
            {
                return 0;
            }

            return item1.CompareTo(item2);
        }

        /// <summary>
        /// Replaces {DateTime} in input strin with a file name compatible date time.
        /// </summary>
        /// <param name="inputString"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ReplaceFileNameCompatibleDateTime(string inputString, DateTime dateTime)
        {
            if (inputString.Contains("{DateTime}") || inputString.Contains("{Datetime}"))
            {
                string dateTimeString = GetFileCompatibleDateTime(dateTime);

                inputString = inputString.Replace("{DateTime}", dateTimeString);
                inputString = inputString.Replace("{Datetime}", dateTimeString);
            }

            return inputString;
        }

        /// <summary>
        /// Get date time in a file name compatible fashion, so a file can be created from it.
        /// </summary>
        public static string GetFileCompatibleDateTime(DateTime dateTime)
        {
            return dateTime.Year.ToString() + '-' + dateTime.Month.ToString("00")
                + '-' + dateTime.Day.ToString("00") + "." + dateTime.Hour.ToString("00")
                + '-' + dateTime.Minute.ToString("00") + '-' + dateTime.Second.ToString("00") + '.' + dateTime.Millisecond.ToString();
        }


        #region Time and Number Helpers

        static public NumberFormatInfo UniversalNumberFormatInfo = new System.Globalization.CultureInfo("en-US", false).NumberFormat;

        static public CultureInfo InvariantFormatProvider = CultureInfo.InvariantCulture;

        static readonly DateTime Time1970 = new DateTime(1970, 1, 1);
        public static DateTime? GenerateDateTimeSecondsFrom1970(Int64? secondsFrom1970)
        {
            if (secondsFrom1970.HasValue == false)
            {
                return null;
            }
            else
            {
                return Time1970.AddSeconds(secondsFrom1970.Value);
            }
        }

        public static Int64 GenerateSecondsDateTimeFrom1970(DateTime time)
        {
            TimeSpan span = time - Time1970;
            return (long)span.TotalSeconds;
        }



        /// <summary>
        /// Convert a secure string into normal string.
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        static public string SecureStringToString(SecureString secureString)
        {
            IntPtr pointer = Marshal.SecureStringToBSTR(secureString);
            return Marshal.PtrToStringUni(pointer);
        }

        /// <summary>
        /// Convert a secure string into normal string.
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        static public SecureString StringToSecureString(string value)
        {
            SecureString result = new SecureString();
            foreach (Char c in value)
            {
                result.AppendChar(c);
            }
            return result;
        }

        /// <summary>
        /// Convert a secure string into a byte array.
        /// </summary>
        /// <param name="secureString"></param>
        /// <returns></returns>
        static public byte[] SecureStringToByte(SecureString secureString)
        {
            lock (_encoding)
            {
                return _encoding.GetBytes(SecureStringToString(secureString));
            }
        }

        /// <summary>
        /// Is this a nullable "TypeName?" type of type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// Generate an array containing the same value in all members.
        /// </summary>
        public static ItemType[] GenerateSingleValueArray<ItemType>(int lenght, ItemType value)
        {
            ItemType[] result = new ItemType[lenght];
            for (int i = 0; i < lenght; i++)
            {
                result[i] = value;
            }

            return result;
        }

        #endregion

        /// <summary>
        /// Allows to uniquely identify a method.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="omitEventMethodPrefix">Should the provided name omit the "add_" and "_remove" prefix of the method (if it has it).</param>
        /// <returns></returns>
        public static string GetMethodExtendedName(MethodInfo methodInfo, bool includeAssemblyInfo)
        {
            Type reflectedType = methodInfo.ReflectedType;
            string methodName = methodInfo.ToString();

            if (includeAssemblyInfo)
            {
                AssemblyName assemblyName = reflectedType.Module.Assembly.GetName();
                return assemblyName.Name + "[" + assemblyName.Version + "]" + reflectedType.Namespace + "." +
                    reflectedType.Name + "." + methodInfo.ToString();
            }

            return reflectedType.Namespace + "." + reflectedType.Name + "." + methodInfo.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetAssemblyGuidString(Assembly assembly)
        {
            object[] objects = assembly.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
            if (objects.Length == 0)
            {
                return String.Empty;
            }
            return ((System.Runtime.InteropServices.GuidAttribute)objects[0]).Value;
        }

        /// <summary>
        /// Obtain the name of an event method.
        /// </summary>
        public static string GetEventExtendedNameByMethod(MethodInfo methodInfo, bool includeAssemblyInfo, bool omitEventMethodPrefix)
        {
            Type reflectedType = methodInfo.ReflectedType;
            string methodName = methodInfo.ToString();

            if (methodName.Contains(" add_"))
            {
                if (omitEventMethodPrefix)
                {
                    methodName = methodName.Replace(" add_", " ");
                }
            }
            else if (methodName.Contains(" remove_"))
            {
                if (omitEventMethodPrefix)
                {
                    methodName = methodName.Replace(" remove_", " ");
                }
            }
            else
            {// Not an event method.
                return string.Empty;
            }

            if (includeAssemblyInfo)
            {
                AssemblyName assemblyName = reflectedType.Module.Assembly.GetName();
                return assemblyName.Name + "[" + assemblyName.Version + "]" + reflectedType.Namespace + "." +
                    reflectedType.Name + "." + methodName;
            }

            return reflectedType.Namespace + "." + reflectedType.Name + "." + methodName;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                return StringHelper.NoVallueString;
            }

            string message = "Exception [" + exception.GetType().Name;
            if (string.IsNullOrEmpty(exception.Message) == false)
            {
                message += ", " + exception.Message;
            }
            message += "]";

            if (exception.InnerException != null)
            {
                message += ", Inner [" + exception.InnerException.GetType().Name;
                if (string.IsNullOrEmpty(exception.InnerException.Message) == false)
                {
                    message += ", " + exception.InnerException.Message;
                }
            }
            message += "]";

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        public static string GetEventMethodExtendedName(EventInfo eventInfo, bool includeAssemblyInfo)
        {
            //Type reflectedType = eventInfo.ReflectedType;
            //string methodName = eventInfo.Name;

            // We shall use the add event method and reuse the GetEventExtendedNameByMethod() to generate the full name.
            MethodInfo addMethodInfo = eventInfo.GetAddMethod();
            return GetEventExtendedNameByMethod(addMethodInfo, includeAssemblyInfo, true);
        }

        /// <summary>
        /// Helper, converts exception details to string message.
        /// </summary>
        public static string ProcessExceptionMessage(string errorDetails, Exception exception)
        {
            if (string.IsNullOrEmpty(errorDetails) == false)
            {
                return errorDetails + ", " + CommonHelper.GetExceptionMessage(exception);
            }

            return GetExceptionMessage(exception);
        }

        /// <summary>
        /// Convert a set of parameters to string.
        /// </summary>
        public static string ParamsToString(params string[] parameters)
        {
            string result = "";
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i == 0)
                    {
                        result += parameters[i];
                    }
                    else
                    {
                        result += ", " + parameters[i];
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Inclusive min, *exclusive* max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Random(int min, int max)
        {
            lock (_random)
            {
                return _random.Next(min, max);
            }
        }

        /// <summary>
        /// Helper, generates a random decimal.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static decimal Random(decimal min, decimal max)
        {
            decimal randomValue;
            lock (_random)
            {
                randomValue = (decimal)_random.NextDouble();
            }

            decimal difference = max - min;
            return min + difference * randomValue;
        }

        /// <summary>
        /// Helper, generates a random double.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Random(double min, double max)
        {
            lock (_random)
            {
                double randomValue = _random.NextDouble();
                double difference = max - min;

                return min + difference * randomValue;
            }
        }

        /// <summary>
        /// Helper, swaps 2 objects values.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        public static void Swap<TType>(ref TType value1, ref TType value2)
        {
            TType cache = value1;
            value1 = value2;
            value2 = cache;
        }

        /// <summary>
        /// Helper, generates a random number; max is Exclusive.
        /// </summary>
        public static int Random(int max)
        {
            lock (_random)
            {
                return _random.Next(max);
            }
        }

        /// <summary>
        /// Run the given url.
        /// </summary>
        /// <param name="url"></param>
        public static void RunUrl(string url)
        {
            // Uri.EscapeDataString, Uri.EscapeUriString, HttpUtility.UrlEncode, HttpUtility.UrlPathEncode
            //string sUrl = HttpUtility.UrlDecode(url);
            System.Diagnostics.Process.Start(url);
        }

        /// <summary>
        /// Map relative path using as basis provided assembly location.
        /// </summary>
        public static string MapRelativeFilePathToAssemblyDirectory(Assembly assembly, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.Combine(Path.GetDirectoryName(assembly.Location), path);
        }

        /// <summary>
        /// Map relative path using as basis entry assembly location.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string MapRelativeFilePathToStartupDirectory(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string assemblyLaunch = StartupExecutingDirectory;
            // GetGullPath needed to convert any ../ elements.
            return Path.GetFullPath(Path.Combine(assemblyLaunch, path));
        }

        /// <summary>
        /// See SeparateCapitalLetters().
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string SplitCapitalLetters(string input)
        {
            return SeparateCapitalLetters(input);
        }

        /// <summary>
        /// Helper, will return a string that has the conjoined capital letters in the input
        /// string separated into separate words.
        /// </summary>
        public static string SeparateCapitalLetters(string input)
        {
            StringBuilder result = new StringBuilder();

            char previousChar = ' ';
            foreach (char c in input)
            {
                if (c.ToString() == c.ToString().ToUpper() && previousChar != ' ')
                {
                    result.Append(" ");
                }

                result.Append(c);
                previousChar = c;
            }

            return result.ToString();
        }

        /// <summary>
        /// Helper method, looks for a full match of a string inside a collection of other strings.
        /// </summary>
        public static bool ContainsString(string stringValue, IEnumerable<string> values)
        {
            foreach (string n in values)
            {
                if (n == stringValue)
                {
                    return true;
                }
            }
            return false;
        }

        public static string GenerateUniqueName(string initialName, string[] existingNames)
        {
            if (ContainsString(initialName, existingNames))
            {// We need to generate a new name.
                string tempName = initialName;
                for (int i = 0; i < 9999; i++)
                {
                    if (ContainsString(tempName + i.ToString(), existingNames) == false)
                    {// Ok, found unique one.
                        tempName += i.ToString();
                        break;
                    }
                }
                initialName = tempName;
            }
            return initialName;
        }

        /// <summary>
        /// Helper, converts enumerable items collection into a list of the same.
        /// </summary>
        static public List<TDataType> EnumerableToList<TDataType>(System.Collections.IEnumerable enumerable)
        {
            List<TDataType> list = new List<TDataType>();
            foreach (TDataType value in enumerable)
            {
                list.Add(value);
            }

            return list;
        }

        /// <summary>
        /// Helper, converts enumerable items collection into a list of the same.
        /// </summary>
        static public List<TDataType> EnumerableToList<TDataType>(IEnumerable<TDataType> enumerable)
        {
            List<TDataType> list = new List<TDataType>();
            foreach (TDataType value in enumerable)
            {
                list.Add(value);
            }
            return list;
        }

        /// <summary>
        /// Helper, converts enumerable items collection into an array of the same.
        /// </summary>
        //[Obsolete("Use EnumerableToList for improved speed and functionality.")] 
        static public object[] EnumerableToArray(IEnumerable enumerable)
        {
            List<object> list = new List<object>();
            foreach (object value in enumerable)
            {
                list.Add(value);
            }

            return list.ToArray();
        }

        /// <summary>
        /// Helper, converts enumerable items collection into an array of the same.
        /// </summary>
        //[Obsolete("Use EnumerableToList for improved speed and functionality.")] 
        static public TDataType[] EnumerableToArray<TDataType>(IEnumerable<TDataType> enumerable)
        {
            List<TDataType> list = new List<TDataType>();
            foreach (TDataType value in enumerable)
            {
                list.Add(value);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Helper, searches an enumerable collection for a given item; usefull for arrays.
        /// </summary>
        /// <typeparam name="TDataType"></typeparam>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool EnumerableContains<TDataType>(IEnumerable<TDataType> enumerable, TDataType item)
            where TDataType : IEquatable<TDataType>
        {
            foreach (TDataType enumerableItem in enumerable)
            {
                if (enumerableItem.Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Provides the first element in a enumerable of items.
        /// </summary>
        static public TDataType EnumerableFirst<TDataType>(IEnumerable<TDataType> enumerable)
            where TDataType : class
        {
            foreach (TDataType item in enumerable)
            {
                return item;
            }

            return null;
        }

        /// <summary>
        /// Get the first element of an enumerable, or throw exception.
        /// </summary>
        static public TDataType EnumerableFirstThrows<TDataType>(IEnumerable<TDataType> enumerable)
        {
            foreach (TDataType item in enumerable)
            {
                return item;
            }

            throw new Exception("Invalid enumeration.");
        }



        /// <summary>
        /// Helper, atomic read operation of long value.
        /// </summary>
        static public long AtomicRead(ref long value)
        {
            return Interlocked.Read(ref value);
        }

        /// <summary>
        /// Atomic read operation.
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        static public double AtomicRead(ref double value)
        {
            double result = 0;
            Interlocked.Exchange(ref result, value);
            return result;
        }

        static public float[] DoublesToFloats(double[] values)
        {
            float[] result = new float[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (float)values[i];
            }
            return result;
        }

        static public double[] FloatsToDoubles(float[] values)
        {
            double[] result = new double[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i];
            }
            return result;
        }

        /// <summary>
        /// Get the values of the combined enum value.
        /// Make sure to have a int based enum.
        /// </summary>
        static public List<TEnumType> GetCombinedEnumValues<TEnumType>(TEnumType value)
        {
            List<TEnumType> result = new List<TEnumType>();
            foreach (object enumValue in Enum.GetValues(value.GetType()))
            {
                if (((int)enumValue & (int)((object)value)) != 0)
                {
                    result.Add((TEnumType)enumValue);
                }
            }

            return result;
        }

        /// <summary>
        /// Get the name of the combined enum value.
        /// Make sure to have a int based enum.
        /// </summary>
        static public string GetCombinedEnumName(Type enumType, int value)
        {
            string result = string.Empty;
            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                if ((value & (int)values.GetValue(i)) != 0)
                {
                    if (string.IsNullOrEmpty(result) == false)
                    {
                        result += ", ";
                    }
                    result += names[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Use this helper you can, without specifying the template parameters, just pass them in,
        /// the compiler will figure it out.
        /// </summary>
        public static KeyValuePair<ItemType1, ItemType2> CreatePair<ItemType1, ItemType2>(ItemType1 item1, ItemType2 item2)
            where ItemType1 : class
            where ItemType2 : class
        {
            return new KeyValuePair<ItemType1, ItemType2>(item1, item2);
        }

        public static DefaultDelegate CreateDefaultDelegate(DefaultDelegate target)
        {
            return new DefaultDelegate(target);
        }

        /// <summary>
        /// Allows to iterate a collection of parent type items and return only the ones that are child typed.
        /// </summary>
        /// <typeparam name="TParentType"></typeparam>
        /// <param name="parentCollection"></param>
        /// <returns></returns>
        static public IEnumerable<TChildType> SafeChildTypeIteration<TParentType, TChildType>(IEnumerable<TParentType> parentCollection)
            where TParentType : class
            where TChildType : TParentType
        {
            foreach (TParentType item in parentCollection)
            {
                if (item is TChildType)
                {
                    yield return (TChildType)item;
                }
            }
        }

        static public Stack<T> CloneStack<T>(Stack<T> inputStack)
        {
            Stack<T> result = new Stack<T>();

            T[] array = inputStack.ToArray();
            for (int i = array.Length - 1; i >= 0; i--)
            {
                result.Push(array[i]);
            }
            return result;
        }

        static public bool CompareArrays<ArrayType>(ArrayType[] array1, ArrayType[] array2)
            where ArrayType : IEquatable<ArrayType>
        {
            System.Diagnostics.Debug.Assert(array1.Length == array2.Length);
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i].Equals(array2[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        static public double[] Sort(IEnumerable<double> inputArray, bool reverseOrder)
        {
            List<double> doubles = new List<double>(inputArray);
            doubles.Sort();
            if (reverseOrder)
            {
                doubles.Reverse();
            }
            return doubles.ToArray();
        }

        /// <summary>
        /// Limit a value to be inside a range.
        /// </summary>
        static public decimal LimitRange(decimal value, decimal min, decimal max)
        {
            if (value > max)
            {
                return max;
            }

            if (value < min)
            {
                return min;
            }

            return value;
        }

        /// <summary>
        /// Helper, returns the Min, but considering if there is a value or not.
        /// </summary>
        static public decimal? Min(decimal? value1, decimal? value2)
        {
            if (value1.HasValue && value2.HasValue)
            {
                return Math.Min(value1.Value, value2.Value);
            }
            else if (value1.HasValue)
            {
                return value1.Value;
            }
            else if (value2.HasValue)
            {
                return value2.Value;
            }

            return null;
        }

        /// <summary>
        /// Helper, returns the Max, but considering if there is a value or not.
        /// </summary>
        static public decimal? Max(decimal? value1, decimal? value2)
        {
            if (value1.HasValue && value2.HasValue)
            {
                return Math.Max(value1.Value, value2.Value);
            }
            else if (value1.HasValue)
            {
                return value1.Value;
            }
            else if (value2.HasValue)
            {
                return value2.Value;
            }

            return null;
        }

        static public void GetMinMax(IEnumerable<double> values, out double min, out double max)
        {
            min = double.MaxValue;
            max = double.MinValue;

            foreach (double value in values)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }
        }

        static public float[] IntsToFloats(int[] values)
        {
            float[] result = new float[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i];
            }
            return result;
        }

        static public double[] IntsToDoubles(int[] values)
        {
            double[] result = new double[values.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = values[i];
            }
            return result;
        }

        static public float[] DecimalsToFloats(IEnumerable<decimal> values, int count)
        {
            float[] result = new float[count];
            int i = 0;
            foreach (decimal value in values)
            {
                result[i] = (float)value;
                i++;
            }
            return result;
        }

        static public float[] DoublesToFloats(IEnumerable<double> values, int count)
        {
            float[] result = new float[count];
            int i = 0;
            foreach (double value in values)
            {
                result[i] = (float)value;
                i++;
            }
            return result;
        }

        /// <summary>
        /// Helper, sometimes decimal encoding is used as minValue (or maxValue) instead of null structure.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Decimal? ValueDencodeConvert(Decimal value)
        {
            if (value != Decimal.MinValue && value != decimal.MaxValue)
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// See ValueEncodeConvert.
        /// </summary>
        public static Decimal ValueEncodeConvert(Decimal? value)
        {
            if (value == null)
            {
                return decimal.MinValue;
            }
            else
            {
                return value.Value;
            }
        }

        /// <summary>
        /// Application version string, may fail if entry assembly not available.
        /// </summary>
        public static string ApplicationVersion
        {
            get
            {
                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    return entryAssembly.GetName().Version.ToString();
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Helper, will remove any symbols not allowed to be used in a file name.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RepairFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, ' ');
            }

            return input;
        }

        public static string RepairHTMLString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Replace special HTML symbols.
            input = input.Replace("&amp;", "&");
            // Remove tags.
            input = Regex.Replace(input, "<(.|\n)*?((>(.|\n)*?</(.|\n)*?>)|(/>))", "");

            return WebHelper.HtmlDecode(input);
        }

        /// <summary>
        /// This allows parsing with taking TimeZones into the string in consideration.
        /// </summary>
        public static DateTime ParseDateTimeWithZone(string dateTime)
        {
            DateTime result = DateTime.MinValue;

            try
            {

                dateTime = dateTime.Trim();

                if (string.IsNullOrEmpty(dateTime))
                {
                    return result;
                }

                if (dateTime.Contains("/"))
                {// Some date time formats contain 2 zones like "Etc/GMT" - fix that here.
                    dateTime = dateTime.Substring(0, dateTime.IndexOf("/"));
                }

                if (dateTime.Contains("."))
                {// Some date time formats contain a dot, clear it.
                    dateTime = dateTime.Replace(".", "");
                }

                if (dateTime.Contains("Thur"))
                {// Thur occurs in some feeds and is not recognized.
                    dateTime = dateTime.Replace("Thur", "");
                }

                // The dateTime parameter is either local (no time
                // zone specified), or it has a time zone.
                // Use the regex to examine the last part of the
                // dateTime string and determine which category it falls into.

                Match m = Regex.Match(dateTime.Trim(), @"(\b\w{3,4}|[+-]?\d{4})$");
                //Match m = Regex.Match(dateTime.Trim(), @"(\b\w{3,4})$");

                if (m.Value == DateTime.Now.Year.ToString() ||
                    m.Value == (DateTime.Now.Year - 1).ToString() ||
                    m.Value == (DateTime.Now.Year - 2).ToString() ||
                        m.Value == (DateTime.Now.Year - 3).ToString())
                {// Sometimes the year is passed this way and the algo confuses it with a timizing zone.
                    m = null;
                }

                if (m == null || m.Length == 0)
                {
                    //result = DateTime.Parse(dateTime);
                    if (DateTimeHelper.TryParse(dateTime, DateTimeHelper.DateTimeFormat.USA_DATE, out result) == false)
                    {
                        result = DateTime.MinValue;
                    }
                }
                else
                {
                    // Date w/ time zone. m.Value holds the time zone info
                    // (either a time zone name (e.g. PST), or the
                    // numeric offset (e.g. -0800).
                    result = ConvertToLocalDateTime(dateTime, m.Value);
                }
            }
            catch (Exception ex)
            {
                CoreSystemMonitor.OperationError("Failed to parse time.", ex);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        static public string GetShortDateTimeNoYear(DateTime? dateTime)
        {
            if (dateTime.HasValue == false)
            {
                return string.Empty;
            }
            //if (dateTime.DayOfYear == DateTime.Now.DayOfYear && dateTime.Year == DateTime.Now.Year)
            //{
            //    return dateTime.ToString("HH:mm");
            //}
            //else
            //{
            return dateTime.Value.ToString("dd MMM HH:mm");
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        static public string GetShortDateTime(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.Day.ToString("00") + "/" + dateTime.Value.Month + "/" + dateTime.Value.Year + " " + dateTime.Value.Hour.ToString("00") + ":" + dateTime.Value.Minute.ToString("00");
            }
            else
            {
                return StringHelper.NoVallueAssignedString;
            }
        }

        /// <summary>
        /// Helper.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="timeZoneId"></param>
        /// <returns></returns>
        private static DateTime ConvertToLocalDateTime(string dateTime, string timeZoneId)
        {
            // Strip the time zone ID from the end of the dateTime string.
            dateTime = dateTime.Replace(timeZoneId, "").Trim();

            // Convert the timeZoneId to a TimeSpan.
            // (Leading + signs aren't allowed in the TimeSpan.Parse
            // parameter, although leading - signs are.
            // The purpose of the [+]*? at the beginning of the
            // regex is to account for, and ignore, any leading + sign).

            string ts = Regex.Replace(GetTimeZoneOffset(timeZoneId),
            @"^[+]*?(?<hours>[-]?\d\d)(?<minutes>\d\d)$",
            "${hours}:${minutes}:00");
            TimeSpan timeZoneOffset = TimeSpan.Parse(ts);

            TimeSpan localUtcOffset =
            TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);

            // Get the absolute time difference between the given
            // datetime's time zone and the local datetime's time zone.

            TimeSpan absoluteOffset = timeZoneOffset - localUtcOffset;
            absoluteOffset = absoluteOffset.Duration();

            // Now that the absolute time difference is known,
            // determine whether to add or subtract it from the
            // given dateTime, and then return the result.

            try
            {
                if (timeZoneOffset < localUtcOffset)
                {
                    return DateTime.Parse(dateTime) + absoluteOffset;
                }
                else
                {
                    return DateTime.Parse(dateTime) - absoluteOffset;
                }
            }
            catch
            {
                CoreSystemMonitor.OperationWarning("Parsing of date time [" + dateTime + "] failed.");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Converts a time zone (e.g. "PST") to an offset string
        /// (e.g. "-0700").
        /// </summary>
        /// <param name="tz">The time zone to convert.</param>
        /// <returns>The offset value (e.g. -0700).</returns>
        private static string GetTimeZoneOffset(string tz)
        {
            tz = tz.ToUpper();
            // If the time zone is already in number format,
            // just return it.
            if (Regex.IsMatch(tz, @"^[+-]?\d{4}$"))
            {
                return tz;
            }

            string result = string.Empty;
            foreach (string[] sa in TimeZones)
            {
                if (sa[0].ToUpper() == tz)
                {
                    result = sa[1];
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Used to execute the thread pool items.
        /// </summary>
        /// <param name="state"></param>
        protected static void WaitCallbackMethod(object state)
        {
            object[] stateObjects = state as object[];
            Delegate del = ((Delegate)stateObjects[0]);
            del.Method.Invoke(del.Target, (object[])(stateObjects[1]));
        }

        /// <summary>
        /// Override of the full version of FireAndForgetDotNetPool().
        /// </summary>
        public static void FireAndForgetDotNetPool(CommonHelper.DefaultDelegate d)
        {
            FireAndForgetDotNetPool(d, null);
        }

        /// <summary>
        /// Helps to execute the delegate in a new thread, using the .NET Thread Pool.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="args"></param>
        public static void FireAndForgetDotNetPool(Delegate d, params object[] args)
        {
            ThreadPool.QueueUserWorkItem(WaitCallbackMethod, new object[] { d, args });
        }

        /// <summary>
        /// Copy folder helper.
        /// </summary>
        static public void CopyFolder(string sourceFolder, string destFolder)
        {
            throw new Exception("Not tested.");

            //if (!Directory.Exists(destFolder))
            //{
            //    Directory.CreateDirectory(destFolder);
            //}

            //string[] files = Directory.GetFiles( sourceFolder );
            //foreach (string file in files)
            //{
            //    string name = Path.GetFileName( file );
            //    string dest = Path.Combine( destFolder, name );
            //    File.Copy( file, dest );
            //}

            //string[] folders = Directory.GetDirectories( sourceFolder );
            //foreach (string folder in folders)
            //{
            //    string name = Path.GetFileName( folder );
            //    string dest = Path.Combine( destFolder, name );
            //    CopyFolder( folder, dest );
            //}
        }
    }
}

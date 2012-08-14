#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Elmah
{
    using System.Collections.Generic;

    static partial class Elnumerable
    { 
        public static string ToDelimitedString(this IEnumerable<string> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<bool> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<sbyte> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<byte> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<char> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<short> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<int> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<long> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<float> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<double> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<decimal> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<ushort> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<uint> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        public static string ToDelimitedString(this IEnumerable<ulong> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

    }
}

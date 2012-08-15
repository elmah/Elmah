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
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    #endregion

    static partial class Elnumerable
    {
        public static IEnumerable<KeyValuePair<int, T>> Index<T>(this IEnumerable<T> source)
        {
            return Index(source, 0);
        }

        public static IEnumerable<KeyValuePair<int, T>> Index<T>(this IEnumerable<T> source, int startIndex)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Select((item, index) => KeyValuePair.Create(startIndex + index, item));
        }

        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter)
        {
            return FormatDelimitedString(source, delimiter, (sb, e) => sb.Append(e));
        }

        static string FormatDelimitedString<T>(IEnumerable<T> source, string delimiter, Func<StringBuilder, T, StringBuilder> append)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.Index()
                         .Aggregate(new StringBuilder(),
                                    (sb, e) => append(sb.Append(e.Key > 0 ? delimiter : null), e.Value),
                                    sb => sb.ToString());
        }
    }
}

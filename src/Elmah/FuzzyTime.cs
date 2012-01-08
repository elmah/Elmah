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
    using System;
    using System.Globalization;

    static class FuzzyTime
    {
        public static string FormatInEnglish(DateTime time)
        {
            Debug.Assert(time.Kind != DateTimeKind.Unspecified);

            // Adapted from http://stackoverflow.com/a/1248/6682
            // License cc-wiki: http://creativecommons.org/licenses/by-sa/2.5/

            const int second = 1;
            const int minute = 60 * second;
            const int hour   = 60 * minute;
            const int day    = 24 * hour;
            const int month  = 30 * day;

            var ts = TimeSpan.FromTicks((DateTime.UtcNow.Ticks - time.ToUniversalTime().Ticks));
            var delta = Math.Abs(ts.TotalSeconds);
            var provider = DateTimeFormatInfo.InvariantInfo;

            return delta < 0
                 ? "not yet"
                 : delta < 1 * minute
                 ? (ts.Seconds == 1 ? "one second ago" : ts.Seconds.ToString(provider) + " seconds ago")
                 : delta < 2 * minute
                 ? "a minute ago"
                 : delta < 45 * minute
                 ? ts.Minutes.ToString(provider) + " minutes ago"
                 : delta < 90 * minute
                 ? "an hour ago"
                 : delta < 24 * hour
                 ? ts.Hours.ToString(provider) + " hours ago"
                 : delta < 48 * hour
                 ? "yesterday"
                 : delta < 5 * day
                 ? ts.Days.ToString(provider) + " days ago"
                 : time.ToString(delta < 6 * month ? "MMM d" : "MMM yyyy", provider);
        }
    }
}

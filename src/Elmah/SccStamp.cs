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

[assembly: Elmah.Scc("$Id: SccStamp.cs 618 2009-05-30 00:18:30Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using Mannex;

    #endregion

    /// <summary>
    /// Represents a source code control (SCC) stamp and its components.
    /// </summary>

    [ Serializable ]
    public sealed class SccStamp
    {
        private readonly string _id;
        private readonly string _author;
        private readonly string _fileName;
        private readonly int _revision;
        private readonly DateTime _lastChanged;
        private static readonly Regex _regex;

        static SccStamp()
        {
            //
            // Expression to help parse:
            //
            // STAMP := "$Id:" FILENAME REVISION DATE TIME "Z" USERNAME "$"
            // DATE  := 4-DIGIT-YEAR "-" 2-DIGIT-MONTH "-" 2-DIGIT-DAY
            // TIME  := HH ":" MM ":" SS
            //

            var escapedNonFileNameChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));

            _regex = new Regex(
                @"\$ id: \s* 
                     (?<f>[^" + escapedNonFileNameChars + @"]+) \s+         # FILENAME
                     (?<r>[0-9]+) \s+                                       # REVISION
                     ((?<y>[0-9]{4})-(?<mo>[0-9]{2})-(?<d>[0-9]{2})) \s+    # DATE
                     ((?<h>[0-9]{2})\:(?<mi>[0-9]{2})\:(?<s>[0-9]{2})Z) \s+ # TIME (UTC)
                     (?<a>\w+)                                              # AUTHOR",
                RegexOptions.CultureInvariant
                | RegexOptions.IgnoreCase
                | RegexOptions.IgnorePatternWhitespace
                | RegexOptions.Singleline
                | RegexOptions.ExplicitCapture
                | RegexOptions.Compiled);
        }

        /// <summary>
        /// Initializes an <see cref="SccStamp"/> instance given a SCC stamp 
        /// ID. The ID is expected to be in the format popularized by CVS 
        /// and SVN.
        /// </summary>

        public SccStamp(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");
            
            var match = _regex.Match(id);
            
            if (!match.Success)
                throw new ArgumentException(null, "id");

            _id = id;

            var groups = match.Groups;

            _fileName = groups["f"].Value;
            _revision = int.Parse(groups["r"].Value);
            _author = groups["a"].Value;

            var year = int.Parse(groups["y"].Value);
            var month = int.Parse(groups["mo"].Value);
            var day = int.Parse(groups["d"].Value);
            var hour = int.Parse(groups["h"].Value);
            var minute = int.Parse(groups["mi"].Value);
            var second = int.Parse(groups["s"].Value);
            
            _lastChanged = new DateTime(year, month, day, hour, minute, second).ToLocalTime();
        }

        /// <summary>
        /// Gets the original SCC stamp ID.
        /// </summary>

        public string Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the author component of the SCC stamp ID.
        /// </summary>

        public string Author
        {
            get { return _author; }
        }

        /// <summary>
        /// Gets the file name component of the SCC stamp ID.
        /// </summary>

        public string FileName
        {
            get { return _fileName; }
        }

        /// <summary>
        /// Gets the revision number component of the SCC stamp ID.
        /// </summary>

        public int Revision
        {
            get { return _revision; }
        }

        /// <summary>
        /// Gets the last modification time component of the SCC stamp ID.
        /// </summary>

        public DateTime LastChanged
        {
            get { return _lastChanged; }
        }

        /// <summary>
        /// Gets the last modification time, in coordinated universal time 
        /// (UTC), component of the SCC stamp ID in local time.
        /// </summary>

        public DateTime LastChangedUtc
        {
            get { return _lastChanged.ToUniversalTime(); }
        }

        public override string ToString()
        {
            return Id;
        }

        /// <summary>
        /// Finds and builds an array of <see cref="SccStamp"/> instances 
        /// from all the <see cref="SccAttribute"/> attributes applied to
        /// the given assembly.
        /// </summary>

        public static SccStamp[] FindAll(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            var attributes = (SccAttribute[]) Attribute.GetCustomAttributes(assembly, typeof(SccAttribute), false);
            
            if (attributes.Length == 0)
                return new SccStamp[0];
            
            var list = new List<SccStamp>(attributes.Length);

            foreach (var attribute in attributes)
            {
                var id = attribute.Id.Trim();

                if (id.Length > 0 && string.Compare("$Id" + /* IMPORTANT! */ "$", id, true, CultureInfo.InvariantCulture) != 0)
                    list.Add(new SccStamp(id));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Finds the latest SCC stamp for an assembly. The latest stamp is 
        /// the one with the highest revision number.
        /// </summary>

        public static SccStamp FindLatest(Assembly assembly)
        {
            return FindLatest(FindAll(assembly));
        }

        /// <summary>
        /// Finds the latest stamp among an array of <see cref="SccStamp"/> 
        /// objects. The latest stamp is the one with the highest revision 
        /// number.
        /// </summary>

        public static SccStamp FindLatest(SccStamp[] stamps)
        {
            if (stamps == null)
                throw new ArgumentNullException("stamps");
            
            if (stamps.Length == 0)
                return null;
            
            stamps = stamps.CloneObject();
            SortByRevision(stamps, /* descending */ true);
            return stamps[0];
        }

        /// <summary>
        /// Sorts an array of <see cref="SccStamp"/> objects by their 
        /// revision numbers in ascending order.
        /// </summary>

        public static void SortByRevision(SccStamp[] stamps)
        {
            SortByRevision(stamps, false);
        }

        /// <summary>
        /// Sorts an array of <see cref="SccStamp"/> objects by their 
        /// revision numbers in ascending or descending order.
        /// </summary>

        public static void SortByRevision(SccStamp[] stamps, bool descending)
        {
            Comparison<SccStamp> comparer = (lhs, rhs) => lhs.Revision.CompareTo(rhs.Revision);

            Array.Sort(stamps, descending 
                               ? ((lhs, rhs) => -comparer(lhs, rhs)) 
                               : comparer);
        }
    }
}

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

namespace Elmah.Tests
{
    extern alias e;
    
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using e::Elmah;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation designed to be used for
    /// unit testing. It stores copies of logged errors in memory.
    /// </summary>
    /// <remarks>
    /// This implementation is similar to <see cref="MemoryErrorLog"/> but
    /// differs in two ways: (1) the backing store is private to an instance
    /// and (2) there is no enforced upper limit on the number of the errors 
    /// that can be stored at any given time.
    /// </remarks>

    class TestErrorLog : ErrorLog
    {
        readonly EntryCollection _entries = new EntryCollection();

        static Error Clone(Error error) { return (Error) ((ICloneable) error).Clone(); }

        public override string Log(Error error)
        {
            var id = Guid.NewGuid();
            var entry = new ErrorLogEntry(this, id.ToString(), Clone(error));
            _entries.Add(entry);
            return entry.Id;
        }

        public override ErrorLogEntry GetError(string id)
        {
            var entries =
                from key in Enumerable.Repeat(id, 1)
                select _entries[key] into e
                where e != null
                select new ErrorLogEntry(this, e.Id, Clone(e.Error));
            return entries.SingleOrDefault();
        }

        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            var entries =
                from sorted in new[]
                {
                    from e in _entries
                    orderby e.Error.Time descending 
                    select e
                }
                from e in sorted.Skip(pageIndex * pageSize).Take(pageSize)
                select errorEntryList != null
                     ? new ErrorLogEntry(this, e.Id, Clone(e.Error))
                     : e;
            if (errorEntryList != null)
            {
                foreach (var e in entries = entries.ToArray())
                    errorEntryList.Add(e);
            }
            return entries.Count();
        }

        sealed class EntryCollection : KeyedCollection<string, ErrorLogEntry>
        {
            protected override string GetKeyForItem(ErrorLogEntry item)
            {
                return item.Id;
            }
        }
    }
}

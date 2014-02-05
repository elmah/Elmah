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

[assembly: Elmah.Scc("$Id: MemoryErrorLog.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using Mannex;
    using IDictionary = System.Collections.IDictionary;
    using CultureInfo = System.Globalization.CultureInfo;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses memory as its 
    /// backing store. 
    /// </summary>
    /// <remarks>
    /// All <see cref="MemoryErrorLog"/> instances will share the same memory 
    /// store that is bound to the application (not an instance of this class).
    /// </remarks>

    public sealed class MemoryErrorLog : ErrorLog
    {
        //
        // The collection that provides the actual storage for this log
        // implementation and a lock to guarantee concurrency correctness.
        //

        private static EntryCollection _entries;
        private readonly static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        //
        // IMPORTANT! The size must be the same for all instances
        // for the entires collection to be intialized correctly.
        //

        private readonly int _size;

        /// <summary>
        /// The maximum number of errors that will ever be allowed to be stored
        /// in memory.
        /// </summary>
        
        public static readonly int MaximumSize = 500;
        
        /// <summary>
        /// The maximum number of errors that will be held in memory by default 
        /// if no size is specified.
        /// </summary>
        
        public static readonly int DefaultSize = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// with a default size for maximum recordable entries.
        /// </summary>

        public MemoryErrorLog() : this(DefaultSize) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// with a specific size for maximum recordable entries.
        /// </summary>

        public MemoryErrorLog(int size)
        {
            if (size < 0 || size > MaximumSize)   
                throw new ArgumentOutOfRangeException("size", size, string.Format("Size must be between 0 and {0}.", MaximumSize));

            _size = size;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        
        public MemoryErrorLog(IDictionary config)
        {
            if (config == null)
            {
                _size = DefaultSize;
            }
            else
            {
                var sizeString = config.Find("size", string.Empty);

                if (sizeString.Length == 0)
                {
                    _size = DefaultSize;
                }
                else
                {
                    _size = Convert.ToInt32(sizeString, CultureInfo.InvariantCulture);
                    _size = Math.Max(0, Math.Min(MaximumSize, _size));
                }

                //
                // Set the application name. This implementation does not
                // and cannot provide per-app isolation.
                // Fixes: https://code.google.com/p/elmah/issues/detail?id=291
                //

                var appName = config.Find("applicationName", string.Empty);
                ApplicationName = appName;
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "In-Memory Error Log"; }
        }

        /// <summary>
        /// Logs an error to the application memory.
        /// </summary>
        /// <remarks>
        /// If the log is full then the oldest error entry is removed.
        /// </remarks>

        public override string Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Make a copy of the error to log since the source is mutable.
            // Assign a new GUID and create an entry for the error.
            //

            error = error.CloneObject();
            error.ApplicationName = ApplicationName;
            var newId = Guid.NewGuid();
            var entry = new ErrorLogEntry(this, newId.ToString(), error);

            _lock.EnterWriteLock(); 

            try
            {
                var entries = _entries ?? (_entries = new EntryCollection(_size));
                entries.Add(entry);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            return newId.ToString();
        }

        /// <summary>
        /// Returns the specified error from application memory, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            _lock.EnterReadLock();

            ErrorLogEntry entry;

            try
            {
                if (_entries == null)
                    return null;

                entry = _entries[id];
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (entry == null)
                return null;

            //
            // Return a copy that the caller can party on.
            //

            var error = entry.Error.CloneObject();
            return new ErrorLogEntry(this, entry.Id, error);
        }

        /// <summary>
        /// Returns a page of errors from the application memory in
        /// descending order of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            //
            // To minimize the time for which we hold the lock, we'll first
            // grab just references to the entries we need to return. Later,
            // we'll make copies and return those to the caller. Since Error 
            // is mutable, we don't want to return direct references to our 
            // internal versions since someone could change their state.
            //

            ErrorLogEntry[] selectedEntries = null;
            int totalCount;

            _lock.EnterReadLock();

            try
            {
                if (_entries == null)
                    return 0;

                totalCount = _entries.Count;

                var startIndex = totalCount - ((pageIndex + 1) * Math.Min(pageSize, totalCount));
                var endIndex = Math.Min(startIndex + pageSize, totalCount);
                var count = Math.Max(0, endIndex - startIndex);
                
                if (count > 0)
                {
                    selectedEntries = new ErrorLogEntry[count];

                    var sourceIndex = endIndex;
                    var targetIndex = 0;

                    while (sourceIndex > startIndex)
                        selectedEntries[targetIndex++] = _entries[--sourceIndex];
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (errorEntryList != null && selectedEntries != null)
            {
                //
                // Return copies of fetched entries. If the Error class would 
                // be immutable then this step wouldn't be necessary.
                //

                foreach (var entry in selectedEntries)
                {
                    var error = entry.Error.CloneObject();
                    errorEntryList.Add(new ErrorLogEntry(this, entry.Id, error));
                }
            }

            return totalCount;
        }

        /// <summary>
        /// Used to clear the content of the collection used by this logger. This method is ment
        /// for testing and debugging purposes only.
        /// </summary>

        internal void Reset()
        {
            _lock.EnterWriteLock();

            try
            {
                _entries = null;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        sealed class EntryCollection : KeyedCollection<string, ErrorLogEntry>
        {
            private readonly int _size;

            public EntryCollection(int size)
            {
                _size = size;
            }

            protected override string GetKeyForItem(ErrorLogEntry item)
            {
                return item.Id;
            }

            protected override void InsertItem(int index, ErrorLogEntry item)
            {
                if (Count == _size)
                {
                    RemoveAt(0);
                    index--;
                }
                base.InsertItem(index, item);
            }
        }
    }
}

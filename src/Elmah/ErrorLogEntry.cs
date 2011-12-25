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

[assembly: Elmah.Scc("$Id: ErrorLogEntry.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;

    #endregion

    /// <summary>
    /// Binds an <see cref="Error"/> instance with the <see cref="ErrorLog"/>
    /// instance from where it was served.
    /// </summary>
    
    [ Serializable ]
    public class ErrorLogEntry
    {
        private readonly string _id;
        private readonly ErrorLog _log;
        private readonly Error _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorLogEntry"/> class
        /// for a given unique error entry in an error log.
        /// </summary>
     
        public ErrorLogEntry(ErrorLog log, string id, Error error)
        {
            if (log == null)
                throw new ArgumentNullException("log");

            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");

            if (error == null)
                throw new ArgumentNullException("error");
            
            _log = log;
            _id = id;
            _error = error;
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance where this entry 
        /// originated from.
        /// </summary>
   
        public ErrorLog Log
        {
            get { return _log; }
        }
        
        /// <summary>
        /// Gets the unique identifier that identifies the error entry 
        /// in the log.
        /// </summary>
        
        public string Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the <see cref="Error"/> object held in the entry.
        /// </summary>

        public Error Error
        {
            get { return _error; }
        }
    }
}

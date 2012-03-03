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

// All code in this file requires .NET Framework 2.0 or later.

#if !NET_1_1 && !NET_1_0

[assembly: Elmah.Scc("$Id: ErrorLogDataSourceAdapter.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.Web.UI.WebControls;
    using System.Web;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// Methods of this type are designed to serve an
    /// <see cref="System.Web.UI.WebControls.ObjectDataSource" /> control
    /// and are adapted according to expected call signatures and
    /// behavior.
    /// </summary>

    public sealed class ErrorLogDataSourceAdapter
    {
        private readonly ErrorLog _log;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ErrorLogDataSourceAdapter"/> class with the default
        /// error log implementation.
        /// </summary>

        public ErrorLogDataSourceAdapter()
        {
            _log = ErrorLog.GetDefault(HttpContext.Current);
        }

        /// <summary>
        /// Use as the value for <see cref="ObjectDataSource.SelectCountMethod"/>.
        /// </summary>

        public int GetErrorCount()
        {
            return _log.GetErrors(0, 0, null);
        }

        /// <summary>
        /// Use as the value for <see cref="ObjectDataSource.SelectMethod"/>.
        /// </summary>
        /// <remarks>
        /// The parameters of this method are named after the default values
        /// for <see cref="ObjectDataSource.StartRowIndexParameterName"/> 
        /// and <see cref="ObjectDataSource.MaximumRowsParameterName"/> so
        /// that the minimum markup is needed for the object data source
        /// control.
        /// </remarks>

        public ErrorLogEntry[] GetErrors(int startRowIndex, int maximumRows)
        {
            return GetErrorsPage(startRowIndex / maximumRows, maximumRows);
        }

        private ErrorLogEntry[] GetErrorsPage(int index, int size)
        {
            List<ErrorLogEntry> list = new List<ErrorLogEntry>(size);
            _log.GetErrors(index, size, list);
            return list.ToArray();
        }
    }
}

#endif

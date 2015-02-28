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

[assembly: Elmah.Scc("$Id: ExceptionFiltering.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;

    #endregion
    
    public interface IExceptionFiltering
    {
        event ExceptionFilterEventHandler Filtering;
    }

    public delegate void ExceptionFilterEventHandler(object sender, ExceptionFilterEventArgs args);

    [ Serializable ]
    public sealed class ExceptionFilterEventArgs : EventArgs
    {
        private readonly Exception _exception;
        [ NonSerialized ] private readonly object _context;
        private bool _dismissed;

        public ExceptionFilterEventArgs(Exception e, object context)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            _exception = e;
            _context = context;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public object Context
        {
            get { return _context; }
        }

        public bool Dismissed
        {
            get { return _dismissed; }
        }
        
        public void Dismiss()
        {
            _dismissed = true;
        }
    }
}
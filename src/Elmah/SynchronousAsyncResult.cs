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

[assembly: Elmah.Scc("$Id: SynchronousAsyncResult.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Threading;

    #endregion

    internal sealed class SynchronousAsyncResult : IAsyncResult
    {
        private ManualResetEvent _waitHandle;
        private readonly string _syncMethodName;
        private readonly object _asyncState;
        private readonly object _result;
        private readonly Exception _exception;
        private bool _ended;

        public static SynchronousAsyncResult OnSuccess(string syncMethodName, object asyncState, object result)
        {
            return new SynchronousAsyncResult(syncMethodName, asyncState, result, null);
        }

        public static SynchronousAsyncResult OnFailure(string syncMethodName, object asyncState, Exception e)
        {
            Debug.Assert(e != null);

            return new SynchronousAsyncResult(syncMethodName, asyncState, null, e);
        }

        private SynchronousAsyncResult(string syncMethodName, object asyncState, object result, Exception e)
        {
            Debug.AssertStringNotEmpty(syncMethodName);

            _syncMethodName = syncMethodName;
            _asyncState = asyncState;
            _result = result;
            _exception = e;
        }

        public bool IsCompleted 
        {
            get { return true; }
        }

        public WaitHandle AsyncWaitHandle 
        {
            get
            {
                //
                // Create the async handle on-demand, assuming the caller
                // insists on having it even though CompletedSynchronously and
                // IsCompleted should make this redundant.
                //

                if (_waitHandle == null)
                    _waitHandle = new ManualResetEvent(true);
    
                return _waitHandle;
            }
        }

        public object AsyncState 
        {
            get { return _asyncState; }
        }

        public bool CompletedSynchronously 
        {
            get { return true; }
        }

        public object End()
        {
            if (_ended)
                throw new InvalidOperationException(string.Format("End{0} can only be called once for each asynchronous operation.", _syncMethodName));

            _ended = true;

            if (_exception != null)
                throw _exception;

            return _result;
        }
    }
}

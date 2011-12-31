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
    using System.Web;

    sealed class DelegatingHttpHandler : IHttpHandler
    {
        private readonly Action<HttpContextBase> _requestProcessor;

        public DelegatingHttpHandler(Action<HttpContextBase> requestProcessor)
        {
            if (requestProcessor == null) throw new ArgumentNullException("requestProcessor");
            _requestProcessor = requestProcessor;
        }

        public void ProcessRequest(HttpContext context)
        {
            _requestProcessor(new HttpContextWrapper(context));
        }

        public bool IsReusable { get { return false; } }
    }

    /*
    sealed class DelegatingAsyncHttpHandler : IHttpAsyncHandler
    {
        private readonly Func<HttpContextBase, AsyncCallback, object, DelegatedAsyncResult> _starter;

        public DelegatingAsyncHttpHandler(
            Func<HttpContextBase, AsyncCallback, object, DelegatedAsyncResult> starter)
        {
            if (starter == null) throw new ArgumentNullException("starter");
            _starter = starter;
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            return _starter(new HttpContextWrapper(context), cb, extraData);
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            ((DelegatedAsyncResult) result).End();
        }

        public void ProcessRequest(HttpContext context)
        {
            EndProcessRequest(BeginProcessRequest(context, null, null));
        }

        public bool IsReusable { get { return false; } }
    }

    sealed class DelegatedAsyncResult : IAsyncResult
    {
        private readonly IAsyncResult _result;
        private readonly Action<IAsyncResult> _ender;

        public DelegatedAsyncResult(IAsyncResult result, Action<IAsyncResult> ender)
        {
            _result = result;
            _ender = ender;
        }

        public void End()
        {
            _ender(_result);
        }

        public bool IsCompleted { get { return _result.IsCompleted; } }
        public WaitHandle AsyncWaitHandle { get { return _result.AsyncWaitHandle; } }
        public object AsyncState { get { return _result.AsyncState; } }
        public bool CompletedSynchronously { get { return _result.CompletedSynchronously; } }
    }*/
}

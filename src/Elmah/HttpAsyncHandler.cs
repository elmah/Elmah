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
    using System.Web;

    #endregion

    abstract class HttpAsyncHandler : IHttpAsyncHandler
    {
        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            OnProcessRequest(new HttpContextWrapper(context));
        }

        protected virtual void OnProcessRequest(HttpContextBase context)
        {
            if (context == null) throw new ArgumentNullException("context");
            EndProcessRequest(BeginProcessRequest(context, null, null));
        }

        bool IHttpHandler.IsReusable { get { return false; } }

        IAsyncResult IHttpAsyncHandler.BeginProcessRequest(HttpContext context, 
            AsyncCallback cb, object extraData)
        {
            return BeginProcessRequest(new HttpContextWrapper(context), cb, extraData);
        }

        public IAsyncResult BeginProcessRequest(HttpContextBase context, 
            AsyncCallback cb, object extraData)
        {
            if (context == null) throw new ArgumentNullException("context");
            
            return Apm.Begin(cbf => ProcessRequest(context, cbf), 
                             cb, extraData, 
                             this, "ProcessRequest");
        }

        public virtual void EndProcessRequest(IAsyncResult result)
        {
            AsyncResult.End(result, this, "ProcessRequest");
        }

        protected abstract IEnumerable<IAsyncResult> ProcessRequest(
            HttpContextBase context, Func<AsyncCallback> getAsyncCallback);
    }
}

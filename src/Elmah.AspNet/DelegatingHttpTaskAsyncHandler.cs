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

#if !NET_3_5 && !NET_4_0

namespace Elmah
{
    #region Imports

    using System;
    using System.Threading.Tasks;
    using System.Web;

    #endregion

    sealed class DelegatingHttpTaskAsyncHandler : HttpTaskAsyncHandler
    {
        readonly Func<HttpContextBase, Task> _delegatee;

        public DelegatingHttpTaskAsyncHandler(Func<HttpContextBase, Task> delegatee)
        {
            if (delegatee == null) throw new ArgumentNullException("delegatee");
            _delegatee = delegatee;
        }

        public override void ProcessRequest(HttpContext context)
        {
            // Because the base implementation throws NotSupportedException 
            // and there's no seemingly good reason not to support it.
            ProcessRequestAsync(context).Wait();
        }

        public override Task ProcessRequestAsync(HttpContext context)
        {
            return _delegatee(new HttpContextWrapper(context));
        }
    }
}

#endif
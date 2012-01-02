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

    static class Apm
    {
        //
        // Provides boilerplate implementation for implementing asnychronous 
        // BeginXXXX and EndXXXX methods over a synchronous implementation.
        //

        public static AsyncResult<T> BeginSync<T>(AsyncCallback asyncCallback, object asyncState, object owner, string operationId, Func<T> syncFunc)
        {
            Debug.Assert(!string.IsNullOrEmpty(operationId));
            Debug.Assert(syncFunc != null);

            var asyncResult = new AsyncResult<T>(asyncCallback, asyncState, owner, operationId);

            try
            {
                asyncResult.SetResult(syncFunc());
                asyncResult.Complete(null, true);
            }
            catch (Exception e)
            {
                asyncResult.Complete(e, true);
            }

            return asyncResult;
        }
    }
}
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

    #endregion

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

        public static AsyncResult Begin(
            Func<Func<AsyncCallback>, IEnumerable<IAsyncResult>> starter, 
            AsyncCallback asyncCallback, object state,
            object owner, string operationId)
        {
            if (starter == null) throw new ArgumentNullException("starter");

            var result = new AsyncResult(asyncCallback, state, owner, operationId);

            IEnumerator<IAsyncResult> are = null;
            AsyncCallback quantum = null;
            IAsyncResult nar = null;
            var busy = false;
            var popped = new bool[1];

            quantum = ar => // ReSharper disable AccessToModifiedClosure
            {
                Debug.Assert(are != null);
                Debug.Assert(quantum != null);

                if (are.Current != null && ar == null)
                    throw new ArgumentNullException("ar");

                if (busy)
                {
                    // Debug.Assert(ar.CompletedSynchronously);
                    Debug.Assert(nar == null);
                    nar = ar;
                }
                else
                {
                    if (!ReferenceEquals(ar, are.Current))
                        throw new ArgumentException(null, "ar");

                    do
                    {
                        bool done;
                        popped[0] = false;
                        busy = true;
                        try
                        {
                            done = !are.MoveNext();
                        }
                        catch (Exception e)
                        {
                            try { are.Dispose(); } // ReSharper disable EmptyGeneralCatchClause                        
                            catch { }              // ReSharper restore EmptyGeneralCatchClause
                            result.Complete(e);
                            return;
                        }
                        finally
                        {
                            busy = false;
                        }

                        if (done)
                        {
                            result.Complete();
                            ar = null;
                        }
                        else
                        {
                            if (!popped[0])
                                throw new InvalidOperationException();
                            ar = nar;
                            nar = null;
                        }
                    }
                    while (ar != null);
                }
            };
            // ReSharper restore AccessToModifiedClosure

            var cbf = new Func<AsyncCallback>(() =>
            {
                if (popped[0]) throw new InvalidOperationException();
                popped[0] = true;
                return quantum;
            });

            try
            {
                are = starter(cbf).GetEnumerator();
            }
            catch (Exception e)
            {
                result.Complete(e, true);
                return result;
            }

            quantum(null);

            return result;
        }
    }
    /*
    class AsyncContext
    {
        public Func<AsyncCallback> GetAsyncCallback { get; private set; }

        public AsyncContext(Func<AsyncCallback> getAsyncCallback)
        {
            if (getAsyncCallback == null) throw new ArgumentNullException("getAsyncCallback");
            GetAsyncCallback = getAsyncCallback;
        }
    }
    */
}
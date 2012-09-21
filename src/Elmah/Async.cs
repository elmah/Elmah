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

#if !NET_3_5

namespace Elmah
{
    #region Imports

    using System;
    using System.Threading.Tasks;

    #endregion

    static class Async
    {
        public static Task<TResult> RunSynchronously<TResult>(Func<TResult> sync)
        {
            Debug.Assert(sync != null);
            var result = default(TResult);
            var exception = (Exception)null;
            try { result = sync(); }
            catch (Exception e) { exception = e; }
            return TaskFromResultOrError(result, exception);
        }

        public static Task<TResult> RunSynchronously<T, TResult>(Func<T, TResult> sync, T arg)
        {
            Debug.Assert(sync != null);
            var result = default(TResult);
            var exception = (Exception)null;
            try { result = sync(arg); }
            catch (Exception e) { exception = e; }
            return TaskFromResultOrError(result, exception);
        }

        public static Task<TResult> RunSynchronously<T1, T2, TResult>(Func<T1, T2, TResult> sync, T1 arg1, T2 arg2)
        {
            Debug.Assert(sync != null);
            var result = default(TResult);
            var exception = (Exception)null;
            try { result = sync(arg1, arg2); }
            catch (Exception e) { exception = e; }
            return TaskFromResultOrError(result, exception);
        }

        public static Task<TResult> RunSynchronously<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> sync, T1 arg1, T2 arg2, T3 arg3)
        {
            Debug.Assert(sync != null);
            var result = default(TResult);
            var exception = (Exception)null;
            try { result = sync(arg1, arg2, arg3); }
            catch (Exception e) { exception = e; }
            return TaskFromResultOrError(result, exception);
        }

        /// <summary>
        /// Creates a task that has already completed with either the
        /// given result or faulted with the given exception.
        /// </summary>
        /// <remarks>
        /// If <paramref name="exception"/> is supplied then 
        /// <paramref name="result"/> is ignored.
        /// </remarks>

        static Task<T> TaskFromResultOrError<T>(T result, Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            if (exception == null)
                tcs.SetResult(result);
            else
                tcs.SetException(exception);
            return tcs.Task;
        }
    }
}

#endif

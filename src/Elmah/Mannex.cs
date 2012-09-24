#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
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

namespace Mannex
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.Globalization;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="int"/>.
    /// </summary>

    static partial class Int32Extensions
    {
        /// <summary>
        /// Converts <see cref="int"/> to its string representation in the
        /// invariant culture.
        /// </summary>

        [DebuggerStepThrough]
        public static string ToInvariantString(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Calculates the quotient and remainder from dividing two numbers 
        /// and returns a user-defined result.
        /// </summary>

        [DebuggerStepThrough]
        public static T DivRem<T>(this int dividend, int divisor, Func<int, int, T> resultFunc)
        {
            if (resultFunc == null) throw new ArgumentNullException("resultFunc");
            var quotient = dividend / divisor;
            var remainder = dividend % divisor;
            return resultFunc(quotient, remainder);
        }
    }
}

namespace Mannex.Collections.Generic
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>

    static partial class DictionaryExtensions
    {
        /// <summary>
        /// Finds the value for a key, returning the default value for 
        /// <typeparamref name="TKey"/> if the key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            return Find(dict, key, default(TValue));
        }

        /// <summary>
        /// Finds the value for a key, returning a given default value for 
        /// <typeparamref name="TKey"/> if the key is not present.
        /// </summary>

        [DebuggerStepThrough]
        public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default)
        {
            if (dict == null) throw new ArgumentNullException("dict");
            TValue value;
            return dict.TryGetValue(key, out value) ? value : @default;
        }
    }
}

namespace Mannex
{
    using System;

    /// <summary>
    /// Extension methods for <see cref="ICloneable"/> objects.
    /// </summary>

    static partial class ICloneableExtensions
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>

        public static T CloneObject<T>(this T source) where T : class, ICloneable
        {
            if (source == null) throw new ArgumentNullException("source");
            return (T)source.Clone();
        }
    }
}

#if !NET_3_5

namespace Mannex.Threading.Tasks
{
    #region Imports

    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>

    static partial class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Attempts to conclude <see cref="TaskCompletionSource{TResult}"/>
        /// as being canceled, faulted or having completed successfully
        /// based on the corresponding status of the given 
        /// <see cref="Task{T}"/>.
        /// </summary>

        public static bool TryConcludeFrom<T>(this TaskCompletionSource<T> source, Task<T> task)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (task == null) throw new ArgumentNullException("task");

            if (task.IsCanceled)
            {
                source.TrySetCanceled();
            }
            else if (task.IsFaulted)
            {
                var aggregate = task.Exception;
                Debug.Assert(aggregate != null);
                source.TrySetException(aggregate.InnerExceptions);
            }
            else if (TaskStatus.RanToCompletion == task.Status)
            {
                source.TrySetResult(task.Result);
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}

namespace Mannex.Threading.Tasks
{
    #region Imports

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="Task"/>.
    /// </summary>

    static partial class TaskExtensions
    {
        /// <summary>
        /// Returns a <see cref="Task{T}"/> that can be used as the
        /// <see cref="IAsyncResult"/> return value from the method
        /// that begin the operation of an API following the 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms228963.aspx">Asynchronous Programming Model</a>.
        /// If an <see cref="AsyncCallback"/> is supplied, it is invoked
        /// when the supplied task concludes (fails, cancels or completes
        /// successfully).
        /// </summary>

        public static Task<T> Apmize<T>(this Task<T> task, AsyncCallback callback, object state)
        {
            return Apmize(task, callback, state, null);
        }

        /// <summary>
        /// Returns a <see cref="Task{T}"/> that can be used as the
        /// <see cref="IAsyncResult"/> return value from the method
        /// that begin the operation of an API following the 
        /// <a href="http://msdn.microsoft.com/en-us/library/ms228963.aspx">Asynchronous Programming Model</a>.
        /// If an <see cref="AsyncCallback"/> is supplied, it is invoked
        /// when the supplied task concludes (fails, cancels or completes
        /// successfully).
        /// </summary>

        public static Task<T> Apmize<T>(this Task<T> task, AsyncCallback callback, object state, TaskScheduler scheduler)
        {
            var result = task;

            TaskCompletionSource<T> tcs = null;
            if (task.AsyncState != state)
            {
                tcs = new TaskCompletionSource<T>(state);
                result = tcs.Task;
            }

            Task t = task;
            if (tcs != null)
            {
                t = t.ContinueWith(delegate { tcs.TryConcludeFrom(task); }, 
                                   CancellationToken.None,
                                   TaskContinuationOptions.ExecuteSynchronously,
                                   TaskScheduler.Default);
            }
            if (callback != null)
            {
                // ReSharper disable RedundantAssignment
                t = t.ContinueWith(delegate { callback(result); }, // ReSharper restore RedundantAssignment
                                   CancellationToken.None,
                                   TaskContinuationOptions.None,
                                   scheduler ?? TaskScheduler.Default);
            }
            
            return result;
        }
    }
}

#endif // !NET_3_5


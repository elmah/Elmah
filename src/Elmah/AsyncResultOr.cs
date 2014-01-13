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

    struct AsyncResultOr<T>
    {
        public bool HasValue { get { return AsyncResult == null; } }
        public T Value { get; set; }
        public IAsyncResult AsyncResult { get; private set; }

        private AsyncResultOr(T value) : this() { Value = value; }
        private AsyncResultOr(IAsyncResult value) : this() { AsyncResult = value; }

        public static AsyncResultOr<T> Async(IAsyncResult ar)
        {
            if (ar == null) throw new ArgumentNullException("ar");
            return new AsyncResultOr<T>(ar);
        }

        public static AsyncResultOr<T> Result(T value)
        {
            return new AsyncResultOr<T>(value);
        }
    }

    static class AsyncResultOr
    {
        public static AsyncResultOr<T> Value<T>(T value)
        {
            return AsyncResultOr<T>.Result(value);
        }

        public static AsyncResultOr<T> InsteadOf<T>(this IAsyncResult ar)
        {
            return AsyncResultOr<T>.Async(ar);
        }
    }
}

namespace Elmah
{
    using System;
    using System.Threading.Tasks;

    struct TaskOr<T>
    {
        public bool HasValue { get { return Task == null; } }
        public T Value       { get; set; }
        public Task Task     { get; private set; }

        private TaskOr(T value)    : this() { Value = value; }
        private TaskOr(Task value) : this() { Task = value;  }

        public static TaskOr<T> Async(Task task)
        {
            if (task == null) throw new ArgumentNullException("task");
            return new TaskOr<T>(task);
        }

        public static TaskOr<T> Result(T value) { return new TaskOr<T>(value); }
    }

    static class TaskOr
    {
        public static TaskOr<T> Value<T>(T value)          { return TaskOr<T>.Result(value); }
        public static TaskOr<T> InsteadOf<T>(this Task ar) { return TaskOr<T>.Async(ar);     }
    }
}
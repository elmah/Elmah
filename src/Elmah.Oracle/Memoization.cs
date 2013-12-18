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
    using System.Threading;

    #endregion

    sealed class Memoization
    {
        public static Func<TInput, TOutput> MemoizeLast<TInput, TOutput>(Func<TInput, TOutput> function)
        {
            return MemoizeLast(function, null);
        }

        public static Func<TInput, TOutput> MemoizeLast<TInput, TOutput>(Func<TInput, TOutput> function, IEqualityComparer<TInput> inputComparer)
        {
            return MemoizeLastImpl(function, inputComparer ?? EqualityComparer<TInput>.Default);
        }

        static Func<TInput, TOutput> MemoizeLastImpl<TInput, TOutput>(Func<TInput, TOutput> function, IEqualityComparer<TInput> inputComparer)
        {
            if (function == null) throw new ArgumentNullException("function");
            Debug.Assert(inputComparer != null);

            Tuple<TInput, TOutput> entry = null;

            return input =>
            {
                Tuple<TInput, TOutput> current, result;

                do
                {
                    current = entry;
                    if (current != null && inputComparer.Equals(current.Item1, input))
                    {
                        result = current;
                        break;
                    }
                    result = new Tuple<TInput, TOutput>(input, function(input));
                }
                while (current != Interlocked.CompareExchange(ref entry, result, current));

                return result.Item2;
            };
        }

        sealed class Tuple<T1, T2>
        {
            public T1 Item1 { get; private set; }
            public T2 Item2 { get; private set; }

            public Tuple(T1 item1, T2 item2)
            {
                Item1 = item1;
                Item2 = item2;
            }
        }
    }
}

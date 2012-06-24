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
    using System.Linq;
    using System.Threading;

    #endregion

    public class Message<TInput, TOutput>
    {
        sealed class State
        {
            public Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> Binder { get; private set; }
            public Func<object, TInput, TOutput> Handler { get; private set; }

            public State(Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> binder, Func<object, TInput, TOutput> handler)
            {
                Binder = binder;
                Handler = handler;
            }
        }

        private State _state = new State(null, null);

        bool TryUpdateState(State replacement, State current)
        {
            return current == Interlocked.CompareExchange(ref _state, replacement, current);
        }

        public IDisposable PushHandler(Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> binder)
        {
            if (binder == null) throw new ArgumentNullException("binder");

            for (var updated = false; !updated; )
            {
                var current = _state;
                var state = new State(current.Binder + binder, null);
                updated = TryUpdateState(state, current);
            }
            return new DelegatingDisposable(() => RemoveHandler(binder));
        }

        void RemoveHandler(Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> binder)
        {
            Debug.Assert(binder != null);

            for (var updated = false; !updated; )
            {
                var current = _state;
                var state = new State(current.Binder - binder, null);
                updated = TryUpdateState(state, current);
            }
        }

        public TOutput Send(TInput input)
        {
            return Send(null, input);
        }

        public virtual TOutput Send(object sender, TInput input)
        {
            Func<object, TInput, TOutput> handler = null;
            for (var updated = false; !updated; )
            {
                var state = _state;
                handler = state.Handler;
                if (handler != null)
                    break;
                var binder = _state.Binder;
                if (binder == null)
                    return default(TOutput);
                var delegates = binder.GetInvocationList();
                var binders =
                    from Func<Func<object, TInput, TOutput>, Func<object, TInput, TOutput>> d
                        in delegates
                    select d;
                handler = binders.Aggregate((Func<object, TInput, TOutput>) delegate { return default(TOutput); }, (next, b) => b(next));
                updated = TryUpdateState(new State(state.Binder, handler), state);
            }

            return handler(sender, input);
        }
    }
}
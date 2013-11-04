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

namespace Elmah.Tests
{
    extern alias e;

    #region Imports

    using System;
    using System.Collections.Generic;
    using Xunit;
    using e::Elmah;
    
    #endregion

    public class MessageTests
    {
        [Fact]
        public void AddHandlerThrowsWithNullBinder()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new Message<object, object>().PushHandler(null));
            Assert.Equal("binder", e.ParamName);
        }

        [Fact]
        public void SendWithoutHandlersReturnsDefault()
        {
            var msg = new Message<object, object>();
            Assert.Null(msg.Send(new object()));
        }

        [Fact]
        public void SendWithSingleHandler()
        {
            var msg = new Message<object, Capture<object>>();
            msg.PushHandler(next => (sender, input) => new Capture<object>(sender, input));
            var arg = new object();
            var capture = msg.Send(this, arg);
            Assert.NotNull(capture);
            Assert.Equal(this, capture.Sender);
            Assert.Equal(arg, capture.Input);
        }

        [Fact]
        public void SendDoesNotCallDisposedHandler()
        {
            var msg = new Message<object, object>();
            var called = false;
            var registration = msg.PushHandler(next => (sender, input) => { called = true; return null; });
            Assert.NotNull(registration);
            registration.Dispose();
            msg.Send(this, new object());
            Assert.False(called);
        }

        [Fact]
        public void SendCallsHandlersInOrder()
        {
            var msg = new Message<object, object>();
            var queue = new Queue<object>();
            var registration = msg.PushHandler(next => (sender, input) => { queue.Enqueue(1); return next(sender, input); });
            Assert.NotNull(registration);
            msg.Send(this, new object());
            Assert.Equal(1, queue.Count);
            Assert.Equal(1, queue.Dequeue());
            msg.PushHandler(next => (sender, input) => { queue.Enqueue(2); return next(sender, input); });
            msg.Send(this, new object());
            Assert.Equal(2, queue.Count);
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(1, queue.Dequeue());
            registration.Dispose();
            msg.Send(this, new object());
            Assert.Equal(1, queue.Count);
            Assert.Equal(2, queue.Dequeue());
        }

        class Capture<T>
        {
            public object Sender { get; private set; }
            public T Input { get; private set; }

            public Capture(object sender, T input)
            {
                Sender = sender;
                Input = input;
            }
        }
    }
}

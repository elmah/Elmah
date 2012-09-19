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
    #region Imports

    using System;
    using System.IO;
    using System.Web;
    using Moq;
    using Xunit;

    #endregion

    public class ErrorSignalTests
    {
        [Fact]
        public void RaiseFiresRaised()
        {
            var subjects = CreateSubjects((sig, ctx) => new { Signal = sig, Context = ctx });
            var signal = subjects.Signal;
            var context = subjects.Context;
            ErrorSignalEventArgs args = null;
            signal.Raised += (_, ea) => args = ea;
            var callerInfo = new CallerInfo("foobar", "baz.cs", 42);
            var exception = new Exception();

            signal.Raise(exception, context, callerInfo);

            Assert.NotNull(args);
            Assert.Same(exception, args.Exception);
            Assert.Same(context, args.Context);
            Assert.Same(callerInfo, args.CallerInfo);
        }

        [Fact]
        public void RaiseWithNullCallerInfoFiresRaisedWithEmptyCallerInfo()
        {
            var subjects = CreateSubjects((sig, ctx) => new { Signal = sig, Context = ctx });
            ErrorSignalEventArgs args = null;
            subjects.Signal.Raised += (_, ea) => args = ea;

            subjects.Signal.Raise(new Exception(), subjects.Context, null);

            Assert.NotNull(args.CallerInfo);
            Assert.True(args.CallerInfo.IsEmpty);
        }

        static T CreateSubjects<T>(Func<ErrorSignal, HttpContextBase, T> resultor)
        {
            var mocks = new
            {
                Context = new Mock<HttpContextBase>
                {
                    DefaultValue = DefaultValue.Mock
                }
            };
            using (var app = new HttpApplication())
            {
                mocks.Context.Setup(c => c.ApplicationInstance).Returns(app);
                var context = mocks.Context.Object;
                return resultor(ErrorSignal.FromContext(context), context);
            }
        }
    }
}
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
    using Xunit;

    #endregion

    public class DelegatingDisposableTests
    {
        [Fact]
        public void InitializationThrowsWithNullAction()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new DelegatingDisposable(null));
            Assert.Equal("disposer", e.ParamName);
        }

        [Fact]
        public void DisposeCallsAction()
        {
            var called = false;
            var disposable = new DelegatingDisposable(delegate { called = true; });
            disposable.Dispose();
            Assert.True(called);
        }

        [Fact]
        public void DisposeCallsActionFirstTimeOnly()
        {
            var calls = 0;
            var disposable = new DelegatingDisposable(delegate { calls++; });
            disposable.Dispose();
            Assert.Equal(1, calls);
            disposable.Dispose();
            Assert.Equal(1, calls);
        }
    }
}
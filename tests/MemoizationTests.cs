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

    public class MemoizationTests
    {
        [Fact]
        public void MemoizeLastThrowsWithNullFunction()
        {
            var e = Assert.Throws<ArgumentNullException>(() => Memoization.MemoizeLast<object, object>(null));
            Assert.Equal("function", e.ParamName);
        }

        [Fact]
        public void MemoizeLastWithNullInputComparer()
        {
            var f = Memoization.MemoizeLast<object, object>(_ => null, null);
            Assert.Null(f(new object()));
        }

        [Fact]
        public void MemoizeLast()
        {
            var calls = 0;
            var f = Memoization.MemoizeLast(delegate(int i) { calls++; return i * 2; });
            Assert.Equal(4, f(2));
            Assert.Equal(1, calls);
            Assert.Equal(4, f(2));
            Assert.Equal(1, calls);
            Assert.Equal(8, f(4));
            Assert.Equal(2, calls);
        }

        [Fact]
        public void MemoizeLastWithCustomComparer()
        {
            var calls = 0;
            var comparer = StringComparer.OrdinalIgnoreCase;
            var f = Memoization.MemoizeLast(delegate(string s) { calls++; return s.ToUpperInvariant(); }, comparer);
            Assert.Equal("FOOBAR", f("foobar"));
            Assert.Equal(1, calls);
            Assert.Equal("FOOBAR", f("FOOBAR"));
            Assert.Equal(1, calls);
            Assert.Equal("FOOBAR", f("FoObAr"));
            Assert.Equal(1, calls);
        }
    }
}

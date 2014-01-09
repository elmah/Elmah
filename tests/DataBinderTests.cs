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
    #region

    using Elmah;
    using System.Collections.Generic;
    using Xunit;

    #endregion

    public class DataBinderTests
    {
        [Fact]
        public void EvalWithNullExpressionReturnsContainer()
        {
            var container = new object();
            Assert.Same(container, DataBinder.Eval(container, null));
        }

        [Fact]
        public void EvalWithBlankExpressionReturnsContainer()
        {
            var container = new object();
            Assert.Same(container, DataBinder.Eval(container, string.Empty));
            Assert.Same(container, DataBinder.Eval(container, new string((char) 32, 10)));
        }

        [Fact]
        public void Eval()
        {
            var quux = new object[]
            {
                1, 2, 
                new[] { 3, 4 }, 
                new Dictionary<string, object>
                {
                    { "one",        1     },
                    { "two",        2     },
                    { "'three'",    3     },
                    { "\"four\"",   4     },
                    { "5",          5     },
                    { "six",        "SIX" },
                }
            };
            var bar = new { Baz = quux };
            var foo = new { Bar = bar };
            var container = new { Foo = foo, Foo2 = foo, Foo_3 = foo, _Foo4 = foo };

            Assert.Equal(foo,   DataBinder.Eval(container, "foo"));
            Assert.Equal(foo,   DataBinder.Eval(container, "foo2"));
            Assert.Equal(foo,   DataBinder.Eval(container, "foo_3"));
            Assert.Equal(foo,   DataBinder.Eval(container, "_foo4"));
            Assert.Equal(bar,   DataBinder.Eval(container, "foo.bar"));
            Assert.Equal(quux,  DataBinder.Eval(container, "foo.bar.baz"));
            Assert.Equal(1,     DataBinder.Eval(container, "foo.bar.baz[0]"));
            Assert.Equal(2,     DataBinder.Eval(container, "foo.bar.baz[1]"));
            Assert.Equal(3,     DataBinder.Eval(container, "foo.bar.baz[2][0]"));
            Assert.Equal(4,     DataBinder.Eval(container, "foo.bar.baz[2][1]"));
            Assert.Equal(1,     DataBinder.Eval(container, "foo.bar.baz.[0]"));
            Assert.Equal(2,     DataBinder.Eval(container, "foo.bar.baz.[1]"));
            Assert.Equal(3,     DataBinder.Eval(container, "foo.bar.baz.[2].[0]"));
            Assert.Equal(4,     DataBinder.Eval(container, "foo.bar.baz.[2].[1]"));
            Assert.Equal(1,     DataBinder.Eval(container, "foo.bar.baz.[3].['one']"));
            Assert.Equal(2,     DataBinder.Eval(container, "foo.bar.baz.[3].['two']"));
            Assert.Equal(3,     DataBinder.Eval(container, "foo.bar.baz.[3].[\"'three'\"]"));
            Assert.Equal(4,     DataBinder.Eval(container, "foo.bar.baz.[3].['\"four\"']"));
            Assert.Equal(5,     DataBinder.Eval(container, "foo.bar.baz.[3].['5']"));
            Assert.Equal(5,     DataBinder.Eval(container, "foo.bar.baz.(3).['5']"));
            Assert.Equal(5,     DataBinder.Eval(container, "foo.bar.baz.[3].('5')"));
            Assert.Equal(5,     DataBinder.Eval(container, "foo.bar.baz[3]('5')"));
            Assert.Equal(3,     DataBinder.Eval(container, "foo.bar.baz.[3].('six').Length"));
            Assert.Equal('I',   DataBinder.Eval(container, "foo.bar.baz.[3].('six')[1]"));
        }

        [Fact]
        public void EvalIsNullSafe()
        {
            Assert.Null(DataBinder.Eval(null, "foo"));
            Assert.Null(DataBinder.Eval(null, "[2]"));
            Assert.Null(DataBinder.Eval(new { Foo = (object) null }, "foo.bar"));
            Assert.Null(DataBinder.Eval(new { Foo = new object[1] }, "foo.[0].bar"));
            Assert.Null(DataBinder.Eval(new { Foo = new object[1] }, "foo(0)bar"));
        }
    }
}
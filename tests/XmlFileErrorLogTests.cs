#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Scott Wilson <sw@scratchstudio.net>
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
    using System.Collections;
    using Xunit;

    #endregion

    public class XmlFileErrorLogTests
    {
        [Fact]
        public void InitializationThrowsWithNullConfig()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new XmlFileErrorLog((IDictionary) null));
            Assert.Equal("config", e.ParamName);
        }

        [Fact]
        public void InitializationThrowsWithNullLogPath()
        {
            var e = Assert.Throws<ArgumentNullException>(() => new XmlFileErrorLog((string)null));
            Assert.Equal("logPath", e.ParamName);
        }

        [Fact]
        public void InitializationThrowsWithEmptyLogPath()
        {
            var e = Assert.Throws<ArgumentException>(() => new XmlFileErrorLog(string.Empty));
            Assert.Equal("logPath", e.ParamName);
        }
    }
}

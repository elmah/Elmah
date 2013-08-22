#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Thomas Ardal, http://www.thomasardal.com
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
    using System.Collections.Generic;
    using Xunit;

    #endregion

    public class MemoryErrorLogTests
    {
        [Fact]
        public void CanPageMultipleErrors()
        {
            var errorLog = new MemoryErrorLog(4);
            var today = DateTime.Today;
            for (var i = 3; i >= 0; i--)
            {
                errorLog.Log(new Error { Time = today.AddDays(-i) });
            }

            var page1 = new List<ErrorLogEntry>();
            errorLog.GetErrors(0, 2, page1);
            var page2 = new List<ErrorLogEntry>();
            errorLog.GetErrors(1, 2, page2);

            Assert.Equal(2, page1.Count);
            Assert.Equal(today, page1[0].Error.Time);
            Assert.Equal(today.AddDays(-1), page1[1].Error.Time);
            Assert.Equal(2, page2.Count);
            Assert.Equal(today.AddDays(-2), page2[0].Error.Time);
            Assert.Equal(today.AddDays(-3), page2[1].Error.Time);
        }
    }
}

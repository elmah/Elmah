#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
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
    extern alias o;

    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using Xunit;
    using OracleErrorLog = o::Elmah.OracleErrorLog;
    using DbProviderFactoryQuery = o::Elmah.DbProviderFactoryQuery;

    public class OracleErrorLogTests
    {
        [Fact]
        public void NameReflectsDbProviderName()
        {
            var dbProvider = new TestDbProviderFactory();
            var log = new OracleErrorLog("...", dbProvider);
            Assert.Equal("Oracle Error Log (Elmah.Tests)", log.Name);
        }

        [Fact]
        public void InitializationDefaultsToOracleThenMicrosoftProvider()
        {
            var lookups = new Queue<string>(2);
            using (DbProviderFactoryQuery.PushGetFactoryHandler(next => (sender, name) =>
            {
                lookups.Enqueue(name);
                throw new ArgumentException();
            }))
            {
                Assert.Throws<ArgumentException>(() => new OracleErrorLog("..."));
                Assert.Equal(2, lookups.Count);
                Assert.Equal("Oracle.DataAccess.Client", lookups.Dequeue());
                Assert.Equal("System.Data.OracleClient", lookups.Dequeue());

                var config = new Dictionary<string, string>
                {
                    { "connectionString", "..." },
                };
                Assert.Throws<ArgumentException>(() => new OracleErrorLog(config));
                Assert.Equal(2, lookups.Count);
                Assert.Equal("Oracle.DataAccess.Client", lookups.Dequeue());
                Assert.Equal("System.Data.OracleClient", lookups.Dequeue());
            }
        }

        sealed class TestDbProviderFactory : DbProviderFactory
        {
        }
    }
}

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

    using System.Linq;
    using MoreLinq;
    using Xunit;

    #endregion

    public class CallerInfoTests
    {
        [Fact]
        public void Serializable()
        {
            Assert.True(typeof(CallerInfo).IsSerializable);
        }

        [Fact]
        public void Initialization()
        {
            const string member = "foobar";
            const string file = "baz.cs";
            const int line = 42;
            var info = new CallerInfo(member, file, line);
            Assert.Equal(member, info.MemberName);
            Assert.Equal(file, info.FilePath);
            Assert.Equal(line, info.LineNumber);
        }

        [Fact]
        public void NullStringsDuringInitializationBecomeEmpty()
        {                                             // ReSharper disable RedundantArgumentDefaultValue
            var info = new CallerInfo(null, null, 0); // ReSharper restore RedundantArgumentDefaultValue
            Assert.NotNull(info.MemberName);
            Assert.Equal(0, info.MemberName.Length);
            Assert.NotNull(info.FilePath);
            Assert.Equal(0, info.FilePath.Length);
        }

        [Fact]
        public void EmptyInfoIsEmpty()
        {
            Assert.True(CallerInfo.Empty.IsEmpty);
        }

        [Fact]
        public void EmptyInfoMemberNameIsEmpty()
        {
            var memberName = CallerInfo.Empty.MemberName;
            Assert.NotNull(memberName);
            Assert.Equal(0, memberName.Length);
        }

        [Fact]
        public void EmptyInfoFilePathIsEmpty()
        {
            var filePath = CallerInfo.Empty.FilePath;
            Assert.NotNull(filePath);
            Assert.Equal(0, filePath.Length);
        }

        [Fact]
        public void EmptyInfoLineNumberIsZero()
        {
            var lineNumber = CallerInfo.Empty.LineNumber;
            Assert.Equal(0, lineNumber);
        }

        [Fact]
        public void StringRepresentations()
        {
            var infos = from member in new[] { null, "foo" }
                        from file in new[] { null, "bar" }
                        from line in new[] { 0, 42 }
                        select new CallerInfo(member, file, line);

            var expectations = new[]
            {
                "<?member>@<?filename>:0",
                "<?member>@<?filename>:42",
                "<?member>@bar:0",
                "<?member>@bar:42",
                "foo@<?filename>:0",
                "foo@<?filename>:42",
                "foo@bar:0",
                "foo@bar:42",
            };

            var assertions = // TODO Zip instead of joining when on .NET 4
                from info in infos.Index()
                join exp in expectations.Index() on info.Key equals exp.Key
                orderby info.Key
                select new
                {
                    Expected = exp.Value,
                    Actual   = info.Value.ToString(),
                };
            
            foreach (var a in assertions)
                Assert.Equal(a.Expected, a.Actual);
        }
    }
}

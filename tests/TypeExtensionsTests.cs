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
    using System.Linq;
    using Xunit;

    #endregion

    public class TypeExtensionsTests
    {
        [Fact]
        public void GetEnumMembersThrowsWithNullThis()
        {
            var e = Assert.Throws<ArgumentNullException>(() => TypeExtensions.GetEnumMembers(null));
            Assert.Equal("type", e.ParamName);
        }

        [Fact]
        public void GetEnumMembersThrowsWithNonEnumThis()
        {
            var e = Assert.Throws<ArgumentException>(() => typeof(object).GetEnumMembers());
            Assert.Equal("type", e.ParamName);
        }

        [Fact]
        public void GetEnumMembers()
        {
            var type = typeof(AttributeTargets);
            var members = type.GetEnumMembers();
            Assert.NotNull(members);
            members = members.ToArray(); // materialize
            Assert.Equal(Enum.GetValues(type).Length, members.Count());
            Assert.True(Enum.GetNames(type).SequenceEqual(from m in members select m.Key));
            Assert.True(Enum.GetValues(type).Cast<object>().SequenceEqual(from m in members select m.Value));
        }
    }
}

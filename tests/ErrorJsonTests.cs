#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Thomas Ardal, http://thomasardal.com
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

    using System.Collections.Specialized;
    using Xunit;

    #endregion

    public class ErrorJsonTests
    {
        /// <summary>
        /// Tests that encoding error to json with query parameter without equal sign.
        /// Test fix of: https://code.google.com/p/elmah/issues/detail?id=308
        /// </summary>
        [Fact]
        public void CanEncodeNullKeysInCollections()
        {
            var error = new Error();
            error.QueryString.Add(new NameValueCollection { { null, "foo" } });
            var json = ErrorJson.EncodeString(error);
            Assert.NotNull(json);
        }
    }
}
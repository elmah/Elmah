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

[assembly: Elmah.Scc("$Id: ComparisonResults.cs 622 2009-05-30 00:35:37Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;

    #endregion

    public static class ComparisonResults
    {
        public readonly static Predicate<int> Equal = result => result == 0;
        public readonly static Predicate<int> Lesser = result => result < 0;
        public readonly static Predicate<int> LesserOrEqual = result => result <= 0;
        public readonly static Predicate<int> Greater = result => result > 0;
        public readonly static Predicate<int> GreaterOrEqual = result => result >= 0;
    }
}

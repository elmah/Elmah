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

[assembly: Elmah.Scc("$Id: Debug.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.Diagnostics;
    using SysDebug = System.Diagnostics.Debug;
    using JetBrains.Annotations;

    #endregion

    /// <summary>
    /// Provides methods for assertions and debugging help that is mostly 
    /// applicable during development.
    /// </summary>
    
    internal sealed class Debug
    {
        [ Conditional("DEBUG") ]
        [ AssertionMethod ]
        public static void Assert([AssertionCondition(AssertionConditionType.IS_TRUE)] bool condition)
        {
            SysDebug.Assert(condition);
        }

        [ Conditional("DEBUG") ]
        public static void AssertStringNotEmpty(string s)
        {
            Assert(s != null);
            Assert(s.Length != 0);
        }
        
        private Debug() {}
    }
}

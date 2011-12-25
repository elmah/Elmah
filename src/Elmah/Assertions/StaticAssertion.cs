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

[assembly: Elmah.Scc("$Id: StaticAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    /// <summary>
    /// An static assertion implementation that always evaluates to 
    /// a preset value.
    /// </summary>

    public sealed class StaticAssertion : IAssertion
    {
        public static readonly StaticAssertion True = new StaticAssertion(true);
        public static readonly StaticAssertion False = new StaticAssertion(false);
        
        private readonly bool _value;

        private StaticAssertion(bool value)
        {
            _value = value;
        }

        public bool Test(object context)
        {
            return _value;
        }
    }
}
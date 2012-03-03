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

[assembly: Elmah.Scc("$Id: RegexMatchAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// An assertion implementation whose test is based on whether
    /// the result of an input expression evaluated against a context
    /// matches a regular expression pattern or not.
    /// </summary>

    public class RegexMatchAssertion : DataBoundAssertion
    {
        private readonly Regex _regex;
        
        public RegexMatchAssertion(IContextExpression source, Regex regex) : 
            base(source)
        {
            if (regex == null) 
                throw new ArgumentNullException("regex");

            _regex = regex;
        }

        public IContextExpression Source
        {
            get { return Expression; }
        }

        public Regex RegexObject
        {
            get { return _regex; }
        }

        protected override bool TestResult(object result)
        {
            return TestResultMatch(Convert.ToString(result, CultureInfo.InvariantCulture));
        }

        protected virtual bool TestResultMatch(string result)
        {
            return RegexObject.Match(result).Success;
        }
    }
}
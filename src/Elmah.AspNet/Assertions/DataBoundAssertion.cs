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

[assembly: Elmah.Scc("$Id: DataBoundAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;

    #endregion

    public abstract class DataBoundAssertion : IAssertion
    {
        private readonly IContextExpression _expression;

        protected DataBoundAssertion(IContextExpression expression)
        {
            if (expression == null) 
                throw new ArgumentNullException("expression");

            _expression = expression;
        }

        protected IContextExpression Expression
        {
            get { return _expression; }
        }

        public virtual bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return TestResult(Expression.Evaluate(context));
        }

        protected abstract bool TestResult(object result);
    }
}
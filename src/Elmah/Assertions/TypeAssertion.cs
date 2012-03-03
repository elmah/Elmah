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

[assembly: Elmah.Scc("$Id: TypeAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    #region Imports

    using System;

    #endregion

    /// <summary>
    /// An assertion implementation whose test is based on whether
    /// the result of an input expression evaluated against a context
    /// matches a regular expression pattern or not.
    /// </summary>

    public sealed class TypeAssertion : DataBoundAssertion
    {
        private readonly Type _expectedType;
        private readonly bool _byCompatibility;

        public TypeAssertion(IContextExpression source, Type expectedType, bool byCompatibility) : 
            base(MaskNullExpression(source))
        {
            if (expectedType == null)
                throw new ArgumentNullException("expectedType");

            if (expectedType.IsInterface || (expectedType.IsClass && expectedType.IsAbstract))
            {
                //
                // Interfaces and abstract classes will always have an 
                // ancestral relationship.
                //
                
                byCompatibility = true;
            }

            _expectedType = expectedType;
            _byCompatibility = byCompatibility;
        }

        public IContextExpression Source
        {
            get { return Expression; }
        }

        public Type ExpectedType
        {
            get { return _expectedType; }
        }

        public bool ByCompatibility
        {
            get { return _byCompatibility; }
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (ExpectedType == null)
                return false;
            
            return base.Test(context);
        }

        protected override bool TestResult(object result)
        {
            if (result == null)
                return false;

            Type resultType = result.GetType();
            Type expectedType = ExpectedType;
            
            Debug.Assert(expectedType != null);
            
            return ByCompatibility ? 
                expectedType.IsAssignableFrom(resultType) : 
                expectedType.Equals(resultType);
        }

        private static IContextExpression MaskNullExpression(IContextExpression expression)
        {
            return expression != null
                 ? expression
                 : new DelegatedContextExpression(new ContextExpressionEvaluationHandler(EvaluateToException));
        }

        private static object EvaluateToException(object context)
        {
            //
            // Assume the reasonable default that the user wants the 
            // exception from the context. If the context is not the 
            // expected type so resort to late-binding.
            //

            ExceptionFilterEventArgs args = context as ExceptionFilterEventArgs;
            return args != null ? args.Exception : DataBinder.Eval(context, "Exception");
        }
    }
}
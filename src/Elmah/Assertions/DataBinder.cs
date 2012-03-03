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

[assembly: Elmah.Scc("$Id: DataBinder.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;

    #endregion

    /// <summary>
    /// Provides data expression evaluation facilites similar to 
    /// <see cref="System.Web.UI.DataBinder"/> in ASP.NET.
    /// </summary>

    public sealed class DataBinder
    {
        public static object Eval(object container, string expression)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            //
            // The ASP.NET DataBinder.Eval method does not like an empty or null
            // expression. Rather than making it an unnecessary exception, we
            // turn a nil-expression to mean, "evaluate to container."
            //

            if (Mask.NullString(expression).Length == 0)
                return container;

            //
            // CAUTION! DataBinder.Eval performs late-bound evaluation, using
            // reflection, at runtime, therefore it can cause performance less
            // than optimal. If needed, this point can be used to either
            // compile the expression or optimize out certain cases (known to be
            // heavily used) by binding statically at compile-time or even
            // partially at runtime using delegates.
            //

            return System.Web.UI.DataBinder.Eval(container, expression);
        }

        private DataBinder()
        {
            throw new NotSupportedException();
        }
    }
}

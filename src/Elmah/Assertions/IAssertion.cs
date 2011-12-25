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

[assembly: Elmah.Scc("$Id: IAssertion.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah.Assertions
{
    /// <summary>
    /// Provides evaluation of a context to determine whether it matches
    /// certain criteria or not.
    /// </summary>
    
    public interface IAssertion
    {
        /// <remarks>
        /// The context is typed generically as System.Object when it could have
        /// been restricted to System.Web.HttpContext and also avoid unnecessary
        /// casting downstream. However, using object allows simple
        /// assertions to be unit-tested without having to stub out a lot of
        /// the classes from System.Web (most of which cannot be stubbed anyhow
        /// due to lack of virtual and instance methods).
        /// </remarks>

        bool Test(object context);
    }
}
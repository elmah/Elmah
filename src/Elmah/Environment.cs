#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll
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

[assembly: Elmah.Scc("$Id: Environment.cs 739 2010-10-19 00:08:29Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System.Security;
    using System.Web;

    #endregion

    internal sealed class Environment
    {
        public static string TryGetMachineName()
        {
            return TryGetMachineName(null);
        }

        public static string TryGetMachineName(HttpContext context)
        {
            return TryGetMachineName(context, null);
        }

        /// <remarks>
        /// If <paramref name="unknownName"/> is a null reference then this
        /// method will still return an empty string.
        /// </remarks>

        public static string TryGetMachineName(HttpContext context, string unknownName)
        {
            //
            // System.Web.HttpServerUtility.MachineName and 
            // System.Environment.MachineName require different permissions.
            // Try the former then the latter...chances are higher to have
            // permissions for the former.
            //

            if (context != null)
            {
                try
                {
                    return context.Server.MachineName;
                }
                catch (HttpException)
                {
                    // Yes, according to docs, HttpServerUtility.MachineName
                    // throws HttpException on failing to obtain computer name.
                }
                catch (SecurityException)
                {
                    // A SecurityException may occur in certain, possibly 
                    // user-modified, Medium trust environments.
                }
            }

            try
            {
                return System.Environment.MachineName;
            }
            catch (SecurityException)
            {
                // A SecurityException may occur in certain, possibly 
                // user-modified, Medium trust environments.
            }

            return Mask.NullString(unknownName);
        }

        private Environment() { }
    }
}

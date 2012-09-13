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

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Web;
    using System.Web.Hosting;

    #endregion

    static class HttpRequestValidation
    {
        /// <summary>
        /// Returns unvalidated collections if build targets .NET Framework
        /// 4.0 or later and if caller is hosted at run-time (based on value
        /// of <see cref="HostingEnvironment.IsHosted"/>) when targeting 
        /// .NET Framework 4.0 exclusively. In all other cases except when
        /// targeting .NET Framework 4.5, collections returned are validated 
        /// ones from <see cref="HttpRequestBase.Form"/> and 
        /// <see cref="HttpRequestBase.QueryString"/> and therefore
        /// could raise <see cref="HttpRequestValidationException"/>.
        /// </summary>

        internal static T TryGetUnvalidatedCollections<T>(this HttpRequestBase request, 
            Func<NameValueCollection, NameValueCollection, HttpCookieCollection, T> resultor)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (resultor == null) throw new ArgumentNullException("resultor");

            NameValueCollection form = null, queryString = null;

            #if NET_4_0

            // ValidationUtility.GetUnvalidatedCollections relies on
            // HttpContext as opposed to HttpContextBase, which would render
            // the caller untestable. As a result, getting unvalidated
            // collections via Microsoft.Web.Infrastructure is only 
            // attempted when hosted.

            if (HostingEnvironment.IsHosted) // Won't be hosted during testing
            {
                var context = request.RequestContext.HttpContext;
                // Allow future patching of GetUnvalidatedCollections
                // as Microsoft reserves it for internal use only.
                var fqs = context.Items["ELMAH:Microsoft.Web.Infrastructure.DynamicValidationHelper.ValidationUtility.GetUnvalidatedCollections"] as Func<HttpRequestBase, IEnumerable<NameValueCollection>> ?? GetUnvalidatedCollections;
                
                using (var collections = (fqs(request) ?? Enumerable.Empty<NameValueCollection>()).GetEnumerator())
                {
                    if (collections.MoveNext())
                    {
                        form = collections.Current;
                        if (collections.MoveNext())
                            queryString = collections.Current;
                    }
                }
            }

            // TODO Use HttpRequestBase.Unvalidated[1] in ASP.NET 4.5 and later
            // [1] http://msdn.microsoft.com/en-us/library/system.web.httprequestbase.unvalidated.aspx
            #endif
            
            #if NET_3_5 || NET_4_0
            var qsfc = request;
            #else
            var qsfc = request.Unvalidated; // ASP.NET 4.5 and later
            #endif
                                                             // ReSharper disable ConstantNullCoalescingCondition
            return resultor(form ?? qsfc.Form,
                            queryString ?? qsfc.QueryString, // ReSharper restore ConstantNullCoalescingCondition
                            qsfc.Cookies);
        }

        #if NET_4_0

        [MethodImpl(MethodImplOptions.NoInlining)]
        static IEnumerable<NameValueCollection> GetUnvalidatedCollections(HttpRequestBase request)
        {
            Debug.Assert(request != null);

            Func<NameValueCollection> form, qs;
            var context = request.RequestContext.HttpContext.ApplicationInstance.Context;
            Microsoft.Web.Infrastructure.DynamicValidationHelper /* ...
               ... */.ValidationUtility.GetUnvalidatedCollections(context, out form, out qs);
            return Array.AsReadOnly(new[] { form(), qs() });
        }
        
        #endif
    }
}

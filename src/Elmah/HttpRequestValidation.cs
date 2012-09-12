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
        /// of <see cref="HostingEnvironment.IsHosted"/>). In all other 
        /// cases, collections returned are validated ones from
        /// <see cref="HttpRequestBase.Form"/> and 
        /// <see cref="HttpRequestBase.QueryString"/> and therefore
        /// could raise <see cref="HttpRequestValidationException"/>.
        /// </summary>

        internal static T TryGetUnvalidatedCollections<T>(this HttpRequestBase request, Func<NameValueCollection, NameValueCollection, T> resultor)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (resultor == null) throw new ArgumentNullException("resultor");

            NameValueCollection form = null, queryString = null;

            #if NET_4_0 // ASP.NET 4 and later

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

            // ReSharper disable ConstantNullCoalescingCondition
            return resultor(form ?? request.Form, queryString ?? request.QueryString); // ReSharper restore ConstantNullCoalescingCondition
        }

        #if NET_4_0 // .NET Framework 4.0 and later

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

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

[assembly: Elmah.Scc("$Id: ErrorLogPageFactory.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.ObjectModel;
    using System.Web;

    using CultureInfo = System.Globalization.CultureInfo;
    using Encoding = System.Text.Encoding;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// HTTP handler factory that dispenses handlers for rendering views and 
    /// resources needed to display the error log.
    /// </summary>

    public class ErrorLogPageFactory : IHttpHandlerFactory
    {
        private static readonly object _authorizationHandlersKey = new object();
        private static readonly IRequestAuthorizationHandler[] _zeroAuthorizationHandlers = new IRequestAuthorizationHandler[0];

        IHttpHandler IHttpHandlerFactory.GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            return GetHandler(new HttpContextWrapper(context), requestType, url, pathTranslated);
        }

        /// <summary>
        /// Returns an object that implements the <see cref="IHttpHandler"/> 
        /// interface and which is responsible for serving the request.
        /// </summary>
        /// <returns>
        /// A new <see cref="IHttpHandler"/> object that processes the request.
        /// </returns>

        public virtual IHttpHandler GetHandler(HttpContextBase context, string requestType, string url, string pathTranslated)
        {
            //
            // The request resource is determined by the looking up the
            // value of the PATH_INFO server variable.
            //

            string resource = context.Request.PathInfo.Length == 0 ? string.Empty :
                context.Request.PathInfo.Substring(1).ToLower(CultureInfo.InvariantCulture);

            IHttpHandler handler = FindHandler(resource);

            if (handler == null)
                throw new HttpException(404, "Resource not found.");

            //
            // Check if authorized then grant or deny request.
            //

            int authorized = IsAuthorized(context);
            if (authorized == 0
                || (authorized < 0 // Compatibility case...
                    && !context.Request.IsLocal 
                    && !SecurityConfiguration.Default.AllowRemoteAccess))
            {
                ManifestResourceHandler.Create("RemoteAccessError.htm", "text/html")(context);
                var response = context.Response;
                response.Status = "403 Forbidden";
                response.End();

                //
                // HttpResponse.End docs say that it throws 
                // ThreadAbortException and so should never end up here but
                // that's not been the observation in the debugger. So as a
                // precautionary measure, bail out anyway.
                //

                return null;
            }

            return handler;
        }

        private static IHttpHandler FindHandler(string name) 
        {
            Debug.Assert(name != null);

            switch (name)
            {
                case "detail":
                    return new ErrorDetailPage();

                case "html":
                    return new ErrorHtmlPage();

                case "xml":
                    return new DelegatingHttpHandler(ErrorXmlHandler.ProcessRequest);

                case "json":
                    return new DelegatingHttpHandler(ErrorJsonHandler.ProcessRequest);

                case "rss":
                    return new DelegatingHttpHandler(ErrorRssHandler.ProcessRequest);

                case "digestrss":
                    return new DelegatingHttpHandler(ErrorDigestRssHandler.ProcessRequest);

                case "download":
                    return new HttpAsyncHandler((context, getAsyncCallback) => HttpTextAsyncHandler.Create(ErrorLogDownloadHandler.ProcessRequest)(context, getAsyncCallback));

                case "stylesheet":
                    return new DelegatingHttpHandler(ManifestResourceHandler.Create(StyleSheetHelper.StylesheetResourceNames, "text/css", Encoding.GetEncoding("Windows-1252"), true));

                case "test":
                    throw new TestException();

                case "about":
                    return new AboutPage();

                default:
                    return name.Length == 0 ? new ErrorLogPage() : null;
            }
        }

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>

        public virtual void ReleaseHandler(IHttpHandler handler)
        {
        }

        /// <summary>
        /// Determines if the request is authorized by objects implementing
        /// <see cref="IRequestAuthorizationHandler" />.
        /// </summary>
        /// <returns>
        /// Returns zero if unauthorized, a value greater than zero if 
        /// authorized otherwise a value less than zero if no handlers
        /// were available to answer.
        /// </returns>

        private static int IsAuthorized(HttpContextBase context)
        {
            Debug.Assert(context != null);

            int authorized = /* uninitialized */ -1;
            var authorizationHandlers = GetAuthorizationHandlers(context).GetEnumerator();
            while (authorized != 0 && authorizationHandlers.MoveNext())
            {
                IRequestAuthorizationHandler authorizationHandler = authorizationHandlers.Current;
                authorized = authorizationHandler.Authorize(context) ? 1 : 0;
            }
            return authorized;
        }

        private static IList<IRequestAuthorizationHandler> GetAuthorizationHandlers(HttpContextBase context)
        {
            Debug.Assert(context != null);

            object key = _authorizationHandlersKey;
            IList<IRequestAuthorizationHandler> handlers = (IList<IRequestAuthorizationHandler>)context.Items[key];

            if (handlers == null)
            {
                const int capacity = 4;
                List<IRequestAuthorizationHandler> list = new List<IRequestAuthorizationHandler>(capacity);

                HttpApplication application = context.ApplicationInstance;
                IRequestAuthorizationHandler appReqHandler = application as IRequestAuthorizationHandler;
                if (appReqHandler != null)
                {
                    list.Add(appReqHandler);
                }

                foreach (IHttpModule module in HttpModuleRegistry.GetModules(application))
                {
                    IRequestAuthorizationHandler modReqHander = module as IRequestAuthorizationHandler;
                    if (modReqHander != null)
                    {
                        list.Add(modReqHander);
                    }
                }
                
                if (list != null)

                context.Items[key] = handlers = list.AsReadOnly();
            }

            return handlers;
        }

        internal static Uri GetRequestUrl(HttpContextBase context)
        {
            if (context == null) throw new ArgumentNullException("context");

            Uri url = context.Items["ELMAH_REQUEST_URL"] as Uri;
            return url != null ? url : context.Request.Url;
        }
    }

    public interface IRequestAuthorizationHandler
    {
        bool Authorize(HttpContextBase context);
    }
}

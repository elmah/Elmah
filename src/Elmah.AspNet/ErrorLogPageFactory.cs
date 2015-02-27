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
    using System.Linq;
    using System.Web;
    using System.Collections.Generic;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// HTTP handler factory that dispenses handlers for rendering views and 
    /// resources needed to display the error log.
    /// </summary>

    public class ErrorLogPageFactory : IHttpHandlerFactory
    {
        private static readonly object _authorizationHandlersKey = new object();

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

            var request = context.Request;
            var resource = request.PathInfo.Length == 0 
                         ? string.Empty 
                         : request.PathInfo.Substring(1).ToLowerInvariant();

            var handler = FindHandler(resource);

            if (handler == null)
                throw new HttpException(404, "Resource not found.");

            //
            // Check if authorized then grant or deny request.
            //

            var authorized = IsAuthorized(context);
            if (authorized == false
                || (authorized == null // Compatibility case...
                    && !request.IsLocal 
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
                    return CreateTemplateHandler<ErrorDetailPage>();
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
                    #if NET_3_5 || NET_4_0
                    return new HttpAsyncHandler((context, getAsyncCallback) => HttpTextAsyncHandler.Create(ErrorLogDownloadHandler.ProcessRequest)(context, getAsyncCallback));
                    #else
                    return new DelegatingHttpTaskAsyncHandler(ErrorLogDownloadHandler.ProcessRequestAsync);
                    #endif
                case "stylesheet":
                    return new DelegatingHttpHandler(ManifestResourceHandler.Create(StyleSheetHelper.StyleSheetResourceNames, "text/css", Encoding.GetEncoding("Windows-1252"), true));
                case "test":
                    throw new TestException();
                case "about":
                    return CreateTemplateHandler<AboutPage>();
                default:
                    return name.Length == 0 ? CreateTemplateHandler<ErrorLogPage>() : null;
            }
        }

        static IHttpHandler CreateTemplateHandler<T>() where T : WebTemplateBase, new()
        {
            return new DelegatingHttpHandler(context =>
            {
                var template = new T { Context = context };
                context.Response.Write(template.TransformText());
            });
        }

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>

        public virtual void ReleaseHandler(IHttpHandler handler) {}

        /// <summary>
        /// Determines if the request is authorized by objects implementing
        /// <see cref="IRequestAuthorizationHandler" />.
        /// </summary>
        /// <returns>
        /// Returns <c>false</c> if unauthorized, <c>true</c> if authorized 
        /// otherwise <c>null</c> if no handlers were available to answer.
        /// </returns>

        private static bool? IsAuthorized(HttpContextBase context)
        {
            Debug.Assert(context != null);

            var handlers = GetAuthorizationHandlers(context).ToArray();
            return handlers.Length != 0 
                 ? handlers.All(h => h.Authorize(context)) 
                 : (bool?) null;
        }

        private static IEnumerable<IRequestAuthorizationHandler> GetAuthorizationHandlers(HttpContextBase context)
        {
            Debug.Assert(context != null);

            var key = _authorizationHandlersKey;
            var handlers = (IEnumerable<IRequestAuthorizationHandler>) context.Items[key];

            if (handlers == null)
            {
                handlers =
                    from app in new[] { context.ApplicationInstance }
                    let mods = HttpModuleRegistry.GetModules(app)
                    select new[] { app }.Concat(from object m in mods select m) into objs
                    from obj in objs
                    select obj as IRequestAuthorizationHandler into handler
                    where handler != null
                    select handler;

                context.Items[key] = handlers = Array.AsReadOnly(handlers.ToArray());
            }

            return handlers;
        }

        internal static Uri GetRequestUrl(HttpContextBase context)
        {
            if (context == null) throw new ArgumentNullException("context");
            var url = context.Items["ELMAH_REQUEST_URL"] as Uri;
            return url ?? context.Request.Url;
        }
    }

    public interface IRequestAuthorizationHandler
    {
        bool Authorize(HttpContextBase context);
    }
}

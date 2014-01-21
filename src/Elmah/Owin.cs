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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Mannex.Threading.Tasks;
    using Microsoft.Owin;
    using global::Owin;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// HTTP handler factory that dispenses handlers for rendering views and 
    /// resources needed to display the error log.
    /// </summary>

    public static class OwinSupport
    {
        static Task CreateTemplateHandler<T>(IOwinContext context) 
            where T : WebTemplateBase, new()
        {
            var template = new T { Context = context };
            var response = context.Response;
            response.ContentType = new ContentType { MediaType = "text/html", CharSet = Encoding.UTF8.WebName }.ToString();
            return response.WriteAsync(template.TransformText());
        }

        public static Func<IOwinContext, Task> FindHandler(string name)
        {
            Debug.Assert(name != null);

            switch (name)
            {
                case "detail":
                    return CreateTemplateHandler<ErrorDetailPage>;
                case "html":
                    return context => ErrorHtmlPage.ProcessRequest(context, ErrorLog.GetDefault(context));
                case "xml":
                    return context => ErrorXmlHandler.ProcessRequest(context, ErrorLog.GetDefault(context));
                case "json":
                    return context => ErrorJsonHandler.ProcessRequest(context, ErrorLog.GetDefault(context));
                case "rss":
                    return context => ErrorRssHandler.ProcessRequest(context, ErrorLog.GetDefault(context), context.Request.MyBaseUrl(), e => GetErrorDetailPageUrl(context, e));
                case "digestrss":
                    return context => ErrorDigestRssHandler.ProcessRequest(context, ErrorLog.GetDefault(context), context.Request.MyBaseUrl(), e => GetErrorDetailPageUrl(context, e));
                case "download":
                    #if NET_4_0
                        return context => Task.Factory.StartNew(HttpTextAsyncHandler.Create(ErrorLogDownloadHandler.ProcessRequest)(context));
                    #else // .NET Framework 4.5 and later...
                        return ErrorLogDownloadHandler.ProcessRequestAsync;
                    #endif
                case "stylesheet":
                    return context => ManifestResourceHandler.ProcessRequest(context, StyleSheetHelper.StyleSheetResourceNames, "text/css", Encoding.GetEncoding("Windows-1252"), true);
                case "test":
                    return context => { throw new TestException(); };
                case "about":
                    return CreateTemplateHandler<AboutPage>;
                default:
                    return name.Length == 0 
                         ? CreateTemplateHandler<ErrorLogPage> 
                         : (Func<IOwinContext, Task>) null;
            }
        }

        static Uri GetErrorDetailPageUrl(IOwinContext context, ErrorLogEntry e)
        {
            return new Uri(context.Request.MyBaseUrl(), "detail?id=" + Uri.EscapeDataString(e.Id));
        }

        static IAppBuilder CommonConfiguration(this IAppBuilder app)
        {
            if (app == null) throw new ArgumentNullException("app");

            // TODO Review if versioning info is done as suggested and intended in the OWIN spec:
            //
            // "Implementers SHOULD include a technology specific version 
            //  key that clearly identifies not only the version of the 
            //  underlying component, but also the version of any OWIN 
            //  specific wrapper of that component.  E.g. 
            //  'mshttplistener.Version: .NET 4.0, OWIN wrapper 1.0.1'.  
            //  This is intended to help consumers cross reference 
            //  documentation with implementations, facilitating both 
            //  development and debugging.
            //
            //  Where the version includes the .NET framework version, 
            //  specify the version compiled against, not the version 
            //  currently running.  E.g. if the component targeted 
            //  .NET 4.0 and is running on .NET 4.5, specify 4.0."
            //
            //  - Section 3, OWIN Key Guidelines and Common Keys
            //    http://owin.org/spec/CommonKeys.html#_3._Naming_conventions

            app.Properties["elmah.Version"] = PoweredBy.GetAbout((v, fv, p, cr) => v.ToString());
            return app;
        }

        public static void UseElmahWeb(this IAppBuilder builder)
        {
            // TODO Check host compatibility as pointed out at http://stackoverflow.com/a/19613529/6682

            builder.CommonConfiguration()
                   .Use((context, next) => GetHandler(context) ?? next());
        }

        public static void UseElmahLogging(this IAppBuilder builder)
        {
            // TODO Check host compatibility as pointed out at http://stackoverflow.com/a/19613529/6682

            builder.CommonConfiguration()
                   .Use((context, next) =>
                   {
                       try
                       {
                           return next().ContinueWith(t =>
                           {
                               if (t.IsFaulted)
                                   ErrorLog.GetDefault(null).Log(new Error(t.Exception));
                           });
                       }
                       catch (Exception e)
                       {
                           ErrorLog.GetDefault(null).Log(new Error(e));
                           return CompletedTask.Error(e);
                       }
                   });
        }

        internal static Task NotFound(this IOwinResponse response, string message)
        {
            response.StatusCode = HttpStatus.NotFound.Code;
            response.ReasonPhrase = HttpStatus.NotFound.Reason;
            return CompletedTask.Return();
            // TODO? throw new HttpException(404, "Resource not found.");
        }

        static Task GetHandler(IOwinContext context)
        {
            // TODO Make base URL configurable
            const string elmah = "/elmah";

            var request = context.Request;
            PathString pathInfo;
            if (!request.Path.StartsWithSegments(new PathString(elmah), out pathInfo))
                return null;
            context.Set(OwinKeys.BaseUrl, elmah + "/");
            var resource = pathInfo.HasValue
                         ? (pathInfo.ToString().Substring(1)).ToLowerInvariant()
                         : string.Empty;

            var handler = FindHandler(resource);

            if (handler == null) // TODO logging?
                return context.Response.NotFound("Resource not found.");

            //
            // Check if authorized then grant or deny request.
            //

            // TODO render truly async with await
            var authorized = IsAuthorized(context).Result;
            if (authorized == false
                || (authorized == null // Compatibility case...
                    && !request.IsLocal())
                    && !SecurityConfiguration.Default.AllowRemoteAccess)
            {
                var response = context.Response;
                response.StatusCode = HttpStatus.Forbidden.Code;
                return ManifestResourceHandler.ProcessRequest(context, "RemoteAccessError.htm", "text/html");
            }

            return handler(context);
        }

        /// <summary>
        /// Determines if the request is authorized by functions with taking 
        /// an OWIN environment as input and return <see cref="Task{TResult}"/> 
        /// of <see cref="bool"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>false</c> if unauthorized, <c>true</c> if authorized 
        /// otherwise <c>null</c> if no handlers were available to answer.
        /// </returns>

        static Task<bool?> IsAuthorized(IOwinContext context)
        {
            Debug.Assert(context != null);

            var handlers = GetAuthorizationHandlers(context).ToArray();
            var authorized = handlers.Length != 0
                           ? handlers.All(h => h(context.Environment).Result)
                           : (bool?) null;
            return CompletedTask.Return(authorized);
        }

        static IEnumerable<Func<IDictionary<string, object>, Task<bool>>> GetAuthorizationHandlers(IOwinContext context)
        {
            Debug.Assert(context != null);
            return Array.AsReadOnly((context.Get<IEnumerable<Func<IDictionary<string, object>, Task<bool>>>>("elmah.RequestAuthorizationHandlers") ?? Enumerable.Empty<Func<IDictionary<string, object>, Task<bool>>>()).ToArray());
        }
    }

    static class OwinKeys
    {
        public static readonly string BaseUrl = "elmah.BaseUrl";
    }

    static class OwinExtensions
    {
        public static bool IsLocal(this IOwinRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            string remoteAddress;
            return request.Get<bool?>("server.IsLocal") // http://owin.org/spec/CommonKeys.html#_6._Common_keys
                // Fallback
                ?? !string.IsNullOrEmpty(remoteAddress = request.RemoteIpAddress) 
                   && (remoteAddress == "127.0.0.1" 
                       || remoteAddress == "::1" 
                       || remoteAddress == request.LocalIpAddress);
        }

        public static Task WriteUtf8TextAsync(this IOwinResponse response, string mediaType, string text)
        {
            return WriteTextAsync(response, mediaType, Encoding.UTF8, text);
        }
       
        public static Task WriteTextAsync(this IOwinResponse response, string mediaType, Encoding encoding, string text)
        {
            response.SettingContentType(mediaType, encoding);
            return WriteTextAsyncImpl(response, encoding, text);
        }
        
        static Task WriteTextAsyncImpl(IOwinResponse response, Encoding encoding, string text)
        {
            Debug.Assert(response != null);
            Debug.Assert(encoding != null);

            if (string.IsNullOrEmpty(text))
                return CompletedTask.Return();

            #if NET_4_0
            
            var bytes = encoding.GetBytes(text);
            return Task.Factory.FromAsync(response.Body.BeginWrite, response.Body.EndWrite, bytes, 0, bytes.Length, null);

            #else // .NET Framework 4.5 and later...

            using (var writer = new System.IO.StreamWriter(response.Body, encoding, 1024, true))
                return writer.WriteAsync(text);
            
            #endif
        }

        public static IOwinResponse SettingContentType(this IOwinResponse response, string mediaType, Encoding encoding)
        {
            if (response == null) throw new ArgumentNullException("response");
            if (mediaType == null) throw new ArgumentNullException("mediaType");
            if (mediaType.Length == 0) throw new ArgumentException(null, "mediaType");
            if (encoding == null) throw new ArgumentNullException("encoding");
            return response.SettingContentType(new ContentType { MediaType = mediaType, CharSet = encoding.WebName });
        }

        public static IOwinResponse SettingContentType(this IOwinResponse response, ContentType contentType)
        {
            if (response == null) throw new ArgumentNullException("response");
            if (contentType == null) throw new ArgumentNullException("contentType");
            response.ContentType = contentType.ToString();
            return response;
        }

        public static Uri MyBaseUrl(this IOwinRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            return new Uri(request.Scheme
                           + "://"
                           + request.Host.Value
                           + request.PathBase
                           + request.Context.Get<string>(OwinKeys.BaseUrl));
        }
    }

    /* TODO internal */ public static class CompletedTask
    {
        public static Task Error(Exception exception) { return Error<object>(exception); }

        public static Task<T> Error<T>(Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        public static Task Return() { return Return<object>(null); }

        public static Task<T> Return<T>(T result)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(result);
            return tcs.Task;
        }
    }
}

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    #endregion

    static class HttpTextAsyncHandler
    {
        public static Func<IOwinContext, IEnumerable<Task>> Create(Func<IOwinContext, IEnumerable<TaskOr<string>>> handler)
        {
            return context => ProcessRequest(context, handler);
        }

        private static IEnumerable<Task> ProcessRequest(
            IOwinContext context, 
            Func<IOwinContext, IEnumerable<TaskOr<string>>> handler)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (handler == null)
                yield break;

            var response = context.Response;
            var output = response.Body;
            // TODO var encoding = response.ContentEncoding;
            var encoding = Encoding.UTF8;
            var encoder = encoding.GetEncoder();

            var chars = new char[2048];
            var bytes = new byte[encoding.GetMaxByteCount(chars.Length)];

            foreach (var item in handler(context))
            {
                string text;
                if (!item.HasValue)
                {
                    yield return item.Task;
                }
                else if ((text = item.Value) != null)
                {
                    int charsRead, textIndex = 0;

                    do
                    {
                        charsRead = Math.Min(chars.Length, text.Length - textIndex);
                        text.CopyTo(textIndex, chars, 0, charsRead);
                        textIndex += charsRead;

                        var completed = false;
                        var charIndex = 0;

                        while (!completed)
                        {
                            var flush = charsRead == 0;
                            int bytesUsed, charsUsed;
                            encoder.Convert(chars, charIndex, charsRead - charIndex,
                                            bytes, 0, bytes.Length, flush,
                                            out charsUsed, out bytesUsed,
                                            out completed);

                            yield return Task.Factory.FromAsync(output.BeginWrite, output.EndWrite, bytes, 0, bytesUsed, null);
                            charIndex += charsUsed;
                        }
                    }
                    while (charsRead != 0);
                }
            }

            output.Flush();
        }
    }
}

#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
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

namespace Mannex.Threading.Tasks
{
    #if NET_4_0

    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="TaskFactory"/>.
    /// </summary>

    static partial class TaskFactoryExtensions
    {
        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job)
        {
            return StartNew(taskFactory, job, CancellationToken.None);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job,
            CancellationToken cancellationToken)
        {
            return StartNew(taskFactory, job, cancellationToken, TaskCreationOptions.None, null);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job,
            TaskCreationOptions creationOptions)
        {
            return StartNew(taskFactory, job, CancellationToken.None, creationOptions, null);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job,
            CancellationToken cancellationToken, TaskCreationOptions creationOptions,
            TaskScheduler scheduler)
        {
            return StartNew(taskFactory, job, null, cancellationToken, creationOptions, scheduler);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job, object state)
        {
            return StartNew(taskFactory, job, state, CancellationToken.None);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>
        
        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job, object state,
            CancellationToken cancellationToken)
        {
            return StartNew(taskFactory, job, state, cancellationToken, TaskCreationOptions.None, null);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>

        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job, object state, 
            TaskCreationOptions creationOptions)
        {
            return StartNew(taskFactory, job, state, CancellationToken.None, creationOptions, null);
        }

        /// <summary>
        /// Creates and starts a new <see cref="Task" /> that iterates
        /// through a sequence of tasks where each task is run as a 
        /// continuation of its predecessor.
        /// </summary>

        public static Task StartNew(
            this TaskFactory taskFactory, IEnumerable<Task> job, object state, 
            CancellationToken cancellationToken, TaskCreationOptions creationOptions, 
            TaskScheduler scheduler)
        {
            if (taskFactory == null) throw new ArgumentNullException("taskFactory");
            if (job == null) throw new ArgumentNullException("job");

            var tcs = new TaskCompletionSource<object>(state, creationOptions);

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
            }
            else
            {
                IEnumerator<Task> task = null;
                Action quantum = null;

                quantum = () => // ReSharper disable AccessToModifiedClosure
                {
                    Debug.Assert(task != null);
                    Debug.Assert(quantum != null);

                    if (cancellationToken.IsCancellationRequested)
                        tcs.SetCanceled();

                    bool done;
                    try
                    {
                        done = !task.MoveNext();
                    }
                    catch (Exception e)
                    {
                        try { task.Dispose(); } // ReSharper disable EmptyGeneralCatchClause                        
                        catch { }               // ReSharper restore EmptyGeneralCatchClause
                        tcs.SetException(e);
                        return;
                    }

                    if (done)
                        tcs.SetResult(null);
                    else
                    {
                        if (scheduler != null)
                            task.Current.ContinueWith(s => quantum(), scheduler);
                        else
                            task.Current.ContinueWith(s => quantum());
                    }
                };
                // ReSharper restore AccessToModifiedClosure

                try
                {
                    task = job.GetEnumerator();
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                    return tcs.Task;
                }

                quantum();
            }

            return tcs.Task;
        }
    }

    #endif // NET4
}

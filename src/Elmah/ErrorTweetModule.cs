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

[assembly: Elmah.Scc("$Id: ErrorTweetModule.cs 566 2009-05-11 10:37:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Web;

    #endregion

    /// <summary>
    /// HTTP module implementation that posts tweets (short messages 
    /// usually limited to 140 characters) about unhandled exceptions in 
    /// an ASP.NET Web application to a Twitter account.
    /// </summary>
    /// <remarks>
    /// This module requires that the hosting application has permissions
    /// send HTTP POST requests to another Internet domain.
    /// </remarks>
    
    public class ErrorTweetModule : HttpModuleBase, IExceptionFiltering
    {
        public event ExceptionFilterEventHandler Filtering;

        private ICredentials _credentials;
        private string _statusFormat;
        private Uri _url;
        private int _maxStatusLength;
        private string _ellipsis;
        private string _formFormat;
        private ArrayList _requests;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        
        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            //
            // Get the configuration section of this module.
            // If it's not there then there is nothing to initialize or do.
            // In this case, the module is as good as mute.
            //

            IDictionary config = (IDictionary) GetConfig();

            if (config == null)
                return;

            string userName = GetSetting(config, "userName", string.Empty);
            string password = GetSetting(config, "password", string.Empty);
            string statusFormat = GetSetting(config, "statusFormat", "{Message}");
            int maxStatusLength = int.Parse(GetSetting(config, "maxStatusLength", "140"), NumberStyles.None, CultureInfo.InvariantCulture);
            string ellipsis = GetSetting(config, "ellipsis", /* ... */ "\x2026");
            string formFormat = GetSetting(config, "formFormat", "status={0}");
            Uri url = new Uri(GetSetting(config, "url", "http://twitter.com/statuses/update.xml")
#if !NET_1_1 && !NET_1_0
                , UriKind.Absolute
#endif
            );

            _credentials = new NetworkCredential(userName, password);
            _statusFormat = statusFormat;
            _url = url;
            _maxStatusLength = maxStatusLength;
            _ellipsis = ellipsis;
            _formFormat = formFormat;
            _requests = ArrayList.Synchronized(new ArrayList(4));

            application.Error += new EventHandler(OnError);
            ErrorSignal.Get(application).Raised += new ErrorSignalEventHandler(OnErrorSignaled);
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance to which the module
        /// will log exceptions.
        /// </summary>
        
        protected virtual ErrorLog GetErrorLog(HttpContext context)
        {
            return ErrorLog.GetDefault(context);
        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs args)
        {
            HttpApplication application = (HttpApplication) sender;
            LogException(application.Server.GetLastError(), application.Context);
        }

        /// <summary>
        /// The handler called when an exception is explicitly signaled.
        /// </summary>

        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            LogException(args.Exception, args.Context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // logging of the uncaught exception.
            //

            ExceptionFilterEventArgs args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);
            
            if (args.Dismissed)
                return;

            //
            // Tweet away...
            //

            HttpWebRequest request = null;

            try
            {
                string status = StringFormatter.Format(_statusFormat, new Error(e, context));

                //
                // Apply ellipsis if status is too long. If the trimmed 
                // status plus ellipsis yields nothing then just use
                // the trimmed status without ellipsis. This can happen if
                // someone gives an ellipsis that is ridiculously long.
                //

                int maxLength = _maxStatusLength;
                if (status.Length > maxLength) 
                {
                    string ellipsis = _ellipsis;
                    int trimmedStatusLength = maxLength - ellipsis.Length;
                    status = trimmedStatusLength >= 0
                           ? status.Substring(0, trimmedStatusLength) + ellipsis
                           : status.Substring(0, maxLength);
                }

                //
                // Submit the status by posting form data as typically down
                // by browsers for forms found in HTML.
                //

                request = (HttpWebRequest) WebRequest.Create(_url);
                request.Method = "POST"; // WebRequestMethods.Http.Post;
                request.ContentType = "application/x-www-form-urlencoded";

                if (_credentials != null)   // Need Basic authentication?
                {
                    request.Credentials = _credentials;
                    request.PreAuthenticate = true;
                }

                // See http://blogs.msdn.com/shitals/archive/2008/12/27/9254245.aspx
                request.ServicePoint.Expect100Continue = false;

                //
                // URL-encode status into the form and get the bytes to
                // determine and set the content length.
                //
                
                string encodedForm = string.Format(_formFormat, HttpUtility.UrlEncode(status));
                byte[] data = Encoding.ASCII.GetBytes(encodedForm);
                Debug.Assert(data.Length > 0);
                request.ContentLength = data.Length;

                //
                // Get the request stream into which the form data is to 
                // be written. This is done asynchronously to free up this
                // thread.
                //
                // NOTE: We maintain a (possibly paranoid) list of 
                // outstanding requests and add the request to it so that 
                // it does not get treated as garbage by GC. In effect, 
                // we are creating an explicit root. It is also possible
                // for this module to get disposed before a request
                // completes. During the callback, no other member should
                // be touched except the requests list!
                //

                _requests.Add(request);

                IAsyncResult ar = request.BeginGetRequestStream(
                    new AsyncCallback(OnGetRequestStreamCompleted), 
                    AsyncArgs(request, data));
            }
            catch (Exception localException)
            {
                //
                // IMPORTANT! We swallow any exception raised during the 
                // logging and send them out to the trace . The idea 
                // here is that logging of exceptions by itself should not 
                // be  critical to the overall operation of the application.
                // The bad thing is that we catch ANY kind of exception, 
                // even system ones and potentially let them slip by.
                //

                OnWebPostError(request, localException);
            }
        }

        private void OnWebPostError(WebRequest request, Exception e)
        {
            Debug.Assert(e != null);
            Trace.WriteLine(e);
            if (request != null) _requests.Remove(request);
        }

        private static object[] AsyncArgs(params object[] args)
        {
            return args;
        }

        private void OnGetRequestStreamCompleted(IAsyncResult ar)
        {
            if (ar == null) throw new ArgumentNullException("ar");
            object[] args = (object[]) ar.AsyncState;
            OnGetRequestStreamCompleted(ar, (WebRequest) args[0], (byte[]) args[1]);
        }

        private void OnGetRequestStreamCompleted(IAsyncResult ar, WebRequest request, byte[] data)
        {
            Debug.Assert(ar != null);
            Debug.Assert(request != null);
            Debug.Assert(data != null);
            Debug.Assert(data.Length > 0);

            try
            {
                using (Stream output = request.EndGetRequestStream(ar))
                    output.Write(data, 0, data.Length);
                request.BeginGetResponse(new AsyncCallback(OnGetResponseCompleted), request);
            }
            catch (Exception e)
            {
                OnWebPostError(request, e);
            }
        }

        private void OnGetResponseCompleted(IAsyncResult ar)
        {
            if (ar == null) throw new ArgumentNullException("ar");
            OnGetResponseCompleted(ar, (WebRequest) ar.AsyncState);
        }

        private void OnGetResponseCompleted(IAsyncResult ar, WebRequest request)
        {
            Debug.Assert(ar != null);
            Debug.Assert(request != null);

            try
            {
                Debug.Assert(request != null);
                request.EndGetResponse(ar).Close(); // Not interested; assume OK
                _requests.Remove(request);
            }
            catch (Exception e)
            {
                OnWebPostError(request, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="Filtering"/> event.
        /// </summary>

        protected virtual void OnFiltering(ExceptionFilterEventArgs args)
        {
            ExceptionFilterEventHandler handler = Filtering;
            
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Determines whether the module will be registered for discovery
        /// in partial trust environments or not.
        /// </summary>

        protected override bool SupportDiscoverability
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the configuration object used by <see cref="OnInit"/> to read
        /// the settings for module.
        /// </summary>

        protected virtual object GetConfig()
        {
            return Configuration.GetSubsection("errorTweet");
        }

        private static string GetSetting(IDictionary config, string name, string defaultValue)
        {
            Debug.Assert(config != null);
            Debug.AssertStringNotEmpty(name);

            string value = Mask.NullString((string)config[name]);

            if (value.Length == 0)
            {
                if (defaultValue == null)
                {
                    throw new ApplicationException(string.Format(
                        "The required configuration setting '{0}' is missing for the error tweeting module.", name));
                }

                value = defaultValue;
            }

            return value;
        }
    }
}

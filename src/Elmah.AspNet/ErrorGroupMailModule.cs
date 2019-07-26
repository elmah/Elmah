namespace Elmah
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Web;
    using System.Web.Configuration;

    /// <summary>
    /// Categorization of all possible ways to group Errors
    /// </summary>
    public enum ErrorGroupingMethod
    {
        /// <summary>
        /// Group Errors by request URL
        /// </summary>
        Url,
        /// <summary>
        /// Group Errors by type of Exception
        /// </summary>
        Exception
    }

    /// <summary>
    /// Property bag for all configuration related to Error grouping logic
    /// </summary>
    public class ErrorGroupConfiguration
    {
        /// <summary>
        /// Instead of sending an email per error, similar errors will be grouped together.
        /// The ErrorGroupingMethods collection specifies how to group errors together.
        /// </summary>
        public ErrorGroupingMethod[] ErrorGroupingMethods { get; set; }

        /// <summary>
        /// How many milliseconds elapse before a group of errors are flushed and sent to email?
        /// Default is 600000 or ten minutes
        /// </summary>
        public long ErrorGroupFlushTimeInMilliseconds { get; set; }

        /// <summary>
        /// How many occurrences of an error within a given group are allowed before the group is flushed and sent?
        /// Default is 50
        /// </summary>
        public int ErrorGroupFlushMaxOccurrences { get; set; }

        private ErrorGroupConfiguration()
        {
            var configurationSection = (IDictionary) Configuration.GetSubsection("errorMail");
            if (configurationSection == null)
            {
                throw new ArgumentNullException("errorMail configuration section");
            }
            ErrorGroupingMethods = ErrorMailModule.GetSetting(configurationSection, "groupingMethods", "Exception")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Enum.Parse(typeof(ErrorGroupingMethod), x))
                .Cast<ErrorGroupingMethod>()
                .ToArray();
            ErrorGroupFlushTimeInMilliseconds = Convert.ToInt64(ErrorMailModule.GetSetting(configurationSection, "groupingFlushMilliseconds", "600000"));
            ErrorGroupFlushMaxOccurrences = Convert.ToInt32(ErrorMailModule.GetSetting(configurationSection, "groupingFlushMaxOccurrences", "50"));
        }

        // Thread-safe singleton (using double-check locking pattern) to ensure configuration is read once per appdomain
        // https://en.wikipedia.org/wiki/Double-checked_locking#Usage_in_C#
        private static ErrorGroupConfiguration _instance;
        private static object _obj = new object();
        public static ErrorGroupConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_obj)
                    {
                        if (_instance == null)
                        {
                            _instance = new ErrorGroupConfiguration();
                        }
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// A thread-safe key/value store of ErrorGroups where:
    /// The key is based on the ErrorGroupingMethods employed
    /// The value is the ErrorGroup
    /// </summary>
    public sealed class ErrorGroupCollection
    {
        /// <summary>
        /// The send event is an action called when an ErrorGroup should be flushed
        /// </summary>
        private readonly Action<ErrorGroup> _sendEvent;
        public ConcurrentDictionary<string, ErrorGroup> Storage { get; }
        private readonly ErrorGroupingMethod[] _errorGroupingMethods;
        private readonly long _errorGroupFlushTimeInMilliseconds;
        private readonly int _errorGroupFlushMaxOccurrences;
        private static readonly Regex _urlParsingRegex = new Regex(@"[\?#].*");

        public ErrorGroupCollection(Action<ErrorGroup> sendEvent, ErrorGroupingMethod [] errorGroupingMethods, long errorGroupFlushTimeInMilliseconds, int errorGroupFlushMaxOccurrences)
        {
            Storage = new ConcurrentDictionary<string, ErrorGroup>();
            _sendEvent = sendEvent;
            _errorGroupingMethods = errorGroupingMethods;
            _errorGroupFlushTimeInMilliseconds = errorGroupFlushTimeInMilliseconds;
            _errorGroupFlushMaxOccurrences = errorGroupFlushMaxOccurrences;
        }

        public ErrorGroupCollection(Action<ErrorGroup> sendEvent)
            : this(sendEvent,
                ErrorGroupConfiguration.Instance.ErrorGroupingMethods,
                ErrorGroupConfiguration.Instance.ErrorGroupFlushTimeInMilliseconds,
                ErrorGroupConfiguration.Instance.ErrorGroupFlushMaxOccurrences) { }

        public void Add(Error error)
        {
            var errorGroupKey = GetErrorGroupKey(error);
            var errorGroup = Storage.GetOrAdd(errorGroupKey, new ErrorGroup(errorGroupKey, _sendEvent, _errorGroupFlushTimeInMilliseconds, _errorGroupFlushMaxOccurrences));
            errorGroup.Add(error);
        }

        /// <summary>
        /// Returns the grouping key for a given Error.
        /// The grouping key returned depends on the configured ErrorGroupingMethods used.
        /// </summary>
        public string GetErrorGroupKey(Error error)
        {
            var attributes = new List<string>();
            if (!_errorGroupingMethods.Any())
            {
                throw new ApplicationException("Unable to derive error errorGroup key because no groupingMethods defined");
            }
            if (_errorGroupingMethods.Contains(ErrorGroupingMethod.Url))
            {
                var rawUrl = error.ServerVariables["URL"];
                var urlWithoutQueryString = _urlParsingRegex.Replace(rawUrl, string.Empty);
                attributes.Add(urlWithoutQueryString);
            }
            if (_errorGroupingMethods.Contains(ErrorGroupingMethod.Exception))
            {
                attributes.Add(error.Exception.GetType().ToString());
            }

            return string.Join("~", attributes.ToArray());
        }
    }

    /// <summary>
    /// A group of "similar" errors which are tracked together for the purpose of emailing together in a single message
    /// </summary>
    public sealed class ErrorGroup
    {
        /// <summary>
        /// The action which should be performed when an ErrorGroup is cleared (such as sending an email)
        /// </summary>
        private readonly Action<ErrorGroup> _onFlush;
        public ConcurrentStack<Error> Errors { get; }
        public Timer ErrorRetentionTimer { get; }
        public string GroupKey { get; }
        private readonly object _obj = new object();
        private readonly long _errorGroupFlushTimeInMilliseconds;
        private readonly int _errorGroupFlushMaxOccurrences;
        
        public ErrorGroup(string groupKey, Action<ErrorGroup> flushAction, long errorGroupFlushTimeInMilliseconds, int errorGroupFlushMaxOccurrences)
        {
            _onFlush = flushAction;
            _errorGroupFlushTimeInMilliseconds = errorGroupFlushTimeInMilliseconds;
            _errorGroupFlushMaxOccurrences = errorGroupFlushMaxOccurrences;
            GroupKey = groupKey;
            Errors = new ConcurrentStack<Error>();
            ErrorRetentionTimer = new Timer(){AutoReset = false};
            if (_errorGroupFlushTimeInMilliseconds > 0)
            {
                ErrorRetentionTimer.Interval = _errorGroupFlushTimeInMilliseconds;
                ErrorRetentionTimer.Elapsed += (s,e) => Flush();
            }
        }

        /// <summary>
        /// Add an Error to the ErrorGroup.
        /// </summary>
        public void Add(Error error)
        {
            // if there is a defined retention time and the ErrorGroup is currently empty then start the timer
            if (_errorGroupFlushTimeInMilliseconds != 0 && Errors.IsEmpty)
            {
                ErrorRetentionTimer.Start();
            }

            // ensure no separate thread is flushing Errors while this thread is adding an Error
            lock (_obj)
            {
                Errors.Push(error);
            }

            // if there is a defined retention limit and the ErrorGroup matches/exceeds that limit then flush the group
            if (_errorGroupFlushMaxOccurrences != 0 && Errors.Count >= _errorGroupFlushMaxOccurrences)
            {
                Flush();
            }
        }

        /// <summary>
        /// Perform the configured flush action, clear the Errors, stop the timer
        /// </summary>
        public void Flush()
        {
            // ensure no other threads are flushing nor adding errors while this thread is flushing
            lock (_obj)
            {
                if (Errors.Any())
                {
                    _onFlush(this);
                    Errors.Clear();
                    ErrorRetentionTimer.Stop();
                }
            }
        }

        public string GetWebHostHtmlMessage()
        {
            return Errors.Select(x => x.WebHostHtmlMessage)
                       .FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? String.Empty;
        }
    }

    public sealed class ErrorGroupMailEventArgs : EventArgs
    {
        private readonly ErrorGroup _error;
        private readonly MailMessage _mail;


        public ErrorGroupMailEventArgs(ErrorGroup error, MailMessage mail)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            if (mail == null)
                throw new ArgumentNullException("mail");

            _error = error;
            _mail = mail;
        }

        public ErrorGroup Error
        {
            get { return _error; }
        }

        public MailMessage Mail
        {
            get { return _mail; }
        }
    }

    public class ErrorGroupMailModule : ErrorMailModule
    {
        private ErrorGroupCollection _errorCollection;
        public delegate void ErrorGroupMailEventHandler(object sender, ErrorGroupMailEventArgs eventArgs);

        public event ExceptionFilterEventHandler Filtering;
        public event ErrorGroupMailEventHandler Mailing;
        public event ErrorGroupMailEventHandler Mailed;
        public event ErrorGroupMailEventHandler DisposingMail;

        protected override void OnInit(HttpApplication application)
        {
            base.OnInit(application);
            _errorCollection = new ErrorGroupCollection(ReportErrorGroup);
        }

        protected override void OnError(Exception e, HttpContextBase context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // reporting of the uncaught exception.
            //
            var args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);

            if (args.Dismissed)
                return;

            // add the error to the groupings
            _errorCollection.Add(new Error(e, context));
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        protected virtual void ReportErrorGroup(ErrorGroup errorGroup)
        {
            if (errorGroup == null)
                throw new ArgumentNullException("errorGroup");

            var subjectFormat = Mask.EmptyString(this.MailSubjectFormat, "Error ({0})");
            var subject = string.Format(subjectFormat, errorGroup.GroupKey).Replace('\r', ' ').Replace('\n', ' ');
            var formatter = new ErrorGroupMailHtmlFormatter();
            var bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, errorGroup);
            var mail = GetMailMessage(subject, bodyWriter.ToString(), formatter.MimeType, errorGroup.GetWebHostHtmlMessage());
            if (mail == null)
            {
                return;
            }
            var args = new ErrorGroupMailEventArgs(errorGroup, mail);

            try
            {
                //
                // Send off the mail with some chance to pre- or post-process
                // using event.
                //

                OnMailing(args);
                SendMail(mail);
                OnMailed(args);
            }
            finally
            {
                OnDisposingMail(args);
                mail.Dispose();
            }
        }

        /// <summary>
        /// Fires the <see cref="Mailing"/> event.
        /// </summary>

        protected virtual void OnMailing(ErrorGroupMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var handler = Mailing;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Fires the <see cref="Mailed"/> event.
        /// </summary>

        protected virtual void OnMailed(ErrorGroupMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var handler = Mailed;

            if (handler != null)
                handler(this, args);
        }
            
        /// <summary>
        /// Fires the <see cref="DisposingMail"/> event.
        /// </summary>
        
        protected virtual void OnDisposingMail(ErrorGroupMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var handler = DisposingMail;

            if (handler != null)
                handler(this, args);
        }

    }

    /// <summary>
    /// Formats the HTML to display the details of a given error that is
    /// suitable for sending as the body of an e-mail message.
    /// </summary>
    
    public class ErrorGroupMailHtmlFormatter
    {
        /// <summary>
        /// Returns the text/html MIME type that is the format provided 
        /// by this <see cref="ErrorTextFormatter"/> implementation.
        /// </summary>

        public string MimeType
        { 
            get { return "text/html"; }
        }

        /// <summary>
        /// Formats a complete HTML document describing the given 
        /// <see cref="Error"/> instance.
        /// </summary>

        public void Format(TextWriter writer, ErrorGroup grouping)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (grouping == null) throw new ArgumentNullException("error");

            var page = new ErrorGroupMailHtmlPage(grouping);
            writer.Write(page.TransformText());
        }
    }

    /// <summary>
    /// Renders an HTML page displaying details about an error from the 
    /// error log ready for emailing.
    /// </summary>

    internal partial class ErrorGroupMailHtmlPage
    {
        public ErrorGroup ErrorGroup { get; private set; }

        public ErrorGroupMailHtmlPage(ErrorGroup errorGroup)
        {
            if (errorGroup == null) throw new ArgumentNullException("errorGroup");
            ErrorGroup = errorGroup;
        }
    }
}
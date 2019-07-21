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
    using System.Threading;
    using System.Web;

    public enum ErrorMailDeduplicationMethods
    {
        Url,
        Exception
    }

    public sealed class ErrorMailDeduplicationGrouping
    {
        public ErrorMailDeduplicationGrouping(IList<Error> errors, int deduplicationMaxTimeSpanSeconds, Action<ErrorMailDeduplicationGrouping> flushEvent)
        {
            Errors = errors;
            if (deduplicationMaxTimeSpanSeconds > 0)
            {
                ErrorRetentionTimer = new Timer(i => flushEvent(this), this, TimeSpan.FromSeconds(deduplicationMaxTimeSpanSeconds), TimeSpan.FromMilliseconds(-1));
            }
        }

        public string GetWebHostHtmlMessage()
        {
            return "WebHostHtmlMessage here!";
        }

        public IList<Error> Errors { get; private set; }
        public Timer ErrorRetentionTimer { get; }
    }

    public sealed class DeduplicatedErrorMailEventArgs : EventArgs
    {
        private readonly ErrorMailDeduplicationGrouping _error;
        private readonly MailMessage _mail;


        public DeduplicatedErrorMailEventArgs(ErrorMailDeduplicationGrouping error, MailMessage mail)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            if (mail == null)
                throw new ArgumentNullException("mail");

            _error = error;
            _mail = mail;
        }

        public ErrorMailDeduplicationGrouping Error
        {
            get { return _error; }
        }

        public MailMessage Mail
        {
            get { return _mail; }
        }
    }

    public delegate void DeduplicatedErrorMailEventHandler(object sender, DeduplicatedErrorMailEventArgs args);
    public class DeduplicatedErrorMailModule : ErrorMailModule
    {
        private int _deduplicationMaxTimeSpanSeconds;
        private int _deduplicationMaxOccurrences;
        private ErrorMailDeduplicationMethods[] _deduplicationMethods = { };
        private static IDictionary<string, ErrorMailDeduplicationGrouping> _deduplicationErrorMailEventArgsGroups = new ConcurrentDictionary<string, ErrorMailDeduplicationGrouping>();

        public event ExceptionFilterEventHandler Filtering;
        public event DeduplicatedErrorMailEventHandler Mailing;
        public event DeduplicatedErrorMailEventHandler Mailed;
        public event DeduplicatedErrorMailEventHandler DisposingMail;


        protected override void OnInit(HttpApplication application)
        {
            base.OnInit(application);
            var config = (IDictionary)GetConfig();
            var deduplicationMaxTimeSpanSeconds = Convert.ToInt32(GetSetting(config, "deduplicationMaxTimeSpanSeconds", "0"));
            var deduplicationMaxOccurrences = Convert.ToInt32(GetSetting(config, "deduplicationMaxOccurrences", "0"));
            var deduplicationMethods = GetSetting(config, "deduplicationMethods", "Exception")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Enum.Parse(typeof(ErrorMailDeduplicationMethods), x))
                .Cast<ErrorMailDeduplicationMethods>()
                .ToArray();
            _deduplicationMaxTimeSpanSeconds = deduplicationMaxTimeSpanSeconds;
            _deduplicationMaxOccurrences = deduplicationMaxOccurrences;
            _deduplicationMethods = deduplicationMethods;
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

            var error = new Error(e, context);

            // add the error to the groupings
            var errorKey = GetErrorKey(error);
            if (!_deduplicationErrorMailEventArgsGroups.ContainsKey(errorKey))
            {
                _deduplicationErrorMailEventArgsGroups.Add(
                    new KeyValuePair<string, ErrorMailDeduplicationGrouping>(
                        errorKey,
                        new ErrorMailDeduplicationGrouping(new List<Error> { error }, _deduplicationMaxTimeSpanSeconds, SendErrorMailDeduplicationGroup)));
            }
            else
            {
                _deduplicationErrorMailEventArgsGroups[errorKey].Errors.Add(error);
            }

            // if deduplication max occurrences is exceeded then send mail and clear the queue
            if (_deduplicationMaxOccurrences > 0 && _deduplicationErrorMailEventArgsGroups[errorKey].Errors.Count > _deduplicationMaxOccurrences)
            {
                SendErrorMailDeduplicationGroup(_deduplicationErrorMailEventArgsGroups[errorKey]);
                _deduplicationErrorMailEventArgsGroups.Remove(errorKey);
            }
        }

        /// <summary>
        /// Sends
        /// </summary>

        private void SendErrorMailDeduplicationGroup(ErrorMailDeduplicationGrouping errorMailDeduplicationGrouping)
        {
            if (errorMailDeduplicationGrouping.Errors.Any())
            {
                // TODO: need to modify template to support groupings!
                ReportError(errorMailDeduplicationGrouping.Errors.First());
                errorMailDeduplicationGrouping.Errors.Clear();
            }
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        protected virtual void ReportError(ErrorMailDeduplicationGrouping grouping)
        {
            if (grouping == null)
                throw new ArgumentNullException("grouping");

            //
            // Start by checking if we have a sender and a recipient.
            // These values may be null if someone overrides the
            // implementation of OnInit but does not override the
            // MailSender and MailRecipient properties.
            //

            var sender = this.MailSender ?? string.Empty;
            var recipient = this.MailRecipient ?? string.Empty;
            var copyRecipient = this.MailCopyRecipient ?? string.Empty;

            if (recipient.Length == 0)
                return;

            //
            // Create the mail, setting up the sender and recipient and priority.
            //

            var mail = new MailMessage();
            mail.Priority = this.MailPriority;

            mail.From = new MailAddress(sender);
            mail.To.Add(recipient);
            
            if (copyRecipient.Length > 0)
                mail.CC.Add(copyRecipient);

            //
            // Format the mail subject.
            // 

            var subjectFormat = Mask.EmptyString(this.MailSubjectFormat, "Error ({1}): {0}");
            mail.Subject = string.Format(subjectFormat, "subject", "type").
                Replace('\r', ' ').Replace('\n', ' ');

            //
            // Format the mail body.
            //

            var formatter = new DeduplicatedErrorMailHtmlFormatter();

            var bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, grouping);
            mail.Body = bodyWriter.ToString();

            switch (formatter.MimeType)
            {
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;

                default :
                {
                    throw new ApplicationException(string.Format(
                        "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                        formatter.GetType().FullName, formatter.MimeType));
                }
            }

            var args = new DeduplicatedErrorMailEventArgs(grouping, mail);

            try
            {
                //
                // If an HTML message was supplied by the web host then attach 
                // it to the mail if not explicitly told not to do so.
                //

                if (!NoYsod && grouping.GetWebHostHtmlMessage().Length > 0)
                {
                    var ysodAttachment = CreateHtmlAttachment("YSOD", grouping.GetWebHostHtmlMessage());

                    if (ysodAttachment != null)
                        mail.Attachments.Add(ysodAttachment);
                }

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

        protected virtual void OnMailing(DeduplicatedErrorMailEventArgs args)
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

        protected virtual void OnMailed(DeduplicatedErrorMailEventArgs args)
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
        
        protected virtual void OnDisposingMail(DeduplicatedErrorMailEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            var handler = DisposingMail;

            if (handler != null)
                handler(this, args);
        }

        private string GetErrorKey(Error error)
        {
            if (!_deduplicationMethods.Any())
            {
                throw new ApplicationException("Unable to derive error grouping key because no deduplicationMethods defined");
            }

            var result = new StringBuilder();

            if (_deduplicationMethods.Contains(ErrorMailDeduplicationMethods.Url))
            {
                result.Append(error.ServerVariables["URL"]);
            }
            if (_deduplicationMethods.Contains(ErrorMailDeduplicationMethods.Url))
            {
                result.Append(error.Exception);
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// Formats the HTML to display the details of a given error that is
    /// suitable for sending as the body of an e-mail message.
    /// </summary>
    
    public class DeduplicatedErrorMailHtmlFormatter
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

        public void Format(TextWriter writer, ErrorMailDeduplicationGrouping grouping)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (grouping == null) throw new ArgumentNullException("error");

            var page = new DeduplicatedErrorMailHtmlPage(grouping);
            writer.Write(page.TransformText());
        }
    }

    /// <summary>
    /// Renders an HTML page displaying details about an error from the 
    /// error log ready for emailing.
    /// </summary>

    internal partial class DeduplicatedErrorMailHtmlPage
    {
        public ErrorMailDeduplicationGrouping Grouping { get; private set; }

        public DeduplicatedErrorMailHtmlPage(ErrorMailDeduplicationGrouping grouping)
        {
            if (grouping == null) throw new ArgumentNullException("grouping");
            Grouping = grouping;
        }
    }
}
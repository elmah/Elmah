namespace Elmah
{
    using System;
    using System.Text;
    using System.Web;

    // http://msdn.microsoft.com/en-us/library/system.web.ihtmlstring.aspx

    interface IHtmlString
    {
        string ToHtmlString();
    }

    class HtmlString : IHtmlString
    {
        private readonly string _html;

        public HtmlString(string html)
        {
            _html = html;
        }

        public string ToHtmlString() { return _html; }

        public override string ToString() { return ToHtmlString(); }
    }

    class WebTemplateBase : RazorTemplateBase, IHttpHandler
    {
        public HttpContextBase Context { get; private set; }
        public HttpApplicationStateBase Application { get { return Context.Application; } }
        public HttpResponseBase Response { get { return Context.Response; } }
        public HttpRequestBase Request { get { return Context.Request; } }
        public HttpServerUtilityBase Server { get { return Context.Server; } }
        public HttpSessionStateBase Session { get { return Context.Session; } }

        public IHtmlString Html(string html)
        {
            return new HtmlString(html);
        }

        public string AttributeEncode(string text)
        {
            return string.IsNullOrEmpty(text)
                 ? string.Empty
                 : HttpUtility.HtmlAttributeEncode(text);
        }

        public string Encode(string text)
        {
            return string.IsNullOrEmpty(text) 
                 ? string.Empty 
                 : Server.HtmlEncode(text);
        }

        public override void Write(object value)
        {
            if (value == null)
                return;
            var html = value as IHtmlString;
            base.Write(html != null ? html.ToHtmlString() : Encode(value.ToString()));
        }

        public override void Execute()
        {
            if (Context == null)
                throw new InvalidOperationException("The Context property has not been initialzed with an instance.");
            base.Execute();
        }

        void IHttpHandler.ProcessRequest(HttpContext context)
        {
            var oldContext = Context;

            try
            {
                Context = new HttpContextWrapper(context);
                context.Response.Write(TransformText());
            }
            finally
            {
                Context = oldContext;
            }
        }

        bool IHttpHandler.IsReusable
        {
            get { return false; }
        }
    }

    class RazorTemplateBase
    {
        string _content;
        private readonly StringBuilder _generatingEnvironment = new StringBuilder();

        public RazorTemplateBase Layout { get; set; }

        public virtual void Execute() {}

        public void WriteLiteral(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
                return;
            _generatingEnvironment.Append(textToAppend); ;
        }

        public virtual void Write(object value)
        {
            if (value == null)
                return;
            WriteLiteral(value.ToString());
        }

        public string RenderBody()
        {
            return _content;
        }

        public string TransformText()
        {
            Execute();
            
            if (Layout != null)
            {
                Layout._content = _generatingEnvironment.ToString();
                return Layout.TransformText();
            }

            return _generatingEnvironment.ToString();
        }
    }
}

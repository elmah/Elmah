<%@ Page Language="C#" Title="Error Log Sample Using XmlDataSource" %>
<%@ Import Namespace="System.Globalization" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<script runat="server">
    const string ErrorLogRssUrl = "~/elmah.axd/rss";
    protected void Page_Load(object sender, EventArgs e)
    {
        ErrorLogRssDataSource.DataBind();
    }
</script>
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" />
    <style type="text/css">
        body
        {
            font-size: small;
            font-family: Arial, Sans-Serif;
            background-color: white;
        }
        h1
        {
            font-size: large;
        }
        dt
        {
            font-weight: bold;
            margin-bottom: 0.25em;
        }
        dd
        {
        }
        dd.time
        {
            margin-top: 0.25em;
            margin-bottom: 1em;
        }
        span.time
        {
            white-space: nowrap;
            color: #888;
        }
        code
        {
            font-family: Courier New, Courier, Monospace;
            font-size: small;
        }
    </style>
</head>
<body>
    <h1><%= Server.HtmlEncode(Title) %></h1>
    <p>
        ELMAH supplies an <a href="<%= ResolveUrl(ErrorLogRssUrl) %>">RSS feed 
        for your error log</a> so you can monitor in your favorite RSS reader. 
        This sample uses the <code><a href="http://msdn.microsoft.com/en-us/library/system.web.ui.webcontrols.xmldatasource.aspx">XmlDataSource</a></code>
        to consume that RSS feed and bind it to the <a href="http://msdn.microsoft.com/en-us/library/system.web.ui.webcontrols.repeater.aspx">Repeater</a> 
        from ASP.NET in order to create a custom presentation of the feed.
    </p>
    <hr />
    <asp:Repeater runat="server" ID="ErrorRepeater" DataSourceID="ErrorLogRssDataSource">
        <HeaderTemplate><dl></HeaderTemplate>
        <ItemTemplate>
            <dt><a href='<%# XPath("link") %>'><%# Server.HtmlEncode(XPath("title").ToString()) %></a></dt>
            <dd><%# Server.HtmlEncode(XPath("description").ToString()) %></dd>
            <dd class="time"><span class="time"><%# Server.HtmlEncode(DateTime.ParseExact(XPath("pubDate").ToString(), "r", null, DateTimeStyles.AssumeUniversal).ToString("f")) %></span></dd>
        </ItemTemplate>
        <FooterTemplate></dl>
        <asp:Literal runat="server" Visible='<%# ErrorRepeater.Items.Count == 0 ? true : false %>' Text="No exceptions have been logged in ELMAH." />
        </FooterTemplate>
    </asp:Repeater>
    <asp:XmlDataSource ID="ErrorLogRssDataSource" runat="server" EnableCaching="false"
        DataFile='<%# Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute(ErrorLogRssUrl) %>'
        XPath="rss/channel/item" />
    <!-- $Id: ErrorsRssView.aspx 572 2009-05-11 14:48:43Z azizatif $ -->
</body>
</html>

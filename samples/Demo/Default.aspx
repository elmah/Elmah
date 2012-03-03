<%@ Page Language="C#" Title="ELMAH Demo Application" EnableViewState="false" %>
<%@ Import Namespace="Elmah" %>
<%@ Import Namespace="System.IO"%>
<%@ Import Namespace="System.Net.Configuration"%>
<%@ Import Namespace="System.Web.Configuration"%>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<script runat="server">
    protected SccStamp Stamp = new SccStamp("$Id: Default.aspx 573 2009-05-11 14:49:01Z azizatif $");
    protected const string RevisionDetailUrlFormat = "http://code.google.com/p/elmah/source/detail?r={0}";
    protected string MailPath;
    protected string SampleWebConfigPath;
    
    protected override void OnLoad(EventArgs e)
    {
        SmtpSection smtpSection = (SmtpSection) WebConfigurationManager.GetSection("system.net/mailSettings/smtp");

        MailPath = (smtpSection != null && smtpSection.SpecifiedPickupDirectory != null 
                    ? smtpSection.SpecifiedPickupDirectory.PickupDirectoryLocation 
                    : null) ?? string.Empty;

        SampleWebConfigPath = Path.Combine(Path.GetDirectoryName(Server.MapPath(".")), "web.config");

        base.OnLoad(e);
    }

    protected void ErrorButton_Click(object sender, EventArgs e)
    {
        ThrowSampleException();
    }

    protected void SignalErrorButton_Click(object sender, EventArgs e)
    {
        try
        {
            ThrowSampleException();
        }
        catch (Exception ex)
        {
            ErrorSignal.FromContext(Context).Raise(ex);
            SignalMessage.InnerText = "Error trapped and signaled at " 
                + DateTime.Now.ToLongTimeString();
        }

        SignalMessage.DataBind();
    }

    private static void ThrowSampleException()
    {
        throw new System.ApplicationException();
    }

</script>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=EmulateIE7" />
    <title>ELMAH Demo</title>
    <style type="text/css">
        body
        {
            background-color: white;
        }
        body, td, th, input, select
        {
            font-family: Arial, Sans-Serif;
            font-size: small;
        }
        code, pre
        {
            font-family: Courier New, Courier, Monospace;
            font-size: small;
        }
        li
        {
            margin-bottom: 0.5em;
        }
        dd
        {
            margin-bottom: 2em;
        }
        dt
        {
            font-weight: bold;
        }
        #SignalMessage
        {
            color: #F00;
        }
        h1
        {
            font-size: large;
        }
        h2
        {
            font-size: medium;
        }
        h3
        {
            font-size: small;
        }
    </style>
</head>
<body>
    <form runat="server">
    <h1><%= Server.HtmlEncode(Title) %></h1>
    <h2>Introduction</h2>
    <p>
        This sample web application is set up to use ELMAH to log errors
        as well as send e-mail notifications to a pick-up directory
        when an unhandled exception occurs.
    </p>
    <h2>Got Exceptions?</h2>
    <p>
        Exceptions in applications fall into two basic buckets: 
        <em>handled</em> and <em>unhandled</em>. ELMAH can work with both.
    </p>
    <h3>Unhandled Exceptions</h3>
    <p>
        Click the button below to generate an exception. The exception
        will not be handled by this sample application. As a result,
        ELMAH will log the error and <em>send</em> an e-mail. Bear in
        mind that the exception will generate what has come to be known as 
        the <em><a href="http://en.wikipedia.org/wiki/Yellow_Screen_of_Death#ASP.NET">yellow screen of death</a></em>
        that ASP.NET developers often dread. You will need to hit the 
        &ldquo;back&rdquo; button on your browser to return here.</p>
    <p>
        <asp:Button ID="ErrorButton" runat="server" Text="Throw Exception" OnClick="ErrorButton_Click" />
    </p>
    <p>
        You can drop and configure ELMAH for an ASP.NET application without making
        any code changes to start logging and viewing unhandled exceptions!
        Unlike unhandled exceptions, however, you do need to change the application
        code in place where you wish to explicitly signal the exception to ELMAH.
    </p>
    <h3>Handled Exceptions</h3>
    <p>
        Click the button below to <em>signal</em> an exception to ELMAH.
        Signaling is useful when you usually handle or swallow an exception 
        in the application code but still want to report it to ELMAH.
    </p>
    <p>
        <asp:Button ID="SignalButton" runat="server" Text="Signal Handled Exception" OnClick="SignalErrorButton_Click" />
        <span ID="SignalMessage" runat="server" visible="<%# !string.IsNullOrEmpty(SignalMessage.InnerText) %>" />
    </p>
    <p>
        Signaling can be especially useful for logging errors that are
        usually handled on the server by propagating them to the client
        as is typically the case with Web service or Ajax scenarios.
    </p>
    <h3>Testing in a Running Application</h3>
    <p>
        ELMAH also includes a simple feature to allow you to raise an exception without writing any code.
        This can come quite handy when you drop ELMAH into a running application and want to be sure that
        it is configured correctly to log and mail or what have you. To do this, you simply
        append <em>slash-test</em> to where you configured ELMAH&rsquo;s handler. For example, for this
        demo application, visit <a href="elmah.axd/test">elmah.axd/test</a> to generate the test error.
        Again, this will generate what is known as the <em><a href="http://en.wikipedia.org/wiki/Yellow_Screen_of_Death#ASP.NET">yellow screen of death</a></em>
        and you will need to hit the &ldquo;back&rdquo; button in your browser to return here.
    </p>
    <h2>Viewing Errors</h2>
    <p>
        To see the list of errors logged, vist <a href="elmah.axd">elmah.axd</a>.
    </p>
    <p>
        To see the notification mails, go to the <a href="Mails/"
        title='<%= Server.HtmlEncode(MailPath) %>'>pick-up directory</a>.
        There you should find files with the <code>eml</code> extension and which 
        you can open and inspect using any text editor.
    </p>
    <p>
        See also the following samples:        
    </p>
    <ul>
        <li><a href="ErrorsGridView.aspx">Error Log Sample Using ObjectDataSource and GridView</a></li>
        <li><a href="ErrorsRssView.aspx">Error Log Sample Using XmlDataSource</a></li>
    </ul>
    <h2 id="Questions">Questions?</h2>
    <ul>
        <li><a href="#Cassini">How does the sample run?</a></li>
        <li><a href="#DataLocation">Where is the ELMAH data stored?</a></li>
        <li><a href="#Email">How does the sample send e-mail?</a></li>
        <li><a href="#Wiki">How do I find out more about getting started?</a></li>
        <li><a href="#Language">I see the code is written in C#. Do I have to use that too?</a></li>
        <li><a href="#NoSource">What if I don't have the source code for an application?</a></li>
        <li><a href="#Signaling">What exactly <em>is</em> signaling?</a></li>
        <li><a href="#CustomErrors">What happens if I turn on custom error handling?</a></li>
        <li><a href="#MediumTrustSupport">Can I use ELMAH in a medium trust application?</a></li>
    </ul>
    <dl>
        <dt><a name="Cassini" />How does the sample run?</dt>
        <dd><p>The sample runs regardless of whether you have Microsoft Visual Studio or 
        Microsoft Internet Information Services (IIS) installed or not. All you need is the
        .NET Framework 2.0 run-time installed on the machine. It runs without any other
        dependencies because it ships with a version of <a href="http://www.asp.net/Downloads/archived/cassini/">Cassini</a> 
        Personal Web Server. Cassini is light-weight and self-contained ASP.NET hosting Web server 
        that allows this ELMAH sample to run with as little as possible.</p>
        <p>You will probably have noticed that when the sample starts, an icon appears
        for Cassini in your task bar. If you shut this down, the sample with cease to work
        until it is restarted.</p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="DataLocation" />Where is the ELMAH data stored?</dt>
        <dd><p>This sample uses <code>SQLiteErrorLog</code> to log errors to a <a href="http://www.sqlite.org/">SQLite</a> database.
        This database is created on the fly by ELMAH making it perfect for the sample. 
        If you are curious and would like to look at it, you can find it at:</p>
        <pre><%= Server.HtmlEncode(Server.MapPath("~/App_Data/errors.s3db")) %></pre>
        <p>There are several clients available for querying and administrating a
        SQLite database. If you don't have one handy, check out <a href="http://sqliteadmin.orbmu2k.de/">SQLite Administrator</a> (freeware).</p>
        <p>The sample could have just as easily used Access or VistaDB as its database, 
        logged to XML files or even memory and it still could have shipped as is with just
        a couple of changes to the <code>web.config</code> file.</p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="Email" />How does the sample send e-mail?</dt>
        <dd><p>You may be wondering how the sample sends e-mail when it requires
        no SMTP server or setup on your part. That's because it doesn't actually <em>send</em> any e-mail.
        Instead, it drops files with the raw e-mail message into a <a href="Mails/"
        title='<%= Server.HtmlEncode(MailPath) %>'>pick-up directory</a>
        where you can view them.
        </p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="Wiki" />How do I find out more about getting started?</dt>
        <dd><p>There are a few key places to go if you need more help getting started:</p>
        <ul>
            <li>Firstly, there's the <a href="http://code.google.com/p/elmah/">project home page</a>. 
            You've probably already been there, but it's a good place to start when looking for
            information.</li>
            <li>Next up is the <a href="http://code.google.com/p/elmah/w/list">project wiki pages</a>.
            There are several articles there which will help you set up ELMAH and also describe
            some of the more involved features.</li>
            <li>You should also look at the sample <code>web.config</code> file that ships with
            ELMAH. You should be able to find it in the following location: 
            <code><% =Server.HtmlEncode(SampleWebConfigPath)%></code>.
            This file contains lots of comments regarding how you can configure ELMAH for your
            environment.</li>
        </ul>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="Language" />I see the code is written in C#. Do I have to use that too?</dt>
        <dd><p>Not at all! You can use any .NET language for your development. Simply plug ELMAH into your
        application and make some configuration changes to <code>web.config</code> and you'll
        be up and running.</p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="NoSource" />What if I don't have the source code for an application?</dt>
        <dd><p>That doesn't matter. ELMAH requires no changes to an ASP.NET application's source code.
        Simply throw ELMAH into the
        application's <code>bin</code> and make some configuration changes to <code>web.config</code> and you'll
        be up and running.</p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="Signaling" />What exactly <em>is</em> signaling?</dt>
        <dd><p>Out of the box, ELMAH will intercept any <em>unhandled</em> exception.
        However, what happens when your application code handles the exception, yet
        want it logged as well?</p>
        <p>That's where signaling comes in. Inside your own error handling code, you can
        call ELMAH and ask it to log the error for you. This is done with code similar to this in C#:</p>
        <pre>    try
    {
        throw new Exception("Oops! I did it again.");
    }
    catch (Exception ex)
    {
        ErrorSignal.FromCurrentContext().Raise(ex);            
    }
</pre>
        <p>or this if you are working in VB.NET:</p>
        <pre>    Try
        Throw New Exception("Oops! I did it again.")
    Catch ex As Exception
        ErrorSignal.FromCurrentContext().Raise(ex)
    End Try
</pre>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="CustomErrors"/>What happens if I turn on custom error handling?</dt>
        <dd><p>It makes no difference to ELMAH! ELMAH still catches the unhandled exception
        and logs it for you. Custom error handling will then kick in and show a user-friendly
        error page instead of the <em><a href="http://en.wikipedia.org/wiki/Yellow_Screen_of_Death#ASP.NET">yellow screen of death</a></em>.</p>
        <p><a href="#Questions">Back to top</a></p></dd>
        <dt><a name="MediumTrustSupport"></a>Can I use ELMAH in a medium trust application?</dt>
        <dd><p>Yes, medium trust is fully supported by ELMAH as long as you use 
        <code>SqlErrorLog</code> (Microsoft SQL Server) or <code>VistaDBErrorLog</code> (<a href="http://www.vistadb.net/">VistaDB</a>)
        as the log implementations.</p></dd>
    </dl>   
    </form>
    <hr />
    <p>        
        Updated: <%= Server.HtmlEncode(Stamp.LastChanged.ToString("f")) %>
        (revision <a href='<%= string.Format(RevisionDetailUrlFormat, Stamp.Revision) %>'><%= Server.HtmlEncode(Stamp.Revision.ToString()) %></a>)
    </p>
</body>
</html>

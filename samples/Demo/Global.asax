<%@ Application Language="C#" %>
<%@ Import Namespace="Elmah" %>

<script runat="server">

    // ReSharper disable InconsistentNaming
   
    void ErrorMail_Filtering(object sender, ExceptionFilterEventArgs e)
    {
        if (MailSetup.SuppressesErrorMailing) 
            e.Dismiss();
    }
    
    // ReSharper restore InconsistentNaming
       
</script>

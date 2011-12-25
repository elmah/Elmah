<%@ Page Language="C#" Title="Error Log Sample Using ObjectDataSource and GridView" EnableViewState="true" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
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
        td 
        {
            vertical-align: top;
        }
        .error-table 
        {
            width: 100%;
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
        ELMAH supplies a class <code>Elmah.ErrorLogDataSourceAdapter</code> that is ready
        to be used with the <code><a href="http://msdn.microsoft.com/en-us/library/system.web.ui.webcontrols.objectdatasource.aspx">ObjectDataSource</a></code>
        control from ASP.NET. This sample uses the two together with a 
        <code><a href="http://msdn.microsoft.com/en-us/library/system.web.ui.webcontrols.gridview.aspx">GridView</a></code> control
        to create a custom presentation of the error log, all in server-side markup 
        and without a single line of code! 
        If you are using .NET Framework 3.5 then you may want to consider the 
        <code><a href="http://msdn.microsoft.com/en-us/library/system.web.ui.webcontrols.listview.aspx">ListView</a></code> 
        control in lieu of <code>GridView</code> since the former does not require 
        view state for paging.
    </p>
    <form runat="server">
    <asp:GridView class="error-table" ID="GridView1" runat="server" 
        AllowPaging="True" AutoGenerateColumns="False"
        DataSourceID="ErrorLogDataSource" CellPadding="4" ForeColor="#333333" 
        GridLines="None">
        <Columns>
            <asp:TemplateField HeaderText="Host" ItemStyle-Wrap="False">
                <ItemTemplate><%# Server.HtmlEncode(Eval("Error.HostName").ToString()) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Code" ItemStyle-Wrap="False">
                <ItemTemplate><%# Server.HtmlEncode(Eval("Error.StatusCode").ToString()) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Type" ItemStyle-Wrap="False">
                <ItemTemplate>
                    <span title="<%# Server.HtmlEncode(Eval("Error.Type").ToString()) %>"><%# 
                        Server.HtmlEncode(Elmah.ErrorDisplay.HumaneExceptionErrorType(Eval("Error.Type").ToString())) %></span>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Message">
                <ItemTemplate>
                    <%# Server.HtmlEncode(Eval("Error.Message").ToString()) %>
                    <asp:HyperLink runat="server" Text="More&hellip;" NavigateUrl='<%# "~/elmah.axd/detail?id=" + Eval("Id") %>' />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="User" ItemStyle-Wrap="False">
                <ItemTemplate><%# Server.HtmlEncode(Eval("Error.User").ToString())%></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Date" ItemStyle-Wrap="False">
                <ItemTemplate><%# Server.HtmlEncode(Eval("Error.Time", "{0:d}"))%></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Time" ItemStyle-Wrap="False">
                <ItemTemplate><%# Server.HtmlEncode(Eval("Error.Time", "{0:t}"))%></ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <PagerSettings Position="TopAndBottom" />
        <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
        <RowStyle BackColor="#E3EAEB" />
        <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
        <SelectedRowStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
        <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
        <EditRowStyle BackColor="#7C6F57" />
        <AlternatingRowStyle BackColor="White" />
        <EmptyDataRowStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
        <EmptyDataTemplate>
            No exceptions have been logged in ELMAH.
        </EmptyDataTemplate>
    </asp:GridView>
    <asp:ObjectDataSource ID="ErrorLogDataSource" runat="server" EnablePaging="True"
        TypeName="Elmah.ErrorLogDataSourceAdapter" 
        SelectMethod="GetErrors" SelectCountMethod="GetErrorCount" />
    </form>
    <!-- $Id: ErrorsGridView.aspx 571 2009-05-11 14:45:56Z azizatif $ -->
</body>
</html>

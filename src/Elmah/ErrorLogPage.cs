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

[assembly: Elmah.Scc("$Id: ErrorLogPage.cs 776 2011-01-12 21:09:24Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Collections.Generic;

    using CultureInfo = System.Globalization.CultureInfo;

    #endregion

    /// <summary>
    /// Renders an HTML page displaying a page of errors from the error log.
    /// </summary>

    internal sealed class ErrorLogPage : ErrorPageBase
    {
        private int _pageIndex;
        private int _pageSize; 
        private int _totalCount;
        private List<ErrorLogEntry> _errorEntryList;
        
        private const int _defaultPageSize = 15;
        private const int _maximumPageSize = 100;

        protected override void OnLoad(EventArgs e)
        {
            //
            // Get the page index and size parameters within their bounds.
            //

            _pageSize = Convert.ToInt32(this.Request.QueryString["size"], CultureInfo.InvariantCulture);
            _pageSize = Math.Min(_maximumPageSize, Math.Max(0, _pageSize));

            if (_pageSize == 0)
            {
                _pageSize = _defaultPageSize;
            }

            _pageIndex = Convert.ToInt32(this.Request.QueryString["page"], CultureInfo.InvariantCulture);
            _pageIndex = Math.Max(1, _pageIndex) - 1;

            //
            // Read the error records.
            //

            _errorEntryList = new List<ErrorLogEntry>(_pageSize);
            _totalCount = this.ErrorLog.GetErrors(_pageIndex, _pageSize, _errorEntryList);

            //
            // Set the title of the page.
            //

            string hostName = Environment.TryGetMachineName(Context);
            this.PageTitle = string.Format(
                hostName.Length > 0 
                ? "Error log for {0} on {2} (Page #{1})" 
                : "Error log for {0} (Page #{1})", 
                this.ApplicationName, (_pageIndex + 1).ToString("N0"), hostName);

            base.OnLoad(e);
        }

        protected override void RenderHead(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            base.RenderHead(writer);

            //
            // Write a <link> tag to relate the RSS feed.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Rel, HtmlLinkType.Alternate);
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "application/rss+xml");
            writer.AddAttribute(HtmlTextWriterAttribute.Title, "RSS");
            writer.AddAttribute(HtmlTextWriterAttribute.Href, this.BasePageName + "/rss");
            writer.RenderBeginTag(HtmlTextWriterTag.Link);
            writer.RenderEndTag();
            writer.WriteLine();

            //
            // If on the first page, then enable auto-refresh every minute
            // by issuing the following markup:
            //
            //      <meta http-equiv="refresh" content="60">
            //

            if (_pageIndex == 0)
            {
                writer.AddAttribute("http-equiv", "refresh");
                writer.AddAttribute("content", "60");
                writer.RenderBeginTag(HtmlTextWriterTag.Meta);
                writer.RenderEndTag();
                writer.WriteLine();
            }
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            //
            // Write out the page title and speed bar in the body.
            //

            RenderTitle(writer);

            SpeedBar.Render(writer, 
                SpeedBar.RssFeed.Format(BasePageName),
                SpeedBar.RssDigestFeed.Format(BasePageName),
                SpeedBar.DownloadLog.Format(BasePageName),
                SpeedBar.Help,
                SpeedBar.About.Format(BasePageName));
            
            if (_errorEntryList.Count != 0)
            {
                //
                // Write error number range displayed on this page and the
                // total available in the log, followed by stock
                // page sizes.
                //

                writer.RenderBeginTag(HtmlTextWriterTag.P);

                RenderStats(writer);
                RenderStockPageSizes(writer);
                
                writer.RenderEndTag(); // </p>
                writer.WriteLine();

                //
                // Write out the main table to display the errors.
                //

                RenderErrors(writer);

                //
                // Write out page navigation links.
                //

                RenderPageNavigators(writer);
            }
            else
            {
                //
                // No errors found in the log, so display a corresponding
                // message.
                //

                RenderNoErrors(writer);
            }

            base.RenderContents(writer);
        }

        private void RenderPageNavigators(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            //
            // If not on the last page then render a link to the next page.
            //
    
            writer.RenderBeginTag(HtmlTextWriterTag.P);
    
            int nextPageIndex = _pageIndex + 1;
            bool moreErrors = nextPageIndex * _pageSize < _totalCount;
    
            if (moreErrors)
                RenderLinkToPage(writer, HtmlLinkType.Next, "Next errors", nextPageIndex);

            //
            // If not on the first page then render a link to the firs page.
            //
    
            if (_pageIndex > 0 && _totalCount > 0)
            {
                if (moreErrors)
                    writer.Write("; ");

                RenderLinkToPage(writer, HtmlLinkType.Start, "Back to first page", 0);
            }
    
            writer.RenderEndTag(); // </p>
            writer.WriteLine();
        }

        private void RenderStockPageSizes(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            //
            // Write out a set of stock page size choices. Note that
            // selecting a stock page size re-starts the log 
            // display from the first page to get the right paging.
            //
    
            writer.Write("Start with ");
    
            int[] stockSizes = new int[] { 10, 15, 20, 25, 30, 50, 100 };
    
            for (int stockSizeIndex = 0; stockSizeIndex < stockSizes.Length; stockSizeIndex++)
            {
                int stockSize = stockSizes[stockSizeIndex];

                if (stockSizeIndex > 0)
                    writer.Write(stockSizeIndex + 1 < stockSizes.Length ? ", " : " or ");
                    
                RenderLinkToPage(writer, HtmlLinkType.Start, stockSize.ToString(), 0, stockSize);
            }
    
            writer.Write(" errors per page.");
        }

        private void RenderStats(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);
            
            int firstErrorNumber = _pageIndex * _pageSize + 1;
            int lastErrorNumber = firstErrorNumber + _errorEntryList.Count - 1;
            int totalPages = (int) Math.Ceiling((double) _totalCount / _pageSize);
    
            writer.Write("Errors {0} to {1} of total {2} (page {3} of {4}). ",
                         firstErrorNumber.ToString("N0"), 
                         lastErrorNumber.ToString("N0"),
                         _totalCount.ToString("N0"),
                         (_pageIndex + 1).ToString("N0"),
                         totalPages.ToString("N0"));
        }

        private void RenderTitle(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            //
            // If the application name matches the APPL_MD_PATH then its
            // of the form /LM/W3SVC/.../<name>. In this case, use only the 
            // <name> part to reduce the noise. The full application name is 
            // still made available through a tooltip.
            //

            string simpleName = this.ApplicationName;

            if (string.Compare(simpleName, this.Request.ServerVariables["APPL_MD_PATH"], 
                true, CultureInfo.InvariantCulture) == 0)
            {
                int lastSlashIndex = simpleName.LastIndexOf('/');

                if (lastSlashIndex > 0)
                    simpleName = simpleName.Substring(lastSlashIndex + 1);
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "PageTitle");
            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            writer.Write("Error Log for ");

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ApplicationName");
            writer.AddAttribute(HtmlTextWriterAttribute.Title, this.Server.HtmlEncode(this.ApplicationName));
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            Server.HtmlEncode(simpleName, writer);
            string hostName = Environment.TryGetMachineName(Context);
            if (hostName.Length > 0)
            {
                writer.Write(" on ");
                Server.HtmlEncode(hostName, writer);
            }
            writer.RenderEndTag(); // </span>

            writer.RenderEndTag(); // </h1>
            writer.WriteLine();
        }

        private void RenderNoErrors(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            writer.RenderBeginTag(HtmlTextWriterTag.P);

            writer.Write("No errors found. ");

            //
            // It is possible that there are no error at the requested 
            // page in the log (especially if it is not the first page).
            // However, if there are error in the log
            //
            
            if (_pageIndex > 0 && _totalCount > 0)
            {
                RenderLinkToPage(writer, HtmlLinkType.Start, "Go to first page", 0);
                writer.Write(". ");
            }

            writer.RenderEndTag();
            writer.WriteLine();
        }

        private void RenderErrors(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            //
            // Create a table to display error information in each row.
            //

            Table table = new Table();
            table.ID = "ErrorLog";
            table.CellSpacing = 0;

            //
            // Create the table row for headings.
            //
            
            TableRow headRow = new TableRow();

            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Host", "host-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Code", "code-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Type", "type-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Error", "error-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "User", "user-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Date", "date-col"));
            headRow.Cells.Add(FormatCell(new TableHeaderCell(), "Time", "time-col"));

            table.Rows.Add(headRow);

            //
            // Generate a table body row for each error.
            //

            for (int errorIndex = 0; errorIndex < _errorEntryList.Count; errorIndex++)
            {
                ErrorLogEntry errorEntry = (ErrorLogEntry) _errorEntryList[errorIndex];
                Error error = errorEntry.Error;

                TableRow bodyRow = new TableRow();
                bodyRow.CssClass = errorIndex % 2 == 0 ? "even-row" : "odd-row";

                //
                // Format host and status code cells.
                //

                bodyRow.Cells.Add(FormatCell(new TableCell(), error.HostName, "host-col"));
                bodyRow.Cells.Add(FormatCell(new TableCell(), error.StatusCode.ToString(), "code-col", HttpWorkerRequest.GetStatusDescription(error.StatusCode) ?? string.Empty));
                bodyRow.Cells.Add(FormatCell(new TableCell(), ErrorDisplay.HumaneExceptionErrorType(error), "type-col", error.Type));
                    
                //
                // Format the message cell, which contains the message 
                // text and a details link pointing to the page where
                // all error details can be viewed.
                //

                TableCell messageCell = new TableCell();
                messageCell.CssClass = "error-col";

                Label messageLabel = new Label();
                messageLabel.Text = this.Server.HtmlEncode(error.Message);

                HyperLink detailsLink = new HyperLink();
                detailsLink.NavigateUrl = BasePageName + "/detail?id=" + HttpUtility.UrlEncode(errorEntry.Id);
                detailsLink.Text = "Details&hellip;";

                messageCell.Controls.Add(messageLabel);
                messageCell.Controls.Add(new LiteralControl(" "));
                messageCell.Controls.Add(detailsLink);

                bodyRow.Cells.Add(messageCell);

                //
                // Format the user, date and time cells.
                //
                    
                bodyRow.Cells.Add(FormatCell(new TableCell(), error.User, "user-col"));
                bodyRow.Cells.Add(FormatCell(new TableCell(), error.Time.ToShortDateString(), "date-col", 
                    error.Time.ToLongDateString()));
                bodyRow.Cells.Add(FormatCell(new TableCell(), error.Time.ToShortTimeString(), "time-col",
                    error.Time.ToLongTimeString()));

                //
                // Finally, add the row to the table.
                //

                table.Rows.Add(bodyRow);
            }

            table.RenderControl(writer);
        }

        private TableCell FormatCell(TableCell cell, string contents, string cssClassName)
        {
            return FormatCell(cell, contents, cssClassName, string.Empty);
        }

        private TableCell FormatCell(TableCell cell, string contents, string cssClassName, string toolTip)
        {
            Debug.Assert(cell != null);
            Debug.AssertStringNotEmpty(cssClassName);

            cell.Wrap = false;
            cell.CssClass = cssClassName;

            if (contents.Length == 0)
            {
                cell.Text = "&nbsp;";
            }
            else
            {
                string encodedContents = this.Server.HtmlEncode(contents);
                
                if (toolTip.Length == 0)
                {
                    cell.Text = encodedContents;
                }
                else
                {
                    Label label = new Label();
                    label.ToolTip = toolTip;
                    label.Text = encodedContents;
                    cell.Controls.Add(label);
                }
            }

            return cell;
        }

        private void RenderLinkToPage(HtmlTextWriter writer, string type, string text, int pageIndex)
        {
            RenderLinkToPage(writer, type, text, pageIndex, _pageSize);
        }

        private void RenderLinkToPage(HtmlTextWriter writer, string type, string text, int pageIndex, int pageSize)
        {
            Debug.Assert(writer != null);
            Debug.Assert(text != null);
            Debug.Assert(pageIndex >= 0);
            Debug.Assert(pageSize >= 0);

            string href = string.Format("{0}?page={1}&size={2}",
                BasePageName,
                (pageIndex + 1).ToString(CultureInfo.InvariantCulture),
                pageSize.ToString(CultureInfo.InvariantCulture));

            writer.AddAttribute(HtmlTextWriterAttribute.Href, href);

            if (type != null && type.Length > 0)
                writer.AddAttribute(HtmlTextWriterAttribute.Rel, type);
            
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            this.Server.HtmlEncode(text, writer);
            writer.RenderEndTag();
        }   
    }
}

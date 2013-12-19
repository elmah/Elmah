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

[assembly: Elmah.Scc("$Id: SqlErrorLog.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Threading;
    #if !NET_3_5
    using System.Threading.Tasks;
    #endif
    using System.Xml;

    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft SQL 
    /// Server 2000 as its backing store.
    /// </summary>
    
    public class SqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SqlErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            var connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQL error log.");

            _connectionString = connectionString;

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

            var appName = config.Find("applicationName", string.Empty);

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public SqlErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");
            
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "Microsoft SQL Server Error Log"; }
        }

        /// <summary>
        /// Gets the connection string used by the log to connect to the database.
        /// </summary>
        
        public virtual string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Logs an error to the database.
        /// </summary>
        /// <remarks>
        /// Use the stored procedure called by this implementation to set a
        /// policy on how long errors are kept in the log. The default
        /// implementation stores all errors for an indefinite time.
        /// </remarks>

        public override string Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            var errorXml = ErrorXml.EncodeString(error);
            var id = Guid.NewGuid();

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = Commands.LogError(
                id, ApplicationName, 
                error.HostName, error.Type, error.Source, error.Message, error.User,
                error.StatusCode, error.Time.ToUniversalTime(), errorXml))
            {
                command.Connection = connection;
                connection.Open();
                command.ExecuteNonQuery();
                return id.ToString();
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = Commands.GetErrorsXml(ApplicationName, pageIndex, pageSize))
            {
                command.Connection = connection;
                connection.Open();

                var xml = ReadSingleXmlStringResult(command.ExecuteReader());
                ErrorsXmlToList(xml, errorEntryList);

                int total;
                Commands.GetErrorsXmlOutputs(command, out total);
                return total;
            }
        }

        private static string ReadSingleXmlStringResult(SqlDataReader reader)
        {
            using (reader)
            {
                if (!reader.Read())
                    return null;

                //
                // See following MS KB article why the XML string is read 
                // and reconstructed in chunks:
                //
                // The XML data row is truncated at 2,033 characters when you use the SqlDataReader object
                // http://support.microsoft.com/kb/310378
                // 
                // When you read XML data from Microsoft SQL Server by using 
                // the SqlDataReader object, the XML in the first column of 
                // the first row is truncated at 2,033 characters. You 
                // expect all of the contents of the XML data to be 
                // contained in a single row and column. This behavior 
                // occurs because, for XML results greater than 2,033 
                // characters in length, SQL Server returns the XML in 
                // multiple rows of 2,033 characters each. 
                //
                // See also comment 18 in issue 129:
                // http://code.google.com/p/elmah/issues/detail?id=129#c18
                //

                var sb = new StringBuilder(/* capacity */ 2033);
                do { sb.Append(reader.GetString(0)); } while (reader.Read());
                return sb.ToString();
            }
        }
        
        #if !NET_3_5 && !NET_4_0 // i.e. Microsoft .NET Framework 4.5 and later

        /// <summary>
        /// Asynchronous version of <see cref="GetErrors"/>.
        /// </summary>

        public async override Task<int> GetErrorsAsync(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList, CancellationToken cancellationToken)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            var csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                AsynchronousProcessing = true
            };

            using (var connection = new SqlConnection(csb.ConnectionString))
            using (var command = Commands.GetErrorsXml(ApplicationName, pageIndex, pageSize))
            {
                command.Connection = connection;
                await connection.OpenAsync(cancellationToken);

                //
                // See following MS KB article why the XML string is read 
                // and reconstructed in chunks:
                //
                // The XML data row is truncated at 2,033 characters when you use the SqlDataReader object
                // http://support.microsoft.com/kb/310378
                // 
                // When you read XML data from Microsoft SQL Server by using 
                // the SqlDataReader object, the XML in the first column of 
                // the first row is truncated at 2,033 characters. You 
                // expect all of the contents of the XML data to be 
                // contained in a single row and column. This behavior 
                // occurs because, for XML results greater than 2,033 
                // characters in length, SQL Server returns the XML in 
                // multiple rows of 2,033 characters each. 
                //
                // See also comment 18 in issue 129:
                // http://code.google.com/p/elmah/issues/detail?id=129#c18
                //

                List<string> chunks = null;
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    while (await reader.ReadAsync(cancellationToken))
                        (chunks ?? (chunks = new List<string>())).Add(reader.GetString(0));

                var xml = chunks != null && chunks.Count > 0 ? string.Join(null, chunks) : null;
                ErrorsXmlToList(xml, errorEntryList);

                int total;
                Commands.GetErrorsXmlOutputs(command, out total);
                return total;
            }
        }

        #else // Microsoft .NET Framework 3.5 or 4.0

        #if NET_4_0

        /// <summary>
        /// Asynchronous version of <see cref="GetErrors"/>.
        /// </summary>

        public override Task<int> GetErrorsAsync(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList, CancellationToken cancellationToken)
        {
            return Task.Factory.FromAsync<int, int, ICollection<ErrorLogEntry>, int>(
                       BeginGetErrors, EndGetErrors, 
                       pageIndex, pageSize, errorEntryList, null);
        }

        #endif

        /// <summary>
        /// Begins an asynchronous version of <see cref="GetErrors"/>.
        /// </summary>

        public override IAsyncResult BeginGetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList, AsyncCallback asyncCallback, object asyncState)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            //
            // Modify the connection string on the fly to support async 
            // processing otherwise the asynchronous methods on the
            // SqlCommand will throw an exception. This ensures the
            // right behavior regardless of whether configured
            // connection string sets the Async option to true or not.
            //

            var csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                AsynchronousProcessing = true
            };
            var connection = new SqlConnection(csb.ConnectionString);

            //
            // Create the command object with input parameters initialized
            // and setup to call the stored procedure.
            //

            var command = Commands.GetErrorsXml(ApplicationName, pageIndex, pageSize);
            command.Connection = connection;

            //
            // Create a closure to handle the ending of the async operation
            // and retrieve results.
            //

            AsyncResultWrapper asyncResult = null;

            Func<IAsyncResult, int> endHandler = delegate
            {
                Debug.Assert(asyncResult != null);

                using (connection)
                using (command)
                {
                    var xml = ReadSingleXmlStringResult(command.EndExecuteReader(asyncResult.InnerResult));
                    ErrorsXmlToList(xml, errorEntryList);
                    int total;
                    Commands.GetErrorsXmlOutputs(command, out total);
                    return total;
                }
            };

            //
            // Open the connenction and execute the command asynchronously,
            // returning an IAsyncResult that wrap the downstream one. This
            // is needed to be able to send our own AsyncState object to
            // the downstream IAsyncResult object. In order to preserve the
            // one sent by caller, we need to maintain and return it from
            // our wrapper.
            //

            try
            {
                connection.Open();

                asyncResult = new AsyncResultWrapper(
                    command.BeginExecuteReader(
                        asyncCallback != null ? /* thunk */ delegate { asyncCallback(asyncResult); } : (AsyncCallback) null, 
                        endHandler), asyncState);

                return asyncResult;
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        #endif // NET_3_5 || NET_4_0

        private void ErrorsXmlToList(string xml, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (xml == null || xml.Length == 0) 
                return;

            var settings = new XmlReaderSettings();
            settings.CheckCharacters = false;
            settings.ConformanceLevel = ConformanceLevel.Fragment;

            using (var reader = XmlReader.Create(new StringReader(xml), settings))
                ErrorsXmlToList(reader, errorEntryList);
        }

        /// <summary>
        /// Ends an asynchronous version of <see cref="ErrorLog.GetErrors"/>.
        /// </summary>

        public override int EndGetErrors(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            var wrapper = asyncResult as AsyncResultWrapper;

            if (wrapper == null)
                throw new ArgumentException("Unexepcted IAsyncResult type.", "asyncResult");

            var endHandler = (Func<IAsyncResult, int>) wrapper.InnerResult.AsyncState;
            return endHandler(wrapper.InnerResult);
        }

        private void ErrorsXmlToList(XmlReader reader, ICollection<ErrorLogEntry> errorEntryList)
        {
            Debug.Assert(reader != null);

            if (errorEntryList != null)
            {
                while (reader.IsStartElement("error"))
                {
                    var id = reader.GetAttribute("errorId");
                    var error = ErrorXml.Decode(reader);
                    errorEntryList.Add(new ErrorLogEntry(this, id, error));
                }
            }
        }

        /// <summary>
        /// Returns the specified error from the database, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (id.Length == 0) throw new ArgumentException(null, "id");

            Guid errorGuid;

            try
            {
                errorGuid = new Guid(id);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml;

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = Commands.GetErrorXml(ApplicationName, errorGuid))
            {
                command.Connection = connection;
                connection.Open();
                errorXml = (string) command.ExecuteScalar();
            }

            if (errorXml == null)
                return null;

            var error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        private static class Commands
        {
            public static SqlCommand LogError(
                Guid id,
                string appName,
                string hostName,
                string typeName,
                string source,
                string message,
                string user,
                int statusCode,
                DateTime time,
                string xml)
            {
                var command = new SqlCommand("ELMAH_LogError");
                command.CommandType = CommandType.StoredProcedure;

                var parameters = command.Parameters;

                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;
                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@Host", SqlDbType.NVarChar, 30).Value = hostName;
                parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = typeName;
                parameters.Add("@Source", SqlDbType.NVarChar, 60).Value = source;
                parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = message;
                parameters.Add("@User", SqlDbType.NVarChar, 50).Value = user;
                parameters.Add("@AllXml", SqlDbType.NVarChar, -1).Value = xml;
                parameters.Add("@StatusCode", SqlDbType.Int).Value = statusCode;
                parameters.Add("@TimeUtc", SqlDbType.DateTime).Value = time;

                return command;
            }

            public static SqlCommand GetErrorXml(string appName, Guid id)
            {
                var command = new SqlCommand("ELMAH_GetErrorXml") { CommandType = CommandType.StoredProcedure };

                var parameters = command.Parameters;
                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;

                return command;
            }

            public static SqlCommand GetErrorsXml(string appName, int pageIndex, int pageSize)
            {
                var command = new SqlCommand("ELMAH_GetErrorsXml") { CommandType = CommandType.StoredProcedure };

                var parameters = command.Parameters;

                parameters.Add("@Application", SqlDbType.NVarChar, _maxAppNameLength).Value = appName;
                parameters.Add("@PageIndex", SqlDbType.Int).Value = pageIndex;
                parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                parameters.Add("@TotalCount", SqlDbType.Int).Direction = ParameterDirection.Output;

                return command;
            }

            public static void GetErrorsXmlOutputs(SqlCommand command, out int totalCount)
            {
                Debug.Assert(command != null);

                totalCount = (int) command.Parameters["@TotalCount"].Value;
            }
        }

        /// <summary>
        /// An <see cref="IAsyncResult"/> implementation that wraps another.
        /// </summary>

        private sealed class AsyncResultWrapper : IAsyncResult
        {
            public AsyncResultWrapper(IAsyncResult inner, object asyncState)
            {
                InnerResult = inner;
                AsyncState = asyncState;
            }

            public IAsyncResult InnerResult { get; private set; }

            public bool IsCompleted
            {
                get { return InnerResult.IsCompleted; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return InnerResult.AsyncWaitHandle; }
            }

            public object AsyncState { get; private set; }

            public bool CompletedSynchronously
            {
                get { return InnerResult.CompletedSynchronously; }
            }
        }
    }
}

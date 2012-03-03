#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
//      with contributions from Hath1
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

// All code in this file requires .NET Framework 1.1 or later.

#if !NET_1_0

[assembly: Elmah.Scc("$Id: OracleErrorLog.cs 907 2011-12-18 13:03:58Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Data.OracleClient;
    using System.IO;
    using System.Text;

    using IDictionary = System.Collections.IDictionary;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Oracle as its backing store.
    /// </summary>

    public class OracleErrorLog : ErrorLog
    {
        private readonly string _connectionString;
        private string _schemaOwner;
        private bool _schemaOwnerInitialized;

        private const int _maxAppNameLength = 60;
        private const int _maxSchemaNameLength = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public OracleErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the Oracle error log.");

            _connectionString = connectionString;

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

            string appName = Mask.NullString((string)config["applicationName"]);

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;

            SchemaOwner = (string)config["schemaOwner"];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public OracleErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string for connecting to the database and
        /// a specific schema owner.
        /// </summary>

        public OracleErrorLog(string connectionString, string schemaOwner)
            : this(connectionString)
        {
            SchemaOwner = schemaOwner;
        }

        /// <summary>
        /// Gets the name of the schema owner where the errors are being stored.
        /// </summary>

        public string SchemaOwner
        {
            get { return Mask.NullString(_schemaOwner); }

            set
            {
                if (_schemaOwnerInitialized)
                    throw new InvalidOperationException("The schema owner cannot be reset once initialized.");

                _schemaOwner = Mask.NullString(value);

                if (_schemaOwner.Length == 0)
                    return;

                if (_schemaOwner.Length > _maxSchemaNameLength)
                    throw new ApplicationException(string.Format(
                        "Oracle schema owner is too long. Maximum length allowed is {0} characters.",
                        _maxSchemaNameLength.ToString("N0")));
                
                _schemaOwner = _schemaOwner + ".";
                _schemaOwnerInitialized = true;
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "Oracle Error Log"; }
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

            string errorXml = ErrorXml.EncodeString(error);
            Guid id = Guid.NewGuid();

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                connection.Open();
                using (OracleTransaction transaction = connection.BeginTransaction())
                {
                    // because we are storing the XML data in a NClob, we need to jump through a few hoops!!
                    // so first we've got to operate within a transaction
                    command.Transaction = transaction;

                    // then we need to create a temporary lob on the database server
                    command.CommandText = "declare xx nclob; begin dbms_lob.createtemporary(xx, false, 0); :tempblob := xx; end;";
                    command.CommandType = CommandType.Text;

                    OracleParameterCollection parameters = command.Parameters;
                    parameters.Add("tempblob", OracleType.NClob).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    // now we can get a handle to the NClob
                    OracleLob xmlLob = (OracleLob)parameters[0].Value;
                    // create a temporary buffer in which to store the XML
                    byte[] tempbuff = Encoding.Unicode.GetBytes(errorXml);
                    // and finally we can write to it!
                    xmlLob.BeginBatch(OracleLobOpenMode.ReadWrite);
                    xmlLob.Write(tempbuff,0,tempbuff.Length);
                    xmlLob.EndBatch();

                    command.CommandText = SchemaOwner + "pkg_elmah$log_error.LogError";
                    command.CommandType = CommandType.StoredProcedure;

                    parameters.Clear();
                    parameters.Add("v_ErrorId", OracleType.NVarChar, 32).Value = id.ToString("N");
                    parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                    parameters.Add("v_Host", OracleType.NVarChar, 30).Value = error.HostName;
                    parameters.Add("v_Type", OracleType.NVarChar, 100).Value = error.Type;
                    parameters.Add("v_Source", OracleType.NVarChar, 60).Value = error.Source;
                    parameters.Add("v_Message", OracleType.NVarChar, 500).Value = error.Message;
                    parameters.Add("v_User", OracleType.NVarChar, 50).Value = error.User;
                    parameters.Add("v_AllXml", OracleType.NClob).Value = xmlLob;
                    parameters.Add("v_StatusCode", OracleType.Int32).Value = error.StatusCode;
                    parameters.Add("v_TimeUtc", OracleType.DateTime).Value = error.Time.ToUniversalTime();

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
                return id.ToString();
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                command.CommandText = SchemaOwner + "pkg_elmah$get_error.GetErrorsXml";
                command.CommandType = CommandType.StoredProcedure;

                OracleParameterCollection parameters = command.Parameters;

                parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("v_PageIndex", OracleType.Int32).Value = pageIndex;
                parameters.Add("v_PageSize", OracleType.Int32).Value = pageSize;
                parameters.Add("v_TotalCount", OracleType.Int32).Direction = ParameterDirection.Output;
                parameters.Add("v_Results", OracleType.Cursor).Direction = ParameterDirection.Output;

                connection.Open();

                using (OracleDataReader reader = command.ExecuteReader())
                {
                    Debug.Assert(reader != null);

                    if (errorEntryList != null)
                    {
                        while (reader.Read())
                        {
                            string id = reader["ErrorId"].ToString();
                            Guid guid = new Guid(id);

                            Error error = new Error();

                            error.ApplicationName = reader["Application"].ToString();
                            error.HostName = reader["Host"].ToString();
                            error.Type = reader["Type"].ToString();
                            error.Source = reader["Source"].ToString();
                            error.Message = reader["Message"].ToString();
                            error.User = reader["UserName"].ToString();
                            error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                            error.Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime();

                            errorEntryList.Add(new ErrorLogEntry(this, guid.ToString(), error));
                        }
                    }
                    reader.Close();
                }

                return (int)command.Parameters["v_TotalCount"].Value;
            }
        }

        /// <summary>
        /// Returns the specified error from the database, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");

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

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                command.CommandText = SchemaOwner + "pkg_elmah$get_error.GetErrorXml";
                command.CommandType = CommandType.StoredProcedure;

                OracleParameterCollection parameters = command.Parameters;
                parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("v_ErrorId", OracleType.NVarChar, 32).Value = errorGuid.ToString("N");
                parameters.Add("v_AllXml", OracleType.NClob).Direction = ParameterDirection.Output;

                connection.Open();
                command.ExecuteNonQuery();
                OracleLob xmlLob = (OracleLob)command.Parameters["v_AllXml"].Value;

                StreamReader streamreader = new StreamReader(xmlLob, Encoding.Unicode);
                char[] cbuffer = new char[1000];
                int actual;
                StringBuilder sb = new StringBuilder();
                while((actual = streamreader.Read(cbuffer, 0, cbuffer.Length)) >0)
                    sb.Append(cbuffer, 0, actual);
                errorXml = sb.ToString();
            }

            if (errorXml == null)
                return null;

            Error error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }
    }
}

#endif //!NET_1_0

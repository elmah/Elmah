#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
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

[assembly: Elmah.Scc("$Id: AccessErrorLog.cs 923 2011-12-23 22:02:10Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Data.OleDb;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using IDictionary = System.Collections.IDictionary;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft Access 
    /// as its backing store.
    /// </summary>
    /// <remarks>
    /// The MDB file is automatically created at the path specified in the 
    /// connection string if it does not already exist.
    /// </remarks>

    public class AccessErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;
        private const string _scriptResourceName = "mkmdb.vbs";

        private static readonly object _mdbInitializationLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public AccessErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the Access error log.");

            _connectionString = connectionString;

            InitializeDatabase();

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

            string appName = config.Find("applicationName", string.Empty);

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public AccessErrorLog(string connectionString)
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
            get { return "Microsoft Access Error Log"; }
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

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                connection.Open();

                command.CommandType = CommandType.Text;
                command.CommandText = @"INSERT INTO ELMAH_Error
                                            (Application, Host, Type, Source, 
                                            Message, UserName, StatusCode, TimeUtc, AllXml)
                                        VALUES
                                            (@Application, @Host, @Type, @Source, 
                                            @Message, @UserName, @StatusCode, @TimeUtc, @AllXml)";
                command.CommandType = CommandType.Text;

                OleDbParameterCollection parameters = command.Parameters;

                parameters.Add("@Application", OleDbType.VarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("@Host", OleDbType.VarChar, 30).Value = error.HostName;
                parameters.Add("@Type", OleDbType.VarChar, 100).Value = error.Type;
                parameters.Add("@Source", OleDbType.VarChar, 60).Value = error.Source;
                parameters.Add("@Message", OleDbType.LongVarChar, error.Message.Length).Value = error.Message;
                parameters.Add("@User", OleDbType.VarChar, 50).Value = error.User;
                parameters.Add("@StatusCode", OleDbType.Integer).Value = error.StatusCode;
                parameters.Add("@TimeUtc", OleDbType.Date).Value = error.Time.ToUniversalTime();
                parameters.Add("@AllXml", OleDbType.LongVarChar, errorXml.Length).Value = errorXml;
                
                command.ExecuteNonQuery();

                using (OleDbCommand identityCommand = connection.CreateCommand())
                {
                    identityCommand.CommandType = CommandType.Text;
                    identityCommand.CommandText = "SELECT @@IDENTITY";

                    return Convert.ToString(identityCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT COUNT(*) FROM ELMAH_Error";

                connection.Open();
                int totalCount = (int)command.ExecuteScalar();

                if (errorEntryList != null && pageIndex * pageSize < totalCount)
                {
                    int maxRecords = pageSize * (pageIndex + 1);
                    if (maxRecords > totalCount)
                    {
                        maxRecords = totalCount;
                        pageSize = totalCount - pageSize * (totalCount / pageSize);
                    }

                    StringBuilder sql = new StringBuilder(1000);
                    sql.Append("SELECT e.* FROM (");
                    sql.Append("SELECT TOP ");
                    sql.Append(pageSize.ToString(CultureInfo.InvariantCulture));
                    sql.Append(" TimeUtc, ErrorId FROM (");
                    sql.Append("SELECT TOP ");
                    sql.Append(maxRecords.ToString(CultureInfo.InvariantCulture));
                    sql.Append(" TimeUtc, ErrorId FROM ELMAH_Error ");
                    sql.Append("ORDER BY TimeUtc DESC, ErrorId DESC) ");
                    sql.Append("ORDER BY TimeUtc ASC, ErrorId ASC) AS i ");
                    sql.Append("INNER JOIN Elmah_Error AS e ON i.ErrorId = e.ErrorId ");
                    sql.Append("ORDER BY e.TimeUtc DESC, e.ErrorId DESC");

                    command.CommandText = sql.ToString();

                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        Debug.Assert(reader != null);

                        while (reader.Read())
                        {
                            var id = Convert.ToString(reader["ErrorId"], CultureInfo.InvariantCulture);

                            var error = new Error
                            {
                                ApplicationName = reader["Application"].ToString(),
                                HostName = reader["Host"].ToString(),
                                Type = reader["Type"].ToString(),
                                Source = reader["Source"].ToString(),
                                Message = reader["Message"].ToString(),
                                User = reader["UserName"].ToString(),
                                StatusCode = Convert.ToInt32(reader["StatusCode"]),
                                Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime()
                            };

                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                        }

                        reader.Close();
                    }
                }

                return totalCount;
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

            int errorId;
            try
            {
                errorId = int.Parse(id, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml;

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT   AllXml
                                        FROM     ELMAH_Error
                                        WHERE    ErrorId = @ErrorId";
                command.CommandType = CommandType.Text;

                OleDbParameterCollection parameters = command.Parameters;
                parameters.Add("@ErrorId", OleDbType.Integer).Value = errorId;

                connection.Open();
                errorXml = (string)command.ExecuteScalar();
            }

            if (errorXml == null)
                return null;

            Error error = ErrorXml.DecodeString(errorXml);
            return new ErrorLogEntry(this, id, error);
        }

        private void InitializeDatabase()
        {
            string connectionString = ConnectionString;
            Debug.AssertStringNotEmpty(connectionString);

            string dbFilePath = ConnectionStringHelper.GetDataSourceFilePath(connectionString);
            if (File.Exists(dbFilePath))
                return;

            //
            // Make sure that we don't have multiple instances trying to create the database.
            //

            lock (_mdbInitializationLock)
            {
                //
                // Just double-check that no other thread has created the database while
                // we were waiting for the lock.
                //

                if (File.Exists(dbFilePath))
                    return;

                //
                // Create a temporary copy of the mkmdb.vbs script.
                // We do this in the same directory as the resulting database for security permission purposes.
                //

                string scriptPath = Path.Combine(Path.GetDirectoryName(dbFilePath), _scriptResourceName);

                using (FileStream scriptStream = new FileStream(scriptPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    ManifestResourceHelper.WriteResourceToStream(scriptStream, _scriptResourceName);
                }

                //
                // Run the script file to create the database using batch 
                // mode (//B), which suppresses script errors and prompts 
                // from displaying.
                //

                ProcessStartInfo psi = new ProcessStartInfo(
                    "cscript", "\"" + scriptPath + "\" \"" + dbFilePath + "\" //B //NoLogo");
                
                psi.UseShellExecute = false;    // i.e. CreateProcess
                psi.CreateNoWindow = true;      // Stay lean, stay mean
                
                try
                {
                    using (Process process = Process.Start(psi))
                    {
                        //
                        // A few seconds should be plenty of time to create the database.
                        //

                        TimeSpan tolerance = TimeSpan.FromSeconds(2);
                        if (!process.WaitForExit((int) tolerance.TotalMilliseconds))
                        {
                            //
                            // but it wasn't, so clean up and throw an exception!
                            // Realistically, I don't expect to ever get here!
                            //

                            process.Kill();

                            throw new Exception(string.Format(
                                "The Microsoft Access database creation script took longer than the allocated time of {0} seconds to execute. "
                                + "The script was terminated prematurely.", 
                                tolerance.TotalSeconds));
                        }

                        if (process.ExitCode != 0)
                        {
                            throw new Exception(string.Format(
                                "The Microsoft Access database creation script failed with exit code {0}.",
                                process.ExitCode));
                        }
                    }
                }
                finally
                {
                    //
                    // Clean up after ourselves!!
                    //

                    File.Delete(scriptPath);
                }
            }
        }
    }
}

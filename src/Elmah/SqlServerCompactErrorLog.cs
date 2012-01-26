#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Erik Ejlskov Jensen, http://erikej.blogspot.com/
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

[assembly: Elmah.Scc("$Id: SqlServerCompactErrorLog.cs 925 2011-12-23 22:46:09Z azizatif $")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlServerCe;
    using System.IO;

    using IDictionary = System.Collections.IDictionary;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses SQL Server 
    /// Compact 4 as its backing store.
    /// </summary>

    public class SqlServerCompactErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerCompactErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SqlServerCompactErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = ConnectionStringHelper.GetConnectionString(config, true);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new Elmah.ApplicationException("Connection string is missing for the SQL Server Compact error log.");

            _connectionString = connectionString;

            InitializeDatabase();

            ApplicationName = (string) config["applicationName"] ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerCompactErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public SqlServerCompactErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = ConnectionStringHelper.GetResolvedConnectionString(connectionString);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string connectionString = ConnectionString;
            Debug.AssertStringNotEmpty(connectionString);

            string dbFilePath = ConnectionStringHelper.GetDataSourceFilePath(connectionString);
            if (File.Exists(dbFilePath))
                return;
            using (SqlCeEngine engine = new SqlCeEngine(ConnectionString))
            {
                engine.CreateDatabase();
            }

            using (SqlCeConnection conn = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand cmd = new SqlCeCommand())
                {
                    conn.Open();
                    SqlCeTransaction transaction = conn.BeginTransaction();
                    
                    try
                    {
                        cmd.Connection = conn;
                        cmd.Transaction = transaction;

                        cmd.CommandText = @"
                        SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ELMAH_Error'";

                        object obj = cmd.ExecuteScalar();
                        if (obj == null)
                        {
                            cmd.CommandText = @"
                            CREATE TABLE ELMAH_Error (
                                [ErrorId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT newid(),
                                [Application] NVARCHAR(60) NOT NULL,
                                [Host] NVARCHAR(50) NOT NULL,
                                [Type] NVARCHAR(100) NOT NULL,
                                [Source] NVARCHAR(60) NOT NULL,
                                [Message] NVARCHAR(500) NOT NULL,
                                [User] NVARCHAR(50) NOT NULL,
                                [StatusCode] INT NOT NULL,
                                [TimeUtc] DATETIME NOT NULL,
                                [Sequence] INT IDENTITY (1, 1) NOT NULL,
                                [AllXml] NTEXT NOT NULL
                            )";
                            cmd.ExecuteNonQuery();

                            cmd.CommandText = @"
                            CREATE NONCLUSTERED INDEX [IX_Error_App_Time_Seq] ON [ELMAH_Error] 
                            (
                                [Application]   ASC,
                                [TimeUtc]       DESC,
                                [Sequence]      DESC
                            )";
                            cmd.ExecuteNonQuery();
                        }
                        transaction.Commit(CommitMode.Immediate);
                    }
                    catch (SqlCeException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }


        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "SQL Server Compact Error Log"; }
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
            
            const string query = @"
                INSERT INTO ELMAH_Error (
                    [ErrorId], [Application], [Host], 
                    [Type], [Source], [Message], [User], [StatusCode], 
                    [TimeUtc], [AllXml] )
                VALUES (
                    @ErrorId, @Application, @Host, 
                    @Type, @Source, @Message, @User, @StatusCode, 
                    @TimeUtc, @AllXml);";

            using (SqlCeConnection connection = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand command = new SqlCeCommand(query, connection))
                {
                    SqlCeParameterCollection parameters = command.Parameters;

                    parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;
                    parameters.Add("@Application", SqlDbType.NVarChar, 60).Value = ApplicationName;
                    parameters.Add("@Host", SqlDbType.NVarChar, 30).Value = error.HostName;
                    parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = error.Type;
                    parameters.Add("@Source", SqlDbType.NVarChar, 60).Value = error.Source;
                    parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = error.Message;
                    parameters.Add("@User", SqlDbType.NVarChar, 50).Value = error.User;
                    parameters.Add("@StatusCode", SqlDbType.Int).Value = error.StatusCode;
                    parameters.Add("@TimeUtc", SqlDbType.DateTime).Value = error.Time.ToUniversalTime();
                    parameters.Add("@AllXml", SqlDbType.NText).Value = errorXml;

                    command.Connection = connection;
                    connection.Open();
                    command.ExecuteNonQuery();
                    return id.ToString();
                }
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>
        /// 

        public override int GetErrors(int pageIndex, int pageSize, ICollection<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            const string sql = @"
                SELECT
                    [ErrorId],
                    [Application],
                    [Host],
                    [Type],
                    [Source],
                    [Message],
                    [User],
                    [StatusCode],
                    [TimeUtc]
                FROM
                    [ELMAH_Error]
                ORDER BY
                    [TimeUtc] DESC, 
                    [Sequence] DESC
                OFFSET @PageSize * @PageIndex ROWS FETCH NEXT @PageSize ROWS ONLY;
                ";

                const string getCount = @"
                SELECT COUNT(*) FROM [ELMAH_Error]";

            using (SqlCeConnection connection = new SqlCeConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCeCommand command = new SqlCeCommand(sql, connection))
                {
                    SqlCeParameterCollection parameters = command.Parameters;

                    parameters.Add("@PageIndex", SqlDbType.Int).Value = pageIndex;
                    parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                    parameters.Add("@Application", SqlDbType.NVarChar, 60).Value = ApplicationName;


                    using (SqlCeDataReader reader = command.ExecuteReader())
                    {
                        if (errorEntryList != null)
                        {
                            while (reader.Read())
                            {
                                string id = reader["ErrorId"].ToString();

                                Elmah.Error error = new Elmah.Error();
                                error.ApplicationName = reader["Application"].ToString();
                                error.HostName = reader["Host"].ToString();
                                error.Type = reader["Type"].ToString();
                                error.Source = reader["Source"].ToString();
                                error.Message = reader["Message"].ToString();
                                error.User = reader["User"].ToString();
                                error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                                error.Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime();
                                errorEntryList.Add(new ErrorLogEntry(this, id, error));
                            }
                        }
                    }
                }

                using (SqlCeCommand command = new SqlCeCommand(getCount, connection))
                {
                    return (int)command.ExecuteScalar();
                }
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

            const string sql = @"
                SELECT 
                    [AllXml]
                FROM 
                    [ELMAH_Error]
                WHERE
                    [ErrorId] = @ErrorId";

            using (SqlCeConnection connection = new SqlCeConnection(ConnectionString))
            {
                using (SqlCeCommand command = new SqlCeCommand(sql, connection))
                {
                    command.Parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = errorGuid;

                    connection.Open();

                    string errorXml = (string)command.ExecuteScalar();

                    if (errorXml == null)
                        return null;

                    Error error = ErrorXml.DecodeString(errorXml);
                    return new ErrorLogEntry(this, id, error);
                }
            }
        }
    }
}

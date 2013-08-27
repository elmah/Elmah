/*
  
   ELMAH - Error Logging Modules and Handlers for ASP.NET
   Copyright (c) 2004-9 Atif Aziz. All rights reserved.
  
    Author(s):
  
      James Driscoll, mailto:jamesdriscoll@btinternet.com
  
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
  
      http://www.apache.org/licenses/LICENSE-2.0
  
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
  
*/

-- $Id: Oracle.sql 568 2009-05-11 14:18:34Z azizatif $

-- NB This script assumes you have logged on in the schema where you want to create the ELMAH objects

-- create a sequence for the errors (user to simulate an identity in SQL Server)
CREATE SEQUENCE elmah$error_seq START WITH 1 INCREMENT BY 1 NOMAXVALUE NOCYCLE NOCACHE NOORDER;

-- create the table to store the data
-- you can optionally specify tablespaces here too!
CREATE TABLE elmah$error
(
    -- if using Oracle 10g and above you can add DEFAULT SYS_GUID() 
    -- to the errorid definition.
    -- Oracle 8i doesn't like it with an NVARCHAR2
    -- haven't tested it against 9i
    errorid         NVARCHAR2(32) NOT NULL,
    application     NVARCHAR2(60) NOT NULL,
    host            NVARCHAR2(30) NOT NULL,
    type            NVARCHAR2(100) NOT NULL,
    source          NVARCHAR2(60),
    message         NVARCHAR2(500) NOT NULL,
    username        NVARCHAR2(50),
    statuscode      NUMBER NOT NULL,
    timeutc         DATE NOT NULL,
    sequencenumber  NUMBER NOT NULL,
    allxml          NCLOB NOT NULL,
    CONSTRAINT idx_elmah$error_pk 
        PRIMARY KEY (errorid) 
        USING INDEX -- TABLESPACE "TABLESPACE FOR INDEX"
) -- TABLESPACE "TABLESPACE FOR DATA"
/

-- trigger to make sure we get our sequence number in the table
CREATE TRIGGER trg_elmah$error_bi
BEFORE INSERT ON elmah$error
FOR EACH ROW
BEGIN
    SELECT elmah$error_seq.NEXTVAL INTO :new.sequencenumber FROM dual;
END trg_elmah$error_bi;
/

-- create the index on the table
CREATE INDEX idx_elmah$error_app_time_seq ON elmah$error(application, timeutc DESC, sequencenumber DESC)
/

-- package containing the procedure we need for ELMAH to log errors
CREATE OR REPLACE PACKAGE pkg_elmah$log_error
IS
    PROCEDURE LogError
    (
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_Application IN elmah$error.application%TYPE,
        v_Host IN elmah$error.host%TYPE,
        v_Type IN elmah$error.type%TYPE,
        v_Source IN elmah$error.source%TYPE,
        v_Message IN elmah$error.message%TYPE,
        v_User IN elmah$error.username%TYPE,
        v_AllXml IN elmah$error.allxml%TYPE,
        v_StatusCode IN elmah$error.statuscode%TYPE,
        v_TimeUtc IN elmah$error.timeutc%TYPE
    );

END pkg_elmah$log_error;
/

CREATE OR REPLACE PACKAGE BODY pkg_elmah$log_error
IS
    PROCEDURE LogError
    (
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_Application IN elmah$error.application%TYPE,
        v_Host IN elmah$error.host%TYPE,
        v_Type IN elmah$error.type%TYPE,
        v_Source IN elmah$error.source%TYPE,
        v_Message IN elmah$error.message%TYPE,
        v_User IN elmah$error.username%TYPE,
        v_AllXml IN elmah$error.allxml%TYPE,
        v_StatusCode IN elmah$error.statuscode%TYPE,
        v_TimeUtc IN elmah$error.timeutc%TYPE
    )
    IS
    BEGIN
        INSERT INTO elmah$error
            (
                errorid,
                application,
                host,
                type,
                source,
                message,
                username,
                allxml,
                statuscode,
                timeutc
            )
        VALUES
            (
                UPPER(v_ErrorId),
                v_Application,
                v_Host,
                v_Type,
                v_Source,
                v_Message,
                v_User,
                v_AllXml,
                v_StatusCode,
                v_TimeUtc
            );

    END LogError;   

END pkg_elmah$log_error;
/


-- package containing the procedure we need for ELMAH to retrieve errors
CREATE OR REPLACE PACKAGE pkg_elmah$get_error
IS
	-- NB this is for backwards compatibility with Oracle 8i
    TYPE t_cursor IS REF CURSOR;
    
    PROCEDURE GetErrorXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_AllXml OUT elmah$error.allxml%TYPE
    );

    PROCEDURE GetErrorsXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_PageIndex IN NUMBER DEFAULT 0,
        v_PageSize IN NUMBER DEFAULT 15,
        v_TotalCount OUT NUMBER,
        v_Results OUT t_cursor
    );
    
END pkg_elmah$get_error;
/

CREATE OR REPLACE PACKAGE BODY pkg_elmah$get_error
IS
    PROCEDURE GetErrorXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_AllXml OUT elmah$error.allxml%TYPE
    )
    IS
    BEGIN
        SELECT  allxml
        INTO    v_AllXml
        FROM    elmah$error
        WHERE   errorid = UPPER(v_ErrorId)
        AND     application = v_Application;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_AllXml := NULL;
    END GetErrorXml;

    PROCEDURE GetErrorsXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_PageIndex IN NUMBER DEFAULT 0,
        v_PageSize IN NUMBER DEFAULT 15,
        v_TotalCount OUT NUMBER,
        v_Results OUT t_cursor
    )
    IS
        l_StartRowIndex NUMBER;
        l_EndRowIndex   NUMBER;
    BEGIN
        -- Get the ID of the first error for the requested page
        l_StartRowIndex := v_PageIndex * v_PageSize + 1;
        l_EndRowIndex := l_StartRowIndex + v_PageSize - 1;
        
        -- find out how many rows we've got in total
        SELECT  COUNT(*)
        INTO    v_TotalCount
        FROM    elmah$error
        WHERE   application = v_Application;

        OPEN v_Results FOR
            SELECT  *
            FROM
            (
                SELECT  e.*,
                        ROWNUM row_number
                FROM
                (
                    SELECT  /*+ INDEX(elmah$error, idx_elmah$error_app_time_seq) */
                            errorid,
                            application,
                            host,
                            type,
                            source,
                            message,
                            username,
                            statuscode,
                            timeutc
                    FROM    elmah$error
                    WHERE   application = v_Application
                    ORDER BY
                            timeutc DESC, 
                            sequencenumber DESC
                ) e
                WHERE ROWNUM <= l_EndRowIndex
            )
            WHERE   row_number >= l_StartRowIndex;
            
    END GetErrorsXml;

END pkg_elmah$get_error;
/

/* 
-- If you are securing the packages above, you will need to grant execute
-- privileges on them so that they can be called by the user connecting to the database.
-- NB As long as you use the schema owner for the connection string, this is not necessary,
-- although this is generally discouraged by Best Practices.

-- Option 1) Allow any user to execute the package (not recommended)
-- replace OWNER for the schema owner in the following statement
GRANT EXECUTE ON OWNER.pkg_elmah$log_error TO PUBLIC;
GRANT EXECUTE ON OWNER.pkg_elmah$get_error TO PUBLIC;

-- Option 2) Allow a single user to execute the package (better)
-- replace OWNER for the schema owner in the following statement
GRANT EXECUTE ON OWNER.pkg_elmah$log_error TO USER_NAME;
GRANT EXECUTE ON OWNER.pkg_elmah$get_error TO USER_NAME;

-- Option 3) Lock things down so that one user can only log errors, while another user can read and log errors (most secure)
-- replace OWNER for the schema owner in the following statement
-- LOGGING_USER_NAME will be used to connect to the database in all sites which log errors to the database
GRANT EXECUTE ON OWNER.pkg_elmah$log_error TO LOGGING_USER_NAME;
-- ADMIN_USER_NAME will be used to connect to the database in an admin portal which allows users to read errors
GRANT EXECUTE ON OWNER.pkg_elmah$log_error TO ADMIN_USER_NAME;
GRANT EXECUTE ON OWNER.pkg_elmah$get_error TO ADMIN_USER_NAME;

-- NB if you do take this approach, be sure to set the schemaOwner parameter in your web.config
*/

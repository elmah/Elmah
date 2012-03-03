' ELMAH - Error Logging Modules and Handlers for ASP.NET
' Copyright (c) 2004-9 Atif Aziz. All rights reserved.
'
'  Author(s):
'
'      Atif Aziz, http://www.raboof.com
'
' Licensed under the Apache License, Version 2.0 (the "License");
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at
'
'    http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.

' $Id: mkmdb.vbs 760 2011-01-02 23:17:56Z azizatif $

' ---------------------------------------------------------------------------
' Script for creating an empty MS Access database file (MDB) to be used with
' Elmah.AccessErrorLog (Microsoft Access Error Log).

' enum KeyTypeEnum

Const adKeyPrimary = 1
Const adKeyForeign = 2
Const adKeyUnique = 3

' enum ColumnAttributesEnum

Const adColFixed = 1
Const adColNullable = 2

' enum DataTypeEnum

Const adEmpty = 0
Const adTinyInt = 16
Const adSmallInt = 2
Const adInteger = 3
Const adBigInt = 20
Const adUnsignedTinyInt = 17
Const adUnsignedSmallInt = 18
Const adUnsignedInt = 19
Const adUnsignedBigInt = 21
Const adSingle = 4
Const adDouble = 5
Const adCurrency = 6
Const adDecimal = 14
Const adNumeric = 131
Const adBoolean = 11
Const adError = 10
Const adUserDefined = 132
Const adVariant = 12
Const adIDispatch = 9
Const adIUnknown = 13
Const adGUID = 72
Const adDate = 7
Const adDBDate = 133
Const adDBTime = 134
Const adDBTimeStamp = 135
Const adBSTR = 8
Const adChar = 129
Const adVarChar = 200
Const adLongVarChar = 201
Const adWChar = 130
Const adVarWChar = 202
Const adLongVarWChar = 203
Const adBinary = 128
Const adVarBinary = 204
Const adLongVarBinary = 205
Const adChapter = 136
Const adFileTime = 64
Const adPropVariant = 138
Const adVarNumeric = 139

Function CreateTable(ByVal Catalog, ByVal Name)

    Set Table = CreateObject("ADOX.Table")
    Set Table.ParentCatalog = Catalog
    Table.Name = Name

    ' See http://msdn.microsoft.com/en-us/library/aa164917(office.10).aspx for 
    ' type mappings between MS Access and ADOX

    With Table.Columns
        .Append "ErrorId", adInteger
        .Item("ErrorId").Properties("AutoIncrement") = True
        .Append "Application", adVarWChar, 60
        .Append "Host", adVarWChar, 30
        .Append "Type", adVarWChar, 100
        .Append "Source", adVarWChar, 60
        .Append "Message", adLongVarWChar
        .Append "UserName", adVarWChar, 60
        .Append "StatusCode", adInteger
        .Append "TimeUtc", adDate
        .Append "AllXml", adLongVarWChar
    End With

    Table.Keys.Append "PrimaryKey", adKeyPrimary, "ErrorId"

    Catalog.Tables.Append Table
    Set CreateTable = Table

End Function

Sub Main()

    Const ecMissingArgument = 1
    Const ecObjectCreationError = 2
    Const ecCatalogCreationError = 3
    Const ecTableCreationError = 4

    If WScript.Arguments.Count = 0 Then 
        QuitWithError ecMissingArgument, "Missing MDB file path argument."
    End If

    Dim FileName : FileName = WScript.Arguments(0)

    On Error Resume Next

    Set Catalog = CreateObject("ADOX.Catalog") : QuitOnError ecObjectCreationError
    Catalog.Create "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & FileName : QuitOnError ecCatalogCreationError
    CreateTable Catalog, "ELMAH_Error" : QuitOnError ecTableCreationError

End Sub

Sub QuitOnError(ByVal ExitCode)

    If Err.Number = 0 Then Exit Sub
    WScript.StdErr.WriteLine Err.Source & ": " & Err.Description
    WScript.Quit(ExitCode)

End Sub

Sub QuitWithError(ByVal ExitCode, ByVal Message)

    WScript.StdErr.WriteLine Message
    WScript.Quit(ExitCode)

End Sub

Main()

Imports System.Data

Public Module DB

        Dim context1 As System.Web.HttpContext = System.Web.HttpContext.Current
        Dim Server_IP As String = context1.Request.ServerVariables("HTTP_X_FORWARDED_FOR")
        Dim Server_Name As String = context1.Request.ServerVariables("SERVER_NAME")


        Public ReadOnly Property EBDB As String
            Get
                Return EBDB_CS()
            End Get
        End Property

        Public ReadOnly Property ICBS As String
            Get
                Return ICBS_CS()
            End Get
        End Property
        'Private Function EBDB_CS(Optional lcDatabase As String = "EBDB", Optional lcUserID As String = "inap", Optional lcUserPW As String = "inap") As String
        '    EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0344)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '    Dim lcServerName As String = Get_Server_Name()
        '    If InStr(UCase(Left(lcServerName, 20)), "DRWEB") > 0 Then
        '        EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0344)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '    End If
        '    If InStr(UCase(Left(lcServerName, 20)), "S0305") > 0 Then
        '        EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0344)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '    End If
        '    If InStr(UCase(Left(lcServerName, 20)), "S0307") > 0 Then
        '        EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0320)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '    End If
        '    If InStr(UCase(Left(lcServerName, 20)), "LOCALHOST") > 0 Then
        '        EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0320)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '        'EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0344)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"
        '        'EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=S0344)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=" + lcDatabase + ")));User Id=" + lcUserID + ";Password=" + lcUserPW + ";"


        '    End If
        'End Function

        Function EBDB_CS() As String
            EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ebdb)));User Id=inap;Password=inap;"
            Dim env = ConfigurationManager.AppSettings("ENV")
            If env = "PROD" Then
                EBDB_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ebdb)));User Id=inap;Password=inap;"
            End If
        End Function


        Private Function ICBS_CS(Optional lcDatabase As String = "") As String
            Select Case UCase(lcDatabase)
                Case "UAT"
                    ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=s0320.EskanBank.com)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSTRN3)));User Id=ICBS;Password=sbci;"
                Case "ICBS16", "TEST"
                    ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBS)));User Id=ICBS;Password=icbs01;"
                Case "PRODUCTION"
                    'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=DB2)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBS)));User Id=INAP;Password=inap;"
                    ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSSRV)));User Id=ICBS;Password=icbs2019;"
                Case "DR"
                    ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=drdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSDR)));User Id=INAP;Password=inap;"
                Case Else
                    'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBS)));User Id=INAP;Password=inap;"
                    ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSSRV)));User Id=ICBS;Password=icbs2019;"
                    Dim lcServerName As String = Get_Server_Name()
                    If InStr(UCase(Left(lcServerName, 20)), "DRWEB") > 0 Then
                        ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=drdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSDR)));User Id=ICBS;Password=icbs2019;"
                    End If
                    If InStr(UCase(Left(lcServerName, 20)), "S0307") > 0 Then
                        'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBS)));User Id=ICBS;Password=icbs01;"
                        ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=icbstst1)));User Id=inap;Password=inap;"
                    End If
                    If InStr(UCase(Left(lcServerName, 20)), "LOCALHOST") > 0 Then
                        'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBS)));User Id=inap;Password=inap;"
                        ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=testdb)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=icbstst1)));User Id=inap;Password=inap;"
                        'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSSRV)));User Id=ICBS;Password=icbs2019;" 'live system
                        'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSSRV)));User Id=ICBS;Password=icbs2019;"' to be removed
                        'ICBS_CS = "Provider=OraOLEDB.Oracle;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=scan-eskan)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ICBSSRV)));User Id=ICBS;Password=icbs2019;"

                    End If
            End Select
        End Function

        Public Function Get_Server_Name() As String
            Try
                Dim context As System.Web.HttpContext = System.Web.HttpContext.Current
                Dim sIPAddress As String = context.Request.ServerVariables("HTTP_X_FORWARDED_FOR")
                If String.IsNullOrEmpty(sIPAddress) Then
                    Return context.Request.ServerVariables("SERVER_NAME")
                Else
                    Dim ipArray As String() = sIPAddress.Split(New [Char]() {","c})
                    Return ipArray(0)
                End If
            Catch ex As Exception
                If String.IsNullOrEmpty(Server_IP) Then
                    Return Server_Name
                Else
                    Dim ipArray As String() = Server_IP.Split(New [Char]() {","c})
                    Return ipArray(0)
                End If
            End Try
        End Function

        Public Sub InsertRecord(ByVal DBConnection As String,
                                ByVal TableName As String,
                                ByVal Dic As Dictionary(Of String, String))
            Dim SQL As String = GetSqlFromDictionary(Dic, TableName, SQLtype.Insert)
            ExecuteNonQuery(DBConnection, SQL)
        End Sub

        Public Sub UpdateRecord(ByVal DBConnection As String,
                                ByVal TableName As String,
                                ByVal Dic As Dictionary(Of String, String), ByVal WhereClause As String)
            Dim SQL As String = GetSqlFromDictionary(Dic, TableName, SQLtype.Update)

            If WhereClause <> "" Then
                If InStr(WhereClause.ToUpper, "WHERE") Then
                    SQL = SQL + " " + WhereClause
                Else
                    SQL = SQL + " where " + WhereClause
                End If

            End If

            ExecuteNonQuery(DBConnection, SQL)
        End Sub

        Sub ExecuteNonQuery(ByVal DBConnection As String, ByVal SQL As String)
            Dim Dcom As New Data.OleDb.OleDbCommand
            Dim Dcon As New Data.OleDb.OleDbConnection

            Dcon.ConnectionString = DBConnection
            Dcom.Connection = Dcon
            Dcom.CommandText = SQL

            'MsgBox(System.AppDomain.CurrentDomain.BaseDirectory)




            Try
                Dcom.Connection.Open()
            Catch ex As Exception

            End Try

            Try
                Dcom.ExecuteNonQuery()
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try

            Try
                Dcom.Connection.Close()
            Catch ex As Exception

            End Try

        End Sub

        Function RetreiveScalarInteger(ByVal Field As String,
                                       ByVal Table As String,
                                       ByVal Condition As String) As Integer

            If Condition.Trim <> "" And InStr(Condition.ToLower, "Where".ToLower) = False Then
                Condition = " where " & Condition
            End If
            Dim Sql As String = "Select Max(Cint(" & Field & ")) from " & Table & " " & Condition

            Return RetreiveScalarInteger(InfoDB, Sql)

        End Function


        Function RetreiveScalarInteger(ByVal DBConnection As String,
                                       ByVal SQL As String) As Integer
            Dim Result As Object = RetreiveScalar(DBConnection, SQL)

            If IsDBNull(Result) Or Result Is Nothing Then
                RetreiveScalarInteger = 0
            Else
                RetreiveScalarInteger = CInt(Result)
            End If

        End Function


        Function RetreiveScalarSTRING(ByVal DBConnection As String, ByVal SQL As String) As String
            Dim Result As New Object
            Result = RetreiveScalar(DBConnection, SQL).ToString

            If IsDBNull(Result) Or Result Is Nothing Then
                RetreiveScalarSTRING = ""
            Else
                RetreiveScalarSTRING = CStr(Result)
            End If

        End Function

        Function RetreiveScalar(ByVal DBConnection As String, ByVal SQL As String) As Object
            Dim Dcom As New Data.OleDb.OleDbCommand
            Dim Dcon As New Data.OleDb.OleDbConnection

            Dcon.ConnectionString = DBConnection
            Dcom.Connection = Dcon
            Dcom.CommandText = SQL

            Try
                Dcom.Connection.Open()
            Catch ex As Exception

            End Try

            RetreiveScalar = Dcom.ExecuteScalar
            If RetreiveScalar Is Nothing Then RetreiveScalar = DBNull.Value
            Try
                Dcom.Connection.Close()
            Catch ex As Exception

            End Try



        End Function


        Function GetSqlFromDictionary(
                                 ByVal Dic As Dictionary(Of String, String),
                                 ByVal TableName As String,
                                 ByVal SQLType As SQLtype,
                                 Optional WherePharse As String = "") As String

            If SQLType = SQLtype.Insert Then
                GetSqlFromDictionary = GetInsertSQL(Dic, TableName)
            Else
                GetSqlFromDictionary = GetUpdateSQL(Dic, TableName, WherePharse)
            End If
        End Function

        Function GetInsertSQL(ByVal Dic As Dictionary(Of String, String),
                               ByVal TableName As String) As String

            Dim Fields As String = Nothing
            Dim Values As String = Nothing

            For Each KV As KeyValuePair(Of String, String) In Dic
                Fields = Fields & "," & KV.Key
                Values = Values & ",'" & TitleCase(KV.Value.Replace("'", "''")) & "'"
            Next

            Fields = Fields.Substring(1)
            Values = Values.Substring(1)

            GetInsertSQL = "insert into " & TableName & "(" & Fields & ") Values (" & Values & ")"

        End Function

        Function GetUpdateSQL(ByVal Dic As Dictionary(Of String, String),
                           ByVal TableName As String,
                           Optional WherePharse As String = "") As String

            Dim SQL As String = Nothing
            For Each KV As KeyValuePair(Of String, String) In Dic
                SQL = SQL & "," & KV.Key & "='" & TitleCase(KV.Value.Replace("'", "''")) & "'"
            Next

            SQL = SQL.Substring(1)
            If WherePharse = "" Then
                GetUpdateSQL = "update " & TableName & " set " & SQL
            Else
                GetUpdateSQL = "update " & TableName & " set " & SQL & " where " & WherePharse
            End If



        End Function

        Function TitleCase(ByVal Name As String) As String
            TitleCase = Name
            If Name Is Nothing Then
                TitleCase = ""
            End If
        End Function

        Public Function GetDataTable(ByVal lcConnection As String, lcSql As String) As Data.DataTable
            lcSql = lcSql.Replace(";", "")
            Dim dt As Data.DataTable = New Data.DataTable
            Dim oda As Data.OleDb.OleDbDataAdapter = New Data.OleDb.OleDbDataAdapter(lcSql, lcConnection)
            oda.Fill(dt)
            GetDataTable = dt
        End Function


        Property InfoDB As String
            Get
                Dim DBPAth As String = System.AppDomain.CurrentDomain.BaseDirectory & "\App_Data\Debts.mdb"
                Return "Provider=Microsoft.Jet.OLEDB.4.0;Data Source='" & DBPAth & "'"
            End Get
            Set(value As String)

            End Set
        End Property

        Function GetNextID_New(ByVal ConnectionString As String,
                               ByVal lcTable As String,
                               ByVal lcField As String,
                               ByVal lnLength As Integer,
                               Optional lnIncrement As Integer = 1,
                               Optional WhereStatement As String = "") As String

            'Where Statement ========================================
            If WhereStatement <> "" Then
                If InStr(WhereStatement.ToUpper, "where".ToUpper) Then

                Else
                    WhereStatement = " where " & WhereStatement
                End If
            End If
            '========================================================

            Dim lcSql As String = "Select Max(" + lcField + ") as MAXID from " + lcTable + " " + WhereStatement
            Dim strNextID As String = RetreiveScalar(ConnectionString, lcSql).ToString

            If strNextID.Trim = "" Then
                strNextID = "0"
            End If

            Dim lnNextID As Integer = lnIncrement
            strNextID = CInt(CInt(strNextID) + lnNextID)

            GetNextID_New = Right("00000000000000000000" + CStr(strNextID), lnLength)
        End Function
    End Module

Public Enum SQLtype
    Update
    Insert
End Enum

Imports System.Data
Imports System.Collections.Generic
Imports System.DateTime
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports CrystalDecisions.CrystalReports.Engine

Partial Class ShowHistory
    Inherits System.Web.UI.Page
    Private encryNdecry As New EncryDecry

    Private Sub AutoTransfer_Load(sender As Object, e As EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then


            lblPID.Text = Request("NodeID")
            lblPRJID.Text = Request("ProjectId")
            lblSTATEID.Text = Request("STATEID")
            lblActionID.Text = Request("ActionId")

            BindHistory()
        End If

    End Sub

    ''' <summary>
    ''' Fills gvHistory with this unit's UNITSHUB_UNITSHISTORY rows, newest first, with
    ''' readable names joined in:
    '''   From / To       status name (UNITSHUB_PROJECTSTATUS) + sub-status subtitle
    '''                   (UNITSHUB_PROJECTSUBSTATUS); the IDs when a name isn't found
    '''                   (e.g. old status IDs from before the sub-status change)
    '''   Action          UNITSHUB_ACTIONS.ACTION_TITLE
    '''   Implemented by  UNITSHUB_USERS.FULL_NAME
    '''   Customer        UNITSHUB_CONTACTS.NAME
    '''   Date and time   DATENTIME (Unix seconds) shown in Bahrain time
    ''' </summary>
    Private Sub BindHistory()
        Dim NodeId As String = Convert.ToString(lblPID.Text).Trim()
        If NodeId = "" Then
            litHistoryFor.Text = "No unit was given."
            gvHistory.DataSource = Nothing
            gvHistory.DataBind()
            Exit Sub
        End If

        Dim safeNode As String = NodeId.Replace("'", "''")
        Dim projectFilter As String = ""
        If Convert.ToString(lblPRJID.Text).Trim() <> "" Then
            projectFilter = " AND H.PROJECT_ID = '" & lblPRJID.Text.Trim().Replace("'", "''") & "' "
        End If

        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT H.DATENTIME, "
        SQL = SQL + vbCrLf + "        H.FROM_STATUS, H.FROM_SUBSTATUS, H.TO_STATUS, H.TO_SUBSTATUS, "
        SQL = SQL + vbCrLf + "        FS.STATUS AS FROM_STATUS_NAME, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE, CASE WHEN H.FROM_SUBSTATUS IS NULL THEN FS.SUBTITLE END) AS FROM_SUB_NAME, "
        SQL = SQL + vbCrLf + "        TS.STATUS AS TO_STATUS_NAME, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE, CASE WHEN H.TO_SUBSTATUS IS NULL THEN TS.SUBTITLE END) AS TO_SUB_NAME, "
        ' Card colours: the sub-status's own where set, otherwise the status's (as on MainPage)
        SQL = SQL + vbCrLf + "        NVL(FSS.STATUS_BG_COLOR, FS.STATUS_BG_COLOR) AS FROM_BG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.STATUS_FG_COLOR, FS.STATUS_FG_COLOR) AS FROM_FG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE_BG_COLOR, FS.SUBTITLE_BG_COLOR) AS FROM_SUB_BG, "
        SQL = SQL + vbCrLf + "        NVL(FSS.SUBTITLE_FG_COLOR, FS.SUBTITLE_FG_COLOR) AS FROM_SUB_FG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.STATUS_BG_COLOR, TS.STATUS_BG_COLOR) AS TO_BG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.STATUS_FG_COLOR, TS.STATUS_FG_COLOR) AS TO_FG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE_BG_COLOR, TS.SUBTITLE_BG_COLOR) AS TO_SUB_BG, "
        SQL = SQL + vbCrLf + "        NVL(TSS.SUBTITLE_FG_COLOR, TS.SUBTITLE_FG_COLOR) AS TO_SUB_FG, "
        SQL = SQL + vbCrLf + "        NVL(A.ACTION_TITLE, H.ACTION_ID) AS ACTION_TITLE, "
        SQL = SQL + vbCrLf + "        NVL(U.FULL_NAME, H.IMPLEMENTEDBY) AS IMPLEMENTED_BY_NAME, "
        SQL = SQL + vbCrLf + "        C.NAME AS CUSTOMER_NAME, "
        SQL = SQL + vbCrLf + "        H.SUMMARY "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_UNITSHISTORY H "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSTATUS FS "
        SQL = SQL + vbCrLf + "        ON FS.PROJECT_ID = H.PROJECT_ID AND FS.STATE_ID = H.FROM_STATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSUBSTATUS FSS "
        SQL = SQL + vbCrLf + "        ON FSS.PROJECT_ID = H.PROJECT_ID AND FSS.STATE_ID = H.FROM_STATUS AND FSS.SUBSTATE_ID = H.FROM_SUBSTATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSTATUS TS "
        SQL = SQL + vbCrLf + "        ON TS.PROJECT_ID = H.PROJECT_ID AND TS.STATE_ID = H.TO_STATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_PROJECTSUBSTATUS TSS "
        SQL = SQL + vbCrLf + "        ON TSS.PROJECT_ID = H.PROJECT_ID AND TSS.STATE_ID = H.TO_STATUS AND TSS.SUBSTATE_ID = H.TO_SUBSTATUS "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_ACTIONS A "
        SQL = SQL + vbCrLf + "        ON A.PROJECT_ID = H.PROJECT_ID AND A.ACTION_ID = H.ACTION_ID "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_USERS U "
        SQL = SQL + vbCrLf + "        ON U.USER_ID = H.IMPLEMENTEDBY "
        SQL = SQL + vbCrLf + " LEFT JOIN UNITSHUB_CONTACTS C "
        SQL = SQL + vbCrLf + "        ON C.ID = H.CONTACT_ID "
        SQL = SQL + vbCrLf + " WHERE  H.NODE_ID = '" & safeNode & "' " & projectFilter
        SQL = SQL + vbCrLf + " ORDER BY CASE WHEN REGEXP_LIKE(TRIM(H.DATENTIME), '^[0-9]+$') THEN TO_NUMBER(TRIM(H.DATENTIME)) END DESC NULLS LAST "

        Dim DT As DataTable = GetDataTable(EBDB, SQL)
        If DT Is Nothing Then DT = New DataTable()

        DT.Columns.Add("WHEN_TEXT", GetType(String))
        DT.Columns.Add("FROM_HTML", GetType(String))
        DT.Columns.Add("TO_HTML", GetType(String))

        For Each r As DataRow In DT.Rows
            r("WHEN_TEXT") = FormatUnixTime(Convert.ToString(r("DATENTIME")))
            r("FROM_HTML") = StatusHtml(r, "FROM")
            r("TO_HTML") = StatusHtml(r, "TO")
        Next

        gvHistory.DataSource = DT
        gvHistory.DataBind()

        ' "Unit SAHEL17 · Sahel Villas · 9 records" - the unit's Reference and project name
        ' instead of its node ID (the node ID is used only if the unit has no Reference)
        Dim unitRef As String = GetUnitReference(NodeId)
        Dim projectName As String = GetProjectName(NodeId)
        Dim parts As New List(Of String)
        parts.Add("Unit " & If(unitRef = "", NodeId, unitRef))
        If projectName <> "" Then parts.Add(projectName)
        parts.Add(DT.Rows.Count.ToString() & If(DT.Rows.Count = 1, " record", " records"))
        litHistoryFor.Text = Server.HtmlEncode(String.Join(" · ", parts))
    End Sub

    ''' <summary>
    ''' The unit's "Reference" attribute (the attribute whose NAME_IN_UI is Reference in
    ''' UNITSHUB_ATTRIBUTES_PROPERTIES, value from UNITSHUB_NODE_ATTRIBUTE_VALUE) - the same
    ''' Reference MainPage shows. "" if the unit has none.
    ''' </summary>
    Private Function GetUnitReference(NodeId As String) As String
        Dim SQL As String = ""
        SQL = SQL + vbCrLf + " SELECT NAV.VALUE_TEXT "
        SQL = SQL + vbCrLf + " FROM   UNITSHUB_NODES N "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_ATTRIBUTES_PROPERTIES AP "
        SQL = SQL + vbCrLf + "        ON AP.PROJECT_ID   = N.PROJECT_ID "
        SQL = SQL + vbCrLf + "       AND AP.NODE_TYPE_ID = N.NODE_TYPE_ID "
        SQL = SQL + vbCrLf + "       AND UPPER(REPLACE(AP.NAME_IN_UI, '""', '')) = 'REFERENCE' "
        SQL = SQL + vbCrLf + " JOIN   UNITSHUB_NODE_ATTRIBUTE_VALUE NAV "
        SQL = SQL + vbCrLf + "        ON NAV.NODE_ID = N.NODE_ID "
        SQL = SQL + vbCrLf + "       AND TO_NUMBER(NAV.DISPLAY_ORDER) = TO_NUMBER(AP.DISPLAY_ORDER) "
        SQL = SQL + vbCrLf + " WHERE  N.NODE_ID = '" & If(NodeId, "").Trim().Replace("'", "''") & "' "
        SQL = SQL + vbCrLf + "   AND  ROWNUM = 1 "
        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB, SQL)).Trim()
    End Function

    ''' <summary>
    ''' Project name (UNITSHUB_PROJECTS.PROJECT_NAME_EN) for the ProjectId passed in, or for
    ''' the unit's own project when none was passed. "" if not found.
    ''' </summary>
    Private Function GetProjectName(NodeId As String) As String
        Dim projectId As String = Convert.ToString(lblPRJID.Text).Trim()
        If projectId = "" Then
            projectId = Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
                "SELECT PROJECT_ID FROM UNITSHUB_NODES WHERE NODE_ID = '" & If(NodeId, "").Trim().Replace("'", "''") & "'")).Trim()
        End If
        If projectId = "" Then Return ""
        Return Convert.ToString(DB.RetreiveScalarSTRING(EBDB,
            "SELECT PROJECT_NAME_EN FROM UNITSHUB_PROJECTS WHERE PROJECT_ID = '" & projectId.Replace("'", "''") & "'")).Trim()
    End Function

    ''' <summary>
    ''' The status as a small card like the ones on MainPage: status name on the card colour,
    ''' and the sub-status (or the status's own subtitle) in a rounded box underneath, with
    ''' that box's colours. Side = "FROM" or "TO". Falls back to the raw IDs and default
    ''' colours when a status / sub-status isn't found (e.g. old IDs in early history rows).
    ''' </summary>
    Private Function StatusHtml(r As DataRow, Side As String) As String
        Dim statusId As String = Convert.ToString(r(Side & "_STATUS")).Trim()
        Dim subId As String = Convert.ToString(r(Side & "_SUBSTATUS")).Trim()
        Dim statusName As String = Convert.ToString(r(Side & "_STATUS_NAME")).Trim()
        Dim subName As String = Convert.ToString(r(Side & "_SUB_NAME")).Trim()

        If statusId = "" AndAlso statusName = "" Then Return "&ndash;"
        If statusName = "" Then statusName = statusId
        If subName = "" AndAlso subId <> "" Then subName = subId

        Dim cardStyle As String = ColorStyle(r(Side & "_BG"), r(Side & "_FG"))
        Dim subStyle As String = ColorStyle(r(Side & "_SUB_BG"), r(Side & "_SUB_FG"))

        Dim html As String = "<span class=""st-card""" & cardStyle & ">" &
                             "<span class=""st-title"">" & Server.HtmlEncode(statusName) & "</span>"
        If subName <> "" Then
            html &= "<span class=""st-sub""" & subStyle & ">" & Server.HtmlEncode(subName) & "</span>"
        End If
        Return html & "</span>"
    End Function

    ''' <summary>
    ''' style="background-color:..;color:.." from two colour values; only values that look
    ''' like colours (#hex or a plain colour name) are used, anything else is left out.
    ''' </summary>
    Private Function ColorStyle(Background As Object, Foreground As Object) As String
        Dim parts As New List(Of String)
        Dim bg As String = SafeColor(Background)
        Dim fg As String = SafeColor(Foreground)
        If bg <> "" Then parts.Add("background-color:" & bg)
        If fg <> "" Then parts.Add("color:" & fg)
        Return If(parts.Count = 0, "", " style=""" & String.Join(";", parts) & """")
    End Function

    Private Function SafeColor(Value As Object) As String
        Dim v As String = Convert.ToString(Value).Trim()
        If v = "" Then Return ""
        If System.Text.RegularExpressions.Regex.IsMatch(v, "^#?[0-9A-Fa-f]{3,8}$") Then
            Return If(v.StartsWith("#"), v, "#" & v)
        End If
        If System.Text.RegularExpressions.Regex.IsMatch(v, "^[A-Za-z]+$") Then Return v
        Return ""
    End Function

    ''' <summary>
    ''' DATENTIME is a Unix timestamp in seconds (UTC). Shown in Bahrain time (UTC+3) on two
    ''' lines - date above (yyyy-MM-dd), time below (HH:mm) - or as stored if it isn't a number.
    ''' </summary>
    Private Function FormatUnixTime(Value As String) As String
        Dim seconds As Long
        If Not Long.TryParse(If(Value, "").Trim(), seconds) Then Return Server.HtmlEncode(If(Value, ""))

        Dim utc As DateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds)
        Dim bahrain As DateTimeOffset = utc.ToOffset(TimeSpan.FromHours(3))
        Return "<span class=""d"">" & bahrain.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) & "</span>" &
               "<span class=""t"">" & bahrain.ToString("HH:mm", CultureInfo.InvariantCulture) & "</span>"
    End Function

    Protected Sub imgClose_Click(sender As Object, e As ImageClickEventArgs) Handles imgClose.Click
        VendorPopupHelper.RegisterPopupSelectionAndClose(Me, False, skipPostBack:=False)
    End Sub

End Class
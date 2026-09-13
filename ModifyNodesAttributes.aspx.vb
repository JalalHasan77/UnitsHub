Imports System.Data
Imports System.Globalization
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.Web.Caching
Imports System.Web.UI.HtmlControls


Partial Class ModifyNodesAttributes
    Inherits System.Web.UI.Page

    Private Shared ReadOnly BadgeColors As String() = {"badge-blue", "badge-green", "badge-orange", "badge-purple", "badge-teal", "badge-pink"}
    Private encryNdecry As New EncryDecry

    ''' <summary>
    ''' Per-request cache for GetAvailableActions(). The permission set for a user
    ''' only varies by USER_ID and PROJECT_ID (it covers every STATE_ID for that
    ''' project in one shot), so this is keyed by "ProjectID|UserID" and holds the
    ''' full unfiltered DataTable of (PROJECT_ID, STATE_ID, ACTION_ID, ICON,
    ''' PERMISSION_NAME, STATUS_NAME, STATUS_SUBTITLE) rows for that project/user.
    ''' GetAvailableActions() then filters that table in memory by the row's own
    ''' status name. Without this, binding a grid with
    ''' N rows would fire N identical DB round-trips - and RepopulateGridActionsIfNeeded()
    ''' would repeat that on EVERY postback, even ones unrelated to the grid.
    ''' </summary>
    Private ReadOnly _projectActionsCache As New Dictionary(Of String, DataTable)

    ''' <summary>
    ''' Shared, cross-user, cross-request cache for slow-changing lookup/config data
    ''' (column aliases, visible-field lists, unit type, project view names) that is
    ''' identical for every user viewing the same project. loadData() re-runs on every
    ''' Approve/Bounce/DropDownList change, so without this cache those config queries
    ''' would be re-fetched from the DB on every single one of those postbacks even
    ''' though the underlying config almost never changes.
    ''' </summary>
    Private Shared Function GetOrAddToCache(Of T As Class)(cacheKey As String,
                                                            expirationMinutes As Double,
                                                            factory As Func(Of T)) As T
        Dim cached As T = TryCast(HttpRuntime.Cache.Get(cacheKey), T)
        If cached IsNot Nothing Then Return cached

        Dim fresh As T = factory()
        HttpRuntime.Cache.Insert(cacheKey,
                                  fresh,
                                  Nothing,
                                  DateTime.Now.AddMinutes(expirationMinutes),
                                  Cache.NoSlidingExpiration)
        Return fresh
    End Function

    ''' <summary>
    ''' Clears both Session and the ASP.NET Cache when MainPage is freshly loaded (not
    ''' on postback - see Page_Load).
    ''' - Session.Clear() drops THIS visitor's own leftovers - e.g.
    '''   Session("FilterMainTable"), Session("CheckedCheckBox"), Session("Result"),
    '''   Session("PopupParams_...") - from a previous visit.
    ''' - The Cache sweep below drops every entry HttpRuntime.Cache is holding, not
    '''   just this file's own config caches (UnitType_/ProjectView_/ProjectViewName_/
    '''   VisibleFields_/ColumnAliases_ per project, plus the global UnitStatuses
    '''   lookup - see GetOrAddToCache's callers). ASP.NET's Cache is shared
    '''   APPLICATION-WIDE across every user and every page, unlike Session, so this
    '''   also evicts anything any other page in the app may be caching. The very next
    '''   request anywhere - this user or another, this page or another - re-fetches
    '''   from the DB instead of getting a cache hit, which is exactly what
    '''   GetOrAddToCache exists to avoid. Only wire this in where that trade-off is
    '''   genuinely wanted.
    ''' </summary>
    Private Sub ClearSessionAndCache()
        Session.Clear()

        Dim cache As System.Web.Caching.Cache = HttpRuntime.Cache
        Dim keysToRemove As New List(Of String)

        Dim enumerator As IDictionaryEnumerator = cache.GetEnumerator()
        While enumerator.MoveNext()
            keysToRemove.Add(CStr(enumerator.Key))
        End While

        For Each key As String In keysToRemove
            cache.Remove(key)
        Next
    End Sub

    'Dim DT As DataTable
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        If Not Page.IsPostBack Then
            ClearSessionAndCache()

            PopulateDropDownList(ddl:=DropDownList1,
                                 sql:="Select PROJECT_NAME_EN,PROJECT_ID from UNITSHUB_PROJECTS",
                                 DataConnection:=EBDB_CS,
                                 firstItemIs:="Select A Project")
        End If
    End Sub

    ''' <summary>
    ''' TODO: Wire this to your actual authentication/session mechanism.
    ''' Currently reads a "UserType" session value (e.g. "ADM", "FCD") set at login.
    ''' </summary>
    Private Function GetCurrentUserRole() As String
        Return Convert.ToString(Session("UserType"))
    End Function

    ''' <summary>
    ''' TODO: Wire this to your actual authentication/session mechanism, same as
    ''' GetCurrentUserRole(). Currently reads a "UserID" session value set at login,
    ''' falling back to the previously hardcoded test user ('2271') so behavior is
    ''' unchanged until real session wiring is in place - remove the fallback once
    ''' Session("UserID") is reliably populated at login.
    ''' </summary>
    Private Function GetCurrentUserID() As String
        Dim sessionUserID As String = Convert.ToString(Session("UserID"))
        If String.IsNullOrEmpty(sessionUserID) Then
            Return "2271" ' TEMP: fallback while real auth/session wiring is pending
        End If
        Return sessionUserID
    End Function

    ''' <summary>
    ''' Fires when the user picks a project in DropDownList1. Refreshes ListBox1 with
    ''' the distinct set of node types (TYPE_NAME / NODE_TYPE_ID) that actually occur
    ''' in UNITSHUB_NODES for that project.
    ''' </summary>
    Protected Sub DropDownList1_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        PopulateNodeTypesForProject()
    End Sub

    ''' <summary>
    ''' Clears and repopulates ListBox1 with the distinct NODE_TYPE_IDs found in
    ''' UNITSHUB_NODES for the project currently selected in DropDownList1, showing
    ''' each one's TYPE_NAME (from UNITSHUB_NODE_TYPES) as the visible text and the
    ''' NODE_TYPE_ID as the item's value. No-ops (and clears the list) if no real
    ''' project is selected - i.e. the "Select A Project" placeholder ("##").
    ''' </summary>
    Private Sub PopulateNodeTypesForProject()
        ListBox1.Items.Clear()
        ListBox1.Rows = 1
        ListBox2.Items.Clear()
        ListBox2.Rows = 1

        Dim ProjectID As String = DropDownList1.SelectedValue
        If String.IsNullOrEmpty(ProjectID) OrElse ProjectID = "##" Then
            Exit Sub
        End If

        Dim sql As String = "SELECT DISTINCT NT.NODE_TYPE_ID, NT.TYPE_NAME " &
                             "FROM UNITSHUB_NODES N " &
                             "INNER JOIN UNITSHUB_NODE_TYPES NT ON NT.NODE_TYPE_ID = N.NODE_TYPE_ID " &
                             "WHERE N.PROJECT_ID = " & ProjectID & " " &
                             "ORDER BY NT.NODE_TYPE_ID"

        Dim DT As DataTable = GetDataTable(EBDB_CS, sql)

        For Each DR As DataRow In DT.Rows
            Dim I As New ListItem
            I.Text = DR("TYPE_NAME").ToString
            I.Value = DR("NODE_TYPE_ID").ToString
            ListBox1.Items.Add(I)
        Next

        ' Grow the box downward to exactly fit its items instead of scrolling
        ' inside a fixed-height box.
        ListBox1.Rows = Math.Max(ListBox1.Items.Count, 1)
    End Sub

    ''' <summary>
    ''' Fires when the user picks a node type in ListBox1. Refreshes ListBox2 with
    ''' that node type's attributes (for the currently selected project).
    ''' </summary>
    Protected Sub ListBox1_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        PopulateAttributesForNodeType()
    End Sub

    ''' <summary>
    ''' Clears and repopulates ListBox2 with the attributes (ATTRIBUTE_NAME /
    ''' DATA_TYPE) defined in UNITSHUB_ATTRIBUTES for the project selected in
    ''' DropDownList1 and the node type selected in ListBox1, ordered by
    ''' DISPLAY_ORDER. No-ops (and clears the list) if either selection is missing.
    ''' </summary>
    Private Sub PopulateAttributesForNodeType()
        ListBox2.Items.Clear()
        ListBox2.Rows = 1

        Dim ProjectID As String = DropDownList1.SelectedValue
        If String.IsNullOrEmpty(ProjectID) OrElse ProjectID = "##" Then
            Exit Sub
        End If

        If ListBox1.SelectedItem Is Nothing Then
            Exit Sub
        End If
        Dim NodeTypeID As String = ListBox1.SelectedValue

        Dim sql As String = "SELECT ATTRIBUTE_NAME, DATA_TYPE " &
                             "FROM UNITSHUB_ATTRIBUTES " &
                             "WHERE PROJECT_ID = " & ProjectID & " " &
                             "AND NODE_TYPE_ID = " & NodeTypeID & " " &
                             "ORDER BY DISPLAY_ORDER"

        Dim DT As DataTable = GetDataTable(EBDB_CS, sql)

        For Each DR As DataRow In DT.Rows
            Dim I As New ListItem
            I.Text = DR("ATTRIBUTE_NAME").ToString
            I.Value = DR("DATA_TYPE").ToString
            ListBox2.Items.Add(I)
        Next

        ' Grow the box downward to exactly fit its items instead of scrolling
        ' inside a fixed-height box.
        ListBox2.Rows = Math.Max(ListBox2.Items.Count, 1)
    End Sub

    Private Sub PopulateDropDownList(ByRef ddl As DropDownList,
                                     sql As Object,
                                     DataConnection As String,
                                     Optional firstItemIs As String = "",
                                     Optional SelectedItemIs As String = "")

        Dim DT As New Data.DataTable
        DT = GetDataTable(DataConnection, sql)

        For Each DR As DataRow In DT.Rows
            Dim I As New ListItem
            I.Text = DR(0).ToString
            I.Value = DR(1).ToString
            ddl.Items.Add(I)
        Next

        If firstItemIs <> "" Then
            Dim I As New ListItem
            I.Text = firstItemIs
            I.Value = "##"
            ddl.Items.Insert(0, I)
        End If
    End Sub

End Class
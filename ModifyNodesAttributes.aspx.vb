Imports System.Data
Imports System.Data.SqlClient
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
        Panel1.Visible = False
        pnlAddAttribute.Visible = False
        ViewState("PanelDisplayedItemIndex") = -1

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
        Panel1.Visible = False
        pnlAddAttribute.Visible = False
        ViewState("PanelDisplayedItemIndex") = -1

        Dim ProjectID As String = DropDownList1.SelectedValue
        If String.IsNullOrEmpty(ProjectID) OrElse ProjectID = "##" Then
            Exit Sub
        End If

        If ListBox1.SelectedItem Is Nothing Then
            Exit Sub
        End If
        Dim NodeTypeID As String = ListBox1.SelectedValue

        Dim sql As String = "SELECT ATTRIBUTE_NAME, DISPLAY_ORDER, DATA_TYPE " &
                             "FROM UNITSHUB_ATTRIBUTES " &
                             "WHERE PROJECT_ID = " & ProjectID & " " &
                             "AND NODE_TYPE_ID = " & NodeTypeID & " " &
                             "ORDER BY DISPLAY_ORDER"

        Dim DT As DataTable = GetDataTable(EBDB_CS, sql)

        For Each DR As DataRow In DT.Rows
            Dim I As New ListItem
            I.Text = DR("ATTRIBUTE_NAME").ToString
            ' DISPLAY_ORDER is packed in first because it's the key needed to look
            ' up this attribute's row in UNITSHUB_ATTRIBUTES_PROPERTIES; DATA_TYPE
            ' rides along after the separator in case it's needed later.
            I.Value = DR("DISPLAY_ORDER").ToString & "|" & DR("DATA_TYPE").ToString
            ListBox2.Items.Add(I)
        Next

        ' Grow the box downward to exactly fit its items instead of scrolling
        ' inside a fixed-height box.
        ListBox2.Rows = Math.Max(ListBox2.Items.Count, 1)
    End Sub

    ''' <summary>
    ''' Fires when the user picks an attribute in ListBox2. Shows Panel1 and fills
    ''' it from that attribute's row in UNITSHUB_ATTRIBUTES_PROPERTIES, or hides the
    ''' panel if no attribute is selected.
    ''' </summary>
    Protected Sub ListBox2_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs)
        PopulateAttributePropertiesPanel()
    End Sub

    ''' <summary>
    ''' Builds the packed Value string stored on each ListBox2 item:
    ''' DISPLAY_ORDER|DATA_TYPE|NEW-FLAG|LOADED-FLAG|NAME_IN_UI|SEARCHABLE|SHOW_IN_UI
    ''' The last three fields cache whatever is currently showing in the panel for
    ''' this item (freshly loaded from the DB, defaulted for a new item, or edited
    ''' by the user but not yet saved), so re-selecting the item later restores
    ''' exactly what was last shown instead of re-querying or resetting to blank.
    ''' </summary>
    Private Function BuildAttributeItemValue(DisplayOrder As String, DataType As String, IsNew As Boolean,
                                              NameInUi As String, Searchable As Boolean, ShowInUi As Boolean) As String
        Return DisplayOrder & "|" & DataType & "|" &
               If(IsNew, "new", "") & "|loaded|" &
               NameInUi & "|" &
               If(Searchable, "1", "0") & "|" &
               If(ShowInUi, "1", "0")
    End Function

    ''' <summary>
    ''' Writes whatever is currently sitting in txbxName / chbxSearchable /
    ''' chbxShowInUI back onto whichever ListBox2 item was last displayed in the
    ''' panel (tracked via ViewState), so those in-progress edits aren't lost the
    ''' next time the panel is repopulated for a different item. No-ops if there's
    ''' no previously-displayed item to save into.
    ''' </summary>
    Private Sub SavePanelEditsToPreviouslyDisplayedItem()
        Dim PrevIndexObj As Object = ViewState("PanelDisplayedItemIndex")
        If PrevIndexObj Is Nothing Then Exit Sub

        Dim PrevIndex As Integer = CInt(PrevIndexObj)
        If PrevIndex < 0 OrElse PrevIndex >= ListBox2.Items.Count Then Exit Sub

        Dim PrevItem As ListItem = ListBox2.Items(PrevIndex)
        Dim Parts As String() = PrevItem.Value.Split("|"c)

        ' Only overwrite if this item's panel data had actually been loaded/shown
        ' before (7-part Value with the "loaded" flag set).
        If Parts.Length < 7 OrElse Parts(3) <> "loaded" Then Exit Sub

        Dim DisplayOrder As String = Parts(0)
        Dim DataType As String = Parts(1)
        Dim IsNew As Boolean = (Parts(2) = "new")

        PrevItem.Value = BuildAttributeItemValue(DisplayOrder, DataType, IsNew,
                                                  txbxName.Text.Trim(), chbxSearchable.Checked, chbxShowInUI.Checked)
    End Sub

    ''' <summary>
    ''' Shows Panel1 and populates txbxName / chbxSearchable / chbxShowInUI for the
    ''' attribute currently selected in ListBox2. For a brand-new (not yet saved)
    ''' attribute, or one already loaded earlier in this session, reuses the values
    ''' cached in the item's Value instead of hitting the database - which is also
    ''' what preserves unsaved edits across deselecting/reselecting the item. For
    ''' an existing attribute selected for the first time, loads its row from
    ''' UNITSHUB_ATTRIBUTES_PROPERTIES (matched by PROJECT_ID / NODE_TYPE_ID /
    ''' DISPLAY_ORDER) and caches the result onto the item. Hides the panel if
    ''' nothing is selected, or if no matching properties row is found.
    ''' </summary>
    Private Sub PopulateAttributePropertiesPanel()
        ' Preserve whatever the user was editing for the previously-shown item
        ' before we overwrite the panel controls below.
        SavePanelEditsToPreviouslyDisplayedItem()

        If ListBox2.SelectedItem Is Nothing Then
            Panel1.Visible = False
            ViewState("PanelDisplayedItemIndex") = -1
            Exit Sub
        End If

        Dim SelectedItem As ListItem = ListBox2.SelectedItem
        Dim Parts As String() = SelectedItem.Value.Split("|"c)
        Dim DisplayOrder As String = Parts(0)
        Dim DataType As String = If(Parts.Length > 1, Parts(1), "")
        Dim IsNew As Boolean = (Parts.Length > 2 AndAlso Parts(2) = "new")
        Dim IsLoaded As Boolean = (Parts.Length > 3 AndAlso Parts(3) = "loaded")

        Dim NameInUi As String
        Dim Searchable As Boolean
        Dim ShowInUi As Boolean

        If IsLoaded Then
            NameInUi = Parts(4)
            Searchable = (Parts(5) = "1")
            ShowInUi = (Parts(6) = "1")
        Else
            Dim ProjectID As String = DropDownList1.SelectedValue
            Dim NodeTypeID As String = ListBox1.SelectedValue

            Dim sql As String = "SELECT NAME_IN_UI, SEARCHABEL, SHOW_IN_UI " &
                                 "FROM UNITSHUB_ATTRIBUTES_PROPERTIES " &
                                 "WHERE PROJECT_ID = " & ProjectID & " " &
                                 "AND NODE_TYPE_ID = " & NodeTypeID & " " &
                                 "AND DISPLAY_ORDER = " & DisplayOrder

            Dim DT As DataTable = GetDataTable(EBDB_CS, sql)

            If DT.Rows.Count = 0 Then
                Panel1.Visible = False
                ViewState("PanelDisplayedItemIndex") = -1
                Exit Sub
            End If

            Dim DR As DataRow = DT.Rows(0)
            NameInUi = DR("NAME_IN_UI").ToString
            Searchable = ToBool(DR("SEARCHABEL"))
            ShowInUi = ToBool(DR("SHOW_IN_UI"))

            ' Cache the freshly-loaded values onto the item so re-selecting it
            ' later reuses them (and preserves any edits) instead of re-querying.
            SelectedItem.Value = BuildAttributeItemValue(DisplayOrder, DataType, IsNew, NameInUi, Searchable, ShowInUi)
        End If

        txbxName.Text = NameInUi
        chbxSearchable.Checked = Searchable
        chbxShowInUI.Checked = ShowInUi
        Panel1.Visible = True

        ViewState("PanelDisplayedItemIndex") = ListBox2.SelectedIndex
    End Sub

    ''' <summary>
    ''' Interprets common truthy DB representations (bit/number 1, 'Y'/'YES',
    ''' 'TRUE') as True; anything else (0, 'N', 'FALSE', NULL) as False.
    ''' </summary>
    Private Function ToBool(value As Object) As Boolean
        If value Is Nothing OrElse value Is DBNull.Value Then Return False

        Dim s As String = value.ToString().Trim()
        Return s = "1" _
            OrElse s.Equals("Y", StringComparison.OrdinalIgnoreCase) _
            OrElse s.Equals("YES", StringComparison.OrdinalIgnoreCase) _
            OrElse s.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Fires when the user clicks the Add (+) icon above ListBox2. Reveals the
    ''' inline add-attribute panel with an empty textbox.
    ''' </summary>
    Protected Sub btnAddAttribute_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        txbxNewAttributeName.Text = ""
        pnlAddAttribute.Visible = True
    End Sub

    ''' <summary>
    ''' Fires when the user clicks Cancel on the inline add-attribute panel.
    ''' Hides the panel without touching ListBox2.
    ''' </summary>
    Protected Sub btnCancelAddAttribute_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        txbxNewAttributeName.Text = ""
        pnlAddAttribute.Visible = False
    End Sub

    ''' <summary>
    ''' Fires when the user clicks Add on the inline add-attribute panel. Appends a
    ''' new item to ListBox2 for the typed attribute name, assigning it the next
    ''' DISPLAY_ORDER (one past whatever is currently the highest DISPLAY_ORDER
    ''' packed into ListBox2's existing items - see PopulateAttributesForNodeType).
    ''' Ignored if the textbox is blank. The new item's Value is suffixed with
    ''' "|new" (it has no row in UNITSHUB_ATTRIBUTES yet), then it's selected so the
    ''' properties panel opens with default values for it. Note: this only updates
    ''' the on-screen list; it does not yet INSERT the new attribute into
    ''' UNITSHUB_ATTRIBUTES.
    ''' </summary>
    Protected Sub btnConfirmAddAttribute_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim NewName As String = txbxNewAttributeName.Text.Trim()
        If NewName = "" Then
            Exit Sub
        End If

        Dim MaxDisplayOrder As Integer = 0
        For Each Item As ListItem In ListBox2.Items
            Dim OrderPart As String = Item.Value.Split("|"c)(0)
            Dim ParsedOrder As Integer
            If Integer.TryParse(OrderPart, ParsedOrder) AndAlso ParsedOrder > MaxDisplayOrder Then
                MaxDisplayOrder = ParsedOrder
            End If
        Next
        Dim NewDisplayOrder As Integer = MaxDisplayOrder + 1

        Dim NewItem As New ListItem
        NewItem.Text = NewName
        ' Already marked "loaded" with default property values (Searchable/Show
        ' In UI both off) - there's no UNITSHUB_ATTRIBUTES_PROPERTIES row to load
        ' for it yet, so PopulateAttributePropertiesPanel will use these defaults
        ' as-is instead of querying the database.
        NewItem.Value = BuildAttributeItemValue(NewDisplayOrder.ToString(), "", True, NewName, False, False)
        ListBox2.Items.Add(NewItem)

        ListBox2.Rows = Math.Max(ListBox2.Items.Count, 1)
        ListBox2.SelectedIndex = ListBox2.Items.Count - 1

        txbxNewAttributeName.Text = ""
        pnlAddAttribute.Visible = False

        ' Show the properties panel for the attribute we just added, same as if
        ' the user had clicked it in the list.
        PopulateAttributePropertiesPanel()
    End Sub

    ''' <summary>
    ''' Fires when the user clicks Update. Persists every change currently held
    ''' only in ListBox2's in-memory items:
    ''' - Any item flagged "new" (added via the inline Add panel, never saved) gets
    '''   INSERTed into UNITSHUB_ATTRIBUTES, then a blank ("") VALUE_TEXT row is
    '''   seeded into UNITSHUB_NODE_ATTRIBUTE_VALUE for every existing node of the
    '''   current project + node type, so the new attribute has a value slot on
    '''   every node right away.
    ''' - Every item whose properties panel has been loaded/edited this session
    '''   (NAME_IN_UI / SEARCHABEL / SHOW_IN_UI cached in its Value - see
    '''   PopulateAttributePropertiesPanel) gets that data upserted into
    '''   UNITSHUB_ATTRIBUTES_PROPERTIES (UPDATE if a row already exists there,
    '''   otherwise INSERT - which is always the case for a brand-new attribute).
    ''' Finally reloads ListBox2 straight from the database, which also clears the
    ''' "new" flags and closes both panels.
    ''' </summary>
    Protected Sub btnUpdate_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Dim ProjectID As String = DropDownList1.SelectedValue
        If String.IsNullOrEmpty(ProjectID) OrElse ProjectID = "##" Then
            Exit Sub
        End If
        If ListBox1.SelectedItem Is Nothing Then
            Exit Sub
        End If
        Dim NodeTypeID As String = ListBox1.SelectedValue

        ' Make sure whatever is currently sitting in the panel controls is baked
        ' into its item's cached Value before we read every item's cached data.
        SavePanelEditsToPreviouslyDisplayedItem()

        For Each Item As ListItem In ListBox2.Items
            Dim Parts As String() = Item.Value.Split("|"c)
            Dim DisplayOrder As String = Parts(0)
            Dim DataType As String = If(Parts.Length > 1, Parts(1), "")
            Dim IsNew As Boolean = (Parts.Length > 2 AndAlso Parts(2) = "new")
            Dim IsLoaded As Boolean = (Parts.Length > 3 AndAlso Parts(3) = "loaded")

            If IsNew Then
                InsertNewAttribute(ProjectID, NodeTypeID, Item.Text, DisplayOrder, DataType)
                SeedBlankNodeAttributeValues(ProjectID, NodeTypeID, DisplayOrder)
            End If

            If IsLoaded Then
                Dim NameInUi As String = Parts(4)
                Dim Searchable As Boolean = (Parts(5) = "1")
                Dim ShowInUi As Boolean = (Parts(6) = "1")
                UpsertAttributeProperties(ProjectID, NodeTypeID, DisplayOrder, NameInUi, Searchable, ShowInUi)
            End If
        Next

        ' Reload fresh from the database - clears "new" flags, closes both
        ' panels, and resets the tracked selection.
        PopulateAttributesForNodeType()
    End Sub

    ''' <summary>
    ''' Fires when the user clicks Cancel. Discards every in-memory change
    ''' (unsaved new attributes, unsaved property edits) by reloading ListBox2
    ''' straight from the database.
    ''' </summary>
    Protected Sub btnCancel_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        PopulateAttributesForNodeType()
    End Sub

    ''' <summary>
    ''' INSERTs one new row into UNITSHUB_ATTRIBUTES for a brand-new attribute.
    ''' </summary>
    Private Sub InsertNewAttribute(ProjectID As String, NodeTypeID As String, AttributeName As String, DisplayOrder As String, DataType As String)
        Dim sql As String = "INSERT INTO UNITSHUB_ATTRIBUTES (PROJECT_ID, NODE_TYPE_ID, ATTRIBUTE_NAME, DISPLAY_ORDER, DATA_TYPE) " &
                             "VALUES (@ProjectID, @NodeTypeID, @AttributeName, @DisplayOrder, @DataType)"
        DB.ExecuteNonQuery(EBDB_CS, sql,
                         New SqlParameter("@ProjectID", ProjectID),
                         New SqlParameter("@NodeTypeID", NodeTypeID),
                         New SqlParameter("@AttributeName", AttributeName),
                         New SqlParameter("@DisplayOrder", CInt(DisplayOrder)),
                         New SqlParameter("@DataType", DataType))
    End Sub

    ''' <summary>
    ''' Finds every NODE_ID in UNITSHUB_NODES for the given project + node type,
    ''' then INSERTs a blank ("") VALUE_TEXT row into UNITSHUB_NODE_ATTRIBUTE_VALUE
    ''' for each one, for the given attribute's DISPLAY_ORDER. Used to seed a value
    ''' slot for a brand-new attribute across all of that node type's existing
    ''' nodes.
    ''' </summary>
    Private Sub SeedBlankNodeAttributeValues(ProjectID As String, NodeTypeID As String, DisplayOrder As String)
        Dim nodeIdsSql As String = "SELECT NODE_ID FROM UNITSHUB_NODES WHERE PROJECT_ID = " & ProjectID & " AND NODE_TYPE_ID = " & NodeTypeID
        Dim NodesDT As DataTable = GetDataTable(EBDB_CS, nodeIdsSql)

        For Each NodeRow As DataRow In NodesDT.Rows
            Dim sql As String = "INSERT INTO UNITSHUB_NODE_ATTRIBUTE_VALUE (NODE_ID, DISPLAY_ORDER, VALUE_TEXT) " &
                                 "VALUES (@NodeID, @DisplayOrder, @ValueText)"
            DB.ExecuteNonQuery(EBDB_CS, sql,
                             New SqlParameter("@NodeID", NodeRow("NODE_ID").ToString()),
                             New SqlParameter("@DisplayOrder", CInt(DisplayOrder).ToString("000")),
                             New SqlParameter("@ValueText", ""))
        Next
    End Sub

    ''' <summary>
    ''' Updates the UNITSHUB_ATTRIBUTES_PROPERTIES row for this attribute
    ''' (matched by PROJECT_ID / NODE_TYPE_ID / DISPLAY_ORDER); if no row was
    ''' affected (none exists yet - always true for a brand-new attribute),
    ''' INSERTs one instead.
    ''' </summary>
    Private Sub UpsertAttributeProperties(ProjectID As String, NodeTypeID As String, DisplayOrder As String,
                                           NameInUi As String, Searchable As Boolean, ShowInUi As Boolean)
        Dim updateSql As String = "UPDATE UNITSHUB_ATTRIBUTES_PROPERTIES " &
                                   "SET NAME_IN_UI = @NameInUi, SEARCHABEL = @Searchable, SHOW_IN_UI = @ShowInUi " &
                                   "WHERE PROJECT_ID = @ProjectID AND NODE_TYPE_ID = @NodeTypeID AND DISPLAY_ORDER = @DisplayOrder"

        Dim RowsAffected As Integer = DB.ExecuteNonQuery(EBDB_CS, updateSql,
                         New SqlParameter("@NameInUi", NameInUi),
                         New SqlParameter("@Searchable", If(Searchable, 1, 0)),
                         New SqlParameter("@ShowInUi", If(ShowInUi, 1, 0)),
                         New SqlParameter("@ProjectID", ProjectID),
                         New SqlParameter("@NodeTypeID", NodeTypeID),
                         New SqlParameter("@DisplayOrder", CInt(DisplayOrder)))

        If RowsAffected = 0 Then
            Dim insertSql As String = "INSERT INTO UNITSHUB_ATTRIBUTES_PROPERTIES (PROJECT_ID, NODE_TYPE_ID, DISPLAY_ORDER, NAME_IN_UI, SEARCHABEL, SHOW_IN_UI) " &
                                       "VALUES (@ProjectID, @NodeTypeID, @DisplayOrder, @NameInUi, @Searchable, @ShowInUi)"
            DB.ExecuteNonQuery(EBDB_CS, insertSql,
                             New SqlParameter("@ProjectID", ProjectID),
                             New SqlParameter("@NodeTypeID", NodeTypeID),
                             New SqlParameter("@DisplayOrder", CInt(DisplayOrder)),
                             New SqlParameter("@NameInUi", NameInUi),
                             New SqlParameter("@Searchable", If(Searchable, 1, 0)),
                             New SqlParameter("@ShowInUi", If(ShowInUi, 1, 0)))
        End If
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
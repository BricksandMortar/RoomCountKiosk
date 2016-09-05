// <copyright>
// Copyright by Bricks and Mortar
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace com.bricksandmortarstudio.RoomCountKiosk
{
    [DisplayName( "Location Count" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "A list of people in a location" )]

    [CodeEditorField( "Lava Template", "Lava template to use to display the package details.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"", "", 2 )]
    [BooleanField( "Enable Debug", "Display a list of merge fields available for lava.", false, "", 3 )]
    public partial class LocationCount : RoomCountKioskBlock
    {
        #region Control Methods
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/checkin-core.js" );

            // The Querystring can override the current device / location
            if ( PageParameter( "DeviceId" ).AsIntegerOrNull().HasValue )
            {
                CurrentKioskId = PageParameter( "DeviceId" ).AsIntegerOrNull();
            }

            if ( PageParameter( "LocationId" ).AsIntegerOrNull().HasValue )
            {
                CurrentLocationId = PageParameter( "LocationId" ).AsIntegerOrNull();
            }

            if ( CurrentKioskId == null || CurrentLocationId == null )
            {
                NavigateToParentPage();
                return;
            }

            RockPage.AddScriptLink( "~/scripts/jquery.plugin.min.js" );
            RockPage.AddScriptLink( "~/scripts/jquery.countdown.min.js" );

            RegisterScript();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack && CurrentKioskId != null && CurrentLocationId != null )
            {
                string script = string.Format( @"
    <script>
        $(document).ready(function (e) {{
            if (localStorage) {{
                localStorage.theme = '{0}'
                localStorage.checkInKiosk = '{1}';
                localStorage.checkInLocation = '{2}';
            }}
        }});
    </script>
", CurrentTheme, CurrentKioskId, CurrentLocationId );
                phScript.Controls.Add( new LiteralControl( script ) );
                SaveState();
                RefreshView();
            }
        }
        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            RefreshView();
        }

        /// <summary>
        /// Handles the Click event of the btnManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnManager_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            pnlActive.Visible = false;
            pnlManager.Visible = false;
            ManagerLoggedIn = false;
            tbPIN.Text = string.Empty;

            // Get room counts
            List<int> locations = new List<int>();

            var lUl = new HtmlGenericControl( "ul" );
            lUl.AddCssClass( "kioskmanager-count-locations" );
            phCounts.Controls.Add( lUl );
            var device = new DeviceService( rockContext ).Get( CurrentKioskId.Value );
            if ( device != null )
            {
                var deviceLocations = new List<Location>();
                var locationService = new LocationService( rockContext );
                foreach ( var location in device.Locations )
                {
                    deviceLocations.Add( location );
                    deviceLocations.AddRange( locationService.GetAllDescendents( location.Id ) );
                }

                foreach ( var location in deviceLocations.Distinct().ToList() )
                {
                    if ( !locations.Contains( location.Id ) )
                    {
                        locations.Add( location.Id );
                        var locationAttendance = KioskLocationAttendance.Read( location.Id );

                        if ( locationAttendance != null )
                        {
                            var lLi = new HtmlGenericControl( "li" );
                            lUl.Controls.Add( lLi );
                            lLi.InnerHtml = string.Format( "<strong>{0}</strong>: {1}", locationAttendance.LocationName, locationAttendance.CurrentCount );

                            var gUl = new HtmlGenericControl( "ul" );
                            gUl.AddCssClass( "kioskmanager-count-groups" );
                            lLi.Controls.Add( gUl );

                            foreach ( var groupAttendance in locationAttendance.Groups )
                            {
                                var gLi = new HtmlGenericControl( "li" );
                                gUl.Controls.Add( gLi );
                                gLi.InnerHtml = string.Format( "<strong>{0}</strong>: {1}", groupAttendance.GroupName, groupAttendance.CurrentCount );

                                var sUl = new HtmlGenericControl( "ul" );
                                sUl.AddCssClass( "kioskmanager-count-schedules" );
                                gLi.Controls.Add( sUl );

                                foreach ( var scheduleAttendance in groupAttendance.Schedules.Where( s => s.IsActive ) )
                                {
                                    var sLi = new HtmlGenericControl( "li" );
                                    sUl.Controls.Add( sLi );
                                    sLi.InnerHtml = string.Format( "<strong>{0}</strong>: {1}", scheduleAttendance.ScheduleName, scheduleAttendance.CurrentCount );
                                }
                            }
                        }
                    }
                }
            }

            pnlManagerLogin.Visible = true;

            // set manager timer to 10 minutes
            hfRefreshTimerSeconds.Value = "600";
        }

        /// <summary>
        /// Handles the Click event of the btnBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnBack_Click( object sender, EventArgs e )
        {
            RefreshView();
            ManagerLoggedIn = false;
            pnlManager.Visible = false;
        }

        /// <summary>
        /// Handles the Click event of the lbLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbLogin_Click( object sender, EventArgs e )
        {
            ManagerLoggedIn = false;
            var pinAuth = AuthenticationContainer.GetComponent( typeof( Rock.Security.Authentication.PINAuthentication ).FullName );
            var rockContext = new Rock.Data.RockContext();
            var userLoginService = new UserLoginService( rockContext );
            var userLogin = userLoginService.GetByUserName( tbPIN.Text );
            if ( userLogin != null && userLogin.EntityTypeId.HasValue )
            {
                // make sure this is a PIN auth user login
                var userLoginEntityType = EntityTypeCache.Read( userLogin.EntityTypeId.Value );
                if ( userLoginEntityType != null && userLoginEntityType.Id == pinAuth.EntityType.Id )
                {
                    if ( pinAuth != null && pinAuth.IsActive )
                    {
                        // should always return true, but just in case
                        if ( pinAuth.Authenticate( userLogin, null ) )
                        {
                            if ( !( userLogin.IsConfirmed ?? true ) )
                            {
                                maWarning.Show( "Sorry, account needs to be confirmed.", Rock.Web.UI.Controls.ModalAlertType.Warning );
                            }
                            else if ( ( userLogin.IsLockedOut ?? false ) )
                            {
                                maWarning.Show( "Sorry, account is locked-out.", Rock.Web.UI.Controls.ModalAlertType.Warning );
                            }
                            else
                            {
                                ManagerLoggedIn = true;
                                ShowManagementDetails();
                                return;
                            }
                        }
                    }
                }
            }

            maWarning.Show( "Sorry, we couldn't find an account matching that PIN.", Rock.Web.UI.Controls.ModalAlertType.Warning );
        }

        /// <summary>
        /// Handles the ItemCommand event of the rLocations control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rLocations_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
        {
            int? locationId = ( e.CommandArgument as string ).AsIntegerOrNull();

            if ( locationId.HasValue )
            {
                var rockContext = new RockContext();
                var location = new LocationService( rockContext ).Get( locationId.Value );
                if ( location != null )
                {
                    if ( e.CommandName == "Open" && !location.IsActive )
                    {
                        location.IsActive = true;
                        rockContext.SaveChanges();
                        KioskDevice.FlushAll();
                    }
                    else if ( e.CommandName == "Close" && location.IsActive )
                    {
                        location.IsActive = false;
                        rockContext.SaveChanges();
                        KioskDevice.FlushAll();
                    }
                }

                BindManagerLocationsGrid();
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rLocations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rLocations_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            object locationDataItem = e.Item.DataItem;
            if ( locationDataItem != null )
            {
                var lbOpen = e.Item.FindControl( "lbOpen" ) as LinkButton;
                var lbClose = e.Item.FindControl( "lbClose" ) as LinkButton;
                var isActive = (bool)locationDataItem.GetPropertyValue( "IsActive" );

                if ( isActive )
                {
                    lbClose.RemoveCssClass( "btn-danger" );
                    lbClose.RemoveCssClass( "active" );
                    lbOpen.AddCssClass( "btn-success" );
                    lbOpen.AddCssClass( "active" );
                }
                else
                {
                    lbOpen.RemoveCssClass( "btn-success" );
                    lbOpen.RemoveCssClass( "active" );
                    lbClose.AddCssClass( "btn-danger" );
                    lbClose.AddCssClass( "active" );
                }

                var lLocationName = e.Item.FindControl( "lLocationName" ) as Literal;
                lLocationName.Text = locationDataItem.GetPropertyValue( "Name" ) as string;

                var lLocationCount = e.Item.FindControl( "lLocationCount" ) as Literal;
                lLocationCount.Text = KioskLocationAttendance.Read( (int)locationDataItem.GetPropertyValue( "LocationId" ) ).CurrentCount.ToString();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            btnBack_Click( sender, e );
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlLocation control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlLocation_SelectedIndexChanged( object sender, EventArgs e )
        {
            NavigateToPage( RockPage.Guid, new Dictionary<string, string>() { { "DeviceId", PageParameter( "DeviceId" ) }, { "LocationId", ddlLocation.SelectedValue } } );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Registers the script.
        /// </summary>
        private void RegisterScript()
        {
            // Note: the OnExpiry property of the countdown jquery plugin seems to add a new callback
            // everytime the setting is set which is why the clearCountdown method is used to prevent 
            // a plethora of partial postbacks occurring when the countdown expires.
            string script = string.Format( @"

var timeoutSeconds = $('.js-refresh-timer-seconds').val();
if (timeout) {{
    window.clearTimeout(timeout);
}}
var timeout = window.setTimeout(refreshKiosk, timeoutSeconds * 1000);

var $ActiveWhen = $('.active-when');
var $CountdownTimer = $('.countdown-timer');

function refreshKiosk() {{
    window.clearTimeout(timeout);
    {0};
}}

function clearCountdown() {{
    if ($ActiveWhen.text() != '')
    {{
        $ActiveWhen.text('');
        refreshKiosk();
    }}
}}

if ($ActiveWhen.text() != '')
{{
    var timeActive = new Date($ActiveWhen.text());
    $CountdownTimer.countdown({{
        until: timeActive, 
        compact:true, 
        onExpiry: clearCountdown
    }});
}}

", this.Page.ClientScript.GetPostBackEventReference( lbRefresh, "" ) );
            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "RefreshScript", script, true );
        }

        /// <summary>
        /// Refreshes the view.
        /// </summary>
        private void RefreshView()
        {
            hfRefreshTimerSeconds.Value = "10";
            ManagerLoggedIn = false;
            pnlActive.Visible = false;
            pnlManagerLogin.Visible = false;
            pnlManager.Visible = false;
            btnManager.Visible = true;

            lblActiveWhen.Text = string.Empty;

            if ( CurrentKioskId == null || CurrentLocationId == null )
            {
                NavigateToParentPage();
                return;
            }

            pnlActive.Visible = true;

            var currentPeople = KioskLocationAttendance.Read( CurrentLocationId.Value );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            // add link to detail page
            Dictionary<string, object> linkedPages = new Dictionary<string, object>();
            mergeFields.Add( "PersonIds", currentPeople.DistinctPersonIds );
            mergeFields.Add( "LocationCount", currentPeople.CurrentCount );
            mergeFields.Add( "CurrentKioskId", CurrentKioskId );
            mergeFields.Add( "CurrentLocationId", CurrentLocationId );

            lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );

            // show debug info
            if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
            {
                lDebug.Visible = true;
                lDebug.Text = mergeFields.lavaDebugInfo();
            }
        }

        /// <summary>
        /// Shows the management details.
        /// </summary>
        private void ShowManagementDetails()
        {
            pnlManagerLogin.Visible = false;
            pnlManager.Visible = true;
            btnManager.Visible = false;
            BindManagerLocationsGrid();
        }

        /// <summary>
        /// Binds the manager locations grid.
        /// </summary>
        private void BindManagerLocationsGrid()
        {
            var rockContext = new RockContext();
            if ( this.CurrentKioskId.HasValue )
            {
                var device = new DeviceService( rockContext ).Get( CurrentKioskId.Value );
                if ( device != null )
                {
                    var deviceLocations = new List<Location>();
                    var locationService = new LocationService( rockContext );
                    foreach ( var location in device.Locations )
                    {
                        deviceLocations.Add( location );
                        deviceLocations.AddRange( locationService.GetAllDescendents( location.Id ) );
                    }

                    ddlLocation.DataSource = deviceLocations.Distinct().ToList();
                    ddlLocation.DataTextField = "Name";
                    ddlLocation.DataValueField = "Id";
                    ddlLocation.DataBind();
                    if ( CurrentLocationId != null )
                    {
                        ddlLocation.SetValue( CurrentLocationId );
                    }
                }
            }
        }

        #endregion
    }
}
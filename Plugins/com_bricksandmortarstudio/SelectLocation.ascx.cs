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
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using com.bricksandmortarstudio.RoomCountKiosk;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace com.bricksandmortarstudio.RoomCountKiosk
{
    [DisplayName( "Select Location" )]
    [Category( "Bricks and Mortar Studio" )]
    [Description( "Check-in Administration block" )]
    [LinkedPage( "Location Page", "The page that provides information about the attendees of the location", true )]
    [BooleanField( "Enable Location Sharing", "If enabled, the block will attempt to determine the kiosk's location via location sharing geocode.", false, "Geo Location", 6 )]
    [IntegerField( "Time to Cache Kiosk GeoLocation", "Time in minutes to cache the coordinates of the kiosk. A value of zero (0) means cache forever. Default 20 minutes.", false, 20, "Geo Location", 7 )]
    [BooleanField( "Enable Kiosk Match By Name", "Enable a kiosk match by computer name by doing reverseIP lookup to get computer name based on IP address", false, "", 8, "EnableReverseLookup" )]
    public partial class SelectLocation : RoomCountKioskBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            RockPage.AddScriptLink( "~/Blocks/CheckIn/Scripts/geo-min.js" );
            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/CheckinClient/checkin-core.js" );

            if ( !Page.IsPostBack )
            {
                // Set the check-in state from values passed on query string
                bool themeRedirect = PageParameter( "ThemeRedirect" ).AsBoolean( false );

                CurrentKioskId = PageParameter( "DeviceId" ).AsIntegerOrNull();

                // If valid parameters were used, set state and navigate to welcome page
                if ( CurrentKioskId.HasValue && CurrentLocationId.HasValue && !themeRedirect )
                {
                    // Save the check-in state
                    SaveState();

                    //TODO Error Handling
                    // Navigate to the check-in home (welcome) page
                    NavigateToNextPage();
                }
                else
                {
                    bool enableLocationSharing = GetAttributeValue( "EnableLocationSharing" ).AsBoolean();

                    // Inject script used for geo location determiniation
                    if ( enableLocationSharing )
                    {
                        AddGeoLocationScript();
                    }
                    else
                    {
                        pnlManualConfig.Visible = true;
                        lbOk.Visible = true;
                        AttemptKioskMatchByIpOrName();
                    }

                    if ( !themeRedirect )
                    {
                        string script = string.Format( @"
                    <script>
                        $(document).ready(function (e) {{
                            if (localStorage) {{
                                if (localStorage.checkInKiosk) {{
                                    $('[id$=""hfKiosk""]').val(localStorage.com.bricksandmortarstudio.kiosk);
                                    if (localStorage.theme) {{
                                        $('[id$=""hfTheme""]').val(localStorage.com.bricksandmortarstudio.theme);
                                    }}
                                    if (localStorage.location) {{
                                        $('[id$=""hfLocation""]').val(localStorage.com.bricksandmortarstudio.location);
                                    }}
                                    {0};
                                }}
                            }}
                        }});
                    </script>
                ", this.Page.ClientScript.GetPostBackEventReference( lbRefresh, "" ) );
                        phScript.Controls.Add( new LiteralControl( script ) );
                    }


                    //TODO Change this to your new kiosk type
                    Guid kioskDeviceType = Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK.AsGuid();
                    using ( var rockContext = new RockContext() )
                    {
                        var deviceService = new DeviceService( rockContext );
                        var kiosks = deviceService
                            .Queryable().AsNoTracking()
                            .Where( d => d.DeviceType.Guid.Equals( kioskDeviceType ) )
                            .OrderBy( d => d.Name );
                        ddlKiosk.DataSource = kiosks.ToList();
                        ddlKiosk.DataTextField = "Name";
                        ddlKiosk.DataValueField = "Id";
                        ddlKiosk.DataBind();
                        if ( CurrentKioskId != null )
                        {
                            ddlKiosk.SetValue( CurrentKioskId.Value );
                        }

                        var kiosk = deviceService.Get( ddlKiosk.SelectedValue.AsInteger() );
                        if ( kiosk != null )
                        {
                            var kioskLocations = new List<Location>();
                            var locationService = new LocationService( rockContext );
                            foreach ( var location in kiosk.Locations )
                            {
                                kioskLocations.Add( location );
                                kioskLocations.AddRange( locationService.GetAllDescendents( location.Id ) );
                            }

                            kioskLocations = kioskLocations
                                .Where( l => l.LocationTypeValue == null || l.LocationTypeValue.Guid == Rock.SystemGuid.DefinedValue.LOCATION_TYPE_ROOM.AsGuid() )
                                .Distinct()
                                .ToList();

                            ddlLocation.DataSource = kioskLocations;
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
            }
            else
            {
                phScript.Controls.Clear();
            }
        }

        private void NavigateToNextPage()
        {
            NavigateToLinkedPage( "LocationPage", new Dictionary<string, string>() { { "DeviceId", ddlKiosk.SelectedValue }, { "LocationId", ddlLocation.SelectedValue } } );
        }

        /// <summary>
        /// Attempts to match a known kiosk based on the IP address of the client.
        /// </summary>
        private void AttemptKioskMatchByIpOrName()
        {
            // try to find matching kiosk by REMOTE_ADDR (ip/name).

            //TODO Change this to your new kiosk type
            var checkInDeviceTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;
            using ( var rockContext = new RockContext() )
            {
                bool enableReverseLookup = GetAttributeValue( "EnableReverseLookup" ).AsBoolean( false );
                var device = new DeviceService( rockContext ).GetByIPAddress( Rock.Web.UI.RockPage.GetClientIpAddress(), checkInDeviceTypeId, !enableReverseLookup );
                if ( device != null )
                {
                    ClearMobileCookie();
                    CurrentKioskId = device.Id;

                    SaveState();
                    NavigateToNextPage();
                }
            }
        }

        /// <summary>
        /// Adds GeoLocation script and calls its init() to get client's latitude/longitude before firing
        /// the server side lbCheckGeoLocation_Click click event. Puts the two values into the two corresponding
        /// hidden varialbles, hfLatitude and hfLongitude.
        /// </summary>
        private void AddGeoLocationScript()
        {
            string geoScript = string.Format( @"
    <script>
        $(document).ready(function (e) {{
            tryGeoLocation();

            function tryGeoLocation() {{
                if ( geo_position_js.init() ) {{
                    geo_position_js.getCurrentPosition(success_callback, error_callback, {{ enableHighAccuracy: true }});
                }}
                else {{
                    $(""div.checkin-header h1"").html( ""We're Sorry!"" );
                    $(""div.checkin-header h1"").after( ""<p>We don't support that kind of device yet.</p>"" );
                    alert(""We don't support that kind of device yet."");
                }}
            }}

            function success_callback( p ) {{
                var latitude = p.coords.latitude.toFixed(4);
                var longitude = p.coords.longitude.toFixed(4);
                $(""input[id$='hfLatitude']"").val( latitude );
                $(""input[id$='hfLongitude']"").val( longitude );
                $(""div.checkin-header h1"").html( 'Checking Your Location...' );
                $(""div.checkin-header"").append( ""<p class='text-muted'>"" + latitude + "" "" + longitude + ""</p>"" );
                // now perform a postback to fire the check geo location
                {0};
            }}

            function error_callback( p ) {{
                // TODO: decide what to do in this situation...
                alert( 'error=' + p.message );
            }}
        }});
    </script>
", this.Page.ClientScript.GetPostBackEventReference( lbCheckGeoLocation, "" ) );
            phScript.Controls.Add( new LiteralControl( geoScript ) );
        }

        /// <summary>
        /// Used by the local storage script to rebind the group types if they were previously
        /// saved via local storage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( hfTheme.Value ) &&
                Directory.Exists( Path.Combine( this.Page.Request.MapPath( ResolveRockUrl( "~~" ) ), hfTheme.Value ) ) )
            {
                CurrentTheme = hfTheme.Value;
                RedirectToNewTheme( hfTheme.Value );
            }
        }

        #region GeoLocation related

        /// <summary>
        /// Handles attempting to find a registered Device kiosk by it's latitude and longitude.
        /// This event method is called automatically when the GeoLocation script get's the client's location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void lbCheckGeoLocation_Click( object sender, EventArgs e )
        {
            var lat = hfLatitude.Value;
            var lon = hfLongitude.Value;
            Device kiosk = null;

            if ( !string.IsNullOrEmpty( lat ) && !string.IsNullOrEmpty( lon ) )
            {
                kiosk = GetCurrentKioskByGeoFencing( lat, lon );
            }

            if ( kiosk != null )
            {
                SetDeviceIdCookie( kiosk );

                CurrentKioskId = kiosk.Id;
                SaveState();
                //FIXME
                NavigateToNextPage();
            }
            else
            {
                TooFar();
            }
        }

        /// <summary>
        /// Sets the "DeviceId" cookie to expire after TimeToCacheKioskGeoLocation minutes
        /// if IsMobile is set.
        /// </summary>
        /// <param name="kiosk"></param>
        private void SetDeviceIdCookie( Device kiosk )
        {
            // set an expiration cookie for these coordinates.
            double timeCacheMinutes = double.Parse( GetAttributeValue( "TimetoCacheKioskGeoLocation" ) ?? "0" );

            HttpCookie deviceCookie = Request.Cookies[RoomKioskCookie.DEVICEID];
            if ( deviceCookie == null )
            {
                deviceCookie = new HttpCookie( RoomKioskCookie.DEVICEID, kiosk.Id.ToString() );
            }

            deviceCookie.Expires = ( timeCacheMinutes == 0 ) ? DateTime.MaxValue : RockDateTime.Now.AddMinutes( timeCacheMinutes );
            Response.Cookies.Set( deviceCookie );

            HttpCookie isMobileCookie = new HttpCookie( RoomKioskCookie.ISMOBILE, "true" );
            Response.Cookies.Set( isMobileCookie );
        }

        /// <summary>
        /// Clears the flag cookie that indicates this is a "mobile" device kiosk.
        /// </summary>
        private void ClearMobileCookie()
        {
            HttpCookie isMobileCookie = new HttpCookie( RoomKioskCookie.ISMOBILE );
            isMobileCookie.Expires = RockDateTime.Now.AddDays( -1d );
            Response.Cookies.Set( isMobileCookie );
        }

        /// <summary>
        /// Display a "too far" message.
        /// </summary>
        private void TooFar()
        {
            bool allowManualSetup = GetAttributeValue( "AllowManualSetup" ).AsBoolean( true );

            if ( allowManualSetup )
            {
                pnlManualConfig.Visible = true;
                lbOk.Visible = true;
                maWarning.Show( "We could not automatically determine your configuration.", ModalAlertType.Information );
            }
            else
            {
                maWarning.Show( "You are too far. Try again later.", ModalAlertType.Alert );
            }
        }

        protected void lbRetry_Click( object sender, EventArgs e )
        {
            // TODO
        }

        #endregion

        #region Manually Setting Kiosks related

        protected void lbOk_Click( object sender, EventArgs e )
        {
            if ( ddlLocation.SelectedValue == None.IdValue )
            {
                maWarning.Show( "A Location needs to be selected!", ModalAlertType.Warning );
                return;
            }

            CurrentKioskId = ddlKiosk.SelectedValue.AsIntegerOrNull();
            CurrentLocationId = ddlLocation.SelectedValue.AsIntegerOrNull();
            SaveState();

            NavigateToNextPage();
        }

        protected void ddlKiosk_SelectedIndexChanged( object sender, EventArgs e )
        {
            var device = new DeviceService( new RockContext() ).Get( ddlKiosk.SelectedValue.AsInteger() );
            if ( device != null )
            {
                CurrentKioskId = ddlKiosk.SelectedValue.AsInteger();
                BindLocationDropdown();
            }
        }

        protected void BindLocationDropdown()
        {
            if ( CurrentKioskId != null )
            {
                var device = new DeviceService( new RockContext() ).Get( CurrentKioskId.Value );
                if ( device != null )
                {
                    ddlLocation.DataSource = device.Locations;
                    ddlLocation.DataBind();
                }
            }
        }

        /// <summary>
        /// Gets the device group types.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns></returns>
        private List<GroupType> GetDeviceGroupTypes( int deviceId, RockContext rockContext )
        {
            var groupTypes = new Dictionary<int, GroupType>();

            var locationService = new LocationService( rockContext );

            // Get all locations (and their children) associated with device
            var locationIds = locationService
                .GetByDevice( deviceId, true )
                .Select( l => l.Id )
                .ToList();

            // Requery using EF
            foreach ( var groupType in locationService
                .Queryable().AsNoTracking()
                .Where( l => locationIds.Contains( l.Id ) )
                .SelectMany( l => l.GroupLocations )
                .Where( gl => gl.Group.GroupType.TakesAttendance )
                .Select( gl => gl.Group.GroupType )
                .ToList() )
            {
                groupTypes.AddOrIgnore( groupType.Id, groupType );
            }

            return groupTypes
                .Select( g => g.Value )
                .OrderBy( g => g.Order )
                .ToList();
        }

        private GroupTypeCache GetCheckinType( int? groupTypeId )
        {
            Guid templateTypeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();
            var templateType = DefinedValueCache.Read( templateTypeGuid );
            if ( templateType != null )
            {
                return GetCheckinType( GroupTypeCache.Read( groupTypeId.Value ), templateType.Id );
            }

            return null;
        }

        private GroupTypeCache GetCheckinType( GroupTypeCache groupType, int templateTypeId, List<int> recursionControl = null )
        {
            if ( groupType != null )
            {
                recursionControl = recursionControl ?? new List<int>();
                if ( !recursionControl.Contains( groupType.Id ) )
                {
                    recursionControl.Add( groupType.Id );
                    if ( groupType.GroupTypePurposeValueId.HasValue && groupType.GroupTypePurposeValueId == templateTypeId )
                    {
                        return groupType;
                    }

                    foreach ( var parentGroupType in groupType.ParentGroupTypes )
                    {
                        var checkinType = GetCheckinType( parentGroupType, templateTypeId, recursionControl );
                        if ( checkinType != null )
                        {
                            return checkinType;
                        }
                    }
                }
            }

            return null;
        }

        private List<GroupTypeCache> GetDescendentGroupTypes( GroupTypeCache groupType, List<int> recursionControl = null )
        {
            var results = new List<GroupTypeCache>();

            if ( groupType != null )
            {
                recursionControl = recursionControl ?? new List<int>();
                if ( !recursionControl.Contains( groupType.Id ) )
                {
                    recursionControl.Add( groupType.Id );
                    results.Add( groupType );

                    foreach ( var childGroupType in groupType.ChildGroupTypes )
                    {
                        var childResults = GetDescendentGroupTypes( childGroupType, recursionControl );
                        childResults.ForEach( c => results.Add( c ) );
                    }
                }
            }

            return results;
        }

        
        /// <summary>
        /// Returns a kiosk based on finding a geo location match for the given latitude and longitude.
        /// </summary>
        /// <param name="sLatitude">latitude as string</param>
        /// <param name="sLongitude">longitude as string</param>
        /// <returns></returns>
        public static Device GetCurrentKioskByGeoFencing( string sLatitude, string sLongitude )
        {
            double latitude = double.Parse( sLatitude );
            double longitude = double.Parse( sLongitude );
            var checkInDeviceTypeId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK ).Id;

            // We need to use the DeviceService until we can get the GeoFence to JSON Serialize/Deserialize.
            using ( var rockContext = new RockContext() )
            {
                Device kiosk = new DeviceService( rockContext ).GetByGeocode( latitude, longitude, checkInDeviceTypeId );
                return kiosk;
            }
        }

        private void RedirectToNewTheme( string theme )
        {
            var pageRef = RockPage.PageReference;
            pageRef.QueryString = new System.Collections.Specialized.NameValueCollection();
            pageRef.Parameters = new Dictionary<string, string>();
            pageRef.Parameters.Add( "theme", theme );
            pageRef.Parameters.Add( "KioskId", CurrentKioskId.ToString() ?? "0" );
            pageRef.Parameters.Add( "ThemeRedirect", "True" );

            Response.Redirect( pageRef.BuildUrl(), false );
        }

        #endregion
    }
}
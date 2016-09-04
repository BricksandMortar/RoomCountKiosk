using System;
using System.Collections.Generic;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;
namespace com.bricksandmortarstudio.RoomCountKiosk
{
        public abstract class RoomCountKioskBlock : RockBlock
        {

            /// <summary>
            /// The current theme.
            /// </summary>
            protected string CurrentTheme { get; set; }

            /// <summary>
            /// The current kiosk id
            /// </summary>
            protected int? CurrentKioskId { get; set; }

            protected int? CurrentLocationId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [manager logged in].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [manager logged in]; otherwise, <c>false</c>.
            /// </value>
            protected bool ManagerLoggedIn { get; set; }

            /// <summary>
            /// Holds cookie names shared across certain check-in blocks.
            /// </summary>
            public struct RoomKioskCookie
            {
                /// <summary>
                /// The name of the cookie that holds the DeviceId. Setters of this cookie should
                /// be sure to set the expiration to a time when the device is no longer valid.
                /// </summary>
                public static readonly string DEVICEID = "com.bricksandmortarstudio.RoomCountKiosk.DeviceId";

                /// <summary>
                /// The name of the cookie that holds whether or not the device was a mobile device.
                /// </summary>
                public static readonly string ISMOBILE = "com.bricksandmortarstudio.RoomCountKiosk.IsMobile";
            }

            /// <summary>
            /// Gets a value indicating whether the kiosk has active group types and locations that 
            /// are open for check-in.
            /// </summary>
            /// <value>
            /// <c>true</c> if kiosk is active; otherwise, <c>false</c>.
            /// </value>
            //protected bool KioskCurrentlyActive
            //{
            //    get
            //    {
            //        if ( CurrentCheckInState == null ||
            //            CurrentCheckInState.Kiosk == null ||
            //            CurrentCheckInState.Kiosk.FilteredGroupTypes( CurrentGroupTypeIds ).Count == 0 ||
            //            !CurrentCheckInState.Kiosk.HasActiveLocations( CurrentGroupTypeIds ) )
            //        {
            //            return false;
            //        }
            //        else
            //        {
            //            return true;
            //        }
            //    }
            //}


            /// <summary>
            /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
            /// </summary>
            /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
            protected override void OnInit( EventArgs e )
            {
                base.OnInit( e );
                GetState();
            }

            /// <summary>
            /// Saves the current state of the kiosk and workflow
            /// </summary>
            protected void SaveState()
            {
                if ( !string.IsNullOrWhiteSpace( CurrentTheme ) )
                {
                    Session["CheckInTheme"] = CurrentTheme;
                }

                if ( CurrentKioskId.HasValue )
                {
                    Session["CurrentKioskId"] = CurrentKioskId.Value;
                }
                else
                {
                    Session.Remove( "CurrentKioskId" );
                }

                if ( CurrentLocationId.HasValue )
                {
                    Session["CurrentLocationId"] = CurrentLocationId.Value;
                }
                else
                {
                    Session.Remove( "CurrentLocationId" );
                }
            }
          

            /// <summary>
            /// Gets the state.
            /// </summary>
            private void GetState()
            {
                if ( Session["CurrentTheme"] != null )
                {
                    CurrentTheme = Session["CurrentTheme"].ToString();
                }

                if ( Session["CheckInKioskId"] != null )
                {
                    CurrentKioskId = ( int ) Session["CheckInKioskId"];
                }

                if ( Session["CurrentLocationId"] != null )
                {
                    CurrentLocationId = ( int ) Session["CurrentLocationId"];
                }
            }

        }
}

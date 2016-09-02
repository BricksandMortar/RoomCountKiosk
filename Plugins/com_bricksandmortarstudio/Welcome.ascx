<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Welcome.ascx.cs" Inherits="RockWeb.Blocks.CheckIn.Welcome" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <Triggers>
        <%-- make sure lbLogin and lbCancel causes a full postback due to an issue with buttons not firing in IE after clicking the login button --%>
        <asp:PostBackTrigger ControlID="lbLogin" />
        <asp:PostBackTrigger ControlID="lbCancel" />
    </Triggers>

    <ContentTemplate>

        <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>

        <Rock:HiddenFieldWithClass ID="hfRefreshTimerSeconds" runat="server" CssClass="js-refresh-timer-seconds" />

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <span style="display: none">
            <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click"></asp:LinkButton>
            <asp:Label ID="lblActiveWhen" runat="server" CssClass="active-when" />
        </span>

        <%-- Panel for no schedules --%>
        <asp:Panel ID="pnlNotActive" runat="server">
            <div class="checkin-header">
                <h1>Check-in Is Not Active</h1>
            </div>

            <div class="checkin-body">

                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <p>There are no current or future schedules for this kiosk today!</p>
                    </div>
                </div>

            </div>
        </asp:Panel>

        <%-- Panel for schedule not active yet --%>
        <asp:Panel ID="pnlNotActiveYet" runat="server">
            <div class="checkin-header">
                <h1>Check-in Is Not Active Yet</h1>
            </div>

            <div class="checkin-body">

                <div class="checkin-scroll-panel">
                    <div class="scroller">

                        <p>This kiosk is not active yet.  Countdown until active: <span class="countdown-timer"></span></p>
                        <asp:HiddenField ID="hfActiveTime" runat="server" />

                    </div>
                </div>

            </div>
        </asp:Panel>

        <%-- Panel for location closed --%>
        <asp:Panel ID="pnlClosed" runat="server">
            <div class="checkin-header checkin-closed-header">
                <h1>Closed</h1>
            </div>

            <div class="checkin-body checkin-closed-body">
                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <p>This location is currently closed.</p>
                    </div>
                </div>
            </div>
        </asp:Panel>

        <%-- Panel for active checkin --%>
        <asp:Panel ID="pnlActive" runat="server">

            <div class="checkin-body">
                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <div class="checkin-search-actions checkin-start">
                            <Rock:RockDropDownList ID="ddlLocations" runat="server" Label="Room" OnSelectedIndexChanged="ddlLocations_SelectedIndexChanged" ></Rock:RockDropDownList>
                        </div>
                    </div>
                </div>
            </div>

        </asp:Panel>

        <asp:LinkButton runat="server" ID="btnManager" CssClass="kioskmanager-activate" OnClick="btnManager_Click"><i class="fa fa-cog fa-4x"></i></asp:LinkButton>

    </ContentTemplate>
</asp:UpdatePanel>

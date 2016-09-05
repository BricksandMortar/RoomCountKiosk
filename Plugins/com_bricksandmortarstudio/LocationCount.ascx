<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LocationCount.ascx.cs" Inherits="com.bricksandmortarstudio.RoomCountKiosk.LocationCount" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <Triggers>
        <%-- make sure lbLogin and lbCancel causes a full postback due to an issue with buttons not firing in IE after clicking the login button --%>
        <asp:PostBackTrigger ControlID="lbLogin" />
        <asp:PostBackTrigger ControlID="lbCancel" />
    </Triggers>

    <ContentTemplate>
        <script>

            Sys.Application.add_load(function () {
                if ($('.js-manager-login').is(':visible')) {
                    $('.tenkey a.digit').click(function () {
                        $phoneNumber = $("input[id$='tbPIN']");
                        $phoneNumber.val($phoneNumber.val() + $(this).html());
                    });
                    $('.tenkey a.back').click(function () {
                        $phoneNumber = $("input[id$='tbPIN']");
                        $phoneNumber.val($phoneNumber.val().slice(0, -1));
                    });
                    $('.tenkey a.clear').click(function () {
                        $phoneNumber = $("input[id$='tbPIN']");
                        $phoneNumber.val('');
                    });

                    // set focus to the input unless on a touch device
                    var isTouchDevice = 'ontouchstart' in document.documentElement;
                    if (!isTouchDevice) {
                        if ($('.checkin-phone-entry').length) {
                            $('.checkin-phone-entry').focus();
                        }
                    }
                }
                else {
                    // set focus to body if the manager login (ten-key) isn't visible, to fix buttons not working after showing the ten-key panel
                    $('body').focus();
                }
            });

        </script>

        <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>

        <Rock:HiddenFieldWithClass ID="hfRefreshTimerSeconds" runat="server" CssClass="js-refresh-timer-seconds" />

        <Rock:ModalAlert ID="maWarning" runat="server" />

        <span style="display: none">
            <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click"></asp:LinkButton>
            <asp:Label ID="lblActiveWhen" runat="server" CssClass="active-when" />
        </span>

        <%-- Panel for active checkin --%>
        <asp:Panel ID="pnlActive" runat="server">
            <asp:Literal ID="lOutput" runat="server" Visible="true" />
            <asp:Literal ID="lDebug" runat="server" Visible="false" />
        </asp:Panel>

        <asp:LinkButton runat="server" ID="btnManager" CssClass="kioskmanager-activate" OnClick="btnManager_Click"><i class="fa fa-cog fa-4x"></i></asp:LinkButton>

        <%-- Panel for checkin manager --%>
        <asp:Panel ID="pnlManager" runat="server" Visible="false">
            <div class="checkin-header">
                <h1>Locations</h1>
            </div>
            <Rock:RockDropDownList ID="ddlLocation" runat="server" CssClass="input-xlarge" OnSelectedIndexChanged="ddlLocation_SelectedIndexChanged" AutoPostBack="true" />

            <div class="controls kioskmanager-actions checkin-actions">
                <asp:LinkButton ID="btnBack" runat="server" CssClass="btn btn-default btn-large btn-block btn-checkin-select" Text="Back" OnClick="btnBack_Click" />
            </div>

        </asp:Panel>

        <%-- Panel for checkin manager login --%>
        <asp:Panel ID="pnlManagerLogin" CssClass="js-manager-login" runat="server" Visible="false">

            <div class="checkin-header">
                <h1>Manager Login</h1>
            </div>

            <div class="checkin-body">

                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <div class="row">
                            <div class="col-md-6">
                                <div class="checkin-search-body">
                                    <Rock:RockTextBox ID="tbPIN" CssClass="checkin-phone-entry" TextMode="Password" runat="server" Label="PIN" />

                                    <div class="tenkey checkin-phone-keypad">
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">1</a>
                                            <a href="#" class="btn btn-default btn-lg digit">2</a>
                                            <a href="#" class="btn btn-default btn-lg digit">3</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">4</a>
                                            <a href="#" class="btn btn-default btn-lg digit">5</a>
                                            <a href="#" class="btn btn-default btn-lg digit">6</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">7</a>
                                            <a href="#" class="btn btn-default btn-lg digit">8</a>
                                            <a href="#" class="btn btn-default btn-lg digit">9</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg command back">Back</a>
                                            <a href="#" class="btn btn-default btn-lg digit">0</a>
                                            <a href="#" class="btn btn-default btn-lg command clear">Clear</a>
                                        </div>
                                    </div>

                                    <div class="checkin-actions">
                                        <asp:LinkButton ID="lbLogin" runat="server" OnClick="lbLogin_Click" CssClass="btn btn-primary">Login</asp:LinkButton>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="kioskmanager-counts">
                                    <h3>Current Counts</h3>
                                    <asp:PlaceHolder ID="phCounts" runat="server"></asp:PlaceHolder>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="checkin-footer">

                <div class="checkin-actions">
                    <asp:LinkButton ID="lbCancel" runat="server" CausesValidation="false" OnClick="lbCancel_Click" CssClass="btn btn-default">Cancel</asp:LinkButton>
                </div>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>

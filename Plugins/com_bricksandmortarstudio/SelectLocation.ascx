<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelectLocation.ascx.cs" Inherits="com.bricksandmortarstudio.RoomCountKiosk.SelectLocation" %>
<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>
    <asp:HiddenField ID="hfLatitude" runat="server" />
    <asp:HiddenField ID="hfLongitude" runat="server" />
    <asp:HiddenField ID="hfTheme" runat="server" />
    <asp:HiddenField ID="hfKiosk" runat="server" />
    <asp:HiddenField ID="hfLocation" runat="server" />
    <span style="display:none">
        <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click"></asp:LinkButton>
        <asp:LinkButton ID="lbCheckGeoLocation" runat="server" OnClick="lbCheckGeoLocation_Click"></asp:LinkButton>
    </span>

    <Rock:ModalAlert ID="maWarning" runat="server" />

    <div class="checkin-header">
        <h1>Kiosk Options</h1>
    </div>
    
    <asp:Panel runat="server" CssClass="checkin-body" ID="pnlManualConfig" Visible="false">

        <div class="checkin-scroll-panel">
            <div class="scroller">
                <Rock:RockDropDownList ID="ddlKiosk" runat="server" CssClass="input-xlarge" Label="Kiosk Device" OnSelectedIndexChanged="ddlKiosk_SelectedIndexChanged" AutoPostBack="true" DataTextField="Name" DataValueField="Id" />
                <Rock:RockDropDownList ID="ddlLocation" runat="server" CssClass="input-xlarge" Label="Location"/>
            </div>
        </div>

    </asp:Panel>
    

    <div class="checkin-footer">   
        <div class="checkin-actions">
            <asp:LinkButton CssClass="btn btn-primary" ID="lbOk" runat="server" OnClick="lbOk_Click" Text="OK" Visible="false" />
            <a class="btn btn-primary" runat="server" ID="lbRetry" visible="false" href="javascript:window.location.href=window.location.href" >Retry</a>
        </div>
    </div>

</ContentTemplate>
</asp:UpdatePanel>

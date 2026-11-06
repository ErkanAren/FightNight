using System.Collections.Generic;
using UFE3D;
using UnityEngine.UI;

public class DefaultNetworkConnectionScreen : NetworkConnectionScreen
{
    public Button buttonConnect;
    public Button buttonBluetooth;
    public Button buttonLAN;
    public Text connectionInfo;
    public Dropdown dropDown;

    private readonly Dictionary<string, string> regionDictionary = new Dictionary<string, string>
        {
            {"Best", ""},
            {"Australia", "asia"},
            {"Brazil", "au"},
            {"Canada", "cae"},
            {"China", "cn"},
            {"India", "eu"},
            {"Japan", "in"},
            {"Netherlands", "jp"},
            {"Singapore", "za"},
            {"South Africa", "sa"},
            {"South Korea", "kr"},
            {"Turkey", "tr"},
            {"USA, Central", "us"},
            {"USA, East", "usw"},
            {"USA, West", "ussc"}
        };

    #region public override methods
    public override void OnShow()
    {
        base.OnShow();

        connectionInfo.text = "";

        if (buttonConnect != null)
        {
            buttonConnect.interactable = UFE.IsNetworkAddonInstalled && UFE.config.networkOptions.networkService != NetworkService.Disabled;
        }

        if (buttonLAN != null)
        {
            buttonLAN.interactable = UFE.IsMirrorInstalled;
        }

        if (buttonBluetooth != null)
        {
            buttonBluetooth.interactable = UFE.IsBluetoothAddonInstalled;
        }
    }

    public override void ConnectToServer()
    {
        UFE.MultiplayerAPI.SetRegion(regionDictionary[dropDown.options[dropDown.value].text]);
        base.ConnectToServer();
        buttonConnect.interactable = false;
        if (buttonLAN != null) buttonLAN.interactable = false;
        buttonBluetooth.interactable = false;
        connectionInfo.text = "Connecting...";
    }
    public override void OnInitializationSuccessful()
    {
        base.OnInitializationSuccessful();
        connectionInfo.text = "Connected";
        UFE.StartNetworkOptionsScreen();
    }

    public override void OnInitializationError()
    {
        base.OnInitializationError();
        buttonConnect.interactable = true;
        buttonLAN.interactable = true;
        buttonBluetooth.interactable = true;
        connectionInfo.text = "Connection Error";
    }
    #endregion
}
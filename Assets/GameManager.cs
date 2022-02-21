using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

//Moralis
using MoralisWeb3ApiSdk;
using Moralis.Platform.Objects;

//WalletConnect
using WalletConnectSharp.Core.Models;
using WalletConnectSharp.Unity;

public class GameManager : MonoBehaviour
{
    #region PUBLIC_FIELDS

    public MoralisController moralisController;
    public WalletConnect walletConnect;

    public GameObject connectButton;
    public Text walletAddress;

    public Text infoText;

    #endregion

    #region UNITY_LIFECYCLE

    private async void Start()
    {
        if (moralisController != null)
        {
            await moralisController.Initialize();
        }
        else
        {
            Debug.LogError("MoralisController not found.");
        }
    }

    private void OnApplicationQuit()
    {
        LogOut();
    }

    #endregion

    #region WALLET_CONNECT

    public async void WalletConnectHandler(WCSessionData data)
    {
        Debug.Log("Wallet connection received");
        // Extract wallet address from the Wallet Connect Session data object.
        string address = data.accounts[0].ToLower();
        string appId = MoralisInterface.GetClient().ApplicationId;
        long serverTime = 0;

        // Retrieve server time from Moralis Server for message signature
        Dictionary<string, object> serverTimeResponse = await MoralisInterface.GetClient().Cloud.RunAsync<Dictionary<string, object>>("getServerTime", new Dictionary<string, object>());

        if (serverTimeResponse == null || !serverTimeResponse.ContainsKey("dateTime") ||
            !long.TryParse(serverTimeResponse["dateTime"].ToString(), out serverTime))
        {
            Debug.Log("Failed to retrieve server time from Moralis Server!");
        }

        Debug.Log($"Sending sign request for {address} ...");

        string signMessage = $"Moralis Authentication\n\nId: {appId}:{serverTime}";
        string response = await walletConnect.Session.EthPersonalSign(address, signMessage);

        Debug.Log($"Signature {response} for {address} was returned.");

        // Create moralis auth data from message signing response.
        Dictionary<string, object> authData = new Dictionary<string, object> { { "id", address }, { "signature", response }, { "data", signMessage } };

        Debug.Log("Logging in user.");

        // Attempt to login user.
        MoralisUser user = await MoralisInterface.LogInAsync(authData);

        if (user != null)
        {
            Debug.Log($"User {user.username} logged in successfully. ");
            infoText.text = "Logged in successfully!";
        }
        else
        {
            Debug.Log("User login failed.");
            infoText.text = "Login failed";
        }

        UserLoggedInHandler();
    }

    public void WalletConnectSessionEstablished(WalletConnectUnitySession session)
    {
        InitializeWeb3();
    }

    private void InitializeWeb3()
    {
        MoralisInterface.SetupWeb3();
    }

    #endregion

    #region PRIVATE_METHODS

    private async void UserLoggedInHandler()
    {
        var user = await MoralisInterface.GetUserAsync();

        if (user != null)
        {
            connectButton.SetActive(false);

            string addr = user.authData["moralisEth"]["id"].ToString();
            walletAddress.text = "Formatted Wallet Address:\n" + string.Format("{0}...{1}", addr.Substring(0, 6), addr.Substring(addr.Length - 3, 3));
        }
    }

    private async void LogOut()
    {
        await walletConnect.Session.Disconnect();
        walletConnect.CLearSession();

        await MoralisInterface.LogOutAsync();
    }

    #endregion

    #region EDITOR_METHODS

    public void HandleWalletConnected()
    {
        connectButton.SetActive(false);
        infoText.text = "Connection successful. Please sign message";
    }

    public void HandleWalledDisconnected()
    {
        infoText.text = "Connection failed. Try again!";
    }

    #endregion
}
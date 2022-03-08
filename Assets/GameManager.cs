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
using Moralis.Web3Api.Models;
using Assets.Scripts.Moralis;

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
            List<ChainEntry> chains = MoralisInterface.SupportedChains;

            Debug.Log((ChainList)4);
            string addr = user.authData["moralisEth"]["id"].ToString();
            List<Erc20TokenBalance> balanceList = await MoralisInterface.GetClient().Web3Api.Account.GetTokenBalances(addr.ToLower(), (ChainList)3);
            Debug.Log("balanceListcount"+balanceList.Count);
            foreach (var v in balanceList) {
                Debug.Log("tokenbalancelist"+v);
            }
            double balance = 0.0;
            NativeBalance bal =
            await MoralisInterface.GetClient().Web3Api.Account.GetNativeBalance(addr.ToLower(),
                                        (ChainList)4);

            Debug.Log("balance" + bal.ToString());
            if (bal != null && !string.IsNullOrWhiteSpace(bal.Balance))
            {
                double.TryParse(bal.Balance, out balance);
            }
            
            // Display native token amount (ETH) in fractions of ETH.
            // NOTE: May be better to link this to chain since some tokens may have
            // more than 18 sigjnificant figures.
            //balanceText.text = string.Format("{0:0.##} ETH", balance / (double)Mathf.Pow(10.0f, 18.0f));
            walletAddress.text = "Formatted Wallet Address:\n" + string.Format("{0}...{1}", addr.Substring(0, 6), addr.Substring(addr.Length - 3, 3))+ string.Format("{0:0.##} ETH", balance / (double)Mathf.Pow(10.0f, 18.0f)); ;
            
            
            /*//balanceText.text = string.Format("{0:0.##} ETH", balance / (double)Mathf.Pow(10.0f, 18.0f));
            // Make sure a response to the balanace request weas received. The 
            // IsNullOrWhitespace check may not be necessary ...
            if (bal != null && !string.IsNullOrWhiteSpace(bal.Balance))
            {
                double.TryParse(bal.Balance, out balance);
            }

            // Display native token amount (ETH) in fractions of ETH.
            // NOTE: May be better to link this to chain since some tokens may have
            // more than 18 sigjnificant figures.*/
            
        }
        // Retrieve account balanace.
        
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
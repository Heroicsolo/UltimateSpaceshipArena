using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class ChatMessage
{
    public string nickname;
    public string msg;
}

public class ChatManager : MonoBehaviour, IChatClientListener
{
    public static ChatManager instance;

    private ChatClient chatClient;
    private bool m_justEntered = false;
    private bool m_needToConnect = false;
    private List<GameObject> chatMessages = new List<GameObject>();

    [SerializeField] private int maxChatMessagesCount = 20;
    [SerializeField] private Transform chatContent;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private TMP_InputField chatMessageField;
    [SerializeField] private GameObject chatLoadingIndicator;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        if (m_justEntered) return;

        m_justEntered = true;

        chatClient = new ChatClient(this);

        chatClient.ChatRegion = "EU";
        chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion, new Photon.Chat.AuthenticationValues(PhotonNetwork.NickName));
    }

    public void ReconnectIfNeeded()
    {
        if (m_needToConnect)
        {
            chatClient.ChatRegion = "EU";
            chatClient.Connect(PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat, PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion, new Photon.Chat.AuthenticationValues(PhotonNetwork.NickName));
        }
    }

    private void OnApplicationQuit()
    {
        if (chatClient != null)
        {
            chatClient.Disconnect();
        }
    }

    private void Update()
    {
        if (chatClient != null)
            chatClient.Service();
    }

    public void DebugReturn(DebugLevel level, string message)
    {
        Debug.Log(message);
    }

    public void SendChatMessage()
    {
        if (chatMessageField.text.Length > 0)
        {
            if (SendPublicMessage(chatMessageField.text))
                chatMessageField.text = "";
        }
    }

    public bool SendPublicMessage(string text)
    {
        if (chatClient.CanChat)
        {
            ChatMessage msg = new ChatMessage();
            msg.nickname = PhotonNetwork.NickName;
            msg.msg = text;

            string jsonStr = JsonUtility.ToJson(msg);

            chatClient.PublishMessage("General", jsonStr);

            return true;
        }

        return false;
    }

    public void PostOnlineStatus()
    {
        if (chatClient.CanChat)
        {
            ChatMessage msg = new ChatMessage();
            msg.nickname = "/online";
            msg.msg = "";

            string jsonStr = JsonUtility.ToJson(msg);

            chatClient.PublishMessage("General", jsonStr);
        }
    }

    public void OnConnected()
    {
        chatClient.Subscribe("General");
        chatClient.SetOnlineStatus(ChatUserStatus.Online);
        if (m_justEntered)
            PostOnlineStatus();
        m_justEntered = false;
        m_needToConnect = false;
        chatLoadingIndicator.SetActive(false);
        chatMessageField.interactable = true;
    }

    public void OnDisconnected()
    {
        chatLoadingIndicator.SetActive(true);
        chatMessageField.interactable = false;
        m_needToConnect = true;
    }

    public void OnChatStateChange(ChatState state)
    {

    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            GameObject chatMsgGO = Instantiate(chatMessagePrefab, chatContent);

            ChatMessage msgObj = JsonUtility.FromJson<ChatMessage>(messages[i].ToString());

            if (msgObj.nickname == "/online")
            {
                chatMsgGO.GetComponent<TextMeshProUGUI>().text = LangResolver.instance.GetLocalizedString("HasEnteredGame", senders[i], "green");
            }
            else
            {
                chatMsgGO.GetComponent<TextMeshProUGUI>().text = "<color=\"green\">" + msgObj.nickname + ": </color>" + msgObj.msg;
            }

            chatMessages.Add(chatMsgGO);

            if (chatMessages.Count > maxChatMessagesCount)
            {
                GameObject firstChatMsg = chatMessages[0];
                chatMessages.RemoveAt(0);
                Destroy(firstChatMsg);
            }
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {

    }

    public void OnSubscribed(string[] channels, bool[] results)
    {

    }

    public void OnUnsubscribed(string[] channels)
    {

    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {

    }

    public void OnUserSubscribed(string channel, string user)
    {

    }

    public void OnUserUnsubscribed(string channel, string user)
    {

    }
}

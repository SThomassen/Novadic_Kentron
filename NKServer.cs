using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class NKServer : NetworkDiscovery
{
	public delegate void OnClientDisconnectedHandler();
	public event OnClientDisconnectedHandler OnClientDisconnected;

    //private int m_port = 7777;

	// Use this for initialization
	void Start () {

        Application.runInBackground = true;
        
        showGUI = false;
        broadcastData = NetworkManager.singleton.networkPort.ToString();// m_port.ToString();

		NetworkServer.Configure(NKNetwork.ConnectionConfig, 4);

		Initialize();
        StartAsServer();

        NetworkServer.Listen(NetworkManager.singleton.networkPort);
        NetworkServer.RegisterHandler(MsgType.Connect, OnServerConnected);
        NetworkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnected);
        

        //NetworkManager.singleton.networkPort = m_port;
        //NetworkManager.singleton.StartServer();
        //NetworkManager.singleton.useGUILayout = false;

        //NKNetwork.Instance.debug.text += "\n" + broadcastPort;
        
    }

    public void OnServerConnected(NetworkMessage a_msg)
    {
        //Debug.Log("Server Connected: "+m_port);
        //NKNetwork.Instance.debug.text += "\nServer Connected: " + m_port;
        //if (m_debug) m_debug.text += "\n" + "Server Connected..";
        //StopBroadcast();
        //NetworkServer.UnregisterHandler(MsgType.Connect);
        NetworkServer.RegisterHandler(MessageTypes.MSG_TRANSFORM, OnChangeTransform);

        if (NKNetwork.Instance.debug != null)
            NKNetwork.Instance.debug.text = "";

		NKCamera nkCamera = Camera.main.GetComponent<NKCamera>();
		if (nkCamera != null)
		{
			nkCamera.SwitchScene("Lobby_Scene", null, LoadSceneMode.Additive);
		}
		else
		{
			Debug.LogWarning("Could not find NKCamera component on main camera. Did not switch scenes");
		}

		//SceneManager.LoadScene("Lobby_Scene", LoadSceneMode.Single);
		SceneManager.LoadScene("Host_Scene", LoadSceneMode.Additive);
    }

    private void OnServerDisconnected(NetworkMessage a_msg)
    {
        //NetworkServer.UnregisterHandler(MsgType.Disconnect);
        NetworkServer.UnregisterHandler(MessageTypes.MSG_TRANSFORM);
        NetworkServer.DisconnectAll();

		if (OnClientDisconnected != null)
		{
			OnClientDisconnected.Invoke();
		}

		Camera.main.GetComponent<NKCamera>().SwitchScene("Lobby_Scene", null, LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync("Host_Scene");

        //NKNetwork.Instance.debug.text += "Searching for Connection.. \n";
    }

    public void OnChangeTransform(NetworkMessage a_msg)
    {
        PostTransform msg = a_msg.ReadMessage<PostTransform>();

        Quaternion quat = Quaternion.Euler(msg.pitch, msg.yaw, 0);
        if (Camera.main != null)
            Camera.main.transform.rotation = quat;
    }
}

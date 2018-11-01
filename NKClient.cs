using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class NKClient : NetworkDiscovery
{
    private NetworkClient m_client;
    private bool m_foundServer = false;

	public bool IsConnected
	{
		get
		{
			return m_client != null && m_client.isConnected;
		}
	}

	public delegate void OnConnectedToServerDelegate();
	public event OnConnectedToServerDelegate OnConnectedToServer;

	private void Start()
    {
        m_client = new NetworkClient();
		m_client.Configure(NKNetwork.ConnectionConfig, 4);

        showGUI = false;

        Initialize();
        StartAsClient();
        Debug.Log("start as Client");
    }

    public override void OnReceivedBroadcast(string fromAddress, string data)
    {
        base.OnReceivedBroadcast(fromAddress, data);
        
        //Debug.Log("Received broadcast from: " + fromAddress+":"+ data);
        if (!m_foundServer)
        {
            string removeFrom = "::ffff:";
            string ip = fromAddress.Remove(0, removeFrom.Length);
            int port = int.Parse(data);

            Debug.Log("IP: " + ip + ":" + data);
			if (NKNetwork.Instance.debug != null)
			{
				NKNetwork.Instance.debug.text = string.Format("IP: {1}:{0}", port, ip);
			}

			m_client.RegisterHandler(MsgType.Connect, OnClientConnected);
            m_client.Connect(ip, port);

            /*
            NetworkManager.singleton.networkAddress = ip;
            NetworkManager.singleton.networkPort = port;
            NetworkManager.singleton.StartClient();
            */
            m_foundServer = true;
        }
    }

	private void OnClientConnected(NetworkMessage a_msg)
    {
        Debug.Log("Client Connected");
		if (NKNetwork.Instance.debug != null)
		{
			NKNetwork.Instance.debug.text = "Client Connected";
		}

		NKCamera nkCamera = Camera.main.GetComponent<NKCamera>();
		if (nkCamera != null)
		{
			nkCamera.SwitchScene("ClientVideoSyncScene", null, LoadSceneMode.Single);
		}
		else
		{
			Debug.LogWarning("Could not find NKCamera component on main camera. Did not switch scenes");
		}

		m_client.UnregisterHandler(MsgType.Connect);
        m_client.RegisterHandler(MessageTypes.MSG_SCENE, OnSceneChanged);
        m_client.RegisterHandler(MsgType.Disconnect, OnClientDisconnected);

		if (OnConnectedToServer != null)
		{
			OnConnectedToServer.Invoke();
		}
	}

    private void OnClientDisconnected(NetworkMessage a_msg)
    {
        m_client.UnregisterHandler(MsgType.Disconnect);
        m_client.UnregisterHandler(MessageTypes.MSG_SCENE);

        m_foundServer = false;
        Camera.main.GetComponent<NKCamera>().SwitchScene("Lobby_Scene", null, LoadSceneMode.Single);
        //NKNetwork.Instance.debug.text += "Waiting for Client..\n";
    }

	private void OnSceneChanged(NetworkMessage a_msg)
    {
        ChangeSceneMessage msg = a_msg.ReadMessage<ChangeSceneMessage>();
        Camera.main.GetComponent<NKCamera>().SwitchScene(msg.m_scene, msg.m_args,LoadSceneMode.Single);
    }

	public void RegisterHandler(short a_messageType, NetworkMessageDelegate a_handlerFunction)
	{
		if (m_client != null)
		{
			m_client.RegisterHandler(a_messageType, a_handlerFunction);
		}
		else
		{
			Debug.LogError("Could not register message handler. Client not constructed yet");
		}
	}

	public void UnregisterHandler(short a_messageType)
	{
		if (m_client != null)
		{
			m_client.UnregisterHandler(a_messageType);
		}
		else
		{
			Debug.LogError("Could not unregister message handler. Client not constructed yet");
		}
	}

	public void Send(short a_type, MessageBase a_base)
    {
		if (m_client != null && m_client.isConnected)
		{
			m_client.Send(a_type, a_base);
		}
		else
		{
			Debug.LogError("Could not send message. Client is not constructed or connected yet");
		}
	}
}
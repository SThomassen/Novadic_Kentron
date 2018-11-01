using System.Collections;
using System.Collections.Generic;
using Plugins.NK_Networking.Scripts.VideoFileSync;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NKNetwork : NetworkManager {

    private string m_externalIP;
    private static NKNetwork m_instance;

    public Text m_debug;
    //public int m_port = 7777;

    public enum ConnectionType { Client, Server, None };
    public ConnectionType m_connection;

    private NKClient m_client;

    public Text debug
    {
        get
        {
            return m_debug;
        }
    }

    public bool IsServer
    {
        get
        {
            if (m_connection == ConnectionType.Server)
                return true;

            return false;
        }
    }

    public static NKNetwork Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<NKNetwork>();
            }
            return m_instance;
        }
    }

	public static ConnectionConfig ConnectionConfig;
	public static readonly byte CHANNEL_RELIABLE_FRAGMENTED_SEQUENCED;

	static NKNetwork()
	{
		ConnectionConfig = new ConnectionConfig
		{
			//PacketSize = 1470,
			//FragmentSize = 1000,
			SendDelay = 0,
			AcksType = ConnectionAcksType.Acks32,
			MaxCombinedReliableMessageCount = 0
		};
		ConnectionConfig.AddChannel(QosType.Unreliable);
		CHANNEL_RELIABLE_FRAGMENTED_SEQUENCED = ConnectionConfig.AddChannel(QosType.ReliableFragmented);
	}

	private void Start()
    {
        /*
#if !UNITY_EDITOR
#if UNITY_ANDROID
        m_connection = ConnectionType.Client;
#endif

#if UNITY_STANDALONE
        m_connection = ConnectionType.Server;
#endif
#endif
*/
        if (m_connection == ConnectionType.Client) {
            if (m_debug) m_debug.text += "Searching for Connection.. \n";
            m_client = gameObject.AddComponent<NKClient>();
        }
        else if (m_connection == ConnectionType.Server)
        {
            if (m_debug) m_debug.text += "Waiting for Client..\n";
            gameObject.AddComponent<NKServer>();
			gameObject.AddComponent<NKVideoFileServer>();
		}
    }

    public void SendMessage(Vector3 a_rotation)
	{
		if (m_client == null || !m_client.IsConnected)
			return;

        PostTransform msg = new PostTransform();
        msg.pitch = a_rotation.x;
        msg.yaw = a_rotation.y;
        m_client.Send(MessageTypes.MSG_TRANSFORM, msg);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        if (m_debug) m_debug.text += "\nManager: Client Connected";
        Debug.Log("Manager: Client Connected");
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);

        Debug.Log("Manager: Server Connected");
        if (m_debug) m_debug.text += "\nManager: Server Connected";
    }
    
}
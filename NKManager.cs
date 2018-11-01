using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class NKManager : MonoBehaviour {

    public void OnChangeScene(string a_scene)
    {
        FindObjectOfType<NKCamera>().SwitchScene(a_scene, null, LoadSceneMode.Additive);

		ChangeSceneMessage msg = new ChangeSceneMessage {m_scene = a_scene};
		NetworkServer.SendToAll(MessageTypes.MSG_SCENE, msg);
    }

	public void OnReturnToLobbyScene()
	{
		ChangeToVideoScene("NK_Offices_360.mp4");
	}

	public static void ChangeToVideoScene(string a_videoFilename)
	{
		FindObjectOfType<NKCamera>().SwitchScene("360VideoScene", a_videoFilename, LoadSceneMode.Additive);

		ChangeSceneMessage msg = new ChangeSceneMessage {m_scene = "360VideoScene", m_args = a_videoFilename};
		NetworkServer.SendToAll(MessageTypes.MSG_SCENE, msg);
	}

	public void OnShowCredits()
    {
        SceneManager.LoadScene("Credits", LoadSceneMode.Additive);
    }

    public void OnToggleWindow(Animator a_animator)
    {
        a_animator.SetBool("active", !a_animator.GetBool("active"));
    }

	public void OnResyncVideos()
	{
		FindObjectOfType<NKCamera>().SwitchScene("Lobby_scene", null, LoadSceneMode.Additive);

		ChangeSceneMessage msg = new ChangeSceneMessage() {m_scene = "ClientVideoSyncScene"};
		NetworkServer.SendToAll(MessageTypes.MSG_SCENE, msg);
	}
}


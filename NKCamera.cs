using System.Collections;
using Plugins.NK_Networking.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class NKCamera : MonoBehaviour {
    public Animator m_animator;
    private VideoPlayer m_video;

    //private float timer = 0;
    //[Range(0.0f, 1.0f)] public float rate = 0.1f;

    private bool localPlayer = true;

    private string m_scene;
    private LoadSceneMode m_mode;
	private string m_sceneArgs;
	
    private Scene m_previous;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Login_Scene" || SceneManager.GetActiveScene().name == "Lobby_Scene")
            m_animator.SetBool("fade", false);

        m_video = FindObjectOfType<VideoPlayer>();

        m_scene = SceneManager.GetActiveScene().name;

        if (NKNetwork.Instance.IsServer)
            localPlayer = false;
    }

    private void Update()
    {
        if (localPlayer)
            StartCoroutine(CmdSendTransform());
        /*
        timer += Time.deltaTime;
        if (timer > rate && localPlayer)
        {
            
            timer -= rate;
        }*/
    }

    private IEnumerator CmdSendTransform()
    {
        if (!NKNetwork.Instance.IsServer)
            NKNetwork.Instance.SendMessage(transform.rotation.eulerAngles);

        yield return null;
    }

    public void SwitchScene(string a_scene, string a_sceneArgs, LoadSceneMode a_mode)
    {
        if (m_video) m_video.Stop();
        Debug.Log("Load: " + a_scene);
        m_animator.SetBool("fade", true);

        m_scene = a_scene;
        m_mode = a_mode;
		m_sceneArgs = a_sceneArgs;

		Invoke("Switch", 3);
    }

    private void Switch()
    {
        SceneManager.LoadScene(m_scene, m_mode);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene a_scene, LoadSceneMode a_mode)
    {
        if (a_mode == LoadSceneMode.Additive && SceneManager.sceneCount > 1)
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

		NKCamera camera = FindObjectOfType<NKCamera>();
		if (camera != null)
		{
			camera.m_animator.SetBool("fade", true);
			camera.Invoke("WaitFade", 3);
		}

		if (m_sceneArgs != null)
		{
			NKVideoFilePlayer[] filePlayers = FindObjectsOfType<NKVideoFilePlayer>();
			if (filePlayers != null)
			{
				foreach (NKVideoFilePlayer player in filePlayers)
				{
					player.StartPlayingVideoFileByName(m_sceneArgs);
				}
			}
		}

		SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneUnloaded(Scene a_scene)
    {
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        if (FindObjectOfType<NKCamera>().m_animator.GetBool("fade"))
        {
            FindObjectOfType<NKCamera>().m_animator.SetBool("fade", true);
            FindObjectOfType<NKCamera>().Invoke("WaitFade", 3);
        }
    }

    private void WaitFade()
    {
        FindObjectOfType<NKCamera>().m_animator.SetBool("fade", false);
    }
}

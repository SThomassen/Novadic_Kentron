using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Plugins.NK_Networking.Scripts
{
	public class NKVideoFilePlayer: MonoBehaviour
	{
		private const string INTERNAL_VIDEO_FOLDER = "360Videos";

		[SerializeField]
		private VideoPlayer m_TargetPlayer = null;

		public void StartPlayingVideoFileByName(string a_videoFilename)
		{
			VideoClip builtinClip = Resources.Load<VideoClip>(Path.Combine(INTERNAL_VIDEO_FOLDER, a_videoFilename));
			if (builtinClip != null)
			{
				m_TargetPlayer.source = VideoSource.VideoClip;
				m_TargetPlayer.clip = builtinClip;
				return;
			}

			m_TargetPlayer.source = VideoSource.Url;
			//Server side.
			string assetDataPath = Path.Combine(Path.Combine(Application.dataPath, "../Videos/"), a_videoFilename);
			if (File.Exists(assetDataPath))
			{
				m_TargetPlayer.url = assetDataPath;
			}
			else
			{
				//Client side
				string persistentDataPath = Path.Combine(Path.Combine(Application.persistentDataPath, "Videos/"), a_videoFilename);
				if (File.Exists(persistentDataPath))
				{
					m_TargetPlayer.url = persistentDataPath;
				}
				else
				{
					Debug.LogError("Could not find video file with name " + a_videoFilename + " at either " + assetDataPath + " or " + persistentDataPath);
				}
			}
		}
	}
}

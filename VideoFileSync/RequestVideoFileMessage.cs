using UnityEngine.Networking;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	internal class RequestVideoFileMessage : MessageBase
	{
		public string m_videoFilename;

		public RequestVideoFileMessage()
		{
		}

		public RequestVideoFileMessage(string a_filename)
		{
			m_videoFilename = a_filename;
		}
	}
}
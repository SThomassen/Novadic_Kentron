using UnityEngine.Networking;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	class AcknowledgeFileChunkMessage: MessageBase
	{
		public string m_filename;
		public long m_fileDataOffset;

		public AcknowledgeFileChunkMessage()
		{
		}

		public AcknowledgeFileChunkMessage(string a_filename, long a_fileDataOffset)
		{
			m_filename = a_filename;
			m_fileDataOffset = a_fileDataOffset;
		}
	}
}

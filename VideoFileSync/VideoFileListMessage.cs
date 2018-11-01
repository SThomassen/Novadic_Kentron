using System;
using UnityEngine.Networking;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	[Serializable]
	public class VideoFileListMessage: MessageBase
	{
		[Serializable]
		public class FileEntry
		{
			public string Filename;
			public long FileSize;
			public string Checksum;
		}

		public FileEntry[] m_files = null;

		public VideoFileListMessage()
		{
		}

		public VideoFileListMessage(FileEntry[] a_fileEntries)
		{
			m_files = a_fileEntries;
		}
	}
}

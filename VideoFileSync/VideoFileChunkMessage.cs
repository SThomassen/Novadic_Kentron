using UnityEngine.Networking;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	class VideoFileChunkMessage: MessageBase
	{
		public string Filename;
		public byte[] FileChunkData;
		public long FileDataOffsetBytes;
		public long FileDataTotalSize; //Efficiency ho!

		public VideoFileChunkMessage()
		{
		}

		public VideoFileChunkMessage(string a_filename, byte[] a_fileChunkData, long a_fileDataOffsetBytes, long a_fileDataTotalSize)
		{
			Filename = a_filename;
			FileChunkData = a_fileChunkData;
			FileDataOffsetBytes = a_fileDataOffsetBytes;
			FileDataTotalSize = a_fileDataTotalSize;
		}
	}
}

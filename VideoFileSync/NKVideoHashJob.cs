using System.IO;
using System.Threading;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	class NKVideoHashJob
	{
		public bool IsDone
		{
			get
			{
				return m_thread != null && !m_thread.IsAlive;
			}
		}

		private Thread m_thread = null;
		public readonly string FilePath;

		public string Checksum
		{
			get;
			private set;
		}

		public long FileSize
		{
			get;
			private set;
		}

		public NKVideoHashJob(string a_filePath)
		{
			FilePath = a_filePath;
		}

		public void StartJob()
		{
			m_thread = new Thread(ProcessJob)
			{
				IsBackground = true
			};
			m_thread.Start();
		}

		private void ProcessJob()
		{
			FileInfo fileInfo = new FileInfo(FilePath);
			FileSize = fileInfo.Length;
			using (FileStream fs = fileInfo.OpenRead())
			{
				Checksum = NKVideoFileUtility.CalculateFileHash(fs);
			}
		}

		public void BlockingWaitForFinish()
		{
			m_thread.Join(); 
		}
	}
}

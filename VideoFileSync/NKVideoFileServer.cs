using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	public class NKVideoFileServer: MonoBehaviour
	{
		private const int FILE_CHUNK_SIZE = 60000;

		public class VideoFileEntry
		{
			public readonly string Filename;
			public readonly string FullPath;
			public readonly long FileSize;
			public readonly string CheckSum;

			public VideoFileEntry(string a_fullPath, long a_fileSize, string a_checksum)
			{
				Filename = Path.GetFileName(a_fullPath);
				FullPath = a_fullPath;
				FileSize = a_fileSize;
				CheckSum = a_checksum;
			}
		}

		private class QueuedFileChunk
		{
			public readonly VideoFileEntry m_videoFile;
			public readonly NetworkConnection m_targetConnection;
			public readonly long m_chunkDataStart;
			public readonly long m_chunkDataLength;

			public QueuedFileChunk(VideoFileEntry a_videoFile, NetworkConnection a_targetConnection, long a_chunkDataStart, long a_chunkDataLength)
			{
				m_videoFile = a_videoFile;
				m_targetConnection = a_targetConnection;
				m_chunkDataStart = a_chunkDataStart;
				m_chunkDataLength = a_chunkDataLength;
			}
		}

		private NKServer m_server;
		private string m_videoFileDirectory;
		private List<VideoFileEntry> m_fileEntries = new List<VideoFileEntry>();
		private Queue<NKVideoHashJob> m_hashJobQueue = new Queue<NKVideoHashJob>();
		private NKVideoHashJob m_runningHashJob = null;

		//If we are going to support multiple clients this needs to be reworked
		private Queue<QueuedFileChunk> m_queuedChunkTransfers = new Queue<QueuedFileChunk>(128);
		private List<VideoFileChunkMessage> m_activeChunkTransfers = new List<VideoFileChunkMessage>(4);

		private event Action OnFileHashCompletedEvent;

		private void Awake()
		{
			m_videoFileDirectory = Path.Combine(Application.dataPath, "../Videos/");
			PopulateVideoFileHashJobs();

			m_server = GetComponent<NKServer>();

			if (m_server != null)
			{
				m_server.OnClientDisconnected += OnClientDisconnected;
				NetworkServer.RegisterHandler(MessageTypes.VIDEO_REQUEST_FILE_LIST, OnRequestFileList);
				NetworkServer.RegisterHandler(MessageTypes.VIDEO_REQUEST_FILE, OnRequestFile);
				NetworkServer.RegisterHandler(MessageTypes.VIDEO_FILE_CHUNK_ACKNOWLEDGE, OnAcknowledgeChunk);
			}
			else
			{
				Debug.LogError("Could not find server on game object of VideoFileServer component");
			}
		}

		private void OnDestroy()
		{
			NetworkServer.UnregisterHandler(MessageTypes.VIDEO_REQUEST_FILE_LIST);
			NetworkServer.UnregisterHandler(MessageTypes.VIDEO_REQUEST_FILE);
			NetworkServer.UnregisterHandler(MessageTypes.VIDEO_FILE_CHUNK_ACKNOWLEDGE);
		}

		private void OnClientDisconnected()
		{
			m_queuedChunkTransfers.Clear();
			m_activeChunkTransfers.Clear();
		}

		private void Update()
		{
			if (m_activeChunkTransfers.Capacity > m_activeChunkTransfers.Count && m_queuedChunkTransfers.Count > 0)
			{
				QueuedFileChunk chunkToTransmit = m_queuedChunkTransfers.Dequeue();
				using (FileStream videoFile = File.OpenRead(chunkToTransmit.m_videoFile.FullPath))
				{
					byte[] fileData = new byte[chunkToTransmit.m_chunkDataLength];
					videoFile.Seek(chunkToTransmit.m_chunkDataStart, SeekOrigin.Begin);
					videoFile.Read(fileData, 0, (int)chunkToTransmit.m_chunkDataLength);
					
					VideoFileChunkMessage message = new VideoFileChunkMessage(chunkToTransmit.m_videoFile.Filename, fileData,
						chunkToTransmit.m_chunkDataStart, videoFile.Length);

					chunkToTransmit.m_targetConnection.SetChannelOption(NKNetwork.CHANNEL_RELIABLE_FRAGMENTED_SEQUENCED,
						ChannelOption.MaxPendingBuffers, 128);

					chunkToTransmit.m_targetConnection.SendByChannel(MessageTypes.VIDEO_FILE_CHUNK_TRANSFER, message,
						NKNetwork.CHANNEL_RELIABLE_FRAGMENTED_SEQUENCED);
					m_activeChunkTransfers.Add(message);

					//Debug.Log("Transmitting Chunk of file " + chunkToTransmit.m_videoFile.Filename + " offset " + chunkToTransmit.m_chunkDataStart + " size " + chunkToTransmit.m_chunkDataLength);
				}
			}

			UpdateHashJobQueue();
		}

		private void UpdateHashJobQueue()
		{
			if (m_runningHashJob != null && m_runningHashJob.IsDone)
			{
				Debug.Log("Finished hashing " + m_runningHashJob.FilePath + " " + m_hashJobQueue.Count + " files remaining");

				m_fileEntries.Add(new VideoFileEntry(m_runningHashJob.FilePath, m_runningHashJob.FileSize, m_runningHashJob.Checksum));
				m_runningHashJob = null;

				if (m_hashJobQueue.Count == 0)
				{
					if (OnFileHashCompletedEvent != null)
					{
						OnFileHashCompletedEvent.Invoke();
						OnFileHashCompletedEvent = null;
					}
				}
			}

			if (m_runningHashJob == null && m_hashJobQueue.Count > 0)
			{
				m_runningHashJob = m_hashJobQueue.Dequeue();
				m_runningHashJob.StartJob();
			}
		}

		private void PopulateVideoFileHashJobs()
		{
			DirectoryInfo info = new DirectoryInfo(m_videoFileDirectory);

			string fileExtension = "*.mp4";
			foreach (FileInfo file in info.GetFiles(fileExtension))
			{
				m_hashJobQueue.Enqueue(new NKVideoHashJob(file.FullName));
			}
		}

		private void OnRequestFile(NetworkMessage a_netMessage)
		{
			RequestVideoFileMessage msg = a_netMessage.ReadMessage<RequestVideoFileMessage>();
			VideoFileEntry entry = m_fileEntries.Find(a_obj => a_obj.Filename == msg.m_videoFilename);
			if (entry != null)
			{
				QueueFileForTransfer(entry, a_netMessage.conn);
			}
			else
			{
				Debug.LogError("Client requested video file " + msg.m_videoFilename +
							   " but this file was not found in our video file entries");
			}
		}

		private void QueueFileForTransfer(VideoFileEntry a_videoFile, NetworkConnection a_target)
		{
			FileInfo fileInfo = new FileInfo(a_videoFile.FullPath);
			long fileSize = fileInfo.Length;
			int chunkCount = Mathf.CeilToInt((float)fileInfo.Length / (float)FILE_CHUNK_SIZE);

			for (int i = 0; i < chunkCount; ++i)
			{
				long chunkStart = i * FILE_CHUNK_SIZE;
				long chunkSize = FILE_CHUNK_SIZE;
				if (chunkStart + chunkSize > fileSize)
				{
					chunkSize = fileSize - chunkStart;
				}

				QueuedFileChunk queuedChunk = new QueuedFileChunk(a_videoFile, a_target, chunkStart, chunkSize);
				m_queuedChunkTransfers.Enqueue(queuedChunk);
			}
		}

		private void OnRequestFileList(NetworkMessage a_netMessage)
		{
			if (m_runningHashJob != null || m_hashJobQueue.Count > 0)
			{
				//Not done hashing yet. Queue the response.
				OnFileHashCompletedEvent += () => { SendFileListResponse(a_netMessage.conn); };
			}
			else
			{
				SendFileListResponse(a_netMessage.conn);
			}
		}

		private void SendFileListResponse(NetworkConnection a_targetConnection)
		{
			List<VideoFileListMessage.FileEntry> fileEntries = new List<VideoFileListMessage.FileEntry>();
			foreach (VideoFileEntry entry in m_fileEntries)
			{
				fileEntries.Add(new VideoFileListMessage.FileEntry
				{
					Filename = entry.Filename,
					FileSize = entry.FileSize,
					Checksum = entry.CheckSum
				});
			}
			VideoFileListMessage msg = new VideoFileListMessage(fileEntries.ToArray());
			a_targetConnection.Send(MessageTypes.VIDEO_FILE_LIST, msg);
		}

		private void OnAcknowledgeChunk(NetworkMessage a_netMessage)
		{
			AcknowledgeFileChunkMessage msg = a_netMessage.ReadMessage<AcknowledgeFileChunkMessage>();
			VideoFileChunkMessage acknowledgedChunk = m_activeChunkTransfers.Find(a_obj =>
				a_obj.Filename == msg.m_filename && a_obj.FileDataOffsetBytes == msg.m_fileDataOffset);
			if (acknowledgedChunk != null)
			{
				m_activeChunkTransfers.Remove(acknowledgedChunk);
			}
			else
			{
				Debug.LogError("Got an acknowledge message for a chunk that is not being transmitted.");
			}
		}

		public void GetVideoFileList(Action<IEnumerable<VideoFileEntry>> a_callbackAction)
		{
			//We have to route this through a callback as we can't guarantee the file hashing has been completed when we enter the menu.
			if (m_runningHashJob == null && m_hashJobQueue.Count == 0)
			{
				a_callbackAction(m_fileEntries);
			}
			else
			{
				OnFileHashCompletedEvent += () => { a_callbackAction.Invoke(m_fileEntries); };
			}
		}
	}
}

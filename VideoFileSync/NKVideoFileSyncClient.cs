using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	class NKVideoFileSyncClient: MonoBehaviour
	{
		private const string FILE_HASH_EXTENSION = ".nkhash";

		private class FileRequest
		{
			public string m_filename;
			public long m_fileSizeBytes;
			public long m_bytesReceived;
			public float m_startTime;
			public string m_fileHash;
		}

		private NKClient m_client = null;
		private string m_videoPath;

		private List<FileRequest> m_filesToRequest = null;
		private FileRequest m_outstandingFileRequest = null;
		private bool m_fileTransferCompleteCallbackCalled = false;

		[SerializeField]
		private Text m_statusOutputText;

		private void Awake()
		{
			m_videoPath = Path.Combine(Application.persistentDataPath, "Videos/");

			m_client = FindObjectOfType<NKClient>(); //Blech, Client is on an object that is present on DontDestroyOnLoad object. So we find it by all.
			if (m_client == null)
			{
				Debug.LogError("could not find NK client on Video File Sync client component's game object", gameObject);
				return;
			}

			if (m_client.IsConnected)
			{
				OnConnectedToServer();
			}
			else
			{
				m_client.OnConnectedToServer += OnConnectedToServer;
			}
		}

		private void OnDestroy()
		{
			m_client.UnregisterHandler(MessageTypes.VIDEO_FILE_LIST);
			m_client.UnregisterHandler(MessageTypes.VIDEO_FILE_CHUNK_TRANSFER);
		}

		private void Update()
		{
			if (m_outstandingFileRequest == null && m_filesToRequest != null && m_filesToRequest.Count > 0)
			{
				RequestVideoFile(m_filesToRequest[0]);
				m_outstandingFileRequest = m_filesToRequest[0];
				m_outstandingFileRequest.m_startTime = Time.realtimeSinceStartup;
				m_filesToRequest.RemoveAt(0);
			}

			if (m_outstandingFileRequest == null && m_filesToRequest != null && m_filesToRequest.Count == 0 && m_fileTransferCompleteCallbackCalled == false)
			{
				m_fileTransferCompleteCallbackCalled = true;
				OnFileSyncCompleted();
			}
		}

		private void OnFileSyncCompleted()
		{
			NKCamera nkCamera = Camera.main.GetComponent<NKCamera>();
			if (nkCamera != null)
			{
				nkCamera.SwitchScene("Lobby_Scene", null, LoadSceneMode.Single);
			}
		}

		private void OnConnectedToServer()
		{
			m_client.RegisterHandler(MessageTypes.VIDEO_FILE_LIST, OnReceiveVideoFileList);
			m_client.RegisterHandler(MessageTypes.VIDEO_FILE_CHUNK_TRANSFER, OnReceiveVideoFile);

			m_client.Send(MessageTypes.VIDEO_REQUEST_FILE_LIST, new RequestVideoFileListMessage());
		}

		private void OnReceiveVideoFileList(NetworkMessage a_netMessage)
		{
			VideoFileListMessage msg = a_netMessage.ReadMessage<VideoFileListMessage>();

			m_filesToRequest = new List<FileRequest>(msg.m_files.Length);
			for (int i = 0; i < msg.m_files.Length; ++i)
			{
				ProcessFileListing(msg.m_files[i]);
			}

			m_fileTransferCompleteCallbackCalled = false;
		}

		private void ProcessFileListing(VideoFileListMessage.FileEntry a_msgFile)
		{
			bool wantsFile = false;
			string fullFilePath = Path.Combine(m_videoPath, a_msgFile.Filename);
			string hashFilePath = fullFilePath + FILE_HASH_EXTENSION;
			if (File.Exists(fullFilePath))
			{
				string hash;
				if (File.Exists(hashFilePath))
				{
					byte[] hashBytes = File.ReadAllBytes(hashFilePath);
					hash = Encoding.ASCII.GetString(hashBytes);
				}
				else
				{
					//File exists just hash it I guess?
					NKVideoHashJob hashJob = new NKVideoHashJob(fullFilePath);
					hashJob.StartJob();
					//Lolololol
					hashJob.BlockingWaitForFinish();

					File.WriteAllBytes(hashFilePath, Encoding.ASCII.GetBytes(hashJob.Checksum));
					hash = hashJob.Checksum;
				}

				wantsFile = hash != a_msgFile.Checksum;
			}
			else
			{
				wantsFile = true;
			}

			if (wantsFile)
			{
				AddFileRequest(a_msgFile);
			}
		}

		private void OnReceiveVideoFile(NetworkMessage a_netMessage)
		{
			VideoFileChunkMessage chunkMessage = a_netMessage.ReadMessage<VideoFileChunkMessage>();

			if (!Directory.Exists(m_videoPath))
			{
				Directory.CreateDirectory(m_videoPath);
			}

			string outputFilePath = Path.Combine(m_videoPath, chunkMessage.Filename);
			using (FileStream fileStream = File.OpenWrite(outputFilePath))
			{
				if (fileStream.Length != chunkMessage.FileDataTotalSize)
				{
					fileStream.SetLength(chunkMessage.FileDataTotalSize);
				}

				fileStream.Seek(chunkMessage.FileDataOffsetBytes, SeekOrigin.Begin);
				fileStream.Write(chunkMessage.FileChunkData, 0, chunkMessage.FileChunkData.Length);
			}

			a_netMessage.conn.Send(MessageTypes.VIDEO_FILE_CHUNK_ACKNOWLEDGE, new AcknowledgeFileChunkMessage(chunkMessage.Filename, chunkMessage.FileDataOffsetBytes));

			if (m_outstandingFileRequest.m_filename == chunkMessage.Filename)
			{
				m_outstandingFileRequest.m_bytesReceived += chunkMessage.FileChunkData.Length;
				UpdateStatusText(m_outstandingFileRequest);

				if (m_outstandingFileRequest.m_bytesReceived == m_outstandingFileRequest.m_fileSizeBytes)
				{
					using (FileStream fs = File.OpenWrite(outputFilePath + FILE_HASH_EXTENSION))
					{
						fs.Seek(0, SeekOrigin.Begin);
						byte[] bytes = Encoding.ASCII.GetBytes(m_outstandingFileRequest.m_fileHash);
						fs.Write(bytes, 0, bytes.Length);
					}

					OnFileTransferComplete();
				}
			}
		}

		private void UpdateStatusText(FileRequest a_fileRequest)
		{
			if (m_statusOutputText != null)
			{
				float percentage = (float)a_fileRequest.m_bytesReceived / (float)a_fileRequest.m_fileSizeBytes;
				float transferRateKBs = ((float)a_fileRequest.m_bytesReceived / (Time.realtimeSinceStartup - a_fileRequest.m_startTime)) / 1024;
				m_statusOutputText.text = string.Format("File {0} {1}% ({2} kB/s)", a_fileRequest.m_filename, percentage * 100.0f,
					transferRateKBs);
			}
		}

		private void OnFileTransferComplete()
		{
			m_outstandingFileRequest = null;
		}

		private void AddFileRequest(VideoFileListMessage.FileEntry a_fileEntry)
		{
			FileRequest request = new FileRequest
				{
					m_filename = a_fileEntry.Filename,
					m_fileSizeBytes = a_fileEntry.FileSize,
					m_fileHash = a_fileEntry.Checksum
				};
			m_filesToRequest.Add(request);
		}

		private void RequestVideoFile(FileRequest a_request)
		{
			m_client.Send(MessageTypes.VIDEO_REQUEST_FILE, new RequestVideoFileMessage(a_request.m_filename));
		}
	}
}

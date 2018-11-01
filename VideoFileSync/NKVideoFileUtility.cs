using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Plugins.NK_Networking.Scripts.VideoFileSync
{
	public static class NKVideoFileUtility
	{
		public static string CalculateFileHash(FileStream a_fileStream)
		{
			string result;
			using (var md5 = MD5.Create())
			{
				result = Encoding.ASCII.GetString(md5.ComputeHash(a_fileStream));
			}

			return result;
		}
	}
}

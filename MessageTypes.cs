public class MessageTypes
{
	public static short MSG_SCENE = 1005;
	public static short MSG_VIDEO_SCENE = 1006;
	public static short MSG_TRANSFORM = 1010;

	public const short VIDEO_REQUEST_FILE_LIST = 2001;
	public const short VIDEO_FILE_LIST = 2002;
	public const short VIDEO_REQUEST_FILE = 2003;
	public const short VIDEO_FILE_CHUNK_TRANSFER = 2004;	//Send a chunk of a file. (VideoFileChunkMessage)
	public const short VIDEO_FILE_CHUNK_ACKNOWLEDGE = 2005;	//Send acknowledge of file (AcknowledgeFileChunkMessage)
};
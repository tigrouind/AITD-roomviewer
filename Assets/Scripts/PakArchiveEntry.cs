public class PakArchiveEntry
{
	public int Index;
	public int CompressedSize;
	public int UncompressedSize;

	internal PakArchive Archive;
	internal byte CompressionFlags;
	internal byte CompressionType;
	internal int Offset;

	public byte[] Read()
	{
		return Archive.GetData(this);
	}
}
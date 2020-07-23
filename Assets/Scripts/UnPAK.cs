using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class UnPAK : IDisposable
{
	[DllImport("UnPAK")]
	static extern void PAK_explode(byte[] srcBuffer, byte[] dstBuffer, uint compressedSize, uint uncompressedSize, ushort flags);

	readonly FileStream stream;
	readonly BinaryReader reader;
	int? entryCount;

	public UnPAK(string filename)
	{
		stream = new FileStream(filename, FileMode.Open);
		reader = new BinaryReader(stream);
	}

	public byte[] GetEntry(int index)
	{
		stream.Position = (index + 1) * 4;
		stream.Position = reader.ReadUInt32() + 4;

		var compressedSize = reader.ReadUInt32();
		var uncompressedSize = reader.ReadUInt32();
		var flag = reader.ReadByte();
		var info5 = reader.ReadByte();
		var offset = reader.ReadUInt16();
		stream.Position += offset;

		var dest = new byte[uncompressedSize];

		switch (flag)
		{
			case 0: //uncompressed
			{
				stream.Read(dest, 0, (int)compressedSize);
				break;
			}

			case 1: //pak explode
			{
				var source = new byte[compressedSize];
				stream.Read(source, 0, (int)compressedSize);
				PAK_explode(source, dest, compressedSize, uncompressedSize, info5);
				break;
			}

			case 4: //deflate
			{
				using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress, true))
				{
					deflateStream.Read(dest, 0, (int)uncompressedSize);
				}
				break;
			}

			default:
				throw new NotSupportedException();
		}

		return dest;
	}

	public int EntryCount
	{
		get
		{
			if(!entryCount.HasValue)
			{
				entryCount = GetEntryCount();
			}

			return entryCount.Value;
		}
	}

	int GetEntryCount()
	{
		stream.Position = 4;
		int offset = reader.ReadInt32();
		int count = offset / 4 - 1;

		stream.Position = offset - 4;
		if(reader.ReadInt32() == 0) count--; //TIMEGATE

		return count;
	}

	public void Dispose()
	{
		reader.Close();
		stream.Close();
	}
}

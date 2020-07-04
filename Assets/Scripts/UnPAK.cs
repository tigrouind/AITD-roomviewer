using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public static class UnPAK
{
	[DllImport("UnPAK")]
	static extern void PAK_explode(byte[] srcBuffer, byte[] dstBuffer, uint compressedSize, uint uncompressedSize, ushort flags);

	public static byte[] ReadFile(string fileName, int index)
	{
		using(var stream = new FileStream(fileName, FileMode.Open))
		using(var reader = new BinaryReader(stream))
		{
			stream.Position = 4 + index * 4;
			stream.Position = 4 + reader.ReadUInt32();

			var compressedSize = reader.ReadUInt32();
			var uncompressedSize = reader.ReadUInt32();
			var flag = reader.ReadByte();
			var info5 = reader.ReadByte();
			int offset = reader.ReadUInt16();
			stream.Position += offset;

			var dest = new byte[uncompressedSize];

			switch (flag)
			{
				case 0:
				{
					stream.Read(dest, 0, (int)compressedSize);
					break;
				}

				case 1:
				{
					var source = new byte[compressedSize];
					stream.Read(source, 0, (int)compressedSize);
					PAK_explode(source, dest, compressedSize, uncompressedSize, info5);
					break;
				}

				case 4:
				{
					using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
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
	}

	public static int GetFileCount(string fileName)
	{
		using(var stream = new FileStream(fileName, FileMode.Open))
		using(var reader = new BinaryReader(stream))
		{
			stream.Position = 4;
			int offset = reader.ReadInt32();
			int fileCount = (offset - 4) / 4;
			stream.Position = offset - 4;
			if(reader.ReadInt32() == 0) fileCount--; //TIMEGATE
			return fileCount;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class PakArchive : IDisposable, IEnumerable<PakArchiveEntry>
{
	[DllImport("UnPAK", CallingConvention = CallingConvention.Cdecl)]
	static extern void PAK_explode(byte[] srcBuffer, byte[] dstBuffer, uint compressedSize, uint uncompressedSize, ushort flags);

	readonly FileStream stream;
	readonly BinaryReader reader;
	int[] offsets;
	bool disposed;

	public PakArchive(string filename)
	{
		stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
		reader = new BinaryReader(stream);
		ReadEntries();
	}

	void ReadEntries()
	{
		stream.Seek(4, SeekOrigin.Begin);
		int offset = reader.ReadInt32();
		int count = offset / 4 - 1;

		offsets = new int[count];
		offsets[0] = offset;

		for (int i = 1; i < count; i++)
		{
			offset = reader.ReadInt32();
			if (offset == 0) //TIMEGATE
			{
				Array.Resize(ref offsets, i);
				break;
			}

			offsets[i] = offset;
		}
	}

	internal byte[] GetData(PakArchiveEntry entry)
	{
		stream.Seek(offsets[entry.Index] + entry.Offset + 16, SeekOrigin.Begin);

		var dest = new byte[entry.UncompressedSize];
		switch (entry.CompressionType)
		{
			case 0: //uncompressed
				{
					stream.Read(dest, 0, entry.CompressedSize);
					break;
				}

			case 1: //pak explode
				{
					var source = new byte[entry.CompressedSize];
					stream.Read(source, 0, entry.CompressedSize);
					PAK_explode(source, dest, (uint)entry.CompressedSize, (uint)entry.UncompressedSize, entry.CompressionFlags);
					break;
				}

			case 4: //deflate
				{
					using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress, true))
					{
						deflateStream.Read(dest, 0, entry.UncompressedSize);
					}
					break;
				}

			default:
				throw new NotSupportedException();
		}

		return dest;
	}

	public int Count
	{
		get
		{
			return offsets.Length;
		}
	}

	public PakArchiveEntry this[int index]
	{
		get
		{
			return GetEntry(index);
		}
	}

	public IEnumerator<PakArchiveEntry> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			yield return GetEntry(i);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}

	PakArchiveEntry GetEntry(int index)
	{
		var entry = new PakArchiveEntry() { Archive = this, Index = index };

		stream.Seek(offsets[entry.Index], SeekOrigin.Begin);

		int skip = reader.ReadInt32();
		if (skip != 0)
		{
			reader.ReadBytes(skip - 4);
			skip -= 4;
		}

		entry.CompressedSize = reader.ReadInt32();
		entry.UncompressedSize = reader.ReadInt32();
		entry.CompressionType = reader.ReadByte();
		entry.CompressionFlags = reader.ReadByte();
		entry.Offset = reader.ReadUInt16() + skip;

		return entry;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			disposed = true;
			if (disposing)
			{
				reader.Close();
				stream.Close();
			}
		}
	}
}
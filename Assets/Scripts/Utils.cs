using System;

public static class Utils
{
	public static uint ReadUnsignedInt(byte[] data, int offset)
	{
		unchecked
		{
			return (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
		}
	}

	public static int ReadInt(byte[] data, int offset)
	{
		unchecked
		{
			return (int)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
		}
	}

	public static short ReadShort(byte[] data, int offset)
	{
		unchecked
		{
			return (short)(data[offset] | data[offset + 1] << 8);
		}
	}

	public static ushort ReadUnsignedShort(byte[] data, int offset)
	{
		unchecked
		{
			return (ushort)(data[offset] | data[offset + 1] << 8);
		}
	}

	public static void Write(byte[] data, short value, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
		}
	}

	public static void Write(byte[] data, ushort value, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
		}
	}

	public static void Write(byte[] data, uint value,  int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
			data[offset + 2] = (byte)(value >> 16);
			data[offset + 3] = (byte)(value >> 24);
		}
	}
}
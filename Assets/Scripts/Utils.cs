using System;
using UnityEngine;

public static class Utils
{
	#region Read

	public static uint ReadUnsignedInt(this byte[] data, int offset)
	{
		unchecked
		{
			return (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
		}
	}

	public static int ReadInt(this byte[] data, int offset)
	{
		unchecked
		{
			return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
		}
	}

	public static short ReadShort(this byte[] data, int offset)
	{
		unchecked
		{
			return (short)(data[offset] | data[offset + 1] << 8);
		}
	}

	public static Vector3Int ReadVector(this byte[] data, int offset)
	{
		unchecked
		{
			Vector3Int value = new Vector3Int
			{
				x = ReadShort(data, offset + 0),
				y = ReadShort(data, offset + 2),
				z = ReadShort(data, offset + 4)
			};
			return value;
		}
	}

	public static ushort ReadUnsignedShort(this byte[] data, int offset)
	{
		unchecked
		{
			return (ushort)(data[offset] | data[offset + 1] << 8);
		}
	}

	public static int ReadFarPointer(this byte[] data, int offset)
	{
		unchecked
		{
			return ReadUnsignedShort(data, offset) + ReadUnsignedShort(data, offset + 2) * 16;
		}
	}

	public static void ReadBoundingBox(this byte[] data, int offset, out Vector3Int lower, out Vector3Int upper)
	{
		lower.x = data.ReadShort(offset + 0);
		upper.x = data.ReadShort(offset + 2);
		lower.y = data.ReadShort(offset + 4);
		upper.y = data.ReadShort(offset + 6);
		lower.z = data.ReadShort(offset + 8);
		upper.z = data.ReadShort(offset + 10);
	}

	public static void ReadBoundingBox(this byte[] data, int offset, out Vector2Int lower, out Vector2Int upper)
	{
		lower.x = data.ReadShort(offset + 0);
		lower.y = data.ReadShort(offset + 4);
		upper.x = data.ReadShort(offset + 2);
		upper.y = data.ReadShort(offset + 6);
	}

	#endregion

	#region Write

	public static void Write(this byte[] data, Vector3Int value, int offset)
	{
		data.Write((short)value.x, offset + 0);
		data.Write((short)value.y, offset + 2);
		data.Write((short)value.z, offset + 4);
	}

	public static void Write(this byte[] data, Vector3Int lower, Vector3Int upper, int offset)
	{
		data.Write((short)lower.x, offset + 0);
		data.Write((short)upper.x, offset + 2);
		data.Write((short)lower.y, offset + 4);
		data.Write((short)upper.y, offset + 6);
		data.Write((short)lower.z, offset + 8);
		data.Write((short)upper.z, offset + 10);
	}

	public static void Write(this byte[] data, short value, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
		}
	}

	public static void Write(this byte[] data, ushort value, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
		}
	}

	public static void Write(this byte[] data, uint value,  int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
			data[offset + 2] = (byte)(value >> 16);
			data[offset + 3] = (byte)(value >> 24);
		}
	}

	#endregion

	#region Search

	public static int IndexOf(byte[] buffer, byte[] pattern, int offset = 0, int stride = 1)
	{
		for (int index = offset; index < buffer.Length - pattern.Length + 1; index += stride)
		{
			if (buffer[index] == pattern[0] && IsMatch(buffer, pattern, index))
			{
				return index - offset;
			}
		}

		return -1;
	}

	static bool IsMatch(byte[] buffer, byte[] pattern, int index)
	{
		for (int i = 0; i < pattern.Length; i++)
		{
			if (buffer[i + index] != pattern[i])
			{
				return false;
			}
		}

		return true;
	}

	#endregion
}
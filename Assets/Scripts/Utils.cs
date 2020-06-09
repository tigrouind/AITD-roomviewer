using System;
using UnityEngine;

public static class Utils
{
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
			return (int)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
		}
	}

	public static short ReadShort(this byte[] data, int offset)
	{
		unchecked
		{
			return (short)(data[offset] | data[offset + 1] << 8);
		}
	}

	public static byte ReadByte(this byte[] data, int offset)
	{
		return data[offset];
	}

	public static Vector3 ReadVector(this byte[] data, int offset)
	{
		unchecked
		{
			Vector3 value = new Vector3();
			value.x = ReadShort(data, offset + 0);
			value.y = ReadShort(data, offset + 2);
			value.z = ReadShort(data, offset + 4);
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

	public static void ReadBoundingBox(this byte[] data, int offset, out Vector3 lower, out Vector3 upper)
	{
		lower.x = data.ReadShort(offset + 0);
		upper.x = data.ReadShort(offset + 2);
		lower.y = data.ReadShort(offset + 4);
		upper.y = data.ReadShort(offset + 6);
		lower.z = data.ReadShort(offset + 8);
		upper.z = data.ReadShort(offset + 10);
	}

	public static void ReadBoundingBox(this byte[] data, int offset, out Vector2 lower, out Vector2 upper)
	{
		lower.x = data.ReadShort(offset + 0);
		lower.y = data.ReadShort(offset + 4);
		upper.x = data.ReadShort(offset + 2);
		upper.y = data.ReadShort(offset + 6);
	}

	public static void Write(this byte[] data, Vector3 value, int offset)
	{
		data.Write((short)value.x, offset + 0);
		data.Write((short)value.y, offset + 2);
		data.Write((short)value.z, offset + 4);
	}

	public static void Write(this byte[] data, Vector3 lower, Vector3 upper, int offset)
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

	public static int IndexOf(byte[] buffer, byte[] pattern)
	{
		for (int index = 0; index < buffer.Length - pattern.Length + 1; index++)
		{
			if (IsMatch(buffer, pattern, index))
			{
				return index;
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
}
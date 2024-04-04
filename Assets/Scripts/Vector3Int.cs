using UnityEngine;

[System.Serializable]
public struct Vector3Int
{
	public int x;
	public int y;
	public int z;
	public const float kEpsilon = 0.00001f;

	public Vector3Int(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public Vector3Int(Vector3 vector)
	{
		x = (int)vector.x;
		y = (int)vector.y;
		z = (int)vector.z;
	}

	public static bool operator ==(Vector3Int a, Vector3Int b)
	{
		float diff_x = a.x - b.x;
		float diff_y = a.y - b.y;
		float diff_z = a.z - b.z;
		float sqr = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;
		return sqr < kEpsilon * kEpsilon;
	}

	public static bool operator !=(Vector3Int a, Vector3Int b)
	{
		return !(a == b);
	}

	public override int GetHashCode()
	{
		return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
	}

	public override bool Equals(object other)
	{
		if (!(other is Vector3Int)) return false;
		return Equals((Vector3Int)other);
	}

	public bool Equals(Vector3 other)
	{
		return x == other.x && y == other.y && z == other.z;
	}

	public static Vector3Int operator+(Vector3Int a, Vector3Int b)
	{
		return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
	}

	public static Vector3Int operator-(Vector3Int a, Vector3Int b)
	{
		return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
	}

	public static Vector3Int operator*(Vector3Int a, int b)
	{
		return new Vector3Int(a.x * b, a.y * b, a.z * b);
	}

	public static Vector3Int operator/(Vector3Int a, int b)
	{
		return new Vector3Int(a.x / b, a.y / b, a.z / b);
	}

	public static Vector3Int FloorToInt(Vector3 v)
	{
		return new Vector3Int(
			Mathf.FloorToInt(v.x),
			Mathf.FloorToInt(v.y),
			Mathf.FloorToInt(v.z)
		);
	}

	public override string ToString()
	{
		return string.Format("({0}, {1}, {2})", x, y, z);
	}

	public static implicit operator Vector3(Vector3Int v)
	{
		return new Vector3(v.x, v.y, v.z);
	}

	public static Vector3Int Zero { get { return s_Zero; } }

	private static readonly Vector3Int s_Zero = new Vector3Int(0, 0, 0);
}
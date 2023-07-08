using UnityEngine;

[System.Serializable]
public struct Vector3Int
{
	public int x;
	public int y;
	public int z;

	public Vector3Int(int x, int y, int z)
	{
		this.x = x;
		this.y = y;
		this.z = z;
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
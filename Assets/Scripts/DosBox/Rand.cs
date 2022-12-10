public class Rand
{
	public uint Seed;

	public int Next()
	{
		unchecked
		{
			Seed = Seed * 22695477 + 1;
		}

		return (int)((Seed >> 16) & 0x7FFF);
	}

	public int Next(int maxValue)
	{
		return Next() % maxValue;
	}
}
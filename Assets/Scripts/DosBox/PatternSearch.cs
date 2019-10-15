public class PatternSearch
{
	byte[] x;
	byte[] y;
	bool wildcard;

	public PatternSearch(byte[] x, byte[] y, bool wildcard)
	{
		this.x = x;
		this.y = y;
		this.wildcard = wildcard;
	}
	
	public int IndexOf(int count)
	{
		for (int i = 0; i < count - y.Length + 1; i++)
		{
			if (IsMatch(i))
			{
				return i;
			}
		}

		return -1;
	}

	bool IsMatch(int index)
	{
		for (int i = 0; i < y.Length; i++)
		{
			byte val = y[i];
			if(wildcard && val == 0xFF)
			{
				continue;
			}

			if (x[i + index] != val)
			{
				return false;
			}
		}

		return true;
	}
}
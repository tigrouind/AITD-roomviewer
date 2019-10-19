public class PatternSearch
{
	byte[] buffer;
	byte[] pattern;
	bool wildcard;

	public PatternSearch(byte[] buffer, byte[] pattern, bool wildcard)
	{
		this.buffer = buffer;
		this.pattern = pattern;
		this.wildcard = wildcard;
	}
	
	public int IndexOf(int count)
	{
		for (int i = 0; i < count - pattern.Length + 1 ; i++)
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
		for (int i = 0; i < pattern.Length; i++)
		{
			byte val = pattern[i];
			if(wildcard && val == 0xFF)
			{
				continue;
			}

			if (buffer[i + index] != val)
			{
				return false;
			}
		}

		return true;
	}
}
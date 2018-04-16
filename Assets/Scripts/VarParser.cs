using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


public class VarParser
{
	readonly Dictionary<string, Dictionary<int, string>> sections
		= new Dictionary<string, Dictionary<int, string>>();

	public string GetText(string sectionName, int value)
	{
		Dictionary<int, string> section;
		if (sections.TryGetValue(sectionName, out section))
		{
			string text;
			if(section.TryGetValue(value, out text))
			{
				return text;
			}
		}

		return string.Empty;
	}

	public void Parse(string filePath)
	{
		var allLines = System.IO.File.ReadAllLines(filePath);
		Regex section = new Regex("^[A-Z](.*)$");
		Regex item = new Regex("^(?<linenumber>[0-9]+)(-(?<next>[0-9]+))?(?<text>.*)");
		Dictionary<int, string> currentSection = null;

		foreach (string line in allLines)
		{
			//check if new section
			Match sectionMatch = section.Match(line);
			if (sectionMatch.Success)
			{
				currentSection = new Dictionary<int, string>();
				sections.Add(sectionMatch.Value.Trim(), currentSection);
			}
			else if (currentSection != null)
			{
				//parse line if inside section
				Match itemMatch = item.Match(line);
				if (itemMatch.Success)
				{
					int lineNumber = int.Parse(itemMatch.Groups["linenumber"].Value);
					string nextNumberString = itemMatch.Groups["next"].Value.Trim();
					int nextNumber;

					if (!string.IsNullOrEmpty(nextNumberString))
					{
						nextNumber = int.Parse(nextNumberString);
					}
					else
					{
						nextNumber = lineNumber;
					}

					string text = itemMatch.Groups["text"].Value.Trim();
					if (!string.IsNullOrEmpty(text))
					{
						for(int i = lineNumber; i <= nextNumber ; i++)
						{
							currentSection[i] = text;
						}
					}
				}
			}
		}
	}
}

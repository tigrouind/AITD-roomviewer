using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class VarParser
{
	[SerializeField]
	VarSectionDictionary sections = new VarSectionDictionary();

	public string GetText(VarEnum section, int value)
	{
		VarEntryDictionary entryDict;
		if (sections.TryGetValue(section, out entryDict))
		{
			string text;
			if (entryDict.TryGetValue(value, out text))
			{
				return text;
			}
		}

		return string.Empty;
	}

	public void Load(string filePath, params VarEnum[] sectionsToParse)
	{
		var allowedSections = sectionsToParse.ToDictionary(x => x.ToString(), x => x);
		var allLines = ReadLines(filePath);

		Dictionary<int, string> currentSection = null;
		Regex regex = new Regex(@"^(?<from>[0-9]+)(-(?<to>[0-9]+))?\s+(?<text>.*)$");

		foreach (string line in allLines)
		{
			//check if new section
			if (line.Length > 0 && line[0] >= 'A' && line[0] <= 'Z')
			{
				VarEnum section;
				if (allowedSections.TryGetValue(line, out section))
				{
					currentSection = CreateNewSection(section);
				}
				else
				{
					currentSection = null;
				}
			}
			else if (currentSection != null)
			{
				//parse line if inside section
				Match match = regex.Match(line);
				if (match.Success)
				{
					string from = match.Groups["from"].Value;
					string to = match.Groups["to"].Value;
					string text = match.Groups["text"].Value.Trim();

					AddEntry(currentSection, from, to, text);
				}
			}
		}
	}

	Dictionary<int, string> CreateNewSection(VarEnum section)
	{
		VarEntryDictionary entryDict = new VarEntryDictionary();
		sections.Add(section, entryDict);
		return entryDict;
	}

	void AddEntry(Dictionary<int, string> section, string fromString, string toString, string text)
	{
		if (!(string.IsNullOrEmpty(text) || text.Trim() == string.Empty))
		{
			int from = int.Parse(fromString);
			int to = string.IsNullOrEmpty(toString) ? from : int.Parse(toString);

			for (int i = from; i <= to ; i++)
			{
				section[i] = text;
			}
		}
	}

	IEnumerable<string> ReadLines(string filePath)
	{
		using (StreamReader reader = new StreamReader(filePath))
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				yield return line;
			}
		}
	}
}
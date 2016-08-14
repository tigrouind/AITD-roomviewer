using System;
using UnityEngine;

public class Vars : MonoBehaviour
{
	public MenuStyle Style;

	private bool pauseVarsTracking;

	private byte[] memory = new byte[512];
	private Var[] vars = new Var[207];
	private Var[] cvars = new Var[44];

	private byte[] varsMemoryPattern = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x00 };
	private long varsMemoryAddress = -1;

	private byte[] cvarsMemoryPattern = new byte[] { 0x31, 0x00, 0x0E, 0x01, 0xBC, 0x02, 0x12, 0x00, 0x06, 0x00, 0x13, 0x00, 0x14, 0x00, 0x01 };
	private long cvarsMemoryAddress = -1;

	private short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	void Update()
	{
		ProcessMemoryReader processReader = GetComponent<DosBox>().ProcessReader;
		if (processReader != null)
		{
			if (!pauseVarsTracking)
			{
				if (varsMemoryAddress != -1)
				{
					processReader.Read(memory, varsMemoryAddress, 207 * 2);
					CheckDifferences(memory, vars);
				}

				if (cvarsMemoryAddress != -1)
				{
					processReader.Read(memory, cvarsMemoryAddress, 44 * 2);
					CheckDifferences(memory, cvars);
				}
			}
		}

		//freeze vars tracking
		if (Input.GetMouseButtonDown(0))
		{
			pauseVarsTracking = !pauseVarsTracking;
		}

		//hide table
		if (Input.GetMouseButtonDown(1))
		{
			pauseVarsTracking = false;
			GetComponent<Vars>().enabled = false;
		}
	}

	void OnGUI()
	{
		GUIStyle panel = new GUIStyle(Style.Panel);
		panel.normal.background = Style.BlackTexture;
		Rect areaA = new Rect(0, 0, Screen.width, Screen.height * 22.0f / 28.0f);
		Rect areaB = new Rect(0, Screen.height * 22.0f / 28.0f, Screen.width, Screen.height * 6.0f / 28.0f);

		GUILayout.BeginArea(areaA, panel);
		DisplayTable(areaA, 10, 21, vars, "VARS");
		GUILayout.EndArea();

		GUILayout.BeginArea(areaB, panel);
		DisplayTable(areaB, 10, 5, cvars, "CVARS");
		GUILayout.EndArea();
	}

	void CheckDifferences(byte[] memory, Var[] data)
	{
		float currenttime = Time.time;
		for (int i = 0; i < data.Length; i++)
		{
			int value = ReadShort(memory[i * 2 + 0], memory[i * 2 + 1]);
			int oldValue = data[i].value;
			data[i].value = value;

			if (value != oldValue)
			{
				data[i].time = currenttime;
			}

			data[i].difference = (currenttime - data[i].time) < 5.0f;
			data[i].oldValue = value;
		}
	}

	void DisplayTable(Rect area, int columns, int rows, Var[] vars, string title)
	{
		//setup style
		GUIStyle labelStyle = new GUIStyle(Style.Label);
		labelStyle.fixedWidth = area.width / (columns + 1);
		labelStyle.fixedHeight = area.height / ((float)(rows + 1));
		labelStyle.alignment = TextAnchor.MiddleCenter;

		GUIStyle headerStyle = new GUIStyle(labelStyle);
		headerStyle.normal.textColor = Color.black;
		headerStyle.normal.background = pauseVarsTracking ? Style.RedTexture : Style.GreenTexture;

		//header
		GUILayout.BeginHorizontal();
		GUILayout.Label(title, headerStyle);
		for (int i = 0; i < columns; i++)
		{
			GUILayout.Label(i.ToString(), headerStyle);
		}
		GUILayout.EndHorizontal();

		//body
		int count = 0;
		for (int i = 0; i < rows; i++)
		{
			GUILayout.BeginHorizontal();
			headerStyle.alignment = TextAnchor.MiddleRight;
			GUILayout.Label(i.ToString(), headerStyle);

			for (int j = 0; j < columns; j++)
			{
				string stringValue = string.Empty;
				if (count < vars.Length)
				{
					int value = vars[count].value;
					bool different = vars[count].difference;

					if (value != 0 || different)
						stringValue = value.ToString();

					//highlight recently changed vars
					labelStyle.normal.background = different ? Style.RedTexture : null;
				}

				count++;
				GUILayout.Label(stringValue, labelStyle);
			}
			GUILayout.EndHorizontal();
		}
	}

	public void SearchForPatterns(ProcessMemoryReader reader)
	{
		varsMemoryAddress = reader.SearchForBytePattern(varsMemoryPattern);
		cvarsMemoryAddress = reader.SearchForBytePattern(cvarsMemoryPattern);
	}

	public struct Var
	{
		public int value;
		public int oldValue;
		public int state;
		public float time;
		public bool difference;
	}
}
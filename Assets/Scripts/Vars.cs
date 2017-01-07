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

	private bool compare;
	private bool oldcompare;
	private bool ignoreDifferences;

	private string focusedControlName;

	public void OnEnable()
	{
		ignoreDifferences = !compare;
	}

	private short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	private void WriteShort(int value, byte[] data, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
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
					ProcessInput(vars);
					processReader.Read(memory, varsMemoryAddress, 207 * 2);
					CheckDifferences(memory, vars, varsMemoryAddress);
				}

				if (cvarsMemoryAddress != -1)
				{
					ProcessInput(cvars);
					processReader.Read(memory, cvarsMemoryAddress, 44 * 2);
					CheckDifferences(memory, cvars, cvarsMemoryAddress);
				}

				ignoreDifferences = false;
			}
		}

		//hide table
		if (Input.GetMouseButtonDown(1))
		{
			this.enabled = false;
		}
	}

	void OnGUI()
	{
		GUIStyle panel = new GUIStyle(Style.Panel);
		panel.normal.background = Style.BlackTexture;
		Rect screen = new Rect(0, 0, Screen.width, Screen.height - 30 * 1);
		Rect areaA = new Rect(0, 0, screen.width, screen.height * 22.0f / 28.0f);
		Rect areaB = new Rect(0, screen.height * 22.0f / 28.0f, screen.width, screen.height * 6.0f / 28.0f);
		Rect areaC = new Rect(0, screen.height, screen.width, 30 * 1);

		//table
		GUILayout.BeginArea(areaA, panel);
		DisplayTable(areaA, 10, 21, vars, "VARS");
		GUILayout.EndArea();

		GUILayout.BeginArea(areaB, panel);
		DisplayTable(areaB, 10, 5, cvars, "CVARS");
		GUILayout.EndArea();

		//buttons
		GUIStyle button = new GUIStyle(Style.Button);
		GUIStyle buttonToggled = new GUIStyle(button);
		buttonToggled.normal = buttonToggled.active; 
		buttonToggled.hover = buttonToggled.active;

		button.fixedWidth = areaC.width / 3.0f;		
		GUILayout.BeginArea(areaC, panel);
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Freeze", pauseVarsTracking ? buttonToggled : button) && Event.current.button == 0)
		{
			pauseVarsTracking = !pauseVarsTracking;
		}

		if (GUILayout.Button("Save state", button))
		{
			SaveState(vars);
			SaveState(cvars);
		}

		if (GUILayout.Button("Compare", compare ? buttonToggled : button) && Event.current.button == 0)
		{
			compare = !compare;
		}

		if (!compare && oldcompare)
		{
			ignoreDifferences = true;
		}
		oldcompare = compare;
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}

	void SaveState(Var[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			data[i].saveState = data[i].value;
		}
	}

	void CheckDifferences(byte[] memory, Var[] data, long offset)
	{
		float currenttime = Time.time;
		for (int i = 0; i < data.Length; i++)
		{
			int oldValue = data[i].value;
			int value;

			if (compare)
			{
				value = data[i].saveState;
			}
			else
			{
				value = ReadShort(memory[i * 2 + 0], memory[i * 2 + 1]);
			}

			data[i].value = value;
					
			if (ignoreDifferences)
			{
				data[i].time = float.MinValue;
			}
			else if (value != oldValue)
			{
				if (compare)
				{
					data[i].time = float.MaxValue;
				}
				else
				{
					data[i].time = currenttime;
				}
			}

			data[i].offset = offset + i * 2;
			data[i].difference = (currenttime - data[i].time) < 3.0f;
		}
	}

	void ProcessInput(Var[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{			
			if (data[i].text != null && data[i].lostFocus)
			{
				//if a certain amount of time elapsed, write value to memory
				int newValueInt;
				if (int.TryParse(data[i].text, out newValueInt) || data[i].text == string.Empty)
				{
					if (newValueInt > short.MaxValue) newValueInt = short.MaxValue;
					if (newValueInt < short.MinValue) newValueInt = short.MinValue;

					//write new value to memory
					ProcessMemoryReader processReader = GetComponent<DosBox>().ProcessReader;
					byte[] wordValue = new byte[2];
					WriteShort(newValueInt, wordValue, 0);
					processReader.Write(wordValue, data[i].offset, wordValue.Length);
				}

				data[i].text = null;
				data[i].lostFocus = false;
			}
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
		headerStyle.normal.background = Style.GreenTexture;

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
		for (int j = 0; j < rows; j++)
		{
			GUILayout.BeginHorizontal();
			headerStyle.alignment = TextAnchor.MiddleRight;
			GUILayout.Label(j.ToString(), headerStyle);

			for (int i = 0; i < columns; i++)
			{
				labelStyle.normal.background = null;

				string stringValue = string.Empty;
				if (count < vars.Length)
				{
					Var var = vars[count];
					int value = var.value;
					bool different = var.difference;

					if(var.text != null)
						stringValue = var.text;
					else if (value != 0 || different)
						stringValue = value.ToString();

					//highlight recently changed vars
					if (different)
					{
						labelStyle.normal.background = Style.RedTexture;
					}

					if(!(pauseVarsTracking || compare))
					{
						string controlName = var.offset.ToString();
						GUI.SetNextControlName(controlName);
						string newValue = GUILayout.TextField(stringValue, labelStyle);

						//textbox value has changed
						if(newValue != stringValue)
						{
							vars[count].text = newValue;
						}
					}
					else
					{
						GUILayout.Label(stringValue, labelStyle);
					}
				}
				else
				{
					GUILayout.Label(stringValue, labelStyle);
				}

				count++;
			}
			GUILayout.EndHorizontal();
		}

		//check if a control has lost focus 
		string newFocusedControlName = GUI.GetNameOfFocusedControl();
		if(focusedControlName != newFocusedControlName)
		{
			for (int i = 0; i < vars.Length; i++)
			{
				Var var = vars[i];
				string controlName = var.offset.ToString();

				//control lost focus
				if(controlName == focusedControlName)
				{
					vars[i].lostFocus = true;
				}
			}

			focusedControlName = newFocusedControlName;
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
		public int saveState;
		public float time;
		public bool difference;
		public long offset;
		public string text;
		public bool lostFocus;
	}
}
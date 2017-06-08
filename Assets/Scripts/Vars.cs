using System;
using UnityEngine;
using UnityEngine.UI;

public class Vars : MonoBehaviour
{
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

	public RectTransform Panel;
	public RectTransform TabA;
	public RectTransform TabB;
	public RectTransform TableHeaderPrefab;
	public InputField TableCellPrefab;

	public void OnEnable()
	{
		ignoreDifferences = !compare;
		BuildTables();
		UpdateCellSize();
		Panel.gameObject.SetActive(true);
	}

	public void OnDisable()
	{
		Panel.gameObject.SetActive(false);
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
					processReader.Read(memory, varsMemoryAddress, 207 * 2);
					CheckDifferences(memory, vars, varsMemoryAddress);
				}

				if (cvarsMemoryAddress != -1)
				{
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

		UpdateCellSize();
	}

	void UpdateCellSize()
	{
		//set cell size
		Vector2 cellSize = new Vector2(Screen.width / 21.0f, (Screen.height - 30.0f) / 16.0f);
		TabA.GetComponent<GridLayoutGroup>().cellSize = cellSize;
		TabB.GetComponent<GridLayoutGroup>().cellSize = cellSize;	
	}

	void BuildTables()
	{
		BuildTable(TabA, 207, vars);
		BuildTable(TabB, 44, cvars);
	}

	void BuildTable(RectTransform tab, int numberOfCells, Var[] data)
	{
		if (tab.childCount == 0)
		{
			//empty
			GameObject empty = new GameObject(string.Empty, typeof(RectTransform));
			empty.transform.SetParent(tab.transform);

			for (int i = 0; i < 20; i++)
			{
				RectTransform header = Instantiate(TableHeaderPrefab);
				header.transform.SetParent(tab.transform);
				header.GetComponentInChildren<Text>().text = i.ToString();
			}

			for (int i = 0; i < numberOfCells; i++)
			{
				if (i % 20 == 0)
				{
					RectTransform header = Instantiate(TableHeaderPrefab);
					header.transform.SetParent(tab.transform);
					header.GetComponentInChildren<Text>().text = i.ToString();
				}

				InputField cell = Instantiate(TableCellPrefab);
				cell.transform.SetParent(tab.transform);
				int cellIndex = i;
				cell.onEndEdit.AddListener((value) => OnCellChange(cell, data, cellIndex));
				data[i].inputField = cell;
			}
		}
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
				
			data[i].memoryAddress = offset + i * 2;

			//Check differences
			bool difference = (currenttime - data[i].time) < 3.0f;

			InputField inputField = data[i].inputField;

			string newText = string.Empty;
			if (value != 0 || difference)
			{
				newText = value.ToString();
			}

			if (inputField.text != newText && !inputField.isFocused)
			{
				inputField.text = newText;
			}

			Image image = inputField.GetComponent<Image>();
			Text text = inputField.GetComponentInChildren<Text>();
			if (difference)
			{
				image.color = Color.red;
				text.color = Color.white;
			}
			else
			{
				image.color = Color.white;
				text.color = Color.black;
			}
		}
	}

	void OnCellChange(InputField cell, Var[] data, int cellIndex)
	{
		int newValueInt;
		if (int.TryParse(cell.text, out newValueInt) || cell.text == string.Empty)
		{
			if (newValueInt > short.MaxValue) newValueInt = short.MaxValue;
			if (newValueInt < short.MinValue) newValueInt = short.MinValue;

			if (newValueInt != data[cellIndex].value)
			{
				//write new value to memory
				ProcessMemoryReader processReader = GetComponent<DosBox>().ProcessReader;
				byte[] wordValue = new byte[2];
				WriteShort(newValueInt, wordValue, 0);
				processReader.Write(wordValue, data[cellIndex].memoryAddress, wordValue.Length);
			}
		}
	}

	public void FreezeClick()
	{
		pauseVarsTracking = !pauseVarsTracking;
	}

	public void SaveStateClick()
	{
		SaveState(vars);
		SaveState(cvars);
	}

	public void CompareClick()
	{
		compare = !compare;

		if (!compare && oldcompare)
		{
			ignoreDifferences = true;
		}
		oldcompare = compare;
	}


	public void SearchForPatterns(ProcessMemoryReader reader)
	{
		varsMemoryAddress = reader.SearchForBytePattern(varsMemoryPattern);
		cvarsMemoryAddress = reader.SearchForBytePattern(cvarsMemoryPattern);
	}

	public struct Var
	{
		public int value;
		public int saveState; //value set there when using SaveState button
		public float time;  //time since last difference
		public long memoryAddress;
		public InputField inputField;
	}
}
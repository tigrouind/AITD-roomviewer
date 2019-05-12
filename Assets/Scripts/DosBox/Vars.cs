using System;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Vars : MonoBehaviour
{
	private bool pauseVarsTracking;

	private byte[] memory = new byte[512];
	private Var[] vars = new Var[207];
	private Var[] cvars = new Var[44];
	private VarParser varParser = new VarParser();

	private ProcessMemoryReader processReader;
	private long varsMemoryAddress;
	private long cvarsMemoryAddress;

	private bool compare;
	private bool ignoreDifferences = true;
	private int screenWidth = -1;
	private int screenHeight = -1; 

	public RectTransform Panel;
	public RectTransform TabA;
	public RectTransform TabB;
	public RectTransform TableHeaderPrefab;
	public InputField TableCellPrefab;
	public RectTransform ToolTip;

	public void Start()
	{
		//parse vars.txt file
		string varPath = @"GAMEDATA\vars.txt";
		if (File.Exists(varPath))
		{
			varParser.Parse(varPath, "VARS", "C_VARS");
		}

		InitVars(vars);
		InitVars(cvars);
		BuildTables();

		processReader = new ProcessMemoryReader(Shared.ProcessId);
		varsMemoryAddress = Shared.VarsMemoryAddress;
		cvarsMemoryAddress = Shared.CvarsMemoryAddress;
	}

	void OnDestroy()
	{
		if (processReader != null)
		{
			processReader.Close();
		}
	}

	void InitVars(Var[] data)
	{
		for(int i = 0 ; i < data.Length ; i++)
		{
			data[i] = new Var();
		}
	}

	bool RefreshVARS()
	{
		if (varsMemoryAddress != -1)
		{
			if (processReader.Read(memory, varsMemoryAddress, 207 * 2) <= 0)
			{
				return false;
			}

			CheckDifferences(memory, vars, varsMemoryAddress);
		}

		if (cvarsMemoryAddress != -1) 
		{
			if (processReader.Read(memory, cvarsMemoryAddress, 44 * 2) <= 0)
			{
				return false;
			}
			
			CheckDifferences(memory, cvars, cvarsMemoryAddress);
		}
		
		return true;
	}

	void Update()
	{
		if (processReader != null && !pauseVarsTracking)
		{
			if (!RefreshVARS())
			{
				Shared.ProcessId = -1;
				processReader.Close();
				processReader = null;
			}
		}

		ignoreDifferences = false;

		//quit
		if (Input.GetMouseButtonDown(1))
		{
			SceneManager.LoadScene("room");
		}

		UpdateCellSize();
	}

	void UpdateCellSize()
	{
		if (screenWidth != Screen.width || screenHeight != Screen.height)
		{
			screenHeight = Screen.height;
			screenWidth = Screen.width;

			//set cell size
			Vector2 cellSize = new Vector2(screenWidth / 21.0f, (screenHeight - 30.0f) / 16.0f);
			TabA.GetComponent<GridLayoutGroup>().cellSize = cellSize;
			TabB.GetComponent<GridLayoutGroup>().cellSize = cellSize;
		}		
	}

	void BuildTables()
	{
		BuildTable(TabA, "VARS", vars);
		BuildTable(TabB, "C_VARS", cvars);
	}

	void BuildTable(RectTransform tab, string sectionName, Var[] data)
	{
		//empty
		GameObject empty = new GameObject(string.Empty, typeof(RectTransform));
		empty.AddComponent<Image>().color = TableHeaderPrefab.GetComponent<Image>().color;
		empty.transform.SetParent(tab.transform);

		for (int i = 0; i < 20; i++)
		{
			RectTransform header = Instantiate(TableHeaderPrefab);
			header.transform.SetParent(tab.transform);
			header.GetComponentInChildren<Text>().text = i.ToString();
		}

		for (int i = 0; i < data.Length; i++)
		{
			if (i % 20 == 0)
			{
				RectTransform header = Instantiate(TableHeaderPrefab);
				header.transform.SetParent(tab.transform);
				Text textComponent = header.GetComponentInChildren<Text>();
				textComponent.GetComponentInChildren<RectTransform>().offsetMax = new Vector2(-5, 0);
				textComponent.alignment = TextAnchor.MiddleRight;
				textComponent.text = i.ToString();
			}

			InputField cell = Instantiate(TableCellPrefab);
			cell.transform.SetParent(tab.transform);

			Var var = data[i];
			cell.onEndEdit.AddListener((value) => OnCellChange(cell, var));

			int cellIndex = i;
			UIPointerHandler pointerHandler = cell.GetComponent<UIPointerHandler>();
			pointerHandler.PointerEnter.AddListener((value) => OnCellPointerEnter(cell, sectionName, cellIndex));
			pointerHandler.PointerExit.AddListener((value) => OnCellPointerExit());
			var.inputField = cell;
		}
	}

	void SaveState(Var[] data)
	{
		foreach(Var var in data)
		{
			var.saveState = var.value;
		}
	}

	void CheckDifferences(byte[] memory, Var[] data, long offset)
	{
		float currenttime = Time.time;
		for (int i = 0; i < data.Length; i++)
		{
			Var var = data[i];
			int oldValue = var.value;
			int value;

			if (compare)
			{
				value = var.saveState;
			}
			else
			{
				value = Utils.ReadShort(memory, i * 2 + 0);
			}

			var.value = value;

			if (ignoreDifferences)
			{
				var.time = float.MinValue;
			}
			else if (value != oldValue)
			{
				if (compare)
				{
					var.time = float.MaxValue;
				}
				else
				{
					var.time = currenttime;
				}
			}

			var.memoryAddress = offset + i * 2;

			//Check differences
			bool difference = (currenttime - var.time) < 5.0f;

			InputField inputField = var.inputField;

			string newText = string.Empty;
			if (value != 0 || difference)
			{
				newText = value.ToString();
			}

			if (inputField.text != newText && !inputField.isFocused)
			{
				inputField.text = newText;
			}

			if (difference)
			{
				SetInputFieldColor(inputField, new Color32(240, 68, 77, 255));
			}
			else
			{
				SetInputFieldColor(inputField, new Color32(28, 28, 38, 255));
			}
		}
	}

	void OnCellPointerEnter(InputField cell, string sectionName, int cellIndex)
	{
		string text = "#" + cellIndex;
		string description = varParser.GetText(sectionName, cellIndex);
		if(!string.IsNullOrEmpty(description))
		{
			text += "\r\n" + description;
		}

		ToolTip.GetComponentInChildren<Text>().text = text;
		RectTransform cellTransform = cell.GetComponent<RectTransform>();
		RectTransform toolTipTransform = ToolTip.GetComponent<RectTransform>();
		Vector2 toolTipSize = toolTipTransform.sizeDelta;

		toolTipTransform.position = MoveToolTipIfNeeded(cellTransform.position
			- new Vector3(0.0f, cellTransform.sizeDelta.y / 2.0f, 0.0f),
			toolTipSize.x / 2.0f,
			Screen.width - toolTipSize.x / 2.0f);

		ToolTip.gameObject.SetActive(true);
	}

	Vector3 MoveToolTipIfNeeded(Vector3 position, float min, float max)
	{
		return new Vector3(Mathf.Clamp(position.x, min, max), position.y, position.z);
	}

	void OnCellPointerExit()
	{
		ToolTip.gameObject.SetActive(false);
	}

	void OnCellChange(InputField cell, Var var)
	{
		if (processReader != null)
		{
			int newValueInt;
			if (int.TryParse(cell.text, out newValueInt) || cell.text == string.Empty)
			{
				if (newValueInt > short.MaxValue) newValueInt = short.MaxValue;
				if (newValueInt < short.MinValue) newValueInt = short.MinValue;

				if (newValueInt != var.value)
				{
					//write new value to memory
					byte[] wordValue = new byte[2];
					Utils.Write((short)newValueInt, wordValue, 0);
					processReader.Write(wordValue, var.memoryAddress, wordValue.Length);
				}
			}
		}
	}

	public void FreezeClick(Button button)
	{
		pauseVarsTracking = !pauseVarsTracking;
		ToggleButtonState(button, pauseVarsTracking);
	}

	private void SetInputFieldColor(InputField inputField, Color32 color)
	{
		var colors = inputField.colors;
		colors.normalColor = color;
		inputField.colors = colors;
	}

	private void SetButtonColor(Button button, Color32 normalColor, Color32 highlightedColor)
	{
		var colors = button.colors;
		colors.normalColor = normalColor;
		colors.highlightedColor = highlightedColor;
		button.colors = colors;
	}

	public void ToggleButtonState(Button button, bool enabled)
	{
		Text text = button.GetComponentInChildren<Text>();
		text.color = enabled ? Color.red : (Color)new Color32(50, 50, 50, 255);
		SetButtonColor(button, enabled ? new Color32(255, 174, 174, 255) : new Color32(43, 193, 118, 255), enabled ? new Color32(255, 174, 174, 255) : new Color32(178, 255, 207, 255));
	}

	public void SaveStateClick()
	{
		SaveState(vars);
		SaveState(cvars);
	}

	public void CompareClick(Button button)
	{
		compare = !compare;

		if (!compare)
		{
			ignoreDifferences = true;
		}
		ToggleButtonState(button, compare);
	}

	public class Var
	{
		public int value;
		public int saveState; //value set there when using SaveState button
		public float time;	//time since last difference
		public long memoryAddress;
		public InputField inputField;
	}
}
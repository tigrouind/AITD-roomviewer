using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
	public string[] Items;
	public int selectedIndex = 0;

	public int Value
	{
		get
		{
			return selectedIndex;
		}
		set
		{
			if (selectedIndex != value)
			{
				selectedIndex = value;
				RefreshText();
			}
		}
	}

	public bool BoolValue
	{
		get
		{
			return Value == 1;
		}
		set
		{
			Value = value ? 1 : 0;
		}
	}


	// Use this for initialization
	void Start ()
	{
		RefreshText();
	}

	void RefreshText()
	{
		if (Items.Length > 0)
		{
			GetComponentInChildren<Text>().text = Items[Value];
		}
	}
}

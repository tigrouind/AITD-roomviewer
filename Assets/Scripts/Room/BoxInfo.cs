using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BoxInfo : MonoBehaviour
{
	public Text LeftText;
	public Text RightText;
	public int Count;

	readonly StringBuilder names = new StringBuilder();
	readonly StringBuilder values = new StringBuilder();

	public void Clear()
	{
		names.Length = 0;
		names.Capacity = 0;
		values.Length = 0;
		values.Capacity = 0;
		Count = 0;
		LeftText.text = string.Empty;
		RightText.text = string.Empty;
		gameObject.SetActive(false);
	}

	public void Append(string name, object value)
    {
		names.AppendLine(name);
		values.AppendLine(value.ToString());
		Count++;
    }

	public void Append()
    {
		Append(string.Empty, string.Empty);
    }

	public void AppendFormat(string name, string format, params object[] args)
    {
		Append(name, string.Format(format, args));
    }

    public void UpdateText()
    {
		var rect = GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(rect.sizeDelta.x, Count * 18.0f);

    	LeftText.text = names.ToString();	
		RightText.text = values.ToString();	

		gameObject.SetActive(Count > 0);
    }
}


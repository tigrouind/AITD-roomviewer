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

	readonly StringBuilder names = new StringBuilder();
	readonly StringBuilder values = new StringBuilder();

	public void Clear(bool hide = false)
	{
		if (names.Length > 0)
		{
			names.Length = 0;
			names.Capacity = 0;
			values.Length = 0;
			values.Capacity = 0;
		}	

		if (hide)
		{
			LeftText.text = string.Empty;
			RightText.text = string.Empty;
			gameObject.SetActive(false);
		}
	}

	public void Append()
	{
		AppendInternal(string.Empty, string.Empty);
	}

	public void Append(string name, string format, params object[] args)
	{
		AppendInternal(name, string.Format(format, args));
	}

	public void Append(string name, object value)
	{
		AppendInternal(name, value.ToString());
	}

	private void AppendInternal(string name, string value)
	{
		if (names.Length > 0) names.AppendLine();
		if (values.Length > 0) values.AppendLine();

		names.Append(name);
		values.Append(value);
	}

	public void UpdateText()
	{
		gameObject.SetActive(names.Length > 0);
		LeftText.text = names.ToString();	
		RightText.text = values.ToString();	
	}
}


﻿using System;
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
		names.Length = 0;
		values.Length = 0;

		if (hide)
		{
			LeftText.text = string.Empty;
			RightText.text = string.Empty;
			gameObject.SetActive(false);
		}
	}

	public void AppendLine()
	{
		AppendLineIfNecessary();
	}

	public void Append(string name, Vector3 value)
	{
		AppendLineIfNecessary();
		names.Append(name);
		values.Append(value.x);
		values.Append(' ');
		values.Append(value.y);
		values.Append(' ');
		values.Append(value.z);
	}

	public void Append<T>(string name, T value)
	{
		AppendLineIfNecessary();
		names.Append(name);
		values.Append(value.ToString());
	}

	public void Append(string name, string format, params object[] args)
	{
		AppendLineIfNecessary();
		names.Append(name);
		values.AppendFormat(format, args);
	}

	private void AppendLineIfNecessary()
	{
		if (names.Length > 0) names.AppendLine();
		if (values.Length > 0) values.AppendLine();
	}

	public void UpdateText()
	{
		gameObject.SetActive(names.Length > 0);
		LeftText.text = names.ToString();
		RightText.text = values.ToString();
	}
}


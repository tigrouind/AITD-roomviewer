using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ColorTheme : MonoBehaviour
{
	[HideInInspector]
	public Color ClearColor;

	[HideInInspector]
	public Color BoxColor;

	[HideInInspector]
	public Color MenuColor;

	public UnityEvent Load;

	public List<Theme> Themes;

	public void LoadTheme(Theme theme)
	{
		if (!theme.Color.Equals(default(Color)))
		{
			float h, s, v;
			Color.RGBToHSV(theme.Color, out h, out s, out v);

			ClearColor = theme.Color;
			BoxColor = Color.HSVToRGB(h, s, Math.Max(v - 0.1f, 0.0f));
			MenuColor = Color.HSVToRGB(h, s, Math.Max(v - 0.2f, 0.0f));

			Load.Invoke();
		}
	}

	public Theme Theme
	{
		get
		{
			var color = Color;
			var theme = Themes.FirstOrDefault(x => x.Color == color);
			if (theme != null)
			{
				return theme;
			}
			else if (!color.Equals(default(Color)))
			{
				return new Theme() { Color = color };
			}
			else if (Themes.Any())
			{
				return Themes[0];
			}

			return new Theme();
		}

		set
		{
			Color = value.Color;
		}
	}

	Color Color
	{
		get
		{
			Color color;
			return ColorUtility.TryParseHtmlString(string.Format("#{0}00", PlayerPrefs.GetString("theme")), out color)
				? color : default(Color);
		}

		set
		{
			PlayerPrefs.SetString("theme", ColorUtility.ToHtmlStringRGB(value).ToLowerInvariant());
		}
	}
}

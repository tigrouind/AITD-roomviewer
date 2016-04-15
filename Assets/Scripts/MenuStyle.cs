using System;
using UnityEngine;

public class MenuStyle : MonoBehaviour
{
	public Texture2D WhiteTexture;
	public Texture2D HoverTexture;
	public GUIStyle Button;
	public GUIStyle Label;
	public GUIStyle Option;
	public GUIStyle Toggle;

	void Start()
	{
		Button = new GUIStyle();
		Button.normal.textColor = new Color32(0, 200, 100, 255);
		Button.normal.background = WhiteTexture;
		Button.alignment = TextAnchor.MiddleCenter;
		Button.fontSize = 16;
		Button.fixedHeight = 30;
		Button.hover.textColor = new Color32(0, 250, 150, 255);
		Button.hover.background = HoverTexture;

		Label = new GUIStyle(Button);
		Label.normal.textColor = Color.white;
		Label.fixedWidth = 200;

		Option = new GUIStyle(Button);

		Option.fixedWidth = 200;

		Toggle = new GUIStyle(Option);
		Toggle.fontSize = 35;
	}
}
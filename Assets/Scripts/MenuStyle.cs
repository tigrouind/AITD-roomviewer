using System;
using UnityEngine;

public class MenuStyle : MonoBehaviour
{
	public Texture2D PanelTexture;
	public Texture2D HoverTexture;

	public Texture2D SliderTexture;
	public Texture2D SliderHoverTexture;
	public Texture2D ThumbTexture;
	public Texture2D ThumbHoverTexture;

	public Texture2D RedTexture;
	public Texture2D BlackTexture;

	public GUIStyle Panel;
	public GUIStyle Button;
	public GUIStyle Label;
	public GUIStyle Option;

	public GUIStyle Slider;
	public GUIStyle Thumb;

	void Start()
	{
		Panel = new GUIStyle();
		Panel.normal.background = PanelTexture;

		Button = new GUIStyle();
		Button.normal.textColor = new Color32(0, 200, 100, 255);
		Button.alignment = TextAnchor.MiddleCenter;
		Button.fontSize = 16;
		Button.fixedHeight = 30;
		Button.hover.textColor = new Color32(40, 40, 40, 255);
		Button.hover.background = HoverTexture;
		Button.padding = new RectOffset(10, 10, 10, 10);

		Label = new GUIStyle(Button);
		Label.normal.textColor = Color.white;
		Label.alignment = TextAnchor.MiddleRight;
		Label.fixedWidth = 200;

		Option = new GUIStyle(Button);
		Option.fixedWidth = 200;

		Slider = new GUIStyle();
		Slider.fixedHeight = 30;
		Slider.normal.background = SliderTexture;
		Slider.hover.background = SliderHoverTexture;

		Thumb = new GUIStyle();
		Thumb.fixedHeight = 30;
		Thumb.fixedWidth = 30;
		Thumb.normal.background = ThumbTexture;
		Thumb.hover.background = ThumbHoverTexture;
	}
}
using System;
using UnityEngine;

public class MenuStyle : MonoBehaviour
{
	public Texture2D PanelTexture;
	public Texture2D HoverTexture;

	public Texture2D SliderTexture;
	public Texture2D ThumbTexture;

	public GUIStyle Panel;
	public GUIStyle Button;
	public GUIStyle Label;
	public GUIStyle Option;
	public GUIStyle Toggle;

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
		Button.hover.textColor = new Color32(0, 250, 150, 255);
		Button.hover.background = HoverTexture;
		Button.padding = new RectOffset(10, 10, 10, 10);

		Label = new GUIStyle(Button);
		Label.normal.textColor = Color.white;
		Label.alignment = TextAnchor.MiddleRight;
		Label.fixedWidth = 200;

		Option = new GUIStyle(Button);
		Option.fixedWidth = 200;

		Toggle = new GUIStyle(Option);
		Toggle.fontSize = 35;

		Slider = new GUIStyle();
		Slider.fixedHeight = 30;
		Slider.normal.background = SliderTexture;

		Thumb = new GUIStyle();
		Thumb.fixedHeight = 30;
		Thumb.fixedWidth = 30;
		Thumb.normal.background = ThumbTexture;
	}
}
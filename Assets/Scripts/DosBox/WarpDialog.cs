using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class WarpDialog : MonoBehaviour
{
	public GameObject Actors;
	public bool warpMenuEnabled;
	public Box warpActor;
	public InputField positionX, positionY, positionZ;
	public InputField localPosX, localPosY, localPosZ;
	public InputField worldPosX, worldPosY, worldPosZ;
	public InputField boundingPosX, boundingPosY, boundingPosZ;
	public RectTransform Panel;

	public InputField angle;
	public ToggleButton AdvancedMode;

	private float lastTimeKeyPressed;

	void Start ()
	{
		ToggleAdvanceMode(false);
	}

	void Update ()
	{
		if (Input.GetMouseButtonUp(0)
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			warpMenuEnabled = false;
		}

		Panel.gameObject.SetActive(warpMenuEnabled);

		if(warpActor != null && GetComponent<DosBox>().ProcessReader != null)
		{
			if(!Panel.GetComponentsInChildren<InputField>().Any(x => x.isFocused))
			{
				bool enoughTimeElapsed = (Time.time - lastTimeKeyPressed) > 0.1f;
				if (enoughTimeElapsed)
				{
					if (Input.GetKey(KeyCode.Keypad9))
					{
						RotateActor(-1);
					}

					if (Input.GetKey(KeyCode.Keypad7))
					{
						RotateActor(1);
					}

					if (Input.GetKey(KeyCode.Keypad4))
					{
						MoveActor(new Vector3(-1.0f, 0.0f, 0.0f));
					}

					if (Input.GetKey(KeyCode.Keypad6))
					{
						MoveActor(new Vector3(1.0f, 0.0f, 0.0f));
					}

					if (Input.GetKey(KeyCode.Keypad2))
					{
						MoveActor(new Vector3(0.0f, 0.0f, -1.0f));
					}

					if (Input.GetKey(KeyCode.Keypad8))
					{
						MoveActor(new Vector3(0.0f, 0.0f, 1.0f));
					}
				}

				if (Input.GetKeyUp(KeyCode.Keypad4) ||
					Input.GetKeyUp(KeyCode.Keypad8) ||
					Input.GetKeyUp(KeyCode.Keypad6) ||
					Input.GetKeyUp(KeyCode.Keypad2) ||
					Input.GetKeyUp(KeyCode.Keypad7) ||
					Input.GetKeyUp(KeyCode.Keypad9) ||
					Input.GetKey(KeyCode.Keypad0))
				{
					lastTimeKeyPressed = 0.0f;
				}
			}
		}

		//warp to mouse position
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.W))
		{
			if (GetComponent<DosBox>().ProcessReader != null)
			{
				//select player by default
				if (warpActor == null)
				{
					warpActor = GetComponent<DosBox>().Player;
				}

				Vector3 offset = GetComponent<DosBox>().GetMousePosition(warpActor.Room, warpActor.Floor) - warpActor.LocalPosition;
				offset = new Vector3(Mathf.RoundToInt(offset.x), 0.0f, Mathf.RoundToInt(offset.z));
				MoveActor(offset);
			}
		}
	}

	public void LoadActor(Box actor)
	{
		UpdateAngleInputField(actor);
		UpdatePositionInputFields(actor);
		warpActor = actor;
	}

	public void SetPositionClick()
	{
		//parse angle
		int angleInt;
		TryParseAngle(angle, out angleInt, (int)warpActor.Angles.y);
		WriteActorAngle(warpActor, angleInt);
		UpdateAngleInputField(warpActor);

		//parse position
		Vector3 lowerBound, upperBound, local, world;

		if (!AdvancedMode.BoolValue)
		{
			TryParsePosition(positionX, positionY, positionZ, out local, warpActor.LocalPosition);

			//apply offset to world/bound
			Vector3 offset = local - warpActor.LocalPosition;
			world = warpActor.WorldPosition + offset;
			lowerBound = warpActor.BoundingLower + offset;
			upperBound = warpActor.BoundingUpper + offset;
		}
		else
		{
			Vector3 boundingPos;
			TryParsePosition(boundingPosX, boundingPosY, boundingPosZ, out boundingPos, warpActor.BoundingPos);
			TryParsePosition(localPosX, localPosY, localPosZ, out local, warpActor.LocalPosition);
			TryParsePosition(worldPosX, worldPosY, worldPosZ, out world, warpActor.WorldPosition);

			Vector3 offset = boundingPos - warpActor.BoundingPos;
			lowerBound = warpActor.BoundingLower + offset;
			upperBound = warpActor.BoundingUpper + offset;
		}

		WriteActorPosition(warpActor, lowerBound, upperBound, local, world);
		UpdatePositionInputFields(warpActor);
	}

	public void AdvancedModeClick()
	{
		AdvancedMode.BoolValue = !AdvancedMode.BoolValue;
		ToggleAdvanceMode(AdvancedMode.BoolValue);
	}

	public void ToggleAdvanceMode(bool enabled)
	{
		positionX.transform.parent.parent.gameObject.SetActive(!enabled);
		localPosX.transform.parent.parent.gameObject.SetActive(enabled);
		worldPosX.transform.parent.parent.gameObject.SetActive(enabled);
		boundingPosX.transform.parent.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	//input keys
	void RotateActor(int offset)
	{
		int angle = (int)warpActor.Angles.y + offset;
		WriteActorAngle(warpActor, (angle + 1024) % 1024);
		UpdateAngleInputField(warpActor);
		lastTimeKeyPressed = Time.time;
	}

	//input keys
	void MoveActor(Vector3 offset)
	{
		Vector3 local = warpActor.LocalPosition + offset;
		Vector3 world = warpActor.WorldPosition + offset;
		Vector3 boundLow = warpActor.BoundingLower + offset;
		Vector3 boundUpper = warpActor.BoundingUpper + offset;

		WriteActorPosition(warpActor, boundLow, boundUpper, local, world);
		UpdatePositionInputFields(warpActor);
		lastTimeKeyPressed = Time.time;
	}

	void UpdatePositionInputFields(Box actor)
	{
		boundingPosX.text = actor.BoundingPos.x.ToString();
		boundingPosY.text = actor.BoundingPos.y.ToString();
		boundingPosZ.text = actor.BoundingPos.z.ToString();
		localPosX.text = positionX.text = actor.LocalPosition.x.ToString();
		localPosY.text = positionY.text = actor.LocalPosition.y.ToString();
		localPosZ.text = positionZ.text = actor.LocalPosition.z.ToString();
		worldPosX.text = actor.WorldPosition.x.ToString();
		worldPosY.text = actor.WorldPosition.y.ToString();
		worldPosZ.text = actor.WorldPosition.z.ToString();
	}

	void UpdateAngleInputField(Box actor)
	{
		angle.text = (actor.Angles.y * 360.0f / 1024.0f).ToString("N1");
	}

	private void TryParseAngle(InputField inputField, out int intValue, int defaultValue)
	{
		float floatValue;
		if(float.TryParse(inputField.text, out floatValue))
		{
			floatValue = floatValue >= 0.0f ? floatValue % 360.0f : 360.0f - ((-floatValue) % 360.0f);
			intValue = Mathf.RoundToInt((floatValue * 1024.0f) / 360.0f);
		}
		else
		{
			intValue = defaultValue;
		}
	}

	private void TryParsePosition(InputField posX, InputField posY, InputField posZ, out Vector3 intValue, Vector3 defaultValue)
	{
		int x, y, z;
		TryParsePosition(posX, out x, (int)defaultValue.x);
		TryParsePosition(posY, out y, (int)defaultValue.y);
		TryParsePosition(posZ, out z, (int)defaultValue.z);

		intValue = new Vector3(x, y, z);
	}

	private void TryParsePosition(InputField inputField, out int intValue, int defaultValue)
	{
		if(int.TryParse(inputField.text, out intValue))
		{
			intValue = Mathf.Clamp(intValue, short.MinValue, short.MaxValue);
		}
		else
		{
			intValue = defaultValue;
		}
	}

	private void WriteActorAngle(Box actor, int angle)
	{
		ProcessMemoryReader ProcessReader = GetComponent<DosBox>().ProcessReader;

		int index = Actors.GetComponentsInChildren<Box>(true).ToList().IndexOf(actor);
		if (index != -1)
		{
			long offset = GetComponent<DosBox>().GetActorMemoryAddress(index);
			byte[] position = new byte[2];
			Utils.WriteShort(angle, position, 0);
			ProcessReader.Write(position, offset + 42, 2);

			warpActor.Angles.y = angle;
		}
	}

	private void WriteActorPosition(Box actor, Vector3 lowerBound, Vector3 upperBound, Vector3 localPosition, Vector3 worldPosition)
	{
		ProcessMemoryReader ProcessReader = GetComponent<DosBox>().ProcessReader;

		//get object offset
		int index = Actors.GetComponentsInChildren<Box>(true).ToList().IndexOf(actor);
		if(index != -1)
		{
			long offset = GetComponent<DosBox>().GetActorMemoryAddress(index);

			//update to memory
			//bounds
			byte[] buffer = new byte[12];
			Utils.WriteShort((int)lowerBound.x, buffer, 0);
			Utils.WriteShort((int)upperBound.x, buffer, 2);
			Utils.WriteShort((int)lowerBound.y, buffer, 4);
			Utils.WriteShort((int)upperBound.y, buffer, 6);
			Utils.WriteShort((int)lowerBound.z, buffer, 8);
			Utils.WriteShort((int)upperBound.z, buffer, 10);
			ProcessReader.Write(buffer, offset + 8, 12);

			//local+world
			Utils.WriteShort((int)localPosition.x, buffer, 0);
			Utils.WriteShort((int)localPosition.y, buffer, 2);
			Utils.WriteShort((int)localPosition.z, buffer, 4);
			Utils.WriteShort((int)worldPosition.x, buffer, 6);
			Utils.WriteShort((int)worldPosition.y, buffer, 8);
			Utils.WriteShort((int)worldPosition.z, buffer, 10);
			ProcessReader.Write(buffer, offset + 28, 12);

			warpActor.LocalPosition = localPosition;
			warpActor.WorldPosition = worldPosition;
			warpActor.BoundingLower = lowerBound;
			warpActor.BoundingUpper = upperBound;
		}
	}		
}

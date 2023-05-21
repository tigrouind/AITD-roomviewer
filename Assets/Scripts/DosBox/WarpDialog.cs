using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class WarpDialog : MonoBehaviour
{
	public GameObject Actors;
	public bool WarpMenuEnabled;
	public Box WarpActorBox;
	public int WarpActorBoxId = -1;
	public InputField PositionX, PositionY, PositionZ;
	public InputField LocalPosX, LocalPosY, LocalPosZ;
	public InputField worldPosX, worldPosY, worldPosZ;
	public InputField BoundingPosX, BoundingPosY, BoundingPosZ;
	public RectTransform Panel;

	public InputField AngleX, AngleY, AngleZ;
	public ToggleButton AdvancedMode;

	private readonly Timer timer = new Timer();

	void Start()
	{
		timer.Start();
		ToggleAdvanceMode(false);
	}

	void Update()
	{
		if (Input.GetMouseButtonUp(0)
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			WarpMenuEnabled = false;
		}

		WarpActorBox = GetComponent<DosBox>().RefreshBoxUsingID(WarpActorBox, WarpActorBoxId);

		Panel.gameObject.SetActive(WarpMenuEnabled);

		if (GetComponent<DosBox>().ProcessMemory != null)
		{
			if (WarpActorBox != null && !Panel.GetComponentsInChildren<InputField>().Any(x => x.isFocused) && !GetComponent<ExchangeSlot>().ExchangeEnabled)
			{
				MoveOrRotateActor(WarpActorBox);
			}
		}
	}

	void MoveOrRotateActor(Box actor)
	{
		//move actor
		if (timer.Elapsed > 0.1f)
		{
			if (Input.GetKey(KeyCode.Keypad9))
			{
				RotateActor(actor, -1);
			}

			if (Input.GetKey(KeyCode.Keypad7))
			{
				RotateActor(actor, 1);
			}

			if (Input.GetKey(KeyCode.Keypad3))
			{
				MoveActor(actor, new Vector3Int(0, -1, 0));
			}

			if (Input.GetKey(KeyCode.Keypad1))
			{
				MoveActor(actor, new Vector3Int(0, 1, 0));
			}

			if (Input.GetKey(KeyCode.Keypad4))
			{
				MoveActor(actor, new Vector3Int(-1, 0, 0));
			}

			if (Input.GetKey(KeyCode.Keypad6))
			{
				MoveActor(actor, new Vector3Int(1, 0, 0));
			}

			if (Input.GetKey(KeyCode.Keypad2))
			{
				MoveActor(actor, new Vector3Int(0, 0, -1));
			}

			if (Input.GetKey(KeyCode.Keypad8))
			{
				MoveActor(actor, new Vector3Int(0, 0, 1));
			}
		}

		if (Input.GetKeyUp(KeyCode.Keypad1) ||
			Input.GetKeyUp(KeyCode.Keypad3) ||
			Input.GetKeyUp(KeyCode.Keypad4) ||
			Input.GetKeyUp(KeyCode.Keypad8) ||
			Input.GetKeyUp(KeyCode.Keypad6) ||
			Input.GetKeyUp(KeyCode.Keypad2) ||
			Input.GetKeyUp(KeyCode.Keypad7) ||
			Input.GetKeyUp(KeyCode.Keypad9) ||
			Input.GetKey(KeyCode.Keypad0))
		{
			timer.Elapsed = 1.0f;
		}
	}

	public void WarpActor()
	{
		//select player by default
		if (WarpActorBox == null)
		{
			WarpActorBox = GetComponent<DosBox>().Player;
			WarpActorBoxId = WarpActorBox.ID;
		}

		//warp to mouse position
		if (WarpActorBox != null && GetComponent<DosBox>().ProcessMemory != null)
		{
			Vector3Int offset = GetComponent<RoomLoader>().GetMousePosition(WarpActorBox.Room, WarpActorBox.Floor) - WarpActorBox.BoundingPos;
			offset = new Vector3Int(offset.x, 0, offset.z);
			MoveActor(WarpActorBox, offset);
		}
	}

	public void LoadActor(Box actor)
	{
		UpdateAngleInputField(actor);
		UpdatePositionInputFields(actor);
		WarpActorBox = actor;
		WarpActorBoxId = actor.ID;
	}

	public void SetPositionClick()
	{
		SetPosition(WarpActorBox);
	}

	void SetPosition(Box actor)
	{
		//parse angle
		Vector3Int angleInt;
		TryParseAngle(AngleX, AngleY, AngleZ, out angleInt, actor.Angles);
		WriteActorAngle(actor, angleInt);
		UpdateAngleInputField(actor);

		//parse position
		Vector3Int lowerBound, upperBound, local, world;

		if (!AdvancedMode.BoolValue)
		{
			TryParsePosition(PositionX, PositionY, PositionZ, out local, actor.LocalPosition + actor.Mod);
			local -= actor.Mod;

			//apply offset to world/bound
			Vector3Int offset = local - actor.LocalPosition;
			world = actor.WorldPosition + offset;
			lowerBound = actor.BoundingLower + offset;
			upperBound = actor.BoundingUpper + offset;
		}
		else
		{
			Vector3Int boundingPos;
			TryParsePosition(BoundingPosX, BoundingPosY, BoundingPosZ, out boundingPos, actor.BoundingPos);
			TryParsePosition(LocalPosX, LocalPosY, LocalPosZ, out local, actor.LocalPosition + actor.Mod);
			TryParsePosition(worldPosX, worldPosY, worldPosZ, out world, actor.WorldPosition + actor.Mod);
			local -= actor.Mod;
			world -= actor.Mod;

			Vector3Int offset = boundingPos - actor.BoundingPos;
			lowerBound = actor.BoundingLower + offset;
			upperBound = actor.BoundingUpper + offset;
		}

		WriteActorPosition(actor, lowerBound, upperBound, local, world);
		UpdatePositionInputFields(actor);
	}

	public void AdvancedModeClick()
	{
		AdvancedMode.BoolValue = !AdvancedMode.BoolValue;
		ToggleAdvanceMode(AdvancedMode.BoolValue);
	}

	void ToggleAdvanceMode(bool enabled)
	{
		PositionX.transform.parent.parent.gameObject.SetActive(!enabled);
		LocalPosX.transform.parent.parent.gameObject.SetActive(enabled);
		worldPosX.transform.parent.parent.gameObject.SetActive(enabled);
		BoundingPosX.transform.parent.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	//input keys
	void RotateActor(Box actor, int offset)
	{
		int angle = (int)actor.Angles.y + offset;
		angle = (angle + 1024) % 1024;

		WriteActorAngle(actor, new Vector3Int(actor.Angles.x, angle, actor.Angles.z));
		UpdateAngleInputField(actor);
		timer.Restart();
	}

	//input keys or warp
	void MoveActor(Box actor, Vector3Int offset)
	{
		Vector3Int local = actor.LocalPosition + offset;
		Vector3Int world = actor.WorldPosition + offset;
		Vector3Int boundLow = actor.BoundingLower + offset;
		Vector3Int boundUpper = actor.BoundingUpper + offset;

		WriteActorPosition(actor, boundLow, boundUpper, local, world);
		UpdatePositionInputFields(actor);
		timer.Restart();
	}

	void UpdatePositionInputFields(Box actor)
	{
		BoundingPosX.text = actor.BoundingPos.x.ToString();
		BoundingPosY.text = actor.BoundingPos.y.ToString();
		BoundingPosZ.text = actor.BoundingPos.z.ToString();
		LocalPosX.text = PositionX.text = (actor.LocalPosition.x + actor.Mod.x).ToString();
		LocalPosY.text = PositionY.text = (actor.LocalPosition.y + actor.Mod.y).ToString();
		LocalPosZ.text = PositionZ.text = (actor.LocalPosition.z + actor.Mod.z).ToString();
		worldPosX.text = (actor.WorldPosition.x + actor.Mod.x).ToString();
		worldPosY.text = (actor.WorldPosition.y + actor.Mod.y).ToString();
		worldPosZ.text = (actor.WorldPosition.z + actor.Mod.z).ToString();
	}

	void UpdateAngleInputField(Box actor)
	{
		AngleX.text = (actor.Angles.x * 360.0f / 1024.0f).ToString("N1");
		AngleY.text = (actor.Angles.y * 360.0f / 1024.0f).ToString("N1");
		AngleZ.text = (actor.Angles.z * 360.0f / 1024.0f).ToString("N1");
	}

	void TryParseAngle(InputField angleX, InputField angleY, InputField angleZ, out Vector3Int intValue, Vector3Int defaultValue)
	{
		int x, y, z;
		TryParseAngle(angleX, out x, defaultValue.x);
		TryParseAngle(angleY, out y, defaultValue.y);
		TryParseAngle(angleZ, out z, defaultValue.z);

		intValue = new Vector3Int(x, y, z);
	}

	void TryParseAngle(InputField inputField, out int intValue, int defaultValue)
	{
		float floatValue;
		if (float.TryParse(inputField.text, out floatValue))
		{
			floatValue = floatValue >= 0.0f ? floatValue % 360.0f : 360.0f - ((-floatValue) % 360.0f);
			intValue = Mathf.RoundToInt(floatValue * 1024.0f / 360.0f);
		}
		else
		{
			intValue = defaultValue;
		}
	}

	void TryParsePosition(InputField posX, InputField posY, InputField posZ, out Vector3Int intValue, Vector3Int defaultValue)
	{
		int x, y, z;
		TryParsePosition(posX, out x, defaultValue.x);
		TryParsePosition(posY, out y, defaultValue.y);
		TryParsePosition(posZ, out z, defaultValue.z);

		intValue = new Vector3Int(x, y, z);
	}

	void TryParsePosition(InputField inputField, out int intValue, int defaultValue)
	{
		if (int.TryParse(inputField.text, out intValue))
		{
			intValue = Mathf.Clamp(intValue, short.MinValue, short.MaxValue);
		}
		else
		{
			intValue = defaultValue;
		}
	}

	void WriteActorAngle(Box actor, Vector3Int angle)
	{
		ProcessMemory processMemory = GetComponent<DosBox>().ProcessMemory;
		if (processMemory != null)
		{
			int address = GetComponent<DosBox>().GetActorMemoryAddress(actor.Slot);
			byte[] buffer = new byte[6];
			buffer.Write(angle, 0);
			processMemory.Write(buffer, address + 40, buffer.Length);

			actor.Angles = angle;
		}
	}

	void WriteActorPosition(Box actor, Vector3Int lowerBound, Vector3Int upperBound, Vector3Int localPosition, Vector3Int worldPosition)
	{
		ProcessMemory processMemory = GetComponent<DosBox>().ProcessMemory;
		if (processMemory != null)
		{
			//get object offset
			int address = GetComponent<DosBox>().GetActorMemoryAddress(actor.Slot);

			//update to memory
			//bounds
			byte[] buffer = new byte[12];
			buffer.Write(lowerBound, upperBound, 0);
			processMemory.Write(buffer, address + 8, 12);

			//local+world
			buffer.Write(localPosition, 0);
			buffer.Write(worldPosition, 6);
			processMemory.Write(buffer, address + 28, 12);

			actor.LocalPosition = localPosition;
			actor.WorldPosition = worldPosition;
			actor.BoundingLower = lowerBound;
			actor.BoundingUpper = upperBound;
		}
	}
}

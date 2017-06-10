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

		if(warpActor != null)
		{			
			if(!Panel.GetComponentsInChildren<InputField>().Any(x => x.isFocused))
			{
				bool enoughTimeElapsed = (Time.time - lastTimeKeyPressed) > 0.1f;
				if (Input.GetKey(KeyCode.Keypad9) && enoughTimeElapsed)
				{				
					RotateActor(-1);
				}

				if (Input.GetKey(KeyCode.Keypad7) && enoughTimeElapsed)
				{				
					RotateActor(1);
				}

				if (Input.GetKey(KeyCode.Keypad4) && enoughTimeElapsed)
				{
					MoveActor(new Vector3(-1.0f, 0.0f, 0.0f));
				}

				if (Input.GetKey(KeyCode.Keypad6) && enoughTimeElapsed)
				{
					MoveActor(new Vector3(1.0f, 0.0f, 0.0f));
				}

				if (Input.GetKey(KeyCode.Keypad2) && enoughTimeElapsed)
				{
					MoveActor(new Vector3(0.0f, 0.0f,-1.0f));
				}

				if (Input.GetKey(KeyCode.Keypad8) && enoughTimeElapsed)
				{
					MoveActor(new Vector3(00.0f, 0.0f, 1.0f));
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

			//warp to mouse position
			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
			{
				Func<bool> isAITD1 = () => GetComponent<RoomLoader>().DetectGame() == 1;		
				if (Input.GetKeyDown(KeyCode.W) && isAITD1())
				{
					Vector3 offset = GetComponent<DosBox>().GetMousePosition(warpActor.Room, warpActor.Floor) - warpActor.LocalPosition;
					offset = new Vector3(Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y), Mathf.RoundToInt(offset.z));
					MoveActor(offset);
				}
			}
		}
	}

	public void SetPositionClick()
	{
		//parse angle
		int angleInt;
		TryParseAngle(ref angle, out angleInt, Mathf.RoundToInt((warpActor.Angles.y * 1024.0f) / 360.0f));
		WriteActorAngle(warpActor, angleInt);

		//parse position
		Vector3 bound, local, world;

		if (!AdvancedMode.BoolValue)
		{
			TryParsePosition(ref positionX, ref positionY, ref positionZ, out local, warpActor.LocalPosition);

			//apply offset to world/bound
			Vector3 offset = local - warpActor.LocalPosition;
			world = warpActor.WorldPosition + offset;
			bound = warpActor.BoundingPos + offset;
		}
		else
		{
			TryParsePosition(ref boundingPosX, ref boundingPosY, ref boundingPosZ, out bound, warpActor.BoundingPos);
			TryParsePosition(ref localPosX, ref localPosY, ref localPosZ, out local, warpActor.LocalPosition);
			TryParsePosition(ref worldPosX, ref worldPosY, ref worldPosZ, out world, warpActor.WorldPosition);
		}

		UpdatePositionInputFields(local, world, bound);
		WriteActorPosition(warpActor, bound, local, world);
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
		int angleInt = Mathf.RoundToInt((warpActor.Angles.y * 1024.0f) / 360.0f);
		int newAngle = angleInt + offset;
		WriteActorAngle(warpActor, (newAngle + 1024) % 1024);
		angle.text = (newAngle * 360.0f / 1024.0f).ToString("N1");
		lastTimeKeyPressed = Time.time;
	}

	//input keys
	void MoveActor(Vector3 offset)
	{
		Vector3 local = warpActor.LocalPosition + offset;
		Vector3 world = warpActor.WorldPosition + offset;
		Vector3 bound = warpActor.BoundingPos + offset;

		WriteActorPosition(warpActor, bound, local, world);

		UpdatePositionInputFields(local, world, bound);
		lastTimeKeyPressed = Time.time;
	}

	void UpdatePositionInputFields(Vector3 local, Vector3 world, Vector3 bound)
	{
		//update gui
		localPosX.text = positionX.text = local.x.ToString();
		localPosY.text = positionY.text = local.y.ToString();
		localPosZ.text = positionZ.text = local.z.ToString();
		worldPosX.text = world.x.ToString();
		worldPosY.text = world.y.ToString();
		worldPosZ.text = world.z.ToString();
		boundingPosX.text = bound.x.ToString();
		boundingPosY.text = bound.y.ToString();
		boundingPosZ.text = bound.z.ToString();
	}
	
	private void TryParseAngle(ref InputField inputField, out int intValue, int defaultValue)
	{
		float floatValue;
		if(float.TryParse(inputField.text, out floatValue))
		{
			floatValue = floatValue >= 0.0f ? floatValue % 360.0f : 360.0f - ((-floatValue) % 360.0f);
			intValue = Mathf.RoundToInt((floatValue * 1024.0f) / 360.0f) ;
		}
		else
		{
			intValue = defaultValue;
		}

		inputField.text = (intValue * 360 / 1024.0f).ToString("N1");
	}
	
	private void TryParsePosition(ref InputField posX, ref InputField posY, ref InputField posZ, out Vector3 intValue, Vector3 defaultValue)
	{
		int x, y, z;
		TryParsePosition(ref posX, out x, (int)defaultValue.x);
		TryParsePosition(ref posY, out y, (int)defaultValue.y);
		TryParsePosition(ref posZ, out z, (int)defaultValue.z);

		intValue = new Vector3(x, y, z);
	}

	private void TryParsePosition(ref InputField inputField, out int intValue, int defaultValue)
	{
		if(int.TryParse(inputField.text, out intValue))
		{
			intValue = Mathf.Clamp(intValue, short.MinValue, short.MaxValue);
		}
		else
		{
			intValue = defaultValue;
		}

		inputField.text = intValue.ToString();
	}
	
	private void WriteActorAngle(Box actor, int angle)
	{
		ProcessMemoryReader ProcessReader = GetComponent<DosBox>().ProcessReader;

		int index = Actors.GetComponentsInChildren<Box>(true).ToList().IndexOf(actor);
		if (index != -1)
		{
			long offset = GetComponent<DosBox>().GetActorMemoryAddress(index);
			byte[] position = new byte[2];
			WriteShort(angle, position, 0);
			ProcessReader.Write(position, offset + 42, 2);
		}
	}

	private void WriteActorPosition(Box actor, Vector3 boundingPosition, Vector3 localPosition, Vector3 worldPosition)
	{
		ProcessMemoryReader ProcessReader = GetComponent<DosBox>().ProcessReader;

		//get object offset
		int index = Actors.GetComponentsInChildren<Box>(true).ToList().IndexOf(actor);
		if(index != -1)
		{
			long offset = GetComponent<DosBox>().GetActorMemoryAddress(index);

			//update to memory
			//bounding
			Vector3 boundOffset = boundingPosition - actor.BoundingPos;
			byte[] buffer = new byte[12];
			ProcessReader.Read(buffer, offset + 8, 12);
			WriteShort(ReadShort(buffer[0], buffer[1]) + (int)boundOffset.x, buffer, 0); 
			WriteShort(ReadShort(buffer[2], buffer[3]) + (int)boundOffset.x, buffer, 2);
			WriteShort(ReadShort(buffer[4], buffer[5]) + (int)boundOffset.y, buffer, 4); 
			WriteShort(ReadShort(buffer[6], buffer[7]) + (int)boundOffset.y, buffer, 6);
			WriteShort(ReadShort(buffer[8], buffer[9]) + (int)boundOffset.z, buffer, 8);
			WriteShort(ReadShort(buffer[10], buffer[11]) + (int)boundOffset.z, buffer, 10);
			ProcessReader.Write(buffer, offset + 8, 12);

			//local+world
			WriteShort((int)localPosition.x, buffer, 0); 
			WriteShort((int)localPosition.y, buffer, 2); 
			WriteShort((int)localPosition.z, buffer, 4);
			WriteShort((int)worldPosition.x, buffer, 6); 
			WriteShort((int)worldPosition.y, buffer, 8); 
			WriteShort((int)worldPosition.z, buffer, 10);
			ProcessReader.Write(buffer, offset + 28, 12);
		}
	}	

	private short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	private void WriteShort(int value, byte[] data, int offset)
	{
		unchecked
		{
			data[offset + 0] = (byte)(value & 0xFF);
			data[offset + 1] = (byte)(value >> 8);
		}
	}
}

using UnityEngine;
using UnityEngine.UI;

public class ExchangeSlot : MonoBehaviour
{
	public Text RightText;
	public bool ExchangeEnabled;
	private int targetSlot;

	public void UpdateTargetSlot(Box highLightedBox)
	{
		if (highLightedBox != null && highLightedBox.name == "Actor" && !GetComponent<WarpDialog>().WarpMenuEnabled && !Input.GetKeyDown(KeyCode.Escape))
		{
			if (Input.GetKeyDown(KeyCode.X))
			{
				ExchangeEnabled = !ExchangeEnabled;
				if(ExchangeEnabled)
				{
					targetSlot = -1;
				}
				UpdateTargetSlotText();
			}

			if (ExchangeEnabled)
			{
				if (InputDigit(ref targetSlot))
				{
					UpdateTargetSlotText();
				}
				else if (Input.GetKeyDown(KeyCode.Backspace))
				{
					if (targetSlot == -1) ExchangeEnabled = false;
					targetSlot = targetSlot >= 10 ? targetSlot / 10 : -1;
					UpdateTargetSlotText();
				}
				else if(Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
				{
					if (targetSlot >= 0 && targetSlot < 50 && highLightedBox != null)
					{
						ExchangeActorSlots(highLightedBox.Slot, targetSlot);
					}

					ExchangeEnabled = false;
					UpdateTargetSlotText();
				}
			}
		}
		else if (ExchangeEnabled)
		{
			ExchangeEnabled = false;
			UpdateTargetSlotText();
		}
	}

	void UpdateTargetSlotText()
	{
		if (ExchangeEnabled)
		{
			RightText.text = targetSlot == -1 ? "Exchange with SLOT" : string.Format("Exchange with SLOT {0}", targetSlot);
		}
		else
		{
			RightText.text = string.Empty;
		}
	}

	bool InputDigit(ref int value)
	{
		int digit;
		if (IsKeypadKeyDown(out digit))
		{
			if (value == -1)
			{
				value = digit;
			}
			else
			{
				int newValue = digit + value * 10;
				if (newValue < 50)
				{
					value = newValue;
				}
			}

			return true;
		}

		return false;
	}

	bool IsKeypadKeyDown(out int value)
	{
		for(int digit = 0 ; digit <= 9 ; digit++)
		{
			if (Input.GetKeyDown(KeyCode.Keypad0 + digit)
			|| Input.GetKeyDown(KeyCode.Alpha0 + digit))
			{
				value = digit;
				return true;
			}
		}

		value = -1;
		return false;
	}

	void ExchangeActorSlots(int slotFrom, int slotTo)
	{
		var process = GetComponent<DosBox>().ProcessMemory;
		if (process != null)
		{
			if (slotFrom != slotTo)
			{
				int actorSize = GetComponent<DosBox>().GetActorSize();
				int offsetFrom = GetComponent<DosBox>().GetActorMemoryAddress(slotFrom);
				int offsetTo = GetComponent<DosBox>().GetActorMemoryAddress(slotTo);

				byte[] memoryFrom = new byte[actorSize];
				byte[] memoryTo = new byte[actorSize];

				//exchange slots
				process.Read(memoryFrom, offsetFrom, actorSize);
				process.Read(memoryTo, offsetTo, actorSize);

				process.Write(memoryTo, offsetFrom, actorSize);
				process.Write(memoryFrom, offsetTo, actorSize);

				//update ownerID
				int objectIdFrom = memoryFrom.ReadShort(0);
				int objectIdTo = memoryTo.ReadShort(0);

				UpdateObjectOwnerID(objectIdFrom, slotTo, process);
				UpdateObjectOwnerID(objectIdTo, slotFrom, process);
			}
		}
		else
		{
			RightText.text = "Actor swap is not available";
		}
	}

	void UpdateObjectOwnerID(int objectID, int ownerID, ProcessMemory processReader)
	{
		if (objectID != -1)
		{
			int address = GetComponent<DosBox>().GetObjectMemoryAddress(objectID);

			byte[] buffer = new byte[2];
			buffer.Write((short)ownerID, 0);
			processReader.Write(buffer, address, buffer.Length);
		}
	}
}
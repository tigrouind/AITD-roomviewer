using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

public class DosBox : MonoBehaviour
{
	public Text RightText;
	public BoxInfo BoxInfo;
	public GameObject Actors;
	public Arrow Arrow;
	public Box BoxPrefab;
	public Box[] Boxes;
	public uint InternalTimer1;
	public int InternalTimer2;
	public bool ShowAdditionalInfo;
	public bool ShowAITD1Vars;
	public bool SpeedRunMode;

	public ProcessMemoryReader ProcessReader;
	public Box Player;
	public bool IsCDROMVersion;

	private int actorsAddress;
	private int entryPoint;
	private Dictionary<int, int> bodyIdToMemoryAddress = new Dictionary<int, int>();
	private Dictionary<int, int> animIdToMemoryAddress = new Dictionary<int, int>();

	//initial player position
	private int dosBoxPattern;
	private int[] actorArrayAddress = new []
	{
		0x220CE, //AITD1 cdrom (GOG)
		0x300D0, //AITD2
		0x38180, //AITD3
		0x39EC4, //JACK
		0x20542, //AITD1 floppy
		0x2050A, //AITD1 demo
		0x2ADD0  //TIMEGATE
	};

	//offset to apply to get beginning of actors array
	private int[] actorStructSize = new [] { 160, 180, 182, 180, 202 };
	//size of one actor
	private int[] trackModeOffsets = new [] { 82, 90, 90, 90, 90 };

	private Vector3 lastPlayerPosition;
	private int lastValidPlayerIndex = -1;
	private int linkfloor;
	private int linkroom;
	private byte[] memory = new byte[640 * 1024];

	//fps
	private int oldFramesCount;
	private Queue<int> previousFramesCount = new Queue<int>();
	private Queue<float> previousFrameTime = new Queue<float>();
	private int frameCounter;

	private float lastDelay;
	private Timer delayCounter = new Timer();
	private Timer totalDelay = new Timer();

	private int inHand;
	private bool allowInventory;
	private bool saveTimerFlag;
	private int targetSlot;

	Box GetActor(int index)
	{
		Box box = Boxes[index];
		if (box == null)
		{
			box = Instantiate(BoxPrefab);
			box.transform.parent = Actors.transform;
			box.name = "Actor";
			box.Slot = index;
			box.DosBox = this;
			Boxes[index] = box;
		}

		return box;
	}

	void RemoveActor(int index)
	{
		Box box = Boxes[index];
		if (box != null)
		{
			Destroy(box.gameObject);
			Boxes[index] = null;
		}
	}

	void OnDestroy()
	{
		if (ProcessReader != null)
		{
			ProcessReader.Close();
		}
	}

	public void RefreshMemory()
	{
		if (ProcessReader != null && ProcessReader.Read(memory, 0, memory.Length) == 0)
		{
			//unlink DOSBOX
			GetComponent<RoomLoader>().LinkToDosBox();
		}
	}

	public void UpdateAllActors()
	{
		Player = null;
		if (ProcessReader != null)
		{
			//read actors info
			for (int i = 0 ; i < Boxes.Length ; i++) //up to 50 actors max
			{
				int k = actorsAddress + i * actorStructSize[dosBoxPattern];
				int id = memory.ReadShort(k + 0);

				if (id != -1)
				{
					Box box = GetActor(i);
					box.ID = id;
					box.Body = memory.ReadShort(k + 2);
					box.Flags = memory.ReadShort(k + 4);
					box.ColFlags = memory.ReadShort(k + 6);

					memory.ReadBoundingBox(k + 8, out box.BoundingLower, out box.BoundingUpper);

					FixBoundingWrap(ref box.BoundingLower.x, ref box.BoundingUpper.x);
					FixBoundingWrap(ref box.BoundingLower.z, ref box.BoundingUpper.z);

					memory.ReadBoundingBox(k + 20, out box.Box2DLower, out box.Box2DUpper);

					box.LocalPosition = memory.ReadVector(k + 28);
					box.WorldPosition = memory.ReadVector(k + 34);
					box.Angles = memory.ReadVector(k + 40);

					box.Floor = memory.ReadShort(k + 46);
					box.Room = memory.ReadShort(k + 48);
					box.LifeMode = memory. ReadShort(k + 50);
					box.Life = memory.ReadShort(k + 52);
					box.Chrono = memory.ReadUnsignedInt(k + 54);
					box.RoomChrono = memory.ReadUnsignedInt(k + 58);
					box.Anim = memory.ReadShort(k + 62);
					box.AnimType = memory.ReadShort(k + 64);
					box.NextAnim = memory.ReadShort(k + 66);
					box.Keyframe = memory.ReadShort(k + 74);
					box.TotalFrames = memory.ReadShort(k + 76);
					box.EndFrame = memory.ReadShort(k + 78);
					box.EndAnim = memory.ReadShort(k + 80);

					int trackModeOffset = trackModeOffsets[dosBoxPattern];
					box.TrackMode = memory.ReadShort(k + trackModeOffset);
					box.TrackNumber = memory.ReadShort(k + 84);
					box.PositionInTrack = memory.ReadShort(k + 88);

					int bodyAddress, animAddress;
					if (bodyIdToMemoryAddress.TryGetValue(box.Body, out bodyAddress) &&
					    animIdToMemoryAddress.TryGetValue(box.Anim, out animAddress))
					{
						int bonesInAnim = memory.ReadShort(animAddress + 2);
						int keyframeAddress = animAddress + box.Keyframe * (bonesInAnim * 8 + 8);
						box.KeyFrameTime = memory.ReadUnsignedShort(bodyAddress + 20);
						box.KeyFrameLength = memory.ReadShort(keyframeAddress + 4);
					}

					if (dosBoxPattern == 0) //AITD1 only
					{
						box.Mod = memory.ReadVector(k + 90);
					}
					else
					{
						box.Mod = Vector3.zero;
					}

					box.OldAngle = memory.ReadShort(k + 106);
					box.NewAngle = memory.ReadShort(k + 108);
					box.RotateTime = memory.ReadShort(k + 110);
					box.Speed = memory.ReadShort(k + 116);

					box.Col = memory.ReadVector(k + 126);
					box.ColBy = memory.ReadShort(k + 132);
					box.HardTrigger = memory.ReadShort(k + 134);
					box.HardCol = memory.ReadShort(k + 136);
					box.Hit = memory.ReadShort(k + 138);
					box.HitBy = memory.ReadShort(k + 140);
					box.ActionType = memory.ReadShort(k + 142);
					box.HotBoxSize = memory.ReadShort(k + 148);
					box.HitForce = memory.ReadShort(k + 150);
					box.HotPosition = memory.ReadVector(k + 154);
				}
				else
				{
					RemoveActor(i);
				}
			}

			//search current camera target
			int cameraTargetID = -1;
			if (IsCDROMVersion)
			{
				int currentCameraTarget = memory.ReadShort(entryPoint + 0x19B6C);
				foreach (Box box in Boxes)
				{
					if (box != null && box.Slot == currentCameraTarget)
					{
						cameraTargetID = box.ID;
						break;
					}
				}
			}

			//search player
			foreach (Box box in Boxes)
			{
				if (box != null && (box.TrackMode == 1 || box.ID == lastValidPlayerIndex))
				{
					//update player index
					lastValidPlayerIndex = cameraTargetID = box.ID;
					break;
				}
			}

			//automatically switch room and floor (has to be done before setting other actors positions)
			foreach (Box box in Boxes)
			{
				if (box != null && box.ID == cameraTargetID)
				{
					SwitchRoom(box.Floor, box.Room);
				}
			}

			//update all boxes
			foreach (Box box in Boxes)
			{
				if (box != null)
				{
					Transform roomObject = GetComponent<RoomLoader>().GetRoom(box.Floor, box.Room);
					if (roomObject != null)
					{
						//local to global position
						Vector3 boxPosition = box.BoundingPos / 1000.0f;
						boxPosition = new Vector3(boxPosition.x, -boxPosition.y, boxPosition.z) + roomObject.localPosition;

						if (box.transform.position != boxPosition)
						{
							Vector3 offset = 1000.0f * (box.transform.position - boxPosition);
							float distance = new Vector3(Mathf.Round(offset.x), 0.0f, Mathf.Round(offset.z)).magnitude;
							box.LastOffset = Mathf.RoundToInt(distance);
							box.LastDistance += distance;
							box.transform.position = boxPosition;
						}

						//make actors appears slightly bigger than they are to be not covered by colliders
						Vector3 delta = Vector3.one;
						box.transform.localScale = (box.BoundingSize + delta) / 1000.0f;

						//make sure very small actors are visible
						box.transform.localScale = Vector3.Max(box.transform.localScale, Vector3.one * 0.1f);

						bool isAITD1 = dosBoxPattern == 0;
						if (isAITD1)
						{
							UpdateHotPointBox(box, roomObject.localPosition);
						}

						//camera target
						if(box.ID == cameraTargetID)
						{
							//check if player has moved
							if (box.transform.position.x != lastPlayerPosition.x || box.transform.position.z != lastPlayerPosition.z)
							{
								//center camera to player position
								GetComponent<RoomLoader>().CenterCamera(new Vector2(box.transform.position.x, box.transform.position.z));
								lastPlayerPosition = box.transform.position;
							}
						}

						//player
						bool isPlayer = box.ID == lastValidPlayerIndex;
						if (isPlayer)
						{
							//arrow follow player
							Arrow.transform.position = box.transform.position + new Vector3(0.0f, box.transform.localScale.y / 2.0f + 0.001f, 0.0f);

							//face camera
							float angle = box.Angles.y * 360.0f / 1024.0f;
							Arrow.transform.rotation = Quaternion.AngleAxis(90.0f, -Vector3.left);
							Arrow.transform.rotation *= Quaternion.AngleAxis((angle + 180.0f) % 360.0f, Vector3.forward);

							float minBoxScale = Mathf.Min(box.transform.localScale.x, box.transform.localScale.z);
							Arrow.transform.localScale = new Vector3(
								minBoxScale * 0.9f,
								minBoxScale * 0.9f,
								1.0f);

							//player is white
							box.Color = new Color32(255, 255, 255, 255);
							Arrow.AlwaysOnTop = Camera.main.orthographic;
							Player = box;
						}
						else
						{
							if (box.Slot == 0)
							{
								box.Color = new Color32(255, 255, 255, 255);
							}
							else
							{
								//other actors are green
								box.Color = new Color32(0, 128, 0, 255);
							}
						}

						if (isAITD1)
						{
							UpdateWorldPosBox(box, roomObject.localPosition, isPlayer);
						}

						box.AlwaysOnTop = Camera.main.orthographic;
					}
					else
					{
						RemoveActor(box.Slot);
					}
				}
			}

			if (ShowAITD1Vars)
			{
				allowInventory = memory.ReadShort(entryPoint + 0x19B6E) == 1;
				inHand = memory.ReadShort(entryPoint + 0x24054);

				//set by AITD when long running code is started (eg: loading ressource)
				saveTimerFlag = memory[entryPoint + 0x1B0FC] == 1;

				if (!saveTimerFlag)
				{
					InternalTimer1 = memory.ReadUnsignedInt(entryPoint + 0x19D12);
					InternalTimer2 = memory.ReadUnsignedShort(entryPoint + 0x242E0);
				}
			}
		}

		//arrow is only active if actors are active and player is active
		Arrow.gameObject.SetActive(Actors.activeSelf
			&& Player != null
			&& Player.gameObject.activeSelf
			&& Player.transform.localScale.magnitude > 0.01f);
	}

	void SwitchRoom(int floor, int room)
	{
		if (linkfloor != floor || linkroom != room)
		{
			linkfloor = floor;
			linkroom = room;

			GetComponent<RoomLoader>().RefreshRooms(linkfloor, linkroom);
		}
	}

	void RefreshCacheEntries(Dictionary<int, int> entries, int address)
	{
		entries.Clear();
		int cacheAddress = memory.ReadFarPointer(entryPoint + address);
		if (cacheAddress > 0)
		{
			int numEntries = Math.Min((int)memory.ReadUnsignedShort(cacheAddress + 16), 100);
			int baseAddress = memory.ReadFarPointer(cacheAddress + 18);

			for (int i = 0 ; i < numEntries ; i++)
			{
				int addr = cacheAddress + 22 + i * 10;
				int id = memory.ReadUnsignedShort(addr);
				int offset = memory.ReadUnsignedShort(addr + 2);
				entries[id] = baseAddress + offset;
			}
		}
	}

	void UpdateHotPointBox(Box box, Vector3 roomPosition)
	{
		//hot point
		Box hotPoint = box.BoxHotPoint;

		if (box.ActionType == 2)
		{
			if (hotPoint == null)
			{
				hotPoint = Instantiate(BoxPrefab);
				hotPoint.name = "HotPoint";
				hotPoint.Color = new Color32(255, 0, 0, 255);
				Destroy(hotPoint.gameObject.GetComponent<BoxCollider>());
				box.BoxHotPoint = hotPoint;
			}

			Vector3 finalPos = (box.HotPosition + box.LocalPosition + box.Mod) / 1000.0f;
			finalPos = new Vector3(finalPos.x, -finalPos.y, finalPos.z) + roomPosition;
			hotPoint.transform.position = finalPos;

			hotPoint.transform.localScale = Vector3.one * (box.HotBoxSize / 500.0f);
			hotPoint.AlwaysOnTop = Camera.main.orthographic;
		}
		else if (hotPoint != null)
		{
			Destroy(hotPoint.gameObject);
			box.BoxHotPoint = null;
		}
	}

	void UpdateWorldPosBox(Box box, Vector3 roomPosition, bool isPlayer)
	{
		Box worldPos = box.BoxWorldPos;
		Vector3 boundingPos = box.WorldPosition + box.Mod;
		//worldpos unsync
		if (isPlayer && (boundingPos.x != box.BoundingPos.x || boundingPos.z != box.BoundingPos.z))
		{
			if (worldPos == null)
			{
				worldPos = Instantiate(BoxPrefab);
				worldPos.name = "WorldPos";
				worldPos.Color = new Color32(255, 0, 0, 128);
				Destroy(worldPos.gameObject.GetComponent<BoxCollider>());
				box.BoxWorldPos = worldPos;
			}

			Vector3 finalPos = (box.WorldPosition + box.Mod) / 1000.0f;
			float height = -box.BoundingPos.y / 1000.0f;
			finalPos = new Vector3(finalPos.x, height + 0.001f, finalPos.z) + roomPosition;
			worldPos.transform.position = finalPos;
			worldPos.transform.localScale = box.transform.localScale;
			worldPos.AlwaysOnTop = Camera.main.orthographic;
		}
		else if (worldPos != null)
		{
			Destroy(worldPos.gameObject);
			box.BoxWorldPos = null;
		}
	}

	public void UpdateBoxInfo()
	{
		BoxInfo.Clear();
		if (Player != null)
		{
			float angle = Player.Angles.y * 360.0f / 1024.0f;
			float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

			BoxInfo.Append("Position", Player.LocalPosition + Player.Mod);
			BoxInfo.Append("Angle", "{0:N1} {1:N1}", angle, sideAngle);
		}

		if (ShowAITD1Vars || ShowAdditionalInfo)
		{
			if(Player != null) BoxInfo.AppendLine();

			if (ShowAITD1Vars)
			{
				int calculatedFps = previousFramesCount.Sum();
				TimeSpan totalDelayTS = TimeSpan.FromSeconds(totalDelay.Elapsed);

				BoxInfo.Append("Timer 1", "{0}.{1:D2}", TimeSpan.FromSeconds(InternalTimer1 / 60), InternalTimer1 % 60);
				BoxInfo.Append("Timer 2", "{0}.{1:D2}", TimeSpan.FromSeconds(InternalTimer2 / 60), InternalTimer2 % 60);
				BoxInfo.Append("FPS/Frame/Delay", "{0}; {1}; {2} ms", calculatedFps, frameCounter, Mathf.FloorToInt(lastDelay * 1000));
				BoxInfo.Append("Total delay", "{0:D2}:{1:D2}:{2:D2}.{3:D3} ", totalDelayTS.Hours, totalDelayTS.Minutes, totalDelayTS.Seconds, totalDelayTS.Milliseconds);
			}

			Vector3 mousePosition = GetMousePosition(linkroom, linkfloor);
			BoxInfo.Append("Cursor position", "{0} {1}", Mathf.Clamp((int)(mousePosition.x), -32768, 32767), Mathf.Clamp((int)(mousePosition.z), -32768, 32767));
			if(Player != null) BoxInfo.Append("Last offset/dist", "{0}; {1}", Player.LastOffset, Mathf.RoundToInt(Player.LastDistance));

			if (ShowAITD1Vars)
			{
				BoxInfo.Append("Allow inventory", allowInventory ? "Yes" : "No");
				BoxInfo.Append("In hand", inHand);
			}
		}

		BoxInfo.UpdateText();
	}

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			totalDelay.Reset();
		}
		if (Input.GetKeyDown(KeyCode.W) && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
		{
			foreach (Box box in Boxes)
			{
				if (box != null)
				{
					box.LastDistance = 0.0f;
				}
			}
		}
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && IsCDROMVersion && ProcessReader != null)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				//internal timer 1
				InternalTimer1 -= 60 * 5; //back 5 frames
				byte[] buffer = new byte[4];
				buffer.Write(InternalTimer1, 0);
				ProcessReader.Write(buffer, entryPoint + 0x19D12, buffer.Length);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				//internal timer 2
				InternalTimer2 -= 60 * 5; //back 5 frames
				byte[] buffer = new byte[2];
				buffer.Write((ushort)InternalTimer2, 0);
				ProcessReader.Write(buffer, entryPoint + 0x242E0, buffer.Length);
			}
		}
	}

	public void RefreshCacheEntries()
	{
		if (ShowAITD1Vars)
		{
			RefreshCacheEntries(animIdToMemoryAddress, 0x218D3);
			RefreshCacheEntries(bodyIdToMemoryAddress, 0x218D7);
		}
	}

	public void CalculateFPS()
	{
		if (ProcessReader != null && ShowAITD1Vars)
		{
			//fps
			int fps = memory.ReadShort(entryPoint + 0x19D18);

			//frames counter (reset to zero every second by AITD)
			int frames = memory.ReadShort(entryPoint + 0x2117C);

			//check how much frames elapsed since last time
			int diff;
			if (frames >= oldFramesCount)
			{
				diff = frames - oldFramesCount; //eg: 20 - 15
			}
			else
			{
				diff = fps - oldFramesCount + frames; //special case: eg: 60 - 58 + 3
			}
			oldFramesCount = frames;
			frameCounter += diff;

			if(delayCounter.Elapsed >= 0.1f) //100ms
			{
				lastDelay = delayCounter.Elapsed;
			}

			//check for large delays
			if (!saveTimerFlag)
			{
				delayCounter.Reset();
				totalDelay.Stop();
			}
			else
			{
				delayCounter.Start();
				totalDelay.Start();
			}

			float time = Time.time;
			if (diff > 0)
			{
				previousFramesCount.Enqueue(diff);
				previousFrameTime.Enqueue(time);
			}

			//remove any frame info older than one second
			while (previousFrameTime.Count > 0 &&
				previousFrameTime.Peek() < (time - 1.0f))
			{
				previousFramesCount.Dequeue();
				previousFrameTime.Dequeue();
			}
		}
	}

	void FixBoundingWrap(ref float a, ref float b)
	{
		if(a > b)
		{
			if(a < -b)
			{
				b += 65536.0f;
			}
			else
			{
				a -= 65536.0f;
			}
		}
	}

	int SearchDosBoxProcess()
	{
		int? processId = Process.GetProcesses()
				.Where(x => GetProcessName(x).StartsWith("DOSBOX", StringComparison.InvariantCultureIgnoreCase))
				.Select(x => (int?)x.Id)
				.FirstOrDefault();

		if(processId.HasValue)
		{
			return processId.Value;
		}

		return -1;
	}

	string GetProcessName(Process process)
	{
		try
		{
			//could fail because not enough permissions (eg : admin process)
			return process.ProcessName;
		}
		catch
		{
			return string.Empty;
		}
	}

	bool TryGetMemoryReader(out ProcessMemoryReader reader)
	{
		int processId = SearchDosBoxProcess();
		if (processId != -1)
		{
			reader = new ProcessMemoryReader(processId);
			reader.BaseAddress = reader.SearchFor16MRegion();
			if (reader.BaseAddress != -1)
			{
				return true;
			}

			reader.Close();
		}

		reader = null;
		return false;
	}

	bool TryGetExeEntryPoint(out int entryPoint)
	{
		int psp = memory.ReadUnsignedShort(0x0B30) * 16;
		if (psp > 0)
		{
			int exeSize = memory.ReadUnsignedShort(psp - 16 + 3) * 16;
			if (exeSize > 100 * 1024 && exeSize < 250 * 1024) //is AITD exe loaded yet?
			{
				entryPoint = psp + 0x100;
				return true;
			}
		}

		entryPoint = -1;
		return false;
	}

	#region Room loader

	public bool LinkToDosBOX(int floor, int room, int detectedGame)
	{
		if (!TryGetMemoryReader(out ProcessReader))
		{
			return false;
		}

		if (!FindActorsAddress(detectedGame))
		{
			ProcessReader.Close();
			ProcessReader = null;
			return false;
		}

		//force reload
		linkfloor = floor;
		linkroom = room;

		dosBoxPattern = detectedGame - 1;

		return true;
	}

	public void UnlinkDosBox()
	{
		if (ProcessReader != null)
		{
			ProcessReader.Close();
			ProcessReader = null;
		}

		BoxInfo.Clear(true);
		lastValidPlayerIndex = -1;
	}

	public bool FindActorsAddress(int detectedGame)
	{
		if (detectedGame == 5) //TIMEGATE
		{
			return FindActorsAddressTimeGate();
		}

		ProcessReader.Read(memory, 0, memory.Length);

		if (!TryGetExeEntryPoint(out entryPoint))
		{
			return false;
		}

		int patternIndex = detectedGame - 1;
		if (detectedGame == 1) //AITD1 only
		{
			//check version
			IsCDROMVersion = Utils.IndexOf(memory, Encoding.ASCII.GetBytes("CD Not Found")) != -1;
			if (!IsCDROMVersion)
			{
				if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("USA.PAK")) != -1)
				{
					patternIndex = 5; //demo
				}
				else
				{
					patternIndex = 4; //floppy
				}
			}
		}
		else
		{
			IsCDROMVersion = false;
		}

		actorsAddress = entryPoint + actorArrayAddress[patternIndex];
		return true;
	}

	public bool FindActorsAddressTimeGate()
	{
		//scan range: 0x110000 (extended memory) - 0x300000 (3MB)
		byte[] pattern = Encoding.ASCII.GetBytes("HARD_DEC");
		int dataSegment = ProcessReader.SearchForBytePattern(0x110000, 0x1F0000, buffer => Utils.IndexOf(buffer, pattern, 28, 16));

		if (dataSegment != -1)
		{
			ProcessReader.Read(memory, dataSegment + actorArrayAddress[6], 4);
			var result = memory.ReadUnsignedInt(0);
			if (result != 0)
			{
				actorsAddress = 0;
				ProcessReader.BaseAddress += result;
				return true;
			}
		}

		return false;
	}

	public void ResetCamera(int floor, int room)
	{
		lastPlayerPosition = Vector3.zero;
		linkfloor = floor;
		linkroom = room;
	}

	public int GetActorMemoryAddress(int index)
	{
		return actorsAddress + index * actorStructSize[dosBoxPattern];
	}

	public Vector3 GetMousePosition(int room, int floor)
	{
		Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + cameraHeight);
		Transform roomObject = GetComponent<RoomLoader>().GetRoom(floor, room);
		if (roomObject != null)
		{
			mousePosition -= roomObject.position;
		}
		return mousePosition * 1000.0f;
	}

	#endregion

	#region Exchange slots

	public void UpdateTargetSlot(Box highLightedBox)
	{
		if (highLightedBox != null && !GetComponent<WarpDialog>().WarpMenuEnabled)
		{
			if (InputDigit(ref targetSlot))
			{
				UpdateTargetSlotText();
			}

			if (Input.GetKeyDown(KeyCode.Backspace))
			{
				targetSlot = targetSlot >= 10 ? targetSlot / 10 : -1;
				UpdateTargetSlotText();
			}

			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				if (targetSlot >= 0 && targetSlot < 50)
				{
					ExchangeActorSlots(highLightedBox.Slot, targetSlot);
				}

				targetSlot = -1;
				UpdateTargetSlotText();
			}
		}
		else if (targetSlot != -1)
		{
			targetSlot = -1;
			UpdateTargetSlotText();
		}
	}

	void UpdateTargetSlotText()
	{
		RightText.text = (targetSlot == -1) ? string.Empty : string.Format("Exchange with SLOT {0}", targetSlot);
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
			if (Input.GetKeyDown(KeyCode.Keypad0 + digit))
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
		if (ProcessReader != null && IsCDROMVersion)
		{
			if (slotFrom != slotTo)
			{
				int actorSize = actorStructSize[dosBoxPattern];
				int offsetFrom = GetActorMemoryAddress(slotFrom);
				int offsetTo = GetActorMemoryAddress(slotTo);

				byte[] memoryFrom = new byte[actorSize];
				byte[] memoryTo = new byte[actorSize];

				//exchange slots
				ProcessReader.Read(memoryFrom, offsetFrom, actorSize);
				ProcessReader.Read(memoryTo, offsetTo, actorSize);

				ProcessReader.Write(memoryTo, offsetFrom, actorSize);
				ProcessReader.Write(memoryFrom, offsetTo, actorSize);

				//update ownerID
				int objectIdFrom = memoryFrom.ReadShort(0);
				int objectIdTo = memoryTo.ReadShort(0);

				UpdateObjectOwnerID(objectIdFrom, slotTo);
				UpdateObjectOwnerID(objectIdTo, slotFrom);
			}
		}
		else
		{
			RightText.text = "Actor swap is not available";
		}
	}

	void UpdateObjectOwnerID(int objectID, int ownerID)
	{
		if (objectID != -1)
		{
			int objectAddress = memory.ReadFarPointer(entryPoint + 0x2400E);
			int address = objectAddress + objectID * 52;

			byte[] buffer = new byte[2];
			buffer.Write((short)ownerID, 0);
			ProcessReader.Write(buffer, address, buffer.Length);
		}
	}

	#endregion
}

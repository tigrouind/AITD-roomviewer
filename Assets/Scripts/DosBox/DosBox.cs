using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

public class DosBox : MonoBehaviour
{
	public BoxInfo RightText;
	public GameObject Actors;
	public Arrow arrowPrefab;
	public Box BoxPrefab;
	public Box[] Boxes;
	public bool ShowAdditionalInfo;
	public bool ShowAITD1Vars;
	public bool SpeedRunMode;
	public int CurrentCamera = -1;
	public int CurrentCameraRoom = -1;
	public int CurrentCameraFloor = -1;

	public ProcessMemory ProcessMemory;
	public Box Player;

	private int entryPoint;
	private readonly Dictionary<int, int> bodyIdToMemoryAddress = new Dictionary<int, int>();
	private readonly Dictionary<int, int> animIdToMemoryAddress = new Dictionary<int, int>();
	private readonly Dictionary<int, int> trackIdToMemoryAddress = new Dictionary<int, int>();

	public GameVersion GameVersion;
	private GameConfig gameConfig;
	private readonly Rand rand = new Rand();
	private readonly Dictionary<GameVersion, GameConfig> gameConfigs = new Dictionary<GameVersion, GameConfig>
	{
		{ GameVersion.AITD1        , new GameConfig(0x220CE, 160, 82) },
		{ GameVersion.AITD1_FLOPPY , new GameConfig(0x20542, 160, 82) },
		{ GameVersion.AITD1_DEMO   , new GameConfig(0x2050A, 160, 82) },
		{ GameVersion.AITD2        , new GameConfig(0x300D0, 180, 90) },
		{ GameVersion.AITD2_FLOPPY , new GameConfig(0x2F850, 180, 90) },
		{ GameVersion.AITD2_DEMO   , new GameConfig(0x38DC0, 176, 86) },
		{ GameVersion.AITD3        , new GameConfig(0x38180, 182, 90) },
		{ GameVersion.AITD3_DEMO   , new GameConfig(0x377A0, 182, 90) },
		{ GameVersion.JACK         , new GameConfig(0x39EC4, 180, 90) },
		{ GameVersion.TIMEGATE     , new GameConfig(0x00000, 202, 90, 0x2ADD0) },
		{ GameVersion.TIMEGATE_DEMO, new GameConfig(0x00000, 202, 90, 0x45B98) },
	};

	private Vector3 lastPlayerPosition;
	private int lastValidPlayerIndex = -1;
	private int linkfloor;
	private int linkroom;
	private readonly byte[] memory = new byte[640 * 1024];

	//fps
	private int oldFramesCount;
	private readonly Queue<KeyValuePair<int, float>> previousFrames = new Queue<KeyValuePair<int, float>>();
	private int frameCounter;

	//delay
	private float lastDelay;
	private Timer delayCounter = new Timer();
	private Timer totalDelay = new Timer();

	//frame time
	private Timer frameTime = new Timer();
	private int oldFrames;
	private float frameTimeElapsed;

	private int inHand;
	private int redrawFlag;
	private bool allowInventory;
	private bool saveTimerFlag;
	private uint internalTimer1, internalTimer1Frozen;
	private int internalTimer2, internalTimer2Frozen;
	private uint random;

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
			box.PreviousID = -1;
			box.RoomLoader = GetComponent<RoomLoader>();
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
		if (ProcessMemory != null)
		{
			ProcessMemory.Close();
		}
	}

	bool AreActorsInitialized()
	{
		for (int i = 0 ; i < Boxes.Length ; i++)
		{
			int k = GetActorMemoryAddress(i);
			int id = memory.ReadShort(k);
			if (id != 0)
			{
				return true;
			}
		}

		return false;
	}

	public void RefreshMemory()
	{
		if (ProcessMemory != null && ProcessMemory.Read(memory, 0, memory.Length) == 0)
		{
			ProcessMemory.Close();
			ProcessMemory = null;
		}
	}

	public void UpdateAllActors()
	{
		if (gameConfig == null)
		{
			return;
		}

		Player = null;
		bool initialized = AreActorsInitialized();

		//read actors info
		for (int i = 0 ; i < Boxes.Length ; i++) //up to 50 actors max
		{
			int k = GetActorMemoryAddress(i);
			int id = memory.ReadShort(k + 0);

			if (id != -1 && initialized)
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
				box.LifeMode = memory.ReadShort(k + 50);
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

				box.TrackMode = memory.ReadShort(k + gameConfig.TrackModeOffset);
				box.TrackNumber = memory.ReadShort(k + 84);
				box.TrackPosition = memory.ReadShort(k + 88);

				int bodyAddress, animAddress;
				if (bodyIdToMemoryAddress.TryGetValue(box.Body, out bodyAddress) &&
					animIdToMemoryAddress.TryGetValue(box.Anim, out animAddress) &&
					animAddress < memory.Length &&
					bodyAddress < memory.Length)
				{
					int bonesInAnim = memory.ReadShort(animAddress + 2);
					int keyframeAddress = animAddress + box.Keyframe * (bonesInAnim * 8 + 8);
					box.KeyFrameTime = memory.ReadUnsignedShort(bodyAddress + 20);
					box.KeyFrameLength = memory.ReadShort(keyframeAddress + 4);
				}

				if (GameVersion == GameVersion.AITD1
					|| GameVersion == GameVersion.AITD1_FLOPPY
					|| GameVersion == GameVersion.AITD1_DEMO)
				{
					box.Mod = memory.ReadVector(k + 90);
				}
				else
				{
					box.Mod = Vector3Int.Zero;
				}

				box.OldAngle = memory.ReadShort(k + 106);
				box.NewAngle = memory.ReadShort(k + 108);
				box.RotateParam = memory.ReadShort(k + 110);
				box.RotateTime = memory.ReadShort(k + 112);

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

		//search current camera target (fallback if player not found)
		int cameraTargetID = -1;
		if (GameVersion == GameVersion.AITD1)
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
				Vector3Int roomPosition;
				if (GetComponent<RoomLoader>().TryGetRoomPosition(box.Floor, box.Room, out roomPosition))
				{
					//local to global position
					Vector3 boxPosition = (Vector3)(box.BoundingUpper + box.BoundingLower) / 2000.0f + (Vector3)roomPosition / 1000.0f;
					boxPosition = new Vector3(boxPosition.x, -boxPosition.y, boxPosition.z);

					if (box.transform.position != boxPosition)
					{
						Vector3 offset = 1000.0f * (box.transform.position - boxPosition);
						float distance = new Vector3(Mathf.Round(offset.x), 0.0f, Mathf.Round(offset.z)).magnitude;
						if (box.ID == box.PreviousID)
						{
							box.LastOffset = Mathf.RoundToInt(distance);
							box.LastDistance += distance;
						}

						box.transform.position = boxPosition;
					}

					//make actors appears slightly bigger than they are to be not covered by colliders
					Vector3 delta = Vector3.one;
					box.transform.localScale = (box.BoundingSize + delta) / 1000.0f;

					//make sure very small actors are visible
					box.transform.localScale = Vector3.Max(box.transform.localScale, Vector3.one * 0.1f);

					if (GameVersion == GameVersion.AITD1)
					{
						UpdateHotPointBox(box, roomPosition);
						UpdateTrackBox(box, roomPosition);
					}

					//camera target
					if (box.ID == cameraTargetID)
					{
						//check if player has moved
						if (box.transform.position != lastPlayerPosition)
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
						//player is white
						box.Color = new Color32(255, 255, 255, 255);
						Player = box;
					}
					else
					{
						if (box.Slot == 0 && SpeedRunMode)
						{
							box.Color = new Color32(255, 255, 255, 255);
						}
						else
						{
							//other actors are green
							box.Color = new Color32(0, 128, 0, 255);
						}
					}

					//arrow
					if ((box.TrackMode != 0 || box == Player) && box.transform.localScale.magnitude > 0.01f && Actors.activeSelf)
					{
						if (box.Arrow == null)
						{
							box.Arrow = Instantiate(arrowPrefab);
						}

						box.Arrow.transform.position = box.transform.position + new Vector3(0.0f, box.transform.localScale.y / 2.0f + 0.001f, 0.0f);

						//face camera
						float angle = box.Angles.y * 360.0f / 1024.0f;
						box.Arrow.transform.rotation = Quaternion.AngleAxis(90.0f, -Vector3.left);
						box.Arrow.transform.rotation *= Quaternion.AngleAxis((angle + 180.0f) % 360.0f, Vector3.forward);

						float minBoxScale = Mathf.Min(box.transform.localScale.x, box.transform.localScale.z);
						box.Arrow.transform.localScale = new Vector3(
							minBoxScale * 0.9f,
							minBoxScale * 0.9f,
							1.0f);

						box.Arrow.AlwaysOnTop = Camera.main.orthographic;
					}
					else if (box.Arrow != null)
					{
						Destroy(box.Arrow.gameObject);
						box.Arrow = null;
					}

					if (GameVersion == GameVersion.AITD1)
					{
						UpdateWorldPosBox(box, roomPosition, isPlayer);
					}

					box.AlwaysOnTop = Camera.main.orthographic;
					box.PreviousID = box.ID;
				}
				else
				{
					RemoveActor(box.Slot);
				}
			}
		}

		if (GameVersion == GameVersion.AITD1)
		{
			CurrentCamera = memory.ReadShort(entryPoint + 0x24056);
			CurrentCameraFloor = memory.ReadShort(entryPoint + 0x24058);
			CurrentCameraRoom = memory.ReadShort(entryPoint + 0x2405A);
		}

		if (ShowAITD1Vars)
		{
			allowInventory = memory.ReadShort(entryPoint + 0x19B6E) == 1;
			redrawFlag = memory.ReadShort(entryPoint + 0x19BC6);
			inHand = memory.ReadShort(entryPoint + 0x24054);

			//set by AITD when long running code is started (eg: loading ressource)
			saveTimerFlag = memory[entryPoint + 0x1B0FC] == 1;

			internalTimer1 = memory.ReadUnsignedInt(entryPoint + 0x19D12);
			internalTimer2 = memory.ReadUnsignedShort(entryPoint + 0x242E0);

			internalTimer1Frozen = memory.ReadUnsignedInt(entryPoint + 0x1B0F8);
			internalTimer2Frozen = memory.ReadUnsignedShort(entryPoint + 0x1B0F6);

			random = memory.ReadUnsignedInt(entryPoint + 0x214C0);
		}
	}

	void UpdateWorldPosBox(Box box, Vector3Int roomPosition, bool isPlayer)
	{
		Vector3Int currentRoomPos;
		if (GetComponent<RoomLoader>().TryGetRoomPosition(CurrentCameraFloor, CurrentCameraRoom, out currentRoomPos))
		{
			Vector3 finalPos = (Vector3)(box.WorldPosition + box.Mod + currentRoomPos) / 1000.0f;
			float height = -(box.BoundingUpper.y + box.BoundingLower.y) / 2000.0f;
			finalPos = new Vector3(finalPos.x, height + 0.001f, finalPos.z);

			Vector3Int boundingPos = box.WorldPosition + box.Mod + currentRoomPos - roomPosition;
			bool visible = isPlayer && (boundingPos.x != box.BoundingPos.x || boundingPos.z != box.BoundingPos.z);

			box.BoxWorldPos = CreateChildBox(box.BoxWorldPos,
				finalPos, box.transform.localScale, Quaternion.identity,
				"WorldPos", new Color32(255, 0, 0, 128),
				visible);
		}
	}

	void UpdateHotPointBox(Box box, Vector3Int roomPosition)
	{
		Vector3 finalPos = (Vector3)(box.LocalPosition + box.Mod + box.HotPosition + roomPosition) / 1000.0f;
		finalPos = new Vector3(finalPos.x, -finalPos.y, finalPos.z);

		box.BoxHotPoint = CreateChildBox(box.BoxHotPoint,
			finalPos, Vector3.one * (box.HotBoxSize / 500.0f), Quaternion.identity,
			"HotPoint", new Color32(255, 0, 0, 255),
			box.ActionType == 2);
	}

	void UpdateTrackBox(Box box, Vector3Int roomPosition)
	{
		bool visible = false;
		Vector3 position = Vector3.zero;
		Vector3 scale = Vector3.one;
		Quaternion rotation = Quaternion.identity;

		int trackAddress;
		if (box.TrackMode == 3 && box.TrackNumber != -1 && trackIdToMemoryAddress.TryGetValue(box.TrackNumber, out trackAddress))
		{
			int trackPos = trackAddress + box.TrackPosition * 2;
			int instruction = memory.ReadShort(trackPos);
			Vector3Int targetPos = Vector3Int.Zero;

			switch (instruction)
			{
				case 1: //goto pos
					int room = memory.ReadShort(trackPos + 2);
					if (GetComponent<RoomLoader>().TryGetRoomPosition(box.Floor, room, out roomPosition))
					{
						float size = Mathf.Sqrt(0.8f * 0.8f / 2.0f);
						scale = new Vector3(size, size, size);
						rotation = Quaternion.Euler(0.0f, 45.0f, 0.0f);
						targetPos = new Vector3Int(memory.ReadShort(trackPos + 4), 0, memory.ReadShort(trackPos + 6));
						visible = true;
					}
					break;

				case 9: //rotate X
					float rotate = memory.ReadShort(trackPos + 2) * (360.0f / 1024.0f);
					targetPos = box.LocalPosition + box.Mod + new Vector3Int(Quaternion.Euler(0.0f, -rotate, 0.0f)
						* new Vector3(0.0f, 0.0f, -500.0f));
					scale = new Vector3(0.2f, 0.2f, 0.2f);
					visible = true;
					break;

				case 17: //stairs X
				case 18: //stairs Y
					targetPos = memory.ReadVector(trackPos + 2);
					scale = new Vector3(0.2f, 0.2f, 0.2f);
					visible = true;
					break;
			}

			position = (Vector3)(targetPos + roomPosition) / 1000.0f;
			position = new Vector3(position.x, -position.y, position.z);
		}

		box.BoxTrack = CreateChildBox(box.BoxTrack,
						position, scale, rotation,
						"BoxTrack", new Color32(255, 255, 0, 128),
						visible);
	}

	public uint Timer1
	{
		get
		{
			return saveTimerFlag ? internalTimer1Frozen : internalTimer1;
		}
	}

	public int Timer2
	{
		get
		{
			return saveTimerFlag ? internalTimer2Frozen : internalTimer2;
		}
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
		if (cacheAddress > 0 && cacheAddress < memory.Length)
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

	Box CreateChildBox(Box box, Vector3 position, Vector3 scale, Quaternion rotation, string name, Color32 color, bool visible)
	{
		if (visible)
		{
			if (box == null)
			{
				box = Instantiate(BoxPrefab);
				box.name = name;
				box.Color = color;
				Destroy(box.gameObject.GetComponent<BoxCollider>());
			}

			box.transform.position = position;
			box.transform.localScale = scale;
			box.transform.localRotation = rotation;
			box.AlwaysOnTop = Camera.main.orthographic;
		}
		else if (box != null)
		{
			Destroy(box.gameObject);
			box = null;
		}

		return box;
	}

	public void UpdateRightText()
	{
		RightText.Clear();
		if (Player != null)
		{
			float angle = Player.Angles.y * 360.0f / 1024.0f;
			float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

			RightText.Append("Position", Player.LocalPosition + Player.Mod);
			RightText.Append("Angle", "{0:N1} {1:N1}", angle, sideAngle);
		}

		if (ShowAITD1Vars || ShowAdditionalInfo)
		{
			if (Player != null) RightText.AppendLine();

			if (ShowAITD1Vars)
			{
				int calculatedFps = previousFrames.Sum(x => x.Key);
				TimeSpan totalDelayTS = TimeSpan.FromSeconds(totalDelay.Elapsed);
				uint timer1Delay = internalTimer1 - internalTimer1Frozen;
				int timer2Delay = internalTimer2 - internalTimer2Frozen;

				RightText.Append("Timer 1", !saveTimerFlag ? "{0}.{1:D2}" : "{0}.{1:D2} {2:D2}.{3:D2}", TimeSpan.FromSeconds(Timer1 / 60), Timer1 % 60, timer1Delay / 60 % 60, timer1Delay % 60);
				RightText.Append("Timer 2", !saveTimerFlag ? "{0}.{1:D2}" : "{0}.{1:D2} {2:D2}.{3:D2}", TimeSpan.FromSeconds(Timer2 / 60), Timer2 % 60, timer2Delay / 60 % 60, timer2Delay % 60);
				RightText.Append("FPS/Frame/Time", "{0}; {1}; {2} ms", calculatedFps, frameCounter, Mathf.RoundToInt(frameTimeElapsed * 1000));
				RightText.Append("Total delay/Delay", "{0:D2}:{1:D2}.{2:D3}; {3} ms", totalDelayTS.Minutes, totalDelayTS.Seconds, totalDelayTS.Milliseconds, Mathf.FloorToInt(lastDelay * 1000));
			}

			Vector3Int mousePosition = GetComponent<RoomLoader>().GetMousePosition(linkroom, linkfloor);
			RightText.Append("Cursor position", "{0} {1}", Mathf.Clamp(mousePosition.x, -32768, 32767), Mathf.Clamp(mousePosition.z, -32768, 32767));
			if (Player != null) RightText.Append("Last offset/dist", "{0}; {1}", Player.LastOffset, Mathf.RoundToInt(Player.LastDistance));

			if (ShowAITD1Vars)
			{
				RightText.Append("Allow inventory", "{0}; {1}", allowInventory ? "Yes" : "No", redrawFlag);
				RightText.Append("In hand", inHand);
				RightText.Append("Random", "{0:X8}", random);
				AppendRandomInfo();
			}
		}

		RightText.UpdateText();
	}

	void AppendRandomInfo()
	{
		rand.Seed = random;
		for (int i = 0 ; i < 3 ; i++)
		{
			int value;
			int count = 0;
			do
			{
				value = rand.Next(300);
				count++;
			}
			while (value > 2);

			RightText.Append(i == 0 ? "Next noise" : string.Empty, "{0:D2}.{1:D2}; {2} ", count / 60 % 60, count % 60, value);
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			totalDelay.Reset();
		}
		if (Input.GetKeyDown(KeyCode.W))
		{
			foreach (Box box in Boxes)
			{
				if (box != null)
				{
					box.LastDistance = 0.0f;
				}
			}
		}
		if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && GameVersion == GameVersion.AITD1 && ProcessMemory != null)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				//internal timer 1
				byte[] buffer = new byte[4];
				buffer.Write(Timer1 - 60 * 5, 0);  //back 5 frames
				ProcessMemory.Write(buffer, entryPoint + 0x19D12, buffer.Length);
			}
			if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				//internal timer 2
				byte[] buffer = new byte[2];
				buffer.Write((ushort)(Timer2 - 60 * 5), 0);  //back 5 frames
				ProcessMemory.Write(buffer, entryPoint + 0x242E0, buffer.Length);
			}
		}
	}

	public void RefreshCacheEntries()
	{
		if (ShowAITD1Vars)
		{
			RefreshCacheEntries(animIdToMemoryAddress, 0x218D3);
			RefreshCacheEntries(bodyIdToMemoryAddress, 0x218D7);
			RefreshCacheEntries(trackIdToMemoryAddress, 0x218C7);
		}
	}

	public void CalculateFPS()
	{
		if (ProcessMemory == null)
		{
			return;
		}

		if (ShowAITD1Vars)
		{
			//fps
			int fps = memory.ReadShort(entryPoint + 0x19D18);

			//frames counter (reset to zero every second by AITD)
			int frames = memory.ReadShort(entryPoint + 0x2117C);

			//frame time
			if (frames != oldFrames)
			{
				oldFrames = frames;
				if (frames != 0)
				{
					frameTimeElapsed = frameTime.Elapsed;
					frameTime.Restart();
				}
			}

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

			float time = Time.time;
			if (diff > 0)
			{
				previousFrames.Enqueue(new KeyValuePair<int, float>(diff, time));
			}

			//remove any frame info older than one second
			while (previousFrames.Count > 0 &&
				previousFrames.Peek().Value < (time - 1.0f))
			{
				previousFrames.Dequeue();
			}
		}
	}

	public void CheckDelay()
	{
		if (ProcessMemory == null)
		{
			totalDelay.Stop();
			delayCounter.Stop();
			return;
		}

		if (ShowAITD1Vars)
		{
			if (delayCounter.Elapsed >= 0.1f) //100ms
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
		}
	}

	void FixBoundingWrap(ref int a, ref int b)
	{
		if (a > b)
		{
			if (a < -b)
			{
				b += 65536;
			}
			else
			{
				a -= 65536;
			}
		}
	}

	#region SearchDOSBox

	int SearchDOSBoxProcess()
	{
		int? processId = Process.GetProcesses()
				.Where(x => GetProcessName(x).StartsWith("DOSBOX", StringComparison.InvariantCultureIgnoreCase))
				.Select(x => (int?)x.Id)
				.FirstOrDefault();

		if (processId.HasValue)
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

	bool TryGetMemoryReader(out ProcessMemory reader)
	{
		int processId = SearchDOSBoxProcess();
		if (processId != -1)
		{
			reader = new ProcessMemory(processId);
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

	bool FindActorsAddressAITD(GameVersion gameVersion)
	{
		ProcessMemory.Read(memory, 0, memory.Length);

		if (!TryGetExeEntryPoint(out entryPoint))
		{
			return false;
		}

		switch (gameVersion)
		{
			case GameVersion.AITD1:
				if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("CD Not Found")) == -1)
				{
					if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("USA.PAK")) != -1)
					{
						gameVersion = GameVersion.AITD1_DEMO;
					}
					else
					{
						gameVersion = GameVersion.AITD1_FLOPPY;
					}
				}
				break;

			case GameVersion.AITD2:
				if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("Not a CD-ROM drive")) == -1)
				{
					if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("BATL.PAK")) == -1)
					{
						gameVersion = GameVersion.AITD2_DEMO;
					}
					else
					{
						gameVersion = GameVersion.AITD2_FLOPPY;
					}
				}
				break;

			case GameVersion.AITD3:
				if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("usa.pak")) != -1)
				{
					gameVersion = GameVersion.AITD3_DEMO;
				}
				break;
		}

		GameVersion = gameVersion;
		gameConfig = gameConfigs[gameVersion];
		return true;
	}

	bool FindActorsAddressTimeGate(GameVersion gameVersion)
	{
		//scan range: 0x110000 (extended memory) - 0x300000 (3MB)
		byte[] pattern = Encoding.ASCII.GetBytes("HARD_DEC");
		int dataSegment = ProcessMemory.SearchForBytePattern(0x110000, 0x1F0000, buffer => Utils.IndexOf(buffer, pattern, 28, 16));

		if (dataSegment != -1)
		{
			ProcessMemory.Read(memory, dataSegment, memory.Length);
			if (Utils.IndexOf(memory, Encoding.ASCII.GetBytes("Time Gate not found")) == -1)
			{
				gameVersion = GameVersion.TIMEGATE_DEMO;
			}

			GameVersion = gameVersion;
			gameConfig = gameConfigs[gameVersion];
			ProcessMemory.Read(memory, dataSegment + gameConfig.ActorsPointer, 4);
			var result = memory.ReadUnsignedInt(0); //read pointer value
			if (result != 0)
			{
				ProcessMemory.BaseAddress += result;
				entryPoint = 0;
				return true;
			}
		}

		return false;
	}

	#endregion

	#region Room loader

	public bool LinkToDosBOX(int floor, int room, GameVersion gameVersion)
	{
		if (!TryGetMemoryReader(out ProcessMemory))
		{
			return false;
		}

		if (!FindActorsAddress(gameVersion))
		{
			ProcessMemory.Close();
			ProcessMemory = null;
			return false;
		}

		//force reload
		linkfloor = floor;
		linkroom = room;

		Player = null;
		lastValidPlayerIndex = -1;

		return true;
	}

	public bool FindActorsAddress(GameVersion gameVersion)
	{
		if (gameVersion == GameVersion.TIMEGATE)
		{
			return FindActorsAddressTimeGate(gameVersion);
		}

		return FindActorsAddressAITD(gameVersion);
	}

	public void ResetCamera(int floor, int room)
	{
		lastPlayerPosition = Vector3.zero;
		linkfloor = floor;
		linkroom = room;
	}

	public int GetActorMemoryAddress(int index)
	{
		return gameConfig.ActorsAddress + entryPoint + index * gameConfig.ActorStructSize;
	}

	public int GetActorSize()
	{
		return gameConfig.ActorStructSize;
	}

	public int GetObjectMemoryAddress(int index)
	{
		int objectAddress = memory.ReadFarPointer(entryPoint + 0x2400E);
		return objectAddress + index * 52;
	}

	public Box RefreshBoxUsingID(Box box, int boxId)
	{
		//make sure ID still match
		if (box != null && box.ID != boxId)
		{
			box = null;
		}

		if (box == null && boxId != -1)
		{
			//if actor is no more available (eg : after room switch) search for it
			foreach (Box b in Boxes)
			{
				if (b != null && b.ID == boxId)
				{
					box = b;
					break;
				}
			}
		}

		return box;
	}

	#endregion
}

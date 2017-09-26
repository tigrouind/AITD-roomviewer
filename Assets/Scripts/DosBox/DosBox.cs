using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

public class DosBox : MonoBehaviour
{
	public GUIText RightText;
	public GameObject Actors;
	public Arrow Arrow;
	public Box BoxPrefab;
	public uint InternalTimer;
	public bool ShowAdditionalInfo;
	public ProcessMemoryReader ProcessReader;

	//initial player position
	private int dosBoxPattern;
	private byte[][][] PlayerInitialPosition = new byte[][][]
	{
		new byte[][] { new byte[] { 0x9F, 0x0C, 0x00, 0x00, 0xF4, 0xF9, 0x9F, 0x0C, 0x00, 0x00, 0xF4, 0xF9 } }, //AITD1
		new byte[][] { new byte[] { 0x43, 0x01, 0x00, 0x00, 0xD0, 0xE4, 0x43, 0x01, 0x00, 0x00, 0xD0, 0xE4 } }, //AIID2
		new byte[][]
		{
			new byte[] { 0x3F, 0x03, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x03, 0x00, 0x00, 0x00, 0x00 }, //AITD3
			new byte[] { 0x27, 0x03, 0x00, 0x00, 0x00, 0x00, 0x27, 0x03, 0x00, 0x00, 0x00, 0x00 }  //AITD3 (after restart)
		}
	};

	private int[] MemoryOffsets = new [] { -188, -28, -28 };
	//offset to apply to get beginning of actors array
	private int[] ActorStructSize = new [] { 160, 180, 182 };
	//size of one actor
	private int[] TrackModeOffsets = new [] { 82, 90, 90 };

	private Vector3 lastPlayerPosition;
	private int lastValidPlayerIndex;
	private int linkfloor = 0;
	private int linkroom = 0;
	private long memoryAddress;
	private StringBuilder playerInfo;
	private byte[] memory;

	//fps
	private int oldFramesCount;
	private Queue<int> previousFramesCount = new Queue<int>();
	private int calculatedFps;

	private int delayFpsCounter;
	private int lastDelayFpsCounter;
	private StringBuilder fpsInfo;
	private bool allowInventory;
	private int inHand;

	private Vector3 lastPlayerPositionFixedUpdate;
	private int lastPlayerOffset;
	private int lastPlayerMod;

	public void Start()
	{
		//game has maximum 50 actors
		for (int i = 0; i < 50; i++)
		{
			Box box = Instantiate(BoxPrefab);
			box.transform.parent = Actors.transform;
			box.name = "Actor";
		}
	}

	public void Update()
	{
		GameObject player = null;

		if (ProcessReader != null)
		{
			if (ProcessReader.Read(memory, memoryAddress, memory.Length) > 0)
			{
				//read actors info
				int i = 0;
				foreach (Box box in Actors.GetComponentsInChildren<Box>(true))
				{
					int k = i * ActorStructSize[dosBoxPattern];
					int floorNumber = Utils.ReadShort(memory, k + 46);
					int roomNumber = Utils.ReadShort(memory, k + 48);

					int objectid = Utils.ReadShort(memory, k + 0);
					int body = Utils.ReadShort(memory, k + 2);

					int trackModeOffset = TrackModeOffsets[dosBoxPattern];
					int trackMode = Utils.ReadShort(memory, k + trackModeOffset);
					bool isActive = objectid != -1;

					if (isActive)
					{
						//player
						if (trackMode == 1 || objectid == lastValidPlayerIndex)
						{
							//update player index
							lastValidPlayerIndex = objectid;

							//automatically switch room and floor (has to be done before setting other actors positions)
							if (linkfloor != floorNumber || linkroom != roomNumber)
							{
								linkfloor = floorNumber;
								linkroom = roomNumber;

								GetComponent<RoomLoader>().RefreshRooms(linkfloor, linkroom);
							}
						}

						Transform roomObject = GetComponent<RoomLoader>().GetRoom(floorNumber, roomNumber);
						if (roomObject != null)
						{
							//local position
							int boundingX1 = Utils.ReadShort(memory, k + 8);
							int boundingX2 = Utils.ReadShort(memory, k + 10);
							int boundingY1 = Utils.ReadShort(memory, k + 12);
							int boundingY2 = Utils.ReadShort(memory, k + 14);
							int boundingZ1 = Utils.ReadShort(memory, k + 16);
							int boundingZ2 = Utils.ReadShort(memory, k + 18);

							FixBoundingWrap(ref boundingX1, ref boundingX2);
							FixBoundingWrap(ref boundingY1, ref boundingY2);
							FixBoundingWrap(ref boundingZ1, ref boundingZ2);

							box.BoundingLower = new Vector3(boundingX1, boundingY1, boundingZ1);
							box.BoundingUpper = new Vector3(boundingX2, boundingY2, boundingZ2);

							//local to global position
							Vector3 boxPosition = box.BoundingPos / 1000.0f + roomObject.localPosition;
							box.transform.position = new Vector3(boxPosition.x, -boxPosition.y, boxPosition.z);

							//make actors appears slightly bigger than they are to be not covered by colliders
							Vector3 delta = Vector3.one;
							box.transform.localScale = (box.BoundingSize + delta) / 1000.0f;

							//make sure very small actors are visible
							box.transform.localScale = new Vector3(
								Mathf.Max(box.transform.localScale.x, 0.1f),
								Mathf.Max(box.transform.localScale.y, 0.1f),
								Mathf.Max(box.transform.localScale.z, 0.1f));

							box.ID = objectid;
							box.Body = body;
							box.Room = roomNumber;
							box.Floor = floorNumber;
							box.Flags = Utils.ReadShort(memory, k + 4);
							box.ColFlags = Utils.ReadShort(memory, k + 6);
							box.LifeMode =Utils. ReadShort(memory, k + 50);
							box.Life = Utils.ReadShort(memory, k + 52);
							box.Chrono = Utils.ReadUnsignedInt(memory, k + 54);
							box.RoomChrono = Utils.ReadUnsignedInt(memory, k + 58);
							box.Anim = Utils.ReadShort(memory, k + 62);
							box.Keyframe = Utils.ReadShort(memory, k + 74);
							box.TotalFrames = Utils.ReadShort(memory, k + 76);
							box.TrackNumber = Utils.ReadShort(memory, k + 84);
							box.PositionInTrack = Utils.ReadShort(memory, k + 88);
							box.TrackMode = trackMode;
							box.Speed = Utils.ReadShort(memory, k + 116);
							box.Slot = i;

							box.Angles.x = Utils.ReadShort(memory, k + 40);
							box.Angles.y = Utils.ReadShort(memory, k + 42);
							box.Angles.z = Utils.ReadShort(memory, k + 44);

							box.Mod.x = Utils.ReadShort(memory, k + 90);
							box.Mod.y = Utils.ReadShort(memory, k + 92);
							box.Mod.z = Utils.ReadShort(memory, k + 94);

							box.LocalPosition.x = Utils.ReadShort(memory, k + 28) + box.Mod.x;
							box.LocalPosition.y = Utils.ReadShort(memory, k + 30) + box.Mod.y;
							box.LocalPosition.z = Utils.ReadShort(memory, k + 32) + box.Mod.z;

							box.WorldPosition.x = Utils.ReadShort(memory, k + 34) + box.Mod.x;
							box.WorldPosition.y = Utils.ReadShort(memory, k + 36) + box.Mod.y;
							box.WorldPosition.z = Utils.ReadShort(memory, k + 38) + box.Mod.z;

							box.ShowAdditionalInfo = ShowAdditionalInfo;

							//player
							if (objectid == lastValidPlayerIndex)
							{
								float angle = box.Angles.y * 360.0f / 1024.0f;
								float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

								playerInfo = new StringBuilder();
								playerInfo.AppendFormat("Position: {0} {1} {2}\n", box.LocalPosition.x, box.LocalPosition.y, box.LocalPosition.z);
								playerInfo.AppendFormat("Angle: {0:N1} {1:N1}", angle, sideAngle);

								//check if player has moved
								if (box.transform.position != lastPlayerPosition)
								{
									//center camera to player position
									GetComponent<RoomLoader>().CenterCamera(new Vector2(box.transform.position.x, box.transform.position.z));
									lastPlayerPosition = box.transform.position;
								}

								//follow player
								Arrow.transform.position = box.transform.position + new Vector3(0.0f, box.transform.localScale.y / 2.0f + 0.001f, 0.0f);
								//face camera
								Arrow.transform.rotation = Quaternion.AngleAxis(90.0f, -Vector3.left);
								Arrow.transform.rotation *= Quaternion.AngleAxis((angle + 180.0f) % 360.0f, Vector3.forward);

								float minBoxScale = Mathf.Min(box.transform.localScale.x, box.transform.localScale.z);
								Arrow.transform.localScale = new Vector3(
									minBoxScale * 0.9f,
									minBoxScale * 0.9f,
									1.0f);

								//player is white
								box.Color = new Color32(255, 255, 255, 255);
								box.AlwaysOnTop = Camera.main.orthographic;
								Arrow.AlwaysOnTop = Camera.main.orthographic;

								player = box.gameObject;
							}
							else
							{
								//other actors are green
								box.Color = new Color32(0, 128, 0, 255);
							}

							box.gameObject.SetActive(true);
						}
						else
						{
							box.gameObject.SetActive(false);
						}
					}
					else
					{
						box.gameObject.SetActive(false);
					}

					i++;
				}

				if (ShowAdditionalInfo)
				{
					Vector3 mousePosition = GetMousePosition(linkroom, linkfloor);

					fpsInfo = new StringBuilder();
					fpsInfo.AppendFormat("Timer: {0}\n", TimeSpan.FromSeconds(InternalTimer / 60));
					fpsInfo.AppendFormat("Fps: {0}\n", calculatedFps);
					fpsInfo.AppendFormat("Delay: {0} ms\n", lastDelayFpsCounter * 1000 / 200);
					fpsInfo.AppendFormat("Allow inventory: {0}\n", allowInventory ? "Yes" : "No");
					fpsInfo.AppendFormat("Cursor position: {0} {1} {2}\n", (int)(mousePosition.x), (int)(mousePosition.y), (int)(mousePosition.z));
					fpsInfo.AppendFormat("Last player offset: {0}\n", lastPlayerOffset);
					fpsInfo.AppendFormat("Last player mod: {0}\n", lastPlayerMod);
					fpsInfo.AppendFormat("In hand: {0}\n", inHand);
				}
				else
				{
					fpsInfo = null;
				}

				if (playerInfo != null)
					RightText.text = playerInfo.ToString();
				if (fpsInfo != null)
					RightText.text += "\n\n" + fpsInfo.ToString();
			}
			else
			{
				//unlink DOSBOX
				GetComponent<RoomLoader>().ProcessKey(KeyCode.L);
			}
		}

		if (ProcessReader != null)
		{
			if (ShowAdditionalInfo)
			{
				//inventory
				ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 - 0x1A4, 2);
				allowInventory = Utils.ReadShort(memory, 0) == 1;

				//inhand
				ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0xA33C, 2);
				inHand = Utils.ReadShort(memory, 0);
			}
		}

		//arrow is only active if actors are active and player is active
		Arrow.gameObject.SetActive(Actors.activeSelf
			&& player != null
			&& player.activeSelf
			&& player.transform.localScale.magnitude > 0.01f);
	}

	void FixedUpdate()
	{
		if (ProcessReader != null && ShowAdditionalInfo)
		{
			//internal timer
			ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6, 4);
			InternalTimer = Utils.ReadUnsignedInt(memory, 0);

			//fps
			ProcessReader.Read(memory, memoryAddress - 0x83B6, 2);
			int fps = Utils.ReadShort(memory, 0);

			//frames
			ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0x7464, 2);
			int frames = Utils.ReadShort(memory, 0);

			//check how much frames elapsed since last time
			int diff;
			if (frames >= oldFramesCount)
				diff = frames - oldFramesCount;
			else
				diff = (fps - oldFramesCount) + frames;
			oldFramesCount = frames;

			//check for large delays
			if (diff == 0)
			{
				delayFpsCounter++;
				if (delayFpsCounter > 100 / (1000 / 200)) // 20 frames at 200FPS = 100ms
				{
					lastDelayFpsCounter = delayFpsCounter;
				}
			}
			else
			{
				delayFpsCounter = 0;
			}

			previousFramesCount.Enqueue(diff);
			while (previousFramesCount.Count > 200)
				previousFramesCount.Dequeue();

			calculatedFps = previousFramesCount.Sum();

			//playerspeed + frame change
			if (ProcessReader.Read(memory, memoryAddress, memory.Length) > 0)
			{
				int i = 0;
				foreach (Box box in Actors.GetComponentsInChildren<Box>(true))
				{
					int k = i * ActorStructSize[dosBoxPattern];
					int objectid = Utils.ReadShort(memory, k + 0);

					//playerspeed
					if (objectid == lastValidPlayerIndex)
					{
						int boundingX1 = Utils.ReadShort(memory, k + 8);
						int boundingX2 = Utils.ReadShort(memory, k + 10);
						int boundingZ1 = Utils.ReadShort(memory, k + 16);
						int boundingZ2 = Utils.ReadShort(memory, k + 18);

						Vector3 position = new Vector3((boundingX1 + boundingX2) / 2.0f, 0.0f, (boundingZ1 + boundingZ2) / 2.0f);
						if (position != lastPlayerPositionFixedUpdate)
						{
							lastPlayerOffset = Mathf.FloorToInt((position - lastPlayerPositionFixedUpdate).magnitude);
							lastPlayerPositionFixedUpdate = position;
						}

						int modx = Utils.ReadShort(memory, k + 90);
						int mody = Utils.ReadShort(memory, k + 92);
						int modz = Utils.ReadShort(memory, k + 94);
						Vector3 mod = new Vector3(modx, mody, modz);

						if(mod != Vector3.zero)
						{
							lastPlayerMod = Mathf.FloorToInt(mod.magnitude);
						}
					}

					//detect frame change
					int anim = box.Anim = Utils.ReadShort(memory, k + 62);
					int keyframe = Utils.ReadShort(memory, k + 74);

					if(anim != box.Anim || keyframe != box.Keyframe)
					{
						box.Anim = anim;
						box.Keyframe = keyframe;
						box.LastKeyFrameChange = InternalTimer;
					}

					i++;
				}
			}
		}
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

	private void FixBoundingWrap(ref int a, ref int b)
	{
		if(a > b)
		{
			if((Int16.MaxValue - a) > (b - Int16.MinValue))
			{
				b += 65536;
			}
			else
			{
				a -= 65536;
			}
		}
	}

	#region Room loader

	public bool LinkToDosBOX(int floor, int room)
	{
		int[] processIds = Process.GetProcesses()
		.Where(x =>
			{
				string name;
				try
				{
					name = x.ProcessName;
				}
				catch
				{
					name = string.Empty;
				}
				return name.StartsWith("DOSBOX", StringComparison.InvariantCultureIgnoreCase);
			})
		.Select(x => x.Id)
		.ToArray();

		if (!processIds.Any())
		{
			RightText.text = "Cannot find DOSBOX process";
			return false;
		}

		//search player position in DOSBOX processes
		int patternIndex = GetComponent<RoomLoader>().DetectGame() - 1;
		foreach (int processId in processIds)
		{
			ProcessMemoryReader reader = new ProcessMemoryReader(processId);
			foreach (var pattern in PlayerInitialPosition[patternIndex])
			{
				long address = reader.SearchForBytePattern(pattern);
				if (address != -1)
				{
					//force reload
					linkfloor = floor;
					linkroom = room;

					memoryAddress = address + MemoryOffsets[patternIndex];
					ProcessReader = reader;
					memory = new byte[ActorStructSize[patternIndex] * 50];
					dosBoxPattern = patternIndex;

					//vars
					if (patternIndex == 0) //AITD1 only
					{
						GetComponent<Vars>().SearchForPatterns(reader);
					}
					return true;
				}
			}

			reader.Close();
		}

		RightText.text = "Cannot find player data in DOSBOX process memory.";
		return false;
	}

	public void UnlinkDosBox()
	{
		ProcessReader.Close();
		ProcessReader = null;
		RightText.text = string.Empty;
	}

	public void ResetCamera(int floor, int room)
	{
		lastPlayerPosition = Vector3.zero;
		linkfloor = floor;
		linkroom = room;
	}

	public long GetActorMemoryAddress(int index)
	{
		return memoryAddress + index * ActorStructSize[dosBoxPattern];
	}

	public Box GetPlayerBox()
	{
		return Actors.GetComponentsInChildren<Box>(true)
			.FirstOrDefault(x => x.ID == lastValidPlayerIndex);
	}

	#endregion
}

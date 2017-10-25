﻿using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using System.Collections;

public class DosBox : MonoBehaviour
{
	public Text RightText;
	public BoxInfo BoxInfo;
	public GameObject Actors;
	public Arrow Arrow;
	public Box BoxPrefab;
	public uint InternalTimer;
	public uint InternalTimerForKeyFrame;
	public bool ShowAdditionalInfo;
	public ProcessMemoryReader ProcessReader;
	public Box Player;

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
	private byte[] memory;

	//fps
	private int oldFramesCount;
	private Queue<int> previousFramesCount = new Queue<int>();
	private Queue<float> previousFrameTime = new Queue<float>();

	private float lastTimeNoDelay;
	private float lastDelay;

	private Vector3 LastPlayerMod;
	private int inHand;
	private bool allowInventory;

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

	public void UpdateAllActors()
	{
		Box player = null;
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

							int modx = Utils.ReadShort(memory, k + 90);
							int mody = Utils.ReadShort(memory, k + 92);
							int modz = Utils.ReadShort(memory, k + 94);

							FixBoundingWrap(ref boundingX1, ref boundingX2);
							FixBoundingWrap(ref boundingY1, ref boundingY2);
							FixBoundingWrap(ref boundingZ1, ref boundingZ2);

							box.BoundingLower = new Vector3(boundingX1, boundingY1, boundingZ1);
							box.BoundingUpper = new Vector3(boundingX2, boundingY2, boundingZ2);

							//local to global position
							Vector3 boxPosition = box.BoundingPos / 1000.0f + roomObject.localPosition;
							boxPosition = new Vector3(boxPosition.x, -boxPosition.y, boxPosition.z);

							if (box.transform.position != boxPosition)
							{
								Vector3 offset = box.transform.position - boxPosition;
								box.LastOffset = Mathf.FloorToInt(1000.0f * new Vector3(offset.x, 0.0f, offset.z).magnitude);
								box.transform.position = boxPosition;
							}

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
							box.TotalFrames = Utils.ReadShort(memory, k + 76);
							box.TrackNumber = Utils.ReadShort(memory, k + 84);
							box.PositionInTrack = Utils.ReadShort(memory, k + 88);
							box.TrackMode = trackMode;
							box.Speed = Utils.ReadShort(memory, k + 116);
							box.Slot = i;

							box.Angles.x = Utils.ReadShort(memory, k + 40);
							box.Angles.y = Utils.ReadShort(memory, k + 42);
							box.Angles.z = Utils.ReadShort(memory, k + 44);

							box.Mod.x = modx;
							box.Mod.y = mody;
							box.Mod.z = modz;

							box.LocalPosition.x = Utils.ReadShort(memory, k + 28) + box.Mod.x;
							box.LocalPosition.y = Utils.ReadShort(memory, k + 30) + box.Mod.y;
							box.LocalPosition.z = Utils.ReadShort(memory, k + 32) + box.Mod.z;

							box.WorldPosition.x = Utils.ReadShort(memory, k + 34) + box.Mod.x;
							box.WorldPosition.y = Utils.ReadShort(memory, k + 36) + box.Mod.y;
							box.WorldPosition.z = Utils.ReadShort(memory, k + 38) + box.Mod.z;

							box.ShowAdditionalInfo = ShowAdditionalInfo;

							int anim = Utils.ReadShort(memory, k + 62);
							int keyframe = Utils.ReadShort(memory, k + 74);
							if(anim != box.Anim || keyframe != box.Keyframe)
							{
								box.Anim = anim;
								box.Keyframe = keyframe;
								box.LastKeyFrameChange = InternalTimer;
							}

							//player
							if (objectid == lastValidPlayerIndex)
							{
								//check if player has moved
								if (box.transform.position != lastPlayerPosition)
								{
									//center camera to player position
									GetComponent<RoomLoader>().CenterCamera(new Vector2(box.transform.position.x, box.transform.position.z));
									lastPlayerPosition = box.transform.position;
								}

								if (box.Mod != Vector3.zero)
								{
									LastPlayerMod = box.Mod;
								}
								//follow player
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
								box.AlwaysOnTop = Camera.main.orthographic;
								Arrow.AlwaysOnTop = Camera.main.orthographic;
								player = box;
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
					//internal timer
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6, 4);
					InternalTimer = Utils.ReadUnsignedInt(memory, 0);

					//frame buffer
					ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0xB668, 4);
					uint pixels = Utils.ReadUnsignedInt(memory, 0);
					if(pixels != 0)
					{
						//hack: if pixel is not black, were are not in main menu/inventory
						InternalTimerForKeyFrame = InternalTimer;
					}

					//inventory
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 - 0x1A4, 2);
					allowInventory = Utils.ReadShort(memory, 0) == 1;

					//inhand
					ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0xA33C, 2);
					inHand = Utils.ReadShort(memory, 0);
				}
			}
			else
			{
				//unlink DOSBOX
				GetComponent<RoomLoader>().ProcessKey(KeyCode.L);
			}
		}

		//arrow is only active if actors are active and player is active
		Arrow.gameObject.SetActive(Actors.activeSelf
			&& player != null
			&& player.gameObject.activeSelf
			&& player.transform.localScale.magnitude > 0.01f);
		Player = player;
	}

	public void UpdateBoxInfo()
	{
		BoxInfo.Clear();
		if (Player != null)
		{
			float angle = Player.Angles.y * 360.0f / 1024.0f;
			float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

			BoxInfo.AppendFormat("Position", "{0} {1} {2}", Player.LocalPosition.x, Player.LocalPosition.y, Player.LocalPosition.z);
			BoxInfo.AppendFormat("Angle", "{0:N1} {1:N1}", angle, sideAngle);
		}

		if (ShowAdditionalInfo)
		{
			if(Player != null) BoxInfo.Append();

			int calculatedFps = previousFramesCount.Sum();
			
			Vector3 mousePosition = GetMousePosition(linkroom, linkfloor);

			BoxInfo.Append("Timer", TimeSpan.FromSeconds(InternalTimer / 60));
			BoxInfo.AppendFormat("FPS/Delay", "{0}; {1} ms", calculatedFps, Mathf.FloorToInt(lastDelay * 1000));
			BoxInfo.AppendFormat("Cursor position", "{0} {1}", Mathf.Clamp((int)(mousePosition.x), -32768, 32767), Mathf.Clamp((int)(mousePosition.z), -32768, 32767));
			if(Player != null) BoxInfo.AppendFormat("Last offset/mod", "{0}; {1}", Player.LastOffset, Mathf.FloorToInt(LastPlayerMod.magnitude));
			BoxInfo.AppendFormat("Allow inventory", allowInventory ? "Yes" : "No");
			BoxInfo.Append("In hand", inHand);
		}

		BoxInfo.UpdateText();
	}

	public void CalculateFPS()
	{
		if (ProcessReader != null && ShowAdditionalInfo)
		{
			//fps
			ProcessReader.Read(memory, memoryAddress - 0x83B6, 2);
			int fps = Utils.ReadShort(memory, 0);

			//frames counter (reset to zero when every second by AITD)
			ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0x7464, 2);
			int frames = Utils.ReadShort(memory, 0);

			//check how much frames elapsed since last time
			int diff;
			if (frames >= oldFramesCount)
				diff = frames - oldFramesCount; //eg: 15 - 20
			else
				diff = fps - oldFramesCount + frames; //special case: eg: 60 - 58 + 3
			oldFramesCount = frames;

			//check for large delays
			float time = Time.time;
			if (diff != 0)
			{
				lastTimeNoDelay = time;
			}
			else
			{
				float delay = time - lastTimeNoDelay;
				if(delay > 0.1f) //100ms
				{
					lastDelay = delay;
				}
			}

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

	void FixBoundingWrap(ref int a, ref int b)
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
					RightText.text = string.Empty;
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
		BoxInfo.Clear();
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
}

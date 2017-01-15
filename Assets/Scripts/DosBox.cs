﻿using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

public class DosBox : MonoBehaviour
{
	public GUIText RightText;
	public GameObject Actors;
	public Arrow Arrow;
	public Box BoxPrefab;
	public uint InternalTimer;
	public MenuStyle Style;
	public bool ShowAdditionalInfo;
	public ProcessMemoryReader ProcessReader;

	public bool warpMenuEnabled;
	public Box warpActor;
	public string warpX, warpY, warpZ;

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
	private float calculatedFps;
	private int delayFpsCounter;
	private int lastDelayFpsCounter;
	private StringBuilder fpsInfo;
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
					int floorNumber = ReadShort(memory[k + 46], memory[k + 47]);
					int roomNumber = ReadShort(memory[k + 48], memory[k + 49]);

					int objectid = ReadShort(memory[k + 0], memory[k + 1]);
					int body = ReadShort(memory[k + 2], memory[k + 3]);

					int trackModeOffset = TrackModeOffsets[dosBoxPattern];
					int trackMode = ReadShort(memory[k + trackModeOffset], memory[k + trackModeOffset + 1]); 
					bool isActive = objectid >= 0;

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
							int boundingX1 = ReadShort(memory[k + 8], memory[k + 9]);
							int boundingX2 = ReadShort(memory[k + 10], memory[k + 11]);
							int boundingY1 = ReadShort(memory[k + 12], memory[k + 13]);
							int boundingY2 = ReadShort(memory[k + 14], memory[k + 15]);
							int boundingZ1 = ReadShort(memory[k + 16], memory[k + 17]);
							int boundingZ2 = ReadShort(memory[k + 18], memory[k + 19]);
							
							FixBoundingWrap(ref boundingX1, ref boundingX2);
							FixBoundingWrap(ref boundingY1, ref boundingY2);
							FixBoundingWrap(ref boundingZ1, ref boundingZ2);

							int boundingx = (boundingX1 + boundingX2) / 2;
							int boundingy = (boundingY1 + boundingY2) / 2;
							int boundingz = (boundingZ1 + boundingZ2) / 2;

							//local to global position
							int boxPositionx = boundingx + (int)(roomObject.localPosition.x * 1000.0f);
							int boxPositiony = boundingy + (int)(roomObject.localPosition.y * 1000.0f);
							int boxPositionz = boundingz + (int)(roomObject.localPosition.z * 1000.0f);

							box.transform.position = new Vector3(boxPositionx, -boxPositiony, boxPositionz) / 1000.0f;

							//make actors appears slightly bigger than they are to be not covered by colliders
							float delta = 1.0f;
							box.transform.localScale = new Vector3(
								boundingX2 - boundingX1 + delta,
								boundingY2 - boundingY1 + delta,
								boundingZ2 - boundingZ1 + delta) / 1000.0f;

							//make sure very small actors are visible
							box.transform.localScale = new Vector3(
								Mathf.Max(box.transform.localScale.x, 0.1f),
								Mathf.Max(box.transform.localScale.y, 0.1f),
								Mathf.Max(box.transform.localScale.z, 0.1f));

							box.ID = objectid;
							box.Body = body;
							box.Room = roomNumber;
							box.Floor = floorNumber;
							box.Flags = ReadShort(memory[k + 4], memory[k + 5]);
							box.ColFlags = ReadShort(memory[k + 6], memory[k + 7]);
							box.LifeMode = ReadShort(memory[k + 50], memory[k + 51]);
							box.Life = ReadShort(memory[k + 52], memory[k + 53]);
							box.Chrono = ReadUnsignedInt(memory[k + 54], memory[k + 55], memory[k + 56], memory[k + 57]);
							box.RoomChrono = ReadUnsignedInt(memory[k + 58], memory[k + 59], memory[k + 60], memory[k + 61]);
							box.Anim = ReadShort(memory[k + 62], memory[k + 63]);
							box.Frame = ReadShort(memory[k + 74], memory[k + 75]);
							box.TotalFrames = ReadShort(memory[k + 76], memory[k + 77]);
							box.TrackNumber = ReadShort(memory[k + 84], memory[k + 85]);
							box.PositionInTrack = ReadShort(memory[k + 88], memory[k + 89]);
							box.TrackMode = trackMode;
							box.Speed = ReadShort(memory[k + 116], memory[k + 118]);


							box.Angles.x = ReadShort(memory[k + 40], memory[k + 41]) * 360 / 1024.0f;
							box.Angles.y = ReadShort(memory[k + 42], memory[k + 43]) * 360 / 1024.0f;
							box.Angles.z = ReadShort(memory[k + 44], memory[k + 45]) * 360 / 1024.0f;

							box.Mod.x = ReadShort(memory[k + 90], memory[k + 91]);
							box.Mod.y = ReadShort(memory[k + 92], memory[k + 93]);
							box.Mod.z = ReadShort(memory[k + 94], memory[k + 95]);

							box.LocalPosition.x = ReadShort(memory[k + 28], memory[k + 29]) + box.Mod.x;
							box.LocalPosition.y = ReadShort(memory[k + 30], memory[k + 31]) + box.Mod.y;
							box.LocalPosition.z = ReadShort(memory[k + 32], memory[k + 33]) + box.Mod.z;

							box.WorldPosition.x = ReadShort(memory[k + 34], memory[k + 35]) + box.Mod.x;
							box.WorldPosition.y = ReadShort(memory[k + 36], memory[k + 37]) + box.Mod.y;
							box.WorldPosition.z = ReadShort(memory[k + 38], memory[k + 39]) + box.Mod.z;

							box.BoundingPos.x = boundingx;
							box.BoundingPos.y = boundingy;
							box.BoundingPos.z = boundingz;

							box.BoundingSize.x = boundingX2 - boundingX1;
							box.BoundingSize.y = boundingY2 - boundingY1;
							box.BoundingSize.z = boundingZ2 - boundingZ1;
							
							box.ShowAdditionalInfo = ShowAdditionalInfo;

							//player
							if (objectid == lastValidPlayerIndex)
							{								
								float angle = box.Angles.y;
								float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

								playerInfo = new StringBuilder();
								playerInfo.AppendFormat("Position: {0} {1} {2}\nAngle: {3:N1} {4:N1}", box.LocalPosition.x, box.LocalPosition.y, box.LocalPosition.z, angle, sideAngle);

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
					fpsInfo.AppendFormat("Timer: {0}\nFps: {1}\nDelay: {2} ms\nAllow inventory: {3}\nCursor position: {4} {5} {6}", TimeSpan.FromSeconds(InternalTimer / 60),
						calculatedFps, lastDelayFpsCounter * 1000 / 200, allowInventory ? "Yes" : "No", (int)(mousePosition.x), (int)(mousePosition.y), (int)(mousePosition.z));
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
				//timer
				ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6, 4);
				InternalTimer = ReadUnsignedInt(memory[0], memory[1], memory[2], memory[3]);

				//inventory
				ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 - 0x1A4, 4);
				allowInventory = ReadShort(memory[0], memory[1]) == 1;
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
			//fps
			ProcessReader.Read(memory, memoryAddress - 0x83B6, 2);
			int fps = ReadShort(memory[0], memory[1]);

			//frames
			ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0x7464, 2);
			int frames = ReadShort(memory[0], memory[1]);

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
		}
	}

	void OnGUI()
	{
		if (warpMenuEnabled)
		{
			Rect rect = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 45, 400, 90);

			//close menu if there is a click out side
			if (Input.GetMouseButtonDown(0) && !rect.Contains(Input.mousePosition))
			{
				warpMenuEnabled = false;
			}

			GUILayout.BeginArea(rect, Style.Panel);
			GUILayout.BeginVertical();

			//label
			GUILayout.BeginHorizontal();
			GUIStyle label = new GUIStyle(Style.Button);
			label.fixedWidth = rect.width / 3.0f;
			GUILayout.Label("X", label);
			GUILayout.Label("Y", label);
			GUILayout.Label("Z", label);
			GUILayout.EndHorizontal();

			//textbox
			GUILayout.BeginHorizontal();
			GUIStyle button = new GUIStyle(Style.Button);
			button.normal.textColor = Color.white;
			button.fixedWidth = rect.width / 3.0f;
			warpX = GUILayout.TextField(warpX, button);
			warpY = GUILayout.TextField(warpY, button);
			warpZ = GUILayout.TextField(warpZ, button);
			GUILayout.EndHorizontal();

			//button
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Set position", Style.Button) || Event.current.keyCode == KeyCode.Return)
			{
				int x, y, z;
				if(int.TryParse(warpX, out x) && int.TryParse(warpY, out y) && int.TryParse(warpZ, out z))
				{
					x = Mathf.Clamp(x, short.MinValue, short.MaxValue);
					y = Mathf.Clamp(y, short.MinValue, short.MaxValue);
					z = Mathf.Clamp(z, short.MinValue, short.MaxValue);
					warpX = x.ToString();
					warpY = y.ToString();
					warpZ = z.ToString();
					WarpActorToPosition(warpActor, new Vector3(x, y, z));
				}
				else
				{
					warpX = warpActor.LocalPosition.x.ToString();
					warpY = warpActor.LocalPosition.y.ToString();
					warpZ = warpActor.LocalPosition.z.ToString();
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}

	private Vector3 GetMousePosition(int room, int floor)
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

	private void WarpActorToPosition(Box actor, Vector3 warpPosition)
	{
		//get object offset
		int index = Actors.GetComponentsInChildren<Box>(true).ToList().IndexOf(actor);
		if(index != -1)
		{
			long offset = memoryAddress + index * ActorStructSize[dosBoxPattern];

			//offset positions (world + local + bounding box)
			byte[] position = new byte[12];
			ProcessReader.Read(position, offset + 28, 6);
			int offsetX = (int)warpPosition.x - ReadShort(position[0], position[1]);
			int offsetY = (int)warpPosition.y - ReadShort(position[2], position[3]);
			int offsetZ = (int)warpPosition.z - ReadShort(position[4], position[5]);

			//bounding box
			ProcessReader.Read(position, offset + 8, 12);
			WriteShort(ReadShort(position[0], position[1]) + offsetX, position, 0); 
			WriteShort(ReadShort(position[2], position[3]) + offsetX, position, 2);
			WriteShort(ReadShort(position[4], position[5]) + offsetY, position, 4); 
			WriteShort(ReadShort(position[6], position[7]) + offsetY, position, 6);
			WriteShort(ReadShort(position[8], position[9]) + offsetZ, position, 8);
			WriteShort(ReadShort(position[10], position[11]) + offsetZ, position, 10);
			ProcessReader.Write(position, offset + 8, 12);

			//local + world
			ProcessReader.Read(position, offset + 28, 12);
			WriteShort(ReadShort(position[0], position[1]) + offsetX, position, 0); 
			WriteShort(ReadShort(position[2], position[3]) + offsetY, position, 2); 
			WriteShort(ReadShort(position[4], position[5]) + offsetZ, position, 4);
			WriteShort(ReadShort(position[6], position[7]) + offsetX, position, 6); 
			WriteShort(ReadShort(position[8], position[9]) + offsetY, position, 8); 
			WriteShort(ReadShort(position[10], position[11]) + offsetZ, position, 10);
			ProcessReader.Write(position, offset + 28, 12);
		}
	}

	private uint ReadUnsignedInt(byte a, byte b, byte c, byte d)
	{
		unchecked
		{
			return (uint)(a | b << 8 | c << 16 | d << 24);
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

	#endregion
}

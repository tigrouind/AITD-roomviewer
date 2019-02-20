using System;
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
	public bool ShowAdditionalInfo;
	public bool ShowAITD1Vars;
	public bool IsCDROMVersion;

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
	private int lastValidPlayerIndex = -1;
	private int linkfloor = 0;
	private int linkroom = 0;
	private long memoryAddress;
	private byte[] memory;

	//fps
	private int oldFramesCount;
	private Queue<int> previousFramesCount = new Queue<int>();
	private Queue<float> previousFrameTime = new Queue<float>();

	private float lastDelay;
	private Timer delayCounter = new Timer();
	private Timer totalDelay = new Timer();

	private int inHand;
	private bool allowInventory;
	private bool saveTimerFlag;
	private ushort internalTimer2;

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

							int modx = 0, mody = 0, modz = 0;
							if(dosBoxPattern == 0) //AITD1 only
							{
								modx = Utils.ReadShort(memory, k + 90);
								mody = Utils.ReadShort(memory, k + 92);
								modz = Utils.ReadShort(memory, k + 94);
							}

							FixBoundingWrap(ref boundingX1, ref boundingX2);
							FixBoundingWrap(ref boundingY1, ref boundingY2);
							FixBoundingWrap(ref boundingZ1, ref boundingZ2);

							box.BoundingLower = new Vector3(boundingX1, boundingY1, boundingZ1);
							box.BoundingUpper = new Vector3(boundingX2, boundingY2, boundingZ2);

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
							box.LifeMode = Utils. ReadShort(memory, k + 50);
							box.Life = Utils.ReadShort(memory, k + 52);
							box.Chrono = Utils.ReadUnsignedInt(memory, k + 54);
							box.RoomChrono = Utils.ReadUnsignedInt(memory, k + 58);
							box.AnimType = Utils.ReadShort(memory, k + 64);
							box.NextAnim = Utils.ReadShort(memory, k + 66);
							box.TotalFrames = Utils.ReadShort(memory, k + 76);
							box.TrackNumber = Utils.ReadShort(memory, k + 84);
							box.PositionInTrack = Utils.ReadShort(memory, k + 88);
							box.TrackMode = trackMode;
							box.OldAngle = Utils.ReadShort(memory, k + 106);
							box.NewAngle = Utils.ReadShort(memory, k + 108);
							box.RotateTime = Utils.ReadShort(memory, k + 110);
							box.Speed = Utils.ReadShort(memory, k + 116);
							box.HitForce = Utils.ReadShort(memory, k + 150);
							box.Slot = i;

							box.Angles.x = Utils.ReadShort(memory, k + 40);
							box.Angles.y = Utils.ReadShort(memory, k + 42);
							box.Angles.z = Utils.ReadShort(memory, k + 44);

							box.LocalPosition.x = Utils.ReadShort(memory, k + 28) + modx;
							box.LocalPosition.y = Utils.ReadShort(memory, k + 30) + mody;
							box.LocalPosition.z = Utils.ReadShort(memory, k + 32) + modz;

							box.WorldPosition.x = Utils.ReadShort(memory, k + 34) + modx;
							box.WorldPosition.y = Utils.ReadShort(memory, k + 36) + mody;
							box.WorldPosition.z = Utils.ReadShort(memory, k + 38) + modz;
							box.ShowAITD1Vars = ShowAITD1Vars;
							box.ShowAdditionalInfo = ShowAdditionalInfo;

							bool isAITD1 = dosBoxPattern == 0;
							if (isAITD1)
							{
								//hot point
								int animationType = Utils.ReadShort(memory, k + 142);
								Box hotPoint = box.BoxHotPoint;

								if (animationType == 2)
								{
									if (hotPoint == null)
									{ 
										hotPoint = Instantiate(BoxPrefab);
										hotPoint.name = "HotPoint";
										hotPoint.Color = new Color32(255, 0, 0, 255);
										box.BoxHotPoint = hotPoint;
									}

									Vector3 hotPosition;
									hotPosition.x = Utils.ReadShort(memory, k + 154);
									hotPosition.y = Utils.ReadShort(memory, k + 156);
									hotPosition.z = Utils.ReadShort(memory, k + 158);
									
									Vector3 finalPos = (hotPosition + box.LocalPosition) / 1000.0f;
									finalPos = new Vector3(finalPos.x, -finalPos.y, finalPos.z) + roomObject.localPosition;
									hotPoint.transform.position = finalPos;

									float range = Utils.ReadShort(memory, k + 148);
									hotPoint.transform.localScale = new Vector3(range, range, range) / 500.0f;
								}
								else if (hotPoint != null)
								{
									Destroy(hotPoint.gameObject);
									box.BoxHotPoint = null;
								}
							}

							int anim = Utils.ReadShort(memory, k + 62);
							int keyframe = Utils.ReadShort(memory, k + 74);

							if (ShowAITD1Vars)
							{
								int endframe = Utils.ReadShort(memory, k + 78);
								int endanim = Utils.ReadShort(memory, k + 80);

								if(anim != box.Anim || keyframe != box.Keyframe || endframe == 1 || endanim == 1)
								{
									box.lastKeyFrameChange.Reset();
								}

								if (saveTimerFlag)
								{
									box.lastKeyFrameChange.Stop();
								}
								else
								{
									box.lastKeyFrameChange.Start();
								}
							}

							box.Anim = anim;
							box.Keyframe = keyframe;

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

								//worldpost unsync
								Box worldPos = box.BoxWorldPos;

								if (box.WorldPosition.x != box.BoundingPos.x || box.WorldPosition.z != box.BoundingPos.z)
								{
									if (worldPos == null)
									{ 
										worldPos = Instantiate(BoxPrefab);
										worldPos.name = "WorldPos";
										worldPos.Color = new Color32(255, 0, 0, 128);
										box.BoxWorldPos = worldPos;
									}

									Vector3 finalPos = box.WorldPosition / 1000.0f;
									finalPos = new Vector3(finalPos.x, boxPosition.y, finalPos.z) + roomObject.localPosition;
									worldPos.transform.position = finalPos;
									worldPos.transform.localScale = box.transform.localScale;
								}
								else if (worldPos != null)
								{
									Destroy(worldPos.gameObject);
									box.BoxWorldPos = null;
								}
							}
							else
							{
								//other actors are green
								box.Color = new Color32(0, 128, 0, 255);

								//no world pos box for other actors
								if (box.BoxWorldPos != null)
								{
									Destroy(box.BoxWorldPos.gameObject);
									box.BoxWorldPos = null;
								}
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

				if (ShowAITD1Vars)
				{
					//internal timer
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6, 4);
					InternalTimer = Utils.ReadUnsignedInt(memory, 0);

					//internal timer 2
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 + 0xA5CE, 2);
					internalTimer2 = Utils.ReadUnsignedShort(memory, 0);

					//inventory
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 - 0x1A4, 2);
					allowInventory = Utils.ReadShort(memory, 0) == 1;

					//inhand
					ProcessReader.Read(memory, memoryAddress - 0x83B6 + 0xA33C, 2);
					inHand = Utils.ReadShort(memory, 0);

					//set by AITD when long running code is started (eg: loading ressource)
					ProcessReader.Read(memory, memoryAddress - 0x83B6 - 6 + 0x13EA, 4);
					saveTimerFlag = memory[0] == 1;
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

			BoxInfo.Append("Position", Player.LocalPosition);
			BoxInfo.Append("Angle", "{0:N1} {1:N1}", angle, sideAngle);
		}

		if (ShowAITD1Vars || ShowAdditionalInfo)
		{
			if(Player != null) BoxInfo.AppendLine();
			
			if (ShowAITD1Vars)
			{
				int calculatedFps = previousFramesCount.Sum();							
				TimeSpan totalDelayTS = TimeSpan.FromSeconds(totalDelay.Elapsed);

				BoxInfo.Append("Timer 1", "{0}.{1:D2}", TimeSpan.FromSeconds(InternalTimer / 60), InternalTimer % 60);
				BoxInfo.Append("Timer 2", "{0}.{1:D2}", TimeSpan.FromSeconds(internalTimer2 / 60), internalTimer2 % 60);
				BoxInfo.Append("FPS/Delay", "{0}; {1} ms", calculatedFps, Mathf.FloorToInt(lastDelay * 1000));
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
		if (Input.GetKeyDown(KeyCode.W))
		{
			foreach (Box box in Actors.GetComponentsInChildren<Box>(true))
			{
				box.LastDistance = 0.0f;
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha1) && ProcessReader != null)
		{
			//internal timer 1
			InternalTimer -= 60 * 5; //back 5 frames
			Utils.Write(InternalTimer, memory, 0);
			ProcessReader.Write(memory, memoryAddress - 0x83B6 - 6, 4);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) && ProcessReader != null)
		{
			//internal timer 2
			internalTimer2 -= 60 * 5; //back 5 frames
			Utils.Write(internalTimer2, memory, 0);
			ProcessReader.Write(memory, memoryAddress - 0x83B6 - 6 + 0xA5CE, 2);
		}
	}

	public void CalculateFPS()
	{
		if (ProcessReader != null && ShowAITD1Vars)
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

	void CleanRedBoxes()
	{
		foreach(Box box in Actors.GetComponentsInChildren<Box>(true))
		{
			if (box.BoxHotPoint != null)
			{
				Destroy(box.BoxHotPoint.gameObject);
				box.BoxHotPoint = null;
			}

			if (box.BoxWorldPos != null)
			{
				Destroy(box.BoxWorldPos.gameObject);
				box.BoxWorldPos = null;
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
		int detectedGame = GetComponent<RoomLoader>().DetectedGame;
		int patternIndex = detectedGame - 1;
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

					//check if CDROM/floppy version (AITD1 only)				
					byte[] cdPattern = ASCIIEncoding.ASCII.GetBytes("CD Not Found");
					IsCDROMVersion = detectedGame == 1 && reader.SearchForBytePattern(cdPattern) != -1;

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
		BoxInfo.Clear(true);
		RightText.text = string.Empty;
		lastValidPlayerIndex = -1;
		CleanRedBoxes();
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

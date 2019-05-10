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
	public Box[] Boxes;
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

	private byte[] varsMemoryPattern = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x00 };
	private byte[] cvarsMemoryPattern = new byte[] { 0x31, 0x00, 0x0E, 0x01, 0xBC, 0x02, 0x12, 0x00, 0x06, 0x00, 0x13, 0x00, 0x14, 0x00, 0x01 };

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
		Boxes = new Box[50];
		for (int i = 0; i < Boxes.Length; i++)
		{
			Box box = Instantiate(BoxPrefab);
			box.transform.parent = Actors.transform;
			box.name = "Actor";
			box.Slot = i;
			box.DosBox = this;
			Boxes[i] = box;
		}
	}

	void OnDestroy()
	{
		if (ProcessReader != null)
		{
			ProcessReader.Close();
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
				foreach (Box box in Boxes)
				{
					int k = i * ActorStructSize[dosBoxPattern];
					box.ID = Utils.ReadShort(memory, k + 0);

					if (box.ID != -1)
					{						
						int trackModeOffset = TrackModeOffsets[dosBoxPattern];
						box.Body = Utils.ReadShort(memory, k + 2);
						box.Flags = Utils.ReadShort(memory, k + 4);
						box.ColFlags = Utils.ReadShort(memory, k + 6);
						
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

						boundingX1 = Utils.ReadShort(memory, k + 20);
						boundingX2 = Utils.ReadShort(memory, k + 22);
						boundingY1 = Utils.ReadShort(memory, k + 24);
						boundingY2 = Utils.ReadShort(memory, k + 26);

						box.Box2DLower = new Vector2(boundingX1, boundingY1);
						box.Box2DUpper = new Vector2(boundingX2, boundingY2);

						box.LocalPosition.x = Utils.ReadShort(memory, k + 28);
						box.LocalPosition.y = Utils.ReadShort(memory, k + 30);
						box.LocalPosition.z = Utils.ReadShort(memory, k + 32);

						box.WorldPosition.x = Utils.ReadShort(memory, k + 34);
						box.WorldPosition.y = Utils.ReadShort(memory, k + 36);
						box.WorldPosition.z = Utils.ReadShort(memory, k + 38);

						box.Angles.x = Utils.ReadShort(memory, k + 40);
						box.Angles.y = Utils.ReadShort(memory, k + 42);
						box.Angles.z = Utils.ReadShort(memory, k + 44);

						box.Floor = Utils.ReadShort(memory, k + 46);
						box.Room = Utils.ReadShort(memory, k + 48);
						box.LifeMode = Utils. ReadShort(memory, k + 50);
						box.Life = Utils.ReadShort(memory, k + 52);
						box.Chrono = Utils.ReadUnsignedInt(memory, k + 54);
						box.RoomChrono = Utils.ReadUnsignedInt(memory, k + 58);
						box.Anim = Utils.ReadShort(memory, k + 62);
						box.AnimType = Utils.ReadShort(memory, k + 64);
						box.NextAnim = Utils.ReadShort(memory, k + 66);
						box.Keyframe = Utils.ReadShort(memory, k + 74);
						box.TotalFrames = Utils.ReadShort(memory, k + 76);
						box.Endframe = Utils.ReadShort(memory, k + 78);
						box.EndAnim = Utils.ReadShort(memory, k + 80);
						
						box.TrackMode = Utils.ReadShort(memory, k + trackModeOffset);
						box.TrackNumber = Utils.ReadShort(memory, k + 84);
						box.PositionInTrack = Utils.ReadShort(memory, k + 88);

						if(dosBoxPattern == 0) //AITD1 only
						{
							box.Mod.x = Utils.ReadShort(memory, k + 90);
							box.Mod.y = Utils.ReadShort(memory, k + 92);
							box.Mod.z = Utils.ReadShort(memory, k + 94);
						}
						else
						{
							box.Mod = Vector3.zero;
						}

						box.OldAngle = Utils.ReadShort(memory, k + 106);
						box.NewAngle = Utils.ReadShort(memory, k + 108);
						box.RotateTime = Utils.ReadShort(memory, k + 110);
						box.Speed = Utils.ReadShort(memory, k + 116);
						
						box.Col[0] = Utils.ReadShort(memory, k + 126);
						box.Col[1] = Utils.ReadShort(memory, k + 128);
						box.Col[2] = Utils.ReadShort(memory, k + 130);
						box.ColBy = Utils.ReadShort(memory, k + 132);
						box.HardTrigger = Utils.ReadShort(memory, k + 134);
						box.HardCol = Utils.ReadShort(memory, k + 136);
						box.Hit = Utils.ReadShort(memory, k + 138);
						box.HitBy = Utils.ReadShort(memory, k + 140);
						box.ActionType = Utils.ReadShort(memory, k + 142);
						box.HotBoxSize = Utils.ReadShort(memory, k + 148);
						box.HitForce = Utils.ReadShort(memory, k + 150);
						box.HotPosition.x = Utils.ReadShort(memory, k + 154);
						box.HotPosition.y = Utils.ReadShort(memory, k + 156);
						box.HotPosition.z = Utils.ReadShort(memory, k + 158);
					}

					i++;
				}

				//find player + switch floor if necessary
				foreach (Box box in Boxes)
				{
					bool isActive = box.ID != -1;
					if (isActive)
					{
						//player
						if (box.TrackMode == 1 || box.ID == lastValidPlayerIndex)
						{
							//update player index
							lastValidPlayerIndex = box.ID;

							//automatically switch room and floor (has to be done before setting other actors positions)
							if (linkfloor != box.Floor || linkroom != box.Room)
							{
								linkfloor = box.Floor;
								linkroom = box.Room;

								GetComponent<RoomLoader>().RefreshRooms(linkfloor, linkroom);
							}
						}
					}
				}

				//update all boxes
				foreach (Box box in Boxes)
				{
					if (box.ID != -1)
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
							box.transform.localScale = new Vector3(
								Mathf.Max(box.transform.localScale.x, 0.1f),
								Mathf.Max(box.transform.localScale.y, 0.1f),
								Mathf.Max(box.transform.localScale.z, 0.1f));

							bool isAITD1 = dosBoxPattern == 0;
							if (isAITD1)
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
									finalPos = new Vector3(finalPos.x, -finalPos.y, finalPos.z) + roomObject.localPosition;
									hotPoint.transform.position = finalPos;

									int boxSize = box.HotBoxSize;
									hotPoint.transform.localScale = new Vector3(boxSize, boxSize, boxSize) / 500.0f;
									hotPoint.AlwaysOnTop = Camera.main.orthographic;
								}
								else if (hotPoint != null)
								{
									Destroy(hotPoint.gameObject);
									box.BoxHotPoint = null;
								}
							}						

							if (ShowAITD1Vars)
							{
								if(box.PreviousAnim != box.Anim || box.PreviousKeyFrame != box.Keyframe || box.Endframe == 1 || box.EndAnim == 1)
								{
									box.PreviousAnim = box.Anim;
									box.PreviousKeyFrame = box.Keyframe;
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

							//player
							if (box.ID == lastValidPlayerIndex)
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
								Arrow.AlwaysOnTop = Camera.main.orthographic;
								player = box;

								//worldpost unsync
								Box worldPos = box.BoxWorldPos;

								if ((box.WorldPosition.x + box.Mod.x) != box.BoundingPos.x || (box.WorldPosition.z + box.Mod.z) != box.BoundingPos.z)
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
									finalPos = new Vector3(finalPos.x, boxPosition.y + 0.001f, finalPos.z) + roomObject.localPosition;
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

								//no world pos box for other actors
								if (box.BoxWorldPos != null)
								{
									Destroy(box.BoxWorldPos.gameObject);
									box.BoxWorldPos = null;
								}
							}

							box.AlwaysOnTop = Camera.main.orthographic;
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

	int[] GetAllDOSBOXProcesses()
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

		return processIds;
	}

	bool SearchDOSBOXProcess(int patternIndex, out int processId, out long address)
	{
		int[] processIds = GetAllDOSBOXProcesses();
		if (!processIds.Any())
		{
			RightText.text = "Cannot find DOSBOX process";
			processId = -1;
			address = -1;
			return false;
		}

		foreach (int pid in processIds)
		{
			ProcessMemoryReader reader = new ProcessMemoryReader(pid);
			foreach (var pattern in PlayerInitialPosition[patternIndex])
			{
				long foundAddress = reader.SearchForBytePattern(pattern);
				if (foundAddress != -1)
				{
					processId = pid;
					address = foundAddress + MemoryOffsets[patternIndex];

					reader.Close();
					return true;
				}
			}

			reader.Close();
		}

		processId = -1;
		address = -1;
		RightText.text = "Cannot find player data in DOSBOX process memory.";
		return false;
	}

	#region Room loader

	public bool LinkToDosBOX(int floor, int room, int detectedGame)
	{
		//search player position in DOSBOX processes
		int patternIndex = detectedGame - 1;

		int processId = Shared.ProcessId;
		if (processId == -1)
		{
			if (!SearchDOSBOXProcess(patternIndex, out processId, out memoryAddress))
			{
				return false;
			}
			
			Shared.ProcessId = processId;
			Shared.ActorsMemoryAdress = memoryAddress;
			ProcessReader = new ProcessMemoryReader(processId);

			//vars
			if (patternIndex == 0) //AITD1 only
			{
				Shared.VarsMemoryAddress = ProcessReader.SearchForBytePattern(varsMemoryPattern);
				Shared.CvarsMemoryAddress = ProcessReader.SearchForBytePattern(cvarsMemoryPattern);
			}
		}
		else
		{
			memoryAddress = Shared.ActorsMemoryAdress;
			ProcessReader = new ProcessMemoryReader(processId);
		}

		//force reload
		linkfloor = floor;
		linkroom = room;
		
		memory = new byte[ActorStructSize[patternIndex] * 50];
		dosBoxPattern = patternIndex;

		//check if CDROM/floppy version (AITD1 only)				
		byte[] cdPattern = ASCIIEncoding.ASCII.GetBytes("CD Not Found");
		IsCDROMVersion = detectedGame == 1 && ProcessReader.SearchForBytePattern(cdPattern) != -1;

		RightText.text = string.Empty;
		return true;
	}

	public void UnlinkDosBox()
	{
		ProcessReader.Close();
		ProcessReader = null;
		Shared.ProcessId = -1;
		BoxInfo.Clear(true);
		RightText.text = string.Empty;
		lastValidPlayerIndex = -1;
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

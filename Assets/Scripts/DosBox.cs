using System;
using UnityEngine;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

public class DosBox : MonoBehaviour
{
	public GUIText RightText;
	public GameObject Actors;
	public GameObject Arrow;
	public Box BoxPrefab;
	public uint InternalTimer;
	public MenuStyle Style;

	private byte[] varsMemory = new byte[207*2];
	private byte[] oldVarsMemory = new byte[207*2];
	private float[] varsMemoryTime = new float[207];
	private byte[] varsMemoryPattern = new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2E, 0x00, 0x2F, 0x00, 0x00, 0x00, 0x00 }; 
	private long varsMemoryAddress = -1;

	private byte[] cvarsMemory = new byte[44*2];
	private byte[] oldCVarsMemory = new byte[44*2];
	private float[] cvarsMemoryTime = new float[44];
	private byte[] cvarsMemoryPattern = new byte[] { 0x31, 0x00, 0x0E, 0x01, 0xBC, 0x02, 0x12, 0x00, 0x06, 0x00, 0x13, 0x00, 0x14, 0x00, 0x01 }; 
	private long cvarsMemoryAddress = -1;

	public bool ShowVarsMemory;

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

	private int[] MemoryOffsets = new [] { -188, -28, -28 }; //offset to apply to get beginning of actors array
	private int[] ActorStructSize = new [] { 160, 180, 182 }; //size of one actor
	private int[] TrackModeOffsets = new [] { 82, 90, 90 };

	private string[] cardinalPositions = new [] { "N", "E", "S", "W" };

	private Vector3 lastPlayerPosition;
	private int lastValidPlayerIndex;
	private int linkfloor = 0;
	private int linkroom = 0;
	private ProcessMemoryReader processReader;
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
	public bool ShowAdditionalInfo;

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

		if (processReader != null)
		{
			if (processReader.Read(memory, memoryAddress, memory.Length) > 0)
			{
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
							int w = k + 8;
							int x = (ReadShort(memory[w + 0], memory[w + 1]) + ReadShort(memory[w + 2], memory[w + 3])) / 2;
							int y = (ReadShort(memory[w + 4], memory[w + 5]) + ReadShort(memory[w + 6], memory[w + 7])) / 2;
							int z = (ReadShort(memory[w + 8], memory[w + 9]) + ReadShort(memory[w + 10], memory[w + 11])) / 2;

							//local to global position
							x += (int)(roomObject.localPosition.x * 1000.0f);
							y += (int)(roomObject.localPosition.y * 1000.0f);
							z += (int)(roomObject.localPosition.z * 1000.0f);

							box.transform.position = new Vector3(x, -y, z) / 1000.0f;

							//make actors appears slightly bigger than they are to be not covered by actors
							float delta = 1.0f;
							box.transform.localScale = new Vector3(
								ReadShort(memory[w + 2], memory[w + 3]) - ReadShort(memory[w + 0], memory[w + 1]) + delta,
								ReadShort(memory[w + 6], memory[w + 7]) - ReadShort(memory[w + 4], memory[w + 5]) + delta,
								ReadShort(memory[w + 10], memory[w + 11]) - ReadShort(memory[w + 8], memory[w + 9]) + delta) / 1000.0f;

							//make sure very small actors are visible
							box.transform.localScale = new Vector3(
								Mathf.Max(box.transform.localScale.x, 0.1f),
								Mathf.Max(box.transform.localScale.y, 0.1f),
								Mathf.Max(box.transform.localScale.z, 0.1f));

							box.ID = objectid;
							box.Body = body;
							box.Room = roomNumber;
							box.Flags = ReadShort(memory[k + 4], memory[k + 5]);
							box.LifeMode = ReadShort(memory[k + 50], memory[k + 51]);
							box.Life = ReadShort(memory[k + 52], memory[k + 53]);
							box.Chrono = ReadUnsignedInt(memory[k + 54], memory[k + 55], memory[k + 56], memory[k + 57]);
							box.RoomChrono =  ReadUnsignedInt(memory[k + 58], memory[k + 59], memory[k + 60], memory[k + 61]);
							box.Anim = ReadShort(memory[k + 62], memory[k + 63]);
							box.Frame = ReadShort(memory[k + 74], memory[k + 75]);
							box.TotalFrames = ReadShort(memory[k + 76], memory[k + 77]);
							box.TrackMode = trackMode;
							box.Speed = ReadShort(memory[k + 116], memory[k + 118]);

							if(!ShowAdditionalInfo)
							{
								//those fields are only supported in AITD1
								box.Chrono = 0;
								box.RoomChrono = 0;
								box.Frame = -1;
								box.Speed = -1;
							}

							//player
							if (objectid == lastValidPlayerIndex)
							{
								float angle = ReadShort(memory[k + 42], memory[k + 43]) * 360 / 1024.0f;

								angle = (540.0f - angle) % 360.0f;

								float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

								int cardinalPos = (int)Math.Floor((angle + 45.0f) / 90);

								playerInfo = new StringBuilder();
								playerInfo.AppendFormat("Position: {0} {1} {2}\nAngle: {3:N1} {4:N1}{5}", x, y, z, angle, sideAngle, cardinalPositions[cardinalPos % 4]);

								//check if player has moved
								if (box.transform.position != lastPlayerPosition)
								{
									//center camera to player position
									GetComponent<RoomLoader>().CenterCamera(new Vector2(box.transform.position.x, box.transform.position.z));
									lastPlayerPosition = box.transform.position;
								}

								if (Camera.main.orthographic)
								{
									//make sure player is always visible
									box.transform.localScale = new Vector3(box.transform.localScale.x, box.transform.localScale.y * 5.0f, box.transform.localScale.z);
								}

								//follow player
								Arrow.transform.position = box.transform.position + new Vector3(0.0f, box.transform.localScale.y / 2.0f + 0.001f, 0.0f);
								//face camera
								Arrow.transform.rotation = Quaternion.AngleAxis(90.0f, -Vector3.left);
								Arrow.transform.rotation *= Quaternion.AngleAxis(-angle, Vector3.forward);

								Arrow.transform.localScale = new Vector3(
									box.transform.localScale.x * 0.9f,
									box.transform.localScale.z * 0.9f,
									1.0f);

								//player is white
								box.Color = new Color32(255, 255, 255, 255);

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
					fpsInfo = new StringBuilder();
                    fpsInfo.AppendFormat("Timer: {0}\nFps: {1}\nDelay: {2} ms", TimeSpan.FromSeconds(InternalTimer / 60),
                        calculatedFps, lastDelayFpsCounter * 1000 / 200);
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

		if(processReader != null)
		{
            if (ShowAdditionalInfo)
            {
                //timer
                processReader.Read(memory, memoryAddress - 0x83B6 - 6, 4);
                InternalTimer = ReadUnsignedInt(memory[0], memory[1], memory[2], memory[3]);
            }

			if(varsMemoryAddress != -1)
			{
				processReader.Read(varsMemory, varsMemoryAddress, varsMemory.Length);
				CheckDifferences(varsMemory, oldVarsMemory, varsMemoryTime, 207);
			}

			if(cvarsMemoryAddress != -1)
			{
				processReader.Read(cvarsMemory, cvarsMemoryAddress, cvarsMemory.Length);
				CheckDifferences(cvarsMemory, oldCVarsMemory, cvarsMemoryTime, 44);
			}
		}

		//arrow is only active if actors are active and player is active
		Arrow.SetActive(Actors.activeSelf
			&& player != null
			&& player.activeSelf
			&& player.transform.localScale.magnitude > 0.01f);				
	}

	void FixedUpdate()
	{
		if(processReader != null && ShowAdditionalInfo)
		{
			//fps
			processReader.Read(memory, memoryAddress - 0x83B6, 2);
			int fps = ReadShort(memory[0], memory[1]);

			//frames
			processReader.Read(memory, memoryAddress - 0x83B6 + 0x7464, 2);
			int frames = ReadShort(memory[0], memory[1]);

			//check how much frames elapsed since last time
			int diff;
			if(frames >= oldFramesCount)
				diff = frames - oldFramesCount;
			else
				diff = (fps - oldFramesCount) + frames;	
			oldFramesCount = frames;

			//check for large delays
			if(diff == 0) 
			{ 
				delayFpsCounter++;
                if(delayFpsCounter > 100/(1000/200)) // 20 frames at 200FPS = 100ms
				{
					lastDelayFpsCounter = delayFpsCounter;
				}
			}
			else 
			{
				delayFpsCounter = 0; 
			}

			previousFramesCount.Enqueue(diff);
			while(previousFramesCount.Count > 200) previousFramesCount.Dequeue();

			calculatedFps = previousFramesCount.Sum();
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

	#region Room loader

	void OnGUI()
	{
		if(ShowVarsMemory)
		{
			GUIStyle panel = new GUIStyle(Style.Panel);
			panel.normal.background = Style.BlackTexture;
			Rect areaA = new Rect(0, 0, Screen.width, Screen.height * 22.0f/28.0f);
			Rect areaB = new Rect(0, Screen.height * 22.0f/28.0f, Screen.width, Screen.height * 6.0f/28.0f);

			GUILayout.BeginArea(areaA, panel);
			DisplayTable(areaA, 10, 21, varsMemory, varsMemoryTime, "VARS");
			GUILayout.EndArea();

			GUILayout.BeginArea(areaB, panel);
			DisplayTable(areaB, 10, 5, cvarsMemory, cvarsMemoryTime, "CVARS");
			GUILayout.EndArea();
		}
	}

	void CheckDifferences(byte[] values, byte[] oldvalues, float[] time, int count)
	{
        float currenttime = Time.time;
		for(int i = 0 ; i < count ; i++)
		{
			int value = ReadShort(values[i * 2 + 0], values[i * 2 + 1]);
			int oldValue = ReadShort(oldvalues[i * 2 + 0], oldvalues[i * 2 + 1]);
			if (value != oldValue)
			{
                time[i] = currenttime;
			}

			oldvalues[i * 2 + 0] = values[i * 2 + 0];
			oldvalues[i * 2 + 1] = values[i * 2 + 1];
		}
	}

    void DisplayTable(Rect area, int columns, int rows, byte[] values, float[] timer, string title)
	{
        //setup style
		GUIStyle labelStyle = new GUIStyle(Style.Label);
        labelStyle.fixedWidth = area.width/(columns + 1);
		labelStyle.fixedHeight = area.height/((float)(rows + 1));
		labelStyle.alignment = TextAnchor.MiddleCenter;
	
		GUIStyle headerStyle = new GUIStyle(labelStyle);
		headerStyle.normal.textColor = new Color32(0, 0, 0, 255);
		headerStyle.normal.background = Style.GreenTexture;

		//header
		GUILayout.BeginHorizontal();
		GUILayout.Label(title, headerStyle);
		for (int i = 0 ; i < columns ; i++)
		{
			GUILayout.Label(i.ToString(), headerStyle);
		}
		GUILayout.EndHorizontal();

		//body
        int count = 0;
        float currenttime = Time.time;
		for (int i = 0 ; i < rows ; i++)
		{
            GUILayout.BeginHorizontal();
            headerStyle.alignment = TextAnchor.MiddleRight;
            GUILayout.Label(i.ToString(), headerStyle);

            for (int j = 0; j < columns; j++)
            {
                string stringValue = string.Empty;
                if (count < values.Length / 2)
                {
                    int value = ReadShort(values[count * 2 + 0], values[count * 2 + 1]);
                    bool different = (currenttime - timer[count]) < 5.0f;

                    if (value != 0 || different)
                        stringValue = value.ToString();

                    //highlight recently changed vars
                    labelStyle.normal.background = different ? Style.RedTexture : null;
                }
               
                count++;
                GUILayout.Label(stringValue, labelStyle);
            }
            GUILayout.EndHorizontal();
		}
	}

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
					processReader = reader;
					memory = new byte[ActorStructSize[patternIndex] * 50];
					dosBoxPattern = patternIndex;

					//vars
					if(patternIndex == 0) //AITD1 only
					{
						varsMemoryAddress = reader.SearchForBytePattern(varsMemoryPattern);
						cvarsMemoryAddress = reader.SearchForBytePattern(cvarsMemoryPattern);
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
		processReader.Close();
		processReader = null;
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

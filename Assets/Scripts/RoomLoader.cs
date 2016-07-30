using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class RoomLoader : MonoBehaviour
{
	private int floor = 0;
	private int room = 0;
	private int[] cameraColors = new [] { 0xFF8080, 0x789CF0, 0xB0DE6F, 0xCC66C0, 0x5DBAAB, 0xF2BA79, 0x8E71E3, 0x6ED169, 0xBF6080, 0x7CCAF7 };
	private Vector3 mousePosition;
	private float cameraRotation;
	private KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private List<int> floors = new List<int>();

	private int showrooms = 2;
	private bool showtriggers = true;
	private int showareas = 0;
	private int cameraFollow = 1;

	public GUIText LeftText;
	public GUIText BottomText;
	public Box BoxPrefab;

	public GUIText BoxInfo;
	private Box HighLightedBox;
	private Box SelectedBox;

	private bool DosBoxEnabled;
	public GameObject Actors;

	private short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	private int ReadInt(byte a, byte b, byte c, byte d)
	{
		unchecked
		{
			return (int)(a | b << 8 | c << 16 | d << 24);
		}
	}

	void Start()
	{
		Directory.CreateDirectory("GAMEDATA");

		//check existing ETAGEXX folders
		floors = Directory.GetDirectories("GAMEDATA")
			.Select(x => Path.GetFileName(x))
			.Where(x => x.StartsWith("ETAGE"))
			.Select(x => int.Parse(x.Substring(5, 2)))
			.ToList();
		floor = floors.FirstOrDefault();

		RefreshRooms();
	}

	void RefreshRooms()
	{
		LeftText.text = string.Format("E{0}R{1}", floor, room);

		//load new floor only if needed
		if (name != "FLOOR" + floor)
		{
			RemoveAll();
			LoadFloor(floor);
		}

		SetRoomObjectsVisibility(room);

		if (cameraFollow == 1) //room
		{
			CenterCamera(room);
		}
	}


	void CenterCamera(int room)
	{
		if (transform.childCount > 0)
		{
			Transform roomTransform = transform.Find("ROOM" + room);
			if (roomTransform != null)
			{
				Vector3 roomPosition = roomTransform.localPosition;
				Camera.main.transform.position = new Vector3(roomPosition.x, Camera.main.transform.position.y, roomPosition.z);
			}
		}
	}

	void SetRoomObjectsVisibility(int room)
	{
		bool showallrooms = showrooms == 3 || showrooms == 2;
		bool showallroomstransparent = showrooms == 2;
		bool showcolliders = showrooms != 0;

		int roomIndex = 0;
		foreach (Transform roomTransform in transform.Cast<Transform>().Where(x => x.name != "DELETED"))
		{
			bool currentRoom = room == roomIndex;
			//roomTransform.gameObject.SetActive(showallrooms || currentRoom);
			foreach (Box box in roomTransform.GetComponentsInChildren<Box>(true))
			{
				if (box.name == "Trigger")
				{
					box.gameObject.SetActive(showtriggers && currentRoom);
				}

				if (box.name == "Camera")
				{
					box.gameObject.SetActive(showareas == 2 || (showareas == 1 && currentRoom));
				}

				if (box.name == "Collider")
				{
					box.gameObject.SetActive(showcolliders && (showallrooms || currentRoom));
					box.Alpha = (byte)((showallroomstransparent && !currentRoom) ? 40 : 255);
				}
			}
			roomIndex++;
		}
	}

	void LoadFloor(int floor)
	{
		//check folder
		string folder = string.Format("GAMEDATA\\ETAGE{0:D2}", floor);
		if (!Directory.Exists(folder))
		{
			LeftText.text = string.Format("Cannot find folder {0}", folder);
			return;
		}

		//load file
		string filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000000");
		byte[] allPointsA = File.ReadAllBytes(filePath);
		List<List<int>> camerasPerRoom = new List<List<int>>();

		name = "FLOOR" + floor;
		int maxrooms = ReadInt(allPointsA[0], allPointsA[1], allPointsA[2], allPointsA[3]) / 4;
		for (int currentroom = 0; currentroom < maxrooms; currentroom++)
		{
			int i = currentroom * 4;
			int roomheader = ReadInt(allPointsA[i + 0], allPointsA[i + 1], allPointsA[i + 2], allPointsA[i + 3]);
			if (roomheader > allPointsA.Length || roomheader == 0)
			{
				//all rooms parsed
				break;
			}

			//room
			GameObject roomObject = new GameObject();
			roomObject.name = "ROOM" + currentroom;
			roomObject.transform.parent = transform;

			int roomx = ReadShort(allPointsA[roomheader + 4], allPointsA[roomheader + 5]);
			int roomy = ReadShort(allPointsA[roomheader + 6], allPointsA[roomheader + 7]);
			int roomz = ReadShort(allPointsA[roomheader + 8], allPointsA[roomheader + 9]);

			roomObject.transform.localPosition = new Vector3(roomx, roomy, -roomz) / 100.0f;

			//colliders
			i = roomheader + ReadShort(allPointsA[roomheader + 0], allPointsA[roomheader + 1]);
			int totalpoint = ReadShort(allPointsA[i + 0], allPointsA[i + 1]);
			i += 2;

			for (int count = 0; count < totalpoint; count++)
			{
				Box box = Instantiate(BoxPrefab);
				box.name = "Collider";
				box.Room = currentroom;
				box.transform.parent = roomObject.transform;

				box.transform.localPosition = new Vector3((ReadShort(allPointsA[i + 0], allPointsA[i + 1]) + ReadShort(allPointsA[i + 2], allPointsA[i + 3])),
					-(ReadShort(allPointsA[i + 4], allPointsA[i + 5]) + ReadShort(allPointsA[i + 6], allPointsA[i + 7])),
					(ReadShort(allPointsA[i + 8], allPointsA[i + 9]) + ReadShort(allPointsA[i + 10], allPointsA[i + 11]))) / 2000.0f;

				box.transform.localScale = new Vector3((ReadShort(allPointsA[i + 2], allPointsA[i + 3]) - ReadShort(allPointsA[i + 0], allPointsA[i + 1])),
					(ReadShort(allPointsA[i + 6], allPointsA[i + 7]) - ReadShort(allPointsA[i + 4], allPointsA[i + 5])),
					(ReadShort(allPointsA[i + 10], allPointsA[i + 11]) - ReadShort(allPointsA[i + 8], allPointsA[i + 9]))) / 1000.0f;

				box.ID = ReadShort(allPointsA[i + 12], allPointsA[i + 13]);
				box.Flags = ReadShort(allPointsA[i + 14], allPointsA[i + 15]);

				box.Color = new Color32(143, 143, 143, 255);
				if ((box.Flags & 2) == 2)
				{
					//underground floor
					box.Color = new Color32(100, 100, 100, 255);
				}
				if ((box.Flags & 8) == 8)
				{
					//interactive box
					box.Color = new Color32(0, 0, 128, 255);
				}

				i += 16;
			}

			//triggers
			i = roomheader + ReadShort(allPointsA[roomheader + 2], allPointsA[roomheader + 3]);
			totalpoint = ReadShort(allPointsA[i + 0], allPointsA[i + 1]);
			i += 2;

			for (int count = 0; count < totalpoint; count++)
			{

				Box box = Instantiate(BoxPrefab);
				box.name = "Trigger";
				box.Room = currentroom;
				box.transform.parent = roomObject.transform;

				box.transform.localPosition = new Vector3((ReadShort(allPointsA[i + 0], allPointsA[i + 1]) + ReadShort(allPointsA[i + 2], allPointsA[i + 3])),
					-(ReadShort(allPointsA[i + 4], allPointsA[i + 5]) + ReadShort(allPointsA[i + 6], allPointsA[i + 7])),
					(ReadShort(allPointsA[i + 8], allPointsA[i + 9]) + ReadShort(allPointsA[i + 10], allPointsA[i + 11]))) / 2000.0f;

				box.transform.localScale = new Vector3((ReadShort(allPointsA[i + 2], allPointsA[i + 3]) - ReadShort(allPointsA[i + 0], allPointsA[i + 1])),
					(ReadShort(allPointsA[i + 6], allPointsA[i + 7]) - ReadShort(allPointsA[i + 4], allPointsA[i + 5])),
					(ReadShort(allPointsA[i + 10], allPointsA[i + 11]) - ReadShort(allPointsA[i + 8], allPointsA[i + 9]))) / 1000.0f;

				box.ID = ReadShort(allPointsA[i + 12], allPointsA[i + 13]);
				box.Flags = ReadShort(allPointsA[i + 14], allPointsA[i + 15]);

				if (box.Flags == 9 || box.Flags == 10) //custom trigger or exit
				{
					box.Color = new Color32(255, 128, 0, 50);
				}
				else if (box.Flags == 0) //room switch
				{
					box.Color = new Color32(255, 0, 0, 45); 
				}
				else
				{
					box.Color = new Color32(255, 128, 128, 50);
				}

				i += 16;
			}

			//cameras
			int cameraCount = ReadShort(allPointsA[roomheader + 10], allPointsA[roomheader + 11]);
			List<int> cameraInRoom = new List<int>();
			for (int cameraIndex = 0; cameraIndex < cameraCount; cameraIndex++)
			{
				int cameraID = ReadShort(allPointsA[roomheader + cameraIndex * 2 + 12], allPointsA[roomheader + cameraIndex * 2 + 13]);  //camera
				cameraInRoom.Add(cameraID);
			}

			camerasPerRoom.Add(cameraInRoom);
		}


		//cameras
		bool isAITD1 = DetectGame() == 1;
		filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000001");
		byte[] allPointsB = File.ReadAllBytes(filePath);
		int roomIndex = 0;
		List<Transform> rooms = transform.Cast<Transform>().Where(x => x.name != "DELETED").ToList();
		foreach (Transform room in rooms)
		{
			foreach (int cameraID in camerasPerRoom[roomIndex])
			{
				int cameraHeader = ReadShort(allPointsB[cameraID * 4 + 0], allPointsB[cameraID * 4 + 1]);
				int numentries = ReadShort(allPointsB[cameraHeader + 0x12], allPointsB[cameraHeader + 0x13]);

				List<Vector2> points = new List<Vector2>();
				List<int> indices = new List<int>();

				for (int k = 0; k < numentries; k++)
				{
					int i = cameraHeader + 0x14 + k * (isAITD1 ? 12 : 16);
					int cameraRoom = ReadShort(allPointsB[i + 0], allPointsB[i + 1]);

					if (cameraRoom == roomIndex)
					{
						i = cameraHeader + ReadShort(allPointsB[i + 4], allPointsB[i + 5]);
						int totalAreas = ReadShort(allPointsB[i + 0], allPointsB[i + 1]);
						i += 2;

						for (int g = 0; g < totalAreas; g++)
						{
							int totalPoints = ReadShort(allPointsB[i + 0], allPointsB[i + 1]);
							i += 2;

							List<Vector2> pts = new List<Vector2>();
							for (int u = 0; u < totalPoints; u++)
							{
								short px = ReadShort(allPointsB[i + 0], allPointsB[i + 1]);
								short pz = ReadShort(allPointsB[i + 2], allPointsB[i + 3]);
								pts.Add(new Vector2(px, pz) / 100.0f);
								i += 4;
							}

							Triangulator tr = new Triangulator(pts);
							int[] idc = tr.Triangulate();
							indices.AddRange(idc.Select(x => x + points.Count).ToArray());
							points.AddRange(pts);
						}
					}
				}

				if (points.Count > 0)
				{
					int colorRGB = cameraColors[cameraID % cameraColors.Length];

					Box box = Instantiate(BoxPrefab);
					box.name = "Camera";
					box.transform.parent = room;
					box.transform.localPosition = Vector3.zero;
					box.Color = new Color32((byte)((colorRGB >> 16) & 0xFF), (byte)((colorRGB >> 8) & 0xFF), (byte)(colorRGB & 0xFF), 100);
					box.ID = cameraID;
					MeshFilter filter = box.GetComponent<MeshFilter>();

					// Use the triangulator to get indices for creating triangles
					filter.sharedMesh = GetMeshFromPoints(points, indices);
					Destroy(box.gameObject.GetComponent<BoxCollider>());
					box.gameObject.AddComponent<MeshCollider>();
				}
			}

			roomIndex++;
		}
	}

	void RemoveAll()
	{
		name = "DELETED";
		foreach (Transform t in transform)
		{
			t.name = "DELETED"; //bug fix
			Destroy(t.gameObject);
		}
	}

	public int DetectGame()
	{
		//detect game based on number of floors
		if(floors.Count >= 15) return 2; 
		else if(floors.Count >= 14) return 3;
		else return 1;
	}

	void Update()
	{
		if (Input.GetAxis("Mouse ScrollWheel") > 0)
		{
			if (Camera.main.orthographic)
			{
				Camera.main.orthographicSize *= 0.9f;
			}
			else
			{
				Camera.main.transform.position = Vector3.Scale(Camera.main.transform.position, new Vector3(1.0f, 0.9f, 1.0f));
			}

		}

		if (Input.GetAxis("Mouse ScrollWheel") < 0)
		{
			if (Camera.main.orthographic)
			{
				Camera.main.orthographicSize *= 1.0f / 0.9f;
			}
			else
			{
				Camera.main.transform.position = Vector3.Scale(Camera.main.transform.position, new Vector3(1.0f, 1.0f / 0.9f, 1.0f));
			}
		}

		if (!menuEnabled)
		{
			//start drag
			if (Input.GetMouseButtonDown(0))
			{
				mousePosition = Input.mousePosition;
			}

			//dragging
			if (Input.GetMouseButton(0))
			{
				Vector3 newMousePosition = Input.mousePosition;
				if (newMousePosition != this.mousePosition)
				{ 
					Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
					Vector3 mouseDelta = Camera.main.ScreenToWorldPoint(this.mousePosition + cameraHeight)
						- Camera.main.ScreenToWorldPoint(newMousePosition + cameraHeight);

					Camera.main.transform.position += mouseDelta;
					mousePosition = newMousePosition;
				}
			}
		}

		//menu
		if (Input.GetMouseButtonDown(1))
		{
			menuEnabled = !menuEnabled;
		}

		if (!menuEnabled)
		{
			RefreshHighLightedBox();
		}

		foreach (var key in keyCodes)
		{
			if (Input.GetKeyDown(key))
			{
				ProcessKey(key);
			}
		}
	}

	private int BoxComparer(RaycastHit a, RaycastHit b)
	{
		// check distance
		if (Mathf.Abs(a.distance - b.distance) >= 0.0005f)
		{
			return a.distance.CompareTo(b.distance);
		}

		//if objects are too close each other, check current room
		int aCurrentRoom = a.collider.GetComponent<Box>().Room == room ? 0 : 1;
		int bCurrentRoom = b.collider.GetComponent<Box>().Room == room ? 0 : 1;
		if (aCurrentRoom != bCurrentRoom)
		{
			return aCurrentRoom.CompareTo(bCurrentRoom);
		}

		return 0;
	}

	private void RefreshHighLightedBox()
	{
		Vector3 mousePosition = Input.mousePosition;

		RaycastHit[] hitInfos = null;

		if (mousePosition.x > 0 && mousePosition.x < Screen.width &&
			mousePosition.y > 0 && mousePosition.y < Screen.height)
		{
			hitInfos = Physics.RaycastAll(Camera.main.ScreenPointToRay(mousePosition));
		}

		if (hitInfos != null && hitInfos.Length > 0)
		{
			//boxes inside current room have priority over other boxes
			Array.Sort(hitInfos, BoxComparer);

			Box box = hitInfos[0].collider.GetComponent<Box>();
			if (box != HighLightedBox)
			{
				if (HighLightedBox != null)
					HighLightedBox.HighLight = false;
				box.HighLight = true;

				HighLightedBox = box;
			}

			//display info
			Vector3 position = Camera.main.WorldToScreenPoint(box.GetComponent<Renderer>().bounds.center);
			BoxInfo.transform.position = new Vector3(position.x / Screen.width, position.y / Screen.height, 0.0f);

			//text
			BoxInfo.text = box.ToString(GetComponent<DosBox>().InternalTimer);
		}
		else
		{
			if (HighLightedBox != null)
			{
				HighLightedBox.HighLight = false;
				HighLightedBox = null;
				BoxInfo.text = string.Empty;
			}
		}

		//toggle selected box
		if (Input.GetMouseButtonDown(0) && HighLightedBox != null && HighLightedBox.name == "Actor")
		{
			if (SelectedBox != HighLightedBox)
				SelectedBox = HighLightedBox;
			else
				SelectedBox = null;
		}

		if (!DosBoxEnabled)
		{
			SelectedBox = null;
		}

		//display selected box info
		if (SelectedBox != null)
		{
			BottomText.text = SelectedBox.ToString(GetComponent<DosBox>().InternalTimer);
		}
		else
		{
			BottomText.text = string.Empty;
		}
	}

	#region DOSBOX

	public void RefreshRooms(int newfloor, int newroom)
	{
		if(cameraFollow == 1 || cameraFollow == 2) //room or player
		{
			if (floors.Contains(newfloor))
			{
				floor = newfloor;
				room = newroom;
				RefreshRooms();
			}
		}
	}

	public Transform GetRoom(int newfloor, int newroom)
	{
		if (floor == newfloor && newroom >= 0 && newroom < transform.childCount)
		{
			return transform.GetChild(newroom);
		}
		return null;
	}

	public void CenterCamera(Vector2 position)
	{
		if (cameraFollow == 2) //follow player
		{
			Camera.main.transform.position = new Vector3(position.x, Camera.main.transform.position.y, position.y);
		}
	}

	#endregion

	#region GUI

	private string[] roomModes = new string[] { "No", "Current room", "Current room / All", "All"  };
	private string[] areaModes = new string[] { "No", "Current room", "All" };
	private string[] cameraModes = new string[] { "Nothing", "Current room", "Player" };
	private bool menuEnabled;
	public MenuStyle Style;

	void OnGUI()
	{
		if (menuEnabled)
		{
			Rect rect;
			if (!DosBoxEnabled)
				rect = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 15 * 8, 400, 30 * 8);
			else
				rect = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 15 * 9, 400, 30 * 9);

			//close menu if there is a  click out side
			if (Input.GetMouseButtonDown(0) && !rect.Contains(Input.mousePosition))
			{
				menuEnabled = false;
			}

			GUILayout.BeginArea(rect, Style.Panel);
			GUILayout.BeginVertical();

			//dosbox
			if (GUILayout.Button(!DosBoxEnabled ? "Link to DOSBOX" : "Unlink DOSBOX", Style.Button) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.L);
			}

			if (GUILayout.Button("Model viewer", Style.Button) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.Tab);
			}

			//camera
			GUILayout.BeginHorizontal();
			GUILayout.Label("Camera projection", Style.Label);
			if (GUILayout.Button(Camera.main.orthographic ? "Orthographic" : "Perspective", Style.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.C);
			}
			GUILayout.EndHorizontal();

			//follow
			GUILayout.BeginHorizontal();
			GUILayout.Label("Camera follow", Style.Label);
			if (GUILayout.Button(cameraModes[cameraFollow], Style.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.F);
			}
			GUILayout.EndHorizontal();

			//rooms
			GUILayout.BeginHorizontal();
			GUILayout.Label("Rooms", Style.Label);
			if (GUILayout.Button(roomModes[showrooms], Style.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.H);
			}
			GUILayout.EndHorizontal();

			//areas
			GUILayout.BeginHorizontal();
			GUILayout.Label("Cameras", Style.Label);
			if (GUILayout.Button(areaModes[showareas], Style.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.A);
			}
			GUILayout.EndHorizontal();


			//triggers
			GUILayout.BeginHorizontal();
			GUILayout.Label("Triggers", Style.Label);
			if (GUILayout.Button(showtriggers ? "Yes" : "No", Style.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.T);
			}
			GUILayout.EndHorizontal();

			//actors
			if (DosBoxEnabled)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label("Actors", Style.Label);
				if (GUILayout.Button(Actors.activeSelf ? "Yes" : "No", Style.Option) && Event.current.button == 0)
				{
					ProcessKey(KeyCode.J);
				}
				GUILayout.EndHorizontal();
			}

			//camera rotate
			GUILayout.BeginHorizontal();
			GUILayout.Label("Camera rotation", Style.Label);
			float rotation = Mathf.Round(GUILayout.HorizontalSlider(cameraRotation, -8.0f, 8.0f, Style.Slider, Style.Thumb));
			if (Event.current.button == 0)
			{
				cameraRotation = rotation;
				Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, cameraRotation * 22.5f);
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}

	public void ProcessKey(KeyCode keyCode)
	{
		switch (keyCode)
		{
			case KeyCode.L:
				if (!DosBoxEnabled)
				{
					bool result = (GetComponent<DosBox>().LinkToDosBOX(floor, room));
					Actors.SetActive(result);
					DosBoxEnabled = result;
				}
				else
				{
					//follow player => room
					if (cameraFollow == 2)
					{
						cameraFollow = 1;
					}
					GetComponent<DosBox>().UnlinkDosBox();

					Actors.SetActive(false);
					DosBoxEnabled = false;
				}
				menuEnabled = false; //hide menu
				break;

			case KeyCode.Tab:
				SceneManager.LoadScene("model");
				break;

			case KeyCode.C:
				Camera.main.orthographic = !Camera.main.orthographic;
				float planeSize = Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2.0f);

				if (Camera.main.orthographic)
				{
					Camera.main.orthographicSize = Camera.main.transform.position.y * planeSize;
					Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, 20.0f, Camera.main.transform.position.z);
				}
				else
				{
					Camera.main.transform.position = new Vector3(
						Camera.main.transform.position.x,
						Camera.main.orthographicSize / planeSize,
						Camera.main.transform.position.z);
				}
				break;

			case KeyCode.F:
				cameraFollow = (cameraFollow + 1) % (!DosBoxEnabled ? 2 : 3);
				if (cameraFollow == 1) //room
				{
					CenterCamera(room);
				}
				else if (cameraFollow == 2) //player
				{
					//make sure camear snap back
					GetComponent<DosBox>().ResetCamera(floor, room);
				}
				break;

			case KeyCode.H:
				showrooms = (showrooms + 1) % 4;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.A:
				showareas = (showareas + 1) % 3;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.T:
				showtriggers = !showtriggers;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.J:
				Actors.SetActive(!Actors.activeSelf);
				break;


			case KeyCode.Escape:
				if (Screen.fullScreen)
					Application.Quit();
				break;

			case KeyCode.PageUp:
				cameraRotation = Math.Min(cameraRotation + 1, 8);
				Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, cameraRotation * 22.5f);
				break;

			case KeyCode.PageDown:
				cameraRotation = Math.Max(cameraRotation - 1, -8);
				Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, cameraRotation * 22.5f);
				break;

			case KeyCode.DownArrow:
				//skip missing floors
				int index = floors.IndexOf(floor);
				if (index > 0)
				{
					index--;
					floor = floors[index];
					room = 0;
					RefreshRooms();
				}
				break;

			case KeyCode.UpArrow:
				//skip missing floors
				int idx = floors.IndexOf(floor);
				if (idx < floors.Count - 1)
				{
					idx++;
					floor = floors[idx];
					room = 0;
					RefreshRooms();
				}
				break;

			case KeyCode.LeftArrow:
				if (room > 0)
				{
					room--;
					RefreshRooms();
				}
				break;

			case KeyCode.RightArrow:
				int roomsCount = transform.Cast<Transform>().Where(x => x.name != "DELETED").Count();
				if (room < roomsCount - 1)
				{
					room++;
					RefreshRooms();
				}
				break;
		}
	}

	#endregion

	Mesh GetMeshFromPoints(List<Vector2> vertices2D, List<int> indices)
	{
		// Create the Vector3 vertices
		Vector3[] vertices = new Vector3[vertices2D.Count];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = new Vector3(vertices2D[i].x, 0, vertices2D[i].y);
		}

		// Create the mesh
		Mesh msh = new Mesh();
		msh.vertices = vertices;
		msh.triangles = indices.ToArray();
		msh.RecalculateNormals();
		msh.RecalculateBounds();

		return msh;

	}
}

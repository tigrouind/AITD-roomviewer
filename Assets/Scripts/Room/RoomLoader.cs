using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class RoomLoader : MonoBehaviour
{
	private int floor;
	private int room;
	private int currentCamera = -1;
	private int currentCameraRoom = -1;
	private int currentCameraFloor = -1;
	private readonly int[] cameraColors = { 0xFF8080, 0x789CF0, 0xB0DE6F, 0xCC66C0, 0x5DBAAB, 0xF2BA79, 0x8E71E3, 0x6ED169, 0xBF6080, 0x7CCAF7 };
	private Vector3 mousePosition;
	private readonly KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private List<int> floors = new List<int>();
	private List<Transform> rooms = new List<Transform>();
	private List<Vector3Int> roomsPosition = new List<Vector3Int>();
	private List<List<int>> camerasPerRoom = new List<List<int>>();
	private readonly BoxComparer boxComparer = new BoxComparer();
	private float defaultCameraZoom = 10.0f;
	private readonly Timer defaultBoxSelectionTimer = new Timer();
	private readonly Timer linkToDosBoxTimer = new Timer();
	private bool speedRunMode;
	private Vector3 startDragPosition;
	private bool allowWarp;
	private bool refreshSelectedBoxAllowed = true;
	private bool highLightedBoxAllowed = true;
	private Box currentCameraBox;

	public Text LeftText;
	public BoxInfo BottomText;
	public Box BoxPrefab;

	public BoxInfo BoxInfo;
	private Box highLightedBox;
	private Box selectedBox;
	private int selectedBoxId = -1;

	private bool dosBoxEnabled;
	private GameVersion gameVersion;
	public GameObject Actors;

	public RectTransform Panel;
	public ToggleButton ShowAdditionalInfo;
	public ToggleButton ShowActors;
	public Slider CameraRotation;
	public ToggleButton ShowTriggers;
	public ToggleButton ShowRooms;
	public ToggleButton ShowAreas;
	public ToggleButton CameraFollow;
	public ToggleButton CameraMode;
	public GameObject Border;
	public GameObject CameraFrustum;
	public ColorTheme ColorTheme;
	public Button Theme;

	void Start()
	{
		Directory.CreateDirectory(Config.BaseDirectory);

		//check existing ETAGEXX files
		Regex match = new Regex(@"ETAGE(\d\d)\.PAK", RegexOptions.IgnoreCase);
		floors = Directory.GetFiles(Config.BaseDirectory)
			.Select(x => match.Match(Path.GetFileName(x)))
			.Where(x => x.Success)
			.Select(x => int.Parse(x.Groups[1].Value))
			.ToList();

		floor = floors.FirstOrDefault();
		gameVersion = DetectGame();

		ParseCommandLine();
		LoadTheme(ColorTheme.Theme);
		if (floors.Count > 0)
		{
			RefreshRooms();
		}
		ToggleMenuDOSBoxOptions(false);

		Border.GetComponent<MeshFilter>().mesh = GetComponent<CameraHelper>().SetupBorder();
		linkToDosBoxTimer.Start();
		LinkToDosBox();
	}

	void RefreshRooms()
	{
		//load new floor only if needed
		if (name != "FLOOR" + floor)
		{
			RemoveAll();
			LoadFloor(floor);
		}

		SetRoomObjectsVisibility(room);

		if (CameraFollow.Value == 1) //room
		{
			CenterCamera(room);
		}
	}

	void CenterCamera(int room)
	{
		Transform roomTransform = GetRoom(floor, room);
		if (roomTransform != null)
		{
			Vector3 roomPosition = roomTransform.localPosition;
			Camera.main.transform.position = new Vector3(roomPosition.x, Camera.main.transform.position.y, roomPosition.z);
		}
	}

	void SetRoomObjectsVisibility(int room)
	{
		bool showAllRooms = ShowRooms.Value == 3 || ShowRooms.Value == 2;
		bool showAllRoomsTransparent = ShowRooms.Value == 2;
		bool showColliders = ShowRooms.Value != 0;
		bool showTriggers = ShowTriggers.BoolValue;
		bool showAreas = ShowAreas.Value != 0;

		int roomIndex = 0;
		int currentCameraID = currentCameraFloor == floor &&
			currentCameraRoom >= 0 && currentCameraRoom < camerasPerRoom.Count &&
			currentCamera >= 0 && currentCamera < camerasPerRoom[currentCameraRoom].Count ? camerasPerRoom[currentCameraRoom][currentCamera] : -1;

		CameraFrustum.SetActive(showAreas && currentCameraBox != null);

		foreach (Transform roomTransform in rooms)
		{
			bool currentRoom = room == roomIndex;
			if (currentRoom)
			{
				Border.transform.position = roomTransform.position;
			}

			foreach (Box box in roomTransform.GetComponentsInChildren<Box>(true))
			{
				if (box.name == "Trigger")
				{
					box.gameObject.SetActive(showTriggers && currentRoom);
				}

				if (box.name == "Camera")
				{
					box.gameObject.SetActive(ShowAreas.Value == 4
						|| (ShowAreas.Value == 2 && currentRoom)
						|| (ShowAreas.Value == 3 && room >= 0 && room < camerasPerRoom.Count && camerasPerRoom[room].Contains(box.ID))
						|| (ShowAreas.Value == 1 && currentCameraID == box.ID));
				}

				if (box.name == "Collider")
				{
					box.gameObject.SetActive(showColliders && (showAllRooms || currentRoom));
					box.Alpha = (byte)((showAllRoomsTransparent && !currentRoom) ? 40 : 255);
				}
			}
			roomIndex++;
		}

		if (floor == currentCameraFloor && room == currentCameraRoom && currentCameraID != -1 && ShowAdditionalInfo.BoolValue)
		{
			LeftText.text = string.Format("E{0}R{1} C{2}", floor, room, currentCameraID);
		}
		else
		{
			LeftText.text = string.Format("E{0}R{1}", floor, room);
		}
	}

	void LoadFloor(int floor)
	{
		//load cameras and rooms
		camerasPerRoom = new List<List<int>>();
		rooms = new List<Transform>();
		roomsPosition = new List<Vector3Int>();
		name = "FLOOR" + floor;

		string filePath = Config.GetPath("ETAGE{0:D2}.PAK", floor);
		if (gameVersion == GameVersion.TIMEGATE)
		{
			LoadRoomsMulti(filePath);
		}
		else
		{
			LoadRoomsSingle(filePath);
		}
	}

	void LoadRoomsSingle(string filePath)
	{
		using (var pak = new PakArchive(filePath))
		{
			var buffer = pak[0].Read();
			int maxrooms = buffer.ReadInt(0) / 4;
			for (int currentroom = 0; currentroom < maxrooms; currentroom++)
			{
				int roomheader = buffer.ReadInt(currentroom * 4);
				if (roomheader <= 0 || roomheader >= buffer.Length)
				{
					//all rooms parsed
					break;
				}

				LoadRoom(buffer, roomheader, currentroom);
			}

			buffer = pak[1].Read();
			foreach (int cameraID in camerasPerRoom.SelectMany(x => x).Distinct())
			{
				int cameraHeader = buffer.ReadInt(cameraID * 4);
				LoadCamera(buffer, cameraHeader, cameraID);
			}
		}
	}

	void LoadRoomsMulti(string filePath)
	{
		using (var pak = new PakArchive(filePath))
		{
			foreach (var entry in pak)
			{
				LoadRoom(entry.Read(), 0, entry.Index);
			}
		}

		filePath = Config.GetPath("CAMSAL{0:D2}.PAK", floor);
		if (File.Exists(filePath))
		{
			using (var pak = new PakArchive(filePath))
			{
				foreach (var entry in pak)
				{
					LoadCamera(entry.Read(), 0, entry.Index);
				}
			}
		}
	}

	void LoadRoom(byte[] buffer, int roomheader, int currentroom)
	{
		//room
		GameObject roomObject = new GameObject();
		roomObject.name = "ROOM" + currentroom;
		roomObject.transform.parent = transform;
		rooms.Add(roomObject.transform);

		Vector3Int roomPosition = buffer.ReadVector(roomheader + 4);
		roomPosition = new Vector3Int(roomPosition.x, -roomPosition.y, -roomPosition.z) * 10;
		roomObject.transform.localPosition = new Vector3(roomPosition.x, -roomPosition.y, roomPosition.z) / 1000.0f;
		roomsPosition.Add(roomPosition);

		//colliders
		int i = roomheader + buffer.ReadShort(roomheader + 0);
		int totalpoint = buffer.ReadShort(i + 0);
		i += 2;

		for (int count = 0; count < totalpoint; count++)
		{
			Box box = Instantiate(BoxPrefab);
			box.name = "Collider";
			box.Room = currentroom;
			box.transform.parent = roomObject.transform;

			buffer.ReadBoundingBox(i + 0, out box.BoundingLower, out box.BoundingUpper);
			box.SetPositionAndSize();

			box.ID = buffer.ReadShort(i + 12);
			box.Flags = buffer.ReadShort(i + 14);

			box.Color = new Color32(143, 143, 143, 255);
			if ((box.Flags & 2) == 2)
			{
				//underground floor
				box.Color = new Color32(100, 100, 100, 255);
			}
			if ((box.Flags & 4) == 4)
			{
				//room link
				box.Color = new Color32(0, 100, 128, 255);
			}
			if ((box.Flags & 8) == 8)
			{
				//interactive box
				box.Color = new Color32(0, 0, 128, 255);
			}

			i += 16;
		}

		//triggers
		i = roomheader + buffer.ReadShort(roomheader + 2);
		totalpoint = buffer.ReadShort(i + 0);
		i += 2;

		for (int count = 0; count < totalpoint; count++)
		{
			Box box = Instantiate(BoxPrefab);
			box.name = "Trigger";
			box.Room = currentroom;
			box.transform.parent = roomObject.transform;

			buffer.ReadBoundingBox(i + 0, out box.BoundingLower, out box.BoundingUpper);
			box.SetPositionAndSize();

			box.ID = buffer.ReadShort(i + 12);
			box.Flags = buffer.ReadShort(i + 14);

			if (box.Flags == 9) //custom trigger
			{
				box.Color = new Color32(255, 128, 0, 50);
			}
			else if (box.Flags == 10) //exit
			{
				box.Color = new Color32(255, 255, 50, 100);
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
		int cameraCount = buffer.ReadShort(roomheader + 10);
		List<int> cameraInRoom = new List<int>();
		for (int cameraIndex = 0; cameraIndex < cameraCount; cameraIndex++)
		{
			int cameraID = buffer.ReadShort(roomheader + cameraIndex * 2 + 12);	 //camera
			cameraInRoom.Add(cameraID);
		}

		camerasPerRoom.Add(cameraInRoom);
	}

	void LoadCamera(byte[] buffer, int cameraHeader, int cameraID)
	{
		int numentries = buffer.ReadShort(cameraHeader + 0x12);
		for (int k = 0; k < numentries; k++)
		{
			List<Vector2> points = new List<Vector2>();
			List<int> indices = new List<int>();

			int structSize = 16;
			if (gameVersion == GameVersion.AITD1) structSize = 12;
			if (gameVersion == GameVersion.TIMEGATE) structSize = 22;

			int i = cameraHeader + 0x14 + k * structSize;
			int cameraRoom = buffer.ReadShort(i + 0);

			i = cameraHeader + buffer.ReadShort(i + 4);
			int totalAreas = buffer.ReadShort(i + 0);
			i += 2;

			for (int g = 0; g < totalAreas; g++)
			{
				int totalPoints = buffer.ReadShort(i + 0);
				i += 2;

				List<Vector2> pts = new List<Vector2>();
				for (int u = 0; u < totalPoints; u++)
				{
					int px = buffer.ReadShort(i + 0);
					int pz = buffer.ReadShort(i + 2);
					pts.Add(new Vector2(px, pz) / 100.0f);
					i += 4;
				}

				Triangulator tr = new Triangulator(pts);
				int[] idc = tr.Triangulate();
				indices.AddRange(idc.Select(x => x + points.Count).ToArray());
				points.AddRange(pts);
			}

			if (points.Count > 0)
			{
				var room = GetRoom(floor, cameraRoom);
				if (room == null) continue;

				int colorRGB = cameraColors[cameraID % cameraColors.Length];

				Box area = Instantiate(BoxPrefab);
				area.name = "Camera";
				area.transform.parent = room;
				area.transform.localPosition = Vector3.zero;
				area.Color = new Color32((byte)((colorRGB >> 16) & 0xFF), (byte)((colorRGB >> 8) & 0xFF), (byte)(colorRGB & 0xFF), 100);
				area.ID = cameraID;
				area.DosBox = GetComponent<DosBox>();
				area.RoomLoader = this;
				MeshFilter filter = area.GetComponent<MeshFilter>();

				// Use the triangulator to get indices for creating triangles
				filter.sharedMesh = GetComponent<CameraHelper>().GetMeshFromPoints(points, indices);
				Destroy(area.gameObject.GetComponent<BoxCollider>());
				area.gameObject.AddComponent<MeshCollider>();

				//setup camera
				area.CameraRotation = buffer.ReadVector(cameraHeader + 0);
				area.CameraPosition = buffer.ReadVector(cameraHeader + 6);
				area.CameraFocal = buffer.ReadVector(cameraHeader + 12);
			}
		}
	}

	void RemoveAll()
	{
		CameraFrustum.SetActive(false);
		foreach (Transform t in transform)
		{
			Destroy(t.gameObject);
		}
	}

	GameVersion DetectGame()
	{
		//detect game based on number of floors
		if (GetEntriesCount("ETAGE00.PAK") > 2)
			return GameVersion.TIMEGATE;
		else if (floors.Count >= 15 || (floors.Count == 2 && floors.Contains(0) && floors.Contains(8)))
			return GameVersion.AITD2;
		else if (floors.Count >= 14 || (floors.Count == 2 && floors.Contains(0) && floors.Contains(2)))
			return GameVersion.AITD3;
		else if (floors.Count == 1 && floors.Contains(16))
			return GameVersion.JACK;
		else
			return GameVersion.AITD1;
	}

	int GetEntriesCount(string filePath)
	{
		string firstFloorFilePath = Config.GetPath(filePath);
		if (File.Exists(firstFloorFilePath))
		{
			using (var pak = new PakArchive(firstFloorFilePath))
			{
				return pak.Count;
			}
		}

		return 0;
	}

	void Update()
	{
		WarpDialog warpDialog = GetComponent<WarpDialog>();
		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		if (mouseWheel != 0.0f)
		{
			Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
			Vector3 mouseBeforeZoom = Camera.main.ScreenToWorldPoint(Input.mousePosition + cameraHeight);

			float scale = 0.9f;
			if (mouseWheel < 0.0f)
			{
				scale = 1.0f / scale;
			}

			if (Camera.main.orthographic)
			{
				Camera.main.orthographicSize *= scale;
			}
			else
			{
				Camera.main.transform.position = Vector3.Scale(Camera.main.transform.position, new Vector3(1.0f, scale, 1.0f));
			}

			cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
			Camera.main.transform.position += mouseBeforeZoom - Camera.main.ScreenToWorldPoint(Input.mousePosition + cameraHeight);
		}

		bool mustWarpActor = false;
		if (!menuEnabled && !warpDialog.WarpMenuEnabled)
		{
			//start drag
			if (Input.GetMouseButtonDown(0))
			{
				refreshSelectedBoxAllowed = true;
				mousePosition = startDragPosition = Input.mousePosition;

				if (highLightedBox != null && highLightedBox.name == "Actor")
				{
					warpDialog.WarpActorBox = highLightedBox;
					warpDialog.WarpActorBoxId = highLightedBox.ID;
					allowWarp = true;
				}
			}

			//dragging
			if (Input.GetMouseButton(0))
			{
				Vector3 newMousePosition = Input.mousePosition;
				if (newMousePosition != mousePosition)
				{
					if (!allowWarp)
					{
						//move camera
						Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
						Vector3 mouseDelta = Camera.main.ScreenToWorldPoint(mousePosition + cameraHeight)
											- Camera.main.ScreenToWorldPoint(newMousePosition + cameraHeight);

						Camera.main.transform.position += mouseDelta;
						mousePosition = newMousePosition;
					}

					if ((startDragPosition - newMousePosition).magnitude > 4.0f)
					{
						refreshSelectedBoxAllowed = false;
						if (allowWarp)
						{
							highLightedBoxAllowed = false;
						}
					}
				}

				//warp actor
				if (Input.GetMouseButtonDown(1) && warpDialog.WarpActorBox != null)
				{
					refreshSelectedBoxAllowed = false;
					highLightedBoxAllowed = false;

					allowWarp = true;
					mustWarpActor = true;
				}
			}

			//end drag
			if (Input.GetMouseButtonUp(0))
			{
				highLightedBoxAllowed = true;
				allowWarp = false;
			}
		}

		//menu
		if (Input.GetMouseButtonDown(1) && (!Input.GetMouseButton(0) || warpDialog.WarpActorBox == null))
		{
			if (menuEnabled)
			{
				menuEnabled = false;
			}
			else if (warpDialog.WarpMenuEnabled)
			{
				warpDialog.WarpMenuEnabled = false;
				if (speedRunMode)
				{
					warpDialog.WarpActorBox = null; //reset to player
					warpDialog.WarpActorBoxId = -1;
				}
			}
			else if (dosBoxEnabled && highLightedBox != null && highLightedBox.name == "Actor")
			{
				warpDialog.LoadActor(highLightedBox);
				warpDialog.WarpMenuEnabled = true;
			}
			else
			{
				menuEnabled = true;
			}
		}

		if (Input.GetMouseButtonUp(0)
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			menuEnabled = false;
		}

		if (menuEnabled != Panel.gameObject.activeSelf)
		{
			Panel.gameObject.SetActive(menuEnabled);
		}

		DosBox dosBox = GetComponent<DosBox>();
		dosBox.ShowAdditionalInfo = ShowAdditionalInfo.BoolValue && dosBoxEnabled;
		dosBox.ShowAITD1Vars = dosBox.ShowAdditionalInfo && dosBox.GameVersion == GameVersion.AITD1;
		dosBox.ShowActors = ShowActors.BoolValue;
		dosBox.SpeedRunMode = speedRunMode;

		dosBox.RefreshMemory();
		dosBox.RefreshCacheEntries();
		dosBox.CalculateFPS();
		dosBox.CheckDelay();
		dosBox.UpdateAllActors();
		dosBox.UpdateRightText();
		GetComponent<ExchangeSlot>().UpdateTargetSlot(highLightedBox);
		RefreshHighLightedBox();
		RefreshSelectedBox();
		RefreshCurrentCamera();

		//must be done after DosBox update
		if (mustWarpActor)
		{
			warpDialog.WarpActor();
		}

		//process keys
		if (!warpDialog.WarpMenuEnabled)
		{
			foreach (var key in keyCodes)
			{
				if (Input.GetKeyDown(key))
				{
					ProcessKey(key);
				}
			}
		}

		//automatic link
		if (GetComponent<DosBox>().ProcessMemory == null && linkToDosBoxTimer.Elapsed > 1.0f)
		{
			LinkToDosBox();
			linkToDosBoxTimer.Restart();
		}
	}

	void RefreshHighLightedBox()
	{
		Vector3 mousePosition = Input.mousePosition;
		RaycastHit[] hitInfos = null;

		//check screen boundaries
		if (mousePosition.x > 0 && mousePosition.x < Screen.width &&
			mousePosition.y > 0 && mousePosition.y < Screen.height)
		{
			hitInfos = Physics.RaycastAll(Camera.main.ScreenPointToRay(mousePosition));
		}

		if (hitInfos != null && hitInfos.Length > 0
			&& !menuEnabled && !GetComponent<WarpDialog>().WarpMenuEnabled && highLightedBoxAllowed)
		{
			//sort colliders by priority
			boxComparer.Room = room;
			Array.Sort(hitInfos, boxComparer);

			Box box = hitInfos[0].collider.GetComponent<Box>();
			if (box != highLightedBox)
			{
				if (highLightedBox != null)
				{
					highLightedBox.HighLight = false;
				}

				BoxInfo.Reset();
				box.HighLight = true;
				highLightedBox = box;
			}

			//display info
			Vector3 position = Camera.main.WorldToScreenPoint(box.GetComponent<Renderer>().bounds.center);
			Vector2 size = BoxInfo.GetComponent<RectTransform>().sizeDelta;

			//make sure box fit in viewport
			position.x = Mathf.Clamp(position.x, size.x / 2.0f, Screen.width - size.x / 2.0f);
			position.y = Mathf.Clamp(position.y, size.y / 2.0f, Screen.height - size.y / 2.0f);
			BoxInfo.transform.position = new Vector3(position.x, position.y, 0.0f);

			//text
			box.UpdateText(BoxInfo);
		}
		else
		{
			if (highLightedBox != null)
			{
				highLightedBox.HighLight = false;
				highLightedBox = null;
			}

			BoxInfo.Clear(true);
		}
	}

	void RefreshSelectedBox()
	{
		//toggle selected box
		if (Input.GetMouseButtonUp(0) && highLightedBox != null && refreshSelectedBoxAllowed
			&& !(GetComponent<WarpDialog>().WarpMenuEnabled	 //make sure it not possible to change actor when there is a click inside warp menu
				&& RectTransformUtility.RectangleContainsScreenPoint(GetComponent<WarpDialog>().Panel, Input.mousePosition)))
		{
			if (highLightedBox.name == "Camera")
			{
				RefreshSelectedCamera();
			}
			else if (highLightedBox.name == "Actor")
			{
				if (selectedBox != highLightedBox)
				{
					selectedBox = highLightedBox;
					selectedBoxId = highLightedBox.ID;
					BottomText.Reset();
				}
				else
				{
					selectedBox = null;
					selectedBoxId = -1;
					defaultBoxSelectionTimer.Restart();
				}
			}
		}

		selectedBox = GetComponent<DosBox>().RefreshBoxUsingID(selectedBox, selectedBoxId);

		if (selectedBox == null && speedRunMode && defaultBoxSelectionTimer.Elapsed > 1.0f)
		{
			//select player by default
			selectedBox = GetComponent<DosBox>().Player;
			if (selectedBox != null)
			{
				selectedBoxId = selectedBox.ID;
			}
		}

		if (selectedBox != null)
		{
			//display selected box info
			selectedBox.UpdateText(BottomText);
		}
		else
		{
			BottomText.Clear(true);
		}
	}

	void RefreshCurrentCamera()
	{
		var dosBox = GetComponent<DosBox>();
		if (dosBox.CurrentCamera != currentCamera
		|| dosBox.CurrentCameraRoom != currentCameraRoom
		|| dosBox.CurrentCameraFloor != currentCameraFloor)
		{
			currentCamera = dosBox.CurrentCamera;
			currentCameraRoom = dosBox.CurrentCameraRoom;
			currentCameraFloor = dosBox.CurrentCameraFloor;
			SetRoomObjectsVisibility(room);
		}
	}

	Transform GetRoom(int newFloor, int newRoom)
	{
		if (floor == newFloor && newRoom >= 0 && newRoom < rooms.Count)
		{
			return rooms[newRoom];
		}

		return null;
	}

	#region DOSBOX

	public void RefreshRooms(int newfloor, int newroom)
	{
		if (CameraFollow.Value == 1 || CameraFollow.Value == 2) //room or player
		{
			if (floors.Contains(newfloor))
			{
				floor = newfloor;
				room = newroom;
				RefreshRooms();
			}
		}
	}

	public bool TryGetRoomPosition(int newFloor, int newRoom, out Vector3Int position)
	{
		if (floor == newFloor && newRoom >= 0 && newRoom < roomsPosition.Count)
		{
			position = roomsPosition[newRoom];
			return true;
		}

		position = default(Vector3Int);
		return false;
	}

	public void CenterCamera(Vector2 position)
	{
		if (CameraFollow.Value == 2) //follow player
		{
			Camera.main.transform.position = new Vector3(position.x, Camera.main.transform.position.y, position.y);
		}
	}

	public void LinkToDosBox()
	{
		if (Application.platform != RuntimePlatform.WindowsEditor &&
			Application.platform != RuntimePlatform.WindowsPlayer)
		{
			return;
		}

		if (GetComponent<DosBox>().LinkToDosBOX(floor, room, gameVersion))
		{
			if (!dosBoxEnabled)
			{
				//none => current camera
				if (GetComponent<DosBox>().GameVersion == GameVersion.AITD1 && ShowAreas.Value == 0)
				{
					ShowAreas.Value = 1;
				}

				//set follow mode to player
				CameraFollow.Value = 2;
				GetComponent<DosBox>().ResetCamera(floor, room);
			}

			dosBoxEnabled = true;

			//select player by default
			GetComponent<DosBox>().UpdateAllActors();
			selectedBox = GetComponent<DosBox>().Player;
			if (selectedBox != null)
			{
				selectedBoxId = selectedBox.ID;
			}
		}

		Actors.SetActive(dosBoxEnabled && ShowActors.BoolValue);
		Border.SetActive(dosBoxEnabled);

		ToggleMenuDOSBoxOptions(dosBoxEnabled);
	}

	public Vector3Int GetMousePosition(int room, int floor)
	{
		Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
		Vector3Int mousePosition = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition + cameraHeight) * 1000.0f);
		Vector3Int roomPosition;
		if (TryGetRoomPosition(floor, room, out roomPosition))
		{
			mousePosition -= roomPosition;
		}
		return mousePosition;
	}

	public int GetCameraSlot(int index)
	{
		return room >= 0 && room < camerasPerRoom.Count ? camerasPerRoom[room].IndexOf(index) : -1;
	}

	#endregion

	#region GUI

	private bool menuEnabled;

	void SetCameraRotation(Slider slider)
	{
		Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, slider.value * 22.5f);
	}

	void ToggleMenuDOSBoxOptions(bool enabled)
	{
		ShowActors.transform.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	public void ProcessKey(string keyCode)
	{
		KeyCode keyCodeEnum = (KeyCode)Enum.Parse(typeof(KeyCode), keyCode, true);
		ProcessKey(keyCodeEnum);
	}

	void ProcessKey(KeyCode keyCode)
	{
		switch (keyCode)
		{
			case KeyCode.Tab:
				SceneManager.LoadScene("model");
				break;

			case KeyCode.D:
				Camera.main.orthographic = !Camera.main.orthographic;
				CameraMode.BoolValue = Camera.main.orthographic;
				float planeSize = Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2.0f);

				if (Camera.main.orthographic)
				{
					Camera.main.orthographicSize = Camera.main.transform.position.y * planeSize;
					Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, 40.0f, Camera.main.transform.position.z);
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
				CameraFollow.Value = (CameraFollow.Value + 1) % (!dosBoxEnabled ? 2 : 3);
				if (CameraFollow.Value == 1) //room
				{
					CenterCamera(room);
				}
				else if (CameraFollow.Value == 2) //player
				{
					//make sure camera snap back
					GetComponent<DosBox>().ResetCamera(floor, room);
				}
				break;

			case KeyCode.R:
				ShowRooms.Value = (ShowRooms.Value + 1) % 4;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.C:
				ShowAreas.Value = (ShowAreas.Value + 1) % 5;
				if (!(dosBoxEnabled && GetComponent<DosBox>().GameVersion == GameVersion.AITD1) && ShowAreas.Value == 1)
				{
					ShowAreas.Value++; //skip value
				}
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.T:
				ShowTriggers.BoolValue = !ShowTriggers.BoolValue;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.A:
				ShowActors.BoolValue = !ShowActors.BoolValue;
				Actors.SetActive(dosBoxEnabled && ShowActors.BoolValue);
				break;

			case KeyCode.H:
				if (ColorTheme.Themes.Count > 0)
				{
					var themeIndex = ColorTheme.Themes.IndexOf(ColorTheme.Theme);
					var theme = ColorTheme.Themes[(themeIndex + 1) % ColorTheme.Themes.Count];
					LoadTheme(theme);
					ColorTheme.Theme = theme;
				}
				break;

			case KeyCode.E:
				ShowAdditionalInfo.BoolValue = !ShowAdditionalInfo.BoolValue;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.Mouse2:
				//reset position
				Vector3 cameraHeight = new Vector3(0.0f, 0.0f, Camera.main.transform.position.y);
				Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition + cameraHeight);
				Camera.main.transform.position = new Vector3(position.x, Camera.main.transform.position.y, position.z);
				ResetCameraZoom();
				break;

			case KeyCode.Escape:
				if (Screen.fullScreen)
					Application.Quit();
				break;

			case KeyCode.PageUp:
				CameraRotation.value = Math.Min(CameraRotation.value + 1, 8);
				break;

			case KeyCode.PageDown:
				CameraRotation.value = Math.Max(CameraRotation.value - 1, -8);
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
				if (room < rooms.Count - 1)
				{
					room++;
					RefreshRooms();
				}
				break;
		}
	}

	#endregion

	void ParseCommandLine()
	{
		var args = Environment.GetCommandLineArgs();
		if (args.Contains("-speedrun", StringComparer.InvariantCultureIgnoreCase))
		{
			speedRunMode = true;
			defaultCameraZoom = 10.0f / Mathf.Pow(0.9f, 5.0f);
			ResetCameraZoom();
			ShowAdditionalInfo.BoolValue = true;
		}
	}

	public void LoadTheme(ColorTheme colorTheme)
	{
		BottomText.GetComponent<Image>().color = colorTheme.BoxColor;
		BoxInfo.GetComponent<Image>().color = colorTheme.BoxColor;
		Camera.main.backgroundColor = colorTheme.ClearColor;
		Panel.GetComponent<Image>().color = colorTheme.MenuColor;
	}

	void LoadTheme(Theme theme)
	{
		ColorTheme.LoadTheme(theme);
		Theme.GetComponentInChildren<Text>().text = theme.Name;
	}

	void RefreshSelectedCamera()
	{
		if (highLightedBox != currentCameraBox)
		{
			GetComponent<CameraHelper>().SetupCameraFrustum(CameraFrustum, highLightedBox);
			currentCameraBox = highLightedBox;
		}
		else
		{
			currentCameraBox = null;
		}

		SetRoomObjectsVisibility(room);
	}

	void ResetCameraZoom()
	{
		float planeWidth = Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2.0f);
		Camera.main.orthographicSize = defaultCameraZoom * planeWidth;
	}
}

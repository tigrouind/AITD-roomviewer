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
using System.Globalization;

public class RoomLoader : MonoBehaviour
{
	private int floor = 0;
	private int room = 0;
	private int[] cameraColors = new [] { 0xFF8080, 0x789CF0, 0xB0DE6F, 0xCC66C0, 0x5DBAAB, 0xF2BA79, 0x8E71E3, 0x6ED169, 0xBF6080, 0x7CCAF7 };
	private Vector3 mousePosition;
	private KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private List<int> floors = new List<int>();
	private List<List<int>> camerasPerRoom;
	private BoxComparer boxComparer = new BoxComparer();
	private float defaultCameraZoom = 10.0f;
	private Timer defaultBoxSelectionTimer = new Timer();
	private Timer linkToDosBoxTimer = new Timer();
	private bool speedRunMode;
	private Vector3 startDragPosition;
	private bool dragging;
	private Box currentCamera;

	public Text LeftText;
	public BoxInfo BottomText;
	public Box BoxPrefab;

	public BoxInfo BoxInfo;
	private Box highLightedBox;
	private Box selectedBox;
	private int selectedBoxId;

	private bool dosBoxEnabled;
	private bool isAITD1;
	private int detectedGame;
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

	void Start()
	{
		Directory.CreateDirectory("GAMEDATA");

		//check existing ETAGEXX folders
		floors = Directory.GetDirectories("GAMEDATA")
			.Select(x => Path.GetFileName(x))
			.Where(x => x.StartsWith("ETAGE", StringComparison.InvariantCultureIgnoreCase))
			.Select(x => int.Parse(x.Substring(5, 2)))
			.ToList();
		floor = floors.FirstOrDefault();
		detectedGame = DetectGame();
		isAITD1 = detectedGame == 1;

		CheckCommandLine();
		if (floors.Count > 0)
		{
			RefreshRooms();
		}
		ToggleMenuDOSBoxOptions(false);

		SetupBorder();
		linkToDosBoxTimer.Start();
		LinkToDosBox();
	}

	void SetupBorder()
	{
		var triangles = new int[]
		{
			0, 1, 5, 0, 5, 4,
			6, 3, 2, 6, 7, 3,
			0, 4, 2, 4, 6, 2,
			1, 3, 7, 5, 1, 7
		};

		const float outerSize = 32.768f;
		const float innerSize = outerSize + 0.3f;

		var vertices = new Vector3[]
		{
			new Vector3(-innerSize, 0f,  innerSize),
			new Vector3( innerSize, 0f,  innerSize),
			new Vector3(-innerSize, 0f, -innerSize),
			new Vector3( innerSize, 0f, -innerSize),

			new Vector3(-outerSize, 0f,  outerSize),
			new Vector3( outerSize, 0f,  outerSize),
			new Vector3(-outerSize, 0f, -outerSize),
			new Vector3( outerSize, 0f, -outerSize)
		};

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateBounds();
		Border.GetComponent<MeshFilter>().mesh = mesh;
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

		if (CameraFollow.Value == 1) //room
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
		bool showallrooms = ShowRooms.Value == 3 || ShowRooms.Value == 2;
		bool showallroomstransparent = ShowRooms.Value == 2;
		bool showcolliders = ShowRooms.Value != 0;
		bool showtiggers = ShowTriggers.BoolValue;

		int roomIndex = 0;
		foreach (Transform roomTransform in transform.Cast<Transform>().Where(x => x.name != "DELETED"))
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
					box.gameObject.SetActive(showtiggers && currentRoom);
				}

				if (box.name == "Camera")
				{
					box.gameObject.SetActive(ShowAreas.Value == 3 || (ShowAreas.Value == 1 && currentRoom) || (ShowAreas.Value == 2 && camerasPerRoom[room].Contains(box.ID)));
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

		//load cameras and rooms
		camerasPerRoom = new List<List<int>>();
		name = "FLOOR" + floor;

		if (detectedGame == 5)
		{
			LoadRoomsMulti(folder);
		}
		else
		{
			LoadRoomsSingle(folder);
		}
	}

	void LoadRoomsSingle(string folder)
	{
		string filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000000");
		byte[] buffer = File.ReadAllBytes(filePath);

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

		filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000001");
		buffer = File.ReadAllBytes(filePath);

		foreach (int cameraID in camerasPerRoom.SelectMany(x => x).Distinct())
		{
			int cameraHeader = buffer.ReadInt(cameraID * 4);
			LoadCamera(buffer, cameraHeader, cameraID);
		}
	}

	void LoadRoomsMulti(string folder)
	{
		foreach (var filePath in Directory.GetFiles(folder).Where(x => new FileInfo(x).Length > 0))
		{
			int room = int.Parse(Path.GetFileNameWithoutExtension(filePath), NumberStyles.HexNumber);
			byte[] buffer = File.ReadAllBytes(filePath);
			LoadRoom(buffer, 0, room);
		}

		folder = string.Format("GAMEDATA\\CAMSAL{0:D2}", floor);
		if (Directory.Exists(folder))
		{
			foreach (var filePath in Directory.GetFiles(folder).Where(x => new FileInfo(x).Length > 0))
			{
				int camera = int.Parse(Path.GetFileNameWithoutExtension(filePath), NumberStyles.HexNumber);
				byte[] buffer = File.ReadAllBytes(filePath);
				LoadCamera(buffer, 0, camera);
			}
		}
	}

	void LoadRoom(byte[] buffer, int roomheader, int currentroom)
	{
		//room
		GameObject roomObject = new GameObject();
		roomObject.name = "ROOM" + currentroom;
		roomObject.transform.parent = transform;

		Vector3 roomPosition = buffer.ReadVector(roomheader + 4);
		roomObject.transform.localPosition = new Vector3(roomPosition.x, roomPosition.y, -roomPosition.z) / 100.0f;

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

			Vector3 lower, upper;
			buffer.ReadBoundingBox(i + 0, out lower, out upper);

			Vector3 position = lower + upper;
			box.transform.localPosition = new Vector3(position.x, -position.y, position.z) / 2000.0f;
			box.transform.localScale = (upper - lower) / 1000.0f;

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

			Vector3 lower, upper;
			buffer.ReadBoundingBox(i + 0, out lower, out upper);

			Vector3 position = lower + upper;
			box.transform.localPosition = new Vector3(position.x, -position.y, position.z) / 2000.0f;
			box.transform.localScale = (upper - lower) / 1000.0f;

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

			int i = cameraHeader + 0x14 + k * (isAITD1 ? 12 : (detectedGame == 5 ? 22 : 16));
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
					short px = buffer.ReadShort(i + 0);
					short pz = buffer.ReadShort(i + 2);
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
				var room = transform.Cast<Transform>().FirstOrDefault(x => x.name == "ROOM" + cameraRoom);
				if(room == null) continue;

				int colorRGB = cameraColors[cameraID % cameraColors.Length];

				Box area = Instantiate(BoxPrefab);
				area.name = "Camera";
				area.transform.parent = room;
				area.transform.localPosition = Vector3.zero;
				area.Color = new Color32((byte)((colorRGB >> 16) & 0xFF), (byte)((colorRGB >> 8) & 0xFF), (byte)(colorRGB & 0xFF), 100);
				area.ID = cameraID;
				area.DosBox = GetComponent<DosBox>();
				MeshFilter filter = area.GetComponent<MeshFilter>();

				// Use the triangulator to get indices for creating triangles
				filter.sharedMesh = GetMeshFromPoints(points, indices);
				Destroy(area.gameObject.GetComponent<BoxCollider>());
				area.gameObject.AddComponent<MeshCollider>();

				//setup camera
				Vector3 cameraRotation = buffer.ReadVector(cameraHeader + 0);
				Vector3 cameraPosition = buffer.ReadVector(cameraHeader + 6);
				Vector3 cameraFocal = buffer.ReadVector(cameraHeader + 12);

				CameraHelper cameraHelper = GetComponent<CameraHelper>();
				Box camera = Instantiate(BoxPrefab);
				camera.name = "CameraFrustum";
				camera.transform.parent = room;
				camera.Color = new Color32(255, 128, 0, 255);
				camera.HighLight = true;
				cameraHelper.SetupTransform(camera, cameraPosition, cameraRotation, cameraFocal);
				area.Camera = camera;

				filter = camera.GetComponent<MeshFilter>();
				filter.sharedMesh = cameraHelper.CreateMesh(cameraFocal);
				Destroy(camera.gameObject.GetComponent<BoxCollider>());
				camera.gameObject.SetActive(false);
			}
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

	int DetectGame()
	{
		//detect game based on number of floors
		if (Directory.Exists("GAMEDATA\\ETAGE00") && Directory.GetFiles("GAMEDATA\\ETAGE00").Count() > 2)
			return 5; //TIME GATE
		else if (floors.Count >= 15)
			return 2;
		else if (floors.Count >= 14)
			return 3;
		else if (floors.Count == 1 && Directory.Exists("GAMEDATA\\ETAGE16"))
			return 4; //JITD
		else
			return 1;
	}

	bool IsPointVisible(Vector3 point)
	{
		Vector3 screen = Camera.main.WorldToViewportPoint(point);
		return screen.x >= 0.0f && screen.x <= 1.0f && screen.y >= 0.0f && screen.y <= 1.0f;
	}

	void Update()
	{
		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		if(mouseWheel != 0.0f)
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

		if (!menuEnabled && !GetComponent<WarpDialog>().WarpMenuEnabled)
		{
			//start drag
			if (Input.GetMouseButtonDown(0))
			{
				dragging = false;
				mousePosition = startDragPosition = Input.mousePosition;
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
					if((startDragPosition - newMousePosition).magnitude > 4.0f)
					{
						dragging = true;
					}
				}
			}
		}

		//menu
		if (Input.GetMouseButtonDown(1))
		{
			WarpDialog warpDialog = GetComponent<WarpDialog>();
			if(menuEnabled)
			{
				menuEnabled = false;
			}
			else if(warpDialog.WarpMenuEnabled)
			{
				warpDialog.WarpMenuEnabled = false;
				if(speedRunMode)
				{
					warpDialog.WarpActorBox = null; //reset to player
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
		dosBox.ShowAITD1Vars = dosBox.ShowAdditionalInfo && isAITD1 && dosBox.IsCDROMVersion;
		dosBox.SpeedRunMode = speedRunMode;

		dosBox.RefreshMemory();
		dosBox.CalculateFPS();
		dosBox.UpdateAllActors();
		dosBox.UpdateBoxInfo();
		if(isAITD1) dosBox.UpdateTargetSlot(highLightedBox);
		RefreshHighLightedBox();
		RefreshSelectedBox();

		//process keys
		if (!GetComponent<WarpDialog>().WarpMenuEnabled)
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
		if(!dosBoxEnabled && linkToDosBoxTimer.Elapsed > 1.0f)
		{
			LinkToDosBox();
			linkToDosBoxTimer.Restart();
		}
	}

	private void RefreshHighLightedBox()
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
			&& !menuEnabled && !GetComponent<WarpDialog>().WarpMenuEnabled)
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
					if(highLightedBox.name == "Camera")
					{
						highLightedBox.GetComponent<Renderer>().sharedMaterial.renderQueue = 3000;
					}
				}

				box.HighLight = true;

				if(box.name == "Camera")
				{
					box.GetComponent<Renderer>().sharedMaterial.renderQueue = 3500;
				}

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

	private void RefreshSelectedBox()
	{
		//toggle selected box
		if (Input.GetMouseButtonUp(0) && highLightedBox != null && !dragging
			&& !(GetComponent<WarpDialog>().WarpMenuEnabled	 //make sure it not possible to change actor when there is a click inside warp menu
				&& RectTransformUtility.RectangleContainsScreenPoint(GetComponent<WarpDialog>().Panel, Input.mousePosition)))
		{
			if (highLightedBox.name == "Camera")
			{
				RefreshSelectedCamera();
			}
			else
			{
				if (selectedBox != highLightedBox)
				{
					selectedBox = highLightedBox;
					selectedBoxId = highLightedBox.ID;
				}
				else
				{
					selectedBox = null;
					defaultBoxSelectionTimer.Restart();
				}
			}
		}

		if (selectedBox == null)
		{
			//if actor is no more available (eg : after room switch) search for it
			foreach (Box box in GetComponent<DosBox>().Boxes)
			{
				if (box != null && box.ID == selectedBoxId)
				{
					selectedBox = box;
					break;
				}
			}

			if(speedRunMode && defaultBoxSelectionTimer.Elapsed > 1.0f)
			{
				//select player by default
				selectedBox = GetComponent<DosBox>().Player;
				if (selectedBox != null)
				{
					selectedBoxId = selectedBox.ID;
				}
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

	public Transform GetRoom(int newfloor, int newroom)
	{
		if (floor == newfloor && newroom >= 0 && newroom < transform.childCount)
		{
			return transform.Cast<Transform>()
				.FirstOrDefault(x => x.name == "ROOM" + newroom);
		}
		return null;
	}

	public void CenterCamera(Vector2 position)
	{
		if (CameraFollow.Value == 2) //follow player
		{
			Camera.main.transform.position = new Vector3(position.x, Camera.main.transform.position.y, position.y);
		}
	}

	public Box GetSelectedBox()
	{
		return selectedBox;
	}

	public void LinkToDosBox()
	{
		if (!dosBoxEnabled)
		{
			dosBoxEnabled = GetComponent<DosBox>().LinkToDosBOX(floor, room, detectedGame);
			if (dosBoxEnabled)
			{
				//set follow mode to player
				CameraFollow.Value = 2;
				GetComponent<DosBox>().ResetCamera(floor, room);

				//select player by default
				GetComponent<DosBox>().UpdateAllActors();
				selectedBox = GetComponent<DosBox>().Player;
				if (selectedBox != null)
				{
					selectedBoxId = selectedBox.ID;
				}
			}
		}
		else
		{
			dosBoxEnabled = false;
			GetComponent<DosBox>().UnlinkDosBox();

			//follow player => room
			if (CameraFollow.Value == 2)
			{
				CameraFollow.Value = 1;
			}

			selectedBox = null;
			GetComponent<WarpDialog>().WarpMenuEnabled = false; //hide warp
		}

		Actors.SetActive(dosBoxEnabled && ShowActors.BoolValue);
		Border.SetActive(dosBoxEnabled);

		ToggleMenuDOSBoxOptions(dosBoxEnabled);
	}

	#endregion

	#region GUI

	private bool menuEnabled;

	public void SetCameraRotation(Slider slider)
	{
		Camera.main.transform.rotation = Quaternion.Euler(90.0f, 0.0f, slider.value * 22.5f);
	}

	private void ToggleMenuDOSBoxOptions(bool enabled)
	{
		ShowActors.transform.parent.gameObject.SetActive(enabled);
		ShowAdditionalInfo.transform.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	public void ProcessKey(string keyCode)
	{
		KeyCode keyCodeEnum = (KeyCode)Enum.Parse(typeof(KeyCode), keyCode, true);
		ProcessKey(keyCodeEnum);
	}

	public void ProcessKey(KeyCode keyCode)
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
				CameraFollow.Value = (CameraFollow.Value + 1) % (!dosBoxEnabled ? 2 : 3);
				if (CameraFollow.Value == 1) //room
				{
					CenterCamera(room);
				}
				else if (CameraFollow.Value == 2) //player
				{
					//make sure camear snap back
					GetComponent<DosBox>().ResetCamera(floor, room);
				}
				break;

			case KeyCode.R:
				ShowRooms.Value = (ShowRooms.Value + 1) % 4;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.C:
				ShowAreas.Value = (ShowAreas.Value + 1) % 4;
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

			case KeyCode.E:
				ShowAdditionalInfo.BoolValue = !ShowAdditionalInfo.BoolValue;
				break;

			case KeyCode.Mouse2:
				//reset zoom
				Vector3 pos = Camera.main.transform.position;
				Camera.main.transform.position = new Vector3(pos.x, defaultCameraZoom, pos.z);
				float planeWidth = Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad / 2.0f);
				Camera.main.orthographicSize = defaultCameraZoom * planeWidth;
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

	void CheckCommandLine()
	{
		var args = System.Environment.GetCommandLineArgs();
		if (args.Contains("-speedrun", StringComparer.InvariantCultureIgnoreCase))
		{
			speedRunMode = true;
			defaultCameraZoom = 10.0f / Mathf.Pow(0.9f, 5.0f);
			ProcessKey(KeyCode.Mouse2); //reset camera zoom
			ProcessKey(KeyCode.D); //camera perspective
			ProcessKey(KeyCode.C); //camera areas for current room
			ProcessKey(KeyCode.E); //extra info
		}
	}

	Mesh GetMeshFromPoints(List<Vector2> vertices2D, List<int> indices)
	{
		// Create the Vector3 vertices
		Vector3[] vertices = new Vector3[vertices2D.Count];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = new Vector3(vertices2D[i].x, 0, vertices2D[i].y);
		}

		// Create the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = indices.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	void RefreshSelectedCamera()
	{
		if(highLightedBox != currentCamera)
		{
			SetCameraVisibility(currentCamera, false);
			SetCameraVisibility(highLightedBox, true);
			currentCamera = highLightedBox;
		}
		else
		{
			SetCameraVisibility(highLightedBox, false);
			currentCamera = null;
		}
	}

	void SetCameraVisibility(Box box, bool visible)
	{
		if(box != null)
		{
			box.Camera.gameObject.SetActive(visible);
		}
	}
}
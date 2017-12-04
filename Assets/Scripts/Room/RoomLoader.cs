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
	private KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private List<int> floors = new List<int>();
	private BoxComparer boxComparer = new BoxComparer();

	public Text LeftText;
	public BoxInfo BottomText;
	public Box BoxPrefab;

	public BoxInfo BoxInfo;
	private Box HighLightedBox;
	private Box SelectedBox;
	private int SelectedBoxId;

	private bool DosBoxEnabled;
	public GameObject Actors;

	public RectTransform Panel;
	public Button ShowVars;
	public ToggleButton LinkToDOSBox;
	public ToggleButton ShowAdditionalInfo;
	public ToggleButton ShowActors;
	public Slider CameraRotation;
	public ToggleButton ShowTriggers;
	public ToggleButton ShowRooms;
	public ToggleButton ShowAreas;
	public ToggleButton CameraFollow;
	public ToggleButton CameraMode;

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
		ToggleMenuDOSBoxOptions(false);
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
			//roomTransform.gameObject.SetActive(showallrooms || currentRoom);
			foreach (Box box in roomTransform.GetComponentsInChildren<Box>(true))
			{
				if (box.name == "Trigger")
				{
					box.gameObject.SetActive(showtiggers && currentRoom);
				}

				if (box.name == "Camera")
				{
					box.gameObject.SetActive(ShowAreas.Value == 2 || (ShowAreas.Value == 1 && currentRoom));
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
		int maxrooms = Utils.ReadInt(allPointsA, 0) / 4;
		for (int currentroom = 0; currentroom < maxrooms; currentroom++)
		{
			int i = currentroom * 4;
			int roomheader = Utils.ReadInt(allPointsA, i + 0);
			if (roomheader > allPointsA.Length || roomheader == 0)
			{
				//all rooms parsed
				break;
			}

			//room
			GameObject roomObject = new GameObject();
			roomObject.name = "ROOM" + currentroom;
			roomObject.transform.parent = transform;

			int roomx = Utils.ReadShort(allPointsA, roomheader + 4);
			int roomy = Utils.ReadShort(allPointsA, roomheader + 6);
			int roomz = Utils.ReadShort(allPointsA, roomheader + 8);

			roomObject.transform.localPosition = new Vector3(roomx, roomy, -roomz) / 100.0f;

			//colliders
			i = roomheader + Utils.ReadShort(allPointsA, roomheader + 0);
			int totalpoint = Utils.ReadShort(allPointsA, i + 0);
			i += 2;

			for (int count = 0; count < totalpoint; count++)
			{
				Box box = Instantiate(BoxPrefab);
				box.name = "Collider";
				box.Room = currentroom;
				box.transform.parent = roomObject.transform;

				box.transform.localPosition = new Vector3((Utils.ReadShort(allPointsA, i + 0) + Utils.ReadShort(allPointsA, i + 2)),
					-(Utils.ReadShort(allPointsA, i + 4) + Utils.ReadShort(allPointsA, i + 6)),
					(Utils.ReadShort(allPointsA, i + 8) + Utils.ReadShort(allPointsA, i + 10))) / 2000.0f;

				box.transform.localScale = new Vector3((Utils.ReadShort(allPointsA, i + 2) - Utils.ReadShort(allPointsA, i + 0)),
					(Utils.ReadShort(allPointsA, i + 6) - Utils.ReadShort(allPointsA, i + 4)),
					(Utils.ReadShort(allPointsA, i + 10) - Utils.ReadShort(allPointsA, i + 8))) / 1000.0f;

				box.ID = Utils.ReadShort(allPointsA, i + 12);
				box.Flags = Utils.ReadShort(allPointsA, i + 14);

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
			i = roomheader + Utils.ReadShort(allPointsA, roomheader + 2);
			totalpoint = Utils.ReadShort(allPointsA, i + 0);
			i += 2;

			for (int count = 0; count < totalpoint; count++)
			{

				Box box = Instantiate(BoxPrefab);
				box.name = "Trigger";
				box.Room = currentroom;
				box.transform.parent = roomObject.transform;

				box.transform.localPosition = new Vector3((Utils.ReadShort(allPointsA, i + 0) + Utils.ReadShort(allPointsA, i + 2)),
					-(Utils.ReadShort(allPointsA, i + 4) + Utils.ReadShort(allPointsA, i + 6)),
					(Utils.ReadShort(allPointsA, i + 8) + Utils.ReadShort(allPointsA, i + 10))) / 2000.0f;

				box.transform.localScale = new Vector3((Utils.ReadShort(allPointsA, i + 2) - Utils.ReadShort(allPointsA, i + 0)),
					(Utils.ReadShort(allPointsA, i + 6) - Utils.ReadShort(allPointsA, i + 4)),
					(Utils.ReadShort(allPointsA, i + 10) - Utils.ReadShort(allPointsA, i + 8))) / 1000.0f;

				box.ID = Utils.ReadShort(allPointsA, i + 12);
				box.Flags = Utils.ReadShort(allPointsA, i + 14);

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
			int cameraCount = Utils.ReadShort(allPointsA, roomheader + 10);
			List<int> cameraInRoom = new List<int>();
			for (int cameraIndex = 0; cameraIndex < cameraCount; cameraIndex++)
			{
				int cameraID = Utils.ReadShort(allPointsA, roomheader + cameraIndex * 2 + 12);	 //camera
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
				int cameraHeader = Utils.ReadShort(allPointsB, cameraID * 4 + 0);
				int numentries = Utils.ReadShort(allPointsB, cameraHeader + 0x12);

				List<Vector2> points = new List<Vector2>();
				List<int> indices = new List<int>();

				for (int k = 0; k < numentries; k++)
				{
					int i = cameraHeader + 0x14 + k * (isAITD1 ? 12 : 16);
					int cameraRoom = Utils.ReadShort(allPointsB, i + 0);

					if (cameraRoom == roomIndex)
					{
						i = cameraHeader + Utils.ReadShort(allPointsB, i + 4);
						int totalAreas = Utils.ReadShort(allPointsB, i + 0);
						i += 2;

						for (int g = 0; g < totalAreas; g++)
						{
							int totalPoints = Utils.ReadShort(allPointsB, i + 0);
							i += 2;

							List<Vector2> pts = new List<Vector2>();
							for (int u = 0; u < totalPoints; u++)
							{
								short px = Utils.ReadShort(allPointsB, i + 0);
								short pz = Utils.ReadShort(allPointsB, i + 2);
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
		if (floors.Count >= 15)
			return 2;
		else if (floors.Count >= 14)
			return 3;
		else
			return 1;
	}

	void Update()
	{
		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
		if (mouseWheel > 0.0f)
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
		else if (mouseWheel < 0.0f)
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

		if (Input.GetMouseButtonDown(2))
		{
			//reset zoom
			Camera.main.transform.position = new Vector3(0.0f, 10.0f, 0.0f);
		}

		if (!menuEnabled && !GetComponent<WarpDialog>().warpMenuEnabled)
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
			WarpDialog warpDialog = GetComponent<WarpDialog>();
			if(menuEnabled)
			{
				menuEnabled = false;
			}
			else if(warpDialog.warpMenuEnabled)
			{
				warpDialog.warpMenuEnabled = false;
			}
			else if (DetectGame() == 1 && HighLightedBox != null && HighLightedBox.name == "Actor")
			{
				warpDialog.LoadActor(HighLightedBox);
				warpDialog.warpMenuEnabled = true;
			}
			else if (!GetComponent<Vars>().enabled)
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

		GetComponent<DosBox>().CalculateFPS();
		GetComponent<DosBox>().UpdateAllActors();
		GetComponent<DosBox>().UpdateBoxInfo();
		RefreshHighLightedBox();
		RefreshSelectedBox();

		if (!GetComponent<WarpDialog>().warpMenuEnabled)
		{
			foreach (var key in keyCodes)
			{
				if (Input.GetKeyDown(key))
				{
					ProcessKey(key);
				}
			}
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
			&& !menuEnabled && !GetComponent<WarpDialog>().warpMenuEnabled)
		{
			//sort colliders by priority
			boxComparer.Room = room;
			Array.Sort(hitInfos, boxComparer);

			Box box = hitInfos[0].collider.GetComponent<Box>();
			if (box != HighLightedBox)
			{
				if (HighLightedBox != null)
				{
					HighLightedBox.HighLight = false;
					if(HighLightedBox.name == "Camera")
					{
						HighLightedBox.GetComponent<Renderer>().sharedMaterial.renderQueue = 3000;
					}
				}

				box.HighLight = true;

				if(box.name == "Camera")
				{
					box.GetComponent<Renderer>().sharedMaterial.renderQueue = 3500;
				}

				HighLightedBox = box;
			}

			//display info
			Vector3 position = Camera.main.WorldToScreenPoint(box.GetComponent<Renderer>().bounds.center);
			BoxInfo.transform.position = new Vector3(position.x, position.y, 0.0f);

			//text
			box.UpdateText(BoxInfo, GetComponent<DosBox>().InternalTimer);
		}
		else if (HighLightedBox != null)
		{
			HighLightedBox.HighLight = false;
			HighLightedBox = null;
			BoxInfo.Clear(true);
		}
	}

	private void RefreshSelectedBox()
	{
		//toggle selected box
		if (Input.GetMouseButtonDown(0) && HighLightedBox != null
			&& !(GetComponent<WarpDialog>().warpMenuEnabled	 //make sure it not possible to change actor when there is a click inside warp menu
				&& RectTransformUtility.RectangleContainsScreenPoint(GetComponent<WarpDialog>().Panel, Input.mousePosition)))
		{
			if (SelectedBox != HighLightedBox)
			{
				SelectedBox = HighLightedBox;
				SelectedBoxId = HighLightedBox.ID;
			}
			else
			{
				SelectedBox = null;
			}
		}

		if (!DosBoxEnabled)
		{
			SelectedBox = null;
		}

		if (SelectedBox != null)
		{
			if (SelectedBox.ID == SelectedBoxId)
			{
				//display selected box info
				SelectedBox.UpdateText(BottomText, GetComponent<DosBox>().InternalTimer);
			}
			else
			{
				//if actor is no more available (eg : after room switch) search for it
				foreach (Box box in Actors.GetComponentsInChildren<Box>(true))
				{
					if (box.ID == SelectedBoxId)
					{
						SelectedBox = box;
						break;
					}
				}
			}
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
			return transform.GetChild(newroom);
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
		return SelectedBox;
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
		ShowVars.transform.gameObject.SetActive(enabled);
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
			case KeyCode.L:
				if (!DosBoxEnabled)
				{
					bool result = (GetComponent<DosBox>().LinkToDosBOX(floor, room));

					//set follow mode to player
					CameraFollow.Value = 2;
					GetComponent<DosBox>().ResetCamera(floor, room);

					Actors.SetActive(result);
					DosBoxEnabled = result;

					//select player by default
					if (SelectedBox == null)
					{
						GetComponent<DosBox>().UpdateAllActors();
						SelectedBox = GetComponent<DosBox>().Player;
						if (SelectedBox != null)
						{
							SelectedBoxId = SelectedBox.ID;
						}
					}
				}
				else
				{
					//follow player => room
					if (CameraFollow.Value == 2)
					{
						CameraFollow.Value = 1;
					}
					GetComponent<DosBox>().UnlinkDosBox();

					Actors.SetActive(false);
					DosBoxEnabled = false;
				}
				LinkToDOSBox.BoolValue = DosBoxEnabled;
				ToggleMenuDOSBoxOptions(DosBoxEnabled);
				menuEnabled = false; //hide menu
				break;

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
				CameraFollow.Value = (CameraFollow.Value + 1) % (!DosBoxEnabled ? 2 : 3);
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

			case KeyCode.V:
				if (DetectGame() == 1)
				{
					GetComponent<Vars>().enabled = !GetComponent<Vars>().enabled;
					menuEnabled = false;
				}
				break;

			case KeyCode.R:
				ShowRooms.Value = (ShowRooms.Value + 1) % 4;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.C:
				ShowAreas.Value = (ShowAreas.Value + 1) % 3;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.T:
				ShowTriggers.BoolValue = !ShowTriggers.BoolValue;
				SetRoomObjectsVisibility(room);
				break;

			case KeyCode.A:
				ShowActors.BoolValue = !ShowActors.BoolValue;
				Actors.SetActive(ShowActors.BoolValue);
				break;

			case KeyCode.E:
				GetComponent<DosBox>().ShowAdditionalInfo = !GetComponent<DosBox>().ShowAdditionalInfo;
				ShowAdditionalInfo.BoolValue = !ShowAdditionalInfo.BoolValue;
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
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
	private string[] cardinalPositions = new [] { "N", "E", "S", "W" };
	private int[] roomsPerFloor = new [] { 1, 8, 11, 14, 2, 12, 7, 2 };
	private int[] cameraColors = new [] { 0xFF8080, 0x789CF0, 0xB0DE6F, 0xCC66C0, 0x5DBAAB, 0xF2BA79, 0x8E71E3, 0x6ED169, 0xBF6080, 0x7CCAF7 };
	private Vector3 mousePosition;

	private int showrooms = 1;
	private bool showtriggers = true;
	private int showareas = 0;
	private int cameraFollow = 1;

	private int linkfloor = 0;
	private int linkroom = 0;
	private Func<byte[], int, int, bool> readMemoryFunc;
	private byte[] memory = new byte[160 * 50];

	public GUIText LeftText;
	public GUIText RightText;
	public GUIText BottomText;
	public GameObject Actors;
	public GameObject arrow;
	public Box BoxPrefab;

	public GUIText BoxInfo;
	private Box HighLightedBox;
	private Box SelectedBox;

	public static short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	void Start()
	{
		//game has maximum 50 actors
		for (int i = 0; i < 50; i++)
		{            
			Box box = Instantiate(BoxPrefab);
			box.transform.parent = Actors.transform;                
			box.name = "Actor";           
		}                       

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

		if (cameraFollow == 1 && transform.childCount > 0)
		{
			Transform roomTransform = transform.Find("ROOM" + room);
			if (roomTransform != null)
			{
				//center camera on current room
				Vector3 roomPosition = roomTransform.localPosition;
				Camera.main.transform.position = new Vector3(roomPosition.x, Camera.main.transform.position.y, roomPosition.z);
			}
		}
	}

	void SetRoomObjectsVisibility(int room)
	{		
		bool showallrooms = showrooms == 0 || showrooms == 1;
		bool showallroomstransparent = showrooms == 1;
		bool showcolliders = showrooms != 3;

		foreach (Transform roomTransform in transform)
		{
			bool currentRoom = roomTransform.name == "ROOM" + room;
			//roomTransform.gameObject.SetActive(showallrooms || currentRoom);
			foreach (Box box in roomTransform.GetComponentsInChildren<Box>(true))
			{
				if (box.name == "Trigger")
				{
					box.gameObject.SetActive(showtriggers && currentRoom);
				}

				if (box.name == "Camera")
				{
					box.gameObject.SetActive(showareas == 1 || (showareas == 2 && currentRoom));
				} 

				if (box.name == "Collider")
				{
					box.gameObject.SetActive(showcolliders && (showallrooms || currentRoom));
					box.Alpha = (byte)((showallroomstransparent && !currentRoom) ? 40 : 255);
				}
			}
		}
	}

	void LoadFloor(int floor)
	{           
		//check folder  
		string folder = string.Format("ETAGE{0:D2}", floor);   
		if (!Directory.Exists(folder))
		{
			LeftText.text = string.Format("Cannot find folder {0}", folder);
			return;
		}       

		//load file
		string filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000000");
		byte[] allPointsA = File.ReadAllBytes(filePath);

		name = "FLOOR" + floor;
		for (int currentroom = 0; currentroom < roomsPerFloor[floor]; currentroom++)
		{
			int i = currentroom * 4;
			int roomheader = ReadShort(allPointsA[i + 0], allPointsA[i + 1]);

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

				//custom trigger or exit
				box.Color = new Color32(255, 0, 0, 45);
				if (box.Flags == 9 || box.Flags == 10)
				{
					box.Color = new Color32(255, 128, 0, 50);
				}

				i += 16;
			}                       

			//cameras                   
			filePath = Directory.GetFiles(folder).FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == "00000001");
			byte[] allPointsB = File.ReadAllBytes(filePath);

			int cameraCount = ReadShort(allPointsA[roomheader + 10], allPointsA[roomheader + 11]);
			for (int cameraIndex = 0; cameraIndex < cameraCount; cameraIndex++)
			{        
				List<Vector2> points = new List<Vector2>();
				List<int> indices = new List<int>();              

				int cameraID = ReadShort(allPointsA[roomheader + cameraIndex * 2 + 12], allPointsA[roomheader + cameraIndex * 2 + 13]);  //camera
				int cameraHeader = ReadShort(allPointsB[cameraID * 4 + 0], allPointsB[cameraID * 4 + 1]);                 

				int numentries = ReadShort(allPointsB[cameraHeader + 0x12], allPointsB[cameraHeader + 0x13]);               
				for (int k = 0; k < numentries; k++)
				{              
					i = cameraHeader + 0x14 + k * 12; 
					int cameraRoom = ReadShort(allPointsB[i + 0], allPointsB[i + 1]);

					if (cameraRoom == currentroom)
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
					box.Room = currentroom;
					box.transform.parent = roomObject.transform;
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

	void StopCameraFollowPlayer()
	{
		if(cameraFollow == 2)   
			cameraFollow = 1;    
	}

	void Update()
	{       
		if (Input.GetKeyDown(KeyCode.Home))
		{
			if (floor > 0)
			{                                
				floor--;
				room = 0;
				RefreshRooms();  
				StopCameraFollowPlayer();     
			}
		}

		if (Input.GetKeyDown(KeyCode.End))
		{
			if (floor < 7)
			{                                
				floor++;
				room = 0;
				RefreshRooms();
				StopCameraFollowPlayer();       
			}
		}

		if (Input.GetKeyDown(KeyCode.PageUp))
		{
			if (room > 0)
			{                                                                                 
				room--;
				RefreshRooms();
				StopCameraFollowPlayer();      
			}
		}

		if (Input.GetKeyDown(KeyCode.PageDown))
		{                       
			if (room < (roomsPerFloor[floor] - 1))
			{  
				room++;
				RefreshRooms();
				StopCameraFollowPlayer();
			}
		}

		if (Input.GetKeyDown(KeyCode.C))
		{
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
		}

		if (Input.GetKeyDown(KeyCode.F))
		{
			cameraFollow = (cameraFollow+1)%3;
			linkfloor = -1;
			linkroom = -1;
		}
            
		if (Input.GetKeyDown(KeyCode.H))
		{
			showrooms = (showrooms + 1) % 4;
			SetRoomObjectsVisibility(room);
		}
            
		if (Input.GetKeyDown(KeyCode.T))
		{
			showtriggers = !showtriggers; 
			SetRoomObjectsVisibility(room);
		}

		if (Input.GetKeyDown(KeyCode.A))
		{
			showareas = (showareas + 1) % 3;
			SetRoomObjectsVisibility(room);
		}

		if (Input.GetKeyDown(KeyCode.J))
		{
			if (readMemoryFunc != null)
			{
				Actors.SetActive(!Actors.activeSelf);
			}
		}

		if (Input.GetKey(KeyCode.Escape) && Screen.fullScreen)
		{
			Application.Quit();
		}
            
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

		if (Input.GetKeyDown(KeyCode.L))
		{                                           
			if (readMemoryFunc == null)
			{   
				//search player position in DOSBOX                          
				string errorMessage;
				if (MemoryRead.GetAddress("DOSBOX", new byte[]
					{
						0x9F, 0x0C, 0x00, 0x00, 0xF4, 0xF9, 
						0x9F, 0x0C, 0x00, 0x00, 0xF4, 0xF9
					}, out errorMessage, out readMemoryFunc))
				{
					//force reload
					linkfloor = -1;
					linkroom = -1;
					Actors.SetActive(true);
				}
				else
				{
					RightText.text = errorMessage;
				}                                   
			}
			else
			{
				readMemoryFunc = null;
				RightText.text = string.Empty;
				Actors.SetActive(false);
			}                
		}

		if (Input.GetKeyDown(KeyCode.Insert))
		{
			Camera.main.transform.Rotate(0.0f, 0.0f, 22.5f);
		}

		if (Input.GetKeyDown(KeyCode.Delete))
		{
			Camera.main.transform.Rotate(0.0f, 0.0f, -22.5f);
		}
                   
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			SceneManager.LoadScene("model");
		}  

		//start drag
		if (Input.GetMouseButtonDown(0))
		{
			mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y);
		}              

		//dragging
		if (Input.GetMouseButton(0))
		{
			Vector3 newMousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y);
			if (newMousePosition != this.mousePosition)
			{
				Vector3 mouseDelta = Camera.main.ScreenToWorldPoint(this.mousePosition) - Camera.main.ScreenToWorldPoint(newMousePosition);

				Camera.main.transform.position += mouseDelta;
				mousePosition = newMousePosition;
				StopCameraFollowPlayer(); 
			}
		}

		ProcessActors();

		RefreshHighLightedBox();          
	}

	private int BoxComparer(RaycastHit a, RaycastHit b)
	{
		// check distance
		if(Mathf.Abs(a.distance - b.distance) >= 0.0005f)
		{
			return a.distance.CompareTo(b.distance);
		}

		//if objects are too close each other, check current room
		int aCurrentRoom = a.collider.GetComponent<Box>().Room == room ? 0 : 1;
		int bCurrentRoom = b.collider.GetComponent<Box>().Room == room ? 0 : 1;
		if(aCurrentRoom != bCurrentRoom)
		{
			return aCurrentRoom.CompareTo(bCurrentRoom);
		}

		return 0;
	}

	private void RefreshHighLightedBox()
	{
		
		Vector3 mousePosition = Input.mousePosition;
		RaycastHit[] hitInfos = Physics.RaycastAll(Camera.main.ScreenPointToRay(mousePosition));

		if (hitInfos.Length > 0)
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

			if (Input.GetMouseButtonDown(1))
			{
				if (SelectedBox != HighLightedBox)
					SelectedBox = HighLightedBox;
				else
					SelectedBox = null;
			}

			//display info
			Vector3 position = Camera.main.WorldToScreenPoint(box.GetComponent<Renderer>().bounds.center);
			BoxInfo.transform.position = new Vector3(position.x / Screen.width, position.y / Screen.height, 0.0f);  

			//text
			BoxInfo.text = box.ToString();
		}
		else
		{
			if (HighLightedBox != null)
			{
				HighLightedBox.HighLight = false;
				HighLightedBox = null;
				BoxInfo.text = string.Empty;
			}

			if (Input.GetMouseButtonDown(1))
			{
				SelectedBox = null;
			}
		}  

		if (SelectedBox != null)
		{
			BottomText.text = SelectedBox.ToString();
		}
		else
		{
			BottomText.text = string.Empty;
		}
	}

	private void ProcessActors()
	{
		if (readMemoryFunc != null)
		{  
			if (readMemoryFunc(memory, -28 - 160, memory.Length))
			{                
				int i = 0;
				foreach (Box box in Actors.GetComponentsInChildren<Box>(true))
				{                                                           
					int k = i * 160;
					int floorNumber = ReadShort(memory[k + 46], memory[k + 47]);
					int roomNumber = ReadShort(memory[k + 48], memory[k + 49]);

					int objectid = ReadShort(memory[k + 0], memory[k + 1]);
					int body = ReadShort(memory[k + 2], memory[k + 3]);
					bool isActive = objectid != -1;


					//player 
					if (isActive && objectid == 1)
					{
						//automatically switch room and floor (has to be done before setting objects positions)
						if (linkfloor != floorNumber || linkroom != roomNumber)
						{                           
							linkfloor = floor = floorNumber;
							linkroom = room = roomNumber;

							if (linkfloor >= 0 && linkfloor <= 7
							                         && linkroom >= 0 && linkroom < roomsPerFloor[floor])
							{                 
								RefreshRooms();
							}                                   
						}
					}

					if (isActive && floorNumber == floor && roomNumber < roomsPerFloor[floor])
					{
						//local position
						int w = k + 8;
						int x = (ReadShort(memory[w + 0], memory[w + 1]) + ReadShort(memory[w + 2], memory[w + 3])) / 2;
						int y = (ReadShort(memory[w + 4], memory[w + 5]) + ReadShort(memory[w + 6], memory[w + 7])) / 2;
						int z = (ReadShort(memory[w + 8], memory[w + 9]) + ReadShort(memory[w + 10], memory[w + 11])) / 2;

						//local to global position
						Transform roomObject = transform.GetChild(roomNumber); //transform.Find("ROOM" + roomNumber);
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

						box.ID = objectid;
						box.Body = body;
						box.Room = roomNumber;
						box.Flags = ReadShort(memory[k + 4], memory[k + 5]);
						box.Life = ReadShort(memory[k + 52], memory[k + 53]);
						box.Anim = ReadShort(memory[k + 62], memory[k + 63]);
						box.Frame = ReadShort(memory[k + 74], memory[k + 75]);
						box.Speed = ReadShort(memory[k + 116], memory[k + 118]);
                                               
						//player
						if (objectid == 1)
						{
							float angle = ReadShort(memory[k + 42], memory[k + 43]) * 360 / 1024.0f; 

							angle = (540.0f - angle)%360.0f;
							                    							                                           
							float sideAngle = (angle + 45.0f) % 90.0f - 45.0f;

							int cardinalPos = (int)Math.Floor((angle+45.0f)/90);
							
							RightText.text = string.Format("Position: {0} {1} {2}\nAngle: {3:N1} {4:N1}{5}", x, y, z, angle, sideAngle, cardinalPositions[cardinalPos%4]);
                     
							if (cameraFollow == 2) //follow player
							{
								Camera.main.transform.position = new Vector3(box.transform.position.x, 
									Camera.main.transform.position.y,
									box.transform.position.z
								);
							}

							if (Camera.main.orthographic)
							{
								//make sure player is always visible 
								box.transform.localScale = new Vector3(box.transform.localScale.x, box.transform.localScale.y * 5.0f, box.transform.localScale.z);
							}

							//follow player
							arrow.transform.position = box.transform.position + new Vector3(0.0f, box.transform.localScale.y / 2.0f + 0.001f, 0.0f);
							//face camera
							arrow.transform.rotation = Quaternion.AngleAxis(90.0f, -Vector3.left); 
							arrow.transform.rotation *= Quaternion.AngleAxis(-angle, Vector3.forward);

							//player is white
							box.Color = new Color32(255, 255, 255, 255);
						}
						else
						{
							//other objects are green
							box.Color = new Color32(0, 128, 0, 255);
						}

						box.gameObject.SetActive(true);
					}
					else
					{
						box.gameObject.SetActive(false);
					}

					i++;
				}
			}
		}

		//arrow is only active if actors are active and player is active
		arrow.SetActive(Actors.activeSelf && Actors.transform.GetComponentsInChildren<Box>()
                .Where(x => x.ID == 1).Select(x => x.gameObject.activeSelf).FirstOrDefault());

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
		Mesh msh = new Mesh();
		msh.vertices = vertices;
		msh.triangles = indices.ToArray();
		msh.RecalculateNormals();
		msh.RecalculateBounds();

		return msh;

	}
}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System;
using UnityEngine.SceneManagement;

public class ModelLoader : MonoBehaviour
{
	private int modelIndex = 0;
	private int modelFolderIndex = 0;
	private KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();

	private string[] modelFolders = new string[] { "GAMEDATA\\LISTBODY", "GAMEDATA\\LISTBOD2" };
	private List<string> modelFiles = new List<string>();
	private List<Color32> paletteColors = new List<Color32>();
	public GUIText LeftText;
	public Mesh SphereMesh;
	public Mesh CubeMesh;

	private Vector3 cameraSettings = new Vector3(0.0f, 0.0f, 2.0f);

	private Vector3 mousePosition;
	private bool autoRotate;
	private bool enableNoise = true;

	public static short ReadShort(byte a, byte b)
	{
		unchecked
		{
			return (short)(a | b << 8);
		}
	}

	void LoadBody(string filename, bool reset = true)
	{
		LeftText.text = Path.GetFileName(Path.GetDirectoryName(filename)) + " " + modelIndex + "/" + (modelFiles.Count - 1);

		//camera
		if(reset)
		{
			autoRotate = true;
		}

		//clear model
		MeshFilter filter = this.gameObject.GetComponent<MeshFilter>();
		filter.sharedMesh = null;
            
		//load data
		byte[] allbytes = File.ReadAllBytes(filename);
		int i = 0;

		//header
		int flags = ReadShort(allbytes[i + 0], allbytes[i + 1]);
		i += 0xE;
		i += ReadShort(allbytes[i + 0], allbytes[i + 1]) + 2;

		//vertexes
		int count = ReadShort(allbytes[i + 0], allbytes[i + 1]);
		i += 2;
               
		List<Vector3> vertices = new List<Vector3>();
		for (int j = 0; j < count; j++)
		{
			Vector3 position = new Vector3(ReadShort(allbytes[i + 0], allbytes[i + 1]), -ReadShort(allbytes[i + 2], allbytes[i + 3]), ReadShort(allbytes[i + 4], allbytes[i + 5]));
			vertices.Add(position / 1000.0f);
			i += 6;
		}

		//check if model has bones 
		if ((flags & 2) == 2)
		{
			//bones
			count = ReadShort(allbytes[i + 0], allbytes[i + 1]);
			i += 2;
			i += count * 2;
           
			for (int n = 0; n < count; n++)
			{                
				int startindex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
				int numpoints = ReadShort(allbytes[i + 2], allbytes[i + 3]);
				int boneindex = ReadShort(allbytes[i + 4], allbytes[i + 5]) / 6;

				//apply bone transformation
				Vector3 position = vertices[boneindex];
				for (int u = 0; u < numpoints; u++)
				{
					vertices[startindex] += position;
					startindex++;
				}

				i += 0x10;
			}
		}

		//compute line size 
		Bounds bounds = new Bounds();
		foreach (Vector3 vector in vertices)
		{
			bounds.Encapsulate(vector);
		}
		float linesize = bounds.size.magnitude / 250.0f;
		float noisesize = 0.8f / bounds.size.magnitude;

		//primitives
		count = ReadShort(allbytes[i + 0], allbytes[i + 1]);
		i += 2;

		List<Vector3> allVertices = new List<Vector3>();
		List<Color32> colors = new List<Color32>();
		List<int> indices = new List<int>();
		List<Vector2> uv = new List<Vector2>();

		for (int n = 0; n < count; n++)
		{
			int primitiveType = allbytes[i + 0];
			i++;

			switch (primitiveType)
			{
			//line
				case 0:
					{
						i++;
						int colorIndex = allbytes[i + 0];
						i += 2;

						Color32 color = paletteColors[colorIndex];   
						int pointIndexA = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
						int pointIndexB = ReadShort(allbytes[i + 2], allbytes[i + 3]) / 6;
						Vector3 directionVector = vertices[pointIndexA] - vertices[pointIndexB];
						Vector3 middle = (vertices[pointIndexA] + vertices[pointIndexB]) / 2.0f;
						Quaternion rotation = Quaternion.LookRotation(directionVector);                                       

						uv.AddRange(CubeMesh.uv);
						indices.AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x =>
                        rotation * (Vector3.Scale(x, new Vector3(linesize, linesize, directionVector.magnitude)))
								+ middle));
						colors.AddRange(CubeMesh.vertices.Select(x => color));                                       
						i += 4;
						break;
					}
			//polygon
				case 1:
					{                       
						int numPoints = allbytes[i + 0];
						int polyType = allbytes[i + 1];
						int colorIndex = allbytes[i + 2];
						i += 3;

						Color32 color = paletteColors[colorIndex];

						if (polyType == 1 && enableNoise)
						{
							//noise
							color.a = 254;
							color.r = (byte)((colorIndex % 16) * 16);
							color.g = (byte)((colorIndex / 16) * 16);    
						}
						else if (polyType == 2)
						{
							//transparency
							color.a = 128;
						}

						//add vertices   
						List<Vector3> polyVertices = new List<Vector3>();
						int verticesCount = allVertices.Count;
						for (int m = 0; m < numPoints; m++)
						{
							int pointIndex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
							i += 2;

							colors.Add(color);
							allVertices.Add(vertices[pointIndex]);  
							polyVertices.Add(vertices[pointIndex]);  						
						} 

						if(polyType == 1 && enableNoise)
						{
							//compute UV coordinates
							int lastPoly = polyVertices.Count - 1;
							Vector3 up, left, forward;

							do
							{
								Vector3 a = polyVertices[0];
								Vector3 b = polyVertices[1];
								Vector3 c = polyVertices[lastPoly];

								left = (b - a).normalized;
								forward = (c - a).normalized;
								up = Vector3.Cross(left, forward).normalized;
								left = Vector3.Cross(up, forward).normalized;
								lastPoly--;
							} while(up == Vector3.zero && lastPoly > 1);
												
							foreach (Vector3 poly in polyVertices)
							{
								uv.Add(new Vector2(
									Vector3.Dot(poly, left) * noisesize ,
									Vector3.Dot(poly, forward) * noisesize
								));
							}
						}
						else
						{
							uv.AddRange(polyVertices.Select(x => Vector2.zero));
						}

						//triangulate       
						int v0 = 0;
						int v1 = 1;
						int v2 = numPoints - 1;
						bool swap = true;

						while (v1 < v2)
						{                       
							indices.Add(verticesCount + v0);                
							indices.Add(verticesCount + v1); 
							indices.Add(verticesCount + v2); 

							if (swap)
							{
								v0 = v1;
								v1++;
							}
							else
							{
								v0 = v2;
								v2--;
							}

							swap = !swap;
						}   

						break;
					}
			//sphere
				case 3: 
					{
						int polyType = allbytes[i];
						i++;
						int colorIndex = allbytes[i];
						Color32 color = paletteColors[colorIndex];
						i += 2;


						if (polyType == 1)
						{
							//noise
							color.a = 254;
							color.r = (byte)((colorIndex % 16) * 16);
							color.g = (byte)((colorIndex / 16) * 16);    
						}
						else if (polyType == 2)
						{
							//transparency
							color.a = 128;							             
						}               
               
						int size = ReadShort(allbytes[i + 0], allbytes[i + 1]);
						i += 2;
						int pointSphereIndex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
						i += 2;

						Vector3 position = vertices[pointSphereIndex];
						Vector3 scale = new Vector3(size, size, size) / 500.0f;

						uv.AddRange(SphereMesh.uv.Select(x => x * noisesize * size / 200.0f));
						indices.AddRange(SphereMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(SphereMesh.vertices.Select(x => Vector3.Scale(x, scale) + position));
						colors.AddRange(SphereMesh.vertices.Select(x => color));
						break;
					}
                
				case 2: //1x1 pixel
				case 6: //ending guy
				case 7: //square
					{
						i++;
						int colorIndex = allbytes[i];
						i += 2;
						int cubeIndex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
						i += 2;

						Color32 color = paletteColors[colorIndex];
						Vector3 position = vertices[cubeIndex];
						Quaternion rotation = Quaternion.identity;

						Vector3 scale;
						if (primitiveType == 2)
						{
							scale = bounds.size.magnitude / 250.0f * Vector3.one;
						}
						else if (primitiveType == 6)
						{
							scale = new Vector3(0.05f, 0.05f, 0.1f);
							rotation = Quaternion.LookRotation(new Vector3(position.x, 0.0f, position.z));
						}
						else
						{
							scale = 0.05f * Vector3.one;
						}

						uv.AddRange(CubeMesh.uv);
						indices.AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x => rotation * Vector3.Scale(x, scale) + position));
						colors.AddRange(CubeMesh.vertices.Select(x => color));
						break;
					}
				default:
					throw new UnityException("unknown primitive " + primitiveType.ToString() + " at " + i.ToString());
			}

		}

		// Create the mesh
		Mesh msh = new Mesh(); 
		msh.vertices = allVertices.ToArray();      
		msh.colors32 = colors.ToArray();

		//separate transparent/opaque triangles
		List<int> opaque = new List<int>();
		List<int> noise = new List<int>();
		List<int> transparent = new List<int>();

		for (int t = 0; t < indices.Count; t += 3)
		{
			List<int> trianglesList;
			if (colors[indices[t]].a == 255)
			{
				trianglesList = opaque;
			}
			else if (colors[indices[t]].a == 254)
			{
				trianglesList = noise;
			}
			else
			{
				trianglesList = transparent;
			}

			trianglesList.Add(indices[t + 0]);
			trianglesList.Add(indices[t + 1]);
			trianglesList.Add(indices[t + 2]);
		}

		msh.subMeshCount = 3;
		msh.SetTriangles(opaque, 0);
		msh.SetTriangles(transparent, 1);
		msh.SetTriangles(noise, 2);
		msh.SetUVs(0, uv);

		msh.RecalculateNormals();
		msh.RecalculateBounds();
               
		filter = this.gameObject.GetComponent<MeshFilter>();
		filter.sharedMesh = msh;
	}

	void Start()
	{
		//load palette
		TextAsset asset = Resources.Load("palette") as TextAsset;
		byte[] allbytes = asset.bytes;
		for (int i = 0; i < 256; i++)
		{
			Color32 color = new Color32(allbytes[i * 3 + 0], allbytes[i * 3 + 1], allbytes[i * 3 + 2], 255);
			paletteColors.Add(color);
		}

		//load first model
		modelIndex = 0;
		LoadModels(modelFolders[modelFolderIndex]);
	}

	void LoadModels(string foldername)
	{
		if (Directory.Exists(foldername))
		{
			modelFiles = Directory.GetFiles(foldername)
                    .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x), NumberStyles.HexNumber)).ToList();            
			LoadBody(modelFiles[modelIndex]);
		}
		else
		{
			LeftText.text = string.Format("Cannot find folder {0}", foldername);
		}
	}

	void Update()
	{
		int oldModelIndex = modelIndex;

		if (Input.GetAxis("Mouse ScrollWheel") > 0)
		{
			if (cameraSettings.z > 0.1f)
				cameraSettings.z *= 0.9f;           
		}

		if (Input.GetAxis("Mouse ScrollWheel") < 0)
		{            
			cameraSettings.z *= 1.0f / 0.9f;
		}

		//menu
		if (Input.GetMouseButtonDown(1))
		{			
			menuEnabled = !menuEnabled;
			ModelIndexString = modelIndex.ToString();
		}

		//process keys
		foreach (var key in keyCodes)
		{			
			if(Input.GetKeyDown(key))
			{
				ProcessKey(key);
			}
		}

		if(!menuEnabled)
		{
			//start drag
			if (Input.GetMouseButtonDown(0))
			{
				mousePosition = Input.mousePosition;			
				autoRotate = false;
			}
				
			//dragging
			if (Input.GetMouseButton(0))
			{
				Vector3 mouseDelta = mousePosition - Input.mousePosition;
				cameraSettings += mouseDelta * Time.deltaTime * 50.0f;
				cameraSettings.y = Mathf.Clamp(cameraSettings.y, -89.99f, 89.99f);
				mousePosition = Input.mousePosition;
			}
		}

		modelIndex = Math.Min(Math.Max(modelIndex, 0), modelFiles.Count - 1);

		//load new model if needed
		if (oldModelIndex != modelIndex)
		{           
			ModelIndexString = modelIndex.ToString();
			LoadBody(modelFiles[modelIndex]);
		}   
         
		//rotate model
		if (autoRotate)
		{
			cameraSettings.x = Time.time * 100.0f;
			cameraSettings.y = 20.0f;
		}            
	}

	private void LateUpdate()
	{
		//set camera to look at model      
		Vector3 center = Vector3.Scale(gameObject.GetComponent<Renderer>().bounds.center, Vector3.up);   

		Camera.main.transform.position = center 
			+ (Quaternion.AngleAxis(cameraSettings.x, Vector3.down)
			* Quaternion.AngleAxis(cameraSettings.y, Vector3.right))
			* (Vector3.back * cameraSettings.z); 
		Camera.main.transform.LookAt(center);
	}

	private bool menuEnabled;
	public MenuStyle MenuStyle;
	public string ModelIndexString;

	void OnGUI() 
	{
		if (menuEnabled)
		{
			Rect rect = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 15 * 3, 400, 30 * 3);
			if(Input.GetMouseButtonDown(0) && !rect.Contains(Input.mousePosition)) {
				menuEnabled = false;
			}

			GUILayout.BeginArea(rect, MenuStyle.Panel);
			GUILayout.BeginVertical();

			if (GUILayout.Button("Room viewer", MenuStyle.Button) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.Tab);
			}

			//model no
			GUILayout.BeginHorizontal();
			GUILayout.Label("Model", MenuStyle.Label);
			ModelIndexString = GUILayout.TextField(ModelIndexString, MenuStyle.Button);

			if(Event.current.keyCode == KeyCode.Return)
			{
				int.TryParse(ModelIndexString, out modelIndex);
				modelIndex = Math.Min(Math.Max(modelIndex, 0), modelFiles.Count - 1);
				ModelIndexString = modelIndex.ToString();
				LoadBody(modelFiles[modelIndex]);
			}
			GUILayout.EndHorizontal();

			//render mode
			GUILayout.BeginHorizontal();
			GUILayout.Label("Render mode", MenuStyle.Label);
			if (GUILayout.Button(enableNoise ? "Texture shaded" : "Flat shaded", MenuStyle.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.R);
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.EndArea ();
		}
	}

	void ProcessKey(KeyCode code)
	{
		switch(code)
		{
			case KeyCode.LeftArrow:			          
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					modelIndex -= 10;
				}
				else
				{
					modelIndex--;
				}
				break;

			case KeyCode.RightArrow:		
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					modelIndex += 10;
				}
				else
				{
					modelIndex++;
				}
				break;
			
			case KeyCode.UpArrow:					
				if (modelFolderIndex < (modelFolders.Length - 1))
				{					
					modelFolderIndex++;
					LoadModels(modelFolders[modelFolderIndex]);
				}
				break;
			
			case KeyCode.DownArrow:	
				if (modelFolderIndex > 0)
				{
					modelFolderIndex--;
					LoadModels(modelFolders[modelFolderIndex]);
				}
				break;

			case KeyCode.R:
				enableNoise = !enableNoise;
				LoadBody(modelFiles[modelIndex], false);
				break;	
			  
			case KeyCode.Escape:
				if(Screen.fullScreen)
				{
					Application.Quit();
				}
				break;		  

			case KeyCode.Tab:
				SceneManager.LoadScene("room");
				break;
		}
	}
}
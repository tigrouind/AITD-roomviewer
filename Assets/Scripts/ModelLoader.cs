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

	private int PaletteIndex;
	public Texture2D[] PaletteTexture;
	public GUIText LeftText;
	public Mesh SphereMesh;
	public Mesh CubeMesh;
	public MenuStyle MenuStyle;

	private Vector2 cameraRotation = new Vector2();
	private Vector2 cameraPosition = new Vector2();
	private float cameraZoom = 2.0f;

	private Vector3 mousePosition; //mouse drag
	private bool autoRotate;
	private int renderMode = 2;
	private bool displayMenuAfterDrag;
	private bool menuEnabled;
	private string ModelIndexString;

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
		if (reset)
		{
			autoRotate = true;
			cameraPosition = Vector2.zero;
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

		//load palette
		Color32[] paletteColors = PaletteTexture[PaletteIndex].GetPixels32();

		List<Vector3> allVertices = new List<Vector3>();
		List<Color32> colors = new List<Color32>();
		List<int> indices = new List<int>();
		List<Vector2> uv = new List<Vector2>();
		bool enableNoise = renderMode == 1 || renderMode == 2;
		bool enableGradient = renderMode == 2;

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
						else if ((polyType == 3 || polyType == 6) && enableGradient)
						{
							//horizontal gradient
							color.a = 253;
							color.r = 255;
							color.g = 0;
							color.b = (byte)((colorIndex / 16) * 16);
						}
						else if ((polyType == 4 || polyType == 5) && enableGradient)
						{
							//vertical gradient
							color.a = 253;
							color.r = 0;
							color.g = 255;
							color.b = (byte)((colorIndex / 16) * 16);
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

						if (polyType == 1 && enableNoise)
						{
							Vector3 forward, left;
							ComputeUV(polyVertices, out forward, out left);

							foreach (Vector3 poly in polyVertices)
							{
								uv.Add(new Vector2(
										Vector3.Dot(poly, left) * noisesize,
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
						else if ((polyType == 3 || polyType == 6) && enableGradient)
						{
							//horizontal gradient
							color.a = 253;
							color.r = 255;
							color.g = 0;
							color.b = (byte)((colorIndex / 16) * 16);
						}
						else if ((polyType == 4 || polyType == 5) && enableGradient)
						{
							//vertical gradient
							color.a = 253;
							color.r = 0;
							color.g = 255;
							color.b = (byte)((colorIndex / 16) * 16);
						}

						int size = ReadShort(allbytes[i + 0], allbytes[i + 1]);
						i += 2;
						int pointSphereIndex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
						i += 2;

						Vector3 position = vertices[pointSphereIndex];
						float scale = size / 500.0f;
						float uvScale = noisesize * size / 200.0f;

						uv.AddRange(SphereMesh.uv.Select(x => x * uvScale));
						indices.AddRange(SphereMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(SphereMesh.vertices.Select(x => x * scale + position));
						colors.AddRange(SphereMesh.vertices.Select(x => color));
						break;
					}

				case 2: //1x1 pixel
				case 6: //square
				case 7: 
					{
						i++;
						int colorIndex = allbytes[i];
						i += 2;
						int cubeIndex = ReadShort(allbytes[i + 0], allbytes[i + 1]) / 6;
						i += 2;

						Color32 color = paletteColors[colorIndex];
						Vector3 position = vertices[cubeIndex];

						float pointsize;
						if(primitiveType == 2)
						{
							pointsize = linesize;
						}
						else
						{
							pointsize = linesize * 2.5f;
						}

						uv.AddRange(CubeMesh.uv);
						indices.AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x => x * pointsize + position));
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
		List<int> gradient = new List<int>();

		for (int t = 0; t < indices.Count; t += 3)
		{
			List<int> trianglesList;
			float alpha = colors[indices[t]].a;
			if (alpha == 255)
			{
				trianglesList = opaque;
			}
			else if (alpha == 254)
			{
				trianglesList = noise;
			}
			else if (alpha == 253)
			{
				trianglesList = gradient;
			}
			else
			{
				trianglesList = transparent;
			}

			trianglesList.Add(indices[t + 0]);
			trianglesList.Add(indices[t + 1]);
			trianglesList.Add(indices[t + 2]);
		}

		msh.subMeshCount = 4;
		msh.SetTriangles(opaque, 0);
		msh.SetTriangles(transparent, 1);
		msh.SetTriangles(noise, 2);
		msh.SetTriangles(gradient, 3);
		msh.SetUVs(0, uv);

		msh.RecalculateNormals();
		msh.RecalculateBounds();

		filter.sharedMesh = msh;
	}

	void ComputeUV(List<Vector3> polyVertices, out Vector3 forward, out Vector3 left)
	{
		int lastPoly = polyVertices.Count - 1;
		Vector3 up;
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
		} while (up == Vector3.zero && lastPoly > 1);
	}

	void Start()
	{
		if (!Directory.Exists(modelFolders[1]))
		{
			Array.Resize(ref modelFolders, 1);
		}

		//load first model
		modelIndex = 0;
		LoadModels(modelFolders[modelFolderIndex]);
	}

	int DetectGame()
	{
		//detect game based on number of models
		if (modelFiles.Count > 700) return 3;
		else if (modelFiles.Count > 500) return 2;
		else return 1;
	}

	void SetPalette()
	{
		PaletteIndex = DetectGame() - 1;

		GetComponent<Renderer>().materials[2] //noise
			.SetTexture("_Palette", PaletteTexture[PaletteIndex]);
		GetComponent<Renderer>().materials[3] //gradient
			.SetTexture("_Palette", PaletteTexture[PaletteIndex]);
	}

	void LoadModels(string foldername)
	{
		if (Directory.Exists(foldername))
		{
			modelFiles = Directory.GetFiles(foldername)
				.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x), NumberStyles.HexNumber)).ToList();

			SetPalette();
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
			if (cameraZoom > 0.1f)
				cameraZoom *= 0.9f;
		}

		if (Input.GetAxis("Mouse ScrollWheel") < 0)
		{
			cameraZoom *= 1.0f / 0.9f;
		}

		//process keys
		foreach (var key in keyCodes)
		{
			if (Input.GetKeyDown(key))
			{
				ProcessKey(key);
			}
		}

		if (!menuEnabled)
		{
			//start drag (rotate)
			if (Input.GetMouseButtonDown(0))
			{
				mousePosition = Input.mousePosition;
				autoRotate = false;
			}

			//dragging (rotate)
			if (Input.GetMouseButton(0))
			{
				Vector2 mouseDelta = mousePosition - Input.mousePosition;
				cameraRotation += mouseDelta * Time.deltaTime * 50.0f;
				cameraRotation.y = Mathf.Clamp(cameraRotation.y, -90.00f, 90.00f);
				mousePosition = Input.mousePosition;
			}

			//start drag (pan)
			if (Input.GetMouseButtonDown(1))
			{
				mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraZoom);
				autoRotate = false;
				displayMenuAfterDrag = true;
			}

			//dragging (pan)
			if (Input.GetMouseButton(1))
			{
				Vector3 newMousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraZoom);
				if (newMousePosition != this.mousePosition)
				{
					Vector2 mouseDelta = Camera.main.ScreenToWorldPoint(this.mousePosition) - Camera.main.ScreenToWorldPoint(newMousePosition);
					displayMenuAfterDrag = false;
					cameraPosition += mouseDelta;
					mousePosition = newMousePosition;
				}
			}

			//end drag (pan)
			if (Input.GetMouseButtonUp(1))
			{
				//show/hide menu
				if (displayMenuAfterDrag)
				{
					menuEnabled = !menuEnabled;
					if (menuEnabled)
					{
						ModelIndexString = modelIndex.ToString();
					}
				}
			}
		}

		modelIndex = Math.Min(Math.Max(modelIndex, 0), modelFiles.Count - 1);

		//load new model if needed
		if (oldModelIndex != modelIndex)
		{
			LoadBody(modelFiles[modelIndex]);
		}

		//rotate model
		if (autoRotate)
		{
			cameraRotation.x = Time.time * 100.0f;
			cameraRotation.y = 20.0f;
		}

		//update model
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		Vector3 center = Vector3.Scale(gameObject.GetComponent<Renderer>().bounds.center, Vector3.up);

		transform.position = -(Quaternion.AngleAxis(cameraRotation.y, Vector3.left) * center);
		transform.rotation = Quaternion.AngleAxis(cameraRotation.y, Vector3.left)
		* Quaternion.AngleAxis(cameraRotation.x, Vector3.up);

		//set camera
		Camera.main.transform.position = Vector3.back * cameraZoom + new Vector3(cameraPosition.x, cameraPosition.y, 0.0f);
		Camera.main.transform.rotation = Quaternion.AngleAxis(0.0f, Vector3.left);
	}

	private string[] renderModes = new[] { "Flat", "Noise", "Noise / Gradient"};

	void OnGUI()
	{
		if (menuEnabled)
		{
			Rect rect = new Rect((Screen.width / 2) - 200, (Screen.height / 2) - 15 * 3, 400, 30 * 3);
			if (Input.GetMouseButtonDown(0) && !rect.Contains(Input.mousePosition))
			{
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

			if (Event.current.keyCode == KeyCode.Return)
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
			if (GUILayout.Button(renderModes[renderMode], MenuStyle.Option) && Event.current.button == 0)
			{
				ProcessKey(KeyCode.R);
			}
			GUILayout.EndHorizontal();

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}

	void ProcessKey(KeyCode code)
	{
		switch (code)
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
				renderMode = (renderMode+1)%3;
				LoadBody(modelFiles[modelIndex], false);
				break;

			case KeyCode.Escape:
				if (Screen.fullScreen)
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
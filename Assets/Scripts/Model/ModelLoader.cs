using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

public class ModelLoader : MonoBehaviour
{
	private int modelIndex = 0;
	private int animIndex = 0;
	private int modelFolderIndex = 0;

	private KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private VarParser varParser = new VarParser();

	private string[] modelFolders = new string[] { "GAMEDATA\\LISTBODY", "GAMEDATA\\LISTBOD2" };
	private string[] animFolders = new string[] { "GAMEDATA\\LISTANIM", "GAMEDATA\\LISTANI2" };
	private List<string> modelFiles = new List<string>();
	private List<string> animFiles = new List<string>();
	private List<Frame> animFrames;
	private List<Transform> bones;
	private List<Vector3> initialBonesPosition;

	private int PaletteIndex;
	public Texture2D[] PaletteTexture;
	public Text LeftText;
	public Mesh SphereMesh;
	public Mesh CubeMesh;

	private Vector2 cameraRotation = new Vector2();
	private Vector2 cameraPosition = new Vector2();
	private float cameraZoom = 2.0f;

	private Vector3 mousePosition;
	//mouse drag
	private bool autoRotate;
	private bool displayMenuAfterDrag;
	private bool menuEnabled;
	private string LeftTextBody;
	private string LeftTextAnim;

	public RectTransform Panel;
	public InputField ModelInput;
	public InputField AnimationInput;
	public ToggleButton AutoRotate;
	public ToggleButton GradientMaterial;
	public ToggleButton NoiseMaterial;
	public ToggleButton EnableAnimation;
	public ToggleButton ShowAdditionalInfo;

	void LoadBody(string filename, bool resetcamera = true)
	{
		string varName = varParser.GetText("BODYS", modelIndex);
		LeftTextBody = Path.GetFileName(Path.GetDirectoryName(filename)) + " " + modelIndex + "/" + (modelFiles.Count - 1) + " <color=#00c864>" + varName + "</color>";
		RefreshLeftText();

		//camera
		if (resetcamera)
		{
			autoRotate = true;
			cameraPosition = Vector2.zero;
		}

		//clear model
		SkinnedMeshRenderer filter = this.gameObject.GetComponent<SkinnedMeshRenderer>();
		filter.sharedMesh = null;

		//delete all bones
		foreach (Transform child in transform) {
			GameObject.Destroy(child.gameObject);
		}

		//load data
		byte[] allbytes = File.ReadAllBytes(filename);
		int i = 0;

		//header
		int flags = Utils.ReadShort(allbytes, i + 0);
		i += 0xE;
		i += Utils.ReadShort(allbytes, i + 0) + 2;

		//vertexes
		int count = Utils.ReadShort(allbytes, i + 0);
		i += 2;

		List<Vector3> vertices = new List<Vector3>();
		for (int j = 0; j < count; j++)
		{
			Vector3 position = new Vector3(Utils.ReadShort(allbytes, i + 0), -Utils.ReadShort(allbytes, i + 2), Utils.ReadShort(allbytes, i + 4));
			vertices.Add(position / 1000.0f);
			i += 6;
		}

		//check if model has bones
		bones = new List<Transform>();
		List<Matrix4x4> bindPoses = new List<Matrix4x4>();
		Dictionary<int, int> bonesPerVertex = new Dictionary<int, int>();
		List<Vector3> vertexNoTransform = vertices.ToList();

		if ((flags & 2) == 2)
		{
			//bones
			count = Utils.ReadShort(allbytes, i + 0);
			i += 2;
			i += count * 2;

			Dictionary<int, Transform> bonesPerIndex = new Dictionary<int, Transform>();

			bonesPerIndex.Add(255, transform);
			for (int n = 0; n < count; n++)
			{
				int startindex = Utils.ReadShort(allbytes, i + 0) / 6;
				int numpoints = Utils.ReadShort(allbytes, i + 2);
				int vertexindex = Utils.ReadShort(allbytes, i + 4) / 6;
				int parentindex = allbytes[i + 6];
				int boneindex = allbytes[i + 7];

				//create bone
				Transform bone = new GameObject("BONE").transform;
				bonesPerIndex.Add(boneindex, bone);

				bone.parent = bonesPerIndex[parentindex];
				bone.localRotation = Quaternion.identity;
				bone.localPosition = vertexNoTransform[vertexindex];
				bones.Add(bone);

				//create pose
				Matrix4x4 bindPose = new Matrix4x4();
				bindPose = bone.worldToLocalMatrix * transform.localToWorldMatrix;
				bindPoses.Add(bindPose);

				//apply bone transformation
				Vector3 position = vertices[vertexindex];
				for (int u = 0; u < numpoints; u++)
				{
					vertices[startindex] += position;
					bonesPerVertex.Add(startindex, bones.Count - 1);
					startindex++;
				}

				i += 0x10;
			}
		}
		else
		{
			//if no bones add dummy values
			for (int u = 0; u < vertices.Count; u++)
			{
				bonesPerVertex.Add(u, 0);
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
		count = Utils.ReadShort(allbytes, i + 0);
		i += 2;

		//load palette
		Color32[] paletteColors = PaletteTexture[PaletteIndex].GetPixels32();

		List<BoneWeight> boneWeights = new List<BoneWeight>();
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
						int pointIndexA = Utils.ReadShort(allbytes, i + 0) / 6;
						int pointIndexB = Utils.ReadShort(allbytes, i + 2) / 6;
						Vector3 directionVector = vertices[pointIndexA] - vertices[pointIndexB];
						Vector3 middle = (vertices[pointIndexA] + vertices[pointIndexB]) / 2.0f;
						Quaternion rotation = Quaternion.LookRotation(directionVector);

						uv.AddRange(CubeMesh.uv);
						indices.AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x =>
							rotation * (Vector3.Scale(x, new Vector3(linesize, linesize, directionVector.magnitude)))
							+ middle));
						colors.AddRange(CubeMesh.vertices.Select(x => color));
						boneWeights.AddRange(CubeMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[x.z > 0 ? pointIndexA : pointIndexB], weight0 = 1 }));

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

						Color32 color = GetPaletteColor(paletteColors, colorIndex, polyType);

						//add vertices
						List<Vector3> polyVertices = new List<Vector3>();
						int verticesCount = allVertices.Count;
						for (int m = 0; m < numPoints; m++)
						{
							int pointIndex = Utils.ReadShort(allbytes, i + 0) / 6;
							i += 2;

							colors.Add(color);
							allVertices.Add(vertices[pointIndex]);
							polyVertices.Add(vertices[pointIndex]);
							boneWeights.Add(new BoneWeight() { boneIndex0 = bonesPerVertex[pointIndex], weight0 = 1 });
						}

						if (polyType == 1 && NoiseMaterial.BoolValue)
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
						Color32 color = GetPaletteColor(paletteColors, colorIndex, polyType);
						i += 2;

						int size = Utils.ReadShort(allbytes, i + 0);
						i += 2;
						int pointSphereIndex = Utils.ReadShort(allbytes, i + 0) / 6;
						i += 2;

						Vector3 position = vertices[pointSphereIndex];
						float scale = size / 500.0f;
						float uvScale = noisesize * size / 200.0f;

						uv.AddRange(SphereMesh.uv.Select(x => x * uvScale));
						indices.AddRange(SphereMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(SphereMesh.vertices.Select(x => x * scale + position));
						colors.AddRange(SphereMesh.vertices.Select(x => color));
						boneWeights.AddRange(SphereMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[pointSphereIndex], weight0 = 1 }));
						break;
					}

				case 2: //1x1 pixel
				case 6: //square
				case 7:
					{
						i++;
						int colorIndex = allbytes[i];
						i += 2;
						int cubeIndex = Utils.ReadShort(allbytes, i + 0) / 6;
						i += 2;

						Color32 color = paletteColors[colorIndex];
						Vector3 position = vertices[cubeIndex];

						float pointsize;
						if (primitiveType == 2)
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
						boneWeights.AddRange(CubeMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[cubeIndex], weight0 = 1 }));
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

		//apply bones
		if(bones.Count > 0)
		{
			msh.boneWeights = boneWeights.ToArray();
			msh.bindposes = bindPoses.ToArray();
			GetComponent<SkinnedMeshRenderer>().bones = bones.ToArray();
			initialBonesPosition = bones.Select(x => x.localPosition).ToList();
		}

		filter.localBounds = msh.bounds;
		filter.sharedMesh = msh;
	}

	Color32 GetPaletteColor(Color32[] paletteColors, int colorIndex, int polyType)
	{
		Color32 color = paletteColors[colorIndex];

		if (polyType == 1 && NoiseMaterial.BoolValue)
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
		else if ((polyType == 3 || polyType == 6) && GradientMaterial.BoolValue)
		{
			//horizontal gradient
			color.a = 253;
			color.r = 255;
			color.g = 0;
			color.b = (byte)((colorIndex / 16) * 16);
		}
		else if ((polyType == 4 || polyType == 5) && GradientMaterial.BoolValue)
		{
			//vertical gradient
			color.a = 253;
			color.r = 0;
			color.g = 255;
			color.b = (byte)((colorIndex / 16) * 16);
		}

		return color;
	}

	class Frame
	{
		public float Time;
		public float OffsetX;
		public float OffsetY;
		public float OffsetZ;
		public List<Vector4> Bones;
	}

	void LoadAnim(string filename)
	{
		string varName = varParser.GetText("ANIMS", animIndex);
		LeftTextAnim = Path.GetFileName(Path.GetDirectoryName(filename)) + " " + animIndex + "/" + (animFiles.Count - 1) + " <color=#00c864>" + varName + "</color>";

		int i = 0;
		byte[] allbytes = File.ReadAllBytes(filename);
		int frameCount = Utils.ReadShort(allbytes, i + 0);
		int boneCount = Utils.ReadShort(allbytes, i + 2);
		i += 4;

		animFrames = new List<Frame>();
		for(int frame = 0 ; frame < frameCount ; frame++)
		{
			Frame f = new Frame();
			f.Time = Utils.ReadShort(allbytes, i + 0);
			f.OffsetX = Utils.ReadShort(allbytes, i + 2);
			f.OffsetY = Utils.ReadShort(allbytes, i + 4);
			f.OffsetZ = Utils.ReadShort(allbytes, i + 6);

			f.Bones = new List<Vector4>();
			i += 8;
			for(int bone = 0 ; bone < boneCount ; bone++)
			{
				int type = Utils.ReadShort(allbytes, i + 0);
				int x = Utils.ReadShort(allbytes, i + 2);
				int y = Utils.ReadShort(allbytes, i + 4);
				int z = Utils.ReadShort(allbytes, i + 6);

				if(type == 0) //rotate
				{
					f.Bones.Add(new Vector4(-x * 360 / 1024.0f, -y * 360 / 1024.0f, -z * 360 / 1024.0f, type));
				}

				else if(type == 1) //translate
				{
					f.Bones.Add(new Vector4(x / 1000.0f, -y / 1000.0f, z / 1000.0f, type));
				}
				else //scale
				{
					f.Bones.Add(new Vector4(x / 1024.0f + 1.0f, y / 1024.0f + 1.0f, z / 1024.0f + 1.0f, type));
				}
				i += 8;
			}

			animFrames.Add(f);
		}

		RefreshLeftText();
	}

	void AnimateModel()
	{
		float totaltime = animFrames.Sum(x => x.Time);
		float time = (Time.time * 50.0f) % totaltime;

		//find current frame
		totaltime = 0.0f;
		int frame = 0;
		for (int i = 0 ; i < animFrames.Count ; i++)
		{
			totaltime += animFrames[(i + 1) % animFrames.Count].Time;
			if(time < totaltime)
			{
				frame = i;
				break;
			}
		}

		Frame currentFrame = animFrames[frame % animFrames.Count];
		Frame nextFrame = animFrames[(frame + 1) % animFrames.Count];
		float framePosition = (time - (totaltime - nextFrame.Time)) / nextFrame.Time;

		for (int i = 0 ; i < bones.Count; i++)
		{
			Transform boneTransform = bones[i].transform;

			if(i >= currentFrame.Bones.Count)
			{
				//there is more bones in model than anim
				boneTransform.localPosition = initialBonesPosition[i];
				boneTransform.localRotation = Quaternion.identity;
				boneTransform.localScale = Vector3.one;
				continue;
			}

			var currentBone = currentFrame.Bones[i];
			var nextBone = nextFrame.Bones[i];

			//interpolate
			if (nextBone.w == 0.0f)
			{
				//rotation
				boneTransform.localPosition = initialBonesPosition[i];
				boneTransform.localScale = Vector3.one;
				boneTransform.localRotation =
					Quaternion.Slerp(
						Quaternion.AngleAxis(currentBone.z, Vector3.forward) *
						Quaternion.AngleAxis(currentBone.x, Vector3.right) *
						Quaternion.AngleAxis(currentBone.y, Vector3.up),
						Quaternion.AngleAxis(nextBone.z, Vector3.forward) *
						Quaternion.AngleAxis(nextBone.x, Vector3.right) *
						Quaternion.AngleAxis(nextBone.y, Vector3.up),
						framePosition);
			}
			else if (nextBone.w == 1.0f)
			{
				//position
				boneTransform.localRotation = Quaternion.identity;
				boneTransform.localScale = Vector3.one;
				boneTransform.localPosition = initialBonesPosition[i] +
					Vector3.Lerp(
						new Vector3(currentBone.x, currentBone.y, currentBone.z),
						new Vector3(nextBone.x, nextBone.y, nextBone.z),
						framePosition);
			}
			else
			{
				//scaling
				boneTransform.localRotation = Quaternion.identity;
				boneTransform.localPosition = initialBonesPosition[i];
				boneTransform.localScale =
					Vector3.Lerp(
						new Vector3(currentBone.x, currentBone.y, currentBone.z),
						new Vector3(nextBone.x, nextBone.y, nextBone.z),
						framePosition);
			}
		}
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
		//parse vars.txt file
		string varPath = @"GAMEDATA\vars.txt";
		if (File.Exists(varPath))
		{
			varParser.Parse(varPath);
		}

		if(!Directory.Exists(modelFolders[1]))
		{
			Array.Resize(ref modelFolders, 1);
		}

		//load first model
		modelIndex = 0;
		LoadModels(modelFolders[modelFolderIndex]);
		LoadAnims(animFolders[modelFolderIndex]);
		ToggleAnimationMenuItems(false);
	}

	int DetectGame()
	{
		//detect game based on number of models
		if (modelFiles.Count > 700)
			return 3;
		else if (modelFiles.Count > 500)
			return 2;
		else
			return 1;
	}

	void SetPalette()
	{
		PaletteIndex = DetectGame() - 1;

		GetComponent<SkinnedMeshRenderer>().materials[2] //noise
			.SetTexture("_Palette", PaletteTexture[PaletteIndex]);
		GetComponent<SkinnedMeshRenderer>().materials[3] //gradient
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

	void LoadAnims(string foldername)
	{
		if (Directory.Exists(foldername))
		{
			animFiles = Directory.GetFiles(foldername)
				.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x), NumberStyles.HexNumber)).ToList();

			if(EnableAnimation.BoolValue)
			{
				LoadAnim(animFiles[animIndex]);
			}
		}
	}

	void Update()
	{
		int oldModelIndex = modelIndex;
		int oldAnimIndex = animIndex;

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
				mousePosition = Input.mousePosition;
				autoRotate = false;
				displayMenuAfterDrag = true;
			}

			//dragging (pan)
			if (Input.GetMouseButton(1))
			{
				Vector3 newMousePosition = Input.mousePosition;
				if (newMousePosition != this.mousePosition)
				{
					Vector3 cameraDistance = new Vector3(0.0f, 0.0f, cameraZoom);
					Vector2 mouseDelta = Camera.main.ScreenToWorldPoint(this.mousePosition + cameraDistance)
						- Camera.main.ScreenToWorldPoint(newMousePosition + cameraDistance);
					displayMenuAfterDrag = false;
					cameraPosition += mouseDelta;
					mousePosition = newMousePosition;
				}
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
					ModelInput.text = modelIndex.ToString();
					AnimationInput.text = animIndex.ToString();
				}
			}
		}

		if (Input.GetMouseButtonUp(0)
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			menuEnabled = false;
		}

		Panel.gameObject.SetActive(menuEnabled);

		modelIndex = Math.Min(Math.Max(modelIndex, 0), modelFiles.Count - 1);
		animIndex = Math.Min(Math.Max(animIndex, 0), animFiles.Count - 1);

		//load new model if needed
		if (modelFiles.Count > 0 && oldModelIndex != modelIndex)
		{
			ModelInput.text = modelIndex.ToString();
			LoadBody(modelFiles[modelIndex]);
		}

		if (animFiles.Count > 0 && oldAnimIndex != animIndex)
		{
			AnimationInput.text = animIndex.ToString();
			LoadAnim(animFiles[animIndex]);
		}

		//rotate model
		if (autoRotate && AutoRotate.BoolValue)
		{
			cameraRotation.x = Time.time * 100.0f;
			cameraRotation.y = 20.0f;
		}

		//animate
		if(animFrames != null)
		{
			AnimateModel();
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

	void RefreshLeftText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(LeftTextBody);
		if(EnableAnimation.BoolValue)
		{
			stringBuilder.Append("\r\n" + LeftTextAnim);
			if(ShowAdditionalInfo.BoolValue && animFrames != null)
			{
				int index = 0;
				stringBuilder.Append("\r\n\r\n");
				foreach(Frame frame in animFrames)
				{
					stringBuilder.AppendFormat("Frame {0}: <color=#00c864>{1} {2} {3} {4}</color>\r\n", index, frame.Time, frame.OffsetX, frame.OffsetY, -frame.OffsetZ);
					index++;
				}
			}
		}

		LeftText.text = stringBuilder.ToString();
	}

	public void ToggleAnimationMenuItems(bool enabled)
	{
		AnimationInput.transform.parent.gameObject.SetActive(enabled);
		ShowAdditionalInfo.transform.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	public void ModelIndexInputChanged()
	{
		int newModelIndex;
		if(int.TryParse(ModelInput.text, out newModelIndex) && newModelIndex != modelIndex)
		{
			modelIndex = Math.Min(Math.Max(newModelIndex, 0), modelFiles.Count - 1);
			ModelInput.text = modelIndex.ToString();
			LoadBody(modelFiles[modelIndex]);
		}
	}

	public void AnimationIndexInputChanged()
	{
		int newAnimIndex;
		if(int.TryParse(AnimationInput.text, out newAnimIndex) && newAnimIndex != animIndex)
		{
			animIndex = Math.Min(Math.Max(newAnimIndex, 0), animFiles.Count - 1);
			AnimationInput.text = animIndex.ToString();
			LoadAnim(animFiles[animIndex]);
		}
	}

	public void ProcessKey(string keyCode)
	{
		KeyCode keyCodeEnum = (KeyCode)Enum.Parse(typeof(KeyCode), keyCode, true);
		ProcessKey(keyCodeEnum);
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

			case KeyCode.Space:
				modelFolderIndex = (modelFolderIndex + 1) % modelFolders.Length;
				LoadModels(modelFolders[modelFolderIndex]);
				LoadAnims(animFolders[modelFolderIndex]);
				break;

			case KeyCode.UpArrow:
				if(EnableAnimation.BoolValue)
				{
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					{
						animIndex += 10;
					}
					else
					{
						animIndex++;
					}
				}
				break;

			case KeyCode.DownArrow:
				if(EnableAnimation.BoolValue)
				{
					if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
					{
						animIndex -= 10;
					}
					else
					{
						animIndex--;
					}
				}
				break;

			case KeyCode.A:
				if (animFiles.Count > 0)
				{
					EnableAnimation.BoolValue = !EnableAnimation.BoolValue;
					ToggleAnimationMenuItems(EnableAnimation.BoolValue);
					if (EnableAnimation.BoolValue)
					{
						LoadAnim(animFiles[animIndex]);
					}
					else
					{
						animFrames = null;
						LoadBody(modelFiles[modelIndex], false);
					}
				}
				break;

			case KeyCode.E:
				ShowAdditionalInfo.BoolValue = !ShowAdditionalInfo.BoolValue;
				RefreshLeftText();
				break;

			case KeyCode.G:
				GradientMaterial.BoolValue = !GradientMaterial.BoolValue;
				LoadBody(modelFiles[modelIndex], false);
				break;

			case KeyCode.N:
				NoiseMaterial.BoolValue = !NoiseMaterial.BoolValue;
				LoadBody(modelFiles[modelIndex], false);
				break;

			case KeyCode.R:
				AutoRotate.BoolValue = !AutoRotate.BoolValue;
				if (AutoRotate.BoolValue)
				{
					autoRotate = true;
				}
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
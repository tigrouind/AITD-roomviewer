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
using System.Text.RegularExpressions;

public class ModelLoader : MonoBehaviour
{
	private int modelIndex = 0;
	private int animIndex = 0;
	private int modelFolderIndex = 0;

	private readonly KeyCode[] keyCodes = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().ToArray();
	private VarParser varParser = new VarParser();

	private readonly string[] modelFolders = { Config.GetPath("LISTBODY.PAK"), Config.GetPath("LISTBOD2.PAK") };
	private readonly string[] animFolders = { Config.GetPath("LISTANIM.PAK"), Config.GetPath("LISTANI2.PAK") };
	private readonly string textureFolder = Config.GetPath("TEXTURES.PAK");

	private int modelCount;
	private int animCount;
	private int textureCount;

	private List<Frame> animFrames;
	private List<Transform> bones;
	private List<Vector3> initialBonesPosition;
	private int modelFlags;
	private int previousFrame, previousHighlightFrame;
	private float previousAnimCounter;
	private Vector3Int frameDistance;

	private Texture2D paletteTexture;
	public Text LeftText;
	public Mesh SphereMesh;
	public Mesh CubeMesh;
	private List<List<int>> gradientPolygonList;
	private List<int> gradientPolygonType;
	private Mesh bakedMesh;
	private List<Vector3> allVertices;
	private List<Vector2> uv;
	private List<Vector2> uvDepth;
	private List<Color32> colorsRaw;
	private Vector3Int boundingLower;
	private Vector3Int boundingUpper;
	private int boundingBoxMode;
	private readonly string[] boundingBoxModes = { "normal", "cube", "max" };
	private readonly string[] materialNames = { "shadeless", "glass", "noise", "metal_horizontal", "metal_vertical", "texture_a", "texture_b" };

	private Vector2 cameraRotation = new Vector2(0.0f, 20.0f);
	private Vector2 cameraPosition;
	private float cameraZoom = 2.0f;

	private Vector3 mousePosition;
	//mouse drag
	private bool menuEnabled;

	public RectTransform Panel;
	public InputField ModelInput;
	public InputField AnimationInput;
	public ToggleButton AutoRotate;
	public ToggleButton DetailsLevel;
	public ToggleButton EnableAnimation;
	public ToggleButton ShowAdditionalInfo;
	public GameObject BoundingBox;
	public GameObject Grid;

	void LoadBody(bool resetcamera = true)
	{
		string filePath = modelFolders[modelFolderIndex];

		//camera
		if (resetcamera)
		{
			cameraPosition = Vector2.zero;
		}

		//clear model
		SkinnedMeshRenderer filter = GetComponent<SkinnedMeshRenderer>();
		filter.sharedMesh = null;

		//delete all bones
		foreach (Transform child in transform)
		{
			if (child.gameObject != BoundingBox)
			{
				Destroy(child.gameObject);
			}
		}

		//load data
		byte[] buffer;
		using (var pak = new PakArchive(filePath))
		{
			buffer = pak[modelIndex].Read();
		}
		int i = 0;

		//header
		modelFlags = buffer.ReadShort(i + 0);

		//bounding box
		LoadBoundingBox(buffer, i + 2);

		i += 0xE;
		i += buffer.ReadShort(i + 0) + 2;

		//vertexes
		int count = buffer.ReadShort(i + 0);
		i += 2;

		List<Vector3> vertices = new List<Vector3>();
		for (int j = 0; j < count; j++)
		{
			Vector3 position = new Vector3(buffer.ReadShort(i + 0), -buffer.ReadShort(i + 2), buffer.ReadShort(i + 4));
			vertices.Add(position / 1000.0f);
			i += 6;
		}

		bones = new List<Transform>();
		List<Matrix4x4> bindPoses = new List<Matrix4x4>();
		Dictionary<int, int> bonesPerVertex = new Dictionary<int, int>();
		List<Vector3> vertexNoTransform = vertices.ToList();

		if ((modelFlags & 2) == 2) //check if model has bones
		{
			//bones
			count = buffer.ReadShort(i + 0);
			i += 2;
			i += count * 2;

			Dictionary<int, Transform> bonesPerIndex = new Dictionary<int, Transform>();

			bonesPerIndex.Add(255, transform);
			for (int n = 0; n < count; n++)
			{
				int startindex = buffer.ReadShort(i + 0) / 6;
				int numpoints = buffer.ReadShort(i + 2);
				int vertexindex = buffer.ReadShort(i + 4) / 6;
				int parentindex = buffer[i + 6];
				int boneindex = buffer[i + 7];

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

				if ((modelFlags & 8) == 8)
				{
					i += 0x18;
				}
				else
				{
					i += 0x10;
				}
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
		count = buffer.ReadUnsignedShort(i + 0);
		i += 2;

		//load palette
		Color32[] paletteColors = paletteTexture.GetPixels32();

		//load texture
		int texAHeight = 1;
		int texBHeight = 1;
		int uvStart = 0;
		if (File.Exists(textureFolder)) //TIMEGATE
		{
			Texture2D texA, texB;
			Texture.LoadTextures(buffer, paletteColors, textureFolder, textureCount, DetailsLevel.BoolValue, out uvStart, out texAHeight, out texBHeight, out texA, out texB);

			var materials = GetComponent<SkinnedMeshRenderer>().materials;
			materials[5].mainTexture = texA;
			materials[6].mainTexture = texB;
		}

		List<BoneWeight> boneWeights = new List<BoneWeight>();
		allVertices = new List<Vector3>();
		uv = new List<Vector2>();
		uvDepth = new List<Vector2>();
		List<Color32> colors = new List<Color32>();
		List<int>[] indices = new List<int>[7];

		for (int n = 0 ; n < indices.Length ; n++)
		{
			indices[n] = new List<int>();
		}

		gradientPolygonList = new List<List<int>>();
		gradientPolygonType = new List<int>();
		colorsRaw = new List<Color32>();

		for (int n = 0; n < count; n++)
		{
			int primitiveType = buffer[i + 0];
			i++;

			switch (primitiveType)
			{
				//line
				case 0:
					{
						i++;
						int colorIndex = buffer[i + 0];
						i += 2;

						Color32 color = paletteColors[colorIndex];
						int pointIndexA = buffer.ReadShort(i + 0) / 6;
						int pointIndexB = buffer.ReadShort(i + 2) / 6;
						Vector3 directionVector = vertices[pointIndexA] - vertices[pointIndexB];
						Vector3 middle = (vertices[pointIndexA] + vertices[pointIndexB]) / 2.0f;
						Quaternion rotation = Quaternion.LookRotation(directionVector);

						uv.AddRange(CubeMesh.uv);
						uvDepth.AddRange(CubeMesh.vertices.Select(x => Vector2.zero));
						indices[0].AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x =>
							rotation * Vector3.Scale(x, new Vector3(linesize, linesize, directionVector.magnitude))
							+ middle));
						colors.AddRange(CubeMesh.vertices.Select(x => color));
						colorsRaw.AddRange(CubeMesh.vertices.Select(x => color));
						boneWeights.AddRange(CubeMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[x.z > 0 ? pointIndexA : pointIndexB], weight0 = 1 }));

						i += 4;
						break;
					}
				//polygon
				case 1:
					{
						int numPoints = buffer[i + 0];
						int polyType = buffer[i + 1];
						int colorIndex = buffer[i + 2];
						i += 3;

						Color32 color = Palette.GetPaletteColor(paletteColors, colorIndex, polyType, DetailsLevel.BoolValue);
						Color32 colorRaw = Palette.GetRawPaletteColor(paletteColors, colorIndex, polyType);
						List<int> triangleList = indices[GetTriangleListIndex(polyType)];

						//add vertices
						List<int> polyVertices = new List<int>();
						int verticesCount = allVertices.Count;
						for (int m = 0; m < numPoints; m++)
						{
							int pointIndex = buffer.ReadShort(i + 0) / 6;
							i += 2;

							colors.Add(color);
							colorsRaw.Add(colorRaw);
							polyVertices.Add(allVertices.Count);
							allVertices.Add(vertices[pointIndex]);
							boneWeights.Add(new BoneWeight() { boneIndex0 = bonesPerVertex[pointIndex], weight0 = 1 });
						}

						gradientPolygonType.Add(polyType);
						gradientPolygonList.Add(polyVertices);

						if (polyType == 1 && DetailsLevel.BoolValue)
						{
							Vector3 forward, left;
							ComputeUV(polyVertices, out forward, out left);

							foreach (int pointIndex in polyVertices)
							{
								Vector3 poly = allVertices[pointIndex];

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

						uvDepth.AddRange(polyVertices.Select(x => Vector2.zero));

						//triangulate
						int v0 = 0;
						int v1 = 1;
						int v2 = numPoints - 1;
						bool swap = true;

						while (v1 < v2)
						{
							triangleList.Add(verticesCount + v0);
							triangleList.Add(verticesCount + v1);
							triangleList.Add(verticesCount + v2);

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
						int polyType = buffer[i];
						i++;
						int colorIndex = buffer[i];
						Color32 color = Palette.GetPaletteColor(paletteColors, colorIndex, polyType, DetailsLevel.BoolValue);
						Color32 colorRaw = Palette.GetRawPaletteColor(paletteColors, colorIndex, polyType);
						List<int> triangleList = indices[GetTriangleListIndex(polyType)];

						i += 2;

						int size = buffer.ReadShort(i + 0);
						i += 2;
						int pointSphereIndex = buffer.ReadShort(i + 0) / 6;
						i += 2;

						Vector3 position = vertices[pointSphereIndex];
						float scale = size / 500.0f;
						float uvScale = noisesize * size / 200.0f;

						if ((polyType == 3 || polyType == 4 || polyType == 5 || polyType == 6) && DetailsLevel.BoolValue)
						{
							gradientPolygonType.Add(polyType);
							gradientPolygonList.Add(Enumerable.Range(allVertices.Count, SphereMesh.vertices.Length).ToList());
						}

						uv.AddRange(SphereMesh.uv.Select(x => x * uvScale));
						uvDepth.AddRange(SphereMesh.vertices.Select(x => Vector2.zero));
						triangleList.AddRange(SphereMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(SphereMesh.vertices.Select(x => x * scale + position));
						colors.AddRange(SphereMesh.vertices.Select(x => color));
						colorsRaw.AddRange(SphereMesh.vertices.Select(x => colorRaw));
						boneWeights.AddRange(SphereMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[pointSphereIndex], weight0 = 1 }));
						break;
					}

				case 2: //1x1 pixel
				case 6: //2x2 square
				case 7: //NxN square, size depends projected z-value
					{
						i++;
						int colorIndex = buffer[i];
						i += 2;
						int cubeIndex = buffer.ReadShort(i + 0) / 6;
						i += 2;

						Color32 color = paletteColors[colorIndex];
						Vector3 position = vertices[cubeIndex];

						float pointsize = linesize;
						switch (primitiveType)
						{
							case 6:
								pointsize = linesize * 2.5f;
								break;
							case 7:
								pointsize = linesize * 5.0f;
								break;
						}

						uv.AddRange(CubeMesh.uv);
						uvDepth.AddRange(CubeMesh.vertices.Select(x => Vector2.zero));
						indices[0].AddRange(CubeMesh.triangles.Select(x => x + allVertices.Count));
						allVertices.AddRange(CubeMesh.vertices.Select(x => x * pointsize + position));
						colors.AddRange(CubeMesh.vertices.Select(x => color));
						colorsRaw.AddRange(CubeMesh.vertices.Select(x => color));
						boneWeights.AddRange(CubeMesh.vertices.Select(x => new BoneWeight() { boneIndex0 = bonesPerVertex[cubeIndex], weight0 = 1 }));
						break;
					}

				//triangle
				case 8:  //texture
				case 9:  //normals
				case 10: //normals + texture
					{
						float textureHeight = 1.0f;
						int uvIndex = 0, indicesIndex = 0;
						Color color = Color.white;

						if (primitiveType == 8 || primitiveType == 10)
						{
							uvIndex = uvStart + buffer.ReadUnsignedShort(i + 1) / 16 * 3;
							bool texModel = (buffer[i + 1] & 0xF) == 0;
							textureHeight = texModel ? texAHeight : texBHeight;
							indicesIndex = texModel ? 5 : 6;
						}
						else
						{
							int colorIndex = buffer[i + 2];
							color = paletteColors[colorIndex];
						}

						i += 3;

						for (int k = 0 ; k < 3 ; k++)
						{
							int pointIndex = buffer.ReadShort(i + 0) / 6;
							i += 2;

							uvDepth.Add(Vector2.zero);
							indices[indicesIndex].Add(allVertices.Count);
							colors.Add(color);
							colorsRaw.Add(color);
							allVertices.Add(vertices[pointIndex]);
							boneWeights.Add(new BoneWeight() { boneIndex0 = bonesPerVertex[pointIndex], weight0 = 1 });

							if (primitiveType == 8 || primitiveType == 10)
							{
								uv.Add(new Vector2(
									buffer.ReadShort(uvIndex + 0) / 256.0f,
									buffer.ReadShort(uvIndex + 2) / textureHeight));
								uvIndex += 4;
							}
							else
							{
								uv.Add(Vector2.zero);
							}
						}

						if (primitiveType == 9 || primitiveType == 10)
						{
							i +=6; //normals
						}
					}
					break;

				case 4:
				case 5: //should be ignored
					break;

				default:
					throw new UnityException("unknown primitive " + primitiveType.ToString() + " at " + i.ToString());
			}

		}

		// Create the mesh
		Mesh msh = new Mesh();
		msh.vertices = allVertices.ToArray();
		msh.colors32 = colors.ToArray();

		//separate triangles depending their material
		msh.subMeshCount = 7;
		msh.SetTriangles(indices[0], 0);
		msh.SetTriangles(indices[1], 1);
		msh.SetTriangles(indices[2], 2);
		msh.SetTriangles(indices[3], 3);
		msh.SetTriangles(indices[4], 4);
		msh.SetTriangles(indices[5], 5);
		msh.SetTriangles(indices[6], 6);
		msh.SetUVs(0, uv);
		msh.SetUVs(1, uvDepth);
		msh.RecalculateNormals();
		msh.RecalculateBounds();

		//apply bones
		if (bones.Count > 0)
		{
			msh.boneWeights = boneWeights.ToArray();
			msh.bindposes = bindPoses.ToArray();
			GetComponent<SkinnedMeshRenderer>().bones = bones.ToArray();
			initialBonesPosition = bones.Select(x => x.localPosition).ToList();
		}

		filter.localBounds = msh.bounds;
		filter.sharedMesh = msh;

		RefreshLeftText();
		if (resetcamera)
		{
			frameDistance = Vector3Int.Zero;
		}
	}

	int GetTriangleListIndex(int polyType)
	{
		if (!DetailsLevel.BoolValue)
		{
			return 0;
		}

		switch (polyType)
		{
			default:
			case 0: //single color
				return 0;

			case 1: //noise
				return 2;

			case 2: //transparent
				return 1;

			case 3: //gradientH
			case 6:
				return 3;

			case 4: //gradientV
			case 5:
				return 4;
		}
	}

	void LoadBoundingBox(byte[] buffer, int position)
	{
		buffer.ReadBoundingBox(position, out boundingLower, out boundingUpper);

		switch (boundingBoxMode)
		{
			case 1: //cube
				boundingUpper.x = boundingUpper.z = (boundingUpper.x + boundingUpper.z) / 2;
				boundingLower.x = boundingLower.z = -boundingUpper.z;
				break;

			case 2: //max
				boundingUpper.x = boundingUpper.z = Math.Max(boundingUpper.x - boundingLower.x, boundingUpper.z - boundingUpper.x) / 2;
				boundingLower.x = boundingLower.z = -boundingUpper.z;
				break;
		}

		BoundingBox.transform.localScale = (Vector3)(boundingUpper - boundingLower) / 1000.0f;
		Vector3 pos = (Vector3)(boundingUpper + boundingLower) / 2000.0f;
		BoundingBox.transform.localPosition = new Vector3(pos.x, -pos.y, pos.z);
	}

	void LoadAnim()
	{
		string filePath = animFolders[modelFolderIndex];

		int i = 0;
		byte[] buffer;
		using (var pak = new PakArchive(filePath))
		{
			buffer = pak[animIndex].Read();
		}

		int frameCount = buffer.ReadShort(i + 0);
		int boneCount = buffer.ReadShort(i + 2);
		i += 4;

		var isAITD2 = ((boneCount * 16 + 8) * frameCount + 4) == buffer.Length;
		animFrames = new List<Frame>();
		for (int frame = 0 ; frame < frameCount ; frame++)
		{
			Frame f = new Frame();
			f.Time = buffer.ReadShort(i + 0);
			f.Offset = buffer.ReadVector(i + 2);

			f.Bones = new List<Bone>();
			i += 8;
			for (int bone = 0 ; bone < boneCount ; bone++)
			{
				Bone b = new Bone();
				b.Type = buffer.ReadShort(i + 0);
				Vector3Int boneTransform = buffer.ReadVector(i + 2);

				switch (b.Type)
				{
					case 0: //rotate
						if (!isAITD2)
						{
							b.Rotate = GetRotation(new Vector3(-boneTransform.x * 360 / 1024.0f, -boneTransform.y * 360 / 1024.0f, -boneTransform.z * 360 / 1024.0f));
						}
						break;

					case 1: //translate
						b.Position = new Vector3(boneTransform.x / 1000.0f, -boneTransform.y / 1000.0f, boneTransform.z / 1000.0f);
						break;

					case 2: //scale
						b.Scale = new Vector3(boneTransform.x / 256.0f + 1.0f, boneTransform.y / 256.0f + 1.0f, boneTransform.z / 256.0f + 1.0f);
						break;
				}

				i += 8;
				if (isAITD2)
				{
					boneTransform = buffer.ReadVector(i + 0);
					b.Rotate = GetRotation(new Vector3(-boneTransform.x * 360 / 1024.0f, -boneTransform.y * 360 / 1024.0f, -boneTransform.z * 360 / 1024.0f));
					i += 8;
				}

				f.Bones.Add(b);
			}

			animFrames.Add(f);
		}

		RefreshLeftText();
		frameDistance = Vector3Int.Zero;
	}

	Quaternion GetRotation(Vector3 angles)
	{
		return Quaternion.AngleAxis(angles.z, Vector3.forward) *
				Quaternion.AngleAxis(angles.x, Vector3.right) *
				Quaternion.AngleAxis(angles.y, Vector3.up);
	}

	Vector3 AnimateModel()
	{
		float totaltime = animFrames.Sum(x => x.Time);
		float scaledTime = Time.time * 50.0f;
		float time = scaledTime % totaltime;
		int animCounter = Mathf.FloorToInt(scaledTime / totaltime);

		//find current frame
		totaltime = 0.0f;
		int frame = 0;
		for (int i = 0 ; i < animFrames.Count ; i++)
		{
			totaltime += animFrames[(i + 1) % animFrames.Count].Time;
			if (time < totaltime)
			{
				frame = i;
				break;
			}
		}

		Frame currentFrame = animFrames[frame % animFrames.Count];
		Frame nextFrame = animFrames[(frame + 1) % animFrames.Count];
		float framePosition = (nextFrame.Time == 0) ? 0.0f : (time - (totaltime - nextFrame.Time)) / nextFrame.Time;
		if (frame != previousFrame)
		{
			previousFrame = frame;
			frameDistance += animFrames[frame % animFrames.Count].Offset;
		}

		int highLightFrame = (frame + 1) % animFrames.Count;
		if (highLightFrame != previousHighlightFrame)
		{
			RefreshLeftText(highLightFrame);
		}

		if (animFrames.Count == 1 && animCounter != previousAnimCounter)
		{
			previousAnimCounter = animCounter;
			frameDistance += animFrames[0].Offset;
		}

		for (int i = 0 ; i < bones.Count; i++)
		{
			Transform boneTransform = bones[i].transform;
			Vector3 position = Vector3.zero;
			Quaternion rotation = Quaternion.identity;
			Vector3 scale = Vector3.one;

			if (i < currentFrame.Bones.Count) //there is more bones in model than anim
			{
				var currentBone = currentFrame.Bones[i];
				var nextBone = nextFrame.Bones[i];

				//interpolate
				if (nextBone.Type == 0 || (modelFlags & 8) == 8) //rotation
				{
					rotation = Quaternion.Slerp(currentBone.Rotate, nextBone.Rotate, framePosition);
				}

				if (nextBone.Type == 1) //position
				{
					position = Vector3.Lerp(currentBone.Position, nextBone.Position, framePosition);
				}

				if (nextBone.Type == 2) //scaling
				{
					scale = Vector3.Lerp(currentBone.Scale, nextBone.Scale, framePosition);
				}
			}

			boneTransform.localPosition = initialBonesPosition[i] + position;
			boneTransform.localScale = scale;

			if ((modelFlags & 8) == 8)
			{
				boneTransform.rotation = transform.rotation * rotation;
			}
			else
			{
				boneTransform.localRotation = rotation;
			}
		}

		if (ShowAdditionalInfo.BoolValue && bones.Count > 0)
		{
			var scale = new Vector3(1.0f, -1.0f, 1.0f) / 1000.0f;
			return Vector3.Scale((Vector3)nextFrame.Offset * framePosition + frameDistance, scale);
		}
		else
		{
			return Vector3.zero;
		}
	}

	void ComputeUV(List<int> polyVertices, out Vector3 forward, out Vector3 left)
	{
		int lastPoly = polyVertices.Count - 1;
		Vector3 up;
		do
		{
			Vector3 a = allVertices[polyVertices[0]];
			Vector3 b = allVertices[polyVertices[1]];
			Vector3 c = allVertices[polyVertices[lastPoly]];
			left = (b - a).normalized;
			forward = (c - a).normalized;
			up = Vector3.Cross(left, forward).normalized;
			left = Vector3.Cross(up, forward).normalized;
			lastPoly--;
		} while (up == Vector3.zero && lastPoly > 1);
	}

	void Start()
	{
		CheckCommandLine();

		//parse vars.txt file
		string varPath = Config.GetPath("vars.txt");
		if (File.Exists(varPath))
		{
			varParser.Load(varPath, VarEnum.BODYS, VarEnum.ANIMS);
		}

		//load first model
		modelIndex = 0;
		textureCount = Texture.LoadTextures(textureFolder);

		try
		{
			LoadModels(modelFolders[modelFolderIndex]);
		}
		catch (FileNotFoundException ex)
		{
			LeftText.text = ex.Message;
			return;
		}

		LoadAnims(animFolders[modelFolderIndex]);
		ToggleAnimationMenuItems(false);
	}

	void SetPalette()
	{
		var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
		skinnedMeshRenderer.materials[2] //noise
			.SetTexture("_Palette", paletteTexture);
		skinnedMeshRenderer.materials[3] //gradientH
			.SetTexture("_Palette", paletteTexture);
		skinnedMeshRenderer.materials[4] //gradientV
			.SetTexture("_Palette", paletteTexture);
	}

	void LoadModels(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new PakArchive(filePath))
			{
				modelCount = pak.Count;
			}

			paletteTexture = Palette.GetPaletteTexture();
			SetPalette();

			LoadBody();
		}
	}

	void LoadAnims(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new PakArchive(filePath))
			{
				animCount = pak.Count;
			}

			if (EnableAnimation.BoolValue)
			{
				LoadAnim();
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
				AutoRotate.BoolValue = false;
			}

			//dragging (rotate)
			if (Input.GetMouseButton(0))
			{
				Vector2 mouseDelta = mousePosition - Input.mousePosition;
				cameraRotation += mouseDelta;
				cameraRotation.y = Mathf.Clamp(cameraRotation.y, -90.00f, 90.00f);
				mousePosition = Input.mousePosition;
			}

			//start drag (pan)
			if (Input.GetMouseButtonDown(2))
			{
				mousePosition = Input.mousePosition;
				AutoRotate.BoolValue = false;
			}

			//dragging (pan)
			if (Input.GetMouseButton(2))
			{
				Vector3 newMousePosition = Input.mousePosition;
				if (newMousePosition != mousePosition)
				{
					Vector3 cameraDistance = new Vector3(0.0f, 0.0f, cameraZoom);
					Vector2 mouseDelta = Camera.main.ScreenToWorldPoint(mousePosition + cameraDistance)
						- Camera.main.ScreenToWorldPoint(newMousePosition + cameraDistance);
					cameraPosition += mouseDelta;
					mousePosition = newMousePosition;
				}
			}
		}

		//show/hide menu
		if (Input.GetMouseButtonDown(1))
		{
			menuEnabled = !menuEnabled;
			if (menuEnabled)
			{
				if (modelCount > 0)
				{
					ModelInput.text = modelIndex.ToString();
				}

				if (animCount > 0)
				{
					AnimationInput.text = animIndex.ToString();
				}
			}
		}

		if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(2))
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			menuEnabled = false;
		}

		Panel.gameObject.SetActive(menuEnabled);

		modelIndex = Math.Max(Math.Min(modelIndex, modelCount - 1), 0);
		animIndex = Math.Max(Math.Min(animIndex, animCount - 1), 0);

		//load new model if needed
		if (modelCount > 0 && oldModelIndex != modelIndex)
		{
			ModelInput.text = modelIndex.ToString();
			LoadBody();
		}

		if (animCount > 0 && oldAnimIndex != animIndex)
		{
			AnimationInput.text = animIndex.ToString();
			LoadAnim();
		}

		//rotate model
		if (AutoRotate.BoolValue && AutoRotate.BoolValue)
		{
			float damp = 1.0f - Mathf.Pow(0.0001f, Time.deltaTime);
			cameraRotation.x += Time.deltaTime * 100.0f;
			cameraRotation.y = Mathf.Lerp(cameraRotation.y, 20.0f, damp);
			cameraPosition = Vector2.Lerp(cameraPosition, Vector2.zero, damp);
		}

		//animate
		Vector3 offset = Vector3.zero;
		if (animFrames != null && (modelFlags & 2) == 2 && animFrames.Count > 0)
		{
			offset = AnimateModel();
		}

		//rotate model around center
		transform.parent.rotation = Quaternion.identity;
		transform.position = Vector3.zero;
		Vector3 center = Vector3.Scale(gameObject.GetComponent<Renderer>().bounds.center, Vector3.up);
		transform.localPosition = -center;
		Grid.transform.localPosition = -center - new Vector3(offset.x, offset.y, Mathf.Repeat(offset.z, 1.0f));

		transform.parent.rotation = Quaternion.AngleAxis(cameraRotation.y, Vector3.left)
			* Quaternion.AngleAxis(cameraRotation.x, Vector3.up);

		//set camera
		Camera.main.transform.position = Vector3.back * cameraZoom + new Vector3(cameraPosition.x, cameraPosition.y, 0.0f);
		Camera.main.transform.rotation = Quaternion.AngleAxis(0.0f, Vector3.left);

		UpdateMesh();
	}

	Vector3 WorldToViewportPoint(Matrix4x4 mat, Vector3 pos)
	{
		//same as Camera.WorldToViewportPoint() but handle points behind camera correctly (when temp.w < 0)
		Vector4 temp = mat * new Vector4(pos.x, pos.y, pos.z, 1.0f);
		temp.x = (temp.x / Mathf.Abs(temp.w) + 1.0f) * 0.5f;
		temp.y = (temp.y / Mathf.Abs(temp.w) + 1.0f) * 0.5f;
		return new Vector3(temp.x, temp.y, temp.w);
	}

	void UpdateMesh()
	{
		Mesh mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
		if (mesh == null) return;

		if (EnableAnimation.BoolValue)
		{
			if (bakedMesh == null)
			{
				bakedMesh = new Mesh();
			}

			GetComponent<SkinnedMeshRenderer>().BakeMesh(bakedMesh);
			bakedMesh.GetVertices(allVertices);
		}

		Camera cam = Camera.main;
		Matrix4x4 mat = cam.projectionMatrix * cam.worldToCameraMatrix * transform.localToWorldMatrix;

		if (gradientPolygonList != null && gradientPolygonList.Count > 0)
		{
			UpdateGradients(mesh, mat);
		}
	}

	void UpdateGradients(Mesh mesh, Matrix4x4 mat)
	{
		float gmaxY = float.MinValue;
		float gminY = float.MaxValue;
		float gmaxZ = float.MinValue;
		float gminZ = float.MaxValue;

		for (int i = 0 ; i < gradientPolygonList.Count ; i++)
		{
			float maxX = float.MinValue;
			float minX = float.MaxValue;
			float maxY = float.MinValue;
			float maxZ = float.MinValue;

			int polyType = gradientPolygonType[i];

			foreach (int vertexIndex in gradientPolygonList[i])
			{
				Vector3 pos = allVertices[vertexIndex];
				Vector3 point = WorldToViewportPoint(mat, pos);
				point.y = Mathf.Clamp01(point.y);

				if (point.y > maxY) maxY = point.y;
				if (point.y > gmaxY) gmaxY = point.y;
				if (point.y < gminY) gminY = point.y;

				if (point.x > maxX) maxX = point.x;
				if (point.x < minX) minX = point.x;

				if (point.z > maxZ) maxZ = point.z;
				if (point.z > gmaxZ) gmaxZ = point.z;
				if (point.z < gminZ) gminZ = point.z;
			}

			foreach (int vertexIndex in gradientPolygonList[i])
			{
				switch (polyType)
				{
					case 4: //vertical gradient
					case 5: //vertical gradient (2X)
						uv[vertexIndex] = new Vector2(maxY, 0.0f);
						break;

					case 3: //horizontal
						uv[vertexIndex] = new Vector2(minX, maxX);
						break;

					case 6: //horizontal (reversed)
						uv[vertexIndex] = new Vector2(maxX, minX);
						break;
				}

				uvDepth[vertexIndex] = new Vector2(maxZ, 0.0f);
			}
		}

		float range = gmaxZ - gminZ;
		if (range == 0.0f) range = 1.0f;

		for (int i = 0; i < gradientPolygonList.Count; i++)
		{
			foreach (int vertexIndex in gradientPolygonList[i])
			{
				int polyType = gradientPolygonType[i];
				if (polyType == 4 || polyType == 5)
				{
					float maxY = uv[vertexIndex].x;
					uv[vertexIndex] = new Vector2(maxY, gmaxY - gminY);
				}

				float maxZ = uvDepth[vertexIndex].x;
				uvDepth[vertexIndex] = new Vector2((maxZ - gminZ) / range, 0.0f);
			}
		}

		mesh.SetUVs(0, uv);
		mesh.SetUVs(1, uvDepth);
	}

	void RefreshLeftText(int frameIndex = -1)
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendFormat("{0} {1}/{2} <color=#00c864>{3}</color>",
			Path.GetFileNameWithoutExtension(modelFolders[modelFolderIndex]),
			modelIndex, modelCount - 1,
			varParser.GetText(VarEnum.BODYS, modelIndex));

		if (EnableAnimation.BoolValue)
		{
			stringBuilder.Append("\r\n");
			stringBuilder.AppendFormat("{0} {1}/{2} <color=#00c864>{3}</color>",
				Path.GetFileNameWithoutExtension(animFolders[modelFolderIndex]),
				animIndex, animCount - 1,
				varParser.GetText(VarEnum.ANIMS, animIndex));
		}

		if (ShowAdditionalInfo.BoolValue)
		{
			stringBuilder.Append("\r\n\r\n");
			stringBuilder.AppendFormat("Bounding box ({3}): <color=#00c864>{0} {1} {2}</color>",
				boundingUpper.x - boundingLower.x,
				boundingUpper.y - boundingLower.y,
				boundingUpper.z - boundingLower.z,
				boundingBoxModes[boundingBoxMode]);
		}

		if (EnableAnimation.BoolValue && ShowAdditionalInfo.BoolValue && animFrames != null)
		{
			stringBuilder.Append("\r\n");

			int index = 0;
			foreach (Frame frame in animFrames)
			{
				bool selected = index == frameIndex || frameIndex == -1;
				string colorA = selected ? "#ffffff" : "#aaaaaa";
				string colorB = selected ? "#00c864" : "#008040";
				stringBuilder.AppendFormat("<color={5}>Frame {0}:</color> <color={6}>{1} {2} {3} {4}</color>\r\n", index, frame.Time, frame.Offset.x, frame.Offset.y, -frame.Offset.z, colorA, colorB);
				index++;
			}

			previousHighlightFrame = frameIndex;
		}

		LeftText.text = stringBuilder.ToString();
	}

	public void ToggleAnimationMenuItems(bool enabled)
	{
		AnimationInput.transform.parent.gameObject.SetActive(enabled);
		Panel.sizeDelta = new Vector2(Panel.sizeDelta.x, Panel.Cast<Transform>().Count(x => x.gameObject.activeSelf) * 30.0f);
	}

	public void ModelIndexInputChanged()
	{
		if (modelCount > 0)
		{
			int newModelIndex;
			if (int.TryParse(ModelInput.text, out newModelIndex) && newModelIndex != modelIndex)
			{
				modelIndex = newModelIndex;
				modelIndex = Math.Min(Math.Max(modelIndex, 0), modelCount - 1);
				LoadBody();
			}

			ModelInput.text = modelIndex.ToString();
		}
	}

	public void AnimationIndexInputChanged()
	{
		if (animCount > 0)
		{
			int newAnimIndex;
			if (int.TryParse(AnimationInput.text, out newAnimIndex) && newAnimIndex != animIndex)
			{
				animIndex = newAnimIndex;
				animIndex = Math.Min(Math.Max(animIndex, 0), animCount - 1);
				LoadAnim();
			}

			AnimationInput.text = animIndex.ToString();
		}
	}

	void CheckCommandLine()
	{
		var args = Environment.GetCommandLineArgs();
		if (args.Contains("-speedrun", StringComparer.InvariantCultureIgnoreCase))
		{
			ProcessKey(KeyCode.E); //extra info
			ProcessKey(KeyCode.A); //animation
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
				if (File.Exists(modelFolders[1]) && File.Exists(animFolders[1]))
				{
					modelFolderIndex = (modelFolderIndex + 1) % modelFolders.Length;
					LoadModels(modelFolders[modelFolderIndex]);
					LoadAnims(animFolders[modelFolderIndex]);
				}
				break;

			case KeyCode.UpArrow:
				if (EnableAnimation.BoolValue)
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
				if (EnableAnimation.BoolValue)
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
				if (animCount > 0)
				{
					EnableAnimation.BoolValue = !EnableAnimation.BoolValue;
					ToggleAnimationMenuItems(EnableAnimation.BoolValue);
					if (EnableAnimation.BoolValue)
					{
						LoadAnim();
					}
					else
					{
						animFrames = null;
						LoadBody(false);
					}
				}
				break;

			case KeyCode.E:
				ShowAdditionalInfo.BoolValue = !ShowAdditionalInfo.BoolValue;
				Grid.SetActive(ShowAdditionalInfo.BoolValue);
				BoundingBox.gameObject.SetActive(ShowAdditionalInfo.BoolValue);
				RefreshLeftText();
				break;

			case KeyCode.D:
				DetailsLevel.BoolValue = !DetailsLevel.BoolValue;
				LoadBody(false);
				break;

			case KeyCode.B:
				if (ShowAdditionalInfo.BoolValue)
				{
					boundingBoxMode = (boundingBoxMode + 1) % 3;
					LoadBody(false);
				}
				break;

			case KeyCode.R:
				AutoRotate.BoolValue = !AutoRotate.BoolValue;
				break;

			case KeyCode.X:
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					int previousModelIndex = modelIndex;

					for (int i = 0; i < modelCount; i++)
					{
						modelIndex = i;
						try
						{
							LoadBody();
						}
						catch
						{
							continue;
						}
						ExportToOBJ();
					}

					modelIndex = previousModelIndex;
					LoadBody();
				}
				else
				{
					ExportToOBJ();
				}
				menuEnabled = false;
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

	void ExportToOBJ()
	{
		Mesh mesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
		if (mesh == null) return;

		string exportFolder = "Export OBJ";
		Directory.CreateDirectory(exportFolder);

		string fileName = Path.Combine(exportFolder, string.Format("{0:D8}.obj", modelIndex));
		if (File.Exists(fileName)) return;

		using (StreamWriter writer = new StreamWriter(fileName))
		{
			writer.WriteLine("# Exported from AITD room viewer");

			var vertices = new List<Vector3>();
			mesh.GetVertices(vertices);

			for (int i = 0; i < vertices.Count && i < colorsRaw.Count ; i++)
			{
				Vector3 v = vertices[i];
				Color32 c = colorsRaw[i];
				writer.WriteLine("v {0} {1} {2} {3} {4} {5}", v.x, v.y, v.z, c.r / 255.0f, c.g / 255.0f, c.b / 255.0f);
			}

			foreach (Vector2 uv in mesh.uv)
			{
				writer.WriteLine("vt {0} {1}", uv.x, uv.y);
			}

			for (int material = 0; material < mesh.subMeshCount; material++)
			{
				int[] triangles = mesh.GetTriangles(material);
				if (triangles.Any())
				{
					writer.WriteLine("usemtl {0}", materialNames[material]);
					for (int i = 0; i < triangles.Length; i += 3)
					{
						writer.WriteLine("f {0}/{0} {1}/{1} {2}/{2}",
							triangles[i + 0] + 1,
							triangles[i + 1] + 1,
							triangles[i + 2] + 1);
					}
				}
			}
		}

		ExportTextures(exportFolder);
	}

	void ExportTextures(string exportFolder) //TIMEGATE only
	{
		var materials = GetComponent<SkinnedMeshRenderer>().sharedMaterials;
		var textureA = (Texture2D)materials[5].mainTexture;
		var textureB = (Texture2D)materials[6].mainTexture;

		if (textureA != null && textureA.height > 1)
		{
			File.WriteAllBytes(Path.Combine(exportFolder, string.Format("{0:D8}_a.png", modelIndex)), textureA.EncodeToPNG());
		}

		if (textureB != null && textureB.height > 1)
		{
			File.WriteAllBytes(Path.Combine(exportFolder, string.Format("{0:D8}_b.png", modelIndex)), textureB.EncodeToPNG());
		}
	}
}
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

	private string[] modelFolders = { Config.GetPath("LISTBODY.PAK"), Config.GetPath("LISTBOD2.PAK") };
	private string[] animFolders = { Config.GetPath("LISTANIM.PAK"), Config.GetPath("LISTANI2.PAK") };
	private string textureFolder = Config.GetPath("TEXTURES.PAK");

	private int modelCount;
	private int animCount;
	private int textureCount;

	private List<Frame> animFrames;
	private List<Transform> bones;
	private List<Vector3> initialBonesPosition;
	private int modelFlags;
	private int previousFrame;
	private Vector3Int frameDistance;

	private int paletteIndex;
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
	private Vector3Int boundingLower;
	private Vector3Int boundingUpper;

	private Vector2 cameraRotation = new Vector2(0.0f, 20.0f);
	private Vector2 cameraPosition;
	private float cameraZoom = 2.0f;

	private Vector3 mousePosition;
	//mouse drag
	private bool displayMenuAfterDrag;
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
		SkinnedMeshRenderer filter = this.gameObject.GetComponent<SkinnedMeshRenderer>();
		filter.sharedMesh = null;

		//delete all bones
		foreach (Transform child in transform) 
		{
			if(child.gameObject != BoundingBox)
			{
				GameObject.Destroy(child.gameObject);
			}			
		}

		//load data
		byte[] buffer;
		using (var pak = new UnPAK(filePath))
		{
			buffer = pak.GetEntry(modelIndex);
		}
		int i = 0;

		//header
		modelFlags = buffer.ReadShort(i + 0);

		//bounding box
		buffer.ReadBoundingBox(i + 2, out boundingLower, out boundingUpper);
		BoundingBox.transform.localScale = (Vector3)(boundingUpper - boundingLower) / 1000.0f;
		Vector3 pos = (Vector3)(boundingUpper + boundingLower) / 2000.0f;
		BoundingBox.transform.localPosition = new Vector3(pos.x, -pos.y, pos.z);

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

				if((modelFlags & 8) == 8)
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
		if (paletteIndex == 4) //TIMEGATE
		{
			LoadTextures(buffer, paletteColors, out uvStart, out texAHeight, out texBHeight);
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
						int numPoints = buffer[i + 0];
						int polyType = buffer[i + 1];
						int colorIndex = buffer[i + 2];
						i += 3;

						Color32 color = GetPaletteColor(paletteColors, colorIndex, polyType);
						List<int> triangleList = indices[GetTriangleListIndex(polyType)];

						//add vertices
						List<int> polyVertices = new List<int>();
						int verticesCount = allVertices.Count;
						for (int m = 0; m < numPoints; m++)
						{
							int pointIndex = buffer.ReadShort(i + 0) / 6;
							i += 2;

							colors.Add(color);
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
						Color32 color = GetPaletteColor(paletteColors, colorIndex, polyType);
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
						switch(primitiveType)
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
							uvIndex = uvStart + (buffer.ReadUnsignedShort(i + 1) / 16) * 3;
							bool texModel = (buffer[i + 1] & 0xF) == 0;
							textureHeight = (float)(texModel ? texAHeight : texBHeight);
							indicesIndex = texModel ? 5 : 6;
						}
						else
						{
							int colorIndex = buffer[i + 2];
							color = paletteColors[colorIndex];
						}

						i += 3;

						for(int k = 0 ; k < 3 ; k++)
						{
							int pointIndex = buffer.ReadShort(i + 0) / 6;
							i += 2;

							uvDepth.Add(Vector2.zero);
							indices[indicesIndex].Add(allVertices.Count);
							colors.Add(color);
							allVertices.Add(vertices[pointIndex]);
							boneWeights.Add(new BoneWeight() { boneIndex0 = bonesPerVertex[pointIndex], weight0 = 1 });

							if(primitiveType == 8 || primitiveType == 10)
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
		if(bones.Count > 0)
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
			frameDistance = Vector3Int.zero;
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

			case 3: //gradient
			case 6:
				return 3;

			case 4: //gradient2
			case 5:
				return 4;
		}
	}

	Color32 GetPaletteColor(Color32[] paletteColors, int colorIndex, int polyType)
	{
		Color32 color = paletteColors[colorIndex];

		if (polyType == 1 && DetailsLevel.BoolValue)
		{
			//noise
			color.r = (byte)((colorIndex % 16) * 16);
			color.g = (byte)((colorIndex / 16) * 16);
		}
		else if (polyType == 2)
		{
			//transparency
			color.a = 128;
		}
		else if ((polyType == 3 || polyType == 6 || polyType == 4 || polyType == 5) && DetailsLevel.BoolValue)
		{
			//horizontal or vertical gradient
			color.r = (byte)((polyType == 5) ? 127 : 255);
			color.b = (byte)((colorIndex / 16) * 16);
			color.a = (byte)((colorIndex % 16) * 16);
		}

		return color;
	}

	void LoadTextures(byte[] buffer, Color32[] paletteColors, out int uvStart, out int texAHeight, out int texBHeight)
	{
		var offset = buffer[0xE];
		Texture2D texA, texB;
		if(File.Exists(textureFolder))
		{
			paletteColors[0] = Color.clear;

			using (var pak = new UnPAK(textureFolder))
			{
				texA = LoadTexture(pak, buffer.ReadUnsignedShort(offset + 12), paletteColors);
				texB = LoadTexture(pak, buffer.ReadUnsignedShort(offset + 14), paletteColors);
			}
		}
		else
		{
			texA = EmptyTexture();
			texB = EmptyTexture();
		}

		uvStart = buffer.ReadShort(offset + 6);
		texAHeight = texA.height;
		texBHeight = texB.height;

		var materials = GetComponent<SkinnedMeshRenderer>().materials;
		materials[5].mainTexture = texA;
		materials[6].mainTexture = texB;
	}

	Texture2D LoadTexture(UnPAK pak, int textureIndex, Color32[] paletteColors)
	{
		if (textureIndex >= 0 && textureIndex < textureCount)
		{
			var tex256 = pak.GetEntry(textureIndex);
			FixBlackBorders(tex256);

			var texSize = tex256.Length;
			Color32[] textureData = new Color32[texSize];
			for (int i = 0 ; i < tex256.Length ; i++)
			{
				textureData[i] = paletteColors[tex256[i]];
			}

			Texture2D tex = new Texture2D(256, texSize / 256, TextureFormat.ARGB32, false);
			tex.filterMode = DetailsLevel.BoolValue ? FilterMode.Bilinear : FilterMode.Point;
			tex.SetPixels32(textureData);
			tex.Apply();

			return tex;
		}


		return EmptyTexture();
	}

	Texture2D EmptyTexture()
	{
		Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
		tex.SetPixel(1, 1, Color.magenta);
		tex.Apply();
		return tex;
	}

	void FixBlackBorders(byte[] tex256)
	{
		int width = 256;
		int height = tex256.Length / 256;

		byte[] result = tex256.ToArray();
		for (int j = 1 ; j < height - 1 ; j++)
		{
			for (int i = 1 ; i < width - 1 ; i++)
			{
				int n = i + j * 256;
				if (tex256[n] == 0) //black/transparent
				{
					if (tex256[n + 1] != 0) result[n] = tex256[n + 1];
					else if (tex256[n - 1] != 0) result[n] = tex256[n - 1];
					else if (tex256[n + 256] != 0) result[n] = tex256[n + 256];
					else if (tex256[n - 256] != 0) result[n] = tex256[n - 256];
					else if (tex256[n + 256 + 1] != 0) result[n] = tex256[n + 256 + 1];
					else if (tex256[n + 256 - 1] != 0) result[n] = tex256[n + 256 - 1];
					else if (tex256[n - 256 + 1] != 0) result[n] = tex256[n - 256 + 1];
					else if (tex256[n - 256 - 1] != 0) result[n] = tex256[n - 256 - 1];
				}
			}
		}

		Array.Copy(result, tex256, tex256.Length);
	}

	void LoadAnim()
	{
		string filePath = animFolders[modelFolderIndex];

		int i = 0;
		byte[] buffer;
		using (var pak = new UnPAK(filePath))
		{
			buffer = pak.GetEntry(animIndex);
		}

		int frameCount = buffer.ReadShort(i + 0);
		int boneCount = buffer.ReadShort(i + 2);
		i += 4;

		var isAITD2 = ((boneCount * 16 + 8) * frameCount + 4) == buffer.Length;
		animFrames = new List<Frame>();
		for(int frame = 0 ; frame < frameCount ; frame++)
		{
			Frame f = new Frame();
			f.Time = buffer.ReadShort(i + 0);
			f.Offset = buffer.ReadVector(i + 2);

			f.Bones = new List<Bone>();
			i += 8;
			for(int bone = 0 ; bone < boneCount ; bone++)
			{
				Bone b = new Bone();
				b.Type = buffer.ReadShort(i + 0);
				Vector3Int boneTransform = buffer.ReadVector(i + 2);

				switch(b.Type)
				{
					case 0: //rotate
						if(!isAITD2)
						{
							b.Rotate = GetRotation(new Vector3(-boneTransform.x * 360 / 1024.0f, -boneTransform.y * 360 / 1024.0f, -boneTransform.z * 360 / 1024.0f));
						}
						break;

					case 1: //translate
						b.Position = new Vector3(boneTransform.x / 1000.0f, -boneTransform.y / 1000.0f, boneTransform.z / 1000.0f);
						break;

					case 2: //scale
						b.Scale = new Vector3(boneTransform.x / 1024.0f + 1.0f, boneTransform.y / 1024.0f + 1.0f, boneTransform.z / 1024.0f + 1.0f);
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
		frameDistance = Vector3Int.zero;
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
		float framePosition = (nextFrame.Time == 0) ? 0.0f : (time - (totaltime - nextFrame.Time)) / nextFrame.Time;
		if(frame != previousFrame)
		{
			previousFrame = frame;
			frameDistance += animFrames[frame % animFrames.Count].Offset;
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

		if(!File.Exists(modelFolders[1]))
		{
			Array.Resize(ref modelFolders, 1);
		}

		//load first model
		modelIndex = 0;
		LoadTextures(textureFolder);
		LoadModels(modelFolders[modelFolderIndex]);
		LoadAnims(animFolders[modelFolderIndex]);
		ToggleAnimationMenuItems(false);
	}

	int DetectGame()
	{
		//detect game based on number of models
		if (modelCount > 700)
			return 3; //AITD3
		else if (modelCount > 550)
			return 2; //AITD2
		else if (modelCount > 500)
			return 5; //TIME GATE
		else if (modelCount > 200 || modelCount < 30)
			return 1; //AITD1 or DEMO
		else
			return 4; //JITD
	}

	void SetPalette()
	{
		GetComponent<SkinnedMeshRenderer>().materials[2] //noise
			.SetTexture("_Palette", paletteTexture);
		GetComponent<SkinnedMeshRenderer>().materials[3] //gradient
			.SetTexture("_Palette", paletteTexture);
		GetComponent<SkinnedMeshRenderer>().materials[4] //gradient2
			.SetTexture("_Palette", paletteTexture);
	}

	Texture2D GetPaletteTexture()
	{		
		switch (paletteIndex)
		{
			case 0: //AITD1
				return GetPaletteTexture("ITD_RESS.PAK", 3);

			case 1: //AITD2
				return GetPaletteTexture("ITD_RESS.PAK", 59, true);

			case 2: //AITD3
				return GetPaletteTexture("ITD_RESS.PAK", 47, true);
			
			case 3: //JITD
				return GetPaletteTexture("CAMERA16.PAK", 0, true, 64000);

			case 4: //TIME GATE
				return GetPaletteTexture("ITD_RESS.PAK", 43);
		}	

		throw new NotSupportedException();
	}

	Texture2D GetPaletteTexture(string filename, int entry, bool mapTo255 = false, int offset = 0)
	{
		Color32[] colors;
		filename = Config.GetPath(filename);
		if (File.Exists(filename))
		{
			colors = LoadPalette(filename, entry, mapTo255, offset);
		}
		else
		{
			colors = LoadDefaultPalette();
		}
		
		var texture = new Texture2D(16, 16);
		texture.SetPixels32(colors);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Apply();
		return texture;
	}

	Color32[] LoadPalette(string filename, int entry, bool mapTo255, int offset)
	{
		var colors = new Color32[256];
		using (var pak = new UnPAK(filename))
		{
			var buffer = pak.GetEntry(entry);
			for (int i = 0; i < 256; i++)
			{					
				byte r = buffer[i * 3 + 0 + offset];
				byte g = buffer[i * 3 + 1 + offset];
				byte b = buffer[i * 3 + 2 + offset];

				if (mapTo255)
				{
					colors[i] = new Color32((byte)(r << 2 | r >> 4), (byte)(g << 2 | g >> 4), (byte)(b << 2 | b >> 4), 255);
				}	
				else
				{
					colors[i] = new Color32(r, g, b, 255);
				}
			}
		}

		return colors;
	}

	Color32[] LoadDefaultPalette()
	{
		var colors = new Color32[256];
		for (int i = 0 ; i < 256 ; i++)
		{
			var color = (byte)i;
			colors[i] = new Color32(color, color, color, 255);
		}

		return colors;
	}

	void LoadModels(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new UnPAK(filePath))
			{
				modelCount = pak.EntryCount;
			}

			paletteIndex = DetectGame() - 1;
			paletteTexture = GetPaletteTexture();
			SetPalette();

			LoadBody();
		}
	}

	void LoadAnims(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new UnPAK(filePath))
			{
				animCount = pak.EntryCount;
			}

			if (EnableAnimation.BoolValue)
			{
				LoadAnim();
			}
		}
	}

	void LoadTextures(string filePath)
	{
		if (File.Exists(filePath))
		{
			using (var pak = new UnPAK(filePath))
			{
				textureCount = pak.EntryCount;
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
			if (Input.GetMouseButtonDown(1))
			{
				mousePosition = Input.mousePosition;
				AutoRotate.BoolValue = false;
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
					if(modelCount > 0)
					{
						ModelInput.text = modelIndex.ToString();
					}

					if (animCount > 0)
					{
						AnimationInput.text = animIndex.ToString();
					}
				}
			}
		}

		if (Input.GetMouseButtonUp(0)
			&& !RectTransformUtility.RectangleContainsScreenPoint(Panel, Input.mousePosition))
		{
			menuEnabled = false;
		}

		Panel.gameObject.SetActive(menuEnabled);

		modelIndex = Math.Min(Math.Max(modelIndex, 0), modelCount - 1);
		animIndex = Math.Min(Math.Max(animIndex, 0), animCount - 1);

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
		if(animFrames != null && (modelFlags & 2) == 2 && animFrames.Count > 0)
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
		Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
		if (mesh == null) return;

		if (EnableAnimation.BoolValue)
		{
			if (bakedMesh == null)
			{
				bakedMesh = new Mesh();
			}

			gameObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(bakedMesh);
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

			foreach(int vertexIndex in gradientPolygonList[i])
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
		if(range == 0.0f) range = 1.0f;

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

	void RefreshLeftText()
	{
		StringBuilder stringBuilder = new StringBuilder();

		stringBuilder.AppendFormat("{0} {1}/{2} <color=#00c864>{3}</color>",
			Path.GetFileNameWithoutExtension(modelFolders[modelFolderIndex]),
			modelIndex, modelCount - 1,
			varParser.GetText(VarEnum.BODYS, modelIndex));

		if(EnableAnimation.BoolValue)
		{
			stringBuilder.Append("\r\n");
			stringBuilder.AppendFormat("{0} {1}/{2} <color=#00c864>{3}</color>",
				Path.GetFileNameWithoutExtension(animFolders[modelFolderIndex]),
				animIndex, animCount - 1,
				varParser.GetText(VarEnum.ANIMS, animIndex));
		}

		if(ShowAdditionalInfo.BoolValue)
		{
			stringBuilder.Append("\r\n\r\n");
			stringBuilder.AppendFormat("Bounding box: <color=#00c864>{0} {1} {2}</color>\r\n",
				boundingUpper.x - boundingLower.x,
				boundingUpper.y - boundingLower.y,
				boundingUpper.z - boundingLower.z);
		}

		if(EnableAnimation.BoolValue && ShowAdditionalInfo.BoolValue && animFrames != null)
		{
			int index = 0;
			foreach(Frame frame in animFrames)
			{
				stringBuilder.AppendFormat("Frame {0}: <color=#00c864>{1} {2} {3} {4}</color>\r\n", index, frame.Time, frame.Offset.x, frame.Offset.y, -frame.Offset.z);
				index++;
			}
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
		var args = System.Environment.GetCommandLineArgs();
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
				if (modelFolders.Length > 1)
				{
					modelFolderIndex = (modelFolderIndex + 1) % modelFolders.Length;
					LoadModels(modelFolders[modelFolderIndex]);
					LoadAnims(animFolders[modelFolderIndex]);
				}				
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

			case KeyCode.R:
				AutoRotate.BoolValue = !AutoRotate.BoolValue;
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
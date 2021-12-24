using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraHelper : MonoBehaviour
{
	public Mesh CubeMesh;

	readonly Vector3[] quad = new Vector3[]
	{
		new Vector3( 1.0f, 1.0f, 1.0f),
		new Vector3( 1.0f, 1.0f,-1.0f),
		new Vector3(-1.0f, 1.0f,-1.0f),
		new Vector3(-1.0f, 1.0f, 1.0f)
	};

	readonly int[] lines =
	{
		0, 1, 1, 2, 2, 3, 3, 0, //far plane
		4, 5, 5, 6, 6, 7, 7, 4, //near plane
		4, 0, 5, 1, 6, 2, 7, 3, //edges between near and far
		9, 10, 11, 8            //parallel lines behind
	};

	public void SetupCameraFrustum(GameObject cameraFrustum, Box box)
	{
		cameraFrustum.GetComponent<MeshRenderer>().material.color = new Color32(255, 203, 75, 255);
		SetupTransform(cameraFrustum.transform, box.CameraPosition);

		var filter = cameraFrustum.GetComponent<MeshFilter>();
		filter.sharedMesh = CreateMesh(box.CameraRotation, box.CameraFocal);
	}

	public Mesh GetMeshFromPoints(List<Vector2> vertices2D, List<int> indices)
	{
		// Create the Vector3 vertices
		Vector3[] vertices = new Vector3[vertices2D.Count];
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = new Vector3(vertices2D[i].x, 0.0f, vertices2D[i].y);
		}

		// Create the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = indices.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	void SetupTransform(Transform transform, Vector3Int cameraPosition)
	{
		transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -cameraPosition.z) / 100.0f;
	}

	Mesh CreateMesh(Vector3Int cameraRotation, Vector3Int cameraFocal)
	{
		var qrot = Quaternion.Euler((Vector3)cameraRotation / 1024.0f * 360.0f);
		var offset = qrot * new Vector3(0.0f, 0.0f, cameraFocal.x) / 1000.0f;
		var focal = new Vector3(160.0f / cameraFocal.y, 1.0f, 100.0f / cameraFocal.z);

		Matrix4x4 matrix = Matrix4x4.TRS(-offset, qrot * Quaternion.Euler(90.0f, 0.0f, 0.0f), Vector3.one);

		var pts = quad.Select(x => x * 32.768f)
		  .Concat(quad.Select(x => x * 0.4f))
		  .Select(x => Vector3.Scale(x, focal))
		  .Concat(quad.Select(x => Vector3.Scale(x, new Vector3(-500.0f, 0.0f, 2.0f))))
		  .Select(x => matrix.MultiplyPoint3x4(x))
		  .ToArray();

		var vertices = new List<Vector3>();
		var triangles = new List<int>();

		for (int i = 0; i < lines.Length; i += 2)
		{
			AddLine(pts[lines[i]], pts[lines[i + 1]], triangles, vertices);
		}

		//2d border
		int[] borderTriangles;
		Vector3[] borderVertices;
		GetBorder(out borderTriangles, out borderVertices, 0.075f);

		triangles.AddRange(borderTriangles.Select(x => x + vertices.Count));
		vertices.AddRange(borderVertices);

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	void AddLine(Vector3 a, Vector3 b, List<int> indices, List<Vector3> vertices)
	{
		const float linesize = 0.04f;
		Vector3 directionVector = a - b;
		Vector3 middle = (a + b) / 2.0f;
		Quaternion rotation = Quaternion.LookRotation(directionVector);
		Vector3 scale = new Vector3(linesize, linesize, directionVector.magnitude);
		Matrix4x4 matrix = Matrix4x4.TRS(middle, rotation, scale);

		indices.AddRange(CubeMesh.triangles.Select(x => x + vertices.Count));
		vertices.AddRange(CubeMesh.vertices.Select(x => matrix.MultiplyPoint3x4(x)));
	}

	public Mesh SetupBorder()
	{
		int[] triangles;
		Vector3[] vertices;
		GetBorder(out triangles, out vertices, 0.3f);

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}

	void GetBorder(out int[] triangles, out Vector3[] vertices, float borderSize)
	{
		triangles = new int[]
		{
			0, 1, 5, 0, 5, 4,
			3, 6, 2, 7, 6, 3,
			0, 4, 7, 7, 3, 0,
			6, 5, 1, 6, 1, 2
		};

		const float outerSize = 32.768f;
		float innerSize = outerSize + borderSize;

		vertices = quad.Select(x => Vector3.Scale(x, new Vector3(innerSize, 0.0f, innerSize)))
			.Concat(quad.Select(x => Vector3.Scale(x, new Vector3(outerSize, 0.0f, outerSize))))
			.ToArray();
	}
}
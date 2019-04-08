using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraHelper : MonoBehaviour
{
	public Mesh CubeMesh;

	private static readonly Vector3[] quadA = new Vector3[] 
	{
		new Vector3( 1.0f, 1.0f, 1.0f), 
		new Vector3( 1.0f,-1.0f, 1.0f), 
		new Vector3(-1.0f,-1.0f, 1.0f),
		new Vector3(-1.0f, 1.0f, 1.0f) 
	};

	private static readonly Vector3[] quadB = new Vector3[] 
	{
		new Vector3(-500.0f, 2.0f, 0.0f), 
		new Vector3(-500.0f,-2.0f, 0.0f), 
		new Vector3( 500.0f,-2.0f, 0.0f),
		new Vector3( 500.0f, 2.0f, 0.0f)
	};

	public void SetupTransform(Box box, Vector3 cameraPosition, Vector3 cameraRotation, Vector3 cameraFocal) 
	{
		Vector3 rot = cameraRotation / 1024.0f * 360.0f;
		var qrot = Quaternion.Euler(rot.x, rot.y, rot.z);

		box.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, -cameraPosition.z) / 100.0f
			- qrot * new Vector3(0.0f, 0.0f, cameraFocal.x) / 1000.0f;
		box.transform.localRotation = qrot;			
	}

	public Mesh CreateMesh(Vector3 cameraFocal)
	{
		var pos = new Vector3(320.0f / cameraFocal.y * 0.5f, 200.0f / cameraFocal.z * 0.5f, 1.0f);
		
		var pts1 = quadA.Select(x => Vector3.Scale(x, pos) * 4.0f).ToArray();
		var pts2 = quadA.Select(x => Vector3.Scale(x, pos) * 0.4f).ToArray();
		var pts3 = quadB;

		List<Vector3> vertices = new List<Vector3>();
		List<int> indices = new List<int>();

		Action<Vector3, Vector3> addLine = (Vector3 a, Vector3 b) => 
		{
			AddLine(indices, vertices, a, b);
		};

		addLine(pts2[0], pts1[0]);
		addLine(pts2[1], pts1[1]);
		addLine(pts2[2], pts1[2]);
		addLine(pts2[3], pts1[3]);

		addLine(pts1[0], pts1[1]);
		addLine(pts1[1], pts1[2]);
		addLine(pts1[2], pts1[3]);
		addLine(pts1[3], pts1[0]);

		addLine(pts2[0], pts2[1]);
		addLine(pts2[1], pts2[2]);
		addLine(pts2[2], pts2[3]);
		addLine(pts2[3], pts2[0]);

		addLine(pts3[1], pts3[2]);
		addLine(pts3[3], pts3[0]);

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = indices.ToArray();

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	private void AddLine(List<int> indices, List<Vector3> vertices, Vector3 a, Vector3 b) 
	{
		const float linesize = 0.04f;
		Vector3 directionVector = a - b;
		Vector3 middle = (a + b) / 2.0f;
		Quaternion rotation = Quaternion.LookRotation(directionVector);
		
		indices.AddRange(CubeMesh.triangles.Select(x => x + vertices.Count));
		vertices.AddRange(CubeMesh.vertices.Select(x =>
			rotation * (Vector3.Scale(x, new Vector3(linesize, linesize, directionVector.magnitude)))
			+ middle));
	}
}
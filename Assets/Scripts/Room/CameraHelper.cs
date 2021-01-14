using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraHelper : MonoBehaviour
{
	public Mesh CubeMesh;

	private readonly List<Vector3> vertices = new List<Vector3>();
	private readonly List<int> indices = new List<int>();

	private readonly Vector3[] quad = new Vector3[]
	{
		new Vector3( 1.0f, 1.0f, 1.0f),
		new Vector3( 1.0f,-1.0f, 1.0f),
		new Vector3(-1.0f,-1.0f, 1.0f),
		new Vector3(-1.0f, 1.0f, 1.0f)
	};

	private readonly int[] lines =
	{
		0, 1, 1, 2, 2, 3, 3, 0,         //far plane
		4, 5, 5, 6, 6, 7, 7, 4,         //near plane
		4, 0, 5, 1, 6, 2, 7, 3,         //edges between near and far
		9, 10, 11, 8,                   //parallel lines
		12, 13, 13, 14, 14, 15, 15, 12  //2D border
	};

	public void SetupTransform(Transform transform, Vector3 cameraPosition)
	{
		var pos = new Vector3(cameraPosition.x, cameraPosition.y, -cameraPosition.z) / 100.0f;
		transform.position = pos;
	}

	public Mesh CreateMesh(Vector3 cameraRotation, Vector3 cameraFocal)
	{
		var rot = cameraRotation / 1024.0f * 360.0f;
		var qrot = Quaternion.Euler(rot.x, rot.y, rot.z);
		var offset = qrot * new Vector3(0.0f, 0.0f, cameraFocal.x) / 1000.0f;

		Matrix4x4 matrixA = Matrix4x4.TRS(-offset, qrot, Vector3.one);
		Matrix4x4 matrixB = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90.0f, 0.0f, 0.0f), new Vector3(32.767f, 32.767f, 0.0f));

		var foc = new Vector3(160.0f / cameraFocal.y, 100.0f / cameraFocal.z, 1.0f);

		var pts = quad.Select(x => Vector3.Scale(x, foc) * 32.767f)
		  .Concat(quad.Select(x => Vector3.Scale(x, foc) * 0.4f))
		  .Concat(quad.Select(x => Vector3.Scale(x, new Vector3(-500.0f, 2.0f, 0.0f))))
		  .Select(x => matrixA.MultiplyPoint3x4(x))
		  .Concat(quad.Select(x => matrixB.MultiplyPoint3x4(x)))
		  .ToArray();

		vertices.Clear();
		indices.Clear();

		for (int i = 0; i < lines.Length; i += 2)
		{
			AddLine(pts[lines[i]], pts[lines[i + 1]]);
		}

		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = indices.ToArray();

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	private void AddLine(Vector3 a, Vector3 b)
	{
		const float linesize = 0.04f;
		Vector3 directionVector = a - b;
		Vector3 middle = (a + b) / 2.0f;
		Quaternion rotation = Quaternion.LookRotation(directionVector);
		Vector3 scale = new Vector3(linesize, linesize, directionVector.magnitude);

		indices.AddRange(CubeMesh.triangles.Select(x => x + vertices.Count));
		vertices.AddRange(CubeMesh.vertices.Select(x => rotation * Vector3.Scale(x, scale) + middle));
	}

	public Mesh SetupBorder()
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

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		return mesh;
	}
}
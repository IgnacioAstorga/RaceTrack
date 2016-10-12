using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Shape2DExtrudeSegment : MonoBehaviour {

	public Shape2D shape;

	private Transform[] _controlPoints;

	private MeshFilter _meshFilter;

	void Awake() {
		_meshFilter = GetComponent<MeshFilter>();
		_controlPoints = this.GetComponentsInChildrenOnly<Transform>();
	}
	
	void Start() {
		if (shape == null) {
			Debug.LogWarning("WARNING: No shape selected!");
			return;
		}

		ExtrudeShape();
	}

	private void ExtrudeShape() {
		// Creates and populates the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = CreateVertices();
		mesh.normals = new Vector3[mesh.vertexCount];
		mesh.uv = new Vector2[mesh.vertexCount];
		mesh.triangles = CreateTriangles();

		_meshFilter.mesh = mesh;
	}

	private Vector3[] CreateVertices() {
		// Creates the vertices
		Vector3[] meshVertices = new Vector3[_controlPoints.Length * shape.points.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {
			// Caches some values
			Vector3 controlPointLocalPosition = _controlPoints[controlPointIndex].localPosition;
			int meshVertexBaseIndex = controlPointIndex * shape.points.Length;

			// For each point in the shape...
			for (int shapePointIndex = 0; shapePointIndex < shape.points.Length; shapePointIndex++) {
				int meshVertexIndex = meshVertexBaseIndex + shapePointIndex;
				meshVertices[meshVertexIndex] = controlPointLocalPosition;
				meshVertices[meshVertexIndex].x += shape.points[shapePointIndex].x;
				meshVertices[meshVertexIndex].y += shape.points[shapePointIndex].y;
			}
		}

		return meshVertices;
	}

	private int[] CreateTriangles() {
		// Creates the vertices
		int trianglesCount = 3 * (_controlPoints.Length - 1) * shape.lines.Length;
		int[] meshTriangles = new int[trianglesCount];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++) {
			int meshriangleBaseIndex = 3 * controlPointIndex * shape.lines.Length;
			int meshVertexBaseIndex = controlPointIndex * shape.points.Length;
			int meshVertexNextBaseIndex = (controlPointIndex + 1) * shape.points.Length;

			// For each line in the shape...
			for (int shapeLineIndex = 0; shapeLineIndex < shape.lines.Length; shapeLineIndex += 2) {
				// Each line generates 2 triangles, then 6 vertices
				int currentTriangle = meshriangleBaseIndex + 3 * shapeLineIndex;

				// Creates the first triangle
				meshTriangles[currentTriangle] = meshVertexBaseIndex + shape.lines[shapeLineIndex];
				meshTriangles[currentTriangle + 1] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
				meshTriangles[currentTriangle + 2] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];

				// Creates the first triangle
				meshTriangles[currentTriangle + 3] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];
				meshTriangles[currentTriangle + 4] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
				meshTriangles[currentTriangle + 5] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex + 1];
			}
		}

		return meshTriangles;
	}

	void OnDrawGizmos() {
		if (Application.isPlaying)
			return;

		Awake();
		Start();
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++)
			Gizmos.DrawLine(_controlPoints[controlPointIndex].position, _controlPoints[controlPointIndex + 1].position);
	}
}
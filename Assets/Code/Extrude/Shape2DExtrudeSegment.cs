using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class Shape2DExtrudeSegment : MonoBehaviour {

	public enum ControlPointRotation {
		Manual,
		AutomaticNormals,
		AutomaticOrientation,
		AutomaticBoth
	}

	public enum InterpolationMethod {
		Linear,
		Bezier
	}

	public Shape2D visualShape;
	public bool useCollider = true;
	public Shape2D colliderShape;
	public int resolution = 5;
	public float textureStretch = 1f;
	public InterpolationMethod interpolationMethod = InterpolationMethod.Bezier;
	public bool recalculateNormals = true;
	public bool closeShape = false;
	public Shape2D coverShape;
	public ControlPointRotation controlPointRotation = ControlPointRotation.Manual;

	private Shape2DExtrudeControlPoint[] _controlPoints;

	private MeshFilter _meshFilter;
	private MeshCollider _meshCollider;

	void Awake() {
		// Retrieves the desired components
		_meshFilter = GetComponent<MeshFilter>();
		_meshCollider = GetComponent<MeshCollider>();

		// Finds all the control points assigned to this segment
		_controlPoints = this.GetComponentsInChildrenOnly<Shape2DExtrudeControlPoint>();
	}
	
	void Start() {
		if (visualShape == null) {
			Debug.LogWarning("WARNING: No shape selected!");
			return;
		}

		// Calculates the rotation of the control points
		CalculateControlPointsRotation();

		// Extrudes the shape using the control points
		Mesh extrudedShape = ExtrudeShape(visualShape, coverShape);
		_meshFilter.sharedMesh = extrudedShape;

		// Creates the collider
		if (useCollider) {
			if (_meshCollider == null) {
				Debug.LogError("ERROR: No mesh collider attached to the entity!");
				return;
			}

			// If no collider shape is specified, uses the visual one
			if (colliderShape == null)
				_meshCollider.sharedMesh = extrudedShape;
			else
				_meshCollider.sharedMesh = ExtrudeShape(colliderShape, coverShape);
		}
	}

	void Update() {
		if (!Application.isPlaying) {
			Awake();
			Start();
		}
	}

	public Shape2DExtrudeControlPoint[] GetControlPoints() {
		return _controlPoints;
	}

	private void CalculateControlPointsRotation() {

		// At least two points are needed to calculae their rotation
		if (_controlPoints.Length < 2)
			return;

		// For each control point...
		for (int controlPointIndex = 1; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++) {

			// Calculates the direction from the previous control point
			Vector3 directionFromPrevious = _controlPoints[controlPointIndex].GetPosition() - _controlPoints[controlPointIndex - 1].GetPosition();

			// Calculates the direction to the next control point
			Vector3 directionToNext = _controlPoints[controlPointIndex + 1].GetPosition() - _controlPoints[controlPointIndex].GetPosition();

			// Calculates the related directions based on the previous ones
			Vector3 upDirection = _controlPoints[controlPointIndex].GetUpDirection();
			Vector3 forwardDirection = _controlPoints[controlPointIndex].GetForwardDirection();
			Vector3 tangentDirection = directionFromPrevious + directionToNext;

			// The normal needs more calculations
			Vector3 proxUpDirection = _controlPoints[controlPointIndex - 1].GetUpDirection() + _controlPoints[controlPointIndex + 1].GetUpDirection();
			Vector3 normalDirection = Vector3.Cross(directionFromPrevious, directionToNext);
			if (Vector3.Dot(proxUpDirection, normalDirection) < 0)
				normalDirection *= -1;

			// Calculates the rotation based on the segment's configuration
			Quaternion rotation;
			switch (controlPointRotation) {
				case ControlPointRotation.Manual:

					// Keep the control point's rotation
					rotation = _controlPoints[controlPointIndex].GetRotation();
					break;
				case ControlPointRotation.AutomaticNormals:

					// Keep the control point's orientation, but modify the normal
					rotation = Quaternion.LookRotation(forwardDirection, normalDirection);
					break;
				case ControlPointRotation.AutomaticOrientation:
					
					// Keep the control point's normal, but modify the orientation
					rotation = Quaternion.LookRotation(tangentDirection, upDirection);
					break;
				case ControlPointRotation.AutomaticBoth:

					// Modify both the control point's normal and orientation
					rotation = Quaternion.LookRotation(tangentDirection, normalDirection);
					break;
				default:
					throw new InvalidOperationException("The selected rotation method is not supported: " + controlPointRotation);
			}

			// Assigns the new rotation
			_controlPoints[controlPointIndex].GetTransform().localRotation = rotation;
		}
	}

	private Mesh ExtrudeShape(Shape2D visualShape, Shape2D coverShape = null) {
		// At least two control points are needed to extrude the shape
		if (_controlPoints.Length < 2) {
			Debug.LogWarning("WARNING: At least 2 control points needed to extrude!");
			return null;
		}

		// Creates and populates the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = CreateVertices(visualShape);
		mesh.uv = CreateUVs(visualShape);

		// Creates the mesh triangles
		mesh.triangles = CreateTriangles(visualShape);

		// Calculates the mesh's normals
		if (recalculateNormals)
			mesh.RecalculateNormals();
		else
			mesh.normals = CreateNormals(visualShape);

		// If the shape is closed, adds the covers
		if (closeShape) {
			try {
				Mesh closedMesh = new Mesh();
				if (coverShape == null)
					closedMesh.CombineMeshes(CloseShape(mesh, visualShape), false, false);
				else
					closedMesh.CombineMeshes(CloseShape(mesh, coverShape), false, false);
				mesh = closedMesh;
			}
			catch(InvalidOperationException) {
				Debug.LogError("ERROR: The selected shape is not closed and cannot be used as a cover!");
			}
		}

		return mesh;
	}

	private Vector3[] CreateVertices(Shape2D shape) {

		// Creates the vertices
		Vector3[] meshVertices = new Vector3[(resolution * (_controlPoints.Length - 1) + 1) * shape.points.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				Vector3 interpolatedPosition = InterpolatePosition(lerpFactor);
				Quaternion interpolatedRotation = InterpolateRotation(lerpFactor);
				Vector3 interpolatedScale = InterpolateScale(lerpFactor);

				// Caches some values
				int meshVertexBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.points.Length;

				// Creates a vertex for each point using the interpolated information
				Vector3[] newVertices = VerticesFromShape(shape, interpolatedPosition, interpolatedRotation, interpolatedScale);
				Array.Copy(newVertices, 0, meshVertices, meshVertexBaseIndex, newVertices.Length);

				// The last control point only has one resolution pass!
				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}

		return meshVertices;
	}

	private Vector3[] VerticesFromShape(Shape2D shape , Vector3 position, Quaternion rotation, Vector3 scale) {
		Vector3[] vertices = new Vector3[shape.points.Length];
		for (int shapePointIndex = 0; shapePointIndex < shape.points.Length; shapePointIndex++)
			vertices[shapePointIndex] = Shape2DExtrudeControlPoint.TransformPoint(shape.points[shapePointIndex], position, rotation, scale);
		return vertices;
	}

	private Vector3[] CreateNormals(Shape2D shape) {

		// Creates the normals
		Vector3[] meshNormals = new Vector3[(resolution * (_controlPoints.Length - 1) + 1) * shape.normals.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				Quaternion interpolatedRotation = InterpolateRotation(lerpFactor);

				// Caches some values
				int meshNormalBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.normals.Length;

				// For each normal in the shape...
				for (int shapeNormalIndex = 0; shapeNormalIndex < shape.normals.Length; shapeNormalIndex++) {

					// Assigns the normal to each vertex using the interpolated information
					int meshNormalIndex = meshNormalBaseIndex + shapeNormalIndex;
					meshNormals[meshNormalIndex] = Shape2DExtrudeControlPoint.TransformDirection(shape.normals[shapeNormalIndex], interpolatedRotation);
				}

				// The last control point only has one resolution pass!
				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}

		return meshNormals;
	}

	private Vector2[] CreateUVs(Shape2D shape) {

		// Creates the UVs
		Vector2[] meshUVs = new Vector2[(resolution * (_controlPoints.Length - 1) + 1) * shape.us.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				float interpolatedV = lerpFactor * resolution / textureStretch;

				// Caches some values
				int meshUVBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.us.Length;

				// For each U in the shape...
				for (int shapeUIndex = 0; shapeUIndex < shape.us.Length; shapeUIndex++) {

					// Assigns the UVs to each vertex using the interpolated information
					int meshUVIndex = meshUVBaseIndex + shapeUIndex;
					meshUVs[meshUVIndex] = new Vector2(shape.us[shapeUIndex], interpolatedV);
				}

				// The last control point only has one resolution pass!
				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}

		return meshUVs;
	}

	private int[] CreateTriangles(Shape2D shape) {

		// Creates the triangles
		int trianglesCount = 3 * resolution * (_controlPoints.Length - 1) * shape.lines.Length;
		int[] meshTriangles = new int[trianglesCount];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Caches some values
				int meshTriangleBaseIndex = 3 * (resolutionPass + controlPointIndex * resolution ) * shape.lines.Length;
				int meshVertexBaseIndex =  (resolutionPass + controlPointIndex * resolution) * shape.points.Length;
				int meshVertexNextBaseIndex = (resolutionPass + 1 + controlPointIndex * resolution) * shape.points.Length;

				// For each line in the shape...
				for (int shapeLineIndex = 0; shapeLineIndex < shape.lines.Length; shapeLineIndex += 2) {

					// Each line generates 2 triangles, then 6 vertices
					int currentTriangle = meshTriangleBaseIndex + 3 * shapeLineIndex;

					// Creates the first triangle
					meshTriangles[currentTriangle] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];
					meshTriangles[currentTriangle + 1] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 2] = meshVertexBaseIndex + shape.lines[shapeLineIndex];

					// Creates the first triangle
					meshTriangles[currentTriangle + 3] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 4] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 5] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];
				}
			}
		}

		return meshTriangles;
	}

	private CombineInstance[] CloseShape(Mesh originalMesh, Shape2D shape) {

		// Creates the structure that will hold the mesh and both covers
		CombineInstance[] covers = new CombineInstance[3];
		covers[0] = new CombineInstance();
		covers[1] = new CombineInstance();
		covers[2] = new CombineInstance();

		// Creates the covers
		covers[0].mesh = originalMesh;
		covers[1].mesh = CreateCover(shape, 0, false);
		covers[2].mesh = CreateCover(shape, _controlPoints.Length - 1, true);

		return covers;
	}

	private Mesh CreateCover(Shape2D shape, int controlPointIndex, bool reverse = false) {

		// Creates a mesh and populates it
		Mesh mesh = new Mesh();

		// Creates the vertices
		Vector3 interpolatedPosition = InterpolatePosition(controlPointIndex);
		Quaternion interpolatedRotation = InterpolateRotation(controlPointIndex);
		Vector3 interpolatedScale = InterpolateScale(controlPointIndex);
		mesh.vertices = VerticesFromShape(shape, interpolatedPosition, interpolatedRotation, interpolatedScale);

		// Triangulates the shape's points
		Triangulator triangulator = new Triangulator(shape.points);
		int[] coverTrianglesIndices = triangulator.Triangulate();
		if (reverse) {

			// The triangle order is now reversed so the triangles face the other direction
			for (int triangleIndex = 0; triangleIndex < coverTrianglesIndices.Length; triangleIndex += 3) {
				int temp = coverTrianglesIndices[triangleIndex];
				coverTrianglesIndices[triangleIndex] = coverTrianglesIndices[triangleIndex + 2];
				coverTrianglesIndices[triangleIndex + 2] = temp;
			}
		}
		mesh.triangles = coverTrianglesIndices;

		// If the shape is not closed, no triangles have been created. Throw an exception
		if (coverTrianglesIndices.Length == 0)
			throw new InvalidOperationException("The cover shape is not closed!");

		// Finally, calculates the normals and bounds
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();

		return mesh;
	}

	public Vector3 InterpolatePosition(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		switch (interpolationMethod) {
			case InterpolationMethod.Linear:
				return Vector3.Lerp(startControlPoint.GetPosition(), endControlPoint.GetPosition(), lerpFactor);
			case InterpolationMethod.Bezier:
				return BezierCurve.BezierPosition(startControlPoint.GetPosition(), startControlPoint.GetForwardHandlePosition(), endControlPoint.GetPosition(), endControlPoint.GetBackwardHandlePosition(), lerpFactor);
			default:
				throw new InvalidOperationException("The current interpolation method is not supported: " + interpolationMethod);
		}
	}

	public Quaternion InterpolateRotation(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		return Quaternion.Lerp(startControlPoint.GetRotation(), endControlPoint.GetRotation(), lerpFactor);
	}

	public Vector3 InterpolateScale(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		return Vector3.Lerp(startControlPoint.GetScale(), endControlPoint.GetScale(), lerpFactor);
	}

	void OnDrawGizmosSelected() {
		Vector3 previousPosition = _controlPoints[0].GetPosition();
		Matrix4x4 originalMatrix = Gizmos.matrix;
		Gizmos.matrix = transform.localToWorldMatrix;
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {
				if (controlPointIndex == 0 && resolutionPass == 0)
					continue;

				float factor = controlPointIndex + (float)resolutionPass / resolution;
				Vector3 position = InterpolatePosition(factor);
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(position, Shape2DExtrudeControlPoint.gizmosRadius / 4);

				Quaternion rotation = InterpolateRotation(factor);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(position, rotation * Vector3.up);

				Gizmos.color = Color.green;
				Gizmos.DrawLine(previousPosition, position);
				previousPosition = position;

				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}
		Gizmos.matrix = originalMatrix;
	}
}
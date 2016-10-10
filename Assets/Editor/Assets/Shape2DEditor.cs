using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Shape2D))]
public class Shape2DEditor : Editor {
	
	private Shape2D _shape2D;

	private Mesh _mesh;
	private Material _material;
	private PreviewRenderUtility _previewRenderUtility;

	private Vector2 _drag = new Vector2(45, -25);
	private float _distance = 15;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		if (GUILayout.Button("Show Editor Window"))
			Shape2DWindow.ShowWindow();
		if (GUILayout.Button("Recalculate Normals")) {
			Undo.RecordObject(_shape2D, "Recalculate Normals");
			_shape2D.RecalculateAllNormals();
		}
	}

	public override void OnPreviewGUI(Rect rectangle, GUIStyle background) {
		ReadInput(rectangle);

		if (Event.current.type == EventType.Repaint) {
			_previewRenderUtility.BeginPreview(rectangle, background);
			
			_previewRenderUtility.DrawMesh(_mesh, Matrix4x4.identity, _material, 0);

			_previewRenderUtility.m_Camera.transform.position = Vector2.zero;
			_previewRenderUtility.m_Camera.transform.rotation = Quaternion.Euler(new Vector3(-_drag.y, -_drag.x, 0));
			_previewRenderUtility.m_Camera.transform.position = _previewRenderUtility.m_Camera.transform.forward * -_distance;
			_previewRenderUtility.m_Camera.Render();

			Texture resultRender = _previewRenderUtility.EndPreview();
			GUI.DrawTexture(rectangle, resultRender, ScaleMode.StretchToFill, false);
		}
	}

	void OnDestroy() {
		if (_previewRenderUtility != null)
			_previewRenderUtility.Cleanup();
	}

	public override bool HasPreviewGUI() {

		_shape2D = (Shape2D)target;
		_mesh = MeshFromShape(_shape2D, 1);
		_material = new Material(Shader.Find("Unlit/Color NoCull"));

		if (_previewRenderUtility == null) {
			_previewRenderUtility = new PreviewRenderUtility();

			_previewRenderUtility.m_Camera.transform.position = new Vector3(0, 0, -_distance);
			_previewRenderUtility.m_Camera.transform.rotation = Quaternion.identity;
			_previewRenderUtility.m_Camera.farClipPlane = 50;
		}

		return true;
	}

	private Mesh MeshFromShape(Shape2D shape, int length) {
		// Creates the vertices
		Vector3[] meshVertices = new Vector3[2 * shape.points.Length];
		for (int i = 0; i < shape.points.Length; i++) {
			meshVertices[i] = new Vector3(shape.points[i].x, shape.points[i].y, 0);
			meshVertices[i + shape.points.Length] = new Vector3(shape.points[i].x, shape.points[i].y, length);
		}

		// Creates the normals
		Vector3[] meshNormals = new Vector3[2 * shape.normals.Length];
		for (int i = 0; i < shape.normals.Length; i++) {
			meshNormals[i] = new Vector3(shape.normals[i].x, shape.normals[i].y, 0);
			meshNormals[i + shape.normals.Length] = new Vector3(shape.normals[i].x, shape.normals[i].y, 0);
		}

		// Creates the UVs
		Vector2[] meshUVs = new Vector2[2 * shape.us.Length];
		for (int i = 0; i < shape.us.Length; i++) {
			meshUVs[i] = new Vector3(shape.us[i], 0);
			meshUVs[i + shape.us.Length] = new Vector3(shape.us[i], 0);
		}

		// Creates the triangles
		int[] meshTriangles = new int[3 * shape.lines.Length];
		for (int i = 0; i < shape.lines.Length; i += 2) {
			// Each line generates 2 triangles, then 6 vertices
			int currentTriangle = 3 * i;

			// Creates the first triangle
			meshTriangles[currentTriangle] = shape.lines[i];
			meshTriangles[currentTriangle + 1] = shape.lines[i + 1];
			meshTriangles[currentTriangle + 2] = shape.lines[i] + shape.points.Length;

			// Creates the first triangle
			meshTriangles[currentTriangle + 3] = shape.lines[i] + shape.points.Length;
			meshTriangles[currentTriangle + 4] = shape.lines[i + 1];
			meshTriangles[currentTriangle + 5] = shape.lines[i + 1] + shape.points.Length;
		}

		// Populates the mesh
		_mesh = new Mesh();
		_mesh.vertices = meshVertices;
		_mesh.normals = meshNormals;
		_mesh.uv = meshUVs;
		_mesh.triangles = meshTriangles;
		_mesh.RecalculateBounds();

		return _mesh;
	}

	public void ReadInput(Rect position) {
		int controlID = GUIUtility.GetControlID("Slider".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(controlID)) {
			case EventType.MouseDown:
				if (position.Contains(current.mousePosition) && position.width > 50f) {
					GUIUtility.hotControl = controlID;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID) {
					GUIUtility.hotControl = 0;
				}
				EditorGUIUtility.SetWantsMouseJumping(0);
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID) {
					_drag -= current.delta * (float)((!current.shift) ? 1 : 3) / Mathf.Min(position.width, position.height) * 140f;
					_drag.y = Mathf.Clamp(_drag.y, -90f, 90f);
					current.Use();
					GUI.changed = true;
				}
				break;
			case EventType.ScrollWheel:
				if (position.Contains(current.mousePosition)) {
					_distance += current.delta.y;
					_distance = Mathf.Max(0, _distance);
					current.Use();
					GUI.changed = true;
				}
				break;
		}
	}
}
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Shape2D))]
public class Shape2DEditor : Editor {
	
	private Shape2D _shape2D;

	private Mesh _mesh;
	private Material _material;

	private ScenePreview _preview;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		if (GUILayout.Button("Show Editor Window"))
			Shape2DWindow.ShowWindow();
		if (GUILayout.Button("Recalculate Normals")) {
			Undo.RecordObject(_shape2D, "Recalculate Normals");
			_shape2D.RecalculateAllNormals();
		}
		if (_material == null)
			_material = new Material(Shader.Find("Unlit/Texture NoCull"));
		_material = (Material) EditorGUILayout.ObjectField("Preview material", _material, typeof(Material), false);
	}

	public override void OnPreviewGUI(Rect rectangle, GUIStyle background) {
		if (_preview != null) {
			_preview.ReadInput(rectangle);
			if (Event.current.type == EventType.Repaint) {
				Texture resultRender = _preview.GetSceneTexture(rectangle, background);
				GUI.DrawTexture(rectangle, resultRender, ScaleMode.StretchToFill, false);
			}
		}
	}

	void OnDestroy() {
		if (_preview != null)
			_preview.CleanUp();
	}

	public override bool HasPreviewGUI() {

		_shape2D = (Shape2D)target;
		_mesh = MeshFromShape(_shape2D, 1);
		
		if (_preview == null && _material != null) {
			_preview = new ScenePreview();
			_preview.AddModel(_mesh, Matrix4x4.identity, _material);
		}

		return true;
	}

	public static Mesh MeshFromShape(Shape2D shape, float length) {
		// Creates the vertices
		Vector3[] meshVertices = new Vector3[2 * shape.points.Length];
		for (int i = 0; i < shape.points.Length; i++) {
			meshVertices[i] = new Vector3(shape.points[i].x, shape.points[i].y, -length / 2f);
			meshVertices[i + shape.points.Length] = new Vector3(shape.points[i].x, shape.points[i].y, length / 2f);
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
			meshUVs[i + shape.us.Length] = new Vector3(shape.us[i], 1);
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
		Mesh mesh = new Mesh();
		mesh.vertices = meshVertices;
		mesh.normals = meshNormals;
		mesh.uv = meshUVs;
		mesh.triangles = meshTriangles;
		mesh.RecalculateBounds();

		return mesh;
	}
}
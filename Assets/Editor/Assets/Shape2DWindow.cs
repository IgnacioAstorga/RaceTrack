using UnityEngine;
using UnityEditor;

public class Shape2DWindow : EditorWindow {

	private float _scale = 100;
	private Vector2 _offset = Vector2.zero;

	private float _pointRadius = 5f;
	private float _lineWidth = 4;
	private float _normalLength = 0.35f;
	private float _normalHandleSize = 0.75f;

	private Shape2D _shape2D;

	[MenuItem("Window/Shape 2D Editor")]
	public static void ShowWindow() {
		GetWindow<Shape2DWindow>();
	}

	public void LoadShape2D(Shape2D shape2D) {
		_shape2D = shape2D;
	}

	void OnGUI() {
		_shape2D = (Shape2D)EditorGUILayout.ObjectField(_shape2D, typeof(Shape2D), false);

		// Draw Shape
		if (_shape2D != null) {
			// Transforms the points
			Vector2[] points = new Vector2[_shape2D.points.Length];
			for (int i = 0; i < points.Length; i++)
				points[i] = position.size / 2 + _offset + _shape2D.points[i] * _scale;

			// Draws the lines
			for (int i = 0; i < _shape2D.lines.Length - 1; i += 2)
				DrawLine(points[_shape2D.lines[i]], points[_shape2D.lines[i + 1]], _lineWidth, Color.gray);

			// Draws the normals
			for (int i = 0; i < _shape2D.normals.Length; i++) {
				Vector2 normalHandle = points[i] + _shape2D.normals[i] * _normalLength * _scale;
				DrawLine(points[i], normalHandle, _lineWidth * _normalHandleSize, Color.cyan);
				DrawPoint(normalHandle, _pointRadius * _normalHandleSize, Color.cyan);
			}

			// Draws the points
			for (int i = 0; i < points.Length; i++)
				DrawPoint(points[i], _pointRadius, Color.white);
		}

		Repaint();
	}

	private void DrawLine(Vector2 point1, Vector2 point2,float width, Color color) {
		Handles.color = color;
		Handles.DrawAAPolyLine(width, point1, point2);
	}

	private void DrawPoint(Vector2 point, float radius, Color color) {
		Handles.color = color / 4;
		Handles.DrawSolidDisc(point, Vector3.forward, radius);
		Handles.color = color;
		Handles.DrawSolidDisc(point, Vector3.forward, radius - 1);
	}

	void TODO() {
		#region <<< Info // View >>>
		GUILayout.BeginHorizontal();

		#region Info <>
		// TODO

		#region ^^^ Points // Lines vvv
		GUILayout.BeginVertical();

		for (int i = 0; i < 20; i++)
			GUILayout.Label("dfs");

		GUILayout.EndVertical();
		#endregion ^^^ Points // Lines vvv

		// TODO
		#endregion Info <>

		#region View <>
		GUILayout.FlexibleSpace();
		GUILayout.Label("Lower part");
		#endregion View <>

		GUILayout.EndHorizontal();
		#endregion <<< Info // View >>>
	}
}
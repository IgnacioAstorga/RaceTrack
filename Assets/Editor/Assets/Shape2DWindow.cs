using UnityEngine;
using UnityEditor;

public class Shape2DWindow : EditorWindow {

	private float _scale = 100;
	private Vector2 _offset = Vector2.zero;

	private float _pointRadius = 5f;
	private float _lineWidth = 4f;
	private float _normalLength = 0.35f;
	private float _normalHandleSize = 0.75f;

	private float _cursorRectScale = 2f;

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

		// Draw Shape & Handle Events
		if (_shape2D != null) {
			// Transforms the points
			Vector2[] points = new Vector2[_shape2D.points.Length];
			for (int i = 0; i < points.Length; i++) {
				points[i] = _shape2D.points[i] * _scale;
				points[i].y *= -1;
				points[i] += position.size / 2 + _offset;
			}

			// Draws the lines
			for (int i = 0; i < _shape2D.lines.Length - 1; i += 2)
				DrawLine(points[_shape2D.lines[i]], points[_shape2D.lines[i + 1]], _lineWidth, Color.gray);

			// Draws the normals
			Vector2[] normalHandles = new Vector2[_shape2D.normals.Length];
			for (int i = 0; i < _shape2D.normals.Length; i++) {
				normalHandles[i] = _shape2D.normals[i] * _normalLength * _scale;
				normalHandles[i].y *= -1;
				normalHandles[i] += points[i];

				DrawLine(points[i], normalHandles[i], _lineWidth * _normalHandleSize, Color.cyan);
				Rect rect = DrawPoint(normalHandles[i], _pointRadius * _normalHandleSize, Color.cyan);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
				HandlePointEvents(ref normalHandles[i], rect);
			}

			// Draws the points and handles their events
			for (int i = 0; i < points.Length; i++) {
				Rect rect = DrawPoint(points[i], _pointRadius, Color.white);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
				HandlePointEvents(ref points[i], rect);
			}

			// Saves the changes to the shape
			Undo.RecordObject(_shape2D, "Modify Shape2D");

			// Saves the normals
			for (int i = 0; i < normalHandles.Length; i++) {
				normalHandles[i] -= points[i];
				normalHandles[i].y *= -1;
				_shape2D.normals[i] = normalHandles[i].normalized;
			}

			// Saves the points
			for (int i = 0; i < points.Length; i++) {
				points[i] -= position.size / 2 + _offset;
				points[i].y *= -1;
				_shape2D.points[i] = points[i] / _scale;
			}
		}

		Repaint();
	}

	private void DrawLine(Vector2 point1, Vector2 point2,float width, Color color) {
		Handles.color = color;
		Handles.DrawAAPolyLine(width, point1, point2);
	}

	private Rect DrawPoint(Vector2 point, float radius, Color color) {
		Handles.color = color / 4;
		Handles.DrawSolidDisc(point, Vector3.forward, radius);
		Handles.color = color;
		Handles.DrawSolidDisc(point, Vector3.forward, radius - 1);

		radius *= _cursorRectScale;
		return new Rect(point.x - radius, point.y - radius, 2 * radius, 2 * radius);
	}

	private void HandlePointEvents(ref Vector2 point, Rect area) {
		int pointID = GUIUtility.GetControlID("Point".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = pointID;
					current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == pointID && current.button == 0)
					GUIUtility.hotControl = 0;
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == pointID) {
					point += current.delta;
					current.Use();
					GUI.changed = true;
				}
				break;
		}
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
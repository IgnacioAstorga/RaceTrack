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

	private float _selectedPanelHeight = 80f;

	private Shape2D _shape2D;
	
	private int _selectedIndex = -1;

	private Vector2[] points;
	private Vector2[] normalHandles;

	[MenuItem("Window/Shape 2D Editor")]
	public static void ShowWindow() {
		GetWindow<Shape2DWindow>("Shape 2D Editor");
	}

	public void LoadShape2D(Shape2D shape2D) {
		_shape2D = shape2D;
	}

	void OnGUI() {
		// Retrieves the shape
		_shape2D = (Shape2D)EditorGUILayout.ObjectField(_shape2D, typeof(Shape2D), false);

		// Draw Shape & Handle Events
		if (_shape2D != null) {
			// Transforms the points
			points = new Vector2[_shape2D.points.Length];
			for (int i = 0; i < points.Length; i++) {
				points[i] = _shape2D.points[i] * _scale;
				points[i].y *= -1;
				points[i] += position.size / 2 + _offset;
			}

			// Draws the lines
			for (int i = 0; i < _shape2D.lines.Length - 1; i += 2)
				DrawLine(points[_shape2D.lines[i]], points[_shape2D.lines[i + 1]], _lineWidth, Color.gray);

			// Draws the points and handles their events
			for (int i = 0; i < points.Length; i++) {
				Rect rect = DrawPoint(points[i], _pointRadius, Color.white);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
				if (HandlePointEvents(ref points[i], rect))
					_selectedIndex = i;
				if (_selectedIndex == i) {
					Handles.color = Color.green;
					Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
				}
			}

			// Draws the normals
			normalHandles = new Vector2[_shape2D.normals.Length];
			for (int i = 0; i < _shape2D.normals.Length; i++) {
				normalHandles[i] = NormalToHandle(_shape2D.normals[i], points[i]);

				Vector2 normalOrigin = _shape2D.normals[i] * _pointRadius;
				normalOrigin.y *= -1;
				normalOrigin += points[i];

				DrawLine(normalOrigin, normalHandles[i], _lineWidth * _normalHandleSize, Color.cyan);
				Rect rect = DrawPoint(normalHandles[i], _pointRadius * _normalHandleSize, Color.cyan);
				EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
				if (HandlePointEvents(ref normalHandles[i], rect))
					_selectedIndex = i;
				if (_selectedIndex == i) {
					Handles.color = Color.green;
					Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
				}
			}

			// Draws the selected object information
			DrawSelected();

			// Saves the changes to the shape
			SaveChanges();
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

	private bool HandlePointEvents(ref Vector2 point, Rect area) {
		int pointID = GUIUtility.GetControlID("Point".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = pointID;
					current.Use();
					return true;
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
					return true;
				}
				break;
		}
		return false;
	}

	private void DrawSelected() {
		// Doesn't draw anything if no element is selected
		if (_selectedIndex == -1)
			return;

		// Draws the panel
		Rect panelRect = new Rect(0, position.height - _selectedPanelHeight, position.width, _selectedPanelHeight);
		GUILayout.BeginArea(panelRect, GUI.skin.box);

		// Draws the point field
		points[_selectedIndex] = EditorGUILayout.Vector2Field("Point", points[_selectedIndex]);

		// Draws the normal field
		Vector2 normal = HandleToNormal(normalHandles[_selectedIndex], points[_selectedIndex]);
		normal = EditorGUILayout.Vector2Field("Normal", normal);
		normalHandles[_selectedIndex] = NormalToHandle(normal, points[_selectedIndex]);

		GUILayout.EndArea();
	}

	private Vector2 NormalToHandle(Vector2 normal, Vector2 associatedPoint) {
		Vector2 handle = normal * _normalLength * _scale;
		handle.y *= -1;
		handle += associatedPoint;
		return handle;
	}

	private Vector2 HandleToNormal(Vector2 handle, Vector2 associatedPoint) {
		Vector2 normal = handle;
		normal -= associatedPoint;
		normal.y *= -1;
		return normal.normalized;
	}

	private void SaveChanges() {
		// Records an Undo command
		Undo.RecordObject(_shape2D, "Modify Shape2D");

		// Saves the normals
		for (int i = 0; i < normalHandles.Length; i++)
			_shape2D.normals[i] = HandleToNormal(normalHandles[i], points[i]);

		// Saves the points
		for (int i = 0; i < points.Length; i++) {
			points[i] -= position.size / 2 + _offset;
			points[i].y *= -1;
			_shape2D.points[i] = points[i] / _scale;
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
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class Shape2DWindow : EditorWindow {

	private bool HasSelection { get { return _selection.Count > 0; } }
	private bool HasClipboard { get { return _clipboardPoints != null && _clipboardPoints.Length > 0; } }
	private Rect CurrentArea { get { return _areas.Peek(); } }

	private float _scale = 100;
	private float _oldScale;
	private float _fixedScale = 100;
	private Vector2 _offset = Vector2.zero;
	private Vector2 _oldOffset;
	private Vector2 _pointListScroll = Vector2.zero;
	private bool _showingUs = false;
	private Texture _previewTexture = null;
	private bool _autoRecalculateNormals = true;
	private float _weldDistance = 0.05f;
	private float _shapePreviewExtrude = 1f;

	private float _pointRadius = 5f;
	private float _lineWidth = 5f;
	private float _arrowAngle = 30f;
	private float _arrowLength = 0.2f;
	private float _normalLength = 0.35f;
	private float _normalHandleSize = 0.75f;
	private float _selectionAreaExpansionDistance = 5f;

	private float _cursorRectScale = 2f;

	private float _resizeWidth = 5f;
	private float _shapeSelectorHeight = 20f;
	private float _upperRibbonHeight = 20f;
	private float _pointsListWidth = 180f;
	private float _textureSelectorHeight = 70f;
	private float _previewTextureSize = -1f;
	private float _optionsPanelHeight = 22.5f;
	private float _selectedPanelHeight = 120f;
	private float _selectedPanelWidth = 150f;

	private Stack<Rect> _areas = new Stack<Rect>();
	private Rect _mainAreaRect;
	private ScenePreview _preview;
	private Material _previewMaterial;

	private Shape2D _shape2D;
	
	private HashSet<int> _selection = new HashSet<int>();
	private Vector2 _selectionStart;
	private bool _selectionDragged;

	private Vector2[] _clipboardPoints;
	private Vector2[] _clipboardNormals;
	private float[] _clipboardUs;
	private int[] _clipboardLines;

	[MenuItem("Window/Shape 2D Editor")]
	public static void ShowWindow() {
		GetWindow<Shape2DWindow>("Shape 2D Editor");
	}

	public void LoadShape2D(Shape2D shape2D) {
		_shape2D = shape2D;
	}

	void OnGUI() {
		// Resets the drawing area
		_areas.Clear();
		_areas.Push(position);

		// Retrieves the shape
		DrawShapeSelector();

		// Fixes the selection
		_selection.RemoveWhere(e => e >= _shape2D.points.Length);

		// Draw Shape & Handle Events
		if (_shape2D != null) {

			// Records an Undo command
			Undo.RecordObject(_shape2D, "Modify Shape2D");
			EditorUtility.SetDirty(_shape2D);

			// Draws the upper ribbon
			DrawUpperRibbon();

			// Draws the list of points
			DrawPointsList();

			// Draws the texture preview
			if (_showingUs && _previewTexture != null)
				DrawTexturePreview();

			// Draws the main panel
			DrawMainPanel();

			if (_autoRecalculateNormals)
				_shape2D.RecalculateAllNormals();
		}

		Repaint();
	}

	private void BeginArea(Rect rectangle, GUIStyle style = null) {
		_areas.Push(rectangle);
		if (style == null)
			GUILayout.BeginArea(rectangle);
		else
			GUILayout.BeginArea(rectangle, style);
	}

	private void EndArea() {
		_areas.Pop();
		GUILayout.EndArea();
	}

	private void DrawShapeSelector() {
		if (_shape2D == null)
			_selection.Clear();
		_shape2D = (Shape2D)EditorGUILayout.ObjectField(_shape2D, typeof(Shape2D), false);
		if (Selection.activeObject != null && Selection.activeObject is Shape2D)
			LoadShape2D((Shape2D)Selection.activeObject);
	}

	private void DrawUpperRibbon() {
		EditorGUILayout.BeginHorizontal();

		// Edit
		if (GUILayout.Button("Edit")) {
			GenericMenu menu = new GenericMenu();
			if (HasSelection) {
				menu.AddItem(new GUIContent("Copy selected points #c"), false, CopySelectedPoints);
				menu.AddItem(new GUIContent("Cut selected points #x"), false, CutSelectedPoints);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Copy selected points #c"));
				menu.AddDisabledItem(new GUIContent("Cut selected points #x"));
			}
			if (HasClipboard) {
				menu.AddItem(new GUIContent("Paste copied points #v"), false, PasteCopiedPoints);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Paste copied points #v"));
			}
			menu.AddSeparator("");
			if (HasSelection) {
				menu.AddItem(new GUIContent("Focus selected points _f"), false, FocusSelection);
				menu.AddItem(new GUIContent("Center selection on origin #o"), false, CenterSelection, PointToScreen(Vector2.zero));
				menu.AddItem(new GUIContent("Center selection on screen #s"), false, CenterSelection, GetScreenCenter());
			}
			else {
				menu.AddDisabledItem(new GUIContent("Focus selected points _f"));
				menu.AddDisabledItem(new GUIContent("Center selection on origin #o"));
				menu.AddDisabledItem(new GUIContent("Center selection on screen #s"));
			}
			menu.AddSeparator("");
			if (HasSelection) {
				menu.AddItem(new GUIContent("Symmetrify selection horizontally"), false, SymmetryHorizontal);
				menu.AddItem(new GUIContent("Symmetrify selection vertically"), false, SymmetryVertical);
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Mirror selection horizontally global"), false, MirrorSelectionHorizontal, true);
				menu.AddItem(new GUIContent("Mirror selection vertically global"), false, MirrorSelectionVertical, true);
				menu.AddItem(new GUIContent("Mirror selection horizontally local"), false, MirrorSelectionHorizontal, false);
				menu.AddItem(new GUIContent("Mirror selection vertically local"), false, MirrorSelectionVertical, false);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Symmetrify selection horizontally"));
				menu.AddDisabledItem(new GUIContent("Symmetrify selection vertically"));
				menu.AddSeparator("");
				menu.AddDisabledItem(new GUIContent("Mirror selection horizontally global"));
				menu.AddDisabledItem(new GUIContent("Mirror selection vertically global"));
				menu.AddDisabledItem(new GUIContent("Mirror selection horizontally local"));
				menu.AddDisabledItem(new GUIContent("Mirror selection vertically local"));
			}
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Sort all points"), false, SortAllPoints, 0);
			menu.ShowAsContext();
		}

		// Points
		if (GUILayout.Button("Points")) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Create point _p"), false, CreatePoint, GetScreenCenter());
			if (HasSelection) {
				menu.AddItem(new GUIContent("Delete selected points _DELETE"), false, DeleteSelectedPoints);
				menu.AddItem(new GUIContent("Remove selected points #DELETE"), false, RemoveSelectedPoints);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Delete selected points _DELETE"));
				menu.AddDisabledItem(new GUIContent("Remove selected points #DELETE"));
			}
			menu.AddSeparator("");
			if (HasSelection) {
				menu.AddItem(new GUIContent("Break selected points _b"), false, BreakSelectedPoints);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Break selected points _b"));
			}
			menu.AddSeparator("");
			if (HasSelection) {
				menu.AddItem(new GUIContent("Shrink selected points _k"), false, ShrinkSelectedPoints);
				menu.AddItem(new GUIContent("Merge selected points _m"), false, MergeSelectedPoints);
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Weld selected points _w"), false, WeldSelectedPoints);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Shrink selected points _k"));
				menu.AddDisabledItem(new GUIContent("Merge selected points _m"));
				menu.AddSeparator("");
				menu.AddDisabledItem(new GUIContent("Weld selected points _w"));
			}
			menu.AddItem(new GUIContent("Weld all points #w"), false, WeldAllPoints);
			menu.ShowAsContext();
		}

		// Lines
		if (GUILayout.Button("Lines")) {
			GenericMenu menu = new GenericMenu();
			if (_selection.Count >= 2) {
				menu.AddItem(new GUIContent("Create line between selected points _l"), false, CreateLineBetweenSelectedPoints);
				menu.AddItem(new GUIContent("Remove lines between selected points &l"), false, RemoveLinesBetweenSelectedPoints);
				menu.AddItem(new GUIContent("Reverse lines between selected points #l"), false, ReverseSelectedLines); 
			}
			else {
				menu.AddDisabledItem(new GUIContent("Create line between selected points _l"));
				menu.AddDisabledItem(new GUIContent("Remove lines between selected points &l"));
				menu.AddDisabledItem(new GUIContent("Reverse lines between selected points #l"));
			}
			menu.AddSeparator("");
			if (_selection.Count >= 2) {
				menu.AddItem(new GUIContent("Divide selected lines _d"), false, DivideSelectedLines);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Divide selected lines _d"));
			}
			menu.ShowAsContext();
		}

		// Normals
		if (GUILayout.Button("Normals")) {
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Recalculate all normals #n"), false, RecalculateAllNormals);
			if (HasSelection) {
				menu.AddItem(new GUIContent("Recalculate selected normals _n"), false, RecalculateSelectedNormals);
				menu.AddItem(new GUIContent("Invert selected normals _i"), false, InvertSelectedNormals);
			}
			else {
				menu.AddDisabledItem(new GUIContent("Recalculate selected normals _n"));
				menu.AddDisabledItem(new GUIContent("Invert selected normals _i"));
			}
			menu.ShowAsContext();
		}

		EditorGUILayout.EndHorizontal();
	}

	private void DrawPointsList() {
		BeginArea(new Rect(0, _shapeSelectorHeight + _upperRibbonHeight, _pointsListWidth, CurrentArea.height - _shapeSelectorHeight - _upperRibbonHeight));

		float labelWidth = EditorGUIUtility.labelWidth;
		
		BeginArea(new Rect(0, 0, CurrentArea.width, CurrentArea.height - (_showingUs ? _textureSelectorHeight : 0)));

		if (!_showingUs)
			_showingUs = GUILayout.Button("Points");
		else
			_showingUs = !GUILayout.Button("Tex coords");

		_pointListScroll = EditorGUILayout.BeginScrollView(_pointListScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.textArea);

		EditorGUIUtility.labelWidth = 30;
		for (int i = 0; i < _shape2D.points.Length; i++) {
			Rect rect = EditorGUILayout.BeginHorizontal();
			if (_selection.Contains(i))
				EditorGUI.DrawRect(rect, new Color(1f, 0f, 0f, 0.5f));
			EditorGUILayout.PrefixLabel(i.ToString());
			if (!_showingUs)
				_shape2D.points[i] = EditorGUILayout.Vector2Field("", _shape2D.points[i]);
			else
				_shape2D.us[i] = EditorGUILayout.Slider(_shape2D.us[i], 0, 1, GUILayout.MinWidth(120));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Separator();

			HandlePointsListEvents(rect, i);
		}

		// Clear selection when clicking on no point, without alt nor control pressed
		Event current = Event.current;
		Rect area = new Rect(0, 0, CurrentArea.width, CurrentArea.height);
		if (current.type == EventType.MouseDown && current.button == 0 && !current.control && !current.alt && area.Contains(current.mousePosition))
			_selection.Clear();

		EditorGUILayout.EndScrollView();

		EndArea();

		if (_showingUs) {
			BeginArea(new Rect(0, CurrentArea.height - _textureSelectorHeight, CurrentArea.width, _textureSelectorHeight), GUI.skin.box);

			EditorGUIUtility.labelWidth = 60;
			_previewTexture = (Texture)EditorGUILayout.ObjectField("Texture", _previewTexture, typeof(Texture), false);

			EditorGUIUtility.labelWidth = labelWidth;

			EndArea();
		}

		EndArea();
	}

	private void HandlePointsListEvents(Rect area, int index) {
		Vector2 point = PointToScreen(_shape2D.points[index]);
		int pointListID = GUIUtility.GetControlID("PointList".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointListID)) {
			case EventType.ContextClick:
				if (area.Contains(current.mousePosition)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Delete point"), false, DeletePoint, index);
					menu.AddItem(new GUIContent("Remove point"), false, RemovePoint, index);
					if (_selection.Count <= 1) {
						menu.AddItem(new GUIContent("Copy point"), false, CopySelectedPoints);
						menu.AddItem(new GUIContent("Cut point"), false, CutSelectedPoints);
					}
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Delete selected points"), false, DeleteSelectedPoints);
						menu.AddItem(new GUIContent("Remove selected points"), false, RemoveSelectedPoints);
					}
					menu.AddSeparator("");
					if (_selection.Count >= 2) {
						menu.AddItem(new GUIContent("Create line between selected points"), false, CreateLineBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Remove lines between selected points"), false, RemoveLinesBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Reverse lines between selected points"), false, ReverseSelectedLines);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Divide selected lines"), false, DivideSelectedLines);
						menu.AddSeparator("");
					}
					menu.AddItem(new GUIContent("Recalculate normal"), false, RecalculateNormal, index);
					menu.AddItem(new GUIContent("Invert normal"), false, InvertNormal, index);
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Recalculate selected normals"), false, RecalculateSelectedNormals);
						menu.AddItem(new GUIContent("Invert selected normals"), false, InvertSelectedNormals);
					}
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Break point"), false, BreakPoint, index);
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Break selected points"), false, BreakSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Shrink selected points into this one"), false, ShrinkPoints, point);
						menu.AddItem(new GUIContent("Merge selected points into this one"), false, MergePoints, index);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Weld selected points"), false, WeldSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Copy selected points"), false, CopySelectedPoints);
						menu.AddItem(new GUIContent("Cut selected points"), false, CutSelectedPoints);
					}
					menu.AddSeparator("");
					if (HasClipboard) {
						menu.AddItem(new GUIContent("Paste copied points"), false, PasteCopiedPoints);
						menu.AddItem(new GUIContent("Paste copied points here"), false, PasteCopiedPoints, point);
					}
					else {
						menu.AddDisabledItem(new GUIContent("Paste copied points"));
						menu.AddDisabledItem(new GUIContent("Paste copied points here"));
					}
					if (_selection.Count <= 1 && HasClipboard && _clipboardPoints.Length == 1)
						menu.AddItem(new GUIContent("Paste point coordinates"), false, PastePointCoordinates, index);
					else
						menu.AddDisabledItem(new GUIContent("Paste point coordinates"));
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Sort all points starting on this one"), false, SortAllPoints, index);
					menu.ShowAsContext();
					current.Use();
				}
				break;
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition)) {
					if (current.button == 0) {
						if (current.alt)
							_selection.Remove(index);
						else if (current.control)
							_selection.Add(index);
						else {
							if (_selection.Count <= 1)
								_selection.Clear();
							_selectionDragged = false;
							GUIUtility.hotControl = pointListID;
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == pointListID) {
					if (current.button == 0) {
						GUIUtility.hotControl = 0;
						if (!current.control && _selection.Count > 1 && !_selectionDragged) {
							_selection.Clear();
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
		}
	}

	private Vector2 GetScreenCenter() {
		Vector2 center = new Vector2(_mainAreaRect.width / 2, _mainAreaRect.height / 2);
		return center - Vector2.up * (_shapeSelectorHeight + _upperRibbonHeight);
	}

	private Rect GetCurrentAreaSelectionRect() {
		return new Rect(0, 0, CurrentArea.width, CurrentArea.height);
	}

	private void DrawTexturePreview() {
		if (_previewTextureSize < 0)
			_previewTextureSize = Mathf.Min(CurrentArea.height - _shapeSelectorHeight + _upperRibbonHeight, CurrentArea.width - _pointsListWidth) / 2;

		// Draw texture preview
		Rect area = new Rect(_pointsListWidth, _shapeSelectorHeight + _upperRibbonHeight, CurrentArea.width - _pointsListWidth, _previewTextureSize);
		BeginArea(area);
		area = area.Expand(-1);

		float dimensions = Mathf.Min(area.height, area.width);
		Rect textureArea = new Rect(0, 0, dimensions, dimensions);
		BeginArea(textureArea, GUI.skin.box);
		textureArea = textureArea.Expand(-1);

		GUI.DrawTexture(textureArea, _previewTexture);

		for (int i = 0; i < _shape2D.us.Length; i++) {
			if (!_selection.Contains(i)) {
				Vector2 origin = textureArea.position;
				origin.x += (textureArea.width - 1) * _shape2D.us[i];
				Vector2 destination = origin;
				destination.y += textureArea.height;
				Handles.color = Color.green;
				Handles.DrawLine(origin, destination);
			}
		}

		foreach (int index in _selection) {
			Vector2 origin = textureArea.position;
			origin.x += (textureArea.width - 1) * _shape2D.us[index];
			Vector2 destination = origin;
			destination.y += textureArea.height;
			Handles.color = Color.red;
			Handles.DrawLine(origin, destination);
		}

		EndArea();

		// Draw model preview
		BeginArea(new Rect(_previewTextureSize, 0, CurrentArea.width - _previewTextureSize, CurrentArea.height));
		Rect previewArea = new Rect(0, 0, CurrentArea.width, CurrentArea.height);

		if (_preview == null)
			_preview = new ScenePreview();
		Rect inputArea = previewArea;
		inputArea.y += 20;
		inputArea.height -= _resizeWidth + 20;
		_preview.ReadInput(inputArea);

		if (Event.current.type == EventType.repaint) {
			if (_previewMaterial == null) {
				_previewMaterial = new Material(Shader.Find("Unlit/Texture NoCull"));
			}

			_previewMaterial.mainTexture = _previewTexture;
			_preview.ClearModels();
			_preview.AddModel(Shape2DEditor.MeshFromShape(_shape2D, _shapePreviewExtrude), Matrix4x4.identity, _previewMaterial);

			Texture render = _preview.GetSceneTexture(previewArea, GUI.skin.textArea);
			GUI.DrawTexture(previewArea, render, ScaleMode.StretchToFill, false);
		}

		_shapePreviewExtrude = EditorGUILayout.Slider(_shapePreviewExtrude, 0, 10);

		EndArea();

		EndArea();

		Rect resizeArea = new Rect(area.x, area.y + area.height - _resizeWidth, area.width, 2 * _resizeWidth);
		EditorGUIUtility.AddCursorRect(resizeArea, MouseCursor.ResizeVertical);
		HandleTexturePreviewEvents(resizeArea);
	}

	private void HandleTexturePreviewEvents(Rect resizeArea) {
		int resizeAreaID = GUIUtility.GetControlID("PointList".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(resizeAreaID)) {
			case EventType.MouseDown:
				if (resizeArea.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = resizeAreaID;
					current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == resizeAreaID && current.button == 0) {
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == resizeAreaID) {
					float delta = current.mousePosition.y - resizeArea.center.y;
					_previewTextureSize = Mathf.Clamp(_previewTextureSize + delta, 10, position.height - _shapeSelectorHeight - _upperRibbonHeight - 10);
				}
				break;
		}
	}

	private void DrawMainPanel() {
		if (_showingUs && _previewTexture != null)
			_mainAreaRect = new Rect(_pointsListWidth, _shapeSelectorHeight + _upperRibbonHeight + _previewTextureSize, CurrentArea.width - _pointsListWidth, CurrentArea.height - _previewTextureSize - _shapeSelectorHeight - _upperRibbonHeight);
		else
			_mainAreaRect = new Rect(_pointsListWidth, _shapeSelectorHeight + _upperRibbonHeight, CurrentArea.width - _pointsListWidth, CurrentArea.height - _shapeSelectorHeight - _upperRibbonHeight);
		BeginArea(_mainAreaRect);
		
		// Saves the offset and scale values. Transforms the values
		_oldOffset = _offset;
		_oldScale = _scale;

		// Draws the background
		DrawBackground(_oldScale);

		// Draws the selection rect
		DrawSelectionRect();

		// Draws the lines
		for (int i = 0; i < _shape2D.lines.Length - 1; i += 2)
			DrawLineWithArrow(PointToScreen(_shape2D.points[_shape2D.lines[i]]), PointToScreen(_shape2D.points[_shape2D.lines[i + 1]]), _lineWidth, Color.blue, _arrowAngle, _arrowLength);

		// Draws the points
		for (int i = 0; i < _shape2D.points.Length; i++) {
			Rect rect = DrawPoint(PointToScreen(_shape2D.points[i]), _pointRadius, Color.white);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
		}

		// Draws the normals
		for (int i = 0; i < _shape2D.normals.Length; i++) {
			Vector2 normalOrigin = _shape2D.normals[i].normalized * _pointRadius;
			normalOrigin.y *= -1;
			normalOrigin += PointToScreen(_shape2D.points[i]);

			Vector2 handle = NormalToHandle(_shape2D.normals[i], _shape2D.points[i]);
			DrawLine(normalOrigin, handle, _lineWidth * _normalHandleSize, Color.cyan);
			Rect rect = DrawPoint(handle, _pointRadius * _normalHandleSize, Color.cyan);
			EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
		}

		// Handles the points events
		HandlePointsEvents();

		// Handles the normals evenets
		HandleNormalsEvents();

		// Manages the mouse and keyboard events
		HandleSelectionEvents();
		HandleMouseEvents(GetCurrentAreaSelectionRect());
		HandleKeyboardEvents();

		// Draws the options panel
		DrawOptionsPanel();

		// Draws the selected object information
		DrawSelected();

		// Adds a border to the area
		Handles.color = Color.white;
		Handles.DrawSolidRectangleWithOutline(new Rect(0, 0, CurrentArea.width - 1, CurrentArea.height - 1), Color.clear, Color.gray);

		EndArea();
	}

	private void DrawBackground(float scale) {
		while (scale > _fixedScale)
			scale /= 2;
		Vector2 origin = CurrentArea.size / 2 + _oldOffset.Module(scale);

		// Draws the horizontal grid
		for (float x = 0; x < CurrentArea.width / 2; x += scale) {
			Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
			Handles.DrawLine(new Vector2(origin.x + x, 0), new Vector2(origin.x + x, CurrentArea.height));
			Handles.DrawLine(new Vector2(origin.x - x, 0), new Vector2(origin.x - x, CurrentArea.height));
		}

		// Draws the vertical grid
		for (float y = 0; y < CurrentArea.height / 2; y += scale) {
			Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);
			Handles.DrawLine(new Vector2(0, origin.y + y), new Vector2(CurrentArea.width, origin.y + y));
			Handles.DrawLine(new Vector2(0, origin.y - y), new Vector2(CurrentArea.width, origin.y - y));
		}

		// Draws the main axis
		Vector2 center = CurrentArea.size / 2 + _oldOffset;
		Handles.color = Color.red;
		Handles.DrawLine(new Vector2(0, center.y), new Vector2(CurrentArea.width, center.y));
		Handles.color = Color.green;
		Handles.DrawLine(new Vector2(center.x, 0), new Vector2(center.x, CurrentArea.height));

		// Draws the ADD and REMOVE rects
		if (Event.current.alt)
			EditorGUIUtility.AddCursorRect(GetCurrentAreaSelectionRect(), MouseCursor.ArrowMinus);
		else if (Event.current.control)
			EditorGUIUtility.AddCursorRect(GetCurrentAreaSelectionRect(), MouseCursor.ArrowPlus);
	}

	private void DrawSelectionRect() {
		Rect selectionRect = GetSelectionRect(true);
		selectionRect = selectionRect.Expand(_selectionAreaExpansionDistance);
		Color faceColor = new Color(0.5f, 1f, 0.5f, 0.25f);
		Color outlineColor = new Color(0f, 0.5f, 0f, 0.5f);
		Handles.color = Color.white;
		Handles.DrawSolidRectangleWithOutline(selectionRect, faceColor, outlineColor);
	}

	private void DrawLineWithArrow(Vector2 point1, Vector2 point2, float width, Color color, float arrowAngle, float arrowWidth) {
		DrawLine(point1, point2, width, color);

		Vector2 direction = (point2 - point1).normalized * _fixedScale * arrowWidth;
		Vector3 arrowSegmentA = new Vector3(point2.x, point2.y, 0) - Quaternion.Euler(0, 0, arrowAngle) * direction;
		Vector3 arrowSegmentB = new Vector3(point2.x, point2.y, 0) - Quaternion.Euler(0, 0, -arrowAngle) * direction;
		Handles.DrawAAPolyLine(width, point2, arrowSegmentA);
		Handles.DrawAAPolyLine(width, point2, arrowSegmentB);
	}

	private void DrawLine(Vector2 point1, Vector2 point2, float width, Color color) {
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

	private void HandlePointsEvents() {
		// Points events
		float pointRadius = _cursorRectScale * _pointRadius;
		for (int i = 0; i < _shape2D.points.Length; i++) {
			Vector2 point = PointToScreen(_shape2D.points[i]);
			Rect rect = new Rect(point.x - pointRadius, point.y - pointRadius, 2 * pointRadius, 2 * pointRadius);
			Vector2 newPoint = HandleEvents(point, rect, i);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}

			// Moves all the selected points
			if (point != newPoint) {
				Vector2 previousPosition = _shape2D.points[i];
				_shape2D.points[i] = ScreenToPoint(newPoint);
				Vector2 movement = _shape2D.points[i] - previousPosition;
				foreach (int index in _selection)
					if (i != index)
						_shape2D.points[index] += movement;
			}
		}
	}

	private void HandleNormalsEvents() {
		// Normal events
		float handleRadius = _cursorRectScale * _pointRadius * _normalHandleSize;
		for (int i = 0; i < _shape2D.normals.Length; i++) {
			Vector2 handle = NormalToHandle(_shape2D.normals[i], _shape2D.points[i]);
			Rect rect = new Rect(handle.x - handleRadius, handle.y - handleRadius, 2 * handleRadius, 2 * handleRadius);
			_shape2D.normals[i] = HandleToNormal(HandleEvents(handle, rect, i), _shape2D.points[i]);
			if (_selection.Contains(i)) {
				Handles.color = Color.red;
				Handles.DrawWireDisc(rect.center, Vector3.forward, rect.width / 2);
			}
		}
	}

	private Vector2 HandleEvents(Vector2 point, Rect area, int index) {
		int pointID = GUIUtility.GetControlID("Point".GetHashCode(), FocusType.Passive);
		Event current = Event.current;
		switch (current.GetTypeForControl(pointID)) {
			case EventType.ContextClick:
				if (area.Contains(current.mousePosition)) {
					GenericMenu menu = new GenericMenu();
					menu.AddItem(new GUIContent("Delete point"), false, DeletePoint, index);
					menu.AddItem(new GUIContent("Remove point"), false, RemovePoint, index);
					if (_selection.Count <= 1) {
						menu.AddItem(new GUIContent("Copy point"), false, CopySelectedPoints);
						menu.AddItem(new GUIContent("Cut point"), false, CutSelectedPoints);
					}
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Delete selected points"), false, DeleteSelectedPoints);
						menu.AddItem(new GUIContent("Remove selected points"), false, RemoveSelectedPoints);
					}
					menu.AddSeparator("");
					if (_selection.Count >= 2) {
						menu.AddItem(new GUIContent("Create line between selected points"), false, CreateLineBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Remove lines between selected points"), false, RemoveLinesBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Reverse lines between selected points"), false, ReverseSelectedLines);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Divide selected lines"), false, DivideSelectedLines);
						menu.AddSeparator("");
					}
					menu.AddItem(new GUIContent("Recalculate normal"), false, RecalculateNormal, index);
					menu.AddItem(new GUIContent("Invert normal"), false, InvertNormal, index);
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Recalculate selected normals"), false, RecalculateSelectedNormals);
						menu.AddItem(new GUIContent("Invert selected normals"), false, InvertSelectedNormals);
					}
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Break point"), false, BreakPoint, index);
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Break selected points"), false, BreakSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Shrink selected points into this one"), false, ShrinkPoints, point);
						menu.AddItem(new GUIContent("Merge selected points into this one"), false, MergePoints, index);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Weld selected points"), false, WeldSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Copy selected points"), false, CopySelectedPoints);
						menu.AddItem(new GUIContent("Cut selected points"), false, CutSelectedPoints);
					}
					menu.AddSeparator("");
					if (HasClipboard) {
						menu.AddItem(new GUIContent("Paste copied points"), false, PasteCopiedPoints);
						menu.AddItem(new GUIContent("Paste copied points here"), false, PasteCopiedPoints, point);
					}
					else {
						menu.AddDisabledItem(new GUIContent("Paste copied points"));
						menu.AddDisabledItem(new GUIContent("Paste copied points here"));
					}
					if (_selection.Count <= 1 && HasClipboard && _clipboardPoints.Length == 1)
						menu.AddItem(new GUIContent("Paste point coordinates"), false, PastePointCoordinates, index);
					else
						menu.AddDisabledItem(new GUIContent("Paste point coordinates"));
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Sort all points starting on this one"), false, SortAllPoints, index);
					menu.ShowAsContext();
					current.Use();
				}
				break;
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition)) {
					if (current.button == 0) {
						if (current.alt)
							_selection.Remove(index);
						else if (current.control)
							_selection.Add(index);
						else {
							if (_selection.Count <= 1)
								_selection.Clear();
							_selectionDragged = false;
							GUIUtility.hotControl = pointID;
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == pointID) {
					if (current.button == 0) {
						GUIUtility.hotControl = 0;
						if (!current.control && _selection.Count > 1 && !_selectionDragged) {
							_selection.Clear();
							_selection.Add(index);
						}
						current.Use();
					}
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == pointID) {
					point = current.mousePosition;
					_selectionDragged = true;
					current.Use();
				}
				break;
		}
		return point;
	}

	private void ShrinkSelectedPoints() {
		Vector2 avg = Vector2.zero;
		foreach (int index in _selection)
			avg += _shape2D.points[index];
		avg /= _selection.Count;
		ShrinkPoints(PointToScreen(avg));
	}

	private void ShrinkPoints(object point) {
		try {
			Vector2 pointCoordinates = ScreenToPoint((Vector2)point);
			foreach (int index in _selection)
				_shape2D.points[index] = pointCoordinates;
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point: " + point + "\n" + e);
		}
	}

	private void MergeSelectedPoints() {
		Vector2 avg = Vector2.zero;
		Vector2 normal = Vector2.zero;
		float u = 0;
		foreach (int index in _selection) {
			avg += _shape2D.points[index];
			normal += _shape2D.normals[index];
			u += _shape2D.us[index];
		}
		avg /= _selection.Count;
		u /= _selection.Count;
		normal = normal.normalized;
		MergePoints(avg, normal, u);
	}

	private void MergePoints(object pointIndex) {
		try {
			int index = Convert.ToInt32(pointIndex);
			MergePoints(_shape2D.points[index], _shape2D.normals[index], _shape2D.us[index]);
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void MergePoints(Vector2 point, Vector2 normal, float u) {
		// Stores the lines to the selected points
		List<int> lineOrigins = new List<int>();
		List<int> lineDestinations = new List<int>();
		for (int i = 0; i < _shape2D.lines.Length; i += 2) {
			if (!_selection.Contains(_shape2D.lines[i]) && _selection.Contains(_shape2D.lines[i + 1]))
				lineOrigins.Add(_shape2D.lines[i]);
			if (_selection.Contains(_shape2D.lines[i]) && !_selection.Contains(_shape2D.lines[i + 1]))
				lineDestinations.Add(_shape2D.lines[i + 1]);
		}

		// Stores the selection
		int[] selectionCopy = new int[_selection.Count];
		_selection.CopyTo(selectionCopy);
		_selection.Clear();

		// Creates a new point and sets it's normal
		_shape2D.AddPoint(point);
		int pointIndex = _shape2D.points.Length - 1;
		_shape2D.normals[pointIndex] = normal;
		_shape2D.us[pointIndex] = u;

		// Recreates the lines
		foreach (int lineOrigin in lineOrigins)
			_shape2D.CreateLine(lineOrigin, pointIndex);
		foreach (int lineDestination in lineDestinations)
			_shape2D.CreateLine(pointIndex, lineDestination);

		// Removes the selected points
		Array.Sort(selectionCopy);
		for (int i = selectionCopy.Length - 1; i >= 0; i--)
			_shape2D.DeletePoint(selectionCopy[i]);
	}

	private void WeldAllPoints() {
		int[] indices = new int[_shape2D.points.Length];
		for (int i = 0; i < indices.Length; i++)
			indices[i] = i;
		WeldPoints(indices, _weldDistance);
	}

	private void WeldSelectedPoints() {
		WeldPoints(_selection, _weldDistance);
	}

	private void WeldPoints(IEnumerable<int> pointIndices, float distance) {

		// Groups the points by distance
		HashSet<int> remainingPoints = new HashSet<int>(pointIndices);
		List<List<int>> weldings = new List<List<int>>();
		while (remainingPoints.Count > 0) {

			// For each point, find the close ones
			List<int> nearPoints = new List<int>();
			foreach (int index in remainingPoints) {
				if (nearPoints.Count == 0)
					nearPoints.Add(index);
				else if (Vector2.Distance(_shape2D.points[nearPoints[0]], _shape2D.points[index]) <= distance)
					nearPoints.Add(index);
			}

			// Removes the points from the remaining points set
			foreach (int index in nearPoints) {
				remainingPoints.Remove(index);
			}
			weldings.Add(nearPoints);
		}

		// For each welding...
		HashSet<int> finalSelection = new HashSet<int>();
		for (int weldingIndex = 0; weldingIndex < weldings.Count; weldingIndex++) {

			// Merges the points
			_selection = new HashSet<int>(weldings[weldingIndex]);
			MergeSelectedPoints();
			finalSelection.UnionWith(_selection);

			// Modifies the index of the other weldings
			foreach (int pointIndex in weldings[weldingIndex]) {
				for (int nextWeldingIndex = weldingIndex + 1; nextWeldingIndex < weldings.Count; nextWeldingIndex++) {
					for (int i = 0; i < weldings[nextWeldingIndex].Count; i++) {
						if (weldings[nextWeldingIndex][i] >= pointIndex)
							weldings[nextWeldingIndex][i] -= 1;
					}
				}
			}
		}
		_selection = finalSelection;
	}

	private void CreatePoint(object position) {
		try {
			// Creates the point
			_shape2D.AddPoint(ScreenToPoint((Vector2)position));

			// Selects the point
			_selection.Clear();
			_selection.Add(_shape2D.points.Length - 1);
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid position: " + position + "\n" + e);
		}
	}

	private void ExtrudePoint(object position) {
		try {
			if (_selection.Count != 1) {
				Debug.LogWarning("WARNING: Can only extrude with a single point selected.");
				return;
			}

			// Creates the point
			_shape2D.AddPoint(ScreenToPoint((Vector2)position));

			// Creates the line
			float u = 0;
			foreach (int index in _selection) {
				_shape2D.CreateLine(index, _shape2D.points.Length - 1);
				_shape2D.RecalculateNormal(index);
				u = _shape2D.us[index];
			}
			_shape2D.RecalculateNormal(_shape2D.points.Length - 1);
			_shape2D.us[_shape2D.points.Length - 1] = u;

			// Selects the point
			_selection.Clear();
			_selection.Add(_shape2D.points.Length - 1);
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid position: " + position + "\n" + e);
		}
	}

	private void CopySelectedPoints() {
		CopySelection(out _clipboardPoints, out _clipboardNormals, out _clipboardUs, out _clipboardLines);
	}

	private void CopySelection(out Vector2[] points, out Vector2[] normals, out float[] us, out int[] lines) {
		if (HasSelection) {
			points = new Vector2[_selection.Count];
			normals = new Vector2[_selection.Count];
			us = new float[_selection.Count];
			int[] indices = new int[_selection.Count];
			int it = 0;
			foreach (int index in _selection) {
				points[it] = _shape2D.points[index];
				normals[it] = _shape2D.normals[index];
				us[it] = _shape2D.us[index];
				indices[it] = index;
				it++;
			}
			List<int> lineList = new List<int>();
			for (int line = 0; line < _shape2D.lines.Length; line += 2) {
				for (int i = 0; i < indices.Length; i++) {
					for (int j = i + 1; j < indices.Length; j++) {
						if (_shape2D.lines[line] == indices[i] && _shape2D.lines[line + 1] == indices[j]) {
							lineList.Add(i);
							lineList.Add(j);
						}
						if (_shape2D.lines[line] == indices[j] && _shape2D.lines[line + 1] == indices[i]) {
							lineList.Add(j);
							lineList.Add(i);
						}
					}
				}
			}
			lines = lineList.ToArray();
		}
		else {
			points = null;
			normals = null;
			us = null;
			lines = null;
		}
	}

	private void CutSelectedPoints() {
		CopySelectedPoints();
		DeleteSelectedPoints();
	}

	private void PasteCopiedPoints() {
		PastePoints(_clipboardPoints, _clipboardNormals, _clipboardUs, _clipboardLines, Vector2.zero);
	}

	private void PasteCopiedPoints(object offset) {
		try {
			Rect containing = new Rect();
			containing = containing.FromPoints(_clipboardPoints);
			Vector2 displacement = ScreenToPoint((Vector2)offset) - containing.center;
			PastePoints(_clipboardPoints, _clipboardNormals, _clipboardUs, _clipboardLines, displacement);
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid offset: " + offset + "\n" + e);
		}
	}

	private void PastePoints(Vector2[] points, Vector2[] normals, float[] us, int[] lines, Vector2 displacement) {
		_selection.Clear();
		int originalLength = _shape2D.points.Length;
		for (int i = 0; i < points.Length; i++) {
			// Copies the point
			_shape2D.AddPoint(displacement + points[i]);
			_selection.Add(_shape2D.points.Length - 1);

			// Copies the normal
			_shape2D.normals[_shape2D.normals.Length - 1] = normals[i];

			// Copies the u
			_shape2D.us[_shape2D.normals.Length - 1] = us[i];
		}

		// Copies the lines
		for (int line = 0; line < lines.Length; line += 2)
			_shape2D.CreateLine(originalLength + lines[line], originalLength + lines[line + 1]);
	}

	private void PastePointCoordinates(object pointIndex) {
		try {
			if (_clipboardPoints == null || _clipboardPoints.Length != 1) {
				Debug.LogWarning("WARNGING: Attempt to paste coordinates with invalid selection!");
				return;
			}
			int index = Convert.ToInt32(pointIndex);
			_shape2D.points[index] = _clipboardPoints[0];
			_shape2D.normals[index] = _clipboardNormals[0];
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void SymmetryHorizontal() {
		DuplicateSelectedPoints();
		MirrorSelectionHorizontal(true);
		ReverseSelectedLines();
	}

	private void SymmetryVertical() {
		DuplicateSelectedPoints();
		MirrorSelectionVertical(true);
		ReverseSelectedLines();
	}

	private void DuplicateSelectedPoints() {
		Vector2[] points;
		Vector2[] normals;
		float[] us;
		int[] lines;
		CopySelection(out points, out normals, out us, out lines);
		PastePoints(points, normals, us, lines, Vector2.zero);
	}

	private void DeletePoint(object pointIndex) {
		try {
			// Deletes the point
			int index = Convert.ToInt32(pointIndex);
			_shape2D.DeletePoint(index);

			// Removes the point from the selection and updates all the indices
			_selection.Remove(index);
			if (HasSelection) {
				int[] selectionCopy = new int[_selection.Count];
				_selection.CopyTo(selectionCopy);
				_selection.Clear();
				for (int i = 0; i < selectionCopy.Length; i++) {
					if (selectionCopy[i] > index)
						selectionCopy[i] -= 1;
					_selection.Add(selectionCopy[i]);
				}
			}
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void DeleteSelectedPoints() {
		int[] selectionCopy = new int[_selection.Count];
		_selection.CopyTo(selectionCopy);
		Array.Sort(selectionCopy);
		for (int i = selectionCopy.Length - 1; i >= 0; i--)
			DeletePoint(selectionCopy[i]);
	}

	private void RemovePoint(object pointIndex) {
		try {
			// Saves lines origins and destinations
			int index = Convert.ToInt32(pointIndex);
			List<int> lineOrigins = new List<int>();
			List<int> lineDestinations = new List<int>();
			for (int line = 0; line < _shape2D.lines.Length; line += 2) {
				if (_shape2D.lines[line] != index && _shape2D.lines[line + 1] == index)
					lineOrigins.Add(_shape2D.lines[line]);
				if (_shape2D.lines[line] == index && _shape2D.lines[line + 1] != index)
					lineDestinations.Add(_shape2D.lines[line + 1]);
			}

			// Creates lines between them
			for (int i = 0; i < lineOrigins.Count; i++)
				for (int j = 0; j < lineDestinations.Count; j++)
					_shape2D.CreateLine(lineOrigins[i], lineDestinations[j]);

			// Removes the point
			DeletePoint(pointIndex);
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void RemoveSelectedPoints() {
		int[] selectionCopy = new int[_selection.Count];
		_selection.CopyTo(selectionCopy);
		Array.Sort(selectionCopy);
		for (int i = selectionCopy.Length - 1; i >= 0; i--)
			RemovePoint(selectionCopy[i]);
	}

	private Rect GetSelectionRect(bool screen = false) {
		Rect rect = new Rect();
		Vector2[] selectedPoints;
		GetSelectedPoints(out selectedPoints, screen);
		rect = rect.FromPoints(selectedPoints);
		return rect;
	}

	private void FocusSelection() {
		// Calculates the containing rect
		Rect containingRect = GetSelectionRect();
		containingRect = containingRect.Expand(0.5f);

		// Calculates the zoom
		_scale =  Mathf.Min(_mainAreaRect.width / containingRect.width, _mainAreaRect.height / containingRect.height);

		// Calculates the offset
		_offset = containingRect.center * _scale;
		_offset.x *= -1;
	}

	private void RecalculateSelectedNormals() {
		_shape2D.RecalculateNormals(_selection);
	}

	private void RecalculateAllNormals() {
		_shape2D.RecalculateAllNormals();
	}

	private void RecalculateNormal(object normalIndex) {
		try {
			_shape2D.RecalculateNormal(Convert.ToInt32(normalIndex));
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid normal index: " + normalIndex + "\n" + e);
		}
	}

	private void InvertSelectedNormals() {
		foreach (int index in _selection)
			InvertNormal(index);
	}

	private void InvertNormal(object normalIndex) {
		try {
			_shape2D.normals[Convert.ToInt32(normalIndex)] *= -1;
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid normal index: " + normalIndex + "\n" + e);
		}
	}

	private void CenterSelection(object point) {
		try {
			Vector2 reference = ScreenToPoint((Vector2)point);
			Rect rect = GetSelectionRect();
			foreach (int index in _selection)
				_shape2D.points[index] += reference - rect.center;
			FocusSelection();
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point: " + point + "\n" + e);
		}
	}

	private void BreakSelectedPoints() {
		int[] selectionCopy = new int[_selection.Count];
		_selection.CopyTo(selectionCopy);
		_selection.Clear();
		Array.Sort(selectionCopy);
		for (int i = selectionCopy.Length - 1; i >= 0; i--)
			BreakPoint(selectionCopy[i]);
	}

	private void BreakPoint(object pointIndex) {
		try {
			int index = Convert.ToInt32(pointIndex);
			List<int> lineOrigins = new List<int>();
			List<int> lineDestinations = new List<int>();
			for (int line = 0; line < _shape2D.lines.Length; line += 2) {
				if (_shape2D.lines[line + 1] == index)
					lineOrigins.Add(_shape2D.lines[line]);
				if (_shape2D.lines[line] == index)
					lineDestinations.Add(_shape2D.lines[line + 1]);
			}

			if (lineOrigins.Count + lineDestinations.Count <= 1) {
				Debug.LogWarning("WARNING: The selected point doesn't have enough lines to break.");
				return;
			}
			
			Vector2 point = _shape2D.points[index];
			float u = _shape2D.us[index];
			_selection.Remove(index);
			DeletePoint(index);

			for (int i = 0; i < lineOrigins.Count; i++)
				if (lineOrigins[i] > index)
					lineOrigins[i] -= 1;
			for (int i = 0; i < lineDestinations.Count; i++)
				if (lineDestinations[i] > index)
					lineDestinations[i] -= 1;

			foreach (int origin in lineOrigins) {
				_shape2D.AddPoint(point);
				int newPointIndex = _shape2D.points.Length - 1;
				_shape2D.CreateLine(origin, newPointIndex);
				_shape2D.RecalculateNormal(newPointIndex);
				_shape2D.us[newPointIndex] = u;
				_selection.Add(newPointIndex);
			}
			foreach (int destination in lineDestinations) {
				_shape2D.AddPoint(point);
				int newPointIndex = _shape2D.points.Length - 1;
				_shape2D.CreateLine(newPointIndex, destination);
				_shape2D.RecalculateNormal(newPointIndex);
				_shape2D.us[newPointIndex] = u;
				_selection.Add(newPointIndex);
			}
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + pointIndex + "\n" + e);
		}
	}

	private void CreateLineBetweenSelectedPoints() {
		if (_selection.Count < 2) {
			Debug.LogWarning("WARNING: Attempted to create a line with an invalid number of points selected!");
			return;
		}
		int[] points = new int[_selection.Count];
		_selection.CopyTo(points);
		for (int i = 0; i < points.Length - 1; i++)
			if (_shape2D.AreConnected(points[i], points[i + 1]) == Shape2D.ConnectionType.None)
				_shape2D.CreateLine(points[i], points[i + 1]);
	}

	private void RemoveLinesBetweenSelectedPoints() {
		if (_selection.Count < 2) {
			Debug.LogWarning("WARNING: Attempted to remove a line with an invalid number of points selected!");
			return;
		}
		int[] points = new int[_selection.Count];
		_selection.CopyTo(points);
		for (int i = 0; i < points.Length; i++)
			for (int j = i + 1; j < points.Length; j++)
				_shape2D.RemoveLine(points[i], points[j]);
	}

	private void ReverseSelectedLines() {
		if (_selection.Count < 2) {
			Debug.LogWarning("WARNING: Attempted to reverse a line with an invalid number of points selected!");
			return;
		}
		for (int line = 0; line < _shape2D.lines.Length; line += 2) {
			if (_selection.Contains(_shape2D.lines[line]) && _selection.Contains(_shape2D.lines[line + 1])) {
				int temp = _shape2D.lines[line];
				_shape2D.lines[line] = _shape2D.lines[line + 1];
				_shape2D.lines[line + 1] = temp;
			}
		}
	}

	private void DivideSelectedLines() {
		if (_selection.Count < 2) {
			Debug.LogWarning("WARNING: Attempted to remove a line with an invalid number of points selected!");
			return;
		}
		int[] points = new int[_selection.Count];
		_selection.CopyTo(points);
		_selection.Clear();
		for (int i = 0; i < points.Length; i++) {
			for (int j = i + 1; j < points.Length; j++) {
				Shape2D.ConnectionType connection = _shape2D.AreConnected(points[i], points[j]);
				if (connection != Shape2D.ConnectionType.None) {
					_shape2D.RemoveLine(points[i], points[j]);
					_shape2D.AddPoint((_shape2D.points[points[i]] + _shape2D.points[points[j]]) / 2);
					int newPointIndex = _shape2D.points.Length - 1;
					_shape2D.normals[newPointIndex] = (_shape2D.normals[points[i]] + _shape2D.normals[points[j]]) / 2;
					_shape2D.us[_shape2D.us.Length - 1] = (_shape2D.us[points[i]] + _shape2D.us[points[j]]) / 2;

					if (connection == Shape2D.ConnectionType.Direct) {
						_shape2D.CreateLine(points[i], newPointIndex);
						_shape2D.CreateLine(newPointIndex, points[j]);
					}
					else if (connection == Shape2D.ConnectionType.Reverse) {
						_shape2D.CreateLine(points[j], newPointIndex);
						_shape2D.CreateLine(newPointIndex, points[i]);
					}

					_selection.Add(newPointIndex);
				}
			}
		}
	}

	private void MirrorSelectionHorizontal(object global) {
		try {
			bool isGlobal = Convert.ToBoolean(global);
			Rect rect = new Rect();
			if (!isGlobal) {
				rect = GetSelectionRect();
			}

			foreach (int index in _selection) {
				Vector2 offset = _shape2D.points[index] - rect.center;
				offset.x *= -1;
				_shape2D.points[index] = rect.center + offset;
				_shape2D.normals[index].x *= -1;
			}
		}
		catch (Exception e) {
			Debug.LogError("ERROR: The parameter is not a valid boolean: " + global + "\n" + e);
		}
	}

	private void MirrorSelectionVertical(object global) {
		try {
			bool isGlobal = Convert.ToBoolean(global);
			Rect rect = new Rect();
			if (!isGlobal) {
				rect = GetSelectionRect();
			}

			foreach (int index in _selection) {
				Vector2 offset = _shape2D.points[index] - rect.center;
				offset.y *= -1;
				_shape2D.points[index] = rect.center + offset;
				_shape2D.normals[index].y *= -1;
			}
		}
		catch (Exception e) {
			Debug.LogError("ERROR: The parameter is not a valid boolean: " + global + "\n" + e);
		}
	}

	private void SortAllPoints(object startingIndex) {
		try {
			int initialIndex = Convert.ToInt32(startingIndex);

			List<int> list = new List<int>();

			HashSet<int> remainingPoints = new HashSet<int>();
			for (int i = 0; i < _shape2D.points.Length; i++)
				if (i != initialIndex)
					remainingPoints.Add(i);

			Stack<int> pointsToCheck = new Stack<int>();
			pointsToCheck.Push(initialIndex);


			while (remainingPoints.Count > 0 || pointsToCheck.Count > 0) {
				while (pointsToCheck.Count > 0) {
					int currentPoint = pointsToCheck.Pop();
					list.Add(currentPoint);
					List<int> connectedPoints = GetConnectedPoints(currentPoint);
					for (int i = connectedPoints.Count - 1; i >= 0; i--) {
						if (remainingPoints.Contains(connectedPoints[i])) {
							remainingPoints.Remove(connectedPoints[i]);
							pointsToCheck.Push(connectedPoints[i]);
						}
					}
				}

				int closest = -1;
				float closestDistance = float.MaxValue;
				foreach (int index in remainingPoints) {
					float distance = Vector3.Distance(_shape2D.points[index], _shape2D.points[list[list.Count - 1]]);
					if (distance < closestDistance) {
						closestDistance = distance;
						closest = index;
					}
				}
				if (closest != -1) {
					remainingPoints.Remove(closest);
					pointsToCheck.Push(closest);
				}
			}

			Vector2[] newPoints = new Vector2[_shape2D.points.Length];
			Vector2[] newNormals = new Vector2[_shape2D.normals.Length];
			float[] newUs = new float[_shape2D.us.Length];
			List<int> lines = new List<int>();
			for (int i = 0; i < list.Count; i++) {
				newPoints[i] = _shape2D.points[list[i]];
				newNormals[i] = _shape2D.normals[list[i]];
				newUs[i] = _shape2D.us[list[i]];
				List<int> connected = GetConnectedPoints(list[i]);
				for (int j = 0; j < connected.Count; j++) {
					lines.Add(list.IndexOf(connected[j]));
					lines.Add(i);
				}
			}
			_shape2D.points = newPoints;
			_shape2D.normals = newNormals;
			_shape2D.us = newUs;
			_shape2D.lines = lines.ToArray();

			_selection.Clear();
		}
		catch (Exception e) {
			Debug.LogError("ERROR: Invalid point index: " + startingIndex + "\n" + e);
		}
	}

	private List<int> GetConnectedPoints(int index) {
		List<int> connectedPoints = new List<int>();
		for (int line = 0; line < _shape2D.lines.Length; line += 2)
			if (_shape2D.lines[line + 1] == index)
				connectedPoints.Add(_shape2D.lines[line]);
		return connectedPoints;
	}

	private void HandleSelectionEvents() {
		if (!HasSelection)
			return;

		Event current = Event.current;
		int dragID = GUIUtility.GetControlID("SelectionDrag".GetHashCode(), FocusType.Passive);
		Rect selectionArea = GetSelectionRect(true);
		selectionArea = selectionArea.Expand(_selectionAreaExpansionDistance);
		EditorGUIUtility.AddCursorRect(selectionArea, MouseCursor.Link);
		Rect selectedFieldsArea = new Rect();
		if (HasSelection)
			selectedFieldsArea = new Rect(CurrentArea.width - _selectedPanelWidth, CurrentArea.height - _selectedPanelHeight, _selectedPanelWidth, _selectedPanelHeight);
		switch (current.GetTypeForControl(dragID)) {
			case EventType.MouseDown:
				if (selectionArea.Contains(current.mousePosition) && !selectedFieldsArea.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = dragID;
					current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == dragID && current.button == 0) {
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == dragID) {
					Vector2 delta = current.delta;
					delta.y *= -1;
					foreach (int index in _selection)
						_shape2D.points[index] += delta / _scale;
					current.Use();
				}
				break;
		}
	}

	private void HandleMouseEvents(Rect area) {
		// Drag Events
		Event current = Event.current;
		int dragID = GUIUtility.GetControlID("Drag".GetHashCode(), FocusType.Passive);
		if (GUIUtility.hotControl == dragID)
			EditorGUIUtility.AddCursorRect(area, MouseCursor.Pan);
		Rect selectedFieldsArea = new Rect();
		if (HasSelection)
			selectedFieldsArea = new Rect(area.width - _selectedPanelWidth, area.height - _selectedPanelHeight, _selectedPanelWidth, _selectedPanelHeight);
		Rect optionsPanelArea = new Rect(0, 0, area.width, _optionsPanelHeight);
		switch (current.GetTypeForControl(dragID)) {
			case EventType.ContextClick:
				if (area.Contains(current.mousePosition) && !selectedFieldsArea.Contains(current.mousePosition) && !optionsPanelArea.Contains(current.mousePosition)) {
					GenericMenu menu = new GenericMenu();
					if (_selection.Count == 1)
						menu.AddItem(new GUIContent("Extrude point"), false, ExtrudePoint, current.mousePosition);
					else
						menu.AddDisabledItem(new GUIContent("Extrude point"));
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Create point"), false, CreatePoint, current.mousePosition);
					if (_selection.Count >= 1) {
						menu.AddItem(new GUIContent("Delete selected points"), false, DeleteSelectedPoints);
						menu.AddItem(new GUIContent("Remove selected points"), false, RemoveSelectedPoints);
					}
					menu.AddSeparator("");
					if (_selection.Count >= 2) {
						menu.AddItem(new GUIContent("Create line between selected points"), false, CreateLineBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Remove lines between selected points"), false, RemoveLinesBetweenSelectedPoints);
						menu.AddItem(new GUIContent("Reverse lines between selected points"), false, ReverseSelectedLines);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Divide selected lines"), false, DivideSelectedLines);
						menu.AddSeparator("");
					}
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Recalculate selected normals"), false, RecalculateSelectedNormals);
						menu.AddItem(new GUIContent("Invert selected normals"), false, InvertSelectedNormals);
						menu.AddSeparator("");
					}
					if (_selection.Count >= 1)
						menu.AddItem(new GUIContent("Break selected points"), false, BreakSelectedPoints);
					if (_selection.Count > 1) {
						menu.AddItem(new GUIContent("Shrink selected points"), false, ShrinkSelectedPoints);
						menu.AddItem(new GUIContent("Merge selected points"), false, MergeSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Weld selected points"), false, WeldSelectedPoints);
						menu.AddSeparator("");
						menu.AddItem(new GUIContent("Copy selected points"), false, CopySelectedPoints);
						menu.AddItem(new GUIContent("Cut selected points"), false, CutSelectedPoints);
						menu.AddSeparator("");
					}
					if (HasClipboard) {
						menu.AddItem(new GUIContent("Paste copied points"), false, PasteCopiedPoints);
						menu.AddItem(new GUIContent("Paste copied points here"), false, PasteCopiedPoints, current.mousePosition);
					}
					else {
						menu.AddDisabledItem(new GUIContent("Paste copied points"));
						menu.AddDisabledItem(new GUIContent("Paste copied points here"));
					}
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("Sort all points"), false, SortAllPoints, 0);
					menu.ShowAsContext();
					current.Use();
				}
				break;
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && !selectedFieldsArea.Contains(current.mousePosition) && !optionsPanelArea.Contains(current.mousePosition) && current.button == 2) {
					GUIUtility.hotControl = dragID;
					current.Use();
					EditorGUIUtility.SetWantsMouseJumping(1);
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == dragID && current.button == 2) {
					GUIUtility.hotControl = 0;
					EditorGUIUtility.SetWantsMouseJumping(0);
					current.Use();
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == dragID) {
					_offset += current.delta;
					current.Use();
				}
				break;
			case EventType.ScrollWheel:
				_scale = Mathf.Clamp(_oldScale - current.delta.y * _oldScale / 60, 1, 600);
				_offset *= _scale / _oldScale;
				break;
		}

		// Select Events
		int selectID = GUIUtility.GetControlID("Select".GetHashCode(), FocusType.Passive);
		if (GUIUtility.hotControl == selectID) {
			// Draw selection rectangle
			Rect rect = new Rect();
			rect = rect.FromPoints(_selectionStart, current.mousePosition);
			Color faceColor = new Color(0, 0, 1, 0.1f);
			Color outlineColor = new Color(0, 0, 1, 0.25f);
			Handles.color = Color.white;
			Handles.DrawSolidRectangleWithOutline(rect, faceColor, outlineColor);
		}
		switch (current.GetTypeForControl(selectID)) {
			case EventType.MouseDown:
				if (area.Contains(current.mousePosition) && !selectedFieldsArea.Contains(current.mousePosition) && !optionsPanelArea.Contains(current.mousePosition) && current.button == 0) {
					GUIUtility.hotControl = selectID;
					current.Use();
					_selectionStart = current.mousePosition;
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == selectID && current.button == 0) {
					Rect rect = new Rect();
					rect = rect.FromPoints(_selectionStart, current.mousePosition);
					if (!current.alt && !current.control)
						_selection.Clear();
					for (int i = 0; i < _shape2D.points.Length; i++) {
						if (rect.Contains(PointToScreen(_shape2D.points[i]))) {
							if (current.alt)
								_selection.Remove(i);
							else
								_selection.Add(i);
						}
					}
					GUIUtility.hotControl = 0;
					current.Use();
				}
				break;
		}
	}

	private void HandleKeyboardEvents() {
		// Drag Events
		Event current = Event.current;
		int keyID = GUIUtility.GetControlID("Key".GetHashCode(), FocusType.Passive);
		switch (current.GetTypeForControl(keyID)) {
			case EventType.KeyDown:
				if (current.isKey && current.keyCode == KeyCode.Delete && HasSelection) {
					DeleteSelectedPoints();
					current.Use();
				}
				if (current.isKey && current.shift && current.keyCode == KeyCode.Delete && HasSelection) {
					RemoveSelectedPoints();
					current.Use();
				}
				else if (current.isKey && current.keyCode == KeyCode.Escape && HasSelection) {
					_selection.Clear();
					current.Use();
				}
				else if (current.isKey && current.keyCode == KeyCode.F && HasSelection) {
					FocusSelection();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.X && HasSelection) {
					CutSelectedPoints();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.C && HasSelection) {
					CopySelectedPoints();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.V && HasClipboard) {
					PasteCopiedPoints();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.O && HasSelection) {
					CenterSelection(PointToScreen(Vector2.zero));
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.S && HasSelection) {
					CenterSelection(GetScreenCenter());
				}
				else if (current.isKey && current.keyCode == KeyCode.K && HasClipboard) {
					ShrinkSelectedPoints();
				}
				else if (current.isKey && current.keyCode == KeyCode.M && HasSelection) {
					MergeSelectedPoints();
				}
				else if (current.isKey && current.keyCode == KeyCode.W && HasSelection) {
					WeldSelectedPoints();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.W && HasSelection) {
					WeldAllPoints();
				}
				else if (current.isKey && current.keyCode == KeyCode.B && HasSelection) {
					BreakSelectedPoints();
				}
				else if (current.isKey && current.keyCode == KeyCode.L && HasSelection) {
					CreateLineBetweenSelectedPoints();
				}
				else if (current.isKey && current.alt && current.keyCode == KeyCode.L && HasSelection) {
					RemoveLinesBetweenSelectedPoints();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.L && HasSelection) {
					ReverseSelectedLines();
				}
				else if (current.isKey && current.keyCode == KeyCode.D && HasSelection) {
					DivideSelectedLines();
				}
				else if (current.isKey && current.shift && current.keyCode == KeyCode.N) {
					RecalculateAllNormals();
				}
				else if (current.isKey && current.keyCode == KeyCode.N && HasSelection) {
					RecalculateSelectedNormals();
				}
				else if (current.isKey && current.keyCode == KeyCode.I && HasSelection) {
					InvertSelectedNormals();
				}
				break;
		}
	}

	private void DrawOptionsPanel() {
		BeginArea(new Rect(0, 0, CurrentArea.width, _optionsPanelHeight), GUI.skin.box);
		EditorGUILayout.BeginHorizontal();
		float labelWidth = EditorGUIUtility.labelWidth;
		
		GUIContent autoNormalsLabel = new GUIContent("Normals Auto");
		EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(autoNormalsLabel).x;
		_autoRecalculateNormals = EditorGUILayout.Toggle(autoNormalsLabel, _autoRecalculateNormals);

		GUIContent weldDistanceLabel = new GUIContent("Weld Distance");
		EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(weldDistanceLabel).x;
		_weldDistance = EditorGUILayout.FloatField(weldDistanceLabel, _weldDistance);
		
		if (GUILayout.Button("Weld Selected Points"))
			WeldSelectedPoints();

		if (GUILayout.Button("Weld All Points"))
			WeldAllPoints();

		EditorGUIUtility.labelWidth = labelWidth;
		EditorGUILayout.EndHorizontal();
		EndArea();
	}

	private void DrawSelected() {
		// Doesn't draw anything if no element is selected
		if (!HasSelection)
			return;

		// Draws the panel
		BeginArea(new Rect(CurrentArea.width - _selectedPanelWidth, CurrentArea.height - _selectedPanelHeight, _selectedPanelWidth, _selectedPanelHeight), GUI.skin.box);
		EditorGUILayout.BeginVertical();

		// Draws the point field
		Vector2[] selectedPoints;
		int[] pointIndices = GetSelectedPoints(out selectedPoints);
		DrawMultiVector2Field("Point", ref selectedPoints);
		for (int i = 0; i < selectedPoints.Length; i++)
			_shape2D.points[pointIndices[i]] = selectedPoints[i];
		
		// Draws the normal field
		Vector2[] selectedNormals;
		int[] normalIndices = GetSelectedNormals(out selectedNormals);
		float[] normalAngles = new float[selectedNormals.Length];
		for (int i = 0; i < normalAngles.Length; i++) {
			normalAngles[i] = Mathf.Acos(selectedNormals[i].x) * Mathf.Rad2Deg;
			if (selectedNormals[i].y < 0)
				normalAngles[i] *= -1;
		}
		DrawMultiFloatField("Normal", ref normalAngles, "Angle");
		for (int i = 0; i < selectedNormals.Length; i++)
			_shape2D.normals[normalIndices[i]] = new Vector2(Mathf.Cos(normalAngles[i] * Mathf.Deg2Rad), Mathf.Sin(normalAngles[i] * Mathf.Deg2Rad));

		// Draws the U field
		float[] selectedUs;
		int[] usIndices = GetSelectedUs(out selectedUs);
		DrawMultiSliderField("U", 0, 1, ref selectedUs);
		for (int i = 0; i < selectedUs.Length; i++)
			_shape2D.us[usIndices[i]] = selectedUs[i];

		EditorGUILayout.EndVertical();
		EndArea();
	}

	private void DrawMultiVector2Field(string label, ref Vector2[] collection) {
		// Checks which values are not the same
		Vector2 avg = Vector2.zero;
		for (int i = 0; i < collection.Length; i++)
			avg += collection[i];
		avg /= collection.Length;

		// Draws the field
		EditorGUILayout.LabelField(label);
		float labelWidth = EditorGUIUtility.labelWidth;
		float fieldWidth = EditorGUIUtility.fieldWidth;
		EditorGUIUtility.labelWidth = 15;
		EditorGUIUtility.fieldWidth = 25;

		Vector2 inputPosition = EditorGUILayout.Vector2Field("", avg);
		Vector2 displacement = inputPosition - avg;
		for (int i = 0; i < collection.Length; i++)
			collection[i] += displacement;

		EditorGUIUtility.labelWidth = labelWidth;
		EditorGUIUtility.fieldWidth = fieldWidth;
	}

	private void DrawMultiFloatField(string label, ref float[] collection, string fieldLabel = "") {
		// Checks which values are not the same
		float avg = 0;
		for (int i = 0; i < collection.Length; i++)
			avg += collection[i];
		avg /= collection.Length;

		// Draws the field
		EditorGUILayout.LabelField(label);
		float labelWidth = EditorGUIUtility.labelWidth;
		float fieldWidth = EditorGUIUtility.fieldWidth;
		EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent(fieldLabel)).x;
		EditorGUIUtility.fieldWidth = 40;

		float inputU = EditorGUILayout.FloatField(fieldLabel, avg);
		float displacement = inputU - avg;
		for (int i = 0; i < collection.Length; i++)
			collection[i] += displacement;

		EditorGUIUtility.labelWidth = labelWidth;
		EditorGUIUtility.fieldWidth = fieldWidth;
	}

	private void DrawMultiSliderField(string label, float min, float max, ref float[] collection) {
		// Checks which values are not the same
		float avg = 0;
		for (int i = 0; i < collection.Length; i++)
			avg += collection[i];
		avg /= collection.Length;

		// Draws the field
		EditorGUILayout.LabelField(label);
		float fieldWidth = EditorGUIUtility.fieldWidth;
		EditorGUIUtility.fieldWidth = 40;
		
		float inputU = EditorGUILayout.Slider(avg, min, max);
		float displacement = inputU - avg;
		for (int i = 0; i < collection.Length; i++) {
			collection[i] += displacement;
			collection[i] = Mathf.Clamp(collection[i], min, max);
		}
		
		EditorGUIUtility.fieldWidth = fieldWidth;
	}

	private int[] GetSelectedPoints(out Vector2[] selectedPoints, bool screen = false) {
		selectedPoints = new Vector2[_selection.Count];
		int[] indices = new int[_selection.Count];
		int i = 0;
		foreach (int index in _selection) {
			indices[i] = index;
			if (screen)
				selectedPoints[i] = PointToScreen(_shape2D.points[index]);
			else
				selectedPoints[i] = _shape2D.points[index];
			i++;
		}
		return indices;
	}

	private int[] GetSelectedNormals(out Vector2[] selectedNormals) {
		selectedNormals = new Vector2[_selection.Count];
		int[] indices = new int[_selection.Count];
		int i = 0;
		foreach (int index in _selection) {
			indices[i] = index;
			selectedNormals[i] = _shape2D.normals[index];
			i++;
		}
		return indices;
	}

	private int[] GetSelectedUs(out float[] selectedUs) {
		selectedUs = new float[_selection.Count];
		int[] indices = new int[_selection.Count];
		int i = 0;
		foreach (int index in _selection) {
			indices[i] = index;
			selectedUs[i] = _shape2D.us[index];
			i++;
		}
		return indices;
	}

	private Vector2 PointToScreen(Vector2 point) {
		point *= _oldScale;
		point.y *= -1;
		point += _mainAreaRect.size / 2 + _oldOffset;
		return point;
	}

	private Vector2 ScreenToPoint(Vector2 point) {
		point -= _mainAreaRect.size / 2 + _oldOffset;
		point.y *= -1;
		point /= _oldScale;
		return point;
	}

	private Vector2 NormalToHandle(Vector2 normal, Vector2 associatedPoint) {
		Vector2 handle = normal * _normalLength * _fixedScale;
		handle.y *= -1;
		handle += PointToScreen(associatedPoint);
		return handle;
	}

	private Vector2 HandleToNormal(Vector2 handle, Vector2 associatedPoint) {
		Vector2 normal = handle;
		normal -= PointToScreen(associatedPoint);
		normal.y *= -1;
		return normal.normalized;
	}

	private Vector2 AveragePoint(IEnumerable<int> indices) {
		Vector2 avg = Vector2.zero;
		int count = 0;
		foreach (int index in indices) {
			avg += _shape2D.points[index];
			count++;
		}
		if (count != 0)
			avg /= count;
		return avg;
	}

	private Vector2 AverageNormal(IEnumerable<int> indices) {
		Vector2 avg = Vector2.zero;
		int count = 0;
		foreach (int index in indices) {
			avg += _shape2D.normals[index];
			count++;
		}
		if (count != 0)
			avg /= count;
		return avg;
	}

	private float AverageU(IEnumerable<int> indices) {
		float avg = 0;
		int count = 0;
		foreach (int index in indices) {
			avg += _shape2D.us[index];
			count++;
		}
		if (count != 0)
			avg /= count;
		return avg;
	}
}
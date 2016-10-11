﻿using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu()]
public class Shape2D : ScriptableObject {

	public Vector2[] points;
	public Vector2[] normals;
	public float[] us;
	public int[] lines;

	public void AddPoint(params Vector2[] positions) {
		Vector2[] newPoints = new Vector2[points.Length + positions.Length];
		Array.Copy(points, newPoints, points.Length);
		Array.Copy(positions, 0, newPoints, points.Length, positions.Length);
		points = newPoints;

		Vector2[] newNormals = new Vector2[normals.Length + positions.Length];
		Array.Copy(normals, newNormals, normals.Length);
		for (int i = normals.Length; i < newNormals.Length; i++)
			newNormals[i] = Vector2.up;
		normals = newNormals;

		float[] newUs = new float[us.Length + positions.Length];
		Array.Copy(us, newUs, us.Length);
		us = newUs;
	}

	public void DeletePoint(int pointIndex) {
		List<Vector2> newPoints = new List<Vector2>();
		for (int i = 0; i < points.Length; i++)
			if (i != pointIndex)
				newPoints.Add(points[i]);
		points = newPoints.ToArray();

		List<Vector2> newNormals = new List<Vector2>();
		for (int i = 0; i < normals.Length; i++)
			if (i != pointIndex)
				newNormals.Add(normals[i]);
		normals = newNormals.ToArray();

		List<int> newLines = new List<int>();
		for (int i = 0; i < lines.Length; i += 2) {
			if (lines[i] != pointIndex && lines[i + 1] != pointIndex) {
				if (lines[i] >= pointIndex)
					lines[i] -= 1;
				if (lines[i + 1] >= pointIndex)
					lines[i + 1] -= 1;
				newLines.Add(lines[i]);
				newLines.Add(lines[i + 1]);
			}
		}
		lines = newLines.ToArray();

		List<float> newUs = new List<float>();
		for (int i = 0; i < us.Length; i++)
			if (i != pointIndex)
				newUs.Add(us[i]);
		us = newUs.ToArray();
	}

	public void CreateLine(int startIndex, int endIndex) {
		int[] newLines = new int[lines.Length + 2];
		Array.Copy(lines, newLines, lines.Length);
		newLines[lines.Length] = startIndex;
		newLines[lines.Length + 1] = endIndex;
		lines = newLines;
	}

	public void RemoveLine(int pointA, int pointB) {
		List<int> newLines = new List<int>();
		for (int line = 0; line < lines.Length; line += 2) {
			if ((lines[line] == pointA && lines[line + 1] == pointB) ||
				(lines[line] == pointB && lines[line + 1] == pointA))
				continue;
			else {
				newLines.Add(lines[line]);
				newLines.Add(lines[line + 1]);
			}
		}
		lines = newLines.ToArray();
	}

	public bool AreConnected(int pointA, int pointB) {
		for (int line = 0; line < lines.Length; line += 2) {
			if (lines[line] == pointA && lines[line + 1] == pointB)
				return true;
			if (lines[line] == pointB && lines[line + 1] == pointA)
				return true;
		}
		return false;
	}

	public void RecalculateNormals(IEnumerable<int> normalIndices) {
		foreach (int index in normalIndices)
			RecalculateNormal(index);
	}

	public void RecalculateAllNormals() {
		for (int i = 0; i < normals.Length; i++)
			RecalculateNormal(i);
	}

	public void RecalculateNormal(int normalIndex) {
		List<Vector2> lineDirections = new List<Vector2>();
		for (int line = 0; line < lines.Length; line += 2) {
			if (lines[line] == normalIndex)
				lineDirections.Add(points[lines[line]] - points[lines[line + 1]]);
			else if ((lines[line + 1] == normalIndex))
				lineDirections.Add(points[lines[line + 1]] - points[lines[line]]);
		}

		if (lineDirections.Count == 0)
			normals[normalIndex] = Vector2.up;
		else if (lineDirections.Count == 1)
			normals[normalIndex] = Quaternion.Euler(0, 0, -90) * lineDirections[0];
		else {
			Vector2 normalSum = Vector2.zero;
			foreach (Vector2 lineDireciton in lineDirections)
				normalSum += lineDireciton.normalized;
			normals[normalIndex] = normalSum.normalized;
		}
	}
}

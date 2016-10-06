using UnityEngine;
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
}

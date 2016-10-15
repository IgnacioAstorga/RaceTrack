using UnityEngine;

public static class RaycastHitExtension {

	public static Vector3 SmoothedNormal(this RaycastHit aHit) {
		var MC = aHit.collider as MeshCollider;
		if (MC == null)
			return aHit.normal;
		var M = MC.sharedMesh;
		var normals = M.normals;
		var indices = M.triangles;
		var N0 = normals[indices[aHit.triangleIndex * 3 + 0]];
		var N1 = normals[indices[aHit.triangleIndex * 3 + 1]];
		var N2 = normals[indices[aHit.triangleIndex * 3 + 2]];
		var B = aHit.barycentricCoordinate;
		var localNormal = (B[0] * N0 + B[1] * N1 + B[2] * N2).normalized;
		return MC.transform.TransformDirection(localNormal);
	}
}
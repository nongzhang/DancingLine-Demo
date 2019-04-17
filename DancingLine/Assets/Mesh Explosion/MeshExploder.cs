using System.Collections.Generic;

using UnityEngine;

[AddComponentMenu("Mesh/Mesh Exploder")]
public class MeshExploder : MonoBehaviour {
	
	public enum ExplosionType { Visual, Physics };
	
	public ExplosionType type = ExplosionType.Visual;
	public float minSpeed = 1;
	public float maxSpeed = 5;
	public float minRotationSpeed = 90;
	public float maxRotationSpeed = 360;
	public float fadeWaitTime = 0.5f;
	public float fadeTime = 2;
	public bool useGravity = false;
	public float colliderThickness = 0.125f;
	public bool useNormals = false;
	public bool useMeshBoundsCenter = false;
	public bool allowShadows = false;
	public bool shadersAlreadyHandleTransparency = false;
	
	public struct MeshExplosionPreparation {
		public Mesh startMesh;
		public Vector3[] triangleNormals;
		public Vector3[] triangleCentroids;
		public int totalFrontTriangles;
		
		public Mesh[] physicsMeshes;
		public Quaternion[] rotations;
		public int[] frontMeshTrianglesPerSubMesh;
	}
	
	MeshExplosionPreparation preparation;
	
	static Dictionary<Mesh, MeshExplosionPreparation> cache =
		new Dictionary<Mesh, MeshExplosionPreparation>();
	
	string ComponentName { get { return this.GetType().Name; } }
	
	void Start() {
		var meshFilter = GetComponent<MeshFilter>();
		if (meshFilter == null) {
#if UNITY_4_0
			var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer != null) {
				// In this case there isn't anything we can do ahead of time to prepare since we
				// need to bake the mesh at the point of explosion.
				return;
			}
			
			Debug.LogError(ComponentName +
				" must be on a GameObject with a MeshFilter or SkinnedMeshRenderer component.");
			return;
#else		
			Debug.LogError(ComponentName + " must be on a GameObject with a MeshFilter component.");
			return;
#endif
		}
		
		var oldMesh = meshFilter.sharedMesh;
		if (oldMesh == null) {
			Debug.LogError("The MeshFilter does not reference a mesh.");
			return;
		}
		
		Prepare(oldMesh);
	}
	
	void PrepareWithoutCaching(Mesh oldMesh) {
		Prepare(oldMesh, false);
	}
	
	void Prepare(Mesh oldMesh, bool cachePreparation = true) {
#if UNITY_4_0
		if (!oldMesh.isReadable) {
			Debug.LogError("The mesh is not readable. Switch on the \"Read/Write Enabled\"" +
				" option on the mesh's import settings.");
			return;
		}
#endif
		
		var usePhysics = type == ExplosionType.Physics;
		
		MeshExplosionPreparation prep;
		
		if (cache.TryGetValue(oldMesh, out prep)) {
			// The caching is different for physics explosions and non-physics explosions, so make
			// sure that we have the correct stuff:
			if ((usePhysics && prep.physicsMeshes != null) ||
				(!usePhysics && prep.startMesh != null)) {
				
				preparation = prep;
				return;
			}
		}
		
		// Make a copy of the mesh but with every triangle made discrete so that it doesn't share
		// any vertices with any other triangle.
		
		var oldVertices = oldMesh.vertices;
		var oldNormals = oldMesh.normals;
		var oldTangents = oldMesh.tangents;
		var oldUV = oldMesh.uv;
		var oldUV2 = oldMesh.uv2;
		var oldColors = oldMesh.colors;
		
		var subMeshCount = oldMesh.subMeshCount;
		var oldMeshTriangles = new int[subMeshCount][];
		var frontMeshTrianglesPerSubMesh = usePhysics ?
			prep.frontMeshTrianglesPerSubMesh = new int[subMeshCount] : null;
		int frontTriangles = 0;
		
		for (var subMesh = 0; subMesh < subMeshCount; ++subMesh) {
			int[] triangles;
			oldMeshTriangles[subMesh] = triangles = oldMesh.GetTriangles(subMesh);
			var frontMeshTrianglesInThisSubMesh = triangles.Length / 3;
			if (usePhysics) frontMeshTrianglesPerSubMesh[subMesh] = frontMeshTrianglesInThisSubMesh;
			frontTriangles += frontMeshTrianglesInThisSubMesh;
		}
		
		prep.totalFrontTriangles = frontTriangles;
		var totalTriangles = frontTriangles * 2; // back faces
		var newTotalVertices = usePhysics ?
			3 * 2 : // one triangle, both sides
			totalTriangles * 3;
		
		const int vertexLimit = 65534;
		if (newTotalVertices > vertexLimit) {
			Debug.LogError("The mesh has too many triangles to explode. It must have" +
				" " + ((vertexLimit / 3) / 2) + " or fewer triangles.");
			return;
		}
		
		var newVertices = new Vector3[newTotalVertices];
		var newNormals = oldNormals == null || oldNormals.Length == 0 ?
			null : new Vector3[newTotalVertices];
		var newTangents = oldTangents == null || oldTangents.Length == 0 ?
			null : new Vector4[newTotalVertices];
		var newUV = oldUV == null || oldUV.Length == 0 ?
			null : new Vector2[newTotalVertices];
		var newUV2 = oldUV2 == null || oldUV2.Length == 0 ?
			null : new Vector2[newTotalVertices];
		var newColors = oldColors == null || oldColors.Length == 0 ?
			null : new Color[newTotalVertices];
		
		var triangleCentroids = prep.triangleCentroids = new Vector3[frontTriangles];
		
		var physicsMeshes = prep.physicsMeshes = usePhysics ? new Mesh[frontTriangles] : null;
		var rotations = prep.rotations = usePhysics ? new Quaternion[frontTriangles] : null;
		
		var physicsMeshTriangles = usePhysics ? new int[] { 0, 1, 2, 3, 4, 5 } : null;
		
		var newVertexNumber = 0;
		var wholeMeshTriangleIndex = 0;
		
		var invRotation = Quaternion.identity;
		
		for (var subMesh = 0; subMesh < subMeshCount; ++subMesh) {
			var triangles = oldMeshTriangles[subMesh];
			var n = triangles.Length;
			
			int i = 0;
			
			while (i < n) {
				var triangleStartI = i;
				var centroid = Vector3.zero;
				
				for (int repeat = 0; repeat < 2; ++repeat) {
					i = triangleStartI;
					var back = repeat == 1;
					
					while (i < n) {
						var oldVertexNumber = triangles[back ?
							(triangleStartI + (3 - 1 - (i - triangleStartI))) : i];
						
						if (usePhysics && (newVertexNumber % 6) == 0) { // Start of a triangle
							var a = oldVertices[oldVertexNumber];
							var b = oldVertices[triangles[i + 1]];
							var c = oldVertices[triangles[i + 2]];
							
							var triangleRealNormal = Vector3.Cross(b - a, c - a);
							// We want to rotate the triangle so that it is flat on the x-z plane.
							// The reason for that is so that we can use an axis-aligned box
							// collider to be its physics proxy.
							rotations[wholeMeshTriangleIndex] =
								Quaternion.FromToRotation(Vector3.up, triangleRealNormal);
							invRotation =
								Quaternion.FromToRotation(triangleRealNormal, Vector3.up);
							triangleCentroids[wholeMeshTriangleIndex] = centroid =
								(a + b + c) / 3;
						}
						
						if (!back) {
							newVertices[newVertexNumber] = invRotation *
								(oldVertices[oldVertexNumber] - centroid);
							
							if (newNormals != null) {
								newNormals[newVertexNumber] = invRotation *
									oldNormals[oldVertexNumber];
							}
							if (newTangents != null) {
								newTangents[newVertexNumber] = invRotation *
									oldTangents[oldVertexNumber];
							}
						} else {
							// This stuff is handled by MeshExplosion.SetBackTriangleVertices().
						}
						
						if (newUV != null) {
							newUV[newVertexNumber] = oldUV[oldVertexNumber];
						}
						if (newUV2 != null) {
							newUV2[newVertexNumber] = oldUV2[oldVertexNumber];
						}
						if (newColors != null) {
							newColors[newVertexNumber] = oldColors[oldVertexNumber];
						}
						
						// It's important that these are here rather than in a for statement so
						// that they get executed even if we break.
						++i;
						++newVertexNumber;
						
						if ((newVertexNumber % 6) == 0) { // End of a triangle
							if (usePhysics) {
								MeshExplosion.SetBackTriangleVertices(
									newVertices, newNormals, newTangents, 1);
								
								var mesh = new Mesh();
								
								mesh.vertices = newVertices;
								if (newNormals != null) mesh.normals = newNormals;
								if (newTangents != null) {
									mesh.tangents = newTangents;
								}
								if (newUV != null) mesh.uv = newUV;
								if (newUV2 != null) mesh.uv2 = newUV2;
								if (newColors != null) mesh.colors = newColors;
								mesh.triangles = physicsMeshTriangles;
								
								physicsMeshes[wholeMeshTriangleIndex] = mesh;
								
								newVertexNumber = 0;
							}
							break;
						} else if ((newVertexNumber % 3) == 0 && !back) {
							break;
						}
					}
				}
				
				++wholeMeshTriangleIndex;
			}
		}
		
		var newMeshCenter = Vector3.zero;
		if (!usePhysics) {
			var newMesh = prep.startMesh = new Mesh();
#if UNITY_4_0
			newMesh.MarkDynamic();
#endif
			newMesh.vertices = newVertices;
			if (newNormals != null) newMesh.normals = newNormals;
			if (newTangents != null) newMesh.tangents = newTangents;
			if (newUV != null) newMesh.uv = newUV;
			if (newUV2 != null) newMesh.uv2 = newUV2;
			if (newColors != null) newMesh.colors = newColors;
			
			newMesh.subMeshCount = subMeshCount;
			newVertexNumber = 0;
			for (var subMesh = 0; subMesh < subMeshCount; ++subMesh) {
				var n = oldMeshTriangles[subMesh].Length * 2;
				var newTriangles = new int[n];
				for (var i = 0; i < n; ++i, ++newVertexNumber) {
					newTriangles[i] = newVertexNumber;
				}
				newMesh.SetTriangles(newTriangles, subMesh);
			}
			
			if (useMeshBoundsCenter) newMeshCenter = newMesh.bounds.center;
		}
		
		var triangleNormals = prep.triangleNormals = new Vector3[frontTriangles];
		
		var firstVertexIndex = 0;
		for (var triangleNumber = 0; triangleNumber < frontTriangles;
			++triangleNumber, firstVertexIndex += 6) {
			
			Vector3 centroid;
			if (usePhysics) {
				centroid = triangleCentroids[triangleNumber];
			} else {
				centroid =
					(newVertices[firstVertexIndex] +
					newVertices[firstVertexIndex + 1] +
					newVertices[firstVertexIndex + 2]) / 3;
				triangleCentroids[triangleNumber] = centroid;
			}

			Vector3 normal;
			if (useNormals && newNormals != null) {
				if (usePhysics) {
					newNormals = physicsMeshes[triangleNumber].normals;
					firstVertexIndex = 0;
				}
				
				normal =
					((newNormals[firstVertexIndex] +
					newNormals[firstVertexIndex + 1] +
					newNormals[firstVertexIndex + 2]) / 3).normalized;
			} else {
				normal = centroid;
				if (useMeshBoundsCenter) {
					normal -= newMeshCenter;
				}
				normal.Normalize();
			}
			
			triangleNormals[triangleNumber] = normal;
		}
		
		preparation = prep;
		if (cachePreparation) cache[oldMesh] = prep;
		
		if (fadeTime != 0 && !shadersAlreadyHandleTransparency) {
			// Preload any replacement shaders that will be needed:
			foreach (var i in GetComponent<Renderer>().sharedMaterials) {
				var shader = i.shader;
				var replacement = Fade.GetReplacementFor(shader);
				if (replacement == null || !replacement.name.StartsWith("Transparent/")) {
					Debug.LogWarning("Couldn't find an explicitly transparent version of shader" +
						" '" + shader.name + "' so fading may not work. If the shader does" +
						" support transparency then this warning can be avoided by enabling" +
						" the 'Shaders Already Handle Transparency' option.");
				}
			}
		}
	}
	
	public GameObject Explode() {
		if (preparation.startMesh == null && preparation.physicsMeshes == null) {
#if UNITY_4_0
			var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
			if (skinnedMeshRenderer == null) {
				return null; // Initialization failed.
			}
			
			if (skinnedMeshRenderer.sharedMesh == null) {
				Debug.LogError("The SkinnedMeshRenderer does not reference a mesh.");
				return null;
			}
			
			var mesh = new Mesh();
			skinnedMeshRenderer.BakeMesh(mesh);
			// Since the mesh we just baked is a one-off, it doesn't make any sense to cache it:
			PrepareWithoutCaching(mesh);
#else
			return null; // Initialization failed.
#endif
		}
		
		var explosionPieceName = gameObject.name + " (Mesh Explosion)";
		
		if (type == ExplosionType.Physics) {
			var physicsMeshes = preparation.physicsMeshes;
			var rotations = preparation.rotations;
			
			var deltaSpeed = maxSpeed - minSpeed;
			var deltaRotationSpeed = maxRotationSpeed - minRotationSpeed;
			var fixedSpeed = minSpeed == maxSpeed;
			var fixedRotationSpeed = minRotationSpeed == maxRotationSpeed;
			
			var triangleCentroids = preparation.triangleCentroids;
			var triangleNormals = preparation.triangleNormals;
			
			var thisTransform = this.transform;
			var thisRotation = thisTransform.rotation;
			var thisPosition = thisTransform.position;
			
			var n = physicsMeshes.Length;
			var currentSubMesh = 0;
			var materials = GetComponent<Renderer>().materials;
			var frontMeshTrianglesPerSubMesh = preparation.frontMeshTrianglesPerSubMesh;
			var trianglesInCurrentSubMesh = frontMeshTrianglesPerSubMesh[0];
			Material currentMaterial = null;
			var totalFadeTime = fadeWaitTime + fadeTime;
			
			for (int i = 0, triangleInCurrentSubMesh = 0; i < n; ++i, ++triangleInCurrentSubMesh) {
				if (triangleInCurrentSubMesh == trianglesInCurrentSubMesh) {
					triangleInCurrentSubMesh = 0;
					++currentSubMesh;
					trianglesInCurrentSubMesh = frontMeshTrianglesPerSubMesh[currentSubMesh];
					currentMaterial = null;
				}
				
				var explosionPiece =
					SetUpExplosionPiece(explosionPieceName, currentMaterial == null);
				
				if (currentMaterial == null) {
					currentMaterial = materials[currentSubMesh];
					if (fadeTime != 0) {
						// Set the material explicitly because otherwise when Fade fetches it from
						// the renderer it will make another copy.
						explosionPiece.GetComponent<Fade>().materials =
							new Material[] { currentMaterial };
					}
				} else {
					if (fadeTime != 0) {
						explosionPiece.AddComponent<DestroyAfterTime>().waitTime = totalFadeTime;
					}
				}
				
				explosionPiece.GetComponent<MeshRenderer>().sharedMaterials =
					new Material[] { currentMaterial };
				
				var position = triangleCentroids[i];
				var rotation = rotations[i];
				
				// Transform them to where this GameObject is:
				position = thisRotation * position + thisPosition;
				rotation = thisRotation * rotation;
				
				var t = explosionPiece.transform;
				t.localPosition = position;
				t.localRotation = rotation;
				
				var mesh = preparation.physicsMeshes[i];
				explosionPiece.GetComponent<MeshFilter>().mesh = mesh;
				
				var rb = explosionPiece.AddComponent<Rigidbody>();
				rb.angularVelocity = Quaternion.AngleAxis(fixedRotationSpeed ?
					minRotationSpeed : minRotationSpeed + Random.value * deltaRotationSpeed,
					Random.onUnitSphere).eulerAngles;
				rb.velocity = (fixedSpeed ? minSpeed : minSpeed + Random.value * deltaSpeed) *
					triangleNormals[i];
				
				var boxCollider = explosionPiece.AddComponent<BoxCollider>();
				
				var size = mesh.bounds.size;
				size.y = colliderThickness;
				boxCollider.size = size;

				const float fragmentDensity = 1;
				rb.SetDensity(fragmentDensity);
			}
			
			return null;
		} else {
			var explosion = SetUpExplosionPiece(explosionPieceName);
			
			explosion.AddComponent<MeshExplosion>().Go(
				preparation, minSpeed, maxSpeed, minRotationSpeed, maxRotationSpeed, useGravity);
			
			return explosion;
		}
	}
	
	GameObject SetUpExplosionPiece(string name, bool addFade = true) {
		var explosion = new GameObject(name);
		
		{
			var thisTransform = transform;
			var explosionTransform = explosion.transform;
			explosionTransform.localPosition = thisTransform.position;
			explosionTransform.localRotation = thisTransform.rotation;
		}
		
		explosion.AddComponent<MeshFilter>();
		var explosionRenderer = explosion.AddComponent<MeshRenderer>();
		{
			var thisRenderer = GetComponent<Renderer>();
			explosionRenderer.castShadows = thisRenderer.castShadows;
			explosionRenderer.receiveShadows = thisRenderer.receiveShadows;
			explosionRenderer.sharedMaterials = thisRenderer.sharedMaterials;
			explosionRenderer.useLightProbes = thisRenderer.useLightProbes;
		}
		
		if (fadeTime != 0) {
			if (addFade) {
				var fade = explosion.AddComponent<Fade>();
				fade.waitTime = fadeWaitTime;
				fade.fadeTime = fadeTime;
				fade.replaceShaders = !shadersAlreadyHandleTransparency;
				explosion.AddComponent<DestroyOnFadeCompletion>();
			}
			
			if (!allowShadows) {
				explosionRenderer.castShadows = false;
				explosionRenderer.receiveShadows = false;
			}
		}
		
		return explosion;
	}
	
}

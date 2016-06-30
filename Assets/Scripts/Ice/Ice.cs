﻿#define DEBUG_CC2D_RAYS
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Heat;
using Assets.Scripts.Helpers;
using UnityEngine;

namespace Assets.Scripts.Ice
{
	[RequireComponent(typeof(PolygonCollider2D), typeof(MeshFilter), typeof(MeshRenderer))]
	public class Ice : MonoBehaviour
	{
		public float ActualDistanceBetweenPoints = 0.2f;

		public bool GrowsBack;
		public bool ExtraPoints;
		public bool AnyJointEnabled { get; set; }

		private PolygonCollider2D _polyCollider;
		private Mesh _mesh;
		private Vector2[] _initialPoints;
		private FixedJoint2D[] _joints;
		private MeshRenderer _meshRenderer;
		private bool _meltedThisFrame;

		void Awake()
		{
			_polyCollider = GetComponent<PolygonCollider2D>();
			_mesh = GetComponent<MeshFilter>().mesh;
			_joints = GetComponents<FixedJoint2D>();
			_meshRenderer = GetComponent<MeshRenderer>();
			AnyJointEnabled = true;
			GrowsBack = true;
		}

		void Start()
		{
			if (ExtraPoints)
				_polyCollider.points = GetColliderPointsAtIntervals(ActualDistanceBetweenPoints);
			_initialPoints = _polyCollider.points;
			SetMeshFilterToPolyColliderPoints();
		}

		private Vector2[] GetColliderPointsAtIntervals(float interval)
		{
			var newPoints = new List<Vector2>();

			for (int i = 0; i < _polyCollider.points.Length; i++)
			{
				Vector2 start = _polyCollider.points[i];
				Vector2 end = i + 1 == _polyCollider.points.Length ? _polyCollider.points[0] : _polyCollider.points[i + 1];

				float distanceBetweenPoints = interval / Vector2.Distance(start, end);
				while (Vector2.Distance(start, end) > distanceBetweenPoints)
				{
					newPoints.Add(start);
					start = Vector2.MoveTowards(start, end, distanceBetweenPoints);
				}
			}
			return newPoints.ToArray();
		}

		private void SetMeshFilterToPolyColliderPoints()
		{
			_mesh.Clear();
			_mesh.vertices = GetNewMeshFilterVertices();
			_mesh.triangles = GetNewMeshFilterTriangles(_polyCollider.points);
			_mesh.RecalculateNormals();
		}

		private Vector3[] GetNewMeshFilterVertices()
		{
			var vertices = new Vector3[_polyCollider.GetTotalPointCount()];
			for (int j = 0; j < vertices.Length; j++)
			{
				vertices[j] = new Vector3(
					_polyCollider.points[j].x,
					_polyCollider.points[j].y,
					0);
			}
			return vertices;
		}

		private static int[] GetNewMeshFilterTriangles(Vector2[] points)
		{
			var tr = new Triangulator(points);
			return tr.Triangulate();
		}

		void Update()
		{
			foreach (FixedJoint2D joint in _joints)
			{
				if (_polyCollider.OverlapPoint(transform.TransformPoint(joint.anchor)) == false)
					joint.enabled = false;
			}

			if (_joints.Length > 0 && _joints.Any(j => j.enabled) == false)
				AnyJointEnabled = false;

			_polyCollider.enabled = true;

			if (_polyCollider.bounds.size.x < 0.3 || _polyCollider.bounds.size.y < 0.3)
			{
				_polyCollider.enabled = false;
				_meshRenderer.enabled = false;
			}
			else
			{
				_polyCollider.enabled = true;
				_meshRenderer.enabled = true;
			}

			if (GrowsBack && _meltedThisFrame == false)
			{
				_polyCollider.points = RaisePoints();
				SetMeshFilterToPolyColliderPoints();
			}

			_meltedThisFrame = false;
		}

		private Vector2[] RaisePoints()
		{
			Vector2[] newPoints = _polyCollider.points;
			for (int i = 0; i < _polyCollider.points.Length; i++)
			{
				Vector2 point = _polyCollider.points[i];
				Vector2 initialPoint = _initialPoints[i];

				if (point != initialPoint)
				{
					point = Vector2.MoveTowards(point, initialPoint, UnityEngine.Random.value / 100);
				}
				newPoints[i] = point;
			}
			return newPoints;
		}

		void Melt(HeatMessage message)
		{
			//Texture2D tex = Instantiate(_meshRenderer.material.mainTexture) as Texture2D;
			//_meshRenderer.material.mainTexture = tex;

			//Vector2 local = transform.InverseTransformPoint(message.Origin.x, message.Origin.y, 0);

			//Circle(tex, System.Convert.ToInt32(local.x), System.Convert.ToInt32(local.y), System.Convert.ToInt32(message.CastDistance), Color.clear);
			_meltedThisFrame = true;
			_polyCollider.points = MovePointsInwards(message);
			_polyCollider.points = FlattenAngles();
			SetMeshFilterToPolyColliderPoints();
		}

		//A message arrives at the ice. A single raycasthit and the origin of the cast. The points of the ice that are within the distance to the origin, and that are not
		//on the wrong side of the normal, move away from the origin proportionally to the distance. If moving would break the polycollider then the point does not move

		private void aaa()
		{
			// duplicate the original texture and assign to the material
			Texture2D texture = Instantiate(_meshRenderer.material.mainTexture) as Texture2D;
			_meshRenderer.material.mainTexture = texture;
			// colors used to tint the first 3 mip levels
			Color[] colors = new Color[3];
			colors[0] = Color.red;
			colors[1] = Color.green;
			colors[2] = Color.blue;
			int mipCount = Mathf.Min(3, texture.mipmapCount);

			// tint each mip level
			for (int mip = 0; mip < mipCount; ++mip)
			{
				Color[] cols = texture.GetPixels();
				for (int i = 0; i < cols.Length; ++i)
				{
					cols[i] = Color.Lerp(cols[i], colors[mip], 0.33f);
				}
				texture.SetPixels(cols, mip);
			}
			// actually apply all SetPixels, don't recalculate mip levels
			texture.Apply(false);
		}

		public void Circle(Texture2D tex, int cx, int cy, int r, Color col)
		{
			

			int x, y, px, nx, py, ny, d;
			Color32[] tempArray = tex.GetPixels32();

			for (x = 0; x <= r; x++)
			{
				d = (int)Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
				for (y = 0; y <= d; y++)
				{
					px = cx + x;
					nx = cx - x;
					py = cy + y;
					ny = cy - y;

					tempArray[py * tex.width + px] = col;
					tempArray[py * tex.width + nx] = col;
					tempArray[ny * tex.width + px] = col;
					tempArray[ny * tex.width + nx] = col;
				}
			}
			tex.SetPixels32(tempArray);
			tex.Apply();
		}

		private Vector2[] MovePointsInwards(HeatMessage message)
		{
			Vector2[] newPoints = _polyCollider.points;
			IEnumerable<int> allIndices = GetIndicesInRange(message.Origin, message.CastDistance);

			foreach (int i in allIndices)
			{
				int beforeIndex = GetBeforeIndex(i);
				int afterIndex = GetAfterIndex(i);

				Vector2 point = transform.TransformPoint(_polyCollider.points[i]);
				Vector2 beforePoint = transform.TransformPoint(_polyCollider.points[beforeIndex]);
				Vector2 afterPoint = transform.TransformPoint(_polyCollider.points[afterIndex]);

				Vector2 offSet = Vector2.Lerp(beforePoint, afterPoint, 0.5f);
				Vector2 perpendicular = new Vector2(-offSet.y, offSet.x) / (offSet.magnitude * 100f);

				Vector2 newPoint = Vector2.MoveTowards(point, perpendicular.normalized, message.DistanceToMove);
				if (_polyCollider.OverlapPoint(newPoint))
				{
					newPoints[i] = transform.InverseTransformPoint(newPoint);
				}
				else
				{
					Vector2 perpendicular2 = new Vector2(-offSet.y, offSet.x) / (offSet.magnitude * -100f);
					newPoint = Vector2.MoveTowards(point, perpendicular2.normalized, message.DistanceToMove);
					if (_polyCollider.OverlapPoint(newPoint))
						newPoints[i] = transform.InverseTransformPoint(newPoint);
				}
			}
			return newPoints;
		}

		private Vector2[] FlattenAngles()
		{
			Vector2[] newPoints = _polyCollider.points;
			int currentIndex = 0;
			int count = 0;
			while (count < _polyCollider.points.Length)
			{
				newPoints[currentIndex] = AcuteAngleFlattened(
					transform.TransformPoint(newPoints[currentIndex]),
					transform.TransformPoint(newPoints[GetBeforeIndex(currentIndex)]),
					transform.TransformPoint(newPoints[GetAfterIndex(currentIndex)]));
				currentIndex = GetBeforeIndex(currentIndex);
				count++;
			}
			return newPoints;
		}

		private Vector2 AcuteAngleFlattened(Vector2 point, Vector2 beforePoint, Vector2 afterPoint)
		{
			Vector2 centre = Vector2.Lerp(beforePoint, afterPoint, 0.5f);
			float angle = Angle(beforePoint, afterPoint, point);

			while (angle < 90)
			{
				point = Vector2.MoveTowards(point, centre, 100f);
				angle = Angle(beforePoint, afterPoint, point);
			}
			return transform.InverseTransformPoint(point);
		}

		private static float Angle(Vector2 beforePoint, Vector2 afterPoint, Vector2 point)
		{
			return Vector2.Angle(beforePoint - point, afterPoint - point);
		}

		private int GetBeforeIndex(int currentIndex)
		{
			return currentIndex == 0 ? _polyCollider.points.Length - 1 : currentIndex - 1;			
		}

		private int GetAfterIndex(int currentIndex)
		{
			return currentIndex == _polyCollider.points.Length - 1 ? 0 : currentIndex + 1;
		}

		private IEnumerable<int> GetIndicesInRange(Vector2 origin, float distance)
		{
			var indices = new List<int>();

			for (int i = 0; i < _polyCollider.points.Length; i++)
			{
				Vector2 worldPoint = transform.TransformPoint(_polyCollider.points[i]);

				if (Vector2.Distance(worldPoint, origin) < distance)
					indices.Add(i);
			}

			return indices;
		}

		void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.layer == LayerMask.NameToLayer(Layers.Background))
			{
				GrowsBack = false;
			}
		}

		[System.Diagnostics.Conditional("DEBUG_CC2D_RAYS")]
		void DrawRay(Vector3 start, Vector3 dir, Color color)
		{
			Debug.DrawRay(start, dir, color);
		}
	}
}

using System;
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;

namespace PlanetaryExpansion
{
	public static class AOneMath
	{
		public static Random random = new Random ();

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatIntUnion
		{
			[FieldOffset(0)]
			public float f;

			[FieldOffset(0)]
			public int tmp;
		}
		
		/// <summary>
		/// Checks whether a ray intersects a triangle. This uses the algorithm
		/// developed by Tomas Moller and Ben Trumbore, which was published in the
		/// Journal of Graphics Tools, volume 2, "Fast, Minimum Storage Ray-Triangle
		/// Intersection".
		/// 
		/// This method is implemented using the pass-by-reference versions of the
		/// XNA math functions. Using these overloads is generally not recommended,
		/// because they make the code less readable than the normal pass-by-value
		/// versions. This method can be called very frequently in a tight inner loop,
		/// however, so in this particular case the performance benefits from passing
		/// everything by reference outweigh the loss of readability.
		/// </summary>
		public static void RayIntersectsTriangle(ref Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float? result)
			{
			    // Compute vectors along two edges of the triangle.
			    Vector3 edge1, edge2;

			    Vector3.Subtract(ref vertex2, ref vertex1, out edge1);
			    Vector3.Subtract(ref vertex3, ref vertex1, out edge2);

			    // Compute the determinant.
			    Vector3 directionCrossEdge2;
			    Vector3.Cross(ref ray.Direction, ref edge2, out directionCrossEdge2);

			    float determinant;
			    Vector3.Dot(ref edge1, ref directionCrossEdge2, out determinant);

			    // If the ray is parallel to the triangle plane, there is no collision.
			    if (determinant > -float.Epsilon && determinant < float.Epsilon)
			    {
			        result = null;
			        return;
			    }

			    float inverseDeterminant = 1.0f / determinant;

			    // Calculate the U parameter of the intersection point.
			    Vector3 distanceVector;
			    Vector3.Subtract(ref ray.Position, ref vertex1, out distanceVector);

			    float triangleU;
			    Vector3.Dot(ref distanceVector, ref directionCrossEdge2, out triangleU);
			    triangleU *= inverseDeterminant;

			    // Make sure it is inside the triangle.
			    if (triangleU < 0 || triangleU > 1)
			    {
			        result = null;
			        return;
			    }

			    // Calculate the V parameter of the intersection point.
			    Vector3 distanceCrossEdge1;
			    Vector3.Cross(ref distanceVector, ref edge1, out distanceCrossEdge1);

			    float triangleV;
			    Vector3.Dot(ref ray.Direction, ref distanceCrossEdge1, out triangleV);
			    triangleV *= inverseDeterminant;

			    // Make sure it is inside the triangle.
			    if (triangleV < 0 || triangleU + triangleV > 1)
			    {
			        result = null;
			        return;
			    }

			    // Compute the distance along the ray to the triangle.
			    float rayDistance;
			    Vector3.Dot(ref edge2, ref distanceCrossEdge1, out rayDistance);
			    rayDistance *= inverseDeterminant;

			    // Is the triangle behind the ray origin?
			    if (rayDistance < 0)
			    {
			        result = null;
			        return;
			    }

			    result = rayDistance;
			}

		public static float Sqrt2(float z)
		{
			if (z == 0) return 0;
			FloatIntUnion u;
			u.tmp = 0;
			float xhalf = 0.5f * z;
			u.f = z;
			u.tmp = 0x5f375a86 - (u.tmp >> 1);
			u.f = u.f * (1.5f - xhalf * u.f * u.f);
			return u.f * z;
		}

		public static float Distance (float x1, float x2, float y1, float y2)
		{
			return (float)Math.Sqrt (Square (x1 - x2) + Square (y1 - y2));
		}

		public static float ManhattanDistance (float x1, float x2, float y1, float y2)
		{
			float distX = x1 - x2;
			float distY = y1 - y2;

			return (float)Math.Abs (distX) + Math.Abs (distY);
		}

		public static float Square(float z)
		{
			z *= z;
			return z;
		}

		public static float Vector2ToRadian (float x, float y)
		{
			return (float)Math.Atan2 (x, -y);
		}

		public static float AngleToRadian (float angle)
		{
			return (float)(angle * (Math.PI / 180));
		}

        //public static float ScreenToScale ()
        //{
        //    float scale = (float)Game1.ScreenSize.X / (float)1920;
        //    return scale;
        //}

		//public static Vector2 CalculateRightTrianglePoint (Vector2 a, Vector2 b, float length)
		//{
			//float x;
			//float y;



			//return new Vector2 (x, y);
		//}
	}
}
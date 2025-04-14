using System.Runtime.CompilerServices;
using UnityEngine;

namespace NWH.WheelController3D
{
    public static class WheelControllerUtility
    {
        /// <summary>
        /// Creates wheel cylinder mesh.
        /// </summary>
        public static Mesh CreateCylinderMesh(int subdivisions, float height, float radius)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[(subdivisions + 1) * 2];
            int[] triangles = new int[subdivisions * 6];

            // Create vertices
            float angleStep = 2 * Mathf.PI / subdivisions;
            for (int i = 0; i <= subdivisions; i++)
            {
                float angle = i * angleStep;
                float y = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices[i] = new Vector3(-height / 2, y, z);
                vertices[i + subdivisions + 1] = new Vector3(height / 2, y, z);
            }

            // Create triangles
            for (int i = 0; i < subdivisions; i++)
            {
                int mod = subdivisions + 1;
                triangles[i * 6 + 0] = i;
                triangles[i * 6 + 1] = (i + 1) % mod;
                triangles[i * 6 + 2] = 0;
                triangles[i * 6 + 3] = i + mod;
                triangles[i * 6 + 4] = (i + 1) % mod + mod;
                triangles[i * 6 + 5] = mod;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            return mesh;
        }
    }
}

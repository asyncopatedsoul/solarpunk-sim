using SpacetimeDB;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Client-side representation of the DbVector3 struct from the server
    /// This class must match the structure defined in the Rust server code
    /// </summary>
    [SpacetimeDB.Table]
    public class DbVector3
    {
        [SpacetimeDB.Column] public float X { get; set; }
        [SpacetimeDB.Column] public float Y { get; set; }
        [SpacetimeDB.Column] public float Z { get; set; }

        public DbVector3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public DbVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        // Conversion from Unity Vector3
        public static implicit operator DbVector3(UnityEngine.Vector3 vector)
        {
            return new DbVector3
            {
                X = vector.x,
                Y = vector.y,
                Z = vector.z
            };
        }

        // Conversion to Unity Vector3
        public static implicit operator UnityEngine.Vector3(DbVector3 vector)
        {
            return new UnityEngine.Vector3(vector.X, vector.Y, vector.Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}

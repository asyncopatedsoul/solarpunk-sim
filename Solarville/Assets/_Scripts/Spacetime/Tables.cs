using SpacetimeDB;
using SpacetimeDB.Types;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Client-side representation of the WorldConfig table from the server
    /// This class must match the structure defined in the Rust server code
    /// </summary>
    [SpacetimeDB.Table]
    public class WorldConfig
    {
        [SpacetimeDB.Column, SpacetimeDB.PrimaryKey]
        public uint Id { get; set; }
        
        [SpacetimeDB.Column]
        public string WorldName { get; set; }
    }

    /// <summary>
    /// Client-side representation of the Player table from the server
    /// This class must match the structure defined in the Rust server code
    /// </summary>
    [SpacetimeDB.Table]
    public class Player
    {
        [SpacetimeDB.Column, SpacetimeDB.PrimaryKey]
        public Identity Identity { get; set; }
        
        [SpacetimeDB.Column, SpacetimeDB.Unique]
        public uint PlayerId { get; set; }
        
        [SpacetimeDB.Column]
        public string Name { get; set; }
        
        [SpacetimeDB.Column]
        public DbVector3 Position { get; set; }
        
        [SpacetimeDB.Column]
        public DbVector3 Rotation { get; set; }
        
        [SpacetimeDB.Column]
        public Timestamp LastUpdate { get; set; }
    }

    /// <summary>
    /// Defines the reducers (methods) that can be called on the server
    /// </summary>
    [SpacetimeDB.Reducer]
    public interface SolarvilleReducers
    {
        void RegisterPlayer(string name, DbVector3 position, DbVector3 rotation);
        void UpdatePlayerPosition(DbVector3 position, DbVector3 rotation);
    }
}
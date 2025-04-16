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
    /// Client-side representation of the MicroprocessCode table from the server
    /// </summary>
    [SpacetimeDB.Table]
    public class MicroprocessCode
    {
        [SpacetimeDB.Column, SpacetimeDB.PrimaryKey]
        public uint CodeId { get; set; }
        
        [SpacetimeDB.Column]
        public uint OwnerId { get; set; }
        
        [SpacetimeDB.Column]
        public string Name { get; set; }
        
        [SpacetimeDB.Column]
        public string FilePath { get; set; }
        
        [SpacetimeDB.Column]
        public string CodeContent { get; set; }
        
        [SpacetimeDB.Column]
        public Timestamp LastUpdated { get; set; }
    }
    
    /// <summary>
    /// Client-side representation of the MicroprocessState table from the server
    /// </summary>
    [SpacetimeDB.Table]
    public class MicroprocessState
    {
        [SpacetimeDB.Column, SpacetimeDB.PrimaryKey]
        public uint StateId { get; set; }
        
        [SpacetimeDB.Column]
        public uint CodeId { get; set; }
        
        [SpacetimeDB.Column]
        public float LeftMotorSpeed { get; set; }
        
        [SpacetimeDB.Column]
        public float RightMotorSpeed { get; set; }
        
        [SpacetimeDB.Column]
        public string ErrorMessage { get; set; }
        
        [SpacetimeDB.Column]
        public Timestamp LastUpdated { get; set; }
        
        [SpacetimeDB.Column]
        public bool IsRunning { get; set; }
    }

    /// <summary>
    /// Defines the reducers (methods) that can be called on the server
    /// </summary>
    [SpacetimeDB.Reducer]
    public interface SolarvilleReducers
    {
        void RegisterPlayer(string name, DbVector3 position, DbVector3 rotation);
        void UpdatePlayerPosition(DbVector3 position, DbVector3 rotation);
        uint SaveMicroprocessCode(string name, string filePath, string codeContent);
        void UpdateMicroprocessState(uint codeId, float leftMotorSpeed, float rightMotorSpeed, string errorMessage, bool isRunning);
        void StartMicroprocess(uint codeId);
        void StopMicroprocess(uint codeId);
    }
}
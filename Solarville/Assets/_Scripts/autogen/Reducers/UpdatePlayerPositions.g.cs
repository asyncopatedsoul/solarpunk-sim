// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN YOUR MODULE SOURCE CODE INSTEAD.

#nullable enable

using System;
using SpacetimeDB.ClientApi;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SpacetimeDB.Types
{
    public sealed partial class RemoteReducers : RemoteBase
    {
        public delegate void UpdatePlayerPositionsHandler(ReducerEventContext ctx, UpdatePlayerTimer timer);
        public event UpdatePlayerPositionsHandler? OnUpdatePlayerPositions;

        public void UpdatePlayerPositions(UpdatePlayerTimer timer)
        {
            conn.InternalCallReducer(new Reducer.UpdatePlayerPositions(timer), this.SetCallReducerFlags.UpdatePlayerPositionsFlags);
        }

        public bool InvokeUpdatePlayerPositions(ReducerEventContext ctx, Reducer.UpdatePlayerPositions args)
        {
            if (OnUpdatePlayerPositions == null) return false;
            OnUpdatePlayerPositions(
                ctx,
                args.Timer
            );
            return true;
        }
    }

    public abstract partial class Reducer
    {
        [SpacetimeDB.Type]
        [DataContract]
        public sealed partial class UpdatePlayerPositions : Reducer, IReducerArgs
        {
            [DataMember(Name = "_timer")]
            public UpdatePlayerTimer Timer;

            public UpdatePlayerPositions(UpdatePlayerTimer Timer)
            {
                this.Timer = Timer;
            }

            public UpdatePlayerPositions()
            {
                this.Timer = new();
            }

            string IReducerArgs.ReducerName => "update_player_positions";
        }
    }

    public sealed partial class SetReducerFlags
    {
        internal CallReducerFlags UpdatePlayerPositionsFlags;
        public void UpdatePlayerPositions(CallReducerFlags flags) => UpdatePlayerPositionsFlags = flags;
    }
}

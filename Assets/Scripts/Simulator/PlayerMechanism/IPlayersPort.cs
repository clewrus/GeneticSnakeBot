using System;
using System.Collections.Generic;

namespace Simulator {
    public interface IPlayersPort {
        Func<int> GetNextId { set; }

        List<MoveInfo> MakeMove ();
        void HandleMoveResult (List<MoveResult> results);

        void AddPlayer (IPlayer player, bool needsInput);
    }
}

using System.Collections;
using System.Collections.Generic;

namespace Simulator {
    public interface IPlayer {
        MoveInfo MakeMove (Projection playerInput);
        void HandleMoveResult (MoveResult result);

        SnakeInfo GetSnakeInfo ();
    }
}
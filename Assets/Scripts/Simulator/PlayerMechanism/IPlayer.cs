using System.Collections;
using System.Collections.Generic;

namespace Simulator {
	public interface IPlayer {
		MoveInfo MakeMove (MoveInfo.Direction forwardDir, Projection playerInput);
		void HandleMoveResult (MoveResult result);

		bool NeedsProjection { get; }

		SnakeInfo GetSnakeInfo ();
	}
}
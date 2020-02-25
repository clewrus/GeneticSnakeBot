using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Simulator {
	public class NormalPlayersPort : IPlayersPort {
		public Func<int> GetNextId { private get; set; }

        private Dictionary<int, IPlayer> idToPlayer;
        private Dictionary<IPlayer, int> playerToId;

        private Dictionary<IPlayer, bool> needsInput;

        public NormalPlayersPort () {
            idToPlayer = new Dictionary<int, IPlayer>();
            playerToId = new Dictionary<IPlayer, int>();
            needsInput = new Dictionary<IPlayer, bool>();
        }

		public void AddPlayer (IPlayer player, bool needsInput) {
			this.needsInput.Add(player, needsInput);
            Debug.Assert(GetNextId != null, $"({this}) GetNextId Func is null");

            int id = GetNextId();
            idToPlayer.Add(id, player);
            playerToId.Add(player, id);
		}

        public void RemovePlayer (int id) {
            if (!idToPlayer.ContainsKey(id)) return;

            var player = idToPlayer[id];
            idToPlayer.Remove(id);
            playerToId.Remove(player);
            needsInput.Remove(player);
        }

		public void HandleMoveResult (List<MoveResult> results) {
			throw new NotImplementedException();
		}

		public List<MoveInfo> MakeMove (FieldProjector projector) {
            var moveInfos = new List<MoveInfo>(idToPlayer.Count);

			foreach (var id_player in idToPlayer) {
                var proj = (needsInput[id_player.Value])? default(Projection) : default(Projection);

                var move = id_player.Value.MakeMove(proj);
                move.id = id_player.Key;
                move.valueUsed = 0.1f;

                moveInfos.Add(move);
            }

            return moveInfos;
		}

		public SnakeInfo GetSnakeInfo (int id) {
			if (idToPlayer.TryGetValue(id, out IPlayer tarPlayer)) {
                return tarPlayer.GetSnakeInfo();
            }

            return null;
		}
	}
}
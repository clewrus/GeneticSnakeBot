using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator {
	public class NormalPlayersPort : IPlayersPort {
		public System.Func<int> GetNextId { private get; set; }

        private Dictionary<int, IPlayer> idToPlayer;
        private Dictionary<IPlayer, int> playerToId;

        private Dictionary<IPlayer, bool> needsInput;
        private Dictionary<int, (Vector2Int headPos, MoveInfo.Direction headDir)> idToHeadInfo;

        public NormalPlayersPort () {
            idToPlayer = new Dictionary<int, IPlayer>();
            playerToId = new Dictionary<IPlayer, int>();
            needsInput = new Dictionary<IPlayer, bool>();
            idToHeadInfo = new Dictionary<int, (Vector2Int, MoveInfo.Direction)>();
        }

		public void AddPlayer (IPlayer player, bool needsInput) {
			this.needsInput.Add(player, needsInput);
            Debug.Assert(GetNextId != null, $"({this}) GetNextId Func is null");

            int id = GetNextId();
            this.idToPlayer.Add(id, player);
            this.playerToId.Add(player, id);

            this.idToHeadInfo.Add(id, (Vector2Int.zero, MoveInfo.Direction.None));
		}

        public void RemovePlayer (int id) {
            if (!idToPlayer.ContainsKey(id)) return;

            var player = idToPlayer[id];
            idToPlayer.Remove(id);
            playerToId.Remove(player);

            needsInput.Remove(player);
            idToHeadInfo.Remove(id);
        }

		public void HandleMoveResult (List<MoveResult> results) {
			results.ForEach((res) => {
                idToHeadInfo[res.id] = (res.headPos, res.headDir);
            });

            results.ForEach((res) => {
                idToPlayer[res.id].HandleMoveResult(res);
            });
		}

		public List<MoveInfo> MakeMove (FieldProjector projector) {
            var moveInfos = new List<MoveInfo>(idToPlayer.Count);

			foreach (var id_player in idToPlayer) {
                var proj = default(Projection);

                if (needsInput[id_player.Value]) {
                    var headInfo = idToHeadInfo[id_player.Key];
                    var snakeInfo = id_player.Value.GetSnakeInfo();

                    proj = projector.CalcSnakeView(headInfo.headPos, headInfo.headDir, snakeInfo.halfViewAngle);
                }

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
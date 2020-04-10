using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator {
	public class NormalPlayersPort : IPlayersPort {
		public System.Func<int> GetNextId { private get; set; }

		public IScorer Scorer { get; set; }

		private Dictionary<int, IPlayer> idToPlayer;
		private Dictionary<IPlayer, int> playerToId;

		private Dictionary<int, (Vector2Int headPos, MoveInfo.Direction headDir)> idToHeadInfo;

		public NormalPlayersPort () {
			idToPlayer = new Dictionary<int, IPlayer>();
			playerToId = new Dictionary<IPlayer, int>();
			idToHeadInfo = new Dictionary<int, (Vector2Int, MoveInfo.Direction)>();
		}

		public void AddPlayer (IPlayer player) {
			Debug.Assert(GetNextId != null, $"({this}) GetNextId Func is null");

			int id = GetNextId();
			this.idToPlayer.Add(id, player);
			this.playerToId.Add(player, id);

			this.idToHeadInfo.Add(id, (Vector2Int.zero, MoveInfo.Direction.None));
		}

		public bool Contains (IPlayer player) {
			return playerToId.ContainsKey(player);
		}

		public bool TryGetPlayerId (IPlayer player, out int id) {
			return playerToId.TryGetValue(player, out id);
		}

		public void RemovePlayer (int id) {
			if (!idToPlayer.ContainsKey(id)) return;

			var player = idToPlayer[id];
			idToPlayer.Remove(id);
			playerToId.Remove(player);

			idToHeadInfo.Remove(id);
		}

		public void HandleMoveResult (List<MoveResult> results) {
			results.ForEach((res) => {
				idToHeadInfo[res.id] = (res.headPos, res.headDir);
			});

			results.ForEach((res) => {
				var curPlayer = idToPlayer[res.id];

				if (Scorer != null) {
					Scorer.UpdateScore(curPlayer, res.eatenValue);
				}

				curPlayer.HandleMoveResult(res);
			});
		}

		public List<MoveInfo> MakeMove (FieldProjector projector) {
			var moveInfos = new List<MoveInfo>(idToPlayer.Count);

			foreach (var id_player in idToPlayer) {
				if (idToHeadInfo[id_player.Key].headDir == MoveInfo.Direction.None) {
					moveInfos.Add(new MoveInfo { id = id_player.Key, valueUsed = 0f });
					continue;
				}

				var proj = default(Projection);
				var headInfo = idToHeadInfo[id_player.Key];

				if (id_player.Value.NeedsProjection) {				
					var snakeInfo = id_player.Value.GetSnakeInfo();
					proj = projector.CalcSnakeView(
						pos: (headInfo.headPos.x, headInfo.headPos.y), 
						dir: headInfo.headDir, 
						cullingDistance: snakeInfo.cullingDistance, 
						halfViewAngle: snakeInfo.halfViewAngle,
						eyeQuality: snakeInfo.eyeQuality
					);
				}

				var move = id_player.Value.MakeMove(headInfo.headDir, proj);
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
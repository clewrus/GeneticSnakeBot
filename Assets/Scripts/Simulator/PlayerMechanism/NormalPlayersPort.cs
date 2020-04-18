using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Simulator {
	public class NormalPlayersPort : IPlayersPort {
		public System.Func<int> GetNextId { private get; set; }

		public IScorer Scorer { get; set; }

		private Dictionary<int, IPlayer> idToPlayer;
		private Dictionary<IPlayer, int> playerToId;
		private List<(int id, IPlayer player)> orderedIdPlayer;

		private Dictionary<int, (Vector2Int headPos, MoveInfo.Direction headDir)> idToHeadInfo;

		private int? availableCores;

		public NormalPlayersPort () {
			idToPlayer = new Dictionary<int, IPlayer>();
			playerToId = new Dictionary<IPlayer, int>();
			orderedIdPlayer = new List<(int id, IPlayer player)>(30);

			idToHeadInfo = new Dictionary<int, (Vector2Int, MoveInfo.Direction)>();

			availableCores = System.Environment.ProcessorCount;
		}

		public void AddPlayer (IPlayer player) {
			Debug.Assert(GetNextId != null, $"({this}) GetNextId Func is null");

			int id = GetNextId();
			this.idToPlayer.Add(id, player);
			this.playerToId.Add(player, id);
			this.orderedIdPlayer.Add((id, player));

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
			orderedIdPlayer.Remove((id, player));

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
			var numOfTasks = availableCores.Value;
			var moveTasks = new Task[numOfTasks];

			int nxtOffset = 0;
			object lockForNxtOffset = new object();

			for (int k = 0; k < numOfTasks; ++k) {
				moveTasks[k] = Task.Run(() => {
					int offset = 0;
					lock(lockForNxtOffset) {
						offset = nxtOffset++;
					}

					for (int i = offset; i < orderedIdPlayer.Count; i += numOfTasks) {
						(int id, IPlayer player) = orderedIdPlayer[i];

						var move = EvaluatePlayerMove(projector, id, player);
						moveInfos.Add(move);
					}
				});
			}

			Task.WhenAll(moveTasks).Wait();
			return moveInfos;
		}

		private MoveInfo EvaluatePlayerMove (FieldProjector projector, int id, IPlayer player) {
			if (idToHeadInfo[id].headDir == MoveInfo.Direction.None) {
				return new MoveInfo { id = id, valueUsed = 0f };
			}

			var proj = default(Projection);
			var headInfo = idToHeadInfo[id];

			if (player.NeedsProjection) {
				var snakeInfo = player.GetSnakeInfo();
				proj = projector.CalcSnakeView(
					pos: (headInfo.headPos.x, headInfo.headPos.y),
					dir: headInfo.headDir,
					cullingDistance: snakeInfo.cullingDistance,
					halfViewAngle: snakeInfo.halfViewAngle,
					eyeQuality: snakeInfo.eyeQuality
				);
			}

			var move = player.MakeMove(headInfo.headDir, proj);
			move.id = id;
			move.valueUsed = 0.1f;

			return move;
		}

		public SnakeInfo GetSnakeInfo (int id) {
			if (idToPlayer.TryGetValue(id, out IPlayer tarPlayer)) {
				return tarPlayer.GetSnakeInfo();
			}

			return null;
		}
	}
}
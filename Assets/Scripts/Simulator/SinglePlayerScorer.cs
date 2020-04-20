

using Simulator;
using System.Collections.Generic;
using UnityEngine;

namespace Simulator {
	public class SinglePlayerScorer : IScorer {

		private Dictionary<IPlayer, ScoreInfo> playerScore;

		public SinglePlayerScorer () {
			playerScore = new Dictionary<IPlayer, ScoreInfo>();
		}

		public bool TryGetScore (IPlayer player, out float score) {
			if (player == null) {
				score = -1;
				return false;
			}

			if (playerScore.TryGetValue(player, out var scoreInfo)) {
				score = scoreInfo.score;
				return true;
			}

			score = -1;
			return false;
		}

		public void UpdateScore (IPlayer player, float scoreDelta) {
			if (playerScore.TryGetValue(player, out var curScoreInfo)) {
				curScoreInfo = UpdateScoreInfo(curScoreInfo, scoreDelta, Time.time);
				playerScore[player] = curScoreInfo;
			} else {
				playerScore.Add(player, new ScoreInfo {
					score = scoreDelta,
					multiplier = 1,
					lastUpdate = Time.time
				});
			}
		}

		public bool TryGetCurrentMultiplier (IPlayer player, out float multiplier) {
			if (player == null) {
				multiplier = -1;
				return false;
			}

			if (playerScore.TryGetValue(player, out var scoreInfo)) {
				float timeDelta = Time.time - scoreInfo.lastUpdate;
				multiplier = CalcCurrentMultiplier(scoreInfo.multiplier, timeDelta);
				return true;
			}

			multiplier = -1;
			return false;
		}

		private ScoreInfo UpdateScoreInfo (ScoreInfo info, float scoreDelta, float curTime) {
			float curTimeDelta = curTime - info.lastUpdate;
			info.multiplier = CalcCurrentMultiplier(info.multiplier, curTimeDelta);

			float multDelta = 1 / curTimeDelta - Mathf.Exp(1 / info.multiplier - 1);
			info.multiplier += Mathf.Clamp(multDelta, 0, 0.1f);

			info.multiplier = Mathf.Max(1, info.multiplier);
			info.score += scoreDelta * info.multiplier;
			info.lastUpdate = curTime;

			return info;
		}

		private float CalcCurrentMultiplier(float mult, float timeSinceLastUpdate) {
			return 1 + (mult - 1) * Mathf.Exp(-Mathf.Pow(timeSinceLastUpdate, 2));
		}

		private struct ScoreInfo {
			public float score;
			public float multiplier;
			public float lastUpdate;
		}
	}
}

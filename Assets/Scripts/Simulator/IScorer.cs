using Simulator;

namespace Simulator {
	public interface IScorer {
		bool TryGetScore (IPlayer player, out float score);
		void UpdateScore (IPlayer player, float scoreDelta);
	}
}

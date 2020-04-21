using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Game {
	public class SimulatorAdapter {
		private Simulator.Simulation m_Simulation;
		private Simulator.IScorer m_Scorer;
		private Visualization.Visualizer m_Visualizer;
		private Simulator.IPlayer m_ObservedPlayer;
		private Visualization.IVisualizerObserver m_ObservingCamera;

		private Simulator.IPlayersPort localPort;
		private Simulator.IPlayersPort networkPort = null;     // for future purpose

		#region Properties

		public EventHandler<ObservedPlayerDiedEventArgs> ObservedPlayerDied { get; set; }
		public Visualizable.IVisualizable Simulation { get => m_Simulation; }
		public Simulator.IScorer Scorer { get => m_Scorer; }

		public Visualization.Visualizer Visualizer {
			get => m_Visualizer;
			set {
				ChangeVisualizer(m_Visualizer, value);
				m_Visualizer = value;
			}
		}

		public Simulator.IPlayer ObservedPlayer {
			get => m_ObservedPlayer;
			set {
				ChangeObservedPlayer(m_ObservedPlayer, value);
				m_ObservedPlayer = value;
			}
		}

		public Visualization.IVisualizerObserver ObservingCamera {
			get => m_ObservingCamera;
			set {
				ChangeObservingCamera(m_ObservingCamera, value);
				m_ObservingCamera = value;
			}
		}

		#endregion

		public SimulatorAdapter (int width, int height, int seed) {
			this.m_Simulation = new Simulator.Simulation(width, height, seed) { FOOD_SPAWN_RATE = 0 };
			this.m_Scorer = new Simulator.SinglePlayerScorer();
			this.localPort = new Simulator.NormalPlayersPort() { ValuePerMove = 0f };

			localPort.Scorer = m_Scorer;
			m_Simulation.AddPlayerPort(localPort);
		}

		public void AddPlayer (Simulator.IPlayer player) {
			if (!localPort.Contains(player)) {
				localPort.AddPlayer(player);
			}
		}

		public void MakeStep () {
			m_Simulation.MakeStep();
		}


		public bool TryGetObservedPlayerScoreInfo (out (float score, float? multiplier) scoreInfo) {
			scoreInfo = (-1, null);

			if (m_Scorer is Simulator.SinglePlayerScorer singePlayerScorer) {
				if (singePlayerScorer.TryGetCurrentMultiplier(m_ObservedPlayer, out float multiplier)) {
					scoreInfo.multiplier = multiplier;
				}
			}

			if (m_Scorer.TryGetScore(m_ObservedPlayer, out float score)) {
				scoreInfo.score = score;
				return true;
			}

			return false;
		}

		private void ObservationFinishedHandler (object sender, EventArgs args) {
			OnObservedPlayerDied();
		}

		private void OnObservedPlayerDied () {
			var hasInfo = TryGetObservedPlayerScoreInfo(out var info);
			ObservedPlayerDied?.Invoke(this, new ObservedPlayerDiedEventArgs() {
				player = m_ObservedPlayer,
				score = (hasInfo) ? (float?)info.score : null,
			});
		}

		#region Properties update methods

		private void ChangeVisualizer (Visualization.Visualizer old, Visualization.Visualizer nw) {
			if (old != null) {
				Simulation.RemoveObserver(old);

				if (m_ObservingCamera != null) {
					old.RemoveObserver(m_ObservingCamera);
				}
			}

			if (nw == null) return;
			Simulation.AttachObserver(nw);

			if (m_ObservingCamera != null) {
				nw.AddObserver(m_ObservingCamera);
			}
		}

		private void ChangeObservedPlayer (Simulator.IPlayer old, Simulator.IPlayer nw) {
			if (nw == null) return;

			if (!localPort.Contains(nw)) {
				localPort.AddPlayer(nw);
			}

			if ( m_ObservingCamera != null && TryFindPlayerId(nw, out int id)) {
				m_ObservingCamera.ExpectedPlacementId = id;
			}
		}

		private bool TryFindPlayerId (Simulator.IPlayer player, out int id) {
			if (localPort.TryGetPlayerId(player, out id)) {
				return true;
			}

			if (networkPort != null) {
				return networkPort.TryGetPlayerId(player, out id);
			}

			id = -1;
			return false;
		}

		private void ChangeObservingCamera (Visualization.IVisualizerObserver old, Visualization.IVisualizerObserver nw) {
			if (old != null && m_Visualizer != null) {
				old.ObservationFinished -= ObservationFinishedHandler;
				m_Visualizer.RemoveObserver(old);
			}

			if (nw == null) return;
			nw.ObservationFinished -= ObservationFinishedHandler;
			nw.ObservationFinished += ObservationFinishedHandler;

			if (m_ObservedPlayer != null && TryFindPlayerId(m_ObservedPlayer, out int id)) {
				nw.ExpectedPlacementId = id;
			}

			if (m_Visualizer != null) {
				m_Visualizer.AddObserver(nw);
			}
		}

		#endregion

		public class ObservedPlayerDiedEventArgs : EventArgs {
			public Simulator.IPlayer player;
			public float? score;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Game {
	public class SimulatorAdapter {
		private Simulator.Simulation m_Simulation;
		private Visualization.Visualizer m_Visualizer;
		private Simulator.IPlayer m_ObservedPlayer;
		private Visualization.IVisualizerObserver m_ObservingCamera;

		private Simulator.IPlayersPort localPort;
		private Simulator.IPlayersPort networkPort = null;     // for future purpose

		#region Properties

		public Visualizable.IVisualizable Simulation { get => m_Simulation; }

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
			this.m_Simulation = new Simulator.Simulation(width, height, seed);
			this.localPort = new Simulator.NormalPlayersPort();

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
				m_Visualizer.RemoveObserver(old);
			}

			if (nw == null) return;

			if (m_ObservedPlayer != null && TryFindPlayerId(m_ObservedPlayer, out int id)) {
				nw.ExpectedPlacementId = id;
			}

			if (m_Visualizer != null) {
				m_Visualizer.AddObserver(nw);
			}
		}

		#endregion
	}
}

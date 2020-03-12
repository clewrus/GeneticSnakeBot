
using System.Collections.Generic;
using Simulator;
using UnityEngine;

namespace Visualization {
	public interface ISimulationObserver {
		void SimulationUpdateHandler(Simulation curSimulation, HashSet<(int id, Vector2Int? pos)> entities);
	}
}

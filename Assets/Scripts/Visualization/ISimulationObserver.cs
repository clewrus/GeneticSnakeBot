
using System.Collections.Generic;
using Simulator;
using UnityEngine;
using Visualizable;

namespace Visualization {
	public interface ISimulationObserver {
		void SimulationUpdateHandler(IVisualizable curSimulation, HashSet<(int id, Vector2Int? pos)> entities);
	}
}

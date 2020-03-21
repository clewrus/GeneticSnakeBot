
using System.Collections.Generic;
using Simulator;
using UnityEngine;
using Visualizable;

namespace Visualization {
	public interface ISimulationObserver {
		void SimulationUpdateHandler(IVisualizable curSimulation, IEnumerable<(int x, int y)> updatedPositions);
	}
}

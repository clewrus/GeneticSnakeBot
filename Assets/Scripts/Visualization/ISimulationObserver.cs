
using System.Collections.Generic;

namespace Visualization {
	interface ISimulationObserver {
		void SimulationUpdateHandler(HashSet<int> entitiesIds);
	}
}

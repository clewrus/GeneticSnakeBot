
using System.Collections.Generic;

namespace Visualization {
	public interface ISimulationObserver {
		void SimulationUpdateHandler(HashSet<int> entitiesIds);
	}
}

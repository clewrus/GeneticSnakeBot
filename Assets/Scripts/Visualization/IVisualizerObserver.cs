using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Visualization {
	public interface IVisualizerObserver {
		void PlacementChangedHandler (IEnumerable<Vector2Int> placement, bool exists, bool wasRemovedRecently);

		void Restart ();

		Vector2Int FieldSize { set; }
		int ExpectedPlacementId { get; set; }
	}
}

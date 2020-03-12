using Simulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualization;

namespace Visualizable {
	public interface IVisualizable {
		int Width { get; }
		int Height { get; }

		FieldItem[,] Field { get; }

		void AttachObserver (ISimulationObserver nwObserver);
		void RemoveObserver (ISimulationObserver oldObserver);
		void MakeStep ();
	}
}

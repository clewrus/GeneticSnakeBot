using System.Collections;
using System.Collections.Generic;
using Visualization;

namespace Simulator {
    public partial class Simulation {
        public List<ISimulationObserver> observers;

        public FieldProjector fieldProjector { get; private set; }
        public List<IPlayersPort> playersPorts { get; private set; }

        private FieldItem[,] field;
        private int nextEntityId = 0;

        public Simulation (int width, int height) {
            observers = new List<ISimulationObserver>();
            field = new FieldItem[width, height];
            fieldProjector = new FieldProjector(field);
            playersPorts = new List<IPlayersPort>();
        }

        public void AddPlayerPort (IPlayersPort nwPort) {
            if (playersPorts.Contains(nwPort)) return;

            playersPorts.Add(nwPort);
            nwPort.GetNextId = this.GetNextId;
        }

        public void AttachObserver (ISimulationObserver nwObserver) {
            if (observers.Contains(nwObserver)) return;
            observers.Add(nwObserver);
        }

        public void RemoveObserver (ISimulationObserver oldObserver) {
            if (!observers.Contains(oldObserver)) return;
            observers.Remove(oldObserver);
        }

        public void MakeStep () {
            
        }

        public int GetNextId () {
            return nextEntityId++;
        }
    }
}
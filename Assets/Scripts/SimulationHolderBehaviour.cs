
using UnityEngine;
using Simulation;

public class SimulationHolderBehaviour : MonoBehaviour {

	private Simulator simulator;

	[SerializeField] private int width = 10;
	[SerializeField] private int height = 10;

	[SerializeField] public IVisualizer visualizer;

	private void Start () {
		simulator = new Simulator(width, height);
	}

	private void Update () {
		simulator.MakeSimulationStep();
		visualizer.Visualize(simulator);
	}
}
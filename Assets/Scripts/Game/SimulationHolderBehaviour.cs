
using System.Collections;
using Assets.SnakeTiles;
using Game;
using Simulator;
using UnityEngine;
using Visualization;

public class SimulationHolderBehaviour : MonoBehaviour {

	[SerializeField] private int width = 100;
	[SerializeField] private int height = 100;

	[SerializeField] private GameObject[] observers = null;

	private SimulatorAdapter simulator;

	private void Start () {
		simulator = new SimulatorAdapter(width, height, 228);

		simulator.ObservingCamera = Camera.main.GetComponent<IVisualizerObserver>();
		//simulator.ObservedPlayer = FindObjectOfType<HumanPlayer>();
		simulator.Visualizer = FindObjectOfType<Visualizer>();

		for (int i = 0; i < 35; i++) {
			var nwBot = new PlayerMechanism.SimpleBot();
			simulator.AddPlayer(nwBot);
			simulator.ObservedPlayer = nwBot;
		}

		StartCoroutine(SimulationUpdater());
	}

	private IEnumerator SimulationUpdater () {
		while (true) {
			yield return new WaitForSeconds(0.2f);
			simulator.MakeStep();
		}
	}
}
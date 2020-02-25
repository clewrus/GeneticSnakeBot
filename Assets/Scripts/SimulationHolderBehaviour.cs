
using System.Collections;
using Simulator;
using UnityEngine;

public class SimulationHolderBehaviour : MonoBehaviour {

	[SerializeField] private int width = 100;
	[SerializeField] private int height = 100;

	private Simulation curSimulation;
	private IPlayersPort port;
	

	private void Start () {
		curSimulation = new Simulation(width, height);
		port = new NormalPlayersPort();
		curSimulation.AddPlayerPort(port);


		var player = GameObject.FindObjectOfType<HumanPlayer>();
		port.AddPlayer(player, false);

		StartCoroutine(SimulationUpdater());
	}

	private IEnumerator SimulationUpdater () {
		while (true) {
			yield return new WaitForSeconds(2);
			curSimulation.MakeStep();
		}
	}

	private void Update () {

	}
}
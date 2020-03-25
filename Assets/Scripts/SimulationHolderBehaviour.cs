
using System.Collections;
using Assets.SnakeTiles;
using Simulator;
using UnityEngine;
using Visualization;

public class SimulationHolderBehaviour : MonoBehaviour {

	[SerializeField] private int width = 100;
	[SerializeField] private int height = 100;

	[SerializeField] private GameObject[] observers = null; 

	private Simulation curSimulation;
	private IPlayersPort port;

	private void Start () {
		curSimulation = new Simulation(width, height, 228);
		port = new NormalPlayersPort();
		curSimulation.AddPlayerPort(port);

		var player = GameObject.FindObjectOfType<HumanPlayer>();
		port.AddPlayer(player, true);

		SubscribeObservers();
		curSimulation.MakeStep();

		foreach (var obs in observers) {
			if (obs.GetComponent<ISimulationObserver>() is Visualizer visualizer) {
				var snakeCamera = Camera.main.GetComponent<SnakeCamera>();
				snakeCamera.ExpectedPlacementId = player.KnownId.Value;
				snakeCamera.FieldSize = new Vector2Int(width, height);
				visualizer.AddObserver(snakeCamera);
			}
		}

		StartCoroutine(SimulationUpdater());
	}

	private void SubscribeObservers () {
		foreach (var observer in observers) {
			var obs = observer.GetComponent<ISimulationObserver>();
			if (obs == null) continue;

			curSimulation.AttachObserver(obs);
		}
	}

	private IEnumerator SimulationUpdater () {
		while (true) {
			yield return new WaitForSeconds(0.8f);
			curSimulation.MakeStep();
		}
	}

	private void Update () {

	}
}
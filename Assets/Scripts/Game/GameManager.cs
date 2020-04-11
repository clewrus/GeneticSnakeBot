using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game {
	[RequireComponent(typeof(CoroutineBasedDispathcer))]
	public class GameManager : MonoBehaviour {
		[SerializeField] private GameObject menuCamera = null;
		[SerializeField] private GameObject gameCamera = null;
		[Space]
		[SerializeField] private GameObject menuCanvas = null;
		[SerializeField] private GameObject startMenu = null;
		[SerializeField] private GameObject loadingMenu = null;
		[Space]
		[SerializeField] private Visualization.Visualizer visualizer = null;

		[Space]
		[SerializeField] private int width = 10;
		[SerializeField] private int height = 10;
		[SerializeField] private int numberOfBots = 1;

		private SimulatorAdapter currentSimulation;

		private void Start () {
			SetCameraComponentState(gameCamera, enabled: false);
			SetCameraComponentState(menuCamera, enabled: true);

			startMenu?.SetActive(true);
			loadingMenu?.SetActive(false);
		}

		public void PlayButtonClickedHandler () {
			menuCanvas?.SetActive(true);
			startMenu?.SetActive(false);
			loadingMenu?.SetActive(true);

			var dispatcher = GetComponent<CoroutineBasedDispathcer>();

			int seed = Random.Range(0, 1000000);
			dispatcher.RegistrateTask(
				function: () => new SimulatorAdapter(width, height, seed),
				callback: SimulationInitializedHandler
			);
		}

		private void SimulationInitializedHandler (SimulatorAdapter simulation) {
			InitializeSimulation(simulation);

			menuCanvas?.SetActive(false);
			SetCameraComponentState(menuCamera, enabled: false);
			SetCameraComponentState(gameCamera, enabled: true);

			currentSimulation = simulation;
		}

		private void InitializeSimulation (SimulatorAdapter simulation) {
			simulation.Visualizer = visualizer;

			var observingCamera = gameCamera.GetComponent<Visualization.IVisualizerObserver>();
			Debug.Assert(observingCamera != null, "GameCamera must have IVisualizerObserver based component.");
			simulation.ObservingCamera = observingCamera;

			var validPlayer = SelectValidPlayer();
			if (validPlayer is IHumanPlayer humanPlayer) {
				humanPlayer.DirectionSelected += DirectionSelectedHandler;
			}

			simulation.ObservedPlayer = validPlayer;
			AddBots(simulation);

			simulation.MakeStep();
		}

		private void DirectionSelectedHandler (object sender, System.EventArgs eventArgs) {
			currentSimulation.MakeStep();
		}

		private void AddBots (SimulatorAdapter simulator) {
			for (int i = 0; i < numberOfBots; i++) {
				var nwBot = new PlayerMechanism.SimpleBot();
				simulator.AddPlayer(nwBot);
			}
		}

		private Simulator.IPlayer SelectValidPlayer () {
			return GetComponent<PcPlayer>();
		}

		private void SetCameraComponentState (GameObject cameraObj, bool enabled) {
			if (cameraObj == null) return;
			var cameraComponent = cameraObj.GetComponent<Camera>();

			if (cameraComponent == null) return;
			cameraComponent.enabled = enabled;
		}
	}
}


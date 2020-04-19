using Game.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game {
	[RequireComponent(typeof(CoroutineBasedDispathcer))]
	public class GameManager : MonoBehaviour {
		[SerializeField] private GameObject gameCamera = null;
		[Space]
		[SerializeField] private GameObject menuCanvas = null;
		[SerializeField] private GameObject startMenu = null;
		[SerializeField] private GameObject loadingMenu = null;
		[SerializeField] private GameObject pauseMenu = null;
		[Space]
		[SerializeField] private GameObject standartGameInterfase = null;
		[SerializeField] private GameObject scoreLable = null;
		[Space]
		[SerializeField] private Visualization.Visualizer visualizer = null;

		[Space]
		[SerializeField] private int width = 10;
		[SerializeField] private int height = 10;
		[SerializeField] private int numberOfBots = 1;

		private SimulatorAdapter currentSimulation;
		private Coroutine scoreUpdateCoroutine;

		private bool isPause;

		private void Start () {
			SetCameraComponentState(gameCamera, enabled: true);

			menuCanvas?.SetActive(true);
			startMenu?.SetActive(true);

			loadingMenu?.SetActive(false);
			standartGameInterfase?.SetActive(false);
			pauseMenu?.SetActive(false);
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

			startMenu?.SetActive(false);
			loadingMenu?.SetActive(false);
			standartGameInterfase?.SetActive(true);

			currentSimulation = simulation;

			scoreUpdateCoroutine = StartCoroutine(ScoreUpdateCoroutine());
		}

		private IEnumerator ScoreUpdateCoroutine () {
			if (scoreLable == null) yield break;

			ScoreUpdater scoreUpdater = null;
			while (true) {
				if (scoreUpdater == null) {
					scoreUpdater = scoreLable.GetComponent<ScoreUpdater>();
				}

				if (scoreUpdater != null && currentSimulation != null &&
					currentSimulation.TryGetObservedPlayerScoreInfo(out var scoreInfo))
				{
					scoreUpdater.Value = (int)scoreInfo.score;
				}

				yield return null;
			}
		}

		private void InitializeSimulation (SimulatorAdapter simulation) {
			simulation.Visualizer = visualizer;

			var observingCamera = gameCamera.GetComponent<Visualization.IVisualizerObserver>();
			observingCamera.Restart();
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

		public void GameExitButtonPressed () {
			Application.Quit();
		}

		public void SimulationExitButtonPressed () {
			isPause = false;
			StopCoroutine(scoreUpdateCoroutine);

			if (currentSimulation != null) {
				currentSimulation.Visualizer.Clear();
				
				if (currentSimulation.ObservedPlayer is IHumanPlayer humanPlayer) {
					humanPlayer.DirectionSelected -= DirectionSelectedHandler;
				}

				currentSimulation.Visualizer = null;
				currentSimulation.ObservedPlayer = null;
				currentSimulation.ObservingCamera = null;
				currentSimulation = null;
			}

			menuCanvas?.SetActive(true);
			startMenu?.SetActive(true);

			loadingMenu?.SetActive(false);
			standartGameInterfase?.SetActive(false);
			pauseMenu?.SetActive(false);


		}

		public void PauseExitButtonPressed () {
			isPause = false;
			pauseMenu?.SetActive(false);
		}

		public void PauseButtonPressedHandler () {
			isPause = true;
			pauseMenu?.SetActive(true);
		}

		private void DirectionSelectedHandler (object sender, System.EventArgs eventArgs) {
			if (!isPause) {
				currentSimulation.MakeStep();
			}
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


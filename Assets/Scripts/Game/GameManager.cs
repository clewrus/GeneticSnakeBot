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
		[SerializeField] private GameObject endOfSimulationMenu = null;
		[Space]
		[SerializeField] private GameObject standartGameInterfase = null;
		[SerializeField] private GameObject arenaGameInterfase = null;
		[SerializeField] private GameObject scoreLable = null;
		[Space]
		[SerializeField] private Visualization.Visualizer visualizer = null;

		[Space]
		[SerializeField] private int width = 10;
		[SerializeField] private int height = 10;
		[SerializeField] private int numberOfBots = 1;
		[SerializeField] private float GameDuration = 60;

		private SimulatorAdapter currentSimulation;
		private Coroutine scoreUpdateCoroutine;
		private Coroutine countdownCoroutine;

		private bool isPause;

		private void Start () {
			SetCameraComponentState(gameCamera, enabled: true);

			TurnOffAllMenues();

			menuCanvas?.SetActive(true);
			startMenu?.SetActive(true);
		}

		public void PlayButtonClickedHandler () {
			TurnOffAllMenues();

			menuCanvas?.SetActive(true);
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

			TurnOffAllMenues();
			menuCanvas?.SetActive(true);
			standartGameInterfase?.SetActive(true);
			arenaGameInterfase?.SetActive(true);

			currentSimulation = simulation;
			scoreUpdateCoroutine = StartCoroutine(ScoreUpdateCoroutine());
			countdownCoroutine = StartCoroutine(CountdownCoroutine());
		}

		private IEnumerator ScoreUpdateCoroutine () {
			ArenaInterfaceManager arenaManager = null;
			ScoreUpdater scoreUpdater = null;

			while (true) {
				if (currentSimulation != null && currentSimulation.TryGetObservedPlayerScoreInfo(out var scoreInfo)) {
					if (scoreLable != null) {
						if (scoreUpdater == null) {
							scoreUpdater = scoreLable.GetComponent<ScoreUpdater>();
						}
						if (scoreUpdater != null) {
							scoreUpdater.Value = (int)scoreInfo.score;
						}
					}

					if (arenaGameInterfase != null) {
						if (arenaManager == null) {
							arenaManager = arenaGameInterfase.GetComponent<ArenaInterfaceManager>();
						}
						if (arenaManager != null) {
							arenaManager.CurMultiplier = (scoreInfo.multiplier.HasValue) ? scoreInfo.multiplier.Value : 1;
						}
					}
				}

				yield return null;
			}
		}

		private IEnumerator CountdownCoroutine () {
			ArenaInterfaceManager arenaManager = null;
			float leftTime = GameDuration;

			while (leftTime > 0) {
				if (!isPause) {
					leftTime -= Time.deltaTime;
				}

				if (arenaGameInterfase != null) {
					if (arenaManager == null) {
						arenaManager = arenaGameInterfase.GetComponent<ArenaInterfaceManager>();
						if (arenaManager != null) arenaManager.MaxTime = GameDuration;
					}
					if (arenaManager != null) {
						arenaManager.CurTime = Mathf.Max(0, leftTime);
					}
				}
				
				yield return null;
			}

			OnCountdownEnded();
		}

		private void OnCountdownEnded () {
			TurnOnEndOfGameMenu();
		}

		private void InitializeSimulation (SimulatorAdapter simulation) {
			simulation.Visualizer = visualizer;

			var observingCamera = gameCamera.GetComponent<Visualization.IVisualizerObserver>();
			Debug.Assert(observingCamera != null, "GameCamera must have IVisualizerObserver based component.");

			observingCamera.Restart();
			simulation.ObservingCamera = observingCamera;

			var validPlayer = SelectValidPlayer();
			if (validPlayer is IHumanPlayer humanPlayer) {
				humanPlayer.DirectionSelected += DirectionSelectedHandler;
			}

			simulation.ObservedPlayer = validPlayer;
			AddBots(simulation);

			simulation.ObservedPlayerDied -= ObservedPlayerDiedHandler;
			simulation.ObservedPlayerDied += ObservedPlayerDiedHandler;

			simulation.MakeStep();
		}

		private void ObservedPlayerDiedHandler (object sender, SimulatorAdapter.ObservedPlayerDiedEventArgs args) {
			TurnOnEndOfGameMenu();
		}

		private void TurnOnEndOfGameMenu () {
			isPause = true;

			if (countdownCoroutine != null) {
				StopCoroutine(countdownCoroutine);
				countdownCoroutine = null;
			}

			TurnOffAllMenues();

			menuCanvas?.SetActive(true);
			standartGameInterfase?.SetActive(true);
			arenaGameInterfase?.SetActive(true);
			endOfSimulationMenu?.SetActive(true);
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

			TurnOffAllMenues();

			menuCanvas?.SetActive(true);
			startMenu?.SetActive(true);
		}

		public void PlayAgainButtonPressed () {
			TurnOffAllMenues();

			SimulationExitButtonPressed();
			PlayButtonClickedHandler();
		}

		public void TurnOffAllMenues () {
			startMenu?.SetActive(false);
			loadingMenu?.SetActive(false);
			pauseMenu?.SetActive(false);
			endOfSimulationMenu?.SetActive(false);
			standartGameInterfase?.SetActive(false);
			arenaGameInterfase?.SetActive(false);
		}

		public void PauseExitButtonPressed () {
			isPause = false;
			pauseMenu?.SetActive(false);
		}

		public void PauseButtonPressedHandler () {
			if (endOfSimulationMenu != null && endOfSimulationMenu.activeSelf) return;

			if (isPause) {
				PauseExitButtonPressed();
			} else {
				isPause = true;

				TurnOffAllMenues();
				menuCanvas?.SetActive(true);
				standartGameInterfase?.SetActive(true);
				arenaGameInterfase?.SetActive(true);
				pauseMenu?.SetActive(true);
			}
			
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
			Simulator.IPlayer selectedPlayer = GetComponent<PcPlayer>();

			#if UNITY_ANDROID
			selectedPlayer = GetComponent<ScreenButtonsPlayer>();
			#endif

			return selectedPlayer;
		}

		private void SetCameraComponentState (GameObject cameraObj, bool enabled) {
			if (cameraObj == null) return;
			var cameraComponent = cameraObj.GetComponent<Camera>();

			if (cameraComponent == null) return;
			cameraComponent.enabled = enabled;
		}
	}
}


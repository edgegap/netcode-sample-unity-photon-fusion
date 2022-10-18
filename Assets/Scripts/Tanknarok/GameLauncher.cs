using Fusion;
using Fusion.Sockets;
using FusionExamples.FusionHelpers;
using FusionExamples.UIHelpers;
using System;
using System.Collections;
using Tanknarok.UI;
using TMPro;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// App entry point and main UI flow management.
	/// </summary>
	public class GameLauncher : MonoBehaviour
	{
		[SerializeField] private GameManager _gameManagerPrefab;
		[SerializeField] private Player _playerPrefab;
		[SerializeField] private TMP_InputField _room;
		[SerializeField] private TextMeshProUGUI _progress;
		[SerializeField] private Panel _uiCurtain;
		[SerializeField] private Panel _uiStart;
		[SerializeField] private Panel _uiProgress;
		[SerializeField] private Panel _uiRoom;
		[SerializeField] private GameObject _uiGame;
		[SerializeField] private GameObject _uiMatchStarting;

		private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Disconnected;
		private GameMode _gameMode;

		private void Awake()
		{
			DontDestroyOnLoad(this);
		}

		private void Start()
		{
			OnConnectionStatusUpdate(null, FusionLauncher.ConnectionStatus.Disconnected, "");

			if (EdgegapManager.IsServer())
            {
				var getPortAndStartServer = EdgegapAPIInterface.GetPublicIpAndPortFromServer((ip, port) =>
                {
					var serverAddress = NetAddress.CreateFromIpPort(ip, port);
					LaunchGame(GameMode.Server, EdgegapManager.EdgegapRoomCode, serverAddress);

				} );

				StartCoroutine(getPortAndStartServer);
			}
		}

		private void Update()
		{
			if (_uiProgress.isShowing)
			{
				if (Input.GetKeyUp(KeyCode.Escape))
				{
					NetworkRunner runner = FindObjectOfType<NetworkRunner>();
					if (runner != null && !runner.IsShutdown)
					{
						// Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
						runner.Shutdown(false);
					}
				}
				UpdateUI();
			}
		}

		// What mode to play - Called from the start menu
		public void OnHostOptions()
		{
			SetGameMode(GameMode.Host);
		}

		public void OnJoinOptions()
		{
			SetGameMode(GameMode.Client);
		}

		public void OnSharedOptions()
		{
			SetGameMode(GameMode.Shared);
		}

		public void OnEdgegapOptions()
		{
			EdgegapManager.EdgegapPreServerMode = true;
			SetGameMode(GameMode.AutoHostOrClient);
		}

		private void SetGameMode(GameMode gamemode)
		{
			_gameMode = gamemode;
			if (GateUI(_uiStart))
				_uiRoom.SetVisible(true);
		}

		public void OnEnterRoom()
		{
			if (GateUI(_uiRoom))
			{
				LaunchGame(_gameMode, _room.text);
			}
		}

		public void LaunchGame(GameMode gameMode, string roomName, NetAddress? serverAddress = null)
        {
			Debug.Log("Launching in mode " + gameMode + " and room " + roomName);

			_gameMode = gameMode;

			LevelManager lm = FindObjectOfType<LevelManager>();
			if(lm.launcher == null)
            {
				lm.launcher = FindObjectOfType<FusionLauncher>();
				if(lm.launcher == null)
                {
					lm.launcher = new GameObject("Launcher").AddComponent<FusionLauncher>();
				}

			}

			lm.launcher.Launch(_gameMode, roomName, lm, OnConnectionStatusUpdate, OnSpawnWorld, OnSpawnPlayer, OnDespawnPlayer, serverAddress);
		}

		/// <summary>
		/// Call this method from button events to close the current UI panel and check the return value to decide
		/// if it's ok to proceed with handling the button events. Prevents double-actions and makes sure UI panels are closed. 
		/// </summary>
		/// <param name="ui">Currently visible UI that should be closed</param>
		/// <returns>True if UI is in fact visible and action should proceed</returns>
		private bool GateUI(Panel ui)
		{
			if (!ui.isShowing)
				return false;
			ui.SetVisible(false);
			return true;
		}

		private void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
		{
			if (!this)
				return;

			Debug.Log(status);

			if(EdgegapManager.TransferingToEdgegapServer)
            {
				if (status == FusionLauncher.ConnectionStatus.Disconnected)
				{
					// launch game again with edgegap server room code
					var launchAfterDelay = RunAfterTime(0.2f, () => LaunchGame(GameMode.Client, EdgegapManager.EdgegapRoomCode));
					StartCoroutine(launchAfterDelay);
				}
				else if( status == FusionLauncher.ConnectionStatus.Connected)
                {
					EdgegapManager.TransferingToEdgegapServer = false;
					EdgegapManager.EdgegapPreServerMode = false;
				}

				return;
			}

			if (status != _status)
			{
				switch (status)
				{
					case FusionLauncher.ConnectionStatus.Disconnected:
						ErrorBox.Show("Disconnected!", reason, () => { });
						break;
					case FusionLauncher.ConnectionStatus.Failed:
						ErrorBox.Show("Error!", reason, () => { });
						break;
				}
			}

			_status = status;
			UpdateUI();
		}

		private void OnSpawnWorld(NetworkRunner runner)
		{
			Debug.Log("Spawning GameManager");
			runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity, null, InitNetworkState);
			void InitNetworkState(NetworkRunner runner, NetworkObject world)
			{
				world.transform.parent = transform;
			}
		}

		private void OnSpawnPlayer(NetworkRunner runner, PlayerRef playerref)
		{
			if (GameManager.playState == GameManager.PlayState.LEVEL)
			{
				Debug.Log("Not Spawning Player - game has already started");
				return;
			}
			Debug.Log($"Spawning tank for player {playerref}");
			runner.Spawn(_playerPrefab, Vector3.zero, Quaternion.identity, playerref, InitNetworkState);
			void InitNetworkState(NetworkRunner runner, NetworkObject networkObject)
			{
				Player player = networkObject.gameObject.GetComponent<Player>();
				Debug.Log($"Initializing player {player.playerID}");
				player.InitNetworkState(GameManager.MAX_LIVES);
			}
		}

		private void OnDespawnPlayer(NetworkRunner runner, PlayerRef playerref)
		{
			Debug.Log($"Despawning Player {playerref}");
			Player player = PlayerManager.Get(playerref);
			player.TriggerDespawn();
		}

		bool WaitingForEdgegap = false;

		private void UpdateUI()
		{
			bool intro = false;
			bool progress = false;
			bool running = false;

			switch (_status)
			{
				case FusionLauncher.ConnectionStatus.Disconnected:
					_progress.text = "Disconnected!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Failed:
					_progress.text = "Failed!";
					intro = true;
					break;
				case FusionLauncher.ConnectionStatus.Connecting:
					_progress.text = "Connecting";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Connected:
					_progress.text = "Connected";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loading:
					_progress.text = "Loading";
					progress = true;
					break;
				case FusionLauncher.ConnectionStatus.Loaded:
					running = true;
					break;
			}

            if (WaitingForEdgegap)
			{
				_progress.text = "Starting match server";
				progress = true;
			}

			_uiCurtain.SetVisible(!running);
			_uiStart.SetVisible(intro);
			_uiProgress.SetVisible(progress);
			_uiGame.SetActive(running);
			_uiMatchStarting.SetActive(false);

			if (intro)
				MusicPlayer.instance.SetLowPassTranstionDirection( -1f);
		}

		public void ShowMatchStartingUI()
        {
			_uiMatchStarting.SetActive(true);
		}

		IEnumerator RunAfterTime(float timeInSeconds, Action action)
        {
			yield return new WaitForSeconds(timeInSeconds);
			action();
        }
	}
}
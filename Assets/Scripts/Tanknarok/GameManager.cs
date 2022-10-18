using UnityEngine;
using Fusion;
using System.Linq;

namespace FusionExamples.Tanknarok
{
	public class GameManager : NetworkBehaviour, IStateAuthorityChanged
	{
		public enum PlayState
		{
			LOBBY,
			LEVEL,
			TRANSITION
		}

		[Networked]
		private int networkedWinningPlayerIndex { get; set; } = -1;

		[Networked]
		private PlayState networkedPlayState { get; set; }

		public static PlayState playState
		{
			get => (instance != null && instance.Object != null && instance.Object.IsValid) ?  instance.networkedPlayState : PlayState.LOBBY;
			set
			{
				if (instance != null && instance.Object != null && instance.Object.IsValid)
					instance.networkedPlayState = value;
			}
		}

		public static int WinningPlayerIndex
		{
			get => (instance != null && instance.Object != null && instance.Object.IsValid) ? instance.networkedWinningPlayerIndex : -1;
			set
			{
				if (instance != null && instance.Object != null && instance.Object.IsValid)
					instance.networkedWinningPlayerIndex = value;
			}
		}

		public const byte MAX_LIVES = 3;
		public const byte MAX_SCORE = 3;

		private ScoreManager _scoreManager;
		private LevelManager _levelManager;

		private bool _restart;

		public static GameManager instance { get; private set; }

		[Rpc(sources:RpcSources.All, RpcTargets.StateAuthority)]
		public void RpcShareIpAddress(string ipAddress, RpcInfo info = default)
		{
			if (info.Source == PlayerRef.None)
			{
				// source will be None for local player
				Player.local.ipAddress = ipAddress;
				return;
			}

			foreach (var player in PlayerManager.allPlayers)
			{
				if (player.playerID == info.Source.PlayerId)
                {
					player.ipAddress = ipAddress;
					break;
                }
			}
        }

		[Rpc(sources: RpcSources.StateAuthority, RpcTargets.All)]
		public void RpcOnEdgegapServerReady(string roomCode)
        {
			EdgegapManager.EdgegapRoomCode = roomCode;
			EdgegapManager.TransferingToEdgegapServer = true;
			_levelManager.LoadTransferScene();

		}

		public override void Spawned()
		{
			// We only want one GameManager
			if (instance)
				Runner.Despawn(Object); // TODO: I've never seen this happen - do we really need this check?
			else
			{
				instance = this;

				// Find managers and UI
				_levelManager = FindObjectOfType<LevelManager>(true);

				if (Object.HasStateAuthority)
				{
					LoadLevel(-1, -1);
				}
				else if (playState == PlayState.LEVEL)
				{
					Debug.Log("Rejecting Player, game is already running!");
					_restart = true;
				}
			}
		}

		public void OnTankDeath()
		{
			if (playState != PlayState.LOBBY)
			{
				int playersleft = PlayerManager.PlayersAlive();
				Debug.Log($"Someone died - {playersleft} left");
				if (playersleft <= 1)
				{
					Player lastPlayerStanding = playersleft == 0 ? null : PlayerManager.GetFirstAlivePlayer();
					// if there is only one player, who died from a laser (e.g.) we don't award scores. 
					if (lastPlayerStanding != null)
					{
						int winningPlayerIndex = lastPlayerStanding.playerID;
						int nextLevelIndex = _levelManager.GetRandomLevelIndex();
						byte winningPlayerScore = (byte)(lastPlayerStanding.score + 1);
						if (winningPlayerIndex >= 0)
						{
							Player winner = PlayerManager.GetPlayerFromID(winningPlayerIndex);
							if (winner.Object.HasStateAuthority)
								winner.score = winningPlayerScore;
							if (winningPlayerScore >= MAX_SCORE)
								nextLevelIndex = -1;
						}
						LoadLevel(nextLevelIndex, winningPlayerIndex);
					}
				}
			}
		}

		public void Restart(ShutdownReason shutdownReason)
		{
			if (!Runner.IsShutdown)
			{
				// Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher
				Runner.Shutdown(false,shutdownReason);
				instance = null;
				_restart = false;
			}
		}

		public const ShutdownReason ShutdownReason_GameAlreadyRunning = (ShutdownReason)100;

		private void Update()
		{
			if (_restart || Input.GetKeyDown(KeyCode.Escape))
			{
				Restart( _restart ? ShutdownReason_GameAlreadyRunning : ShutdownReason.Ok);
				return;
			}
			PlayerManager.HandleNewPlayers();

			if (EdgegapManager.IsServer())
			{
				if (GameManager.playState == GameManager.PlayState.LOBBY)
				{
					// if we're in the lobby and all players have joined, start the match
					if (PlayerManager.allPlayers.Count >= EdgegapManager.ExpectedPlayerCount)
					{
						OnAllPlayersReady();
					}
				}

				if(Time.time > 60 && PlayerManager.allPlayers.Count == 0 
					&& GameManager.playState != GameManager.PlayState.TRANSITION)
                {
					// if it's been over a minute and there's no one here, stop the server
					Debug.LogWarning("Shutting Down");
					StartCoroutine(EdgegapAPIInterface.StopDeploymentFromServer());
					GameManager.playState = PlayState.TRANSITION;
				}
			}
		}

		private void ResetStats()
		{
			for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
			{
				Debug.Log($"Resetting player {i} stats to lives={MAX_LIVES}");
				PlayerManager.allPlayers[i].lives = MAX_LIVES;
				PlayerManager.allPlayers[i].score = 0;
			}
		}

		private void ResetLives()
		{
			for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
			{
				Debug.Log($"Resetting player {i} lives to {MAX_LIVES}");
				PlayerManager.allPlayers[i].lives = MAX_LIVES;
			}
		}

		// Transition from lobby to level
		public void OnAllPlayersReady()
		{
			Debug.Log("All players are ready");
			if (playState != PlayState.LOBBY)
				return;

			// Reset stats and transition to level.
			ResetStats();

			// close and hide the session from matchmaking / lists. this demo does not allow late join.
			Runner.SessionInfo.IsOpen = false;
			Runner.SessionInfo.IsVisible = false;

			if (EdgegapManager.EdgegapPreServerMode)
			{
				//if host, spin up new edgegap server
				//once server is ready, connect to it

				FindObjectOfType<GameLauncher>().ShowMatchStartingUI();

				if (Object.HasStateAuthority)
				{
					string[] ips = PlayerManager.allPlayers.Select(p => p.ipAddress).ToArray();
					StartCoroutine(EdgegapManager.Instance.Deploy(ips, RpcOnEdgegapServerReady));
				}
			}
			else
			{
				LoadLevel(_levelManager.GetRandomLevelIndex(), -1);
			}

		}
		
		private void LoadLevel(int nextLevelIndex, int winningPlayerIndex)
		{
			if (!Object.HasStateAuthority)
				return;

			// Reset lives and transition to level
			ResetLives();

			// Reset players ready state so we don't launch immediately
			for (int i = 0; i < PlayerManager.allPlayers.Count; i++)
				PlayerManager.allPlayers[i].ResetReady();

			// Start transition
			WinningPlayerIndex = winningPlayerIndex;

			_levelManager.LoadLevel(nextLevelIndex);
		}

		public void StateAuthorityChanged()
		{
			Debug.Log($"State Authority of GameManager changed: {Object.StateAuthority}");
		}
	}
}
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using FusionExamples.Tanknarok;

namespace FusionExamples.FusionHelpers
{
	/// <summary>
	/// Small helper that provides a simple world/player pattern for launching Fusion
	/// </summary>
	public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
	{
		private NetworkRunner _runner;
		private Action<NetworkRunner, ConnectionStatus, string> _connectionCallback;
		private ConnectionStatus _status;
		private FusionObjectPoolRoot _pool;
		private Action<NetworkRunner> _spawnWorldCallback;
		private Action<NetworkRunner,PlayerRef> _spawnPlayerCallback;
		private Action<NetworkRunner,PlayerRef> _despawnPlayerCallback;

		public enum ConnectionStatus
		{
			Disconnected,
			Connecting,
			Failed,
			Connected,
			Loading,
			Loaded
		}

		public async void Launch(GameMode mode, string room,
			INetworkSceneManager sceneLoader,
			Action<NetworkRunner, ConnectionStatus, string> onConnect,
			Action<NetworkRunner> onSpawnWorld,
			Action<NetworkRunner, PlayerRef> onSpawnPlayer,
			Action<NetworkRunner, PlayerRef> onDespawnPlayer,
			NetAddress? serverAddress = null)
		{
			_connectionCallback = onConnect;
			_spawnWorldCallback = onSpawnWorld;
			_spawnPlayerCallback = onSpawnPlayer;
			_despawnPlayerCallback = onDespawnPlayer;

			SetConnectionStatus(ConnectionStatus.Connecting, "");

			DontDestroyOnLoad(gameObject);

			if (_runner == null)
				_runner = gameObject.AddComponent<NetworkRunner>();
			_runner.name = name;
			_runner.ProvideInput = mode != GameMode.Server;

			if (_pool == null)
				_pool = gameObject.AddComponent<FusionObjectPoolRoot>();


			var startArgs = new StartGameArgs()
			{
				GameMode = mode,
				SessionName = room,
				ObjectPool = _pool,
				SceneManager = sceneLoader
			};


			if (mode == GameMode.Server && serverAddress != null)
			{
				Debug.Log("Using specific address " + serverAddress);
				startArgs.Address = NetAddress.Any(5050);
				startArgs.CustomPublicAddress = serverAddress;
			}

			var result = await _runner.StartGame(startArgs);


			// if game is started and this is a server, manually trigger spawnworld
			if (result.Ok && mode == GameMode.Server)
			{
				Debug.Log($"Runner Start DONE"); 
				if (_spawnWorldCallback != null)
				{
					_spawnWorldCallback(_runner);
					_spawnWorldCallback = null;
				}

			}
		}


		public void SetConnectionStatus(ConnectionStatus status, string message)
		{
			_status = status;
			if (_connectionCallback != null)
				_connectionCallback(_runner, status, message);
		}

		public void OnInput(NetworkRunner runner, NetworkInput input)
		{
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
		{
		}

		
		public void OnConnectedToServer(NetworkRunner runner)
		{
			Debug.Log("Connected to server");
			if (runner.GameMode == GameMode.Shared)
			{
				Debug.Log("Shared Mode - Spawning Player");
				InstantiatePlayer(runner, runner.LocalPlayer);
			}
			SetConnectionStatus(ConnectionStatus.Connected, "");
		}

		public void OnDisconnectedFromServer(NetworkRunner runner)
		{
			Debug.Log("Disconnected from server");
			SetConnectionStatus(ConnectionStatus.Disconnected, "");
		}

		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
		{
			request.Accept();
		}

		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
		{
			Debug.Log($"Connect failed {reason}");
			SetConnectionStatus(ConnectionStatus.Failed, reason.ToString());
		}

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			if (runner.IsServer)
			{
				Debug.Log("Hosted Mode - Spawning Player");
				InstantiatePlayer(runner, player);
			}
//			SetConnectionStatus(ConnectionStatus.Connected, "");
		}

		private void InstantiatePlayer(NetworkRunner runner, PlayerRef playerref)
		{
			if (_spawnWorldCallback!=null && (runner.IsServer || runner.IsSharedModeMasterClient) )
			{
				_spawnWorldCallback(runner);
				_spawnWorldCallback = null;
			}

			_spawnPlayerCallback(runner, playerref);
		}

		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
			Debug.Log("Player Left");
			_despawnPlayerCallback(runner, player);

			SetConnectionStatus(_status, "Player Left");
		}

		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }

		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
			Debug.Log("OnShutdown");
			string message = "";
			switch (shutdownReason)
			{
				case GameManager.ShutdownReason_GameAlreadyRunning:
					message = "Game in this room already started!";
					break;
				case ShutdownReason.IncompatibleConfiguration:
					message = "This room already exist in a different game mode!";
					break;
				case ShutdownReason.Ok:
					message = "User terminated network session!"; 
					break;
				case ShutdownReason.Error:
					message = "Unknown network error!";
					break;
				case ShutdownReason.ServerInRoom:
					message = "There is already a server/host in this room";
					break;
				case ShutdownReason.DisconnectedByPluginLogic:
					message = "The Photon server plugin terminated the network session!";
					break;
				default:
					message = shutdownReason.ToString();
					break;
			}
			SetConnectionStatus(ConnectionStatus.Disconnected, message);

			// TODO: This cleanup should be handled by the ClearPools call below, but currently Fusion is not returning pooled objects on shutdown, so...
			// Destroy all NOs
			NetworkObject[] nos = FindObjectsOfType<NetworkObject>();
			for (int i = 0; i < nos.Length; i++)
				Destroy(nos[i].gameObject);
			
			// Clear all the player registries
			// TODO: This does not belong in here
			//PlayerManager.ResetPlayerManager();

			// Reset the object pools
			_pool.ClearPools();
            
      if(_runner!=null && _runner.gameObject)
	    	Destroy(_runner.gameObject);
		}
	}
}
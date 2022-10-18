using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// Handle player input by responding to Fusion input polling, filling an input struct and then working with
	/// that input struct in the Fusion Simulation loop.
	/// </summary>
	public class InputController : NetworkBehaviour, INetworkRunnerCallbacks
	{
		[SerializeField] private LayerMask _mouseRayMask;

		public static bool fetchInput = true;
		public bool ToggleReady { get; set; }

		private Player _player;
		private NetworkInputData _frameworkInput = new NetworkInputData();
		private Vector2 _moveDelta;
		private Vector2 _aimDelta;
		private Vector2 _leftPos;
		private Vector2 _leftDown;
		private Vector2 _rightPos;
		private Vector2 _rightDown;
		private bool _leftTouchWasDown;
		private bool _rightTouchWasDown;
		private bool _primaryFire;
		private bool _secondaryFire;

		private MobileInput _mobileInput;

		/// <summary>
		/// Hook up to the Fusion callbacks so we can handle the input polling
		/// </summary>
		public override void Spawned()
		{
			_mobileInput = FindObjectOfType<MobileInput>(true);
			_player = GetComponent<Player>();
			// Technically, it does not really matter which InputController fills the input structure, since the actual data will only be sent to the one that does have authority,
			// but in the name of clarity, let's make sure we give input control to the gameobject that also has Input authority.
			if (Object.HasInputAuthority)
			{
				Runner.AddCallbacks(this);
			}

			Debug.Log("Spawned [" + this + "] IsClient=" + Runner.IsClient + " IsServer=" + Runner.IsServer + " HasInputAuth=" + Object.HasInputAuthority + " HasStateAuth=" + Object.HasStateAuthority);
		}

		/// <summary>
		/// Get Unity input and store them in a struct for Fusion
		/// </summary>
		/// <param name="runner">The current NetworkRunner</param>
		/// <param name="input">The target input handler that we'll pass our data to</param>
		public void OnInput(NetworkRunner runner, NetworkInput input)
		{
			if (_player!=null && _player.Object!=null && _player.state == Player.State.Active && fetchInput)
			{
				// Fill networked input struct with input data

				_frameworkInput.aimDirection = _aimDelta.normalized;
				
				_frameworkInput.moveDirection = _moveDelta.normalized;

				if ( _primaryFire )
				{
					_primaryFire = false;
					_frameworkInput.Buttons |= NetworkInputData.BUTTON_FIRE_PRIMARY;
				}

				if ( _secondaryFire )
				{
					_secondaryFire = false;
					_frameworkInput.Buttons |= NetworkInputData.BUTTON_FIRE_SECONDARY;
				}

				if (ToggleReady)
				{
					ToggleReady = false;
					_frameworkInput.Buttons |= NetworkInputData.READY;
				}
			}

			// Hand over the data to Fusion
			input.Set(_frameworkInput);
			_frameworkInput.Buttons = 0;
		}

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
		{
		}

		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
		}

		private void Update()
		{
			ToggleReady = ToggleReady || Input.GetKeyDown(KeyCode.R);

			if (Input.mousePresent)
			{
				if (Input.GetMouseButton(0) )
					_primaryFire = true;

				if (Input.GetMouseButton(1) )
					_secondaryFire = true;

				_moveDelta = Vector2.zero;
				
				if (Input.GetKey(KeyCode.W))
					_moveDelta += Vector2.up;

				if (Input.GetKey(KeyCode.S))
					_moveDelta += Vector2.down;

				if (Input.GetKey(KeyCode.A))
					_moveDelta += Vector2.left;

				if (Input.GetKey(KeyCode.D))
					_moveDelta += Vector2.right;

				Vector3 mousePos = Input.mousePosition;

				RaycastHit hit;
				Ray ray = Camera.main.ScreenPointToRay(mousePos);

				Vector3 mouseCollisionPoint = Vector3.zero;
				// Raycast towards the mouse collider box in the world
				if (Physics.Raycast(ray, out hit, Mathf.Infinity, _mouseRayMask))
				{
					if (hit.collider != null)
					{
						mouseCollisionPoint = hit.point;
					}
				}

				Vector3 aimDirection = mouseCollisionPoint - _player.turretPosition;
				_aimDelta = new Vector2(aimDirection.x,aimDirection.z );
			}

			if (Input.touchSupported)
			{
				bool leftIsDown = false;
				bool rightIsDown = false;

				foreach (Touch touch in Input.touches)
				{
					if (touch.position.x < Screen.width / 2)
					{
						leftIsDown = true;
						_leftPos = touch.position;
						if (_leftTouchWasDown)
							_moveDelta += 10.0f * touch.deltaPosition / Screen.dpi;
						else
							_leftDown = touch.position;
					}
					else
					{
						rightIsDown = true;
						_rightPos = touch.position;
						if (_rightTouchWasDown && (touch.position-_rightDown).magnitude>(0.01f*Screen.dpi))
							_aimDelta = (10.0f / Screen.dpi) * (touch.position-_rightDown);
						else
							_rightDown = touch.position;
					}
				}
				if (_rightTouchWasDown && !rightIsDown )
					_primaryFire = true;
				if (_leftTouchWasDown && !leftIsDown && _moveDelta.magnitude < 0.01f )
					_secondaryFire = true;

				if( !leftIsDown )
					_moveDelta = Vector2.zero;
			
				_mobileInput.gameObject.SetActive(true);
				_mobileInput.SetLeft(leftIsDown, _leftDown, _leftPos);
				_mobileInput.SetRight(rightIsDown,_rightDown, _rightPos);

				_leftTouchWasDown = leftIsDown;
				_rightTouchWasDown = rightIsDown;
			}
			else
			{
				_mobileInput.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// FixedUpdateNetwork is the main Fusion simulation callback - this is where
		/// we modify network state.
		/// </summary>
		public override void FixedUpdateNetwork()
		{
			if (GameManager.playState == GameManager.PlayState.TRANSITION)
				return;
			// Get our input struct and act accordingly. This method will only return data if we
			// have Input or State Authority - meaning on the controlling player or the server.
			Vector2 direction = default;
			if (GetInput(out NetworkInputData input))
			{
				direction = input.moveDirection.normalized;

				if (input.IsDown(NetworkInputData.BUTTON_FIRE_PRIMARY))
				{
					_player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.PRIMARY);
				}

				if (input.IsDown(NetworkInputData.BUTTON_FIRE_SECONDARY))
				{
					_player.shooter.FireWeapon(WeaponManager.WeaponInstallationType.SECONDARY);
				}

				if (input.IsDown(NetworkInputData.READY))
				{
					_player.ToggleReady();
				}

				// We let the NetworkCharacterController do the actual work
				_player.SetDirections(direction, input.aimDirection.normalized);
			}
			_player.Move();
		}

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
		public void OnConnectedToServer(NetworkRunner runner) { }
		public void OnDisconnectedFromServer(NetworkRunner runner) { }
		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
		public void OnSceneLoadDone(NetworkRunner runner) { }
		public void OnSceneLoadStart(NetworkRunner runner) { }
	}

	/// <summary>
	/// Our custom definition of an INetworkStruct. Keep in mind that
	/// * bool does not work (C# does not define a consistent size on different platforms)
	/// * Must be a top-level struct (cannot be a nested class)
	/// * Stick to primitive types and structs
	/// * Size is not an issue since only modified data is serialized, but things that change often should be compact (e.g. button states)
	/// </summary>
	public struct NetworkInputData : INetworkInput
	{
		public const uint BUTTON_FIRE_PRIMARY = 1 << 0;
		public const uint BUTTON_FIRE_SECONDARY = 1 << 1;
		public const uint READY = 1 << 6;

		public uint Buttons;
		public Vector2 aimDirection;
		public Vector2 moveDirection;

		public bool IsUp(uint button)
		{
			return IsDown(button) == false;
		}

		public bool IsDown(uint button)
		{
			return (Buttons & button) == button;
		}
	}
}
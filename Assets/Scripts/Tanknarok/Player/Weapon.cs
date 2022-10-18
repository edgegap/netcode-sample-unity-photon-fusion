using System.Collections.Generic;
using Fusion;
using FusionExamples.Utility;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// The Weapon class controls how fast a weapon fires, which projectiles it uses
	/// and the start position and direction of projectiles.
	/// </summary>

	public class Weapon : NetworkBehaviour
	{
		[SerializeField] private Transform[] _gunExits;
		[SerializeField] private Projectile _projectilePrefab; // Networked projectile
		[SerializeField] private float _rateOfFire;
		[SerializeField] private byte _ammo;
		[SerializeField] private bool _infiniteAmmo;
		[SerializeField] private AudioEmitter _audioEmitter;
		[SerializeField] private LaserSightLine _laserSight;
		[SerializeField] private PowerupType _powerupType = PowerupType.DEFAULT;
		[SerializeField] private ParticleSystem _muzzleFlashPrefab;

		[Networked(OnChanged = nameof(OnFireTickChanged))]
		private int fireTick { get; set; }

		private int _gunExit;
		private float _visible;
		private bool _active;
		private List<ParticleSystem> _muzzleFlashList = new List<ParticleSystem>();

		public float delay => _rateOfFire;
		public bool isShowing => _visible >= 1.0f;
		public byte ammo => _ammo;
		public bool infiniteAmmo => _infiniteAmmo;

		public PowerupType powerupType => _powerupType;

		private void Awake()
		{
			// Create a muzzle flash for each gun exit point the weapon has
			if (_muzzleFlashPrefab != null)
			{
				foreach (Transform gunExit in _gunExits)
				{
					_muzzleFlashList.Add(Instantiate(_muzzleFlashPrefab, gunExit.position, gunExit.rotation, transform));
				}
			}
		}

		/// <summary>
		/// Control the visual appearance of the weapon. This is controlled by the Player based
		/// on the currently selected weapon, so the boolean parameter is entirely derived from a
		/// networked property (which is why nothing in this class is sync'ed).
		/// </summary>
		/// <param name="show">True if this weapon is currently active and should be visible</param>
		public void Show(bool show)
		{
			if (_active && !show)
			{
				ToggleActive(false);
			}
			else if (!_active && show)
			{
				ToggleActive(true);
			}

			_visible = Mathf.Clamp(_visible + (show ? Time.deltaTime : -Time.deltaTime) * 5f, 0, 1);

			if (show)
				transform.localScale = Tween.easeOutElastic(0, 1, _visible) * Vector3.one;
			else
				transform.localScale = Tween.easeInExpo(0, 1, _visible) * Vector3.one;
		}

		private void ToggleActive(bool value)
		{
			_active = value;

			if (_laserSight != null)
			{
				if (_active)
				{
					_laserSight.SetDuration(0.5f);
					_laserSight.Activate();
				}
				else
					_laserSight.Deactivate();
			}
		}

		/// <summary>
		/// Fire a weapon, spawning the bullet or, in the case of the hitscan, the visual
		/// effect that will indicate that a shot was fired.
		/// This is called in direct response to player input, but only on the server
		/// (It's filtered at the source in Player)
		/// </summary>
		/// <param name="runner"></param>
		/// <param name="owner"></param>
		/// <param name="ownerVelocity"></param>
		public void Fire(NetworkRunner runner, PlayerRef owner, Vector3 ownerVelocity)
		{
			if (powerupType == PowerupType.EMPTY || _gunExits.Length == 0)
				return;
			
			Transform exit = GetExitPoint();
			SpawnNetworkShot(runner, owner, exit, ownerVelocity);
			fireTick = Runner.Simulation.Tick;
		}

		public static void OnFireTickChanged(Changed<Weapon> changed)
		{
			changed.Behaviour.FireFx();
		}

		private void FireFx()
		{
			// Recharge the laser sight if this weapon has it
			if (_laserSight != null)
				_laserSight.Recharge();

			if(_gunExit<_muzzleFlashList.Count)
				_muzzleFlashList[_gunExit].Play();
			_audioEmitter.PlayOneShot();
		}

		/// <summary>
		/// Spawn a bullet prefab with prediction.
		/// On the authoritative instance this is just a regular spawn (host in hosted mode or weapon owner in shared mode).
		/// In hosted mode, the client with Input Authority will spawn a local predicted instance that will be linked to
		/// the hosts network object when it arrives. This provides instant client-side feedback and seamless transition
		/// to the consolidated state.
		/// </summary>
		private void SpawnNetworkShot(NetworkRunner runner, PlayerRef owner, Transform exit, Vector3 ownerVelocity)
		{
			Debug.Log($"Spawning Shot in tick {Runner.Simulation.Tick} stage={Runner.Simulation.Stage}");
			// Create a key that is unique to this shot on this client so that when we receive the actual NetworkObject
			// Fusion can match it against the predicted local bullet.
			var key = new NetworkObjectPredictionKey {Byte0 = (byte) owner.RawEncoded, Byte1 = (byte) runner.Simulation.Tick};
			runner.Spawn(_projectilePrefab, exit.position, exit.rotation, owner, (runner, obj) =>
			{
				obj.GetComponent<Projectile>().InitNetworkState(ownerVelocity);
			}, key );
		}

		private Transform GetExitPoint()
		{
			_gunExit = (_gunExit + 1) % _gunExits.Length;
			Transform exit = _gunExits[_gunExit];
			return exit;
		}
	}
}
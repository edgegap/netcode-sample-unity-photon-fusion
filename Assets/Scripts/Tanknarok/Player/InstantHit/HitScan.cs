using System;
using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// HitScan is the instant-hit alternative to a moving bullet.
	/// The point of representing this as a NetworkObject is that it allow it to work the same
	/// in both hosted and shared mode. If it had been done at the trigger (the weapon spawning the instant hit) with an RPC for visuals,
	/// we would not have been able to apply damage to the target because we don't have authority over that object in shared mode.
	/// Because it now runs on all clients, it will also run on the client that owns the target that needs to take damage.
	/// </summary>

	public class HitScan : Projectile
	{
		public interface IVisual
		{
			void Activate(Vector3 origin, Vector3 target, bool impact);
			void Deactivate();
			bool IsActive();
		}

		[SerializeField] private HitScanSettings _settings;
		
		[Serializable]
		public class HitScanSettings
		{
			public LayerMask hitMask;
			public float range = 100f;
			public float timeToFade = 1f;

			//Area of effect
			public float areaRadius;
			public float areaImpulse;
			public byte damage;
		}		

		[Networked] 
		public TickTimer networkedLife { get; set; }
		private TickTimer _predictedLife;
		public TickTimer life
		{
			get => Object.IsPredictedSpawn ? _predictedLife : networkedLife;
			set { if (Object.IsPredictedSpawn) _predictedLife = value;else networkedLife = value; }
		}

		[Networked] 
		public Vector3 networkedEndPosition { get; set; }
		private Vector3 _predictedEndPosition;
		public Vector3 endPosition
		{
			get => Object.IsPredictedSpawn ? _predictedEndPosition : networkedEndPosition;
			set { if (Object.IsPredictedSpawn) _predictedEndPosition = value;else networkedEndPosition = value; }
		}

		[Networked]
		public Vector3 networkedStartPosition { get; set; }
		private Vector3 _predictedStartPosition;
		public Vector3 startPosition
		{
			get => Object.IsPredictedSpawn ? _predictedStartPosition : networkedStartPosition;
			set { if (Object.IsPredictedSpawn) _predictedStartPosition = value;else networkedStartPosition = value; }
		}

		[Networked]
		public NetworkBool networkedImpact { get; set; }
		private NetworkBool _predictedImpact;
		public NetworkBool impact
		{
			get => Object.IsPredictedSpawn ? _predictedImpact : networkedImpact;
			set { if (Object.IsPredictedSpawn) _predictedImpact = value;else networkedImpact = value; }
		}

		private List<LagCompensatedHit> _areaHits = new List<LagCompensatedHit>();
		private IVisual _visual;

		private void Awake()
		{
			_visual = GetComponentInChildren<IVisual>(true);
		}

		public override void InitNetworkState(Vector3 ownerVelocity)
		{
			Debug.Log($"Initialising InstantHit predictedspawn={Object.IsPredictedSpawn}");
			life = TickTimer.CreateFromSeconds(Runner, _settings.timeToFade);

			Transform exit = transform;
			// We move the origin back from the actual position to make sure we can't shoot through things even if we start inside them
			impact = Runner.LagCompensation.Raycast(exit.position - 0.5f*exit.forward, exit.forward, _settings.range, Object.InputAuthority, out var hitinfo, _settings.hitMask.value, HitOptions.IncludePhysX);

			Vector3 hitPoint = exit.position + _settings.range * exit.forward;
			if (impact)
			{
				Debug.Log("Hitscan impact : " + hitinfo.GameObject.name);
				hitPoint = hitinfo.Point;
			}

			startPosition = transform.position;
			endPosition = hitPoint;
		}

		public override void Spawned()
		{
			GetComponent<NetworkTransform>().InterpolationDataSource = InterpolationDataSources.Snapshots;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			_visual.Deactivate();
		}

		public override void FixedUpdateNetwork()
		{
			if (life.Expired(Runner))
				Runner.Despawn(Object);
			else if(_visual!=null)
			{
				if (!_visual.IsActive())
				{
					_visual.Activate(startPosition, endPosition, impact);
					if (impact)
					{
						// We want this to execute on all clients to make sure it works in shared mode where the authority of the hitscan does not have authority of the target
						ApplyAreaDamage(_settings, endPosition);
					}
				}
			}
		}

		private void ApplyAreaDamage(HitScanSettings raySetting, Vector3 impactPos)
		{
			HitboxManager hbm = Runner.LagCompensation;
			int cnt = hbm.OverlapSphere(impactPos, raySetting.areaRadius, Object.InputAuthority, _areaHits, raySetting.hitMask, HitOptions.IncludePhysX);
			if (cnt > 0)
			{
				for (int i = 0; i < cnt; i++)
				{
					ICanTakeDamage target = _areaHits[i].GameObject.GetComponent<ICanTakeDamage>();
					if (target != null)
					{
						Vector3 impulse = _areaHits[i].GameObject.transform.position - transform.position;
						float l = Mathf.Clamp(raySetting.areaRadius - impulse.magnitude, 0, raySetting.areaRadius);
						impulse = raySetting.areaImpulse * l * impulse.normalized;
						target.ApplyDamage(impulse, raySetting.damage, Object.InputAuthority);
					}
				}
			}
		}
	}
}
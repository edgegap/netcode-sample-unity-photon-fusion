using UnityEngine;
using Fusion;

namespace FusionExamples.Tanknarok
{
	public class LaserBeam : NetworkBehaviour
	{
		[Header("Laser Beam Settings")] [SerializeField]
		private byte _damage;

		[SerializeField] private LayerMask _collisionMask;
		[SerializeField] private float _range;
		[SerializeField] private bool _lerpShorter = true; //Should the laser lerp when the distance to the new target is shorter than before
		[SerializeField] private bool _lerpFurther = true; //Should the laser lerp when the distance to the new target is further than before
		[SerializeField] private float _lerpSpeed;

		[Header("Particle Systems")] 
		[SerializeField] private ParticleSystem _spark;
		[SerializeField] private ParticleSystem _muzzleFlash;

		[Networked] private Vector3 targetPosition { get; set; }

		private LineRenderer _laser;

		public void Init()
		{
			_laser = gameObject.GetComponent<LineRenderer>();
			_muzzleFlash.Play();
			_spark.Play();
		}

		public void UpdateLaserBeam()
		{
			// Raycast
			RaycastHit hit;
			if (Physics.Raycast(this.transform.position, this.transform.forward, out hit, _range, _collisionMask))
			{
				targetPosition = hit.point;

				//Only deal damage if the laser beam hits something that it's supposed to deal damage to
				if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Player"))
				{
					// Deal Damage
					Player player = hit.collider.GetComponentInParent<Player>();
					if (player != null)
					{
						player.ApplyDamage(Vector3.zero, _damage, PlayerRef.None);
					}
				}
			}
			else // If nothing is hit, extend the beam to range
			{
				targetPosition = transform.position + transform.forward * _range;
			}

			_laser.SetPosition(0, this.transform.position);
			_spark.transform.position = targetPosition;

			if (!_lerpShorter && (targetPosition - transform.position).magnitude < (_laser.GetPosition(1) - transform.position).magnitude)
			{
				_laser.SetPosition(1, targetPosition);
			}
			else if (!_lerpFurther && (targetPosition - transform.position).magnitude > (_laser.GetPosition(1) - transform.position).magnitude)
			{
				_laser.SetPosition(1, targetPosition);
			}
			else
			{
				_laser.SetPosition(1, Vector3.Lerp(_laser.GetPosition(1), targetPosition, Time.deltaTime * _lerpSpeed));
			}
		}
	}
}
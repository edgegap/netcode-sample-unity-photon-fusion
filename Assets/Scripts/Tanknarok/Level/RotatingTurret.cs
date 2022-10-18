using UnityEngine;
using Fusion;

namespace FusionExamples.Tanknarok
{
	public class RotatingTurret : NetworkBehaviour
	{
		[SerializeField] private LaserBeam[] _laserBeams;
		[SerializeField] private float _rpm;

		private float _rotationSpeed;

		public override void Spawned()
		{
			for (int i = 0; i < _laserBeams.Length; i++)
			{
				_laserBeams[i].Init();
			}
		}

		// Rotates the turret and updates laser beams
		public override void FixedUpdateNetwork()
		{
			transform.Rotate(0, _rpm * Runner.DeltaTime, 0);

			for (int i = 0; i < _laserBeams.Length; i++)
			{
				_laserBeams[i].UpdateLaserBeam();
			}
		}
	}
}
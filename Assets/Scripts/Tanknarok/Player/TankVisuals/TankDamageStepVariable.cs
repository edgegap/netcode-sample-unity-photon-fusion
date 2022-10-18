using UnityEngine;

namespace FusionExamples.Tanknarok
{
	[CreateAssetMenu(fileName = "Tank Damage Step", menuName = "Scriptable Object/Special/Tank Damage Step", order = 1)]
	public class TankDamageStepVariable : ScriptableObject
	{
		[SerializeField] private Mesh _hullMesh;
		[SerializeField] private Mesh _turretMesh;
		[SerializeField] private GameObject _hitDebris;
		[SerializeField] private ParticleSystem _hitParticles;
		[SerializeField] private ParticleSystem _drivingDust;

		public Mesh HullMesh => _hullMesh;
		public Mesh TurretMesh => _turretMesh;
		public GameObject HitDebris => _hitDebris;
		public ParticleSystem HitParticles => _hitParticles;
		public ParticleSystem DrivingDust => _drivingDust;
	}
}
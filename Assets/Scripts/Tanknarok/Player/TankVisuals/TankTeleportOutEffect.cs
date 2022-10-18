using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class TankTeleportOutEffect : MonoBehaviour
	{
		private Player _player;

		[SerializeField] private GameObject _dummyTank;
		private Transform _dummyTankTurret;
		private Transform _dummyTankHull;

		[SerializeField] private ParticleSystem _teleportEffect;

		[Header("Audio")] [SerializeField] private AudioEmitter _audioEmitter;

		// Initialize dummy tank and set colors based on the assigned player
		public void Initialize(Player player)
		{
			_player = player;

			_dummyTankTurret = _dummyTank.transform.Find("EnergyTankOut_Turret");
			_dummyTankHull = _dummyTank.transform.Find("EnergyTankOut_Hull");
			_dummyTank.SetActive(false);

			ColorChanger.ChangeColor(transform, player.playerColor);
			
			_teleportEffect.Stop();
		}

		public void StartTeleport()
		{
			if(_audioEmitter.isActiveAndEnabled)
				_audioEmitter.PlayOneShot();

			transform.position = _player.transform.position;

			_dummyTank.SetActive(false);
			_dummyTank.SetActive(true);

			_dummyTankTurret.rotation = _player.turretRotation;
			_dummyTankHull.rotation = _player.hullRotation;

			_teleportEffect.Play();
		}
	}
}
using System.Collections;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class TankTeleportInEffect : MonoBehaviour
	{
		private Player _player;

		[Header("Time Settings")] [SerializeField]
		private float _timeBeforeParticles = 0.1f;

		[SerializeField] private float _timeDelayImpactParticles = 0.2f;

		[Header("Visuals")] [SerializeField] private GameObject _teleportTarget;
		[SerializeField] private ParticleSystem _teleportBeamParticle;
		[SerializeField] private ParticleSystem _teleportImpactParticle;
		[SerializeField] private ParticleSystem _energyDischargeParticle;
		[SerializeField] private GameObject _tankDummy;

		[Header("Audio")] [SerializeField] private AudioEmitter _audioEmitter;
		[SerializeField] private AudioClipData _beamAudioClip;
		[SerializeField] private AudioClipData _dischargeAudioClip;

		private Transform _tankDummyTurret;
		private Transform _tankDummyHull;

		private bool _endTeleportation;

		// Initialize dummy tank and set colors based on the assigned player
		public void Initialize(Player player)
		{
			_player = player;

			_tankDummyTurret = _tankDummy.transform.Find("EnergyTankIn_Turret");
			_tankDummyHull = _tankDummy.transform.Find("EnergyTankIn_Hull");

			ColorChanger.ChangeColor(transform, player.playerColor);

			ResetTeleporter();
		}

		public void EndTeleport()
		{
			_endTeleportation = true;
		}

		private void ResetTeleporter()
		{
			_endTeleportation = false;
			_teleportTarget.SetActive(false);
			_teleportBeamParticle.Stop();
			_teleportImpactParticle.Stop();
			_energyDischargeParticle.Stop();
			_tankDummy.SetActive(false);
		}

		public void StartTeleport()
		{
			ResetTeleporter();
			StartCoroutine(TeleportIn());
		}

		private IEnumerator TeleportIn()
		{
			_teleportTarget.SetActive(true);
			yield return new WaitForSeconds(_timeBeforeParticles);

			// Play the downwards beam
			_teleportBeamParticle.Play();
			_audioEmitter.PlayOneShot(_beamAudioClip);
			yield return new WaitForSeconds(_timeDelayImpactParticles);

			// Play impact particle
			_teleportTarget.SetActive(false);
			_teleportBeamParticle.Stop();
			_teleportImpactParticle.Play();

			// Set the dummy tank
			_tankDummy.SetActive(true);
			_tankDummyTurret.rotation = _player.turretRotation;
			_tankDummyHull.rotation = _player.hullRotation;

			// Waits for the tank to be ready before playing the discharge effect
			while (!_endTeleportation)
				yield return null;

			_tankDummy.SetActive(false);
			_energyDischargeParticle.Play();
			_audioEmitter.PlayOneShot(_dischargeAudioClip);
		}
	}
}
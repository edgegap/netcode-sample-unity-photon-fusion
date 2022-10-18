using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ExplosionFX : MonoBehaviour
	{
		[SerializeField] private AudioEmitter _audioEmitter;
		[SerializeField] private ParticleSystem _particle;

		private bool _hasAudioEmitter => _audioEmitter != null;
		private bool _hasParticle => _particle != null;

		private void OnValidate()
		{
			if (!_hasAudioEmitter)
				_audioEmitter = GetComponent<AudioEmitter>();
			if (!_hasParticle)
				_particle = GetComponent<ParticleSystem>();
		}

		public void PlayExplosion()
		{
			gameObject.SetActive(true);
			PlayParticle();
			PlaySound();
		}

		public void ResetExplosion()
		{
			ResetImpactFX();
			gameObject.SetActive(false);
		}

		private void PlaySound()
		{
			if (_hasAudioEmitter)
				_audioEmitter.PlayOneShot();
		}

		private void PlayParticle()
		{
			if (_hasParticle)
				_particle.Play();
		}

		private void ResetImpactFX()
		{
			if (_hasParticle)
			{
				_particle.Stop();
				_particle.Clear();
			}
		}
	}
}
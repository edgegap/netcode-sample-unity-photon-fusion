using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// Simple destructible that can be destroyed by either getting run over by a tank or taking damage from an explosion.
	/// This is not a network object but relies on local physics triggers. This is not recommended for any behaviour that
	/// has actual gameplay impact, since it is possible for these triggers to be slightly off between different clients.
	/// In this case, destructibles are just visual decorations and the triggers (tanks and explosions) are sufficiently slow moving, that it works.
	
	public class Destructible : MonoBehaviour
	{
		[SerializeField] private ParticleSystem _destroyedParticlePrefab;
		[SerializeField] private GameObject _visual;
		[SerializeField] private Collider _trigger;
		[SerializeField] private GameObject _debrisPrefab;
		[SerializeField] private LayerMask _destroyedByLayers;

		private ParticleSystem _destroyedParticle;
		private GameObject _debris;

		[SerializeField] private AudioEmitter _audioEmitter;

		private void Start()
		{
			if (_destroyedParticlePrefab != null)
				_destroyedParticle = Instantiate(_destroyedParticlePrefab, transform.position, transform.rotation, transform.parent);
		}

		// Using OnEnable to make the destructible recyclable
		private void OnEnable()
		{
			if (_debris != null)
				Destroy(_debris);

			_trigger.enabled = true;
			_visual.SetActive(true);
		}

		private void OnTriggerEnter(Collider other)
		{
			if ( ((1<<other.gameObject.layer) & _destroyedByLayers) !=0 )
			{
				DestroyObject();
			}
		}

		private void DestroyObject()
		{
			if (_audioEmitter != null)
				_audioEmitter.PlayOneShot();
			_destroyedParticle.Play();

			_trigger.enabled = false;
			_visual.SetActive(false);

			if (_debrisPrefab != null)
				_debris = Instantiate(_debrisPrefab, transform.parent);
		}
	}
}
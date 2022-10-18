using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class BulletTargetMarker : MonoBehaviour, Bullet.ITargetVisuals
	{
		[SerializeField] private ParticleSystem _targetMarker;

		private Vector3 _position;

		public void Destroy()
		{
			if(_targetMarker && _targetMarker.gameObject)
				Destroy(_targetMarker.gameObject);
		}

		public void InitializeTargetMarker(Vector3 launchPosition, Vector3 bulletVelocity, Bullet.BulletSettings bulletSettings)
		{
			_targetMarker.gameObject.SetActive(false);
			_targetMarker.transform.SetParent(null);
			_targetMarker.gameObject.SetActive(true);
			_targetMarker.transform.position = CalculateImpactPoint(launchPosition, bulletVelocity, bulletSettings);
			_targetMarker.Play();
		}

		Vector3 CalculateImpactPoint(Vector3 o, Vector3 v, Bullet.BulletSettings bulletSettings)
		{
			Vector3 g = Vector3.up * bulletSettings.gravity;

			// The position curve for the projectile is a 2nd order polynomial
			// p = o + v * t + g * t * t;
			// The velocity is given by the derivative
			// p´ = v + 2 * g * dt
			// So we'll need to reduce g by half to get the coefficient we need for the polynomial
			g.y /= 2; 

			float d = v.y * v.y - 4 * g.y * o.y;
			float t0 = (-v.y - Mathf.Sqrt(d)) / (2 * g.y);
			
			// There is no way the projectile can die mid-air with a single flat floor, but...
			float t = Mathf.Min( bulletSettings.timeToLive, t0);
			
			Vector3 p = o + v*t + g*t*t;
			return p + Vector3.up * 0.05f; //Return the position with a slight y offset to avoid spawning the marker inside whatever it hits
		}
	}
}
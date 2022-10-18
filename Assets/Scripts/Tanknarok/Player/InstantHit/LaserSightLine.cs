using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class LaserSightLine : MonoBehaviour
	{
		[SerializeField] private AnimationCurve _thicknessCurve;
		[SerializeField] private AnimationCurve _visibilityCurve;

		private LineRenderer _laserSight;
		[SerializeField] private float _duration = 10f;
		[SerializeField] private LayerMask _collisionMask;
		[SerializeField] private Transform _sightImpact;

		private float _timer = 0;

		private float _initialThickness;
		private Vector3 _initialImpactBallScale;

		bool _active = false;
		bool _initialized = false;

		public void SetDuration(float duration)
		{
			_duration = duration;
		}

		private void OnDestroy()
		{
			Destroy(_sightImpact.gameObject);
		}

		public void Initialize()
		{
			_laserSight = GetComponent<LineRenderer>();
			_initialThickness = _laserSight.widthMultiplier;
			_initialImpactBallScale = _sightImpact.transform.localScale;

			_initialized = true;
		}

		public void Activate()
		{
			if (!_initialized)
				Initialize();

			_timer = 0;

			ToggleLaserSight(true);
			ResetPosition();
		}

		public void Deactivate()
		{
			if (!_initialized)
				Initialize();

			ToggleLaserSight(false);
		}

		void ToggleLaserSight(bool value)
		{
			_active = value;
			_laserSight.enabled = _active;
			_sightImpact.gameObject.SetActive(_active);
		}

		public void Recharge()
		{
			_timer = 0;
		}

		void ResetPosition()
		{
			_laserSight.SetPosition(0, transform.position);
			_laserSight.SetPosition(1, transform.position);
			_sightImpact.transform.position = transform.position;
		}

		// Do visual updates in late update to get the correct position after FUN has adjusted tank visuals
		private void LateUpdate()
		{
			if (!_active)
				return;

			//Increase the timer
			_timer = Mathf.Clamp(_timer + Time.deltaTime, 0, _duration);

			//Draw the line
			DrawLaserSightLine();
			AdjustVisuals();
		}

		void DrawLaserSightLine()
		{
			_laserSight.SetPosition(0, transform.position);
			Ray ray = new Ray(transform.position, transform.forward);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, Mathf.Infinity, _collisionMask))
			{
				Vector3 direction = Vector3.forward * hit.distance;
				_laserSight.SetPosition(1, hit.point);
				_sightImpact.transform.position = hit.point;
				Debug.DrawRay(transform.position, transform.forward * Mathf.Infinity, Color.yellow);
				Debug.DrawLine(transform.position, hit.point, Color.red);
			}
		}

		void AdjustVisuals()
		{
			float t = _timer / _duration;
			float thickness = _thicknessCurve.Evaluate(t);
			float visibility = _visibilityCurve.Evaluate(t);
			float finalMultiplier = (_initialThickness * thickness) * visibility;
			_laserSight.widthMultiplier = finalMultiplier;

			_sightImpact.transform.localScale = _initialImpactBallScale * finalMultiplier;
		}
	}
}
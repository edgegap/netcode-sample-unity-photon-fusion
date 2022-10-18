using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class BounceUI : MonoBehaviour
	{
		[SerializeField] private AnimationCurve _bounceCurve;

		[SerializeField] private float _animCurveWeight = 1f;
		private Vector3 _defaultScale;
		private RectTransform _rect;


		private float _timer = 0;
		[SerializeField] private float _bounceDuration = 0.5f;
		private float _targetDuration;


		[SerializeField] private float _minDelay = 1f;

		[SerializeField] private float _randomOffset = 0.5f;

		[SerializeField] private bool _bounceWithTime = false;
		private bool _caughtUpWithTime = false;

		private float _randomDelay = 0;

		// Use this for initialization
		void Start()
		{
			_rect = GetComponent<RectTransform>();
			_defaultScale = _rect.localScale;
			_timer = _bounceDuration;
		}

		private void OnEnable()
		{
			_caughtUpWithTime = false;
			StartCoroutine(Wait());
		}

		void UpdateTimer()
		{
			_timer += Time.deltaTime;
			_timer = Mathf.Min(_timer, _bounceDuration);
		}

		void Bounce()
		{
			float t = _timer / _bounceDuration;
			float multiplier = _bounceCurve.Evaluate(t) * _animCurveWeight;

			_rect.localScale = _defaultScale + Vector3.one * multiplier;
		}

		void SetRandomDelay()
		{
			_randomDelay = _minDelay + Random.Range(0, _randomOffset);
		}

		IEnumerator Wait()
		{
			while (gameObject.activeSelf)
			{
				SetRandomDelay();
				yield return new WaitForSeconds(_randomDelay);
				if (_bounceWithTime && !_caughtUpWithTime)
				{
					_timer = _bounceDuration;
					float currentTime = Time.time;
					int ceiledCurrentTime = Mathf.CeilToInt(currentTime);
					float fractionTime = ceiledCurrentTime - currentTime;

					float modFracTime = fractionTime % _bounceDuration;
					_timer = modFracTime;
					_caughtUpWithTime = true;
				}
				else
				{
					_timer = 0;
				}

				yield return new WaitForSeconds(_bounceDuration);
			}
		}

		void Update()
		{
			UpdateTimer();
			Bounce();
		}
	}
}
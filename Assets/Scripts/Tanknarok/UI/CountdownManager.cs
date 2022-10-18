using System.Collections;
using UnityEngine;
using TMPro;

namespace FusionExamples.Tanknarok
{
	public class CountdownManager : MonoBehaviour
	{
		[SerializeField] private float _countdownFrom;
		[SerializeField] private AnimationCurve _countdownCurve;
		[SerializeField] private TextMeshProUGUI _countdownUI;
		[SerializeField] AudioEmitter _audioEmitter;

		private float _countdownTimer;

		public delegate void Callback();

		private void Start()
		{
			Reset();
		}

		public void Reset()
		{
			_countdownUI.transform.localScale = Vector3.zero;
		}

		public IEnumerator Countdown(Callback callback)
		{
			_countdownUI.transform.localScale = Vector3.zero;

			_countdownUI.text = _countdownFrom.ToString();
			_countdownUI.gameObject.SetActive(true);

			int lastCount = Mathf.CeilToInt(_countdownFrom + 1);
			_countdownTimer = _countdownFrom;

			while (_countdownTimer > 0)
			{
				int currentCount = Mathf.CeilToInt(_countdownTimer);

				if (lastCount != currentCount)
				{
					lastCount = currentCount;
					_countdownUI.text = currentCount.ToString();
					_audioEmitter.PlayOneShot();
				}

				float x = _countdownTimer - Mathf.Floor(_countdownTimer);
				
				float t = _countdownCurve.Evaluate(x);
				if (t >= 0)
					_countdownUI.transform.localScale = Vector3.one * t;

				_countdownTimer -= Time.deltaTime * 1.5f;
				yield return null;
			}

			_countdownUI.gameObject.SetActive(false);

			callback?.Invoke();
		}
	}
}
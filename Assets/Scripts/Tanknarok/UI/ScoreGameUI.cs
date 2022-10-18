using System.Collections;
using FusionExamples.Utility;
using UnityEngine;
using TMPro;

namespace FusionExamples.Tanknarok
{
	public class ScoreGameUI : PooledObject
	{
		[SerializeField] private TextMeshProUGUI _score;
		[SerializeField] private AudioEmitter _audioEmitter;

		private byte _currentScore;
		private bool _active;

		public void Initialize(Player player)
		{
			ResetScore();
			HideScore();

			Color scoreColor = player.playerMaterial.GetColor("_SilhouetteColor");
			_score.color = scoreColor;
		}

		public void ShowScore()
		{
			_score.enabled = true;
		}

		public void HideScore()
		{
			_score.enabled = false;
		}

		public void ResetScore()
		{
			_currentScore = 0;
			_score.text = "0";
		}

		public void SetNewScore(byte score)
		{
			_currentScore = score;

			StartCoroutine(IncreaseScoreSequence(1f));
		}

		private IEnumerator IncreaseScoreSequence(float switchDelay)
		{
			ShowScore();
			_active = true;

			yield return new WaitForSeconds(switchDelay);

			_score.text = _currentScore.ToString();
			_score.transform.localScale = Vector3.one * 2f;

			_audioEmitter.PlayOneShot();

			yield return new WaitForSeconds(1f);

			_active = false;
		}

		private void Update()
		{
			if (!_active)
				return;

			_score.transform.localScale = Vector3.Lerp(_score.transform.localScale, Vector3.one, Time.deltaTime * 3f);
		}
	}
}
using System.Collections.Generic;
using FusionExamples.Utility;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ScoreManager : MonoBehaviour
	{
		[SerializeField] private ScoreGameUI _scoreGamePrefab;
		[SerializeField] private Transform _uiScoreParent;

		[SerializeField] private ScoreLobbyUI _scoreLobbyPrefab;
		[SerializeField] private Transform _lobbyScoreParent;

		[SerializeField] private float _singleDigitSpacing;
		[SerializeField] private float _doubleDigitSpacing;

		[SerializeField] private ParticleSystem _confetti;
		[SerializeField] private AudioEmitter _audioEmitter;

		private Dictionary<int, ScoreLobbyUI> _lobbyScoreUI = new Dictionary<int, ScoreLobbyUI>();
		private Dictionary<int, ScoreGameUI> _gameScoreUI = new Dictionary<int, ScoreGameUI>();

		public void UpdateScore(int playerId, byte score)
		{
			foreach (Player player in PlayerManager.allPlayers)
			{
				ScoreGameUI ui;
				if (!_gameScoreUI.TryGetValue(player.playerID, out ui))
				{
					ui = ObjectPool.Instantiate(_scoreGamePrefab, Vector3.zero, _scoreGamePrefab.transform.rotation, _uiScoreParent);
					ui.Initialize(player);
					_gameScoreUI[player.playerID] = ui;
				}
				if(player.playerID==playerId)
					ui.SetNewScore(score);
				else
					ui.ShowScore();
			}
		}

		public void ShowLobbyScore(int winningPlayer)
		{
			foreach (Player player in PlayerManager.allPlayers)
			{
				ScoreLobbyUI scoreLobbyUI = ObjectPool.Instantiate(_scoreLobbyPrefab, Vector3.zero, _scoreLobbyPrefab.transform.rotation, _lobbyScoreParent);
				scoreLobbyUI.SetPlayerName(player);
				_lobbyScoreUI[player.playerID] = scoreLobbyUI;
				scoreLobbyUI.SetScore(player.score);
				scoreLobbyUI.ToggleCrown(player.playerID == winningPlayer);
				scoreLobbyUI.gameObject.SetActive(true);
			}

			// Organize the scores and celebrate with confetti
			OrganizeScoreBoards();
			Celebrate(winningPlayer);
		}

		public void HideLobbyScore()
		{
			foreach (KeyValuePair<int, ScoreLobbyUI> container in _lobbyScoreUI)
			{
				ObjectPool.Recycle(container.Value);
			}
			_lobbyScoreUI.Clear();
			_confetti.Clear();
		}

		public void HideUiScoreAndReset(bool reset)
		{
			foreach (KeyValuePair<int, ScoreGameUI> container in _gameScoreUI)
			{
				container.Value.HideScore();
				if(reset)
					container.Value.ResetScore();
			}
		}

		// Move and play the confetti by the winning players score
		private void Celebrate(int winningPlayer)
		{
			_confetti.transform.position = GetScorePosition(winningPlayer) + Vector3.up;
			_confetti.Play();

			_audioEmitter.PlayOneShot();
		}

		public Vector3 GetScorePosition(int player)
		{
			return _lobbyScoreUI[player].transform.position;
		}

		// Organizing the score that is displayed in the lobby after playing a match
		private void OrganizeScoreBoards()
		{
			List<float> playerSpacings = new List<float>();
			Vector3 defaultPosition = new Vector3(0, 0.05f, 0);
			byte[] scores = new byte[PlayerManager.allPlayers.Count];
			for (int i = 0; i < scores.Length; i++)
			{
				scores[i] = PlayerManager.allPlayers[i].score;

				// Save score spacings depending on how big the scores are
				float space = (scores[i] >= 10) ? _doubleDigitSpacing : _singleDigitSpacing;
				playerSpacings.Add(space);
			}

			// Space all the scores correctly from each other
			float lastSpacing = 0;
			float spaceOffset = 0;
			for (int i = 0; i < scores.Length; i++)
			{
				float space = 0;
				if (PlayerManager.allPlayers.Count > 1)
				{
					if (i != 0)
						space = playerSpacings[i] / 2 + playerSpacings[i - 1] / 2;
				}

				space += lastSpacing;

				Vector3 scorePos = defaultPosition;
				scorePos.x += (space);
				_lobbyScoreUI[PlayerManager.allPlayers[i].playerID].transform.localPosition = scorePos;
				lastSpacing = space;

				if (i == 0 || i == scores.Length - 1)
					spaceOffset += playerSpacings[i] / 2;
				else
					spaceOffset += playerSpacings[i];
			}

			// Center all the scores
			foreach (KeyValuePair<int, ScoreLobbyUI> container in _lobbyScoreUI)
			{
				Vector3 scorePos = container.Value.transform.localPosition;
				scorePos.x -= spaceOffset / 2;
				container.Value.transform.localPosition = scorePos;
			}
		}
	}
}
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ReadyupManager : MonoBehaviour
	{
		[SerializeField] private Transform _readyUIParent;
		[SerializeField] private ReadyupIndicator _readyPrefab;
		[SerializeField] private bool _allowSoloPlay = false;
		[SerializeField] private AudioEmitter _audioEmitter;

		private Dictionary<int, ReadyupIndicator> _readyUIs = new Dictionary<int, ReadyupIndicator>();
		private bool _allPlayersReady;

		private void Update()
		{
			if (_allPlayersReady || (GameManager.playState == GameManager.PlayState.LEVEL))
				return;

			foreach (ReadyupIndicator ui in _readyUIs.Values)
			{
				ui.Dirty();
			}

			_allPlayersReady = PlayerManager.allPlayers.Count>1 || (PlayerManager.allPlayers.Count==1 && _allowSoloPlay);
			foreach (Player player in PlayerManager.allPlayers)
			{
				ReadyupIndicator indicator;
				if (!_readyUIs.TryGetValue(player.playerID, out indicator))
				{
					indicator = Instantiate(_readyPrefab, _readyUIParent);
					_readyUIs.Add(player.playerID, indicator);
				}
				if(indicator.Refresh(player))
					_audioEmitter.PlayOneShot();
				if (!player.ready)
					_allPlayersReady = false;

                if (EdgegapManager.EdgegapPreServerMode && string.IsNullOrEmpty(player.ipAddress))
				{
					_allPlayersReady = false;
                }
			}

			
			if(_allPlayersReady)
				GameManager.instance.OnAllPlayersReady();
		}

		public void ShowUI()
		{
			_allPlayersReady = false;
			_readyUIParent.gameObject.SetActive(true);
		}

		public void HideUI()
		{
			ResetReadyIndicators();
			_readyUIParent.gameObject.SetActive(false);
		}

		private void ResetReadyIndicators()
		{
			// Reset the ready dictionaries for next use
			foreach (ReadyupIndicator ui in _readyUIs.Values)
			{
				ui.Dirty();
			}
			_allPlayersReady = false;
		}
	}
}
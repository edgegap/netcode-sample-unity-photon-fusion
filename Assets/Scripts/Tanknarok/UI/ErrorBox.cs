using System;
using FusionExamples.UIHelpers;
using FusionExamples.Utility;
using TMPro;
using UnityEngine;

namespace Tanknarok.UI
{
	[RequireComponent(typeof(Panel))]
	public class ErrorBox : MonoBehaviour
	{
		[SerializeField] private TMP_Text _header;
		[SerializeField] private TMP_Text _message;

		private Action _onClose;
		private Panel _panel;

		public static void Show(string header, string message, Action onclose)
		{
			Singleton<ErrorBox>.Instance.ShowInternal(header, message, onclose);
		}

		private void ShowInternal(string header, string message, Action onclose)
		{
			_header.text = header;
			_message.text = message;
			_onClose = onclose;
			if(_panel==null)
				_panel = GetComponent<Panel>();
			_panel.SetVisible(true);
		}

		public void OnClose()
		{
			_panel.SetVisible(false);
			_onClose();
		}
	}
}
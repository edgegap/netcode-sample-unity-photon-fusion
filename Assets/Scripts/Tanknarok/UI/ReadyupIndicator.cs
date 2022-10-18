using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ReadyupIndicator : MonoBehaviour
	{
		[SerializeField] private Transform _parent;
		[SerializeField] private TMPro.TextMeshProUGUI _readyText;

		private Camera _camera;
		private Transform _target;
		private float _timer;
		private int _direction = -1;
		private int _previousState = -1;

		public void Dirty()
		{
			_previousState = _direction;
			_direction = -1;
		}

		public bool Refresh(Player followPlayer)
		{
			_camera = Camera.main;
			_direction = followPlayer.ready ? 1 : -1;
			bool changed = _previousState != _direction;
			_previousState = _direction;
			_target = followPlayer.transform;
			_readyText.color = followPlayer.playerMaterial.GetColor("_SilhouetteColor");
			return changed;
		}

		// Follow the assigned transform and scale up or down depending on if the player is ready or not
		private void LateUpdate()
		{
			UpdateTimer();
			ScaleText();

			if (_target == null)
				return;

			transform.position = _camera.WorldToScreenPoint(_target.position);
		}

		private void UpdateTimer()
		{
			_timer += Time.deltaTime * _direction;
			_timer = Mathf.Clamp(_timer, 0, 0.1f);
		}

		private void ScaleText()
		{
			float t = _timer / 0.1f;
			_parent.localScale = Vector3.one * t;
		}
	}
}
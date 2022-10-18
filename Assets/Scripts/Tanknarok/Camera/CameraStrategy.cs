using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class CameraStrategy : MonoBehaviour
	{
		[Header("Base Settings")] [SerializeField]
		protected float _moveSpeed;

		private float _initialMoveSpeed;
		protected Transform _myCamera;

		[Header("-- Targets")] protected List<GameObject> _targets = new List<GameObject>();
		protected List<GameObject> _activeTargets = new List<GameObject>();

		private bool _usingPlaceholderTarget = false;
		private GameObject _placeholderTarget = null;

		private bool _setPlaceholder = true;

		public virtual void Initialize()
		{
			_myCamera = Camera.main.transform;

			_initialMoveSpeed = _moveSpeed;
		}

		public void AddTarget(GameObject target)
		{
			if (_usingPlaceholderTarget)
			{
				_targets.Clear();
				_usingPlaceholderTarget = false;
			}

			_targets.Add(target);
		}

		public void RemoveTarget(GameObject target)
		{
			_targets.Remove(target);
			if (_targets.Count == 0)
				UsePlaceholderTarget();
		}

		public virtual void ClearTargets()
		{
			_targets.Clear();
			_activeTargets.Clear();
		}

		// Workaround for when all tanks are removed and there is no target left
		private void UsePlaceholderTarget()
		{
			_setPlaceholder = true;
		}

		private void Update()
		{
			if (_setPlaceholder)
			{
				_setPlaceholder = false;
				if (_placeholderTarget == null)
				{
					_placeholderTarget = new GameObject("Camera Placeholder Target");
				}

				_usingPlaceholderTarget = true;
				_placeholderTarget.transform.position = transform.position;
				_targets.Add(_placeholderTarget.gameObject);
			}
		}

		public void RemoveAll()
		{
			_targets.Clear();
		}
	}
}
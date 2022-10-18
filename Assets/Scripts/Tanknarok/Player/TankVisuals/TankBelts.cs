using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class TankBelts : MonoBehaviour
	{
		[SerializeField] private Player _tankBehaviour;

		[SerializeField] private Transform _leftBelt;
		[SerializeField] private Transform _rightBelt;
		private List<Transform> _treadTransforms;
		private Vector3[] _prevPositions;

		private const int _numTreads = 2;
		private Renderer _renderer;
		private MaterialPropertyBlock _matProps;
		private List<float> _materialOffsets;

		private Vector3 _lastForward;


		[SerializeField] private float _trackSpeed = 16f;

		public void Awake()
		{
			_renderer = GetComponent<MeshRenderer>();
			_matProps = new MaterialPropertyBlock();

			_treadTransforms = new List<Transform> {_rightBelt, _leftBelt};
			_materialOffsets = new List<float>(_numTreads);
			_prevPositions = new Vector3[_numTreads];
		}


		public void Start()
		{
			for (int i = 0; i < _numTreads; i++)
			{
				_materialOffsets.Add(0f);
				_prevPositions[i] = _treadTransforms[i].position;
			}

			_lastForward = transform.forward;
		}

		void Update()
		{
			for (int i = 0; i < _numTreads; i++)
			{
				CalculateTreadMovementSimplified(i);
			}
		}

		void CalculateTreadMovementSimplified(int treadIndex)
		{
			float tankMovementMultiplier = _tankBehaviour.velocity.magnitude;

			// Only adjust the treads if the tank is moving
			if (tankMovementMultiplier > 0.05f)
			{
				// Which direction is the tank moving?
				float dot = Vector3.Dot(_tankBehaviour.velocity, _treadTransforms[treadIndex].forward);
				float movement = dot < 0 ? -1 : 1;

				// Offset threads based on forward direction - makes them not rotate perfectly together
				float rotationStrengthMultiplier = Vector3.Dot(_lastForward, transform.forward);

				// Update the material offset
				float materialOffset = tankMovementMultiplier * rotationStrengthMultiplier * movement * (_trackSpeed * 0.1f * Time.deltaTime);
				UpdateTreadOffset(treadIndex, materialOffset);
			}

			_lastForward = transform.forward;
		}

		private void UpdateTreadOffset(int treadIndex, float newOffset)
		{
			_materialOffsets[treadIndex] += newOffset;

			_renderer.GetPropertyBlock(_matProps, treadIndex);
			_matProps.SetFloat("_Offset", _materialOffsets[treadIndex]);
			_renderer.SetPropertyBlock(_matProps, treadIndex);
		}
	}
}
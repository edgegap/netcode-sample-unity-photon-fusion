using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ScreenShaker : MonoBehaviour
	{
		[Header("Rotational")] [SerializeField]
		private float _maxYaw = 40f;

		[SerializeField] private float _maxPitch = 20f;
		[SerializeField] private float _maxRoll = 10f;

		[Header("Positional")] [SerializeField]
		private float _maxOfs = 2f;

		[Header("Settings")] [SerializeField] private float _speed = 5f;
		[SerializeField] private float _gravity = 1f;
		[SerializeField] private int _shakeStrength;

		//-----------------------------------//

		private static float _trauma = 0;

		private float _shake = 0;
		private float _modifiedTime = 0;

		private Quaternion _finalRotationShake = Quaternion.identity;
		private Vector3 _finalPositionalShake = Vector3.zero;

		public Quaternion finalRotationShake
		{
			get { return _finalRotationShake; }
		}

		public Vector3 finalPositionalShake
		{
			get { return _finalPositionalShake; }
		}


		void Update()
		{
			_modifiedTime = Time.time * _speed;
			if (_trauma > 0)
				_trauma -= Time.deltaTime * _gravity;
			else
				_trauma = 0f;

			Shake();
		}

		// Stack some trauma to build up a nice shake
		public static void AddTrauma(float newTrauma)
		{
			_trauma += newTrauma;
			_trauma = Mathf.Clamp01(_trauma);
		}

		// Reset trauma to 0 to stop screen shake
		public void ResetTrauma()
		{
			_trauma = 0;
		}

		//Calculate the shaking
		void Shake()
		{
			_shake = Mathf.Pow(_trauma, 2);
			_finalRotationShake = CalculateRotationalShake();
			_finalPositionalShake = CalculatePotitionalShake() * _shakeStrength;
		}

		//Calculate the rotational shake amount
		Quaternion CalculateRotationalShake()
		{
			float yaw = (_maxYaw * _shake * GetPerlinNoise(0)) * _shakeStrength;
			float pitch = (_maxPitch * _shake * GetPerlinNoise(1)) * _shakeStrength;
			float roll = (_maxRoll * _shake * GetPerlinNoise(2)) * _shakeStrength;

			return Quaternion.Euler(yaw, pitch, roll);
		}

		//Calculate the positional shake amount
		Vector3 CalculatePotitionalShake()
		{
			float offsetX = _maxOfs * _shake * GetPerlinNoise(3);
			float offsetY = _maxOfs * _shake * GetPerlinNoise(4);
			float offsetZ = _maxOfs * _shake * GetPerlinNoise(5);

			return new Vector3(offsetX, offsetY, offsetZ);
		}

		float GetPerlinNoise(int seedOffset)
		{
			float noise = Mathf.PerlinNoise(seedOffset, _modifiedTime);
			noise = (noise - 0.5f) * 2f; //Map it from (0-1) to (-1-1) to get negative values aswell;
			return noise;
		}
	}
}
using UnityEngine;
using UnityEngine.Serialization;

namespace FusionExamples.Tanknarok
{
	public class RotateForever : MonoBehaviour
	{
		private enum Axis { X, Y, Z };

		[SerializeField] private Axis axis = Axis.Y;
		[SerializeField] bool reverse = false;
		[SerializeField] public float _rotationsPerSecond = 1f;

		void Update()
		{
			Rotate();
		}

		void Rotate()
		{
			float direction = reverse == true ? -1 : 1;
			float rotation = _rotationsPerSecond * 360f * Time.deltaTime * direction;

			switch (axis)
			{
				case Axis.X:
					transform.Rotate(rotation, 0, 0);
					break;
				case Axis.Y:
					transform.Rotate(0, rotation, 0);
					break;
				case Axis.Z:
					transform.Rotate(0, 0, rotation);
					break;
			}
		}
	}
}
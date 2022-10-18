using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class MotorShake : MonoBehaviour
	{
		[SerializeField] private Vector3 shakeAmountByAxis = Vector3.zero;
		[SerializeField] private float shakeSpeed = 10f;

		private float offset;
		private Vector3 originScale;

		void Start()
		{
			originScale = transform.localScale;
			offset = Random.Range(-Mathf.PI, Mathf.PI);
		}

		Vector3 CalculateShake()
		{
			Vector3 shake = new Vector3(Mathf.Sin(Time.time * shakeSpeed + offset), Mathf.Sin(Time.time * shakeSpeed + offset), Mathf.Sin(Time.time * shakeSpeed + offset));
			shake.x *= shakeAmountByAxis.x;
			shake.y *= shakeAmountByAxis.y;
			shake.z *= shakeAmountByAxis.z;
			return shake;
		}

		void Update()
		{
			transform.localScale = originScale + CalculateShake();
		}
	}
}
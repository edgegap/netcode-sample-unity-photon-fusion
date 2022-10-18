using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class CameraBounds : MonoBehaviour
	{
		[Header("Boundaries")] [SerializeField] private Vector3 bounds = Vector3.one;

		[SerializeField] private Vector3 boundsOffset = Vector3.zero;
		[SerializeField] private float boundsMovementMax = 2f;
		private float boundsMovementMultiplier = 0;

		private Camera mainCamera;

		private float boundsDiagonal;

		public Vector3 Bounds
		{
			get { return bounds; }
		}

		public float Diagonal
		{
			get { return boundsDiagonal; }
		}

		public float SetBoundsMovementMultiplier
		{
			set { boundsMovementMultiplier = value; }
		}


		private bool initialized = false;

		// Start is called before the first frame update
		private void Awake()
		{
			Initialize();
		}

		public void Initialize()
		{
			bounds.y = 0;
			boundsDiagonal = Vector3.Distance(bounds, -bounds) * 2;

			mainCamera = Camera.main;
		}

		private void OnDrawGizmos()
		{
			Vector3 boundsOffset = this.boundsOffset + (new Vector3(0, 0, boundsMovementMax * boundsMovementMultiplier));
			Gizmos.DrawWireCube(boundsOffset, bounds * 2);
		}

		public Vector3 StayWithinBounds(Vector3 position, Vector3 offset, float cameraTiltAngle, float distance, Transform cameraTrans)
		{
			if (!initialized)
				Initialize();

			//Limit camera within boundries
			Vector3 cameraNoZ = new Vector3(offset.x, offset.y, 0) + new Vector3(position.x, position.y, 0);
			Vector3 cameraNoX = new Vector3(0, offset.y, offset.z) + new Vector3(0, position.y, position.z);

			Vector3 boundsOffset = this.boundsOffset + (new Vector3(0, 0, boundsMovementMax * boundsMovementMultiplier));

			//Find direction vectors to the camera from the boundry edges
			Vector3 leftBoundDir = cameraNoZ - new Vector3(-Bounds.x + boundsOffset.x, 0, 0);
			Vector3 rightBoundDir = cameraNoZ - new Vector3(Bounds.x + boundsOffset.x, 0, 0);
			Vector3 upperBoundDir = cameraNoX - new Vector3(0, 0, Bounds.z + boundsOffset.z);
			Vector3 lowerBoundDir = cameraNoX - new Vector3(0, 0, -Bounds.z + boundsOffset.z);

			float horizontalFOV = mainCamera.fieldOfView;
			float verticalFOV = 2 * Mathf.Atan(Mathf.Tan((horizontalFOV * Mathf.Deg2Rad) / 2) * mainCamera.pixelHeight / mainCamera.pixelWidth) * Mathf.Rad2Deg;

			float allowedXAngle = 90 - (horizontalFOV / 2);
			float allowedZAngleLower = cameraTiltAngle + (verticalFOV / 2);
			float allowedZAngleUpper = cameraTiltAngle - (verticalFOV / 2);

			//Limit the x-left movement
			float xLeftLimit = (-Bounds.x + boundsOffset.x) + Mathf.Cos(allowedXAngle * Mathf.Deg2Rad) * distance;
			if (position.x < xLeftLimit)
				position.x = xLeftLimit;

			//Limit the x-right movement
			float xRightLimit = (Bounds.x + boundsOffset.x) - Mathf.Cos(allowedXAngle * Mathf.Deg2Rad) * distance;
			if (position.x > xRightLimit)
				position.x = xRightLimit;

			float zUpperLimit = (Bounds.z + boundsOffset.z) - Mathf.Cos(allowedZAngleUpper * Mathf.Deg2Rad) * distance - cameraTrans.localPosition.z;
			if (position.z > zUpperLimit)
				position.z = zUpperLimit;


			float zLowerLimit = (-Bounds.z + boundsOffset.z) - Mathf.Cos(allowedZAngleLower * Mathf.Deg2Rad) * distance - cameraTrans.localPosition.z;
			if (position.z < zLowerLimit)
				position.z = zLowerLimit;

			//Left side
			//Debug.DrawRay(new Vector3((-Bounds.x + boundsOffset.x), 0, 0), leftBoundDir);
			//Debug.DrawRay(new Vector3((-Bounds.x + boundsOffset.x), 0, 0), new Vector3(Mathf.Cos(allowedXAngle * Mathf.Deg2Rad), Mathf.Sin(allowedXAngle * Mathf.Deg2Rad), 0) * 10, Color.red);

			////Right side
			//Debug.DrawRay(new Vector3((Bounds.x + boundsOffset.x), 0, 0), rightBoundDir);
			//Debug.DrawRay(new Vector3((Bounds.x + boundsOffset.x), 0, 0), new Vector3(-Mathf.Cos(allowedXAngle * Mathf.Deg2Rad), Mathf.Sin(allowedXAngle * Mathf.Deg2Rad), 0) * 10, Color.red);

			////Top side
			//Debug.DrawRay(new Vector3(0, 0, (Bounds.z + boundsOffset.z)), upperBoundDir);
			//Debug.DrawRay(new Vector3(0, 0, (Bounds.z + boundsOffset.z)), new Vector3(0, Mathf.Sin(allowedZAngleUpper * Mathf.Deg2Rad), -Mathf.Cos(allowedZAngleUpper * Mathf.Deg2Rad)) * 10, Color.red);

			////Bottom side
			//Debug.DrawRay(new Vector3(0, 0, (-Bounds.z + boundsOffset.z)), lowerBoundDir);
			//Debug.DrawRay(new Vector3(0, 0, (-Bounds.z + boundsOffset.z)), new Vector3(0, Mathf.Sin(allowedZAngleLower * Mathf.Deg2Rad), -Mathf.Cos(allowedZAngleLower * Mathf.Deg2Rad)) * 10, Color.red);

			boundsMovementMultiplier = 0;
			return position;
		}
	}
}
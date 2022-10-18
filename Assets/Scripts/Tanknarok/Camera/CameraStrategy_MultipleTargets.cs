using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class CameraStrategy_MultipleTargets : CameraStrategy
	{
		// Defining the size of the arena, easily accessable by anyone who needs it
		public const float ARENA_X = 26.35f;
		public const float ARENA_Y = 17.7f;
		
		private Vector3 _offset;
		private Vector3 _averageTarget;

		[Header("Camera Settings")] private float cameraTiltAngle = 55f;

		private float _longestDistance;
		private float _maxDist = 64f;
		private float _minDist = 50f;

		private float _distanceMultiplier = 1f;
		private float _maxDistanceMultiplier = 1.4f;

		private float _furtestTargetWeightMultiplier = 2.08f;

		[Header("Boundries")] [SerializeField] private CameraBounds _cameraBounds;

		private ScreenShaker _screenShaker;

		private float _initialLongestDistance;

		private Vector3 _averageTargetGizmoPosition;
		private Vector3 _weightedTargetGizmoPosition;


		private void Awake()
		{
			Initialize();
		}

		// Use this for initialization
		public override void Initialize()
		{
			base.Initialize();

			_screenShaker = GetComponent<ScreenShaker>();
			UpdateCamera();
			ForceUpdatePositionAndRotation();
		}

		public void ResetCameraPosition()
		{
			transform.position = Vector3.zero;
			_myCamera.transform.localPosition = _offset + _screenShaker.finalPositionalShake;
			_myCamera.transform.rotation = Quaternion.Euler(cameraTiltAngle, 0, 0);
		}

		[SerializeField] private float singleTargetYadditionalOffset = 2f;

		void UpdateCamera()
		{
			CalculateAverages();
			CalculateOffset();

			_averageTarget = _cameraBounds.StayWithinBounds(_averageTarget, _offset, cameraTiltAngle, _longestDistance, _myCamera.transform);
			if (_targets.Count == 1 && _targets[0] != null)
			{
				float arenaYMultiplier = Mathf.Clamp01(_targets[0].transform.position.z / ARENA_Y);
				float additionalZOffsetForOnlineTeleporter = arenaYMultiplier * singleTargetYadditionalOffset;
				_averageTarget += Vector3.forward * additionalZOffsetForOnlineTeleporter;
			}

			UpdatePositionAndRotation();
		}

		// Update is called once per frame
		public void LateUpdate()
		{
			UpdateCamera();
		}

		void CalculateAverages()
		{
			//Reset the distance
			_longestDistance = 0f;
			float yDifference = 0f;

			//Reset the average target variable
			_averageTarget = Vector3.zero;
			Vector3 weightedAverageTarget = Vector3.zero;

			//If there are no targets in the target list, then set the distance to 10
			if (_targets.Count == 0)
			{
				_longestDistance = _maxDist;
				return;
			}

			Vector3 furtestAwayPosition = Vector3.zero;

			_activeTargets.Clear();

			float minY = Mathf.Infinity;
			float maxZ = -Mathf.Infinity;
			float averageZ = 0;

			//Go through each target and calculate its distance to the targets average position and add it to the distance variable
			int count = 0;
			for (int i = 0; i < _targets.Count; i++)
			{
				GameObject targetGameObject = _targets[i];

				if (targetGameObject != null)
				{
					//Get current target
					Transform targetTransform = _targets[i].transform;

					_activeTargets.Add(targetTransform.gameObject);

					//Add target position to average target - gets divided later
					_averageTarget += targetTransform.position;

					//Loop through the target list again and check distances and y differences
					for (int j = i; j < _targets.Count; j++)
					{
						GameObject subTargetGameObject = _targets[j];
						if (subTargetGameObject != targetGameObject)
						{
							//Find the tanks with the longest distance between them
							float targetDistance = Vector3.Distance(targetTransform.position, _targets[j].transform.position);
							if (targetDistance > _longestDistance)
							{
								_longestDistance = targetDistance;
							}

							//Compensate for Y Difference between all tanks
							float yDiff = Mathf.Abs((targetTransform.position.z - _targets[j].transform.position.z));
							if (yDifference < yDiff)
							{
								yDifference = yDiff;
							}

							//Keep track of max and min positions on the z-axis 
							//for moving the boundsCenter around
							float zPos = targetTransform.position.z;
							if (zPos > maxZ)
								maxZ = zPos;
							if (zPos < minY)
								minY = zPos;
							averageZ += zPos;

							count++;
						}
					}
				}
			}

			// Compensate the camera position when driving above the center of the arena
			_distanceMultiplier = Mathf.Clamp(yDifference / (_cameraBounds.Bounds.z * 2), 0, 1f);
			if (averageZ >= 0)
			{
				float clampedAverage = Mathf.Clamp(averageZ / _cameraBounds.Bounds.z, 0, 1);

				_cameraBounds.SetBoundsMovementMultiplier = Mathf.Clamp((averageZ) / (_cameraBounds.Bounds.z), 0, 1);
			}

			_distanceMultiplier = Remap(_distanceMultiplier, 0, 1, 1, _maxDistanceMultiplier);

			if (_targets.Count == 1)
			{
				_longestDistance = _maxDist;
			}
			else
				_longestDistance += _minDist;

			float addOnDistance = (_longestDistance / _cameraBounds.Diagonal) * ((_maxDist * _distanceMultiplier) - _minDist);
			_longestDistance += (addOnDistance);

			//If target count is greater 3 or more, then we need to use a weighted target position to try to keep all players on the screen 
			if (_activeTargets.Count > 2)
			{
				//Reset weightedAverageTarget
				weightedAverageTarget = Vector3.zero;

				//Calculate the average of all the positions
				_averageTarget = _averageTarget / _activeTargets.Count;

				//Save position for gizmo drawing
				_averageTargetGizmoPosition = _averageTarget;

				float[] distances = new float[_activeTargets.Count];
				Vector3[] directions = new Vector3[_activeTargets.Count];
				float longestDistance = 0;

				//Find distances to average point
				for (int i = 0; i < _activeTargets.Count; i++)
				{
					Vector3 direction = _activeTargets[i].transform.position - _averageTarget;
					Debug.DrawRay(_activeTargets[i].transform.position, direction, Color.green);
					directions[i] = direction;

					float distanceToAverage = direction.magnitude;
					distances[i] = distanceToAverage;
					if (distanceToAverage > longestDistance)
						longestDistance = distanceToAverage;
				}

				//Calculate a average target offset with weights between 0-1 based on longestDistance
				//The longer a tank is from the average target, the more impact it will have on the weighted offset
				for (int i = 0; i < _activeTargets.Count; i++)
				{
					float multiplier = Remap(distances[i], 0, longestDistance, 0, 1);
					//weightedAverageTarget += (directions[i] * Mathf.Pow(multiplier, furtestTargetWeightMultiplier));
					weightedAverageTarget += (directions[i] * multiplier * _furtestTargetWeightMultiplier);
				}

				weightedAverageTarget /= _activeTargets.Count;
				_averageTarget += weightedAverageTarget; //Offset the average target with the weightes 

				//Save weighted target for gizmo drawing
				_weightedTargetGizmoPosition = _averageTarget;
			}
			//If there is only 1-2 players, we just use straight up average positioning
			else if (_activeTargets.Count > 0)
			{
				_averageTarget = _averageTarget / _activeTargets.Count;
			}
			else
			{
				_averageTarget = transform.position;
			}
		}

		public float Remap(float value, float oldFrom, float oldTo, float newFrom, float newTo)
		{
			return (value - oldFrom) / (oldTo - oldFrom) * (newTo - newFrom) + newFrom;
		}

		//Use trigonomerty to calculate the distance and height of the camera given a distance and a camera tilt angle
		void CalculateOffset()
		{
			float modifiedAngle = cameraTiltAngle - 90;
			float zOffset = (Mathf.Sin(modifiedAngle * Mathf.Deg2Rad) * _longestDistance);
			float yOffset = (Mathf.Cos(modifiedAngle * Mathf.Deg2Rad) * _longestDistance);
			_offset = new Vector3(0, yOffset, zOffset);
		}

		//Update the position and rotation of both the camera and the camera parent
		void UpdatePositionAndRotation()
		{
			//Camera local position and rotation
			_myCamera.transform.localPosition = Vector3.Lerp(_myCamera.transform.localPosition, _offset, Time.fixedDeltaTime * 10f) + _screenShaker.finalPositionalShake;
			_myCamera.transform.rotation = Quaternion.Lerp(_myCamera.transform.rotation, Quaternion.Euler(cameraTiltAngle, 0, 0), Time.fixedDeltaTime * 10f) * _screenShaker.finalRotationShake;

			//Camera parent position
			transform.position = Vector3.Lerp(transform.position, _averageTarget, Time.fixedDeltaTime * _moveSpeed);
		}

		void ForceUpdatePositionAndRotation()
		{
			//Camera local position and rotation
			_myCamera.transform.localPosition = _offset + _screenShaker.finalPositionalShake;
			_myCamera.transform.rotation = Quaternion.Euler(cameraTiltAngle, 0, 0) * _screenShaker.finalRotationShake;
		}

		public void CameraShake(float impact = 1f)
		{
			ScreenShaker.AddTrauma(impact);
		}

		public Vector3 GetAverageTarget()
		{
			return _averageTarget;
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawSphere(_averageTarget, 0.4f);

			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(_weightedTargetGizmoPosition, 0.4f);

			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(_averageTargetGizmoPosition, 0.4f);
		}
	}
}
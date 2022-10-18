using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class GigavoltVisual : MonoBehaviour, HitScan.IVisual
	{
		[SerializeField] private LineRenderer[] lines;

		private float squiggleSpacing = .75f; // How far apart should the points be added on the lines, in units of distance.

		[SerializeField] private AnimationCurve squiggleIntensityCurve = null;
		[SerializeField] private AnimationCurve fadeCurve = null;
		[SerializeField] private float fadeDuration = 1f;

		[SerializeField] private ParticleSystem sparkExplosion;

		private bool _active;

		public void Activate(Vector3 origin, Vector3 target, bool impact)
		{
			DrawAllBeams(origin, target);
			if (impact)
			{
				sparkExplosion.transform.position = target;
				sparkExplosion.Play();
			}
			_active = true;
		}

		public void Deactivate()
		{
			_active = false;
		}

		public bool IsActive()
		{
			return _active;
		}

		private void DrawAllBeams(Vector3 origin, Vector3 target)
		{
			float distance = Vector3.Distance(origin, target);
			for (int i = 0; i < lines.Length; i++)
			{
				LineRenderer line = lines[i];
				line.enabled = true;

				if (i == 0)
				{
					// draw the first beam as a thick straight line
					DrawBeam(line, origin, target, distance, false);
				}
				else
				{
					// draw the rest of the beams as squiggled lines
					DrawBeam(line, origin, target, distance, true);
				}

				// start fading out the beam
				StartCoroutine(FadeBeam(line, distance));
			}
		}

		// Draw a squiggely beam between fromPosition and toPosition, and add the distance too because this function is too lazy to figure it out.
		private void DrawBeam(LineRenderer lr, Vector3 fromPosition, Vector3 toPosition, float distance, bool squiggled)
		{
			List<Vector3> points = new List<Vector3>();
			//add the first point, which shouldn't be randomized in any way
			points.Add(fromPosition);

			// only if the line is squiggled do we need to add points inbetween from and to position
			if (squiggled)
			{
				int numPoints = Mathf.FloorToInt(distance);

				// add all the in-between points to the list of points.
				float pointDistance = squiggleSpacing;
				while (pointDistance < distance)
				{
					//The squigglecurve determines how randomized each point is - a lower number leads to a more focused beam.
					Vector3 point = Vector3.MoveTowards(fromPosition, toPosition, pointDistance);
					float randomIntensity = squiggleIntensityCurve.Evaluate(pointDistance / distance);
					Vector3 randomizedPoint = RandomizePoint(point, randomIntensity);

					points.Add(randomizedPoint);

					pointDistance += squiggleSpacing;
				}
			}

			// add the end point
			points.Add(toPosition);

			// set all the points in the line renderer
			lr.positionCount = points.Count;
			lr.SetPositions(points.ToArray());
		}

		// Add some randomization to a given Vector3
		private Vector3 RandomizePoint(Vector3 point, float intensity)
		{
			float x = point.x + Random.Range(intensity * -1, intensity);
			float y = point.y + Random.Range(intensity * -1, intensity);
			float z = point.z + Random.Range(intensity * -1, intensity);

			return new Vector3(x, y, z);
		}

		private IEnumerator FadeBeam(LineRenderer lr, float distance)
		{
			float timer = 0;
			float duration = fadeDuration;

			while (timer < duration)
			{
				float t = timer / duration;

				lr.material.SetFloat("_BeamFade", fadeCurve.Evaluate(t));

				timer += Time.deltaTime;
				yield return null;
			}

			lr.material.SetFloat("_BeamFade", 1);
		}
	}
}
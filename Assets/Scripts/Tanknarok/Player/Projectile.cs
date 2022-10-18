using System;
using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public abstract class Projectile : NetworkBehaviour, IPredictedSpawnBehaviour
	{
		private Vector3 _interpolateFrom;
		private Vector3 _interpolateTo;
		private NetworkTransform _nt;
		public abstract void InitNetworkState(Vector3 ownerVelocity);

		/// <summary>
		/// The following methods implement support for Fusions spawn prediction.
		/// While the bullet is in its spawn predicted state, it has no network properties or associations.
		/// It is, for all intents and purposes, a regular Unity gameobject.
		/// For this reason, the regular NetworkObject callbacks are not active while the bullet is spawn predicted,
		/// and you must explicitly handle this to make the object appear correctly.
		/// This example simply forwards the calls to the regular network methods, but this is only possible because
		/// that code does not access any networked state directly.
		/// (See the dual networked/predicted properties declared at the top of the file)
		/// At the relevant tick, the predicted spawn is either confirmed by the authoritative peer and becomes an
		/// actual networked object with a networked state, or it will fail and you must decide what to do with it.
		/// In this case we simply despawn it (important to use Despawn with allowPrediction=true rather Destroy
		/// because these are pooled objects and we want them returned to the pool), but we could also have allowed
		/// them to fade out or add some other visualisation to illustrate a "glitched" shot.
		/// </summary>

		public void PredictedSpawnSpawned()
		{
			_nt = GetComponent<NetworkTransform>();
			_interpolateTo = transform.position;
			_interpolateFrom = _interpolateTo;
			_nt.InterpolationTarget.position = _interpolateTo;
			Spawned();
		}

		public void PredictedSpawnUpdate()
		{
			_interpolateFrom = _interpolateTo;
			_interpolateTo = transform.position;
			FixedUpdateNetwork();
		}

		void IPredictedSpawnBehaviour.PredictedSpawnRender() {
			var a = Runner.Simulation.StateAlpha;
			_nt.InterpolationTarget.position = Vector3.Lerp(_interpolateFrom, _interpolateTo, a);
		}

		public void PredictedSpawnFailed()
		{
			Debug.LogWarning($"Predicted Spawn Failed Object={Object.Id}, instance={gameObject.GetInstanceID()}, resim={Runner.IsResimulation}");
			Runner.Despawn(Object, true);
		}

		public void PredictedSpawnSuccess()
		{
			//Debug.Log($"Predicted Spawn Success Object={Object.Id}, instance={gameObject.GetInstanceID()}, resim={Runner.IsResimulation}");
		}
	}
}
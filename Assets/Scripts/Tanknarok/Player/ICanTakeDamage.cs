using Fusion;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// Interface implemented by any gameobject that can be damaged.
	/// </summary>
	public interface ICanTakeDamage
	{
		void ApplyDamage(Vector3 impulse, byte damage, PlayerRef source);
	}
}
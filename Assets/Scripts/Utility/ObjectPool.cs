using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FusionExamples.Utility
{
	/// <summary>
	/// ObjectPool speeds up creation of prefab instances by re-cycling old instances rather than destroying them.
	/// 
	/// Simply attached the PooledObject to your prefab (or derive your own component from PooledObject) and then use
	/// ObjectPool.Instantiate( prefab ); to create a pooled object.
	/// 
	/// It is important that you do not destroy the pooled object explicitly but instead call ObjectPool.Recycle( instance );
	/// 
	/// Also, since objects are re-used and potentially modified between uses you cannot assume that a new instance will 
	/// have the default prefab property values, but must explicitly reset the instance by overriding the OnRecycled() method
	/// 
	/// The PooledObject has some helper methods to ease this job.
	/// 
	/// Finally, when objects are not used, they are re-parented to a common pool root node - this will be created for you.
	/// </summary>
	public class ObjectPool
	{
		private PooledObject _prefab;
		private List<PooledObject> _free = new List<PooledObject>();

		public PooledObject prefab
		{
			get { return _prefab; }
		}

		public List<PooledObject> freeObjects => _free;

		public ObjectPool(PooledObject prefab)
		{
			_prefab = prefab;
		}

		public PooledObject Instantiate()
		{
			return Instantiate(_prefab.transform.position, _prefab.transform.rotation);
		}

		public PooledObject Instantiate(Vector3 p, Quaternion q, Transform parent = null)
		{
			PooledObject newt = null;

			// Remove dead objects from the pool if any (I've seen som log entries on GA that suggests that this may happen, not sure why)
			// _pool.RemoveAll( (PooledObject obj) => { return !obj; } );

			if (_free.Count > 0)
			{
				var t = _free[0];
				if (t) // In case a recycled object was destroyed
				{
					Transform xform = t.transform;
					xform.SetParent(parent, false);
					xform.position = p;
					xform.rotation = q;
					newt = t;
				}
				else
				{
					Debug.LogWarning("Recycled object of type <" + _prefab + "> was destroyed - not re-using!");
				}

				_free.RemoveAt(0);
			}

			if (newt == null)
			{
				newt = Object.Instantiate(_prefab, p, q, parent);
				newt.name = "Instance(" + newt.name + ")";
				newt.pool = this;
			}

			newt.OnRecycled();
			newt.gameObject.SetActive(true);
			return newt;
		}

		private void Clear()
		{
			foreach (var pooled in _free)
			{
				Object.Destroy(pooled);
			}

			_free = new List<PooledObject>();
		}

		private static Dictionary<object, ObjectPool> _pools = new Dictionary<object, ObjectPool>();
		private static ObjectPoolRoot _poolRoot;

		public static T Instantiate<T>(T prefab) where T : PooledObject
		{
			return Instantiate(prefab, Vector3.zero, Quaternion.identity);
		}

		public static ObjectPool Get<T>(T prefab) where T : PooledObject
		{
			ObjectPool pool;
			if (!_pools.TryGetValue(prefab, out pool))
			{
				pool = new ObjectPool(prefab);
				_pools[prefab] = pool;
			}

			return pool;
		}

		public static T Instantiate<T>(T prefab, Vector3 pos, Quaternion q, Transform parent = null) where T : PooledObject
		{
			return (T) Get(prefab).Instantiate(pos, q, parent);
		}

		public static T Instantiate<T>(Vector3 pos, Quaternion q, Transform parent = null) where T : PooledObject
		{
			ObjectPool pool;
			if (!_pools.TryGetValue(typeof(T), out pool))
			{
				GameObject go = new GameObject("Prefab<" + typeof(T).Name + ">");
				go.SetActive(false);
				T prefab = go.AddComponent<T>();
				pool = new ObjectPool(prefab);
				_pools[typeof(T)] = pool;
			}

			return (T) pool.Instantiate(pos, q, parent);
		}

		public static void Recycle(PooledObject po)
		{
			if (po != null)
			{
				if (po.pool == null)
				{
					po.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
					po.transform.SetParent(null, false);
					Object.Destroy(po.gameObject);
				}
				else
				{
					po.pool._free.Add(po);
					if (_poolRoot == null)
					{
						_poolRoot = Singleton<ObjectPoolRoot>.Instance;
						_poolRoot.name = "ObjectPoolRoot";
					}

					po.gameObject.SetActive(false); // Should always disable before re-parenting, or we will dirty it twice
					po.transform.SetParent(_poolRoot.transform, false);
				}
			}
		}

		public static void ClearPools()
		{
			foreach (ObjectPool pool in _pools.Values)
			{
				pool.Clear();
			}

			_pools = new Dictionary<object, ObjectPool>();
		}
	}
}
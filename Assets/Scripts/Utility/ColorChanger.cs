using System.Collections.Generic;
using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ColorChanger : MonoBehaviour
	{
		private ParticleSystem _partSystem;
		private MeshRenderer _meshRenderer;
		private UnityEngine.UI.Image _imageComponent;

		private ParticleSystem.MainModule _mainModule;

		public void Initialize()
		{
			_partSystem = GetComponent<ParticleSystem>();
			_meshRenderer = GetComponent<MeshRenderer>();
			_imageComponent = GetComponent<UnityEngine.UI.Image>();
		}

		public void ChangeColor(Color newColor)
		{
			if (_meshRenderer != null)
			{
				_meshRenderer.material.color = newColor;
			}

			if (_partSystem != null)
			{
				try
				{
					_mainModule = _partSystem.main;
					_mainModule.startColor = newColor;
				}
				catch (System.NullReferenceException)
				{
					Debug.LogError("NullReference in ColorChanger. (Do not create your own module instances)");
				}
			}

			if (_imageComponent != null)
			{
				_imageComponent.color = newColor;
			}
		}
		
		public static void FindColorChangers(Transform currentTransform, ref List<ColorChanger> colorChangers)
		{
			ColorChanger colorChanger = currentTransform.GetComponent<ColorChanger>();
			if (colorChanger != null)
			{
				colorChangers.Add(colorChanger);
				colorChanger.Initialize();
			}

			foreach (Transform go in currentTransform)
			{
				FindColorChangers(go, ref colorChangers);
			}
		}

		public static void ChangeColor(Color color, List<ColorChanger> colorChangers)
		{
			foreach (ColorChanger colorChanger in colorChangers)
			{
				colorChanger.ChangeColor(color);
			}
		}

		public static void ChangeColor(Transform fromRoot, Color color)
		{
			List<ColorChanger> changers = new List<ColorChanger>();
			FindColorChangers( fromRoot, ref changers);
			ChangeColor(color,changers);
		}
	}
}
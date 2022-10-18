using System;
using System.Collections;
using System.Collections.Generic;
using FusionExamples.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.UIHelpers
{
	public class Panel : PooledObject
	{
		public enum HideLocation
		{
			SAME_POSITION,
			LEFT_EDGE,
			RIGHT_EDGE,
			UPPER_EDGE,
			LOWER_EDGE,
			SPECIFIED_RECT,
			SPECIFIED_POS
		}

		[Tooltip("Easing method used when showing panel")]
		public Tween.EaseType _showEase = Tween.EaseType.easeOutExpo;

		[Tooltip("Easing method used when hiding panel")]
		public Tween.EaseType _hideEase = Tween.EaseType.easeOutExpo;

		[Tooltip("Easing time (secs)")] public float _easeTime = .5f;

		[Tooltip("Abstract location to move panel to when hiding")]
		public HideLocation _hideAt = HideLocation.SAME_POSITION;

		[Tooltip("Hide panel initially")] public bool _hideInitially = true;
		[Tooltip("Disable when hidden")] public bool _disableWhenHidden = true;

		[Tooltip("Target rect when hiding to 'specified rect'")]
		public RectTransform _hideRect;

		[Tooltip("Specific location to move panel to when hiding (If SPECIFIED_POS)")]
		public Vector2 _hiddenPos;

		[Tooltip("Scale to apply when hidden")]
		public Vector3 _hideScale = Vector3.one;

		[Tooltip("Alpha to apply to child Graphics when hidden")]
		public float _hideAlpha = 1.0f;

		[Tooltip("Position to use when showing panel (updates automatically)")]
		public Vector2 _shownPos;

		[Tooltip("Scale to use when showing panel (updates automatically)")]
		public Vector3 _shownScale = Vector3.one;

		[Tooltip("Alpha to apply to child Graphics when shown")]
		public float _showAlpha = 1.0f;

		[Tooltip("True if panel should remember current child-alphas before hiding")]
		public bool _rememberAlphas;

		[Tooltip("True if panel should remember current position before hiding")]
		public bool _rememberPosition;

		private Vector2 _shownSize;

		private bool _isShowing;
		private Coroutine _coroutine;

		public bool isShowing
		{
			get { return _isShowing; }
		}

		public Vector2 shownPos
		{
			get { return _shownPos; }
			set { _shownPos = value; }
		}

		private Dictionary<Graphic, float> _orgAlphas;
		private bool _captured;

		protected void Awake()
		{
			_isShowing = true;
			if (_hideInitially)
			{
				CaptureRect();
				((RectTransform) transform).anchoredPosition = hiddenPos;
				((RectTransform) transform).localScale = _hideScale;
				if (_disableWhenHidden)
					gameObject.SetActive(false);
				_isShowing = false;
			}
		}

		private void CaptureRect()
		{
			if (!_captured)
			{
				_shownPos = ((RectTransform) transform).anchoredPosition;
				_shownScale = transform.localScale;
				_captured = true;
			}
		}

		public virtual Vector2 hiddenSize
		{
			get
			{
				if (_hideAt == HideLocation.SPECIFIED_RECT)
					return _hideRect.rect.size;
				return _shownSize;
			}
		}

		public virtual Vector2 hiddenPos
		{
			get
			{
				if (transform.parent == null || !(transform.parent is RectTransform))
					return _shownPos;

				RectTransform rt = (RectTransform) transform;
				RectTransform prt = (RectTransform) transform.parent;

				Vector2 hidden = _shownPos;

				switch (_hideAt)
				{
					case HideLocation.SAME_POSITION:
						break;
					case HideLocation.LOWER_EDGE:
						hidden.y = prt.rect.height * (rt.anchorMin.y * (rt.pivot.y - 1.0f) - rt.anchorMax.y * rt.pivot.y) + (rt.pivot.y - 1.0f) * _hideScale.y * rt.rect.height;
						break;
					case HideLocation.UPPER_EDGE:
						hidden.y = prt.rect.height * (1 + rt.anchorMin.y * (rt.pivot.y - 1.0f) - rt.anchorMax.y * rt.pivot.y) + (rt.pivot.y) * _hideScale.y * rt.rect.height;
						break;
					case HideLocation.LEFT_EDGE:
						hidden.x = prt.rect.width * (rt.anchorMin.x * (rt.pivot.x - 1.0f) - rt.anchorMax.x * rt.pivot.x) + (rt.pivot.x - 1.0f) * _hideScale.x * rt.rect.width;
						break;
					case HideLocation.RIGHT_EDGE:
						hidden.x = prt.rect.width * (1 + rt.anchorMin.x * (rt.pivot.x - 1.0f) - rt.anchorMax.x * rt.pivot.x) + (rt.pivot.x) * _hideScale.x * rt.rect.width;
						break;
					case HideLocation.SPECIFIED_RECT:
						hidden = MapAnchoredPosition(_hideRect, (RectTransform) transform);
						break;
					case HideLocation.SPECIFIED_POS:
						hidden = _hiddenPos;
						break;
				}

				return hidden;
			}
		}

		private Vector2 MapAnchoredPosition(RectTransform from, RectTransform to)
		{
			float wto = ((RectTransform) from.parent).rect.width;
			float hto = ((RectTransform) from.parent).rect.height;
			Vector2 vto = from.anchoredPosition;
			Vector2 cto = new Vector2((from.anchorMin.x + from.anchorMax.x) * wto / 2, (from.anchorMin.y + from.anchorMax.y) * hto / 2);

			float wfrom = ((RectTransform) to.parent).rect.width;
			float hfrom = ((RectTransform) to.parent).rect.height;
			Vector2 cfrom = new Vector2((to.anchorMin.x + to.anchorMax.x) * wfrom / 2, (to.anchorMin.y + to.anchorMax.y) * hfrom / 2);

			Debug.Log("Center To " + cto + " Center From " + cfrom + " to " + vto);

			return cto + vto - cfrom;
		}

		public void SetVisible(bool v, bool immediately = false, Action then = null)
		{
			if (_shownSize.x == 0 || _shownSize.y == 0)
				_shownSize = ((RectTransform) transform).rect.size;
			if (v == isShowing)
				return;
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
				_coroutine = null;
			}

			if (v)
				Show(immediately, then);
			else
				Hide(immediately, then);
		}

		private void Hide(bool immediately = false, Action then = null)
		{
			_isShowing = false;
			if (onHide != null)
				onHide(this);
			WillHide();
			CaptureRect();
			if (_rememberPosition)
				_shownPos = ((RectTransform) transform).anchoredPosition;
			if (_hideAlpha < 1.0f && (_rememberAlphas || _orgAlphas == null))
				RememberAlphas();

			Action onHidden = () =>
			{
				DidHide();
				if (onDidHide != null)
					onDidHide(this);
				if (_disableWhenHidden)
					gameObject.SetActive(false);
				if (then != null)
					then();
			};

			if (gameObject.activeInHierarchy)
				_coroutine = StartCoroutine(AnimateRect(immediately, _shownPos, hiddenPos, _shownScale, _hideScale, _shownSize, hiddenSize, _showAlpha, _hideAlpha, Tween.GetEasingFunction(_hideEase),
					onHidden));
			else
				AnimateRect(true, _shownPos, hiddenPos, _shownScale, _hideScale, _shownSize, hiddenSize, _showAlpha, _hideAlpha, Tween.GetEasingFunction(_hideEase), onHidden);
		}

		private void RememberAlphas()
		{
			_orgAlphas = new Dictionary<Graphic, float>();
			foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
			{
				_orgAlphas[graphic] = graphic.color.a;
			}
		}

		private void Show(bool immediately = false, Action then = null)
		{
			if (_hideInitially)
			{
				((RectTransform) transform).anchoredPosition = hiddenPos;
				((RectTransform) transform).localScale = _hideScale;
				_hideInitially = false; // Prevent immediately hiding it again if this is the first time we show it!
			}

			if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
			{
				Debug.Log("Can't show " + this + " because parent is disabled");
				return;
			}

			gameObject.SetActive(true);
			if (_hideAlpha < 1.0f && _orgAlphas == null)
				RememberAlphas();
			_coroutine = StartCoroutine(AnimateRect(immediately, hiddenPos, _shownPos, _hideScale, _shownScale, hiddenSize, _shownSize, _hideAlpha, _showAlpha, Tween.GetEasingFunction(_showEase), () =>
			{
				DidShow();
				if (onDidShow != null)
					onDidShow(this);
				if (then != null)
					then();
			}));
			_isShowing = true;
			if (onShow != null)
				onShow(this);
			WillShow();
		}

		public event Action<Panel> onHide;
		public event Action<Panel> onShow;
		public event Action<Panel> onDidHide;
		public event Action<Panel> onDidShow;

		public virtual void WillShow()
		{
		}

		public virtual void DidShow()
		{
		}

		public virtual void WillHide()
		{
		}

		public virtual void DidHide()
		{
		}

		protected IEnumerator AnimateRect(bool immediately, Vector2 from_position, Vector2 to_position, Vector2 from_scale, Vector2 to_scale, Vector2 from_size, Vector2 to_size, float from_alpha,
			float to_alpha, Tween.EasingFunction func, Action whenDone)
		{
			if (_isShowing)
			{
				from_position = ((RectTransform) transform).anchoredPosition;
				from_scale = ((RectTransform) transform).localScale;
				from_size = ((RectTransform) transform).rect.size;
				from_alpha = 1.0f;
			}

			Graphic[] gfx = null;
			float[] fromAlphas = null;
			if (from_alpha != to_alpha) //  _alphas != null)
			{
				gfx = GetComponentsInChildren<Graphic>(false);
				fromAlphas = new float[gfx.Length];

				if (from_alpha > 0)
				{
					for (int g = 0; g < gfx.Length; g++)
					{
						fromAlphas[g] = from_alpha * gfx[g].color.a;
					}
				}
			}

			for (float f = immediately ? 1 : 0; immediately || Step.Forward(ref f, Time.deltaTime / _easeTime);)
			{
				if (from_position != to_position)
				{
					float x = func(from_position.x, to_position.x, f);
					float y = func(from_position.y, to_position.y, f);
					((RectTransform) transform).anchoredPosition = new Vector2(x, y);
				}

				if (from_scale != to_scale)
				{
					float sx = func(from_scale.x, to_scale.x, f);
					float sy = func(from_scale.y, to_scale.y, f);
					((RectTransform) transform).localScale = new Vector3(sx, sy, 1);
				}

				if (_hideAt == HideLocation.SPECIFIED_RECT && from_size != to_size)
				{
					float w = func(from_size.x, to_size.x, f);
					float h = func(from_size.y, to_size.y, f);
					((RectTransform) transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
					((RectTransform) transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
				}

				if (gfx != null) //  _alphas != null)
				{
					for (int g = 0; g < gfx.Length; g++)
					{
						if (gfx[g]) // Just in case someone destroyed this while we were animating it!
						{
							float alpha_org = 1.0f;
							if (_orgAlphas != null)
								_orgAlphas.TryGetValue(gfx[g], out alpha_org);
							Color c = gfx[g].color;
							c.a = func(fromAlphas[g], to_alpha * alpha_org, f);
							gfx[g].color = c;
						}
					}
				}

				if (!immediately)
					yield return new WaitForEndOfFrame();
				immediately = false;
			}

			whenDone();
		}
	}
}
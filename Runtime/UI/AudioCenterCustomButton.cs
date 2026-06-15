using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AudioCenter.UI
{
    /// <summary>
    /// Custom UGUI button with integrated scale animation and AudioCenter SFX.
    ///
    /// Provides a bouncy press-down → overshoot → settle click animation and a hover pop,
    /// driven by a shareable <see cref="AudioCenterButtonAnimationConfig"/> asset (with a
    /// built-in fallback so it works with no config assigned). On click it plays a UI sound
    /// through <see cref="AudioCenterAudioManager.PlaySound(string, string, AudioCenterAudioTrack, bool, AudioCenterPlaySoundMode, bool, Vector2, float)"/>
    /// on the <see cref="AudioCenterAudioTrack.UI"/> track.
    ///
    /// Animations run on unscaled time so they keep playing while the game is paused.
    /// </summary>
    [AddComponentMenu("AudioCenter/UI/Custom Button")]
    [RequireComponent(typeof(Button))]
    public class AudioCenterCustomButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Animation Configuration")]
        [Tooltip("Shared animation preset (scales, durations, curves). When empty, sensible built-in defaults are used.")]
        [SerializeField] private AudioCenterButtonAnimationConfig animationConfig;
        [Tooltip("Play the hover scale pop when the pointer enters/exits the button.")]
        [SerializeField] private bool enableHoverAnimation = true;
        [Tooltip("Play the press-down → bounce → settle scale animation on click.")]
        [SerializeField] private bool enableClickAnimation = true;

        [Header("SFX")]
        [Tooltip("Play a UI sound on click through AudioCenterAudioManager.")]
        [SerializeField] private bool enableSFX = true;
        [Tooltip("Clip group in the AudioCenter library to play on click.")]
        [SerializeField] private string sfxGroupName = "UI";
        [Tooltip("Clip name within the group to play on click.")]
        [SerializeField] private string sfxClipName = "ButtonClick";

        [Header("Custom Events")]
        [Tooltip("Invoked after the click animation/SFX are triggered (in addition to the Button's own onClick).")]
        [SerializeField] private UnityEvent onButtonClicked;

        // Components
        private Button _button;
        private Transform _buttonTransform;
        private Vector3 _originalScale;

        // Animation state
        private Coroutine _hoverRoutine;
        private Coroutine _clickRoutine;
        private bool _isHovering;

        // Resolved at runtime: the assigned config, or a throwaway default instance.
        private AudioCenterButtonAnimationConfig _config;

        #region Unity Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();
            _buttonTransform = transform;
            _originalScale = _buttonTransform.localScale;
            ResolveConfig();
        }

        private void Start()
        {
            if (_button != null)
                _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            // Stop any in-flight animation and snap back so the button doesn't get
            // stuck mid-scale when disabled.
            KillAnimations();
            _isHovering = false;
            if (_buttonTransform != null)
                _buttonTransform.localScale = _originalScale;
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
            KillAnimations();
        }

        #endregion

        #region Configuration

        private void ResolveConfig()
        {
            _config = animationConfig != null
                ? animationConfig
                : AudioCenterButtonAnimationConfig.CreateDefault();
        }

        #endregion

        #region Button Events

        private void OnButtonClicked()
        {
            if (enableClickAnimation && isActiveAndEnabled)
                PlayClickAnimation();

            if (enableSFX)
                PlayButtonSFX();

            onButtonClicked?.Invoke();
        }

        #endregion

        #region Animation

        private void PlayClickAnimation()
        {
            if (_clickRoutine != null)
                StopCoroutine(_clickRoutine);
            _clickRoutine = StartCoroutine(Co_Click());
        }

        // Press-down → overshoot bounce → settle to original, all on unscaled time.
        private IEnumerator Co_Click()
        {
            // A click takes over the scale; cancel any hover lerp so they don't fight.
            if (_hoverRoutine != null)
            {
                StopCoroutine(_hoverRoutine);
                _hoverRoutine = null;
            }

            Vector3 from = _buttonTransform.localScale;
            Vector3 down = _originalScale * _config.clickScale;
            Vector3 over = _originalScale * _config.bounceIntensity;

            yield return ScaleOver(from, down, _config.clickDuration, _config.clickCurve);
            yield return ScaleOver(down, over, _config.bounceDuration, EaseOut());
            yield return ScaleOver(over, _originalScale, _config.settleDuration, EaseOut());

            _buttonTransform.localScale = _originalScale;
            _clickRoutine = null;
        }

        private void PlayHoverAnimation(bool isHovering)
        {
            if (!enableHoverAnimation) return;

            // Don't interrupt a click animation with a hover change; the click resolves
            // back to the original scale on its own.
            if (_clickRoutine != null) return;

            if (_hoverRoutine != null)
                StopCoroutine(_hoverRoutine);

            float targetMultiplier = isHovering ? _config.hoverScale : 1f;
            Vector3 target = _originalScale * targetMultiplier;
            _hoverRoutine = StartCoroutine(Co_Hover(target));
        }

        private IEnumerator Co_Hover(Vector3 target)
        {
            yield return ScaleOver(_buttonTransform.localScale, target, _config.hoverDuration, _config.hoverCurve);
            _hoverRoutine = null;
        }

        // Lerps localScale from -> to over duration seconds (unscaled), shaped by curve.
        private IEnumerator ScaleOver(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
        {
            if (duration <= 0f)
            {
                _buttonTransform.localScale = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = curve != null ? curve.Evaluate(Mathf.Clamp01(t / duration)) : Mathf.Clamp01(t / duration);
                _buttonTransform.localScale = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            _buttonTransform.localScale = to;
        }

        private static AnimationCurve EaseOut() => AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private void KillAnimations()
        {
            if (_hoverRoutine != null) { StopCoroutine(_hoverRoutine); _hoverRoutine = null; }
            if (_clickRoutine != null) { StopCoroutine(_clickRoutine); _clickRoutine = null; }
        }

        #endregion

        #region SFX

        private void PlayButtonSFX()
        {
            if (string.IsNullOrEmpty(sfxGroupName) || string.IsNullOrEmpty(sfxClipName))
                return;

            AudioCenterAudioManager.PlaySound(sfxGroupName, sfxClipName, AudioCenterAudioTrack.UI);
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable) return;

            _isHovering = true;
            PlayHoverAnimation(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_button == null) return;

            _isHovering = false;
            PlayHoverAnimation(false);
        }

        #endregion

        #region Public API

        /// <summary>Manually trigger a click (animation + SFX + events) if interactable.</summary>
        public void TriggerClick()
        {
            if (_button != null && _button.interactable)
                OnButtonClicked();
        }

        /// <summary>Sets interactable state, resetting any hover scale when disabled.</summary>
        public void SetInteractable(bool interactable)
        {
            if (_button == null) return;

            _button.interactable = interactable;
            if (!interactable && _isHovering)
            {
                _isHovering = false;
                PlayHoverAnimation(false);
            }
        }

        /// <summary>Switches to a different animation preset at runtime.</summary>
        public void SwitchAnimationConfig(AudioCenterButtonAnimationConfig config)
        {
            animationConfig = config;
            ResolveConfig();
        }

        public void SetHoverAnimationEnabled(bool enabled)
        {
            enableHoverAnimation = enabled;
            if (!enabled && _isHovering)
            {
                _isHovering = false;
                PlayHoverAnimation(false);
            }
        }

        public void SetClickAnimationEnabled(bool enabled) => enableClickAnimation = enabled;

        public void SetSFXEnabled(bool enabled) => enableSFX = enabled;

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_button == null)
                _button = GetComponent<Button>();
        }
#endif
    }
}

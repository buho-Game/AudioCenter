using UnityEngine;

namespace AudioCenter.UI
{
    /// <summary>
    /// Animation preset for <see cref="AudioCenterCustomButton"/> — a reusable, shareable
    /// ScriptableObject describing how a button scales on hover and click.
    ///
    /// Durations are in seconds (unscaled time). The AnimationCurves shape the easing of each
    /// phase and are sampled over [0,1]; their output value is the normalized progress used to
    /// lerp between scales. Multiple buttons can reference the same asset to share a "named"
    /// preset without any global manager.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ButtonAnimationConfig",
        menuName = "AudioCenter/UI/Button Animation Config")]
    public class AudioCenterButtonAnimationConfig : ScriptableObject
    {
        [Header("Hover")]
        [Tooltip("Scale multiplier (relative to the button's original scale) while hovered.")]
        public float hoverScale = 1.05f;

        [Tooltip("Seconds to reach the hover scale (and to return on exit).")]
        public float hoverDuration = 0.15f;

        [Tooltip("Eases the hover scale lerp over [0,1].")]
        public AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Click")]
        [Tooltip("Scale multiplier at the bottom of the click press.")]
        public float clickScale = 0.9f;

        [Tooltip("Seconds for the initial press-down phase.")]
        public float clickDuration = 0.08f;

        [Tooltip("Eases the press-down scale lerp over [0,1].")]
        public AnimationCurve clickCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Bounce")]
        [Tooltip("Scale multiplier of the overshoot after release (>1 for a pop).")]
        public float bounceIntensity = 1.08f;

        [Tooltip("Seconds for the overshoot phase.")]
        public float bounceDuration = 0.12f;

        [Tooltip("Seconds to settle back to the original scale after the overshoot.")]
        public float settleDuration = 0.08f;

        /// <summary>
        /// Builds a throwaway preset carrying the field defaults. Used by
        /// <see cref="AudioCenterCustomButton"/> as a fallback when no config asset is assigned,
        /// so a button still animates sensibly out of the box.
        /// </summary>
        public static AudioCenterButtonAnimationConfig CreateDefault()
        {
            return CreateInstance<AudioCenterButtonAnimationConfig>();
        }
    }
}

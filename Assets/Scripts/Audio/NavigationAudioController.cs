using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmartClassroomAR.UI
{
    /// <summary>
    /// Modular navigation audio controller for SmartClassroomAR.
    ///
    /// Responsibilities:
    ///   A. Footstep sounds  — played while the simulated route is auto-advancing or
    ///      stepping forward; stopped immediately on pause, cancel, or arrival.
    ///   B. Floor-arrival voice announcements — spoken exactly once per floor change
    ///      during a route; announces the floor name the user has just reached.
    ///   C. Destination arrival  — plays a success chime and a "reached destination"
    ///      voice announcement once when the final waypoint is reached.
    ///
    /// Integration:
    ///   Attach to the same GameObject as NavigationUIController (or any persistent
    ///   GameObject in the scene). Wire the public methods from NavigationUIController:
    ///     • OnStepAdvanced(NavNode currentNode, bool isAuto)
    ///     • OnNavigationStarted()
    ///     • OnNavigationCancelled()
    ///     • OnDestinationReached(string destinationName, string floorName)
    ///
    /// WebGL / Browser Audio:
    ///   Uses Unity AudioSource with procedurally generated audio clips for footsteps
    ///   and a success chime (no external files required).
    ///   Voice announcements use the Web Speech API via a JSLib bridge when available
    ///   (SpeechSynthesis), with a silent fallback when the API is unavailable.
    ///   All speech is deferred until after a user-gesture interaction has occurred
    ///   (required by browser autoplay policy); the first call to any public method
    ///   after application start is sufficient.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NavigationAudioController : MonoBehaviour
    {
        // -----------------------------------------------------------------------------------------
        // Inspector Settings
        // -----------------------------------------------------------------------------------------
        [Header("Footstep Settings")]
        [Tooltip("Volume of footstep sounds (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float footstepVolume = 0.35f;

        [Tooltip("Seconds between footstep beats.")]
        [SerializeField] private float footstepInterval = 0.46f;

        [Header("Chime Settings")]
        [Tooltip("Volume of the arrival chime (0–1).")]
        [Range(0f, 1f)]
        [SerializeField] private float chimeVolume = 0.65f;

        [Header("Speech Settings")]
        [Tooltip("Volume of browser speech synthesis (0–1). Affects Web Speech API 'volume' property.")]
        [Range(0f, 1f)]
        [SerializeField] private float speechVolume = 0.85f;

        [Tooltip("When enabled, voice announcements play using the browser's Speech Synthesis API.")]
        [SerializeField] private bool enableVoiceAnnouncements = true;

        [Header("Global Mute")]
        [Tooltip("Mutes all navigation audio.")]
        [SerializeField] private bool muteAll = false;

        // -----------------------------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------------------------
        private AudioSource _audioSource;
        private AudioClip   _footstepClip;
        private AudioClip   _chimeClip;

        private bool   _isWalking = false;
        private float  _footstepTimer = 0f;
        private string _lastAnnouncedFloorId = null;
        private bool   _routeActive = false;
        private bool   _userInteracted = false; // browser autoplay guard

        private Action    _onSpeechCompletedCallback = null;
        private Coroutine _speechTimeoutCoroutine    = null;

        // -----------------------------------------------------------------------------------------
        // Unity Lifecycle
        // -----------------------------------------------------------------------------------------
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f; // 2D (UI) sound

            _footstepClip = GenerateFootstepClip();
            _chimeClip    = GenerateChimeClip();
        }

        private void Update()
        {
            if (!_isWalking || muteAll) return;

            _footstepTimer += Time.deltaTime;
            if (_footstepTimer >= footstepInterval)
            {
                _footstepTimer = 0f;
                PlayFootstep();
            }
        }

        // -----------------------------------------------------------------------------------------
        // Public API — called from NavigationUIController
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Call when a new navigation route begins (destination selected and route confirmed).
        /// Resets floor announcement state and activates audio.
        /// </summary>
        public void OnNavigationStarted()
        {
            _userInteracted = true;
            _routeActive    = true;
            _lastAnnouncedFloorId = null;
            _isWalking      = false;
            _footstepTimer  = 0f;
        }

        /// <summary>
        /// Call each time the user-marker advances to a new node (Step Forward or Auto Simulate tick).
        /// Starts footstep audio if moving; detects floor changes and announces the new floor.
        /// </summary>
        /// <param name="floorId">floorId of the node just reached (e.g. "F0", "F1", "F2").</param>
        /// <param name="floorName">Human-readable floor name (e.g. "Ground Floor", "First Floor").</param>
        /// <param name="isAutoSimulating">True when Auto Simulate is active; used to start continuous footstep loop.</param>
        public void OnStepAdvanced(string floorId, string floorName, bool isAutoSimulating)
        {
            if (!_routeActive || muteAll) return;
            _userInteracted = true;

            // Start walking sounds if auto-simulating; single step sound for manual step
            if (isAutoSimulating)
            {
                if (!_isWalking)
                {
                    _isWalking     = true;
                    _footstepTimer = 0f;
                    PlayFootstep(); // immediate first beat
                }
            }
            else
            {
                // Manual step: single footstep, stop continuous loop
                _isWalking = false;
                PlayFootstep();
            }

            // Floor-change announcement (fire only once per new floor within a route)
            if (!string.IsNullOrEmpty(floorId) && floorId != _lastAnnouncedFloorId && _lastAnnouncedFloorId != null)
            {
                _lastAnnouncedFloorId = floorId;
                string announcement = BuildFloorAnnouncement(floorName);
                if (enableVoiceAnnouncements)
                {
                    StartCoroutine(AnnounceAfterDelay(announcement, 0.4f));
                }
            }
            else if (_lastAnnouncedFloorId == null)
            {
                // Record the starting floor without announcing it
                _lastAnnouncedFloorId = floorId;
            }
        }

        /// <summary>
        /// Plays the floor-arrival voice announcement for the reached floor and invokes onCompleted
        /// when speech synthesis actually finishes speaking (or safely times out).
        /// Synchronizes floor progression during auto-simulation so transitions never get cut off.
        /// </summary>
        public void PlayFloorAnnouncement(string floorId, string floorName, Action onCompleted)
        {
            if (muteAll || !enableVoiceAnnouncements)
            {
                _lastAnnouncedFloorId = floorId;
                onCompleted?.Invoke();
                return;
            }

            _userInteracted = true;
            _lastAnnouncedFloorId = floorId;
            string announcement = BuildFloorAnnouncement(floorName);

            // Clean up any pending callback or timeout
            if (_speechTimeoutCoroutine != null)
            {
                StopCoroutine(_speechTimeoutCoroutine);
                _speechTimeoutCoroutine = null;
            }

            _onSpeechCompletedCallback = onCompleted;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // Bounded fallback timeout (3.5s) to guarantee simulation never stalls if browser speech drops
                _speechTimeoutCoroutine = StartCoroutine(SpeechSafetyTimeout(3.5f));
                NavAudioBridge_SpeakWithCallback(announcement, speechVolume, gameObject.name, nameof(OnSpeechCallbackFromJS));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NavigationAudioController] Speech bridge error: {ex.Message}");
                TriggerSpeechComplete();
            }
#else
            Debug.Log($"[NavigationAudioController] [Voice-Floor] \"{announcement}\"");
            // Simulate realistic speech time in Editor before continuing simulation
            _speechTimeoutCoroutine = StartCoroutine(EditorSimulateSpeech(1.8f));
#endif
        }

        /// <summary>
        /// Resumes walking sound loop when auto-simulation resumes after a floor announcement.
        /// </summary>
        public void ResumeWalking()
        {
            if (!_routeActive || muteAll) return;
            _isWalking     = true;
            _footstepTimer = 0f;
        }

        /// <summary>
        /// Callback invoked by WebGL JavaScript SendMessage when SpeechSynthesis utterance finishes or errors.
        /// </summary>
        public void OnSpeechCallbackFromJS(string status)
        {
            TriggerSpeechComplete();
        }

        private void TriggerSpeechComplete()
        {
            if (_speechTimeoutCoroutine != null)
            {
                StopCoroutine(_speechTimeoutCoroutine);
                _speechTimeoutCoroutine = null;
            }

            var cb = _onSpeechCompletedCallback;
            _onSpeechCompletedCallback = null;
            cb?.Invoke();
        }

        private IEnumerator SpeechSafetyTimeout(float maxDuration)
        {
            yield return new WaitForSeconds(maxDuration);
            TriggerSpeechComplete();
        }

        private IEnumerator EditorSimulateSpeech(float duration)
        {
            yield return new WaitForSeconds(duration);
            TriggerSpeechComplete();
        }

        /// <summary>
        /// Call when Auto Simulate is paused or manually stopped (but route is still active).
        /// </summary>
        public void OnMovementPaused()
        {
            _isWalking = false;
            _footstepTimer = 0f;
        }

        /// <summary>
        /// Call when the Back / Change button cancels the active route.
        /// Stops all audio and resets state.
        /// </summary>
        public void OnNavigationCancelled()
        {
            TriggerSpeechComplete();
            StopWalking();
            _routeActive = false;
            _lastAnnouncedFloorId = null;
        }

        /// <summary>
        /// Call when the final destination waypoint is genuinely reached.
        /// Stops walking sounds, plays the arrival chime, and announces arrival.
        /// </summary>
        /// <param name="destinationName">Human-readable destination name for the spoken announcement.</param>
        public void OnDestinationReached(string destinationName)
        {
            if (muteAll) { StopWalking(); return; }

            StopWalking();
            _routeActive = false;
            _isWalking   = false;

            // Play arrival chime
            if (_chimeClip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_chimeClip, chimeVolume);
            }

            // Spoken arrival announcement after short pause to let chime start
            if (enableVoiceAnnouncements)
            {
                string msg = string.IsNullOrEmpty(destinationName)
                    ? "You have reached your destination."
                    : $"You have reached {destinationName}.";
                StartCoroutine(AnnounceAfterDelay(msg, 0.8f));
            }
        }

        /// <summary>
        /// Toggles global mute for all navigation audio.
        /// </summary>
        public void SetMuted(bool muted)
        {
            muteAll = muted;
            if (muted)
            {
                StopWalking();
                if (_audioSource != null) _audioSource.Stop();
            }
        }

        public bool IsMuted => muteAll;

        // -----------------------------------------------------------------------------------------
        // Internal helpers
        // -----------------------------------------------------------------------------------------

        private void StopWalking()
        {
            _isWalking     = false;
            _footstepTimer = 0f;
        }

        private void PlayFootstep()
        {
            if (muteAll || _footstepClip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(_footstepClip, footstepVolume);
        }

        private string BuildFloorAnnouncement(string floorName)
        {
            if (string.IsNullOrEmpty(floorName)) return "You have arrived at the next floor.";
            return $"You have arrived at the {floorName}.";
        }

        private IEnumerator AnnounceAfterDelay(string text, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!muteAll) SpeakText(text);
        }

        /// <summary>
        /// Uses the browser's Web Speech API (SpeechSynthesis) via the JSLib bridge
        /// to speak the given text. Silently no-ops on platforms without speech support
        /// or when the user has not interacted yet.
        /// </summary>
        private void SpeakText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || muteAll) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                NavAudioBridge_Speak(text, speechVolume);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NavigationAudioController] Speech synthesis not available in this browser: {ex.Message}");
            }
#else
            // Editor / standalone: log announcement instead of speaking
            Debug.Log($"[NavigationAudioController] [Voice] \"{text}\"");
#endif
        }

        // -----------------------------------------------------------------------------------------
        // Procedural Audio Generation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Generates a realistic soft footstep click using a brief exponential-decay noise burst.
        /// No external files required — fully procedural.
        /// </summary>
        private static AudioClip GenerateFootstepClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.08f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);

            var samples = new float[sampleCount];
            var rng = new System.Random(42);

            for (int i = 0; i < sampleCount; i++)
            {
                float t       = (float)i / sampleCount;
                float decay   = Mathf.Exp(-t * 38f);         // fast exponential decay
                float noise   = (float)(rng.NextDouble() * 2.0 - 1.0);
                float lowFreq = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.4f; // low thump component
                samples[i]    = (noise * 0.55f + lowFreq) * decay;
            }

            var clip = AudioClip.Create("Footstep_Procedural", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a pleasant two-tone arrival chime: a rising major third.
        /// No external files required — fully procedural.
        /// </summary>
        private static AudioClip GenerateChimeClip()
        {
            const int   sampleRate  = 44100;
            const float duration    = 1.2f;
            int         sampleCount = Mathf.CeilToInt(sampleRate * duration);

            float[] noteFreqs   = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5 (major chord)
            float[] noteStarts  = { 0.00f,   0.20f,   0.42f  }; // staggered entry in seconds
            float   noteLength  = 0.80f;
            float   attackTime  = 0.03f;

            var samples = new float[sampleCount];

            for (int n = 0; n < noteFreqs.Length; n++)
            {
                float freq  = noteFreqs[n];
                float start = noteStarts[n];
                int   startSample = Mathf.CeilToInt(start * sampleRate);

                for (int i = startSample; i < sampleCount; i++)
                {
                    float t        = (float)(i - startSample) / sampleRate;
                    if (t > noteLength) break;

                    float attack   = Mathf.Clamp01(t / attackTime);
                    float release  = Mathf.Clamp01((noteLength - t) / (noteLength * 0.55f));
                    float envelope = attack * release;

                    float sine  = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float over1 = Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f;
                    float over2 = Mathf.Sin(2f * Mathf.PI * freq * 3f * t) * 0.10f;

                    samples[i] += (sine + over1 + over2) * envelope * (1f / noteFreqs.Length);
                }
            }

            // Normalize
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            if (peak > 0.01f)
                for (int i = 0; i < samples.Length; i++) samples[i] /= peak;

            var clip = AudioClip.Create("ArrivalChime_Procedural", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // -----------------------------------------------------------------------------------------
        // WebGL JSLib bridge declaration (implemented in NavigationAudio.jslib)
        // -----------------------------------------------------------------------------------------
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void NavAudioBridge_Speak(string text, float volume);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void NavAudioBridge_SpeakWithCallback(string text, float volume, string gameObjectName, string callbackMethodName);
#endif
    }
}

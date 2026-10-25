using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SmartClassroomAR.Evaluation
{
    /// <summary>
    /// User interface manager for conducting structured empirical trials,
    /// recording navigation completion times, and capturing Likert questionnaire feedback.
    /// Compatible with Unity 2022.3 LTS and TextMeshPro.
    /// </summary>
    public class EvaluationUI : MonoBehaviour
    {
        [Header("Panel Containers")]
        [Tooltip("Panel displaying live trial status, participant ID input, and timer.")]
        [SerializeField] private GameObject sessionPanel;

        [Tooltip("Panel displaying usability Likert rating sliders and comments.")]
        [SerializeField] private GameObject feedbackPanel;

        [Header("Session Tracking UI")]
        [SerializeField] private TMP_InputField participantIdInput;
        [SerializeField] private TMP_Text sessionInfoText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button startSessionButton;
        [SerializeField] private Button stopSessionButton;

        [Header("Feedback Questionnaire Sliders (1 - 5)")]
        [SerializeField] private Slider arrowClaritySlider;
        [SerializeField] private Slider destinationEaseSlider;
        [SerializeField] private Slider overallUsabilitySlider;
        [SerializeField] private Slider navigationAccuracySlider;
        [SerializeField] private Slider routeFollowingSlider;
        [SerializeField] private TMP_InputField commentsInputField;
        [SerializeField] private Button submitFeedbackButton;

        [Header("Export Controls")]
        [SerializeField] private Button exportButton;
        [SerializeField] private TMP_Text exportStatusText;

        public EvaluationManager Manager { get; private set; }

        private TestSession _activeSession;
        private bool _isTimerRunning;
        private float _elapsedSeconds;

        private void Awake()
        {
            Manager = new EvaluationManager();

            ConfigureSliders();

            if (startSessionButton != null)
            {
                startSessionButton.onClick.AddListener(HandleStartSessionClicked);
            }
            if (stopSessionButton != null)
            {
                stopSessionButton.onClick.AddListener(StopEvaluationSession);
            }
            if (submitFeedbackButton != null)
            {
                submitFeedbackButton.onClick.AddListener(SubmitFeedback);
            }
            if (exportButton != null)
            {
                exportButton.onClick.AddListener(ExportEvaluationData);
            }

            ShowSessionPanel();
        }

        private void OnDestroy()
        {
            if (startSessionButton != null)
            {
                startSessionButton.onClick.RemoveListener(HandleStartSessionClicked);
            }
            if (stopSessionButton != null)
            {
                stopSessionButton.onClick.RemoveListener(StopEvaluationSession);
            }
            if (submitFeedbackButton != null)
            {
                submitFeedbackButton.onClick.RemoveListener(SubmitFeedback);
            }
            if (exportButton != null)
            {
                exportButton.onClick.RemoveListener(ExportEvaluationData);
            }
        }

        private void Update()
        {
            if (_isTimerRunning)
            {
                _elapsedSeconds += Time.deltaTime;
                if (timerText != null)
                {
                    timerText.text = $"Trial Time: {_elapsedSeconds:F1}s";
                }
            }
        }

        private void ConfigureSliders()
        {
            ConfigureSingleSlider(arrowClaritySlider);
            ConfigureSingleSlider(destinationEaseSlider);
            ConfigureSingleSlider(overallUsabilitySlider);
            ConfigureSingleSlider(navigationAccuracySlider);
            ConfigureSingleSlider(routeFollowingSlider);
        }

        private void ConfigureSingleSlider(Slider slider)
        {
            if (slider != null)
            {
                slider.minValue = 1f;
                slider.maxValue = 5f;
                slider.wholeNumbers = true;
                slider.value = 5f;
            }
        }

        private void HandleStartSessionClicked()
        {
            string participantId = participantIdInput != null && !string.IsNullOrWhiteSpace(participantIdInput.text)
                ? participantIdInput.text.Trim()
                : $"P_{UnityEngine.Random.Range(100, 999)}";

            StartEvaluationSession(participantId);
        }

        /// <summary>
        /// Begins tracking a new evaluation trial.
        /// </summary>
        public void StartEvaluationSession(string participantId)
        {
            _elapsedSeconds = 0f;
            _isTimerRunning = true;

            _activeSession = Manager.StartSession(participantId, "START_LOC", "DEST_LOC");

            if (sessionInfoText != null)
            {
                sessionInfoText.text = $"Active Session: {_activeSession.sessionId.Substring(0, 8)}... (Participant: {participantId})";
            }

            if (startSessionButton != null) startSessionButton.interactable = false;
            if (stopSessionButton != null) stopSessionButton.interactable = true;

            ShowSessionPanel();
            Debug.Log($"[EvaluationUI] Started evaluation trial for participant '{participantId}'.");
        }

        /// <summary>
        /// Concludes the active trial and presents the Likert questionnaire.
        /// </summary>
        public void StopEvaluationSession()
        {
            if (!_isTimerRunning && _activeSession == null) return;

            _isTimerRunning = false;

            if (_activeSession != null)
            {
                Manager.EndSession(_activeSession, completed: true, correctDestination: true);
            }

            if (startSessionButton != null) startSessionButton.interactable = true;
            if (stopSessionButton != null) stopSessionButton.interactable = false;

            ShowFeedbackForm();
        }

        /// <summary>
        /// Displays the feedback questionnaire form.
        /// </summary>
        public void ShowFeedbackForm()
        {
            if (sessionPanel != null) sessionPanel.SetActive(false);
            if (feedbackPanel != null) feedbackPanel.SetActive(true);
        }

        /// <summary>
        /// Displays the session recording panel.
        /// </summary>
        public void ShowSessionPanel()
        {
            if (sessionPanel != null) sessionPanel.SetActive(true);
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
        }

        /// <summary>
        /// Reads participant ratings from UI inputs, records feedback, and resets form.
        /// </summary>
        public void SubmitFeedback()
        {
            var feedback = new UserFeedback
            {
                sessionId = _activeSession != null ? _activeSession.sessionId : "UNSPECIFIED",
                arrowClarityRating = arrowClaritySlider != null ? Mathf.RoundToInt(arrowClaritySlider.value) : 5,
                destinationSelectionEaseRating = destinationEaseSlider != null ? Mathf.RoundToInt(destinationEaseSlider.value) : 5,
                overallUsabilityRating = overallUsabilitySlider != null ? Mathf.RoundToInt(overallUsabilitySlider.value) : 5,
                navigationAccuracyRating = navigationAccuracySlider != null ? Mathf.RoundToInt(navigationAccuracySlider.value) : 5,
                routeFollowingEaseRating = routeFollowingSlider != null ? Mathf.RoundToInt(routeFollowingSlider.value) : 5,
                additionalComments = commentsInputField != null ? commentsInputField.text : string.Empty
            };

            Manager.RecordFeedback(feedback);

            if (commentsInputField != null)
            {
                commentsInputField.text = string.Empty;
            }

            if (sessionInfoText != null)
            {
                sessionInfoText.text = $"Trial finalized ({_elapsedSeconds:F1}s). Feedback saved.";
            }

            ShowSessionPanel();
            Debug.Log("[EvaluationUI] Feedback successfully submitted and archived.");
        }

        /// <summary>
        /// Exports all trial sessions and questionnaire responses to JSON files on disk.
        /// </summary>
        public void ExportEvaluationData()
        {
            try
            {
                string sessionsJson = Manager.ExportSessionsJson();
                string feedbackJson = Manager.ExportFeedbackJson();

                string sessionsPath = Path.Combine(Application.persistentDataPath, "ar_evaluation_sessions.json");
                string feedbackPath = Path.Combine(Application.persistentDataPath, "ar_evaluation_feedback.json");

                File.WriteAllText(sessionsPath, sessionsJson);
                File.WriteAllText(feedbackPath, feedbackJson);

                string statusMsg = $"Exported to:\n{sessionsPath}\n{feedbackPath}";
                Debug.Log($"[EvaluationUI] {statusMsg}");

                if (exportStatusText != null)
                {
                    exportStatusText.text = $"Saved {Manager.Sessions.Count} sessions & {Manager.FeedbackList.Count} responses to disk.";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EvaluationUI] Export failed: {ex.Message}");
                if (exportStatusText != null)
                {
                    exportStatusText.text = $"Export error: {ex.Message}";
                }
            }
        }
    }
}

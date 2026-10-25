using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartClassroomAR.Evaluation
{
    /// <summary>
    /// Represents an evaluation trial session logging user navigation performance metrics.
    /// Non-MonoBehaviour data container.
    /// </summary>
    [Serializable]
    public class TestSession
    {
        public string sessionId = string.Empty;
        public string participantId = string.Empty;
        public string startTimeIso = string.Empty;
        public string endTimeIso = string.Empty;
        public string startNodeId = string.Empty;
        public string destinationId = string.Empty;
        public string expectedPath = string.Empty;
        public string actualPath = string.Empty;
        public bool navigationCompleted = false;
        public bool correctDestinationReached = false;
        public float navigationTimeSeconds = 0f;
        public string arTrackingQuality = "Good";
        public float responseTimeSeconds = 0f;
        public string notes = string.Empty;

        // DateTime accessors
        [NonSerialized]
        private DateTime _startTime;
        [NonSerialized]
        private DateTime _endTime;

        public DateTime startTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                startTimeIso = value.ToString("o");
            }
        }

        public DateTime endTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                endTimeIso = value.ToString("o");
            }
        }
    }

    /// <summary>
    /// Qualitative questionnaire responses recorded from test participants following a trial.
    /// Non-MonoBehaviour data container.
    /// </summary>
    [Serializable]
    public class UserFeedback
    {
        public string sessionId = string.Empty;
        public int arrowClarityRating = 5;
        public int destinationSelectionEaseRating = 5;
        public int overallUsabilityRating = 5;
        public int navigationAccuracyRating = 5;
        public int routeFollowingEaseRating = 5;
        public string additionalComments = string.Empty;
    }

    [Serializable]
    internal class SessionContainerWrapper
    {
        public List<TestSession> sessions = new List<TestSession>();
    }

    [Serializable]
    internal class FeedbackContainerWrapper
    {
        public List<UserFeedback> feedback = new List<UserFeedback>();
    }

    /// <summary>
    /// Manages the recording, compilation, and JSON export of user study evaluation trials
    /// and usability feedback questionnaires for the Smart Classroom AR navigation system.
    /// Pure C# class without MonoBehaviour dependency.
    /// </summary>
    public class EvaluationManager
    {
        private readonly List<TestSession> _sessions = new List<TestSession>();
        private readonly List<UserFeedback> _feedbackList = new List<UserFeedback>();

        public IReadOnlyList<TestSession> Sessions => _sessions;
        public IReadOnlyList<UserFeedback> FeedbackList => _feedbackList;

        /// <summary>
        /// Begins a new test session for an anonymous participant.
        /// </summary>
        public TestSession StartSession(string participantId, string startNodeId, string destinationId)
        {
            var session = new TestSession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                participantId = participantId ?? "Anonymous",
                startNodeId = startNodeId ?? string.Empty,
                destinationId = destinationId ?? string.Empty,
                startTime = DateTime.UtcNow,
                navigationCompleted = false,
                correctDestinationReached = false,
                navigationTimeSeconds = 0f,
                responseTimeSeconds = 0f,
                notes = string.Empty
            };

            _sessions.Add(session);
            Debug.Log($"[EvaluationManager] Started session '{session.sessionId}' for participant '{session.participantId}'.");
            return session;
        }

        /// <summary>
        /// Finalizes a test session with trial outcome and duration.
        /// </summary>
        public void EndSession(TestSession session, bool completed, bool correctDestination)
        {
            if (session == null)
            {
                Debug.LogWarning("[EvaluationManager] EndSession called with null session.");
                return;
            }

            session.endTime = DateTime.UtcNow;
            session.navigationCompleted = completed;
            session.correctDestinationReached = correctDestination;

            if (session.startTime != default)
            {
                session.navigationTimeSeconds = (float)(session.endTime - session.startTime).TotalSeconds;
            }

            Debug.Log($"[EvaluationManager] Ended session '{session.sessionId}'. Completed: {completed}, Correct: {correctDestination}, Duration: {session.navigationTimeSeconds:F1}s.");
        }

        /// <summary>
        /// Records user questionnaire feedback for a completed navigation trial.
        /// </summary>
        public void RecordFeedback(UserFeedback feedback)
        {
            if (feedback == null)
            {
                Debug.LogWarning("[EvaluationManager] RecordFeedback called with null feedback.");
                return;
            }

            // Ensure ratings adhere strictly to 1-5 Likert scale
            feedback.arrowClarityRating = Mathf.Clamp(feedback.arrowClarityRating, 1, 5);
            feedback.destinationSelectionEaseRating = Mathf.Clamp(feedback.destinationSelectionEaseRating, 1, 5);
            feedback.overallUsabilityRating = Mathf.Clamp(feedback.overallUsabilityRating, 1, 5);
            feedback.navigationAccuracyRating = Mathf.Clamp(feedback.navigationAccuracyRating, 1, 5);
            feedback.routeFollowingEaseRating = Mathf.Clamp(feedback.routeFollowingEaseRating, 1, 5);

            _feedbackList.Add(feedback);
            Debug.Log($"[EvaluationManager] Recorded user feedback for session '{feedback.sessionId}'. Usability rating: {feedback.overallUsabilityRating}/5.");
        }

        /// <summary>
        /// Exports all registered test sessions into formatted JSON string.
        /// </summary>
        public string ExportSessionsJson()
        {
            var wrapper = new SessionContainerWrapper
            {
                sessions = new List<TestSession>(_sessions)
            };
            return JsonUtility.ToJson(wrapper, true);
        }

        /// <summary>
        /// Exports all registered user feedback records into formatted JSON string.
        /// </summary>
        public string ExportFeedbackJson()
        {
            var wrapper = new FeedbackContainerWrapper
            {
                feedback = new List<UserFeedback>(_feedbackList)
            };
            return JsonUtility.ToJson(wrapper, true);
        }
    }
}

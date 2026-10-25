// =============================================================================
// TestRunner.cs - Standalone test runner for NavigationTests
// Can be run from Unity Editor or as a console application
// =============================================================================

using System;

namespace SmartClassroomAR.Tests
{
    /// <summary>
    /// Entry point for running navigation tests.
    /// In Unity, attach this to a GameObject in a test scene.
    /// Standalone, run the Main method.
    /// </summary>
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS
    public class TestRunner : UnityEngine.MonoBehaviour
    {
        [UnityEngine.SerializeField]
        private bool runOnStart = true;

        [UnityEngine.SerializeField]
        private UnityEngine.UI.Text resultText;

        private void Start()
        {
            if (runOnStart)
            {
                RunTests();
            }
        }

        public void RunTests()
        {
            NavigationTests tests = new NavigationTests();
            string results = tests.RunAllTests();

            if (resultText != null)
            {
                resultText.text = results;
            }

            UnityEngine.Debug.Log(results);
        }
    }
#else
    public class TestRunner
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Navigation Tests...\n");

            NavigationTests tests = new NavigationTests();
            string results = tests.RunAllTests();

            Console.WriteLine(results);

            // Exit with code 1 if any tests failed
            if (results.Contains("FAIL"))
            {
                Environment.Exit(1);
            }
        }
    }
#endif
}

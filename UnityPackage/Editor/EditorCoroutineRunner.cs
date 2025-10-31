using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple coroutine runner for Unity Editor
    /// </summary>
    public static class EditorCoroutineRunner
    {
        private class EditorCoroutine
        {
            public IEnumerator Enumerator;
            public bool IsRunning = true;

            public bool MoveNext()
            {
                if (!IsRunning) return false;

                try
                {
                    return Enumerator.MoveNext();
                }
                catch (System.Exception)
                {
                    IsRunning = false;
                    throw;
                }
            }
        }

        private static readonly List<EditorCoroutine> activeCoroutines = new List<EditorCoroutine>();

        static EditorCoroutineRunner()
        {
            EditorApplication.update += Update;
        }

        public static void StartCoroutine(IEnumerator enumerator)
        {
            var coroutine = new EditorCoroutine { Enumerator = enumerator };
            activeCoroutines.Add(coroutine);
        }

        private static void Update()
        {
            for (int i = activeCoroutines.Count - 1; i >= 0; i--)
            {
                var coroutine = activeCoroutines[i];

                if (!coroutine.IsRunning || !coroutine.MoveNext())
                {
                    activeCoroutines.RemoveAt(i);
                }
            }
        }
    }
}

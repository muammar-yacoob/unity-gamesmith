using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Database of available AI providers for easy selection
    /// </summary>
    [CreateAssetMenu(fileName = "AI Provider Database", menuName = "Game Smith/AI Provider Database", order = 0)]
    public class AIProviderDatabase : ScriptableObject
    {
        [Header("Available Providers")]
        [Tooltip("List of all configured AI providers")]
        public List<AIProvider> providers = new List<AIProvider>();

        [Header("Default Selection")]
        [Tooltip("Index of the default provider to use")]
        public int defaultProviderIndex = 0;

        /// <summary>
        /// Gets the default provider
        /// </summary>
        public AIProvider GetDefaultProvider()
        {
            if (providers == null || providers.Count == 0)
                return null;

            if (defaultProviderIndex >= 0 && defaultProviderIndex < providers.Count)
                return providers[defaultProviderIndex];

            return providers[0];
        }

        /// <summary>
        /// Gets a provider by name
        /// </summary>
        public AIProvider GetProviderByName(string name)
        {
            return providers?.FirstOrDefault(p => p.providerName == name);
        }

        /// <summary>
        /// Gets a provider by index
        /// </summary>
        public AIProvider GetProviderByIndex(int index)
        {
            if (providers == null || index < 0 || index >= providers.Count)
                return null;
            return providers[index];
        }

        /// <summary>
        /// Gets all provider names for dropdown
        /// </summary>
        public string[] GetProviderNames()
        {
            if (providers == null || providers.Count == 0)
                return new string[] { "No providers configured" };

            return providers.Select(p => p.providerName).ToArray();
        }

        /// <summary>
        /// Gets index of a provider by name
        /// </summary>
        public int GetProviderIndex(string name)
        {
            if (providers == null) return -1;
            return providers.FindIndex(p => p.providerName == name);
        }

        /// <summary>
        /// Adds a new provider to the database
        /// </summary>
        public void AddProvider(AIProvider provider)
        {
            if (providers == null)
                providers = new List<AIProvider>();

            if (!providers.Contains(provider))
            {
                providers.Add(provider);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// Removes a provider from the database
        /// </summary>
        public void RemoveProvider(AIProvider provider)
        {
            if (providers != null && providers.Contains(provider))
            {
                providers.Remove(provider);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// Validates all providers
        /// </summary>
        public List<string> ValidateProviders()
        {
            var issues = new List<string>();

            if (providers == null || providers.Count == 0)
            {
                issues.Add("No providers configured");
                return issues;
            }

            for (int i = 0; i < providers.Count; i++)
            {
                var provider = providers[i];
                if (provider == null)
                {
                    issues.Add($"Provider at index {i} is null");
                    continue;
                }

                if (!provider.IsValid())
                {
                    issues.Add($"{provider.providerName}: {provider.GetValidationMessage()}");
                }
            }

            return issues;
        }
    }
}

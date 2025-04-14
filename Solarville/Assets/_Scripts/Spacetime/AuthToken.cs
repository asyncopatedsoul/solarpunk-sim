using UnityEngine;

namespace Solarville.Spacetime
{
    /// <summary>
    /// Helper class to store and retrieve SpacetimeDB authentication tokens
    /// </summary>
    public static class AuthToken
    {
        private const string AUTH_TOKEN_KEY = "SpacetimeDB_AuthToken";

        /// <summary>
        /// Gets the current auth token from PlayerPrefs, or an empty string if none exists
        /// </summary>
        public static string Token
        {
            get => PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");
        }

        /// <summary>
        /// Saves the token to PlayerPrefs
        /// </summary>
        public static void SaveToken(string token)
        {
            PlayerPrefs.SetString(AUTH_TOKEN_KEY, token);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clears the token from PlayerPrefs
        /// </summary>
        public static void ClearToken()
        {
            PlayerPrefs.DeleteKey(AUTH_TOKEN_KEY);
            PlayerPrefs.Save();
        }
    }
}

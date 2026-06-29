using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GlobalLeaderboard
{
    /// <summary>
    /// Unity client that talks to the Leaderboard API.
    /// Add it to an empty GameObject and fill in baseUrl / apiKey / hmacSecret in the
    /// Inspector. (These three values must match the server's appsettings.json.)
    /// </summary>
    public class LeaderboardClient : MonoBehaviour
    {
        [Header("Server Settings")]
        [Tooltip("e.g. https://my-server.azurewebsites.net (a trailing / is fine)")]
        public string baseUrl = "https://your-server.com";

        [Tooltip("Must match Leaderboard:ApiKey on the server")]
        public string apiKey = "CHANGE_ME_API_KEY";

        [Header("Security")]
        [Tooltip("Must match Leaderboard:HmacSecret on the server. " +
                 "WARNING: a secret embedded in the client can be extracted by a " +
                 "determined attacker. This stops casual cheating / Postman abuse, " +
                 "not every attack.")]
        public string hmacSecret = "CHANGE_ME_HMAC_SECRET";

        /// <summary>
        /// Submits a score. Call as a coroutine:
        /// StartCoroutine(client.SubmitScore(id, name, score, onSuccess, onError));
        /// </summary>
        public IEnumerator SubmitScore(string playerId, string playerName, long score,
            Action<SubmitScoreResponse> onSuccess = null, Action<string> onError = null)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string signature = LeaderboardHmac.Compute(hmacSecret, playerId, score, timestamp);

            var payload = new SubmitScoreRequest
            {
                playerId = playerId,
                playerName = playerName,
                score = score,
                timestamp = timestamp,
                signature = signature
            };
            string json = JsonUtility.ToJson(payload);

            using (var req = new UnityWebRequest(BuildUrl("/scores"), "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Api-Key", apiKey);

                yield return req.SendWebRequest();

                if (IsError(req))
                {
                    onError?.Invoke(req.responseCode + ": " + req.downloadHandler.text);
                }
                else
                {
                    var resp = JsonUtility.FromJson<SubmitScoreResponse>(req.downloadHandler.text);
                    onSuccess?.Invoke(resp);
                }
            }
        }

        /// <summary>Fetches the top N scores.</summary>
        public IEnumerator GetTopScores(int count,
            Action<ScoreView[]> onSuccess = null, Action<string> onError = null)
        {
            using (var req = UnityWebRequest.Get(BuildUrl("/scores/top?count=" + count)))
            {
                req.SetRequestHeader("X-Api-Key", apiKey);
                yield return req.SendWebRequest();

                if (IsError(req))
                {
                    onError?.Invoke(req.responseCode + ": " + req.downloadHandler.text);
                }
                else
                {
                    var wrapper = JsonUtility.FromJson<ScoreListWrapper>(req.downloadHandler.text);
                    onSuccess?.Invoke(wrapper != null && wrapper.items != null
                        ? wrapper.items
                        : new ScoreView[0]);
                }
            }
        }

        /// <summary>Fetches a single player's rank.</summary>
        public IEnumerator GetPlayerRank(string playerId,
            Action<ScoreView> onSuccess = null, Action<string> onError = null)
        {
            using (var req = UnityWebRequest.Get(BuildUrl("/scores/rank/" + UnityWebRequest.EscapeURL(playerId))))
            {
                req.SetRequestHeader("X-Api-Key", apiKey);
                yield return req.SendWebRequest();

                if (IsError(req))
                {
                    onError?.Invoke(req.responseCode + ": " + req.downloadHandler.text);
                }
                else
                {
                    var view = JsonUtility.FromJson<ScoreView>(req.downloadHandler.text);
                    onSuccess?.Invoke(view);
                }
            }
        }

        private string BuildUrl(string path)
        {
            return baseUrl.TrimEnd('/') + path;
        }

        private static bool IsError(UnityWebRequest req)
        {
#if UNITY_2020_1_OR_NEWER
            return req.result != UnityWebRequest.Result.Success;
#else
            return req.isNetworkError || req.isHttpError;
#endif
        }
    }
}

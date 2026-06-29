using UnityEngine;

namespace GlobalLeaderboard
{
    /// <summary>
    /// Simple console-only example. Add it to the SAME GameObject as LeaderboardClient,
    /// press Play and watch the Console. For an on-screen UI, use LeaderboardUI instead.
    /// </summary>
    [RequireComponent(typeof(LeaderboardClient))]
    public class LeaderboardDemo : MonoBehaviour
    {
        private LeaderboardClient _client;

        private void Awake()
        {
            _client = GetComponent<LeaderboardClient>();
        }

        private void Start()
        {
            // A stable id per device (use your real player id in a real game)
            string playerId = SystemInfo.deviceUniqueIdentifier;
            string playerName = "Player";
            long score = Random.Range(100, 5000);

            // 1) Submit a score
            StartCoroutine(_client.SubmitScore(playerId, playerName, score,
                resp => Debug.Log($"[Leaderboard] Submitted -> accepted={resp.accepted}, rank={resp.rank}, message={resp.message}"),
                err => Debug.LogError("[Leaderboard] Submit error: " + err)));

            // 2) Fetch the top 10
            StartCoroutine(_client.GetTopScores(10,
                scores =>
                {
                    Debug.Log("[Leaderboard] === TOP SCORES ===");
                    foreach (var s in scores)
                    {
                        Debug.Log($"  #{s.rank}  {s.playerName} : {s.score}");
                    }
                },
                err => Debug.LogError("[Leaderboard] Top error: " + err)));
        }
    }
}

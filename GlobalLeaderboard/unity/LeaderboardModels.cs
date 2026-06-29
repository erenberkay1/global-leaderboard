using System;

namespace GlobalLeaderboard
{
    // NOTE: JsonUtility matches field names exactly. The server returns camelCase
    // JSON, so these field names must stay camelCase. DO NOT RENAME.

    [Serializable]
    public class SubmitScoreRequest
    {
        public string playerId;
        public string playerName;
        public long score;
        public long timestamp;
        public string signature;
    }

    [Serializable]
    public class SubmitScoreResponse
    {
        public bool accepted;
        public int rank;
        public string message;
    }

    [Serializable]
    public class ScoreView
    {
        public int rank;
        public string playerId;
        public string playerName;
        public long score;
        public string createdAtUtc;
    }

    // JsonUtility cannot deserialize a top-level array, so the server wraps the
    // /scores/top response as { "items": [...] }.
    [Serializable]
    public class ScoreListWrapper
    {
        public ScoreView[] items;
    }
}

namespace LiveSplit.Teslagrad2
{
    public class SplitEntry
    {
        public const float DEFAULT_RADIUS = 5f;

        public SplitType Type { get; set; }
        public int ScrollId { get; set; }
        public string SceneName { get; set; }
        public string SegmentName { get; set; }

        // PositionReached: center and half-size of the square to enter
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Radius { get; set; } = DEFAULT_RADIUS;

        public SplitEntry(SplitType type, int scrollId = 0, string sceneName = "", string segmentName = null)
        {
            Type = type;
            ScrollId = scrollId;
            SceneName = sceneName ?? "";
            SegmentName = segmentName;
        }
    }
}

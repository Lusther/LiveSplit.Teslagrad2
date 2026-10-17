namespace LiveSplit.Teslagrad2
{
    public class SplitEntry
    {
        public SplitType Type { get; set; }
        public int ScrollId { get; set; }
        public string SceneName { get; set; }
        public string TriggerName { get; set; }
        public string SegmentName { get; set; }

        public SplitEntry(SplitType type, int scrollId = 0, string sceneName = "", string triggerName = "", string segmentName = null)
        {
            Type = type;
            ScrollId = scrollId;
            SceneName = sceneName ?? "";
            TriggerName = triggerName ?? "";
            SegmentName = segmentName;
        }
    }
}

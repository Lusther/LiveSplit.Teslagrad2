namespace LiveSplit.Teslagrad2
{
    public class SplitEntry
    {
        public SplitType Type { get; set; }
        public int ScrollId { get; set; }
        public string SceneName { get; set; }

        public SplitEntry(SplitType type, int scrollId = 0, string sceneName = "")
        {
            Type = type;
            ScrollId = scrollId;
            SceneName = sceneName ?? "";
        }
    }
}

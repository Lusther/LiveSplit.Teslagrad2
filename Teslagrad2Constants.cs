using System.Collections.Generic;
using System.Linq;
using LiveSplit.UI.Components;
using static System.Math;

namespace LiveSplit.Teslagrad2
{
    public enum GameVersion
    {
        Unknown,
        Public,
        SpeedrunPatch
    }

    public class VersionOffsets
    {
        public int BaseSaveDataSlot { get; }
        public int BaseSceneHandler { get; }
        public int BaseSaveDataFile { get; }

        public VersionOffsets(int saveDataSlot, int sceneHandler, int saveDataFile)
        {
            BaseSaveDataSlot = saveDataSlot;
            BaseSceneHandler = sceneHandler;
            BaseSaveDataFile = saveDataFile;
        }
    }

    public enum ScrollCollection
    {
        Vikings,
        Teslamancers,
        Goo,
        Clash,
        Huldr,
        Lumina,
        None
    }

    public class ScrollCollectionTotal
    {
        public int CollectionTotal {get; }
        public int CumulatedTotalBefore {get; }
        public int CumulatedTotalAfter {get; }

        public ScrollCollectionTotal(int collectionTotal, int cumulatedTotalBefore, int cumulatedTotalAfter)
        {
            CollectionTotal = collectionTotal;
            CumulatedTotalBefore = cumulatedTotalBefore;
            CumulatedTotalAfter = cumulatedTotalAfter;
        }
    }


    public static class Teslagrad2Constants
    {
        public static readonly Dictionary<GameVersion, VersionOffsets> Versions =
            new Dictionary<GameVersion, VersionOffsets>
        {
            // Build 20406723, module size 48156672
            { GameVersion.Public, new VersionOffsets(0x2775B38, 0x273C230, 0x274A468) },
            // Build 17572578, module size 47239168
            { GameVersion.SpeedrunPatch, new VersionOffsets(0x02667C18, 0x0269B9F8, 0x0263BE40) }
        };

        public static GameVersion DetectVersion(int moduleSize)
        {
            switch (moduleSize)
            {
                case 48156672: return GameVersion.Public;
                case 47239168: return GameVersion.SpeedrunPatch;
                default: return GameVersion.Unknown;
            }
        }

        public static string GetVersionName(GameVersion version)
        {
            switch (version)
            {
                case GameVersion.Public: return "public";
                case GameVersion.SpeedrunPatch: return "old_version_for_speedrunning";
                default: return "unknown";
            }
        }

        public static readonly Dictionary<ScrollCollection, ScrollCollectionTotal> ScrollCollectionTotals = new Dictionary<ScrollCollection, ScrollCollectionTotal>
        {
            { ScrollCollection.Vikings,       new ScrollCollectionTotal( 9,  0,  9) },
            { ScrollCollection.Teslamancers,  new ScrollCollectionTotal( 6,  9, 15) },
            { ScrollCollection.Goo,           new ScrollCollectionTotal(15, 15, 30) },
            { ScrollCollection.Clash,         new ScrollCollectionTotal(27, 30, 57) },
            { ScrollCollection.Huldr,         new ScrollCollectionTotal(12, 57, 69) },
            { ScrollCollection.Lumina,        new ScrollCollectionTotal(12, 69, 81) }
            // Note: The None case is dealt with in the helpers, which have default values if totals are not found
        };

        public static readonly int TotalScrollCount = ScrollCollectionTotals.Values.Sum(t => t.CollectionTotal);

        static Teslagrad2Constants()
        {
            ValidateScrollCollectionTotals();
        }

        private static void ValidateScrollCollectionTotals()
        {
            int runningTotal = 0;
            bool valid = true;

            foreach (var entry in ScrollCollectionTotals.OrderBy(e => e.Value.CumulatedTotalBefore))
            {
                var totals = entry.Value;
                if (totals.CumulatedTotalBefore != runningTotal)
                {
                    Log.Error($"Invalid scroll totals for {entry.Key}: expected CumulatedTotalBefore={runningTotal}, got {totals.CumulatedTotalBefore}.");
                    valid = false;
                }
                if (totals.CumulatedTotalAfter != runningTotal + totals.CollectionTotal)
                {
                    Log.Error($"Invalid scroll totals for {entry.Key}: expected CumulatedTotalAfter={runningTotal + totals.CollectionTotal}, got {totals.CumulatedTotalAfter}.");
                    valid = false;
                }
                runningTotal = totals.CumulatedTotalAfter;
            }

            if (runningTotal != TotalScrollCount)
            {
                Log.Error($"Invalid total scroll count: computed {runningTotal}, expected {TotalScrollCount}.");
                valid = false;
            }

            if (!valid)
            {
                Log.Error("Scroll collection totals are inconsistent. Check Teslagrad2Constants.ScrollCollectionTotals.");
            }
            else
            {
                Log.Info("Scroll collection totals validated successfully.");
            }
        }

        public static ScrollCollection GetScrollCollectionFromID(int scrollId)
        {
            foreach (var collection in ScrollCollectionTotals)
            {
                if (scrollId > collection.Value.CumulatedTotalBefore && scrollId <= collection.Value.CumulatedTotalAfter)
                    return collection.Key;
            }
            return ScrollCollection.None;
        }

        public static ScrollCollection GetScrollCollectionFromIDOrDefault(int scrollId, ScrollCollection defaultCollection = ScrollCollection.Vikings)
        {
            var collection = GetScrollCollectionFromID(scrollId);
            return collection == ScrollCollection.None ? defaultCollection : collection;
        }

        public static int GetScrollTotalNumber()
        {
            return TotalScrollCount;
        }

        public static int GetScrollCollectionTotal(ScrollCollection collection)
        {
            if (ScrollCollectionTotals.TryGetValue(collection, out var totals))
                return totals.CollectionTotal;
            return 0;
        }

        public static int GetScrollNumberInCollection(int scrollId, ScrollCollection collection, int defaultNumber = 1)
        {
            if (!ScrollCollectionTotals.TryGetValue(collection, out var totals))
                return defaultNumber;
            int number = scrollId - totals.CumulatedTotalBefore;
            if (number < 1 || number > totals.CollectionTotal)
                return defaultNumber;
            return number;
        }

        public static int GetScrollIdForCollectionNumber(ScrollCollection collection, int number)
        {
            if (!ScrollCollectionTotals.TryGetValue(collection, out var totals))
                return number;
            number = Max(1, Min(number, totals.CollectionTotal));
            return totals.CumulatedTotalBefore + number;
        }
    }

    public enum SplitType
    {
        StartTimer,
        ManualSplit,

        // Skills
        Blink,
        BlueCloak,
        Waterblink,
        Mjolnir,
        PowerSlide,
        Axe,
        BlinkWireAxe,
        RedCloak,
        OmniBlink,
        DoubleJump,
        SecretsMap,
        Map,

        // Bosses
        Hulder,
        Moose,
        Fafnir,
        Halvtann,
        Galvan,
        Elenor,
        Troll,

        // Scrolls
        Scrolls,
        ScrollsByCollection,

        // Scene
        SceneEntered
    }

    public static class SplitTypeExtensions
    {
        public static string GetDisplayName(this SplitType type)
        {
            switch (type)
            {
                case SplitType.StartTimer: return "Start Timer";
                case SplitType.ManualSplit: return "Manual Split";
                case SplitType.Blink: return "Blink";
                case SplitType.BlueCloak: return "Blue Cloak";
                case SplitType.Waterblink: return "Water Blink";
                case SplitType.Mjolnir: return "Mjolnir";
                case SplitType.PowerSlide: return "Power Slide";
                case SplitType.Axe: return "Axe";
                case SplitType.BlinkWireAxe: return "Blink Wire Axe";
                case SplitType.RedCloak: return "Red Cloak";
                case SplitType.OmniBlink: return "OmniBlink";
                case SplitType.DoubleJump: return "Double Jump";
                case SplitType.SecretsMap: return "Secrets Map";
                case SplitType.Map: return "Map";
                case SplitType.Hulder: return "Hulder";
                case SplitType.Moose: return "Moose";
                case SplitType.Fafnir: return "Fafnir";
                case SplitType.Halvtann: return "Halvtann";
                case SplitType.Galvan: return "Galvan";
                case SplitType.Elenor: return "Elenor";
                case SplitType.Troll: return "Troll";
                case SplitType.Scrolls: return "Scrolls";
                case SplitType.ScrollsByCollection: return "Scrolls (by Collection)";
                case SplitType.SceneEntered: return "Scene Entered";
                default: return type.ToString();
            }
        }

        public static string GetCategory(this SplitType type)
        {
            switch (type)
            {
                case SplitType.Blink:
                case SplitType.BlueCloak:
                case SplitType.Waterblink:
                case SplitType.Mjolnir:
                case SplitType.PowerSlide:
                case SplitType.Axe:
                case SplitType.BlinkWireAxe:
                case SplitType.RedCloak:
                case SplitType.OmniBlink:
                case SplitType.DoubleJump:
                case SplitType.SecretsMap:
                case SplitType.Map:
                    return "Skills";
                case SplitType.Hulder:
                case SplitType.Moose:
                case SplitType.Fafnir:
                case SplitType.Halvtann:
                case SplitType.Galvan:
                case SplitType.Elenor:
                case SplitType.Troll:
                    return "Bosses";
                case SplitType.Scrolls:
                case SplitType.ScrollsByCollection:
                    return "Scrolls";
                case SplitType.ManualSplit:
                    return "General";
                case SplitType.SceneEntered:
                    return "Scene";
                default:
                    return "";
            }
        }
    }
}

using System;
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
        // We need the right order in the enum, in order to compute the cumulative totals correctly. 
        // The order is based on the order of the scrolls in the game.
        // We make the numbering explicit to avoid any accidental changes in the future.
        Vikings = 1,
        Teslamancers = 2,
        Goo = 3,
        Clash = 4,
        Huldr = 5,
        Lumina = 6
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

        // Make sure it's consistent with the ScrollCollection enum
        public static readonly Dictionary<ScrollCollection, int> ScrollCollectionTotals = new Dictionary<ScrollCollection, int>
        {
            { ScrollCollection.Vikings,        9 },
            { ScrollCollection.Teslamancers,   6 },
            { ScrollCollection.Goo,           15 },
            { ScrollCollection.Clash,         27 },
            { ScrollCollection.Huldr,         12 },
            { ScrollCollection.Lumina,        12 }
        };

        public static readonly int TotalScrollCount = ScrollCollectionTotals.Values.Sum();

        public static int GetScrollTotalNumber()
        {
            return TotalScrollCount;
        }

        // Helpers to get cumulated totals
        public static int GetScrollCumulatedTotalBefore(ScrollCollection collection)
        {
            int total = 0;
            // IMPORTANT: Loop through the enum values in order
            foreach (ScrollCollection coll in Enum.GetValues(typeof(ScrollCollection)))
            {
                if (coll == collection)
                    break;
                total += ScrollCollectionTotals[coll];
            }
            return total;
        }

        public static int GetScrollCumulatedTotalAfter(ScrollCollection collection)
        {
            return GetScrollCumulatedTotalBefore(collection) + ScrollCollectionTotals[collection];
        }

        // Helpers to translate between scroll IDs and collections
        public static void ScrollIdToCollectionAndNumber(int scrollId, out ScrollCollection collection, out int number,
            ScrollCollection defaultCollection = ScrollCollection.Vikings, int defaultNumber = 1)
        {
            number = scrollId;
            
            // Check validity of scrollId
            if (scrollId < 1 || scrollId > TotalScrollCount)
            {
                Log.Info($"Received invalid ScrollId {scrollId}. Must be between 1 and {TotalScrollCount}.\n"
                        +$"Defaulting to collection {defaultCollection} and number {defaultNumber}.");
                collection = defaultCollection;
                number = defaultNumber;
            }

            foreach (ScrollCollection coll in Enum.GetValues(typeof(ScrollCollection)))
            {
                if (number >= 1 && number <= ScrollCollectionTotals[coll])
                {
                    collection = coll;
                    return;
                }
                number -= ScrollCollectionTotals[coll];
            }

            // Failure (shouldn't happen)
            Log.Error($"Failed to convert ScrollId {scrollId} to collection and number.\n"
                    +$"Defaulting to collection {defaultCollection} and number {defaultNumber}.");
            collection = defaultCollection;
            number = defaultNumber;
        }

        public static int ScrollCollectionAndNumberToScrollId(ScrollCollection collection, int number, int defaultNumber = 1)
        {
            if (number < 1 || number > ScrollCollectionTotals[collection])
            {
                Log.Info($"Received invalid number {number} for collection {collection}. Must be between 1 and {ScrollCollectionTotals[collection]}.\n"
                        +$"Defaulting to number {defaultNumber}.");
                number = defaultNumber;
            }
            return GetScrollCumulatedTotalBefore(collection) + number;
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

        public static bool RequiresScrollId(this SplitType type)
        {
            return type == SplitType.Scrolls || type == SplitType.ScrollsByCollection;
        }
    }
}

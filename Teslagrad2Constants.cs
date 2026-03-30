using System.Collections.Generic;

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
                case SplitType.Hulder: return "Hulder";
                case SplitType.Moose: return "Moose";
                case SplitType.Fafnir: return "Fafnir";
                case SplitType.Halvtann: return "Halvtann";
                case SplitType.Galvan: return "Galvan";
                case SplitType.Elenor: return "Elenor";
                case SplitType.Troll: return "Troll";
                case SplitType.Scrolls: return "Scrolls";
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

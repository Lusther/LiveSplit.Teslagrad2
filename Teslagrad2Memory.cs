using LiveSplit.ComponentUtil;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LiveSplit.Teslagrad2
{
    public class Teslagrad2Memory
    {
        private const string PROCESS_NAME = "Teslagrad 2";
        private const string MODULE_NAME = "GameAssembly.dll";
        private const int HOOK_RETRY_INTERVAL_MS = 2000;

        public Process Game { get; private set; }
        public GameVersion Version { get; private set; } = GameVersion.Unknown;
        private VersionOffsets _offsets;
        private DateTime _lastHookAttempt = DateTime.MinValue;

        // Pointer paths from the ASL state block
        public MemoryWatcherList Watchers { get; private set; }

        // Skills
        public MemoryWatcher<bool> BlinkUnlocked { get; private set; }
        public MemoryWatcher<bool> BlueCloakUnlocked { get; private set; }
        public MemoryWatcher<bool> WaterblinkUnlocked { get; private set; }
        public MemoryWatcher<bool> MjolnirUnlocked { get; private set; }
        public MemoryWatcher<bool> PowerSlideUnlocked { get; private set; }
        public MemoryWatcher<bool> AxeUnlocked { get; private set; }
        public MemoryWatcher<bool> BlinkWireAxeUnlocked { get; private set; }
        public MemoryWatcher<bool> RedCloakUnlocked { get; private set; }
        public MemoryWatcher<bool> OmniBlinkUnlocked { get; private set; }
        public MemoryWatcher<bool> DoubleJumpUnlocked { get; private set; }
        public MemoryWatcher<bool> SecretsMapUnlocked { get; private set; }
        public MemoryWatcher<bool> MapUnlocked { get; private set; }

        // Bosses
        public MemoryWatcher<bool> HulderBeaten { get; private set; }
        public MemoryWatcher<bool> MooseBeaten { get; private set; }
        public MemoryWatcher<bool> FafnirBeaten { get; private set; }
        public MemoryWatcher<bool> HalvtannBeaten { get; private set; }
        public MemoryWatcher<bool> GalvanBeaten { get; private set; }
        public MemoryWatcher<bool> TrollBeaten { get; private set; }

        // Other
        public MemoryWatcher<int> ScrollCount { get; private set; }
        public MemoryWatcher<int> TriggerCount { get; private set; }
        public MemoryWatcher<bool> InElenorFight { get; private set; }
        public MemoryWatcher<int> SaveSlotCount { get; private set; }

        // Scroll tracking
        private MemoryWatcher<int> _lastScrollWatcher;

        // Trigger tracking
        public string LastTrigger { get; private set; } = "";
        public string OldLastTrigger { get; private set; } = "";
        private readonly System.Collections.Generic.List<string> _newTriggers = new System.Collections.Generic.List<string>();

        // Elenor fight - magnet health tracking
        private readonly SigScanTarget _magnetScanTarget;
        private volatile IntPtr _magnetControllerAddress;
        private volatile MemoryWatcherList _magnetWatchers;
        private Task _magnetSearchTask;

        // Scene tracking (read manually as string)
        private DeepPointer _currentScenePointer;
        public string CurrentScene { get; private set; } = "";
        public string OldScene { get; private set; } = "";

        // Time tracking (read manually as string)
        private DeepPointer _timeSpentPointer;
        public string CurrentTimeSpent { get; private set; } = "";
        public string OldTimeSpent { get; private set; } = "";

        public Teslagrad2Memory()
        {
            _magnetScanTarget = new SigScanTarget(-0x20,
                "000000410000F0410000F0410000A041000000009A99193E0000003FAE47E13D????????0000F042");
            _magnetControllerAddress = IntPtr.Zero;
            _magnetWatchers = new MemoryWatcherList();
        }

        public bool TryHook()
        {
            if (Game != null && !Game.HasExited)
                return true;

            if (Game != null)
            {
                Game.Dispose();
                Game = null;
            }

            if ((DateTime.UtcNow - _lastHookAttempt).TotalMilliseconds < HOOK_RETRY_INTERVAL_MS)
                return false;
            _lastHookAttempt = DateTime.UtcNow;

            var processes = Process.GetProcessesByName(PROCESS_NAME);
            if (processes.Length == 0)
                return false;

            Game = processes[0];
            for (int i = 1; i < processes.Length; i++)
                processes[i].Dispose();

            var module = Game.ModulesWow64Safe()
                .FirstOrDefault(m => m.ModuleName == MODULE_NAME);
            if (module == null)
            {
                Game.Dispose();
                Game = null;
                return false;
            }

            Log.Info($"GameAssembly.dll ModuleMemorySize: {module.ModuleMemorySize}");
            Version = Teslagrad2Constants.DetectVersion(module.ModuleMemorySize);
            if (Version == GameVersion.Unknown)
            {
                Log.Error($"Unknown GameAssembly.dll module size: {module.ModuleMemorySize} - cannot detect game version");
                Game.Dispose();
                Game = null;
                return false;
            }

            _offsets = Teslagrad2Constants.Versions[Version];
            InitializeWatchers();

            _lastScrollWatcher = null;
            _magnetControllerAddress = IntPtr.Zero;
            _magnetWatchers.Clear();
            return true;
        }

        private void InitializeWatchers()
        {
            int save = _offsets.BaseSaveDataSlot;
            int scene = _offsets.BaseSceneHandler;
            int file = _offsets.BaseSaveDataFile;

            BlinkUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x44)) { Name = "blink" };
            BlueCloakUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x45)) { Name = "blue_cloak" };
            WaterblinkUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x46)) { Name = "waterblink" };
            MjolnirUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x47)) { Name = "mjolnir" };
            PowerSlideUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x48)) { Name = "power_slide" };
            AxeUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x49)) { Name = "axe" };
            BlinkWireAxeUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4A)) { Name = "blink_wire_axe" };
            RedCloakUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4B)) { Name = "red_cloak" };
            OmniBlinkUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4C)) { Name = "omni_blink" };
            DoubleJumpUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4D)) { Name = "double_jump" };
            SecretsMapUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4E)) { Name = "secrets_map" };
            MapUnlocked = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x4F)) { Name = "map" };

            HulderBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x51)) { Name = "hulder" };
            MooseBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x52)) { Name = "moose" };
            FafnirBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x53)) { Name = "fafnir" };
            HalvtannBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x54)) { Name = "halvtann" };
            GalvanBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x55)) { Name = "galvan" };
            TrollBeaten = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x56)) { Name = "troll" };

            ScrollCount = new MemoryWatcher<int>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x80, 0x18)) { Name = "scroll_count" };
            TriggerCount = new MemoryWatcher<int>(new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x68, 0x18)) { Name = "trigger_count" };
            InElenorFight = new MemoryWatcher<bool>(new DeepPointer(MODULE_NAME, scene, 0xB8, 0x69)) { Name = "in_elenor_fight" };
            SaveSlotCount = new MemoryWatcher<int>(new DeepPointer(MODULE_NAME, file, 0xB8, 0x0, 0x10, 0x18)) { Name = "save_slot_count" };

            _currentScenePointer = new DeepPointer(MODULE_NAME, scene, 0xB8, 0x00, 0x28, 0x10, 0x14);
            _timeSpentPointer = new DeepPointer(MODULE_NAME, save, 0xB8, 0x10, 0x28, 0x14);

            Watchers = new MemoryWatcherList
            {
                BlinkUnlocked, BlueCloakUnlocked, WaterblinkUnlocked, MjolnirUnlocked,
                PowerSlideUnlocked, AxeUnlocked, BlinkWireAxeUnlocked, RedCloakUnlocked,
                OmniBlinkUnlocked, DoubleJumpUnlocked, SecretsMapUnlocked, MapUnlocked,
                HulderBeaten, MooseBeaten, FafnirBeaten, HalvtannBeaten, GalvanBeaten, TrollBeaten,
                ScrollCount, TriggerCount, InElenorFight, SaveSlotCount
            };
        }

        public void Update()
        {
            OldScene = CurrentScene;
            _currentScenePointer.DerefString(Game, 255, out string scene);
            CurrentScene = scene ?? "";

            OldTimeSpent = CurrentTimeSpent;
            _timeSpentPointer.DerefString(Game, 40, out string timeSpent);
            CurrentTimeSpent = timeSpent ?? "";

            OldLastTrigger = LastTrigger;
            _newTriggers.Clear();

            try
            {
                Watchers.UpdateAll(Game);
            }
            catch (Exception ex)
            {
                Log.Error($"Watchers.UpdateAll failed: {ex.Message}");
                return;
            }

            UpdateScrollWatcher();
            UpdateTriggerWatcher();
            UpdateElenorFight();
        }

        private void UpdateScrollWatcher()
        {
            if (ScrollCount.Current != ScrollCount.Old)
            {
                var lastScrollPointer = new DeepPointer(
                    MODULE_NAME, _offsets.BaseSaveDataSlot, 0xB8, 0x10, 0x80, 0x10,
                    0x20 + (ScrollCount.Current - 1) * 0x4);
                _lastScrollWatcher = new MemoryWatcher<int>(lastScrollPointer);
            }

            if (_lastScrollWatcher != null)
                _lastScrollWatcher.Update(Game);
        }

        public bool CheckScrollCollected(int scrollId)
        {
            if (_lastScrollWatcher == null || ScrollCount.Current == 0)
                return false;

            return _lastScrollWatcher.Current == scrollId && _lastScrollWatcher.Old != scrollId;
        }

        private void UpdateTriggerWatcher()
        {
            if (TriggerCount.Current > TriggerCount.Old)
            {
                int oldCount = TriggerCount.Old;
                int newCount = TriggerCount.Current;

                Log.Info($"Trigger count increased from {oldCount} to {newCount}");

                // There may be several triggers set at once (or in the update interval)?
                for (int idx = oldCount; idx < newCount; idx++)
                {
                    var p = new DeepPointer(
                        MODULE_NAME, _offsets.BaseSaveDataSlot, 0xB8, 0x10, 0x68,
                        0x10, 0x20 + idx * 0x8);
                    
                    try
                    {
                        p.DerefString(Game, 255, out string trig);
                        trig = trig ?? "";
                        _newTriggers.Add(trig);
                        LastTrigger = trig;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Trigger] Failed to read trigger at index {idx}: {ex.Message}");
                    }
                }
            }
        }

        public bool CheckTriggerSet(string triggerName)
        {
            if (TriggerCount.Current == 0 || string.IsNullOrEmpty(triggerName))
                return false;

            // Return true if the triggerName was among the newly collected triggers this update.
            if (_newTriggers.Contains(triggerName))
                return true;

            // Fallback: check the last trigger transition as before
            return LastTrigger == triggerName && OldLastTrigger != triggerName;
        }

        private void UpdateElenorFight()
        {
            if (InElenorFight.Current && !InElenorFight.Old
                && (_magnetSearchTask == null || _magnetSearchTask.IsCompleted))
            {
                _magnetSearchTask = Task.Run(new Action(FindMagnetController));
            }

            if (InElenorFight.Current && _magnetSearchTask != null && _magnetSearchTask.IsCompleted)
            {
                _magnetWatchers.UpdateAll(Game);
            }

            if (!InElenorFight.Current && InElenorFight.Old)
            {
                _magnetWatchers.Clear();
                _magnetControllerAddress = IntPtr.Zero;
                _magnetSearchTask = null;
            }
        }

        private void FindMagnetController()
        {
            IntPtr address = IntPtr.Zero;
            foreach (var page in Game.MemoryPages())
            {
                var scanner = new SignatureScanner(Game, page.BaseAddress, (int)page.RegionSize);
                address = scanner.Scan(_magnetScanTarget);
                if (address != IntPtr.Zero)
                    break;
            }

            if (address != IntPtr.Zero)
            {
                var watchers = new MemoryWatcherList();
                int[] offsets = { 0x20, 0x28, 0x30 };
                for (int i = 0; i < offsets.Length; i++)
                    watchers.Add(new MemoryWatcher<float>(
                        new DeepPointer(address + 0x18, offsets[i], 0x10, 0x90))
                        { Name = $"magnet{i + 1}_health" });

                _magnetWatchers = watchers;
            }

            _magnetControllerAddress = address;
        }

        public bool IsElenorDead()
        {
            if (!InElenorFight.Current || _magnetSearchTask == null || !_magnetSearchTask.IsCompleted)
                return false;

            if (_magnetControllerAddress == IntPtr.Zero)
                return false;

            var m1 = _magnetWatchers["magnet1_health"] as MemoryWatcher<float>;
            var m2 = _magnetWatchers["magnet2_health"] as MemoryWatcher<float>;
            var m3 = _magnetWatchers["magnet3_health"] as MemoryWatcher<float>;

            return m1 != null && m2 != null && m3 != null &&
                   m1.Current == 0f && m2.Current == 0f && m3.Current == 0f;
        }

        public void Dispose()
        {
            Game?.Dispose();
            Game = null;
            Version = GameVersion.Unknown;
            _offsets = null;
            _lastScrollWatcher = null;
            _magnetWatchers = new MemoryWatcherList();
            _magnetControllerAddress = IntPtr.Zero;
        }
    }
}

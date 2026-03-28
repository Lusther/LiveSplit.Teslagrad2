using LiveSplit.ComponentUtil;
using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.Teslagrad2
{
    public class Teslagrad2Component : IComponent
    {
        public string ComponentName => "Teslagrad 2 Autosplitter";

        public float HorizontalWidth => 0;
        public float VerticalHeight => 0;
        public float MinimumWidth => 0;
        public float MinimumHeight => 0;
        public float PaddingTop => 0;
        public float PaddingBottom => 0;
        public float PaddingLeft => 0;
        public float PaddingRight => 0;

        public IDictionary<string, Action> ContextMenuControls => null;

        private readonly TimerModel _timer;
        private readonly LiveSplitState _state;
        private readonly Teslagrad2Settings _settings;
        private readonly Teslagrad2Memory _memory;

        private bool _hooked;

        public Teslagrad2Component(LiveSplitState state)
        {
            _state = state;
            _timer = new TimerModel { CurrentState = state };
            _settings = new Teslagrad2Settings();
            _memory = new Teslagrad2Memory();
            Log.Info("Component loaded.");
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (!_memory.TryHook())
            {
                if (_hooked)
                {
                    Log.Info("Game process lost.");
                    _hooked = false;
                    UpdateVersionDisplay(state);
                }
                return;
            }

            if (!_hooked)
            {
                Log.Info($"Hooked to process: {_memory.Game.ProcessName} (PID {_memory.Game.Id}), version: {Teslagrad2Constants.GetVersionName(_memory.Version)}");
                _hooked = true;
                UpdateVersionDisplay(state);
            }

            _memory.Update();

            switch (state.CurrentPhase)
            {
                case TimerPhase.NotRunning:
                    if (ShouldStart())
                    {
                        Log.Info("Starting timer.");
                        _timer.Start();
                    }
                    break;

                case TimerPhase.Running:
                case TimerPhase.Paused:
                    if (ShouldReset())
                    {
                        Log.Info("Resetting timer.");
                        _timer.Reset();
                        break;
                    }
                    if (state.CurrentPhase == TimerPhase.Running && ShouldSplit())
                    {
                        Log.Info($"Splitting at index {state.CurrentSplitIndex}.");
                        _timer.Split();
                    }
                    break;
            }
        }

        private bool ShouldStart()
        {
            if (_settings.Splits.Count < 2) return false;
            if (_settings.Splits[0].Type != SplitType.StartTimer) return false;

            return _memory.CurrentTimeSpent == "00:00:00" && _memory.OldTimeSpent != "00:00:00";
        }

        private bool ShouldReset()
        {
            if (!_settings.AutoReset) return false;
            return _memory.SaveSlotCount.Current < _memory.SaveSlotCount.Old;
        }

        private bool ShouldSplit()
        {
            int settingsIndex = _state.CurrentSplitIndex + 1;
            if (settingsIndex < 1 || settingsIndex >= _settings.Splits.Count)
                return false;

            return CheckSplitCondition(_settings.Splits[settingsIndex]);
        }

        private static bool BecameTrue(MemoryWatcher<bool> w) => w.Current && !w.Old;

        private bool CheckSplitCondition(SplitEntry entry)
        {
            switch (entry.Type)
            {
                case SplitType.Blink: return BecameTrue(_memory.BlinkUnlocked);
                case SplitType.BlueCloak: return BecameTrue(_memory.BlueCloakUnlocked);
                case SplitType.Waterblink: return BecameTrue(_memory.WaterblinkUnlocked);
                case SplitType.Mjolnir: return BecameTrue(_memory.MjolnirUnlocked);
                case SplitType.PowerSlide: return BecameTrue(_memory.PowerSlideUnlocked);
                case SplitType.Axe: return BecameTrue(_memory.AxeUnlocked);
                case SplitType.BlinkWireAxe: return BecameTrue(_memory.BlinkWireAxeUnlocked);
                case SplitType.RedCloak: return BecameTrue(_memory.RedCloakUnlocked);
                case SplitType.OmniBlink: return BecameTrue(_memory.OmniBlinkUnlocked);
                case SplitType.DoubleJump: return BecameTrue(_memory.DoubleJumpUnlocked);
                case SplitType.SecretsMap: return BecameTrue(_memory.SecretsMapUnlocked);

                case SplitType.Hulder: return BecameTrue(_memory.HulderBeaten);
                case SplitType.Moose: return BecameTrue(_memory.MooseBeaten);
                case SplitType.Fafnir: return BecameTrue(_memory.FafnirBeaten);
                case SplitType.Halvtann: return BecameTrue(_memory.HalvtannBeaten);
                case SplitType.Galvan: return BecameTrue(_memory.GalvanBeaten);
                case SplitType.Troll: return BecameTrue(_memory.TrollBeaten);
                case SplitType.Elenor: return _memory.IsElenorDead();

                case SplitType.Scrolls:
                    return _memory.CheckScrollCollected(entry.ScrollId);

                case SplitType.SceneEntered:
                    return _memory.CurrentScene == entry.SceneName
                        && _memory.OldScene != entry.SceneName;

                default: return false;
            }
        }

        private const string VERSION_VARIABLE = "Teslagrad2_Version";

        // Reflection is required because the CustomVariable API was added in a newer
        // LiveSplit.Core than the one available at compile time.
        private static MethodInfo _getOrAddCustomVariable;
        private static MethodInfo _setCustomVariable;
        private static bool _reflectionResolved;

        private void UpdateVersionDisplay(LiveSplitState state)
        {
            string versionText = _hooked
                ? Teslagrad2Constants.GetVersionName(_memory.Version)
                : "";

            try
            {
                if (!_reflectionResolved)
                {
                    var metaType = state.Run.Metadata.GetType();
                    _getOrAddCustomVariable = metaType.GetMethod("GetOrAddCustomVariable");
                    _setCustomVariable = metaType.GetMethod("SetCustomVariable");
                    _reflectionResolved = true;
                }

                var metadata = state.Run.Metadata;
                _getOrAddCustomVariable?.Invoke(metadata, new object[] { VERSION_VARIABLE });
                _setCustomVariable?.Invoke(metadata, new object[] { VERSION_VARIABLE, versionText });
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set custom variable: {ex.Message}");
            }
        }

        public Control GetSettingsControl(LayoutMode mode) => _settings;
        public XmlNode GetSettings(XmlDocument document) => _settings.GetSettings(document);
        public void SetSettings(XmlNode settings) => _settings.SetSettings(settings);

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion) { }
        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion) { }

        public void Dispose()
        {
            _memory.Dispose();
        }
    }
}

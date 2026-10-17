using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Linq;

namespace LiveSplit.Teslagrad2
{
    // One line in the layout showing a Teslagrad2_* variable, through a LiveSplit Text component in
    // "Use Variable" mode: the Text component reads the variable itself on every update.
    // The autosplitter cannot draw itself when activated from the splits editor (LiveSplit only
    // calls its Update), so it adds that Text component to the layout instead.
    internal class LayoutTextLine
    {
        private const string TEXT_COMPONENT_FILE = "LiveSplit.Text.dll";
        private const string TEXT_COMPONENT_TYPE = "LiveSplit.UI.Components.TextComponent";

        private readonly string _label;
        private readonly string _variable;
        private ILayoutComponent _line;
        private bool _changePending;

        public LayoutTextLine(string label, string variable)
        {
            _label = label;
            _variable = variable;
        }

        public void Update(LiveSplitState state, bool visible)
        {
            if (_changePending || state.Layout == null) return;

            // The layout may have been reloaded or edited by the user since the last tick
            if (_line != null && !state.Layout.LayoutComponents.Contains(_line))
                _line = null;

            if (!visible)
            {
                if (_line != null)
                    Remove(state);
                return;
            }

            if (_line == null)
            {
                // Reuse a line saved in the layout (matched by variable, so the label can be renamed)
                _line = state.Layout.LayoutComponents.FirstOrDefault(IsOwnLine);
                if (_line == null)
                    Add(state);
            }
        }

        public void Remove(LiveSplitState state)
        {
            var line = _line;
            _line = null;
            if (line == null) return;
            ChangeLayout(state, () =>
            {
                state.Layout.LayoutComponents.Remove(line);
                line.Component.Dispose();
            });
        }

        private void Add(LiveSplitState state)
        {
            ChangeLayout(state, () =>
            {
                var line = ComponentManager.LoadLayoutComponent(TEXT_COMPONENT_FILE, state);
                if (line == null)
                {
                    Log.Error($"Could not create a {TEXT_COMPONENT_FILE} component for {_variable}.");
                    return;
                }
                SetSetting(line, "Text1", _label);
                SetSetting(line, "Text2", _variable);
                SetSetting(line, "CustomVariable", true);
                state.Layout.LayoutComponents.Add(line);
                _line = line;
            });
        }

        // Deferred to after the current tick: a layout component is updated while LiveSplit
        // iterates over the layout, which must not change during that loop
        private void ChangeLayout(LiveSplitState state, Action change)
        {
            _changePending = true;
            Action run = () =>
            {
                try { change(); }
                catch (Exception ex) { Log.Error($"Failed to update the layout line for {_variable}: {ex.Message}"); }
                finally { _changePending = false; }
            };

            try
            {
                if (state.Form != null && state.Form.IsHandleCreated)
                {
                    state.Form.BeginInvoke(run);
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // Window handle destroyed while LiveSplit closes: nothing left to iterate over
            }
            run();
        }

        private bool IsOwnLine(ILayoutComponent line)
        {
            return line.Component?.GetType().FullName == TEXT_COMPONENT_TYPE
                && GetSetting(line, "CustomVariable") is bool useVariable && useVariable
                && GetSetting(line, "Text2") as string == _variable;
        }

        // Reflection: LiveSplit.Text.dll is a separate component the project does not reference
        private static object GetSetting(ILayoutComponent line, string property)
        {
            object settings = line.Component.GetType().GetProperty("Settings")?.GetValue(line.Component);
            return settings?.GetType().GetProperty(property)?.GetValue(settings);
        }

        private static void SetSetting(ILayoutComponent line, string property, object value)
        {
            object settings = line.Component.GetType().GetProperty("Settings")?.GetValue(line.Component);
            settings?.GetType().GetProperty(property)?.SetValue(settings, value);
        }
    }
}

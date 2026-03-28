using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Reflection;

namespace LiveSplit.Teslagrad2
{
    public class Teslagrad2Factory : IComponentFactory
    {
        public string ComponentName => "Teslagrad 2 Autosplitter";
        public string Description => "Autosplitter for Teslagrad 2";
        public ComponentCategory Category => ComponentCategory.Control;

        public string UpdateName => ComponentName;
        public string UpdateURL => "";
        public string XMLURL => UpdateURL + "Components/Updates.xml";
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        public IComponent Create(LiveSplitState state) => new Teslagrad2Component(state);
    }
}

using ExtraToolsPlugin.Osu;
using Sync.Plugins;
using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sync.Plugins.PluginEvents;

namespace ExtraToolsPlugin
{
    public class ExtraToolsPlugin : Plugin
    {
        Setting setting=new Setting();

        PluginConfigurationManager config;

        public ExtraToolsPlugin() : base("ExtraToolsPlugin", "MikiraSora")
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();

            config = new PluginConfigurationManager(this);
            config.AddItem(setting);

            EventBus.BindEvent<InitFilterEvent>(OnInitFilter);
        }

        private void OnInitFilter(InitFilterEvent @event)
        {
            @event.Filters.AddFilter(new OsuCommandFilter(setting));
        }
    }
}

using Sync.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraToolsPlugin
{
    public class Setting : IConfigurable
    {
        public ConfigurationElement MikiraSoraAPIKey { get; set; }
        public ConfigurationElement OsuAPIKey { get; set; }

        public void onConfigurationLoad()
        {

        }

        public void onConfigurationReload()
        {

        }

        public void onConfigurationSave()
        {

        }
    }
}

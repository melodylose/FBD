using System.Collections.Generic;

namespace FBDApp.Models
{
    public class ConfigurationData
    {
        public List<SeceModule> Modules { get; set; } = new List<SeceModule>();
        public List<ConnectionInfo> Connections { get; set; } = new List<ConnectionInfo>();
    }
}

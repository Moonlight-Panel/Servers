using MoonlightServers.Shared.Enums;

namespace MoonlightServers.Shared.Models;

public class ParseConfiguration
{
    public string File { get; set; }
    public FileParsers Parser { get; set; }

    public List<ParseConfigurationEntry> Entries { get; set; } = new();
    
    public class ParseConfigurationEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
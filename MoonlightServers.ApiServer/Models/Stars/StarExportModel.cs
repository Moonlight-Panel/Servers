using MoonlightServers.Shared.Enums;

namespace MoonlightServers.ApiServer.Models.Stars;

public class StarExportModel
{
    // Meta
    public string Name { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string? DonateUrl { get; set; }
    public string? UpdateUrl { get; set; }
    
    // Start and stop
    public string StartupCommand { get; set; }
    public string StopCommand { get; set; }
    public string OnlineDetection { get; set; }

    // Install
    public string InstallShell { get; set; }
    public string InstallDockerImage { get; set; }
    public string InstallScript { get; set; }

    // Misc
    public int RequiredAllocations { get; set; }
    public bool AllowDockerImageChange { get; set; }
    public string ParseConfiguration { get; set; }
    
    // Relations
    public StarVariableExportModel[] Variables { get; set; }
    public StarDockerImageExportModel[] DockerImages { get; set; }
    
    public class StarDockerImageExportModel
    {
        public string DisplayName { get; set; }
        public string Identifier { get; set; }
        public bool AutoPulling { get; set; }
    }
    
    public class StarVariableExportModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    
        public string Key { get; set; }
        public string DefaultValue { get; set; }

        public bool AllowViewing { get; set; }
        public bool AllowEditing { get; set; }

        public StarVariableType Type { get; set; }
        public string? Filter { get; set; }
    }
}
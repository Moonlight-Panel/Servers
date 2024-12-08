using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models.Stars;

namespace MoonlightServers.ApiServer.Services;

[Scoped]
public class StarImportExportService
{
    private readonly DatabaseRepository<Star> StarRepository;
    private readonly ILogger<StarImportExportService> Logger;

    public StarImportExportService(DatabaseRepository<Star> starRepository, ILogger<StarImportExportService> logger)
    {
        StarRepository = starRepository;
        Logger = logger;
    }

    public async Task<string> Export(int id)
    {
        var star = StarRepository
            .Get()
            .Include(x => x.DockerImages)
            .Include(x => x.Variables)
            .FirstOrDefault(x => x.Id == id);

        if (star == null)
            throw new HttpApiException("No star with this id found", 404);

        var exportModel = new StarExportModel()
        {
            Name = star.Name,
            Author = star.Author,
            Version = star.Version,
            DonateUrl = star.DonateUrl,
            UpdateUrl = star.UpdateUrl,
            InstallScript = star.InstallScript,
            InstallShell = star.InstallShell,
            InstallDockerImage = star.InstallDockerImage,
            OnlineDetection = star.OnlineDetection,
            StopCommand = star.StopCommand,
            StartupCommand = star.StartupCommand,
            ParseConfiguration = star.ParseConfiguration,
            RequiredAllocations = star.RequiredAllocations,
            AllowDockerImageChange = star.AllowDockerImageChange,
            Variables = star.Variables.Select(x => new StarExportModel.StarVariableExportModel()
            {
                Name = x.Name,
                Type = x.Type,
                Description = x.Description,
                Filter = x.Filter,
                Key = x.Key,
                AllowEditing = x.AllowEditing,
                AllowViewing = x.AllowViewing,
                DefaultValue = x.DefaultValue
            }).ToArray(),
            DockerImages = star.DockerImages.Select(x => new StarExportModel.StarDockerImageExportModel()
            {
                Identifier = x.Identifier,
                AutoPulling = x.AutoPulling,
                DisplayName = x.DisplayName
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(exportModel, new JsonSerializerOptions()
        {
            WriteIndented = true
        });

        return json;
    }

    public async Task<Star> Import(string json)
    {
        // Determine which importer to use based on simple patterns
        if (json.Contains("RequiredAllocations"))
            return await ImportStar(json);
        else if (json.Contains("AllocationsNeeded"))
            return await ImportImage(json);
        else if (json.Contains("_comment"))
            return await ImportEgg(json);
        else
            throw new HttpApiException("Unable to determine the format of the imported star/image/egg", 400);
    }

    public async Task<Star> ImportStar(string json)
    {
        try
        {
            var model = JsonSerializer.Deserialize<StarExportModel>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });
            
            ArgumentNullException.ThrowIfNull(model);

            var star = new Star()
            {
                Name = model.Name,
                Author = model.Author,
                Version = model.Version,
                DonateUrl = model.DonateUrl,
                UpdateUrl = model.UpdateUrl,
                InstallScript = model.InstallScript,
                InstallShell = model.InstallShell,
                InstallDockerImage = model.InstallDockerImage,
                OnlineDetection = model.OnlineDetection,
                StopCommand = model.StopCommand,
                StartupCommand = model.StartupCommand,
                ParseConfiguration = model.ParseConfiguration,
                RequiredAllocations = model.RequiredAllocations,
                AllowDockerImageChange = model.AllowDockerImageChange,
                Variables = model.Variables.Select(x => new StarVariable()
                {
                    DefaultValue = x.DefaultValue,
                    Description = x.Description,
                    Filter = x.Filter,
                    Key = x.Key,
                    AllowEditing = x.AllowEditing,
                    AllowViewing = x.AllowViewing,
                    Type = x.Type,
                    Name = x.Name
                }).ToList(),
                DockerImages = model.DockerImages.Select(x => new StarDockerImage()
                {
                    DisplayName = x.DisplayName,
                    AutoPulling = x.AutoPulling,
                    Identifier = x.Identifier
                }).ToList()
            };

            var finalStar = StarRepository.Add(star);

            return finalStar;
        }
        catch (Exception e)
        {
            Logger.LogError("An unhandled error occured while importing star: {e}", e);
            throw new HttpApiException("An unhandled error occured while importing the star", 400);
        }
    }

    public async Task<Star> ImportImage(string json)
    {
        throw new NotImplementedException();
    }

    public async Task<Star> ImportEgg(string json)
    {
        throw new NotImplementedException();
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using MoonCore.Attributes;
using MoonCore.Exceptions;
using MoonCore.Extended.Abstractions;
using MoonlightServers.ApiServer.Database.Entities;
using MoonlightServers.ApiServer.Models.Stars;
using MoonlightServers.Shared.Enums;
using MoonlightServers.Shared.Models;

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
            DefaultDockerImage = star.DefaultDockerImage,
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
                DefaultDockerImage = model.DefaultDockerImage,
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

            var finalStar = await StarRepository.Add(star);

            return finalStar;
        }
        catch (Exception e)
        {
            Logger.LogError("An unhandled error occured while importing star: {e}", e);
            throw new HttpApiException("An unhandled error occured while importing star", 400);
        }
    }

    public async Task<Star> ImportImage(string json)
    {
        try
        {
            var model = JsonSerializer.Deserialize<LegacyImageImportModel>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            });

            ArgumentNullException.ThrowIfNull(model);

            var star = new Star()
            {
                Name = model.Name,
                Author = model.Author,
                Version = "Imported from v2.0",
                DonateUrl = model.DonateUrl,
                UpdateUrl = model.UpdateUrl,
                InstallScript = model.InstallScript,
                InstallShell = model.InstallShell,
                InstallDockerImage = model.InstallDockerImage,
                OnlineDetection = model.OnlineDetection,
                StopCommand = model.StopCommand,
                StartupCommand = model.StartupCommand,
                RequiredAllocations = model.AllocationsNeeded,
                AllowDockerImageChange = model.AllowDockerImageChange,
                DefaultDockerImage = model.DefaultDockerImage,
                Variables = model.Variables.Select(x => new StarVariable()
                {
                    DefaultValue = x.DefaultValue,
                    Description = x.Description,
                    Filter = x.Filter,
                    Key = x.Key,
                    AllowEditing = x.AllowEdit,
                    AllowViewing = x.AllowView,
                    Type = StarVariableType.Text,
                    Name = x.DisplayName
                }).ToList(),
                DockerImages = model.DockerImages.Select(x => new StarDockerImage()
                {
                    DisplayName = x.DisplayName,
                    AutoPulling = x.AutoPull,
                    Identifier = x.Name
                }).ToList()
            };

            #region Convert parse configurations

            var oldParseConfig = JsonSerializer.Deserialize<LegacyImageParseConfigModel[]>(model.ParseConfiguration,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            ArgumentNullException.ThrowIfNull(oldParseConfig);

            var newParseConfig = new List<ParseConfiguration>();

            // Remap values
            foreach (var config in oldParseConfig)
            {
                var parseConfiguration = new ParseConfiguration()
                {
                    File = config.File,
                    Parser = Enum.TryParse(config.Type, true, out FileParsers parserType)
                        ? parserType
                        : FileParsers.File
                };

                foreach (var option in config.Configuration)
                {
                    parseConfiguration.Entries.Add(new ParseConfiguration.ParseConfigurationEntry()
                    {
                        Key = option.Key,
                        Value = option.Value
                    });
                }

                newParseConfig.Add(parseConfiguration);
            }

            star.ParseConfiguration = JsonSerializer.Serialize(newParseConfig);

            #endregion

            var finalStar = await StarRepository.Add(star);

            return finalStar;
        }
        catch (Exception e)
        {
            Logger.LogError("An unhandled error occured while importing image: {e}", e);
            throw new HttpApiException("An unhandled error occured while importing image", 400);
        }
    }

    public async Task<Star> ImportEgg(string json)
    {
        // Create result
        var star = new Star();
        
        // Prepare json
        var fixedJson = json;
        fixedJson = fixedJson.Replace("\\/", "/");
        var jsonDocument = JsonDocument.Parse(fixedJson);
        var egg = jsonDocument.RootElement;
        
        // Let's go :O

        // Basic meta
        star.Name = egg.GetProperty("name").GetString() ?? "Parse error";
        star.Author = egg.GetProperty("author").GetString() ?? "Parse error";
        
        // Start & Stop and Status
        var configSection = egg.GetProperty("config");
        
        star.StartupCommand = egg.GetProperty("startup").GetString() ?? "Parse error";
        star.StopCommand = configSection.GetProperty("stop").GetString() ?? "Parse error";

        var startupSectionJson = configSection.GetProperty("startup").GetString() ?? "{}";
        var startupSectionDocument = JsonDocument.Parse(startupSectionJson);
        var startupSection = startupSectionDocument.RootElement;

        var doneProperty = startupSection.GetProperty("done");

        if (doneProperty.ValueKind == JsonValueKind.Array)
            star.OnlineDetection = doneProperty.Deserialize<string[]>()?.First() ?? "Parse error";
        else
            star.OnlineDetection = doneProperty.GetString() ?? "Parse error";
        
        // Installation
        var installationSection = egg.GetProperty("scripts").GetProperty("installation");

        var shell = installationSection.GetProperty("entrypoint").GetString() ?? "bash";
        star.InstallShell = shell.StartsWith("/") ? shell : $"/bin/{shell}";

        star.InstallDockerImage = installationSection.GetProperty("container").GetString() ?? "Parse error";
        star.InstallScript = installationSection.GetProperty("script").GetString() ?? "Parse error";
        
        // Variables
        var variables = egg.GetProperty("variables");

        foreach (var variable in variables.EnumerateArray())
        {
            var starVariable = new StarVariable()
            {
                Name = variable.GetProperty("name").GetString() ?? "Parse error",
                Description = variable.GetProperty("description").GetString() ?? "Parse error",
                Key = variable.GetProperty("env_variable").GetString() ?? "Parse error",
                DefaultValue = variable.GetProperty("default_value").GetString() ?? "Parse error",
                Type = StarVariableType.Text
            };
            
            // Check if the provided value is an int or a boolean as both are apparently valid
            if (variable.GetProperty("user_editable").ValueKind == JsonValueKind.Number)
            {
                starVariable.AllowEditing = variable.GetProperty("user_editable").GetInt32() == 1;
                starVariable.AllowViewing = variable.GetProperty("user_viewable").GetInt32() == 1;
            }
            else
            {
                starVariable.AllowEditing = variable.GetProperty("user_editable").GetBoolean();
                starVariable.AllowViewing = variable.GetProperty("user_viewable").GetBoolean();
            }
            
            star.Variables.Add(starVariable);
        }
        
        // Docker images
        if (egg.TryGetProperty("image", out var imageProperty)) // Variant 1
        {
            star.DockerImages.Add(new StarDockerImage()
            {
                Identifier = imageProperty.GetString() ?? "Parse error",
                DisplayName = imageProperty.GetString() ?? "Parse error",
                AutoPulling = true
            });
        }
        else if (egg.TryGetProperty("images", out var imagesProperty)) // Variant 2
        {
            foreach (var di in imagesProperty.EnumerateObject())
            {
                star.DockerImages.Add(new StarDockerImage()
                {
                    DisplayName = di.Name,
                    Identifier = di.Value.GetString() ?? "Parse error",
                    AutoPulling = true
                });
            }
        }
        else if (egg.TryGetProperty("docker_images", out var dockerImages)) // Variant 3
        {
            foreach (var di in dockerImages.EnumerateObject())
            {
                star.DockerImages.Add(new StarDockerImage()
                {
                    DisplayName = di.Name,
                    Identifier = di.Value.GetString() ?? "Parse error",
                    AutoPulling = true
                });
            }
        }
        
        // Parse configuration
        var parseConfigurationJson = configSection.GetProperty("files").GetString() ?? "{}";
        var parseConfigurationDocument = JsonDocument.Parse(parseConfigurationJson);
        var parseConfiguration = parseConfigurationDocument.RootElement;

        var resultPcs = new List<ParseConfiguration>();
        
        foreach (var pConfig in parseConfiguration.EnumerateObject())
        {
            var pc = new ParseConfiguration()
            {
                File = pConfig.Name
            };

            var parser = pConfig.Value.GetProperty("parser").GetString() ?? "Parse error";
            pc.Parser = Enum.TryParse(parser, true, out FileParsers fileParser) ? fileParser : FileParsers.File;

            foreach (var pConfigFind in pConfig.Value.GetProperty("find").EnumerateObject())
            {
                var entry = new ParseConfiguration.ParseConfigurationEntry()
                {
                    Key = pConfigFind.Name,
                    Value = pConfigFind.Value.GetString() ?? "Parse error"
                };

                // Fix up special variables
                entry.Value = entry.Value.Replace("server.allocations.default.port", "SERVER_PORT");
                entry.Value = entry.Value.Replace("server.build.default.port", "SERVER_PORT");
                
                pc.Entries.Add(entry);
            }
            
            resultPcs.Add(pc);
        }

        star.ParseConfiguration = JsonSerializer.Serialize(resultPcs);
        
        // Post parse fixes
        
        // - Stop command signal
        // Some weird eggs use ^^C in as a stop command, so we need to handle this as well
        // because moonlight handles power signals correctly, wings does/did not
        star.StopCommand = star.StopCommand.Replace("^^C", "^C");
        
        // - Set moonlight native values
        star.RequiredAllocations = 1;
        star.Version = "Imported egg";
        star.AllowDockerImageChange = true;
        
        // Finally save it to the db
        var finalStar = await StarRepository.Add(star);

        return finalStar;
    }
}
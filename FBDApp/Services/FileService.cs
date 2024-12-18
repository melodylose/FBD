using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using FBDApp.Models;
using Microsoft.Win32;

namespace FBDApp.Services
{
    /// <summary>
    /// Service responsible for handling file operations related to configuration management.
    /// This includes saving and loading diagram configurations, and managing file dialogs.
    /// </summary>
    public class FileService : IFileService
    {
        /// <summary>
        /// Saves the current diagram configuration to a JSON file.
        /// </summary>
        /// <param name="filePath">The path where the configuration file will be saved</param>
        /// <param name="modules">Collection of modules to be saved</param>
        /// <param name="connections">Collection of connections between modules to be saved</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        /// <exception cref="FileServiceException">Thrown when any file operation fails</exception>
        public async Task SaveConfiguration(string filePath, ObservableCollection<SeceModule> modules, ObservableCollection<ConnectionInfo> connections)
        {
            try
            {
                LogService.LogInfo($"Starting to save configuration to file: {filePath}");
                var config = new DiagramConfiguration
                {
                    Modules = modules,
                    Connections = connections
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                await File.WriteAllTextAsync(filePath, jsonString);
                LogService.LogInfo($"Configuration successfully saved to file: {filePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogService.LogError(ex, $"Access denied when saving file: {filePath}");
                throw new FileServiceException("Unable to access file. Please check if you have sufficient permissions.", ex);
            }
            catch (IOException ex)
            {
                LogService.LogError(ex, $"IO error when saving file: {filePath}");
                throw new FileServiceException("File access error. Please ensure the file is not being used by another process.", ex);
            }
            catch (JsonException ex)
            {
                LogService.LogError(ex, "Error during configuration serialization");
                throw new FileServiceException("Error occurred while serializing configuration.", ex);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Unexpected error while saving configuration: {filePath}");
                throw new FileServiceException("An unexpected error occurred while saving configuration.", ex);
            }
        }

        /// <summary>
        /// Loads a diagram configuration from a JSON file.
        /// </summary>
        /// <param name="filePath">The path of the configuration file to load</param>
        /// <returns>A tuple containing collections of modules and connections</returns>
        /// <exception cref="FileServiceException">Thrown when any file operation fails</exception>
        public async Task<(ObservableCollection<SeceModule> modules, ObservableCollection<ConnectionInfo> connections)> LoadConfiguration(string filePath)
        {
            try
            {
                LogService.LogInfo($"Starting to load configuration from file: {filePath}");
                string jsonString = await File.ReadAllTextAsync(filePath);
                LogService.LogInfo($"JSON content read: {jsonString}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<DiagramConfiguration>(jsonString, options);
                
                // Log module information if available
                if (config?.Modules != null)
                {
                    foreach (var module in config.Modules)
                    {
                        LogService.LogInfo($"Deserialized module - Name: {module.Name}, ID: {module.Id}");
                    }
                }
                else
                {
                    LogService.LogWarning("No module configuration found");
                }

                // Log connection information if available
                if (config?.Connections != null)
                {
                    foreach (var conn in config.Connections)
                    {
                        LogService.LogInfo($"Deserialized connection - Source ID: {conn.SourceModuleId}, Target ID: {conn.TargetModuleId}");
                    }
                }
                else
                {
                    LogService.LogWarning("No connection configuration found");
                }

                return (config?.Modules ?? new(), config?.Connections ?? new());
            }
            catch (FileNotFoundException ex)
            {
                LogService.LogError(ex, $"Configuration file not found: {filePath}");
                throw new FileServiceException("The specified configuration file was not found.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogService.LogError(ex, $"Access denied when reading file: {filePath}");
                throw new FileServiceException("Unable to access file. Please check if you have sufficient permissions.", ex);
            }
            catch (JsonException ex)
            {
                LogService.LogError(ex, "Error during configuration deserialization");
                throw new FileServiceException("The configuration file format is invalid or corrupted.", ex);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, $"Unexpected error while loading configuration: {filePath}");
                throw new FileServiceException("An unexpected error occurred while loading configuration.", ex);
            }
        }

        /// <summary>
        /// Displays an Open File Dialog for selecting a configuration file.
        /// </summary>
        /// <returns>The selected file path, or null if the dialog was cancelled</returns>
        /// <exception cref="FileServiceException">Thrown when the dialog operation fails</exception>
        public string? ShowOpenFileDialog()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 1
                };

                return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : null;
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error showing Open File Dialog");
                throw new FileServiceException("Unable to open file selection dialog.", ex);
            }
        }

        /// <summary>
        /// Displays a Save File Dialog for selecting where to save a configuration file.
        /// </summary>
        /// <returns>The selected file path, or null if the dialog was cancelled</returns>
        /// <exception cref="FileServiceException">Thrown when the dialog operation fails</exception>
        public string? ShowSaveFileDialog()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 1
                };

                return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error showing Save File Dialog");
                throw new FileServiceException("Unable to open file save dialog.", ex);
            }
        }
    }
}

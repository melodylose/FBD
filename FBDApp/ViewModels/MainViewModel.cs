using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FBDApp.Models;
using FBDApp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using FBDApp.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Windows.Input;
using System.Windows.Controls;

namespace FBDApp.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application, handling the core business logic and UI state management.
    /// Implements MVVM pattern using CommunityToolkit.Mvvm.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;

        [ObservableProperty]
        private string title = "FBD Designer";

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private string mousePosition = "Mouse Position: X:0, Y:0";

        [ObservableProperty]
        private SeceModule selectedModule;

        [ObservableProperty]
        private ConnectionLine selectedConnection;

        private string _connectionStatus = string.Empty;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// Collection of template modules available for dragging onto the canvas
        /// </summary>
        public ObservableCollection<SeceModule> AvailableModules { get; } = new();

        /// <summary>
        /// Collection of modules currently placed on the canvas
        /// </summary>
        public ObservableCollection<SeceModule> CanvasModules { get; } = new();

        /// <summary>
        /// Collection of connections between modules on the canvas
        /// </summary>
        public ObservableCollection<ConnectionLine> Connections { get; } = new();

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class
        /// </summary>
        /// <param name="fileService">Service for handling file operations</param>
        public MainViewModel(IFileService fileService)
        {
            _fileService = fileService;
            LogService.LogInfo("MainViewModel constructor called");
            InitializeModules();
            CanvasModules.CollectionChanged += (s, e) =>
            {
                UpdateStatusMessage();
            };
            SaveCommand = new RelayCommand(async () => await SaveConfiguration());
            LoadCommand = new RelayCommand(async () => await LoadConfiguration());

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedModule) ||
                    e.PropertyName == nameof(SelectedConnection))
                {
                    UpdateStatusMessage();
                }
            };
        }

        /// <summary>
        /// Updates the status message with current canvas information
        /// </summary>
        private void UpdateStatusMessage()
        {
            StatusMessage = $"Modules on canvas: {CanvasModules.Count}";
            LogService.LogInfo($"Canvas modules count updated: {CanvasModules.Count}");
        }

        /// <summary>
        /// Initializes the available module templates
        /// </summary>
        private void InitializeModules()
        {
            LogService.LogInfo("Initializing available modules");
            var moduleTypes = new[]
            {
                (Name: "Tool Status", Type: ModuleType.ToolStatus),
                (Name: "Tool Control", Type: ModuleType.ToolControl),
                (Name: "Exception Handling", Type: ModuleType.ExceptionHandling),
                (Name: "Data Collection", Type: ModuleType.DataCollection),
                (Name: "Process Program", Type: ModuleType.ProcessProgram),
                (Name: "Error", Type: ModuleType.Error),
                (Name: "Terminal Services", Type: ModuleType.TerminalServices)
            };

            foreach (var module in moduleTypes)
            {
                var newModule = new SeceModule(module.Name, module.Type);
                LogService.LogInfo($"Created template module: {module.Name} with ID: {newModule.Id}");
                AvailableModules.Add(newModule);
            }
        }

        /// <summary>
        /// Handles the drop event when a module is dropped onto the canvas
        /// </summary>
        [RelayCommand]
        private void ModuleDrop((double X, double Y, SeceModule Module) dropInfo)
        {
            var newModule = dropInfo.Module.Clone();
            newModule.X = dropInfo.X;
            newModule.Y = dropInfo.Y;

            CanvasModules.Add(newModule);
            LogService.LogInfo($"Added module '{newModule.Name}' (ID: {newModule.Id}) at position X:{newModule.X}, Y:{newModule.Y}");
        }

        /// <summary>
        /// Handles module selection, updating the visual state accordingly
        /// </summary>
        [RelayCommand]
        private void SelectModule(SeceModule module)
        {
            if (SelectedModule != null)
            {
                SelectedModule.IsSelected = false;
            }

            if (module != null)
            {
                module.IsSelected = true;
                SelectedModule = module;
                LogService.LogInfo($"Selected module '{module.Name}'");
            }
        }

        /// <summary>
        /// Updates the position of a module on the canvas
        /// </summary>
        [RelayCommand]
        private void UpdateModulePosition((SeceModule Module, double X, double Y) moveInfo)
        {
            if (moveInfo.Module != null)
            {
                moveInfo.Module.X = moveInfo.X;
                moveInfo.Module.Y = moveInfo.Y;
            }
        }

        /// <summary>
        /// Updates the size of a module on the canvas
        /// </summary>
        [RelayCommand]
        private void UpdateModuleSize((SeceModule Module, double Width, double Height) sizeInfo)
        {
            if (sizeInfo.Module != null)
            {
                sizeInfo.Module.Width = sizeInfo.Width;
                sizeInfo.Module.Height = sizeInfo.Height;
            }
        }

        /// <summary>
        /// Updates the displayed mouse position
        /// </summary>
        [RelayCommand]
        private void UpdateMousePosition(Point position)
        {
            MousePosition = $"Mouse Position: X:{position.X:F0}, Y:{position.Y:F0}";
        }

        /// <summary>
        /// Deletes the currently selected item (module or connection)
        /// </summary>
        [RelayCommand]
        private void DeleteSelectedItem()
        {
            if (SelectedModule != null)
            {
                // Find all connections related to this module
                var relatedConnections = Connections
                    .Where(c => 
                    {
                        var sourcePoint = c.StartPoint;
                        var targetPoint = c.EndPoint;
                        
                        // Check module bounds
                        var moduleLeft = SelectedModule.X;
                        var moduleTop = SelectedModule.Y;
                        var moduleRight = moduleLeft + SelectedModule.Width;
                        var moduleBottom = moduleTop + SelectedModule.Height;

                        // Check if connection points are within module bounds
                        bool isSourceInModule = sourcePoint.X >= moduleLeft && sourcePoint.X <= moduleRight &&
                                             sourcePoint.Y >= moduleTop && sourcePoint.Y <= moduleBottom;
                        bool isTargetInModule = targetPoint.X >= moduleLeft && targetPoint.X <= moduleRight &&
                                             targetPoint.Y >= moduleTop && targetPoint.Y <= moduleBottom;

                        return isSourceInModule || isTargetInModule;
                    })
                    .ToList();

                // Remove related connections
                foreach (var connection in relatedConnections)
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var canvas = mainWindow.GetMainCanvas();
                        canvas?.Children.Remove(connection);
                    }

                    Connections.Remove(connection);
                }

                CanvasModules.Remove(SelectedModule);
                SelectedModule = null;
                LogService.LogInfo("Deleted module and its connections");
            }
            else if (SelectedConnection != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var canvas = mainWindow.GetMainCanvas();
                    canvas?.Children.Remove(SelectedConnection);
                }

                Connections.Remove(SelectedConnection);
                SelectedConnection = null;
                LogService.LogInfo("Deleted connection");
            }
        }

        /// <summary>
        /// Finds the parent DraggableModule in the visual tree
        /// </summary>
        private DraggableModule FindParentModule(FrameworkElement element)
        {
            DependencyObject current = element;
            while (current != null)
            {
                if (current is DraggableModule module)
                {
                    return module;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// Handles connection selection, updating the visual state accordingly
        /// </summary>
        [RelayCommand]
        private void SelectConnection(ConnectionLine connection)
        {
            if (SelectedConnection != null)
            {
                SelectedConnection.IsSelected = false;
            }

            if (connection != null)
            {
                connection.IsSelected = true;
                SelectedConnection = connection;
                if (SelectedModule != null)
                {
                    SelectedModule.IsSelected = false;
                    SelectedModule = null;
                }
                LogService.LogInfo("Selected connection");
            }
        }

        /// <summary>
        /// Loads a configuration from a JSON file
        /// </summary>
        public async Task LoadConfiguration()
        {
            try
            {
                var filePath = _fileService.ShowOpenFileDialog();
                if (filePath == null) return;

                LogService.LogInfo("Starting file load");
                SeceModule.BeginDeserialization();

                var (modules, connections) = await _fileService.LoadConfiguration(filePath);

                // Clear existing modules and connections
                CanvasModules.Clear();
                Connections.Clear();

                // Add modules
                foreach (var module in modules)
                {
                    LogService.LogInfo($"Adding module: {module.Name}, Position: ({module.X}, {module.Y}), ID: {module.Id}");
                    CanvasModules.Add(module);
                }

                // Wait for UI update
                await Application.Current.Dispatcher.InvokeAsync(async () => 
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var canvas = mainWindow.GetMainCanvas();
                        if (canvas != null)
                        {
                            canvas.UpdateLayout();
                            await Task.Delay(100);

                            foreach (var connectionInfo in connections)
                            {
                                await CreateConnection(canvas, connectionInfo);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error loading file");
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateConnection(Canvas canvas, ConnectionInfo connectionInfo)
        {
            LogService.LogInfo($"Attempting to create connection - Source ID: {connectionInfo.SourceModuleId}, Target ID: {connectionInfo.TargetModuleId}");
            
            var sourceModule = CanvasModules.FirstOrDefault(m => m.Id == connectionInfo.SourceModuleId);
            var targetModule = CanvasModules.FirstOrDefault(m => m.Id == connectionInfo.TargetModuleId);

            if (sourceModule != null && targetModule != null)
            {
                var sourceVisual = await FindModuleVisualWithRetry(canvas, sourceModule.Id);
                var targetVisual = await FindModuleVisualWithRetry(canvas, targetModule.Id);

                if (sourceVisual != null && targetVisual != null)
                {
                    var connection = new ConnectionLine
                    {
                        SourceModule = sourceModule,
                        TargetModule = targetModule
                    };

                    if (sourceVisual.OutputConnector != null && targetVisual.InputConnector != null)
                    {
                        connection.SourceConnector = sourceVisual.OutputConnector;
                        connection.TargetConnector = targetVisual.InputConnector;

                        canvas.Children.Add(connection);
                        connection.SetValue(Panel.ZIndexProperty, -1);
                        await Task.Delay(100);
                        connection.UpdateConnectionPoints();
                        Connections.Add(connection);
                        
                        LogService.LogInfo($"Successfully created connection - Source: {connectionInfo.SourceModuleId}, Target: {connectionInfo.TargetModuleId}");
                    }
                    else
                    {
                        LogService.LogWarning("Could not find connectors - Source or target connector is null");
                    }
                }
                else
                {
                    LogService.LogWarning($"Could not find module visuals, Source: {(sourceVisual == null ? "not found" : "found")}, Target: {(targetVisual == null ? "not found" : "found")}");
                }
            }
        }

        /// <summary>
        /// Saves the current configuration to a JSON file
        /// </summary>
        public async Task SaveConfiguration()
        {
            try
            {
                LogService.LogInfo("Starting file save");
                var filePath = _fileService.ShowSaveFileDialog();
                if (filePath == null) return;

                var connectionInfos = Connections.Select(c => c.ToConnectionInfo()).ToList();
                await _fileService.SaveConfiguration(filePath, CanvasModules, new ObservableCollection<ConnectionInfo>(connectionInfos));
                LogService.LogInfo("File save completed");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error saving file");
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Attempts to find a module's visual element with multiple retries
        /// </summary>
        private async Task<DraggableModule> FindModuleVisualWithRetry(Canvas canvas, string moduleId, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var visual = FindModuleVisual(canvas, moduleId);
                if (visual != null)
                {
                    await Task.Delay(200);

                    if (visual.InputConnector != null && visual.OutputConnector != null)
                    {
                        LogService.LogInfo($"Module {moduleId} connectors initialized");
                        return visual;
                    }
                    else
                    {
                        LogService.LogWarning($"Module {moduleId} connectors not initialized, waiting for next retry");
                    }
                }
                LogService.LogInfo($"Attempt {i + 1} to find module visual {moduleId}");
                await Task.Delay(500);
            }
            return null;
        }

        /// <summary>
        /// Finds a module's visual element in the visual tree
        /// </summary>
        private DraggableModule FindModuleVisual(Canvas canvas, string moduleId)
        {
            var moduleItemsControl = canvas.Children.OfType<ItemsControl>()
                .Where(ic => ic.ItemsSource != null)
                .LastOrDefault();

            if (moduleItemsControl == null)
            {
                LogService.LogWarning("Module ItemsControl not found");
                return null;
            }

            moduleItemsControl.UpdateLayout();

            var itemsPanel = VisualTreeHelper.GetChild(moduleItemsControl, 0) as FrameworkElement;
            if (itemsPanel == null)
            {
                LogService.LogWarning("ItemsPanel not found");
                return null;
            }

            LogService.LogInfo($"Searching for module {moduleId}, ItemsPanel has {VisualTreeHelper.GetChildrenCount(itemsPanel)} children");

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(itemsPanel);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current is DraggableModule draggableModule)
                {
                    var module = draggableModule.DataContext as SeceModule;
                    if (module?.Id == moduleId)
                    {
                        LogService.LogInfo($"Found module {moduleId}");
                        return draggableModule;
                    }
                }

                if (current is ContentPresenter presenter)
                {
                    var child = VisualTreeHelper.GetChild(presenter, 0);
                    if (child is DraggableModule module)
                    {
                        var seceModule = module.DataContext as SeceModule;
                        if (seceModule?.Id == moduleId)
                        {
                            LogService.LogInfo($"Found module {moduleId}");
                            return module;
                        }
                    }
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(current); i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child != null)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            LogService.LogWarning($"Module with ID: {moduleId} not found in visual tree");
            return null;
        }

        /// <summary>
        /// Clears the canvas of all modules and connections
        /// </summary>
        [RelayCommand]
        private void NewCanvas()
        {
            CanvasModules.Clear();
            Connections.Clear();
            StatusMessage = "Canvas cleared";
            LogService.LogInfo("Canvas cleared");
        }
    }
}

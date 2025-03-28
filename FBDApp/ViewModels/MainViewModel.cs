// <copyright file="MainViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace FBDApp.ViewModels
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using FBDApp.Controls;
    using FBDApp.Models;
    using FBDApp.Services;

    /// <summary>
    /// Main ViewModel for the application, handling the core business logic and UI state management.
    /// Implements MVVM pattern using CommunityToolkit.Mvvm.
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IFileService fileService;

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

        [ObservableProperty]
        private string connectionStatus = string.Empty;

        /// <summary>
        /// Gets collection of template modules available for dragging onto the canvas.
        /// </summary>
        public ObservableCollection<SeceModule> AvailableModules { get; } = new ();

        /// <summary>
        /// Gets collection of modules currently placed on the canvas.
        /// </summary>
        public ObservableCollection<SeceModule> CanvasModules { get; } = new ();

        /// <summary>
        /// Gets collection of connections between modules on the canvas.
        /// </summary>
        public ObservableCollection<ConnectionLine> Connections { get; } = new ();

        /// <summary>
        /// Gets the command that handles saving the current canvas configuration.
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// Gets the command that handles loading a canvas configuration from a file.
        /// </summary>
        public ICommand LoadCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        /// <param name="fileService">Service for handling file operations.</param>
        public MainViewModel(IFileService fileService)
        {
            this.fileService = fileService;
            LogService.LogInfo("[FBDApp] MainViewModel constructor called");
            this.InitializeModules();
            this.CanvasModules.CollectionChanged += (s, e) =>
            {
                this.UpdateStatusMessage();
            };
            this.SaveCommand = new RelayCommand(async () => await this.SaveConfiguration());
            this.LoadCommand = new RelayCommand(async () => await this.LoadConfiguration());

            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(this.SelectedModule) ||
                    e.PropertyName == nameof(this.SelectedConnection))
                {
                    this.UpdateStatusMessage();
                }
            };
        }

        /// <summary>
        /// Updates the status message with current canvas information.
        /// </summary>
        private void UpdateStatusMessage()
        {
            this.StatusMessage = $"Modules on canvas: {this.CanvasModules.Count}";
            LogService.LogInfo("[FBDApp] Canvas modules count updated: {this.CanvasModules.Count}");
        }

        /// <summary>
        /// Initializes the available module templates.
        /// </summary>
        private void InitializeModules()
        {
            LogService.LogInfo("[FBDApp] Initializing available modules");
            var moduleTypes = new[]
            {
                (Name: "Tool Status", Type: ModuleType.ToolStatus),
                (Name: "Tool Control", Type: ModuleType.ToolControl),
                (Name: "Exception Handling", Type: ModuleType.ExceptionHandling),
                (Name: "Data Collection", Type: ModuleType.DataCollection),
                (Name: "Process Program", Type: ModuleType.ProcessProgram),
                (Name: "Error", Type: ModuleType.Error),
                (Name: "Terminal Services", Type: ModuleType.TerminalServices),
            };

            foreach (var module in moduleTypes)
            {
                var newModule = new SeceModule(module.Name, module.Type);
                LogService.LogInfo($"[FBDApp] Created template module: {module.Name} with ID: {newModule.Id}");
                this.AvailableModules.Add(newModule);
            }
        }

        /// <summary>
        /// Handles the drop event when a module is dropped onto the canvas.
        /// </summary>
        [RelayCommand]
        private void ModuleDrop((double X, double Y, SeceModule Module) dropInfo)
        {
            var newModule = dropInfo.Module.Clone();
            newModule.X = dropInfo.X;
            newModule.Y = dropInfo.Y;

            this.CanvasModules.Add(newModule);
            LogService.LogInfo($"[FBDApp] Added module '{newModule.Name}' (ID: {newModule.Id}) at position X:{newModule.X}, Y:{newModule.Y}");
        }

        /// <summary>
        /// Handles module selection, updating the visual state accordingly.
        /// </summary>
        [RelayCommand]
        private void SelectModule(SeceModule module)
        {
            if (this.SelectedModule != null)
            {
                this.SelectedModule.IsSelected = false;
            }

            if (module != null)
            {
                module.IsSelected = true;
                this.SelectedModule = module;
                LogService.LogInfo($"[FBDApp] Selected module '{module.Name}'");
            }
        }

        /// <summary>
        /// Updates the position of a module on the canvas.
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
        /// Updates the size of a module on the canvas.
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
        /// Updates the displayed mouse position.
        /// </summary>
        [RelayCommand]
        private void UpdateMousePosition(Point position)
        {
            this.MousePosition = $"Mouse Position: X:{position.X:F0}, Y:{position.Y:F0}";
        }

        /// <summary>
        /// Deletes the currently selected item (module or connection).
        /// </summary>
        [RelayCommand]
        private void DeleteSelectedItem()
        {
            if (this.SelectedModule != null)
            {
                // Find all connections related to this module
                var relatedConnections = this.Connections
                    .Where(c =>
                    {
                        var sourcePoint = c.StartPoint;
                        var targetPoint = c.EndPoint;

                        // Check module bounds
                        var moduleLeft = this.SelectedModule.X;
                        var moduleTop = this.SelectedModule.Y;
                        var moduleRight = moduleLeft + this.SelectedModule.Width;
                        var moduleBottom = moduleTop + this.SelectedModule.Height;

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

                    _ = this.Connections.Remove(connection);
                }

                _ = this.CanvasModules.Remove(this.SelectedModule);
                this.SelectedModule = null;
                LogService.LogInfo("[FBDApp] Deleted module and its connections");
            }
            else if (this.SelectedConnection != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var canvas = mainWindow.GetMainCanvas();
                    canvas?.Children.Remove(this.SelectedConnection);
                }

                _ = this.Connections.Remove(this.SelectedConnection);
                this.SelectedConnection = null;
                LogService.LogInfo("[FBDApp] Deleted connection");
            }
        }

        /// <summary>
        /// Finds the parent DraggableModule in the visual tree.
        /// </summary>
        private DraggableModule? FindParentModule(FrameworkElement element)
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
        /// Handles connection selection, updating the visual state accordingly.
        /// </summary>
        [RelayCommand]
        private void SelectConnection(ConnectionLine connection)
        {
            if (this.SelectedConnection != null)
            {
                this.SelectedConnection.IsSelected = false;
            }

            if (connection != null)
            {
                connection.IsSelected = true;
                this.SelectedConnection = connection;
                if (this.SelectedModule != null)
                {
                    this.SelectedModule.IsSelected = false;
                    this.SelectedModule = null;
                }

                LogService.LogInfo("[FBDApp] Selected connection");
            }
        }

        /// <summary>
        /// Loads a configuration from a JSON file.
        /// </summary>
        public async Task LoadConfiguration()
        {
            try
            {
                LogService.LogInfo("[FBDApp] Starting file load");
                SeceModule.BeginDeserialization();

                var filePath = this.fileService.ShowOpenFileDialog();
                if (filePath == null)
                {
                    return;
                }

                var (modules, connections) = await this.fileService.LoadConfiguration(filePath);

                // Clear existing modules and connections
                this.CanvasModules.Clear();
                this.Connections.Clear();

                // Add modules
                foreach (var module in modules)
                {
                    LogService.LogInfo($"[FBDApp] Adding module: {module.Name}, Position: ({module.X}, {module.Y}), ID: {module.Id}");
                    this.CanvasModules.Add(module);
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
                                await this.CreateConnection(canvas, connectionInfo);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "[FBDApp] Error loading file");
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateConnection(Canvas canvas, ConnectionInfo connectionInfo)
        {
            LogService.LogInfo($"[FBDApp] Attempting to create connection - Source ID: {connectionInfo.SourceModuleId}, Target ID: {connectionInfo.TargetModuleId}");

            var sourceModule = this.CanvasModules.FirstOrDefault(m => m.Id == connectionInfo.SourceModuleId);
            var targetModule = this.CanvasModules.FirstOrDefault(m => m.Id == connectionInfo.TargetModuleId);

            if (sourceModule != null && targetModule != null)
            {
                var sourceVisual = await this.FindModuleVisualWithRetry(canvas, sourceModule.Id);
                var targetVisual = await this.FindModuleVisualWithRetry(canvas, targetModule.Id);

                if (sourceVisual != null && targetVisual != null)
                {
                    var connection = new ConnectionLine
                    {
                        SourceModule = sourceModule,
                        TargetModule = targetModule,
                    };

                    if (sourceVisual.OutputConnector != null && targetVisual.InputConnector != null)
                    {
                        connection.SourceConnector = sourceVisual.OutputConnector;
                        connection.TargetConnector = targetVisual.InputConnector;

                        canvas.Children.Add(connection);
                        connection.SetValue(Panel.ZIndexProperty, -1);
                        await Task.Delay(100);
                        connection.UpdateConnectionPoints();
                        this.Connections.Add(connection);

                        LogService.LogInfo($"[FBDApp] Successfully created connection - Source: {connectionInfo.SourceModuleId}, Target: {connectionInfo.TargetModuleId}");
                    }
                    else
                    {
                        LogService.LogWarning("[FBDApp] Could not find connectors - Source or target connector is null");
                    }
                }
                else
                {
                    LogService.LogWarning($"[FBDApp] Could not find module visuals, Source: {(sourceVisual == null ? "not found" : "found")}, Target: {(targetVisual == null ? "not found" : "found")}");
                }
            }
        }

        /// <summary>
        /// Saves the current configuration to a JSON file.
        /// </summary>
        public async Task SaveConfiguration()
        {
            try
            {
                LogService.LogInfo("[FBDApp] Starting file save");
                var filePath = this.fileService.ShowSaveFileDialog();
                if (filePath == null)
                {
                    return;
                }

                var connectionInfos = this.Connections.Select(c => c.ToConnectionInfo()).ToList();
                await this.fileService.SaveConfiguration(filePath, this.CanvasModules, new ObservableCollection<ConnectionInfo>(connectionInfos));
                LogService.LogInfo("[FBDApp] File save completed");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "[FBDApp] Error saving file");
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Attempts to find a module's visual element with multiple retries.
        /// </summary>
        private async Task<DraggableModule> FindModuleVisualWithRetry(Canvas canvas, string moduleId, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                var visual = this.FindModuleVisual(canvas, moduleId);
                if (visual != null)
                {
                    await Task.Delay(200);

                    if (visual.InputConnector != null && visual.OutputConnector != null)
                    {
                        LogService.LogInfo($"[FBDApp] Module {moduleId} connectors initialized");
                        return visual;
                    }
                    else
                    {
                        LogService.LogWarning($"[FBDApp] Module {moduleId} connectors not initialized, waiting for next retry");
                    }
                }

                LogService.LogInfo($"[FBDApp] Attempt {i + 1} to find module visual {moduleId}");
                await Task.Delay(500);
            }

            return null;
        }

        /// <summary>
        /// Finds a module's visual element in the visual tree.
        /// </summary>
        private DraggableModule FindModuleVisual(Canvas canvas, string moduleId)
        {
            var moduleItemsControl = canvas.Children.OfType<ItemsControl>()
                .Where(ic => ic.ItemsSource != null)
                .LastOrDefault();

            if (moduleItemsControl == null)
            {
                LogService.LogWarning("[FBDApp] Module ItemsControl not found");
                return null;
            }

            moduleItemsControl.UpdateLayout();

            var itemsPanel = VisualTreeHelper.GetChild(moduleItemsControl, 0) as FrameworkElement;
            if (itemsPanel == null)
            {
                LogService.LogWarning("[FBDApp] ItemsPanel not found");
                return null;
            }

            LogService.LogInfo($"[FBDApp] Searching for module {moduleId}, ItemsPanel has {VisualTreeHelper.GetChildrenCount(itemsPanel)} children");

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
                        LogService.LogInfo($"[FBDApp] Found module {moduleId}");
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
                            LogService.LogInfo($"[FBDApp] Found module {moduleId}");
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

            LogService.LogWarning($"[FBDApp] Module with ID: {moduleId} not found in visual tree");
            return null;
        }

        /// <summary>
        /// Clears the canvas of all modules and connections.
        /// </summary>
        [RelayCommand]
        private void NewCanvas()
        {
            this.CanvasModules.Clear();
            this.Connections.Clear();
            this.StatusMessage = "Canvas cleared";
            LogService.LogInfo("[FBDApp] Canvas cleared");
        }
    }
}

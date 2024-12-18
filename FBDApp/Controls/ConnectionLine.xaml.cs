using FBDApp.Models;
using FBDApp.Services;
using FBDApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FBDApp.Controls
{
    public partial class ConnectionLine : UserControl
    {
        private bool isSelected;
        private SeceModule sourceModule;
        private SeceModule targetModule;

        public static readonly DependencyProperty LineStrokeProperty =
            DependencyProperty.Register(nameof(LineStroke), typeof(Brush), typeof(ConnectionLine),
                new PropertyMetadata(Brushes.Black));

        public Brush LineStroke
        {
            get => (Brush)GetValue(LineStrokeProperty);
            set => SetValue(LineStrokeProperty, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                UpdateAppearance();
            }
        }

        public SeceModule SourceModule
        {
            get => sourceModule;
            set
            {
                sourceModule = value;
                UpdateConnectionPoints();
            }
        }

        public SeceModule TargetModule
        {
            get => targetModule;
            set
            {
                targetModule = value;
                UpdateConnectionPoints();
            }
        }

        public Point StartPoint
        {
            get => (Point)GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }

        public Point EndPoint
        {
            get => (Point)GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register("StartPoint", typeof(Point), typeof(ConnectionLine),
                new PropertyMetadata(new Point(), OnPointChanged));

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register("EndPoint", typeof(Point), typeof(ConnectionLine),
                new PropertyMetadata(new Point(), OnPointChanged));

        public Ellipse SourceConnector { get; set; }
        public Ellipse TargetConnector { get; set; }

        public ConnectionLine()
        {
            InitializeComponent();
            Loaded += ConnectionLine_Loaded;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
        }

        /// <summary>
        /// Handles the loaded event of the connection line
        /// </summary>
        private void ConnectionLine_Loaded(object sender, RoutedEventArgs e)
        {
            // Add location changed event handlers for source and target modules
            if (SourceConnector != null)
            {
                var sourceModule = FindParentModule(SourceConnector);
                if (sourceModule != null)
                {
                    sourceModule.LocationChanged += Module_LocationChanged;
                    sourceModule.SizeChanged += Module_LocationChanged;
                }
            }

            if (TargetConnector != null)
            {
                var targetModule = FindParentModule(TargetConnector);
                if (targetModule != null)
                {
                    targetModule.LocationChanged += Module_LocationChanged;
                    targetModule.SizeChanged += Module_LocationChanged;
                }
            }
        }

        /// <summary>
        /// Handles location changes of connected modules
        /// </summary>
        private void Module_LocationChanged(object sender, EventArgs e)
        {
            UpdateConnectionPoints();
        }

        /// <summary>
        /// Updates the connection points based on current module positions
        /// </summary>
        public void UpdateConnectionPoints()
        {
            if (SourceModule == null || TargetModule == null)
            {
                LogService.LogWarning("UpdateConnectionPoints: Source or Target module is null");
                return;
            }

            try
            {
                var canvas = this.Parent as Canvas;
                if (canvas == null)
                {
                    LogService.LogWarning("UpdateConnectionPoints: Parent canvas is null");
                    return;
                }

                // Ensure connection points are initialized
                if (SourceConnector == null || TargetConnector == null)
                {
                    LogService.LogWarning("UpdateConnectionPoints: Connectors are not initialized");
                    return;
                }

                // Ensure connection points' sizes are calculated
                if (SourceConnector.ActualWidth == 0 || TargetConnector.ActualWidth == 0)
                {
                    LogService.LogWarning("UpdateConnectionPoints: Connector sizes are not calculated yet");
                    return;
                }

                Point startPoint, endPoint;

                // Use connector positions
                startPoint = SourceConnector.TranslatePoint(
                    new Point(SourceConnector.ActualWidth / 2, SourceConnector.ActualHeight / 2),
                    canvas);

                endPoint = TargetConnector.TranslatePoint(
                    new Point(TargetConnector.ActualWidth / 2, TargetConnector.ActualHeight / 2),
                    canvas);

                StartPoint = startPoint;
                EndPoint = endPoint;

                // Update path
                UpdatePath();

                LogService.LogInfo($"Updated connection points - Source: ({startPoint.X}, {startPoint.Y}), Target: ({endPoint.X}, {endPoint.Y})");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "UpdateConnectionPoints failed");
            }
        }

        /// <summary>
        /// Handles changes to start or end points of the connection
        /// </summary>
        private static void OnPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var connection = d as ConnectionLine;
            connection?.UpdatePath();
        }

        /// <summary>
        /// Updates the path geometry of the connection line
        /// </summary>
        private void UpdatePath()
        {
            try
            {
                if (ConnectionPath == null)
                {
                    LogService.LogWarning("UpdatePath: ConnectionPath is null");
                    return;
                }

                // Calculate control points
                var controlPoint1 = new Point(StartPoint.X + 50, StartPoint.Y);
                var controlPoint2 = new Point(EndPoint.X - 50, EndPoint.Y);

                // Create Bezier curve
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure { StartPoint = StartPoint };
                var bezierSegment = new BezierSegment(controlPoint1, controlPoint2, EndPoint, true);
                pathFigure.Segments.Add(bezierSegment);
                pathGeometry.Figures.Add(pathFigure);

                // Set path
                ConnectionPath.Data = pathGeometry;
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "UpdatePath failed");
            }
        }

        /// <summary>
        /// Finds a module in the visual tree by its ID
        /// </summary>
        /// <param name="canvas">Canvas to search in</param>
        /// <param name="moduleId">ID of the module to find</param>
        /// <returns>The found module or null if not found</returns>
        private DraggableModule FindModuleVisual(Canvas canvas, string moduleId)
        {
            if (string.IsNullOrEmpty(moduleId)) return null;

            // Find the module's ItemsControl (second one, as first is for connection lines)
            var moduleItemsControl = canvas.Children.OfType<ItemsControl>()
                .Where(ic => ic.ItemsSource != null)
                .LastOrDefault();

            if (moduleItemsControl == null) return null;

            // Get the ItemsControl's panel (Canvas)
            var itemsPanel = VisualTreeHelper.GetChild(moduleItemsControl, 0) as FrameworkElement;
            if (itemsPanel == null) return null;

            // Search for the module in the panel
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(itemsPanel); i++)
            {
                var child = VisualTreeHelper.GetChild(itemsPanel, i);
                if (child is ContentPresenter presenter)
                {
                    var draggableModule = VisualTreeHelper.GetChild(presenter, 0) as DraggableModule;
                    if (draggableModule != null)
                    {
                        var module = draggableModule.DataContext as SeceModule;
                        if (module?.Id == moduleId)
                        {
                            return draggableModule;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the parent DraggableModule of a framework element
        /// </summary>
        /// <param name="element">Element to find parent for</param>
        /// <returns>Parent module or null if not found</returns>
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
        /// Updates the visual appearance of the connection line
        /// </summary>
        private void UpdateAppearance()
        {
            if (IsSelected)
            {
                ConnectionPath.Stroke = Brushes.Blue;
                ConnectionPath.StrokeThickness = 3;
            }
            else
            {
                ConnectionPath.Stroke = Brushes.Black;
                ConnectionPath.StrokeThickness = 2;
            }
        }

        /// <summary>
        /// Handles mouse enter events on the connection line
        /// </summary>
        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            // Change the stroke color to blue if the connection is not selected
            if (!IsSelected)
            {
                ConnectionPath.Stroke = Brushes.Blue;
            }

            // Get the main view model from the application's main window
            var mainViewModel = Application.Current.MainWindow?.DataContext as MainViewModel;

            // Check if main view model and connectors are available
            if (mainViewModel != null && SourceConnector != null && TargetConnector != null)
            {
                // Find the parent modules for source and target connectors
                var sourceModuleVisual = FindParentModule(SourceConnector);
                var targetModuleVisual = FindParentModule(TargetConnector);
                
                // Check if both source and target modules are valid SeceModules
                if (sourceModuleVisual?.DataContext is SeceModule sourceModule && 
                    targetModuleVisual?.DataContext is SeceModule targetModule)
                {
                    // Update the connection status in the main view model
                    mainViewModel.ConnectionStatus = $"Connection: {sourceModule.Name} → {targetModule.Name}";
                }
            }
        }

        /// <summary>
        /// Handles mouse leave events on the connection line
        /// </summary>
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!IsSelected)
            {
                ConnectionPath.Stroke = Brushes.Black;
            }
            var mainViewModel = Application.Current.MainWindow?.DataContext as MainViewModel;
            if (mainViewModel != null)
            {
                mainViewModel.ConnectionStatus = string.Empty;
            }
        }

        /// <summary>
        /// Handles mouse left button down events for selection
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mainViewModel = Application.Current.MainWindow?.DataContext as MainViewModel;
            mainViewModel?.SelectConnectionCommand.Execute(this);
            e.Handled = true;
        }

        /// <summary>
        /// Sets up the connection between two modules
        /// </summary>
        /// <param name="sourceModule">Source module of the connection</param>
        /// <param name="targetModule">Target module of the connection</param>
        public void SetConnectors(DraggableModule sourceModule, DraggableModule targetModule)
        {
            if (sourceModule != null && targetModule != null)
            {
                SourceConnector = sourceModule.OutputConnector;
                TargetConnector = targetModule.InputConnector;

                if (SourceConnector != null)
                {
                    var sourceParent = sourceModule;
                    sourceParent.LocationChanged += Module_LocationChanged;
                    sourceParent.SizeChanged += Module_LocationChanged;
                }

                if (TargetConnector != null)
                {
                    var targetParent = targetModule;
                    targetParent.LocationChanged += Module_LocationChanged;
                    targetParent.SizeChanged += Module_LocationChanged;
                }

                UpdateConnectionPoints();
            }
        }
    }
}

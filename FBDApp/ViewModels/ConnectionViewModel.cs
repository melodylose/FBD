using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using FBDApp.Models;
using FBDApp.Controls;
using FBDApp.Services;

namespace FBDApp.ViewModels
{
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private readonly MainViewModel _mainViewModel;
        private bool _isConnecting;
        private ConnectionLine _temporaryConnection;
        private Ellipse _sourceConnector;
        private string _connectionStatus;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsConnecting
        {
            get => _isConnecting;
            private set
            {
                if (_isConnecting != value)
                {
                    _isConnecting = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public ConnectionLine TemporaryConnection => _temporaryConnection;
        public Ellipse SourceConnector => _sourceConnector;

        public ConnectionViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        public void StartConnection(Ellipse sourceConnector, Point startPoint, Canvas canvas)
        {
            LogService.LogInfo($"Starting connection: Dragging from {(sourceConnector.Name == "InputConnection" ? "input point" : "output point")}");

            IsConnecting = true;
            _sourceConnector = sourceConnector;

            // Create temporary connection line
            _temporaryConnection = new ConnectionLine
            {
                StartPoint = startPoint,
                EndPoint = startPoint,
                SourceConnector = _sourceConnector
            };

            // Add to Canvas
            canvas.Children.Add(_temporaryConnection);
            Canvas.SetZIndex(_temporaryConnection, 1000);
        }

        /// <summary>
        /// Updates the temporary connection line's endpoint as the mouse moves
        /// </summary>
        /// <param name="currentPoint">Current mouse position</param>
        public void UpdateConnection(Point currentPoint)
        {
            if (IsConnecting && _temporaryConnection != null)
            {
                _temporaryConnection.EndPoint = currentPoint;
            }
        }

        public void HandleConnectionEnd(Point endPoint, Canvas canvas)
        {
            LogService.LogInfo("Starting to handle connection endpoint");

            if (!IsConnecting || _temporaryConnection == null) return;

            try
            {
                // Use hit testing to find target connection point
                var hitTestParameters = new PointHitTestParameters(endPoint);
                var hitTestResults = new List<DependencyObject>();

                VisualTreeHelper.HitTest(canvas, null,
                    new HitTestResultCallback(result =>
                    {
                        var frameworkElement = result.VisualHit as FrameworkElement;
                        if (frameworkElement != null)
                        {
                            LogService.LogInfo($"Hit element: {frameworkElement.GetType().Name}, Name: {frameworkElement.Name}");
                            hitTestResults.Add(result.VisualHit);
                        }
                        return HitTestResultBehavior.Continue;
                    }),
                    hitTestParameters);

                // Find nearest valid connection point
                Ellipse targetConnector = FindNearestConnectionPoint(hitTestResults, endPoint, canvas);

                if (targetConnector != null && ValidateConnection(targetConnector))
                {
                    CreatePermanentConnection(targetConnector, canvas);
                }
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error occurred while handling connection endpoint");
            }
            finally
            {
                CancelConnection(canvas);
            }
        }

        /// <summary>
        /// Finds the nearest connection point from the hit test results
        /// </summary>
        /// <param name="hitTestResults">List of visual elements from hit testing</param>
        /// <param name="mousePoint">Current mouse position</param>
        /// <param name="canvas">Canvas containing the connection points</param>
        /// <returns>The nearest connection point or null if none found</returns>
        private Ellipse FindNearestConnectionPoint(List<DependencyObject> hitTestResults, Point mousePoint, Canvas canvas)
        {
            // Initialize variables to track the closest connection point
            Ellipse targetElement = null;
            double closestDistance = double.MaxValue;

            // Iterate through all hit test results
            foreach (var element in hitTestResults)
            {
                // Find a connection point in the current element
                var foundElement = FindConnectionPoint(element);
                
                // Check if a valid connection point was found and it's not the source connector
                if (foundElement != null && foundElement != _sourceConnector)
                {
                    // Calculate the center point of the found element
                    Point elementCenter = foundElement.TranslatePoint(
                        new Point(foundElement.Width / 2, foundElement.Height / 2),
                        canvas);

                    // Calculate the distance between the mouse point and the element center
                    double distance = Math.Sqrt(
                        Math.Pow(elementCenter.X - mousePoint.X, 2) +
                        Math.Pow(elementCenter.Y - mousePoint.Y, 2));

                    // Update the target element if this is the closest one so far
                    if (distance < closestDistance)
                    {
                        targetElement = foundElement;
                        closestDistance = distance;
                    }
                }
            }

            // Return the closest connection point found, or null if none was found
            return targetElement;
        }

        /// <summary>
        /// Recursively searches for a connection point (input or output) in the visual tree
        /// </summary>
        /// <param name="element">Starting element to search from</param>
        /// <returns>Found connection point or null if none found</returns>
        private Ellipse FindConnectionPoint(DependencyObject element)
        {
            // Return null if the input element is null
            if (element == null) return null;

            // Initialize a queue for breadth-first search and a set to track visited elements
            Queue<DependencyObject> queue = new Queue<DependencyObject>();
            HashSet<DependencyObject> visited = new HashSet<DependencyObject>();
            queue.Enqueue(element);

            while (queue.Count > 0)
            {
                // Dequeue the next element and skip if already visited
                var current = queue.Dequeue();
                if (visited.Contains(current)) continue;

                // Mark the current element as visited
                visited.Add(current);

                // Check if the current element is a connection point
                if (current is Ellipse ellipse &&
                    (ellipse.Name == "InputConnection" || ellipse.Name == "OutputConnection"))
                {
                    return ellipse;
                }

                // Enqueue all child elements
                int childCount = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (!visited.Contains(child))
                    {
                        queue.Enqueue(child);
                    }
                }

                // Enqueue the parent element if it exists and hasn't been visited
                var parent = VisualTreeHelper.GetParent(current);
                if (parent != null && !visited.Contains(parent))
                {
                    queue.Enqueue(parent);
                }
            }

            // Return null if no connection point is found
            return null;
        }

        private bool ValidateConnection(Ellipse targetConnector)
        {
            if (targetConnector == null || _sourceConnector == null) return false;

            bool isSourceInput = _sourceConnector.Name == "InputConnection";
            bool isTargetInput = targetConnector.Name == "InputConnection";

            // Ensure one is input and one is output
            if (isSourceInput == isTargetInput)
            {
                ConnectionStatus = "Invalid connection: Both connection points are of the same type";
                return false;
            }

            // Ensure not connecting to the same module
            var sourceModule = FindParentModule(_sourceConnector);
            var targetModule = FindParentModule(targetConnector);

            if (sourceModule == null || targetModule == null || 
                sourceModule == targetModule)
            {
                ConnectionStatus = "Invalid connection: Cannot connect to the same module";
                return false;
            }

            return true;
        }

        private void CreatePermanentConnection(Ellipse targetConnector, Canvas canvas)
        {
            try
            {
                var connection = new ConnectionLine
                {
                    StartPoint = _temporaryConnection.StartPoint,
                    EndPoint = targetConnector.TranslatePoint(
                        new Point(targetConnector.Width / 2, targetConnector.Height / 2),
                        canvas),
                    SourceConnector = _sourceConnector,
                    TargetConnector = targetConnector
                };

                // Add to Canvas
                canvas.Children.Add(connection);
                Canvas.SetZIndex(connection, 100);

                // Add to ViewModel's Connections collection
                _mainViewModel.Connections.Add(connection);

                // Subscribe to module's location and size change events
                var sourceModule = FindParentModule(_sourceConnector);
                var targetModule = FindParentModule(targetConnector);

                if (sourceModule != null)
                {
                    sourceModule.LocationChanged += (s, e) => UpdateConnectionPoint(connection, true, canvas);
                    sourceModule.SizeChanged += (s, e) => UpdateConnectionPoint(connection, true, canvas);
                }

                if (targetModule != null)
                {
                    targetModule.LocationChanged += (s, e) => UpdateConnectionPoint(connection, false, canvas);
                    targetModule.SizeChanged += (s, e) => UpdateConnectionPoint(connection, false, canvas);
                }

                LogService.LogInfo("Successfully created permanent connection");
            }
            catch (Exception ex)
            {
                LogService.LogError(ex, "Error occurred while creating permanent connection");
            }
        }

        /// <summary>
        /// Updates the position of a connection line's endpoint
        /// </summary>
        /// <param name="connection">Connection line to update</param>
        /// <param name="isSource">True to update source point, false to update target point</param>
        /// <param name="canvas">Canvas containing the connection</param>
        private void UpdateConnectionPoint(ConnectionLine connection, bool isSource, Canvas canvas)
        {
            // Return early if the canvas is null
            if (canvas == null) return;

            if (isSource && connection.SourceConnector != null)
            {
                // Update the start point for the source connector
                connection.StartPoint = connection.SourceConnector.TranslatePoint(
                    new Point(connection.SourceConnector.Width / 2, connection.SourceConnector.Height / 2),
                    canvas);
            }
            else if (!isSource && connection.TargetConnector != null)
            {
                // Update the end point for the target connector
                connection.EndPoint = connection.TargetConnector.TranslatePoint(
                    new Point(connection.TargetConnector.Width / 2, connection.TargetConnector.Height / 2),
                    canvas);
            }
        }

        /// <summary>
        /// Finds the parent DraggableModule of a connection point
        /// </summary>
        /// <param name="connector">Connection point to find parent for</param>
        /// <returns>Parent DraggableModule or null if not found</returns>
        private DraggableModule FindParentModule(Ellipse connector)
        {
            DependencyObject current = connector;
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
        /// Cancels the current connection operation and cleans up temporary resources
        /// </summary>
        /// <param name="canvas">Canvas containing the temporary connection</param>
        public void CancelConnection(Canvas canvas)
        {
            if (_temporaryConnection != null && canvas != null)
            {
                canvas.Children.Remove(_temporaryConnection);
            }

            IsConnecting = false;
            _temporaryConnection = null;
            _sourceConnector = null;
            ConnectionStatus = string.Empty;
        }

        /// <summary>
        /// Raises the PropertyChanged event when a property value changes
        /// </summary>
        /// <param name="propertyName">Name of the changed property</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

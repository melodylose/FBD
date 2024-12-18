using FBDApp.Controls;
using FBDApp.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FBDApp.Models
{
    public static class ConnectionExtensions
    {
        public static ConnectionInfo ToConnectionInfo(this ConnectionLine line)
        {
            return new ConnectionInfo
            {
                SourceModuleId = line.SourceModule.Id,
                TargetModuleId = line.TargetModule.Id,
                SourceConnectorName = line.SourceConnector?.Name,
                TargetConnectorName = line.TargetConnector?.Name,
                StartPoint = SerializablePoint.FromWindowsPoint(line.StartPoint),
                EndPoint = SerializablePoint.FromWindowsPoint(line.EndPoint)
            };
        }

        public static ConnectionLine ToConnectionLine(this ConnectionInfo connectionInfo, ObservableCollection<SeceModule> canvasModules)
        {
            LogService.LogInfo($"Attempting to create connection - Source Module ID: {connectionInfo.SourceModuleId}, Target Module ID: {connectionInfo.TargetModuleId}");

            var sourceModule = canvasModules.FirstOrDefault(m => m.Id == connectionInfo.SourceModuleId);
            var targetModule = canvasModules.FirstOrDefault(m => m.Id == connectionInfo.TargetModuleId);

            if (sourceModule == null || targetModule == null)
            {
                LogService.LogWarning($"Source or target module not found - Source: {connectionInfo.SourceModuleId}, Target: {connectionInfo.TargetModuleId}");
                return null;
            }

            // Wait for UI update
            Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
            {
                LogService.LogWarning("Main window not found");
                return null;
            }

            var canvas = mainWindow.GetMainCanvas();
            if (canvas == null)
            {
                LogService.LogWarning("Canvas not found");
                return null;
            }

            var sourceVisual = FindModuleVisual(canvas, sourceModule.Id);
            var targetVisual = FindModuleVisual(canvas, targetModule.Id);

            if (sourceVisual == null || targetVisual == null)
            {
                LogService.LogWarning($"Module visual elements not found");
                return null;
            }

            var connection = new ConnectionLine
            {
                SourceModule = sourceModule,
                TargetModule = targetModule
            };

            // Set connection line position
            connection.UpdateConnectionPoints();

            return connection;
        }

        private static DraggableModule FindModuleVisual(Canvas canvas, string moduleId)
        {
            // Find module's ItemsControl (second ItemsControl, as the first one is for connection lines)
            var moduleItemsControl = canvas.Children.OfType<ItemsControl>()
                .Where(ic => ic.ItemsSource != null)
                .LastOrDefault();

            if (moduleItemsControl == null)
            {
                LogService.LogWarning("Module ItemsControl not found");
                return null;
            }

            // Wait for ItemsControl to update its visual tree
            moduleItemsControl.UpdateLayout();

            // Get ItemsControl panel (Canvas)
            var itemsPanel = VisualTreeHelper.GetChild(moduleItemsControl, 0) as FrameworkElement;
            if (itemsPanel == null)
            {
                LogService.LogWarning("ItemsPanel not found");
                return null;
            }

            // Find module in panel
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

            LogService.LogWarning($"Module not found in visual tree ID: {moduleId}");
            return null;
        }

        private static void UpdateConnectionPoint(ConnectionLine connection, bool isSource, Canvas canvas)
        {
            if (canvas != null)
            {
                if (isSource && connection.SourceConnector != null)
                {
                    connection.StartPoint = connection.SourceConnector.TranslatePoint(
                        new Point(connection.SourceConnector.Width / 2, connection.SourceConnector.Height / 2), 
                        canvas);
                }
                else if (!isSource && connection.TargetConnector != null)
                {
                    connection.EndPoint = connection.TargetConnector.TranslatePoint(
                        new Point(connection.TargetConnector.Width / 2, connection.TargetConnector.Height / 2), 
                        canvas);
                }
            }
        }

        private static DraggableModule FindParentModule(UIElement element)
        {
            DependencyObject current = element;
            while (current != null)
            {
                if (current is DraggableModule module)
                    return module;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}

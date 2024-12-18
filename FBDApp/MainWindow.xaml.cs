using FBDApp.Controls;
using FBDApp.Models;
using FBDApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FBDApp
{
    /// <summary>
    /// MainWindow is the primary window of the application, responsible for handling module drag-and-drop operations
    /// and canvas management. It serves as the main interface for the Flow-Based Design application.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Provides global access to the current MainWindow instance.
        /// This static property enables other components to access the main window when needed.
        /// </summary>
        public static MainWindow Current { get; private set; }

        /// <summary>
        /// Provides quick access to the main canvas where modules are placed and connected.
        /// This property wraps the XAML-defined MainCanvas for easier access from code.
        /// </summary>
        public Canvas ModuleContainer => MainCanvas;

        /// <summary>
        /// The ViewModel for the main window, handling business logic and data management.
        /// Follows the MVVM pattern for separation of concerns.
        /// </summary>
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// Sets up the window's data context and initializes the global Current property.
        /// </summary>
        /// <param name="viewModel">The ViewModel instance that will manage this window's data and operations</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            Current = this;
        }

        /// <summary>
        /// Handles mouse movement events for modules on the canvas.
        /// Initiates drag-and-drop operations when the left mouse button is pressed.
        /// </summary>
        /// <param name="sender">The source of the event (typically a module control)</param>
        /// <param name="e">Contains mouse event data including button states and coordinates</param>
        private void OnModuleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var module = ((FrameworkElement)sender).DataContext as SeceModule;
                if (module != null)
                {
                    DragDrop.DoDragDrop((DependencyObject)sender, module, DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// Handles drag-over events on the canvas.
        /// Sets appropriate drag effects when a module is being dragged over the canvas.
        /// </summary>
        /// <param name="sender">The canvas receiving the drag event</param>
        /// <param name="e">Contains drag event data including the dragged data and effects</param>
        private void OnCanvasDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(SeceModule)))
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles drop events when a module is released on the canvas.
        /// Calculates the drop position and executes the module placement command through the ViewModel.
        /// </summary>
        /// <param name="sender">The canvas where the drop occurred</param>
        /// <param name="e">Contains drop event data including the dropped item and position</param>
        private void OnCanvasDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(SeceModule)))
            {
                var module = e.Data.GetData(typeof(SeceModule)) as SeceModule;
                var position = e.GetPosition((IInputElement)sender);
                _viewModel.ModuleDropCommand.Execute((position.X, position.Y, module));
            }
        }

        /// <summary>
        /// Provides access to the main canvas of the application.
        /// This method is used by other components that need direct access to the canvas.
        /// </summary>
        /// <returns>The main Canvas control where modules are placed and connected</returns>
        public Canvas GetMainCanvas()
        {
            return MainCanvas;
        }
    }
}
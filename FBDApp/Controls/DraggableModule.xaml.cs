using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FBDApp.Models;
using FBDApp.ViewModels;
using FBDApp.Services;

namespace FBDApp.Controls
{
    public partial class DraggableModule : UserControl
    {
        private readonly DraggableModuleViewModel _viewModel;

        /// <summary>
        /// Event raised when the module's location changes
        /// </summary>
        public event EventHandler LocationChanged;
        /// <summary>
        /// Event raised when the module's size changes
        /// </summary>
        public event EventHandler SizeChanged;

        /// <summary>
        /// Gets the name of the module
        /// </summary>
        public string Name => _viewModel.Module?.Name ?? string.Empty;

        // Add back the connector properties
        /// <summary>
        /// Gets the input connector of the module
        /// </summary>
        public Ellipse InputConnector => InputConnection;
        /// <summary>
        /// Gets the output connector of the module
        /// </summary>
        public Ellipse OutputConnector => OutputConnection;

        protected virtual void OnLocationChanged()
        {
            LocationChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        public DraggableModule()
        {
            InitializeComponent();

            // Initialize ViewModel
            var mainViewModel = Application.Current.MainWindow?.DataContext as MainViewModel;
            if (mainViewModel != null)
            {
                _viewModel = new DraggableModuleViewModel(
                    new DraggingService(mainViewModel),
                    mainViewModel
                );
                _viewModel.Module = DataContext as SeceModule;
            }

            // Register events
            this.Loaded += DraggableModule_Loaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            this.PreviewKeyDown += OnInputPreviewKeyDown;

            // Listen for DataContext changes
            this.DataContextChanged += OnDataContextChanged;

            // Add connection-related event handlers
            InputConnection.MouseDown += OnConnectionPointMouseDown;
            OutputConnection.MouseDown += OnConnectionPointMouseDown;
            InputConnection.MouseUp += OnConnectionPointMouseUp;
            OutputConnection.MouseUp += OnConnectionPointMouseUp;
            InputConnection.MouseMove += OnConnectionPointMouseMove;
            OutputConnection.MouseMove += OnConnectionPointMouseMove;
        }

        /// <summary>
        /// Handles changes to the DataContext
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                if (e.OldValue is SeceModule oldModule)
                {
                    oldModule.PropertyChanged -= OnModulePropertyChanged;
                }

                _viewModel.Module = e.NewValue as SeceModule;

                if (_viewModel.Module != null)
                {
                    _viewModel.Module.PropertyChanged += OnModulePropertyChanged;
                }
            }
        }

        /// <summary>
        /// Handles property changes of the module
        /// </summary>
        private void OnModulePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SeceModule.IsSelected))
            {
                _viewModel.Module.IsSelected = ((SeceModule)sender).IsSelected;
            }
        }

        /// <summary>
        /// Handles mouse left button down event for dragging and selection
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is TextBox) && !(e.OriginalSource is ComboBox))
            {
                MainBorder.Focus();
                Keyboard.ClearFocus();
            }

            // Handle drag start
            if (e.Source is FrameworkElement element &&
                !(element.Name == "TopLeftResize" ||
                  element.Name == "TopRightResize" ||
                  element.Name == "BottomLeftResize" ||
                  element.Name == "BottomRightResize"))
            {
                _viewModel.SelectModule();
                var canvas = FindParentCanvas();
                if (canvas != null)
                {
                    _viewModel.StartDragging(e.GetPosition(canvas));
                    this.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles mouse move event for dragging
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel == null) return;

            var canvas = FindParentCanvas();
            if (canvas != null && _viewModel.IsDragging)
            {
                _viewModel.HandleDragging(e.GetPosition(canvas), canvas.ActualWidth, canvas.ActualHeight);
                OnLocationChanged();
            }
        }

        /// <summary>
        /// Handles mouse left button up event to end dragging
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var canvas = FindParentCanvas();
            if (canvas != null && _viewModel.IsDragging)
            {
                _viewModel.EndDragging(e.GetPosition(canvas));
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the module's loaded event
        /// </summary>
        private void DraggableModule_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SeceModule module)
            {
                module.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(SeceModule.IsSelected))
                    {
                        _viewModel.Module.IsSelected = module.IsSelected;
                    }
                };
            }
        }

        /// <summary>
        /// Handles preview key down events for delete operations
        /// </summary>
        private void OnInputPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If Delete key is pressed and no text is selected in the input control
            if (e.Key == Key.Delete)
            {
                if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.SelectedText))
                {
                    // Mark event as handled and let it bubble up to Window
                    e.Handled = false;
                }
                else if (sender is ComboBox)
                {
                    // For ComboBox, always let Delete event bubble up to Window
                    e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Handles mouse down event on resize handles
        /// </summary>
        private void OnResizeHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement resizeHandle)
            {
                var contentPresenter = VisualTreeHelper.GetParent(this) as ContentPresenter;
                if (contentPresenter != null)
                {
                    _viewModel.StartResizing(
                        resizeHandle,
                        e.GetPosition(FindParentCanvas()),
                        Canvas.GetLeft(contentPresenter),
                        Canvas.GetTop(contentPresenter)
                    );
                    resizeHandle.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles mouse up event on resize handles
        /// </summary>
        private void OnResizeHandleMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.IsResizing)
            {
                _viewModel.EndResizing();
                if (sender is FrameworkElement resizeHandle)
                {
                    resizeHandle.ReleaseMouseCapture();
                }
            }
        }

        /// <summary>
        /// Handles the resizing operation
        /// </summary>
        private void HandleResizing(MouseEventArgs e)
        {
            if (_viewModel.IsResizing)
            {
                var canvas = FindParentCanvas();
                if (canvas != null)
                {
                    _viewModel.HandleResizing(e.GetPosition(canvas));
                    OnSizeChanged();
                }
            }
        }

        /// <summary>
        /// Handles mouse down event on connection points to start a connection
        /// </summary>
        private void OnConnectionPointMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && sender is Ellipse sourcePoint)
            {
                var canvas = FindParentCanvas();
                if (canvas != null)
                {
                    var startPoint = sourcePoint.TranslatePoint(
                        new Point(sourcePoint.Width / 2, sourcePoint.Height / 2),
                        canvas);
                    
                    _viewModel.ConnectionViewModel.StartConnection(sourcePoint, startPoint, canvas);
                    sourcePoint.CaptureMouse(); // Capture mouse to ensure we get all mouse events
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles mouse move event during connection creation
        /// </summary>
        private void OnConnectionPointMouseMove(object sender, MouseEventArgs e)
        {
            if (_viewModel.ConnectionViewModel.IsConnecting)
            {
                var canvas = FindParentCanvas();
                if (canvas != null)
                {
                    _viewModel.ConnectionViewModel.UpdateConnection(e.GetPosition(canvas));
                }
            }
        }

        /// <summary>
        /// Handles mouse up event to complete connection creation
        /// </summary>
        private void OnConnectionPointMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.ConnectionViewModel.IsConnecting)
            {
                var canvas = FindParentCanvas();
                if (canvas != null)
                {
                    _viewModel.ConnectionViewModel.HandleConnectionEnd(e.GetPosition(canvas), canvas);
                    if (sender is Ellipse sourcePoint)
                    {
                        sourcePoint.ReleaseMouseCapture();
                    }
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Finds the parent Canvas in the visual tree
        /// </summary>
        /// <returns>The main canvas if found, otherwise the last canvas found in the tree</returns>
        private Canvas FindParentCanvas()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            Canvas mainCanvas = null;

            while (parent != null)
            {
                if (parent is Canvas canvas)
                {
                    // If we find the main canvas (named "MainCanvas"), use it
                    if (canvas.Name == "MainCanvas")
                    {
                        return canvas;
                    }
                    // Otherwise, store this canvas and continue searching up
                    mainCanvas = canvas;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            // If MainCanvas is not found, use the last canvas found
            return mainCanvas;
        }
    }
}

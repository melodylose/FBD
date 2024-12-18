using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using FBDApp.Controls;
using FBDApp.Models;
using FBDApp.Services;

namespace FBDApp.ViewModels
{
    public class DraggableModuleViewModel : INotifyPropertyChanged
    {
        private readonly IDraggingService _draggingService;
        private readonly MainViewModel _mainViewModel;
        private readonly ConnectionViewModel _connectionViewModel;
        private bool _isDragging;
        private bool _isResizing;
        private bool _isSelected;
        private SeceModule _module;
        private FrameworkElement _currentResizeHandle;
        private Point _resizeStartPoint;
        private double _originalWidth;
        private double _originalHeight;
        private double _originalLeft;
        private double _originalTop;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LocationChanged;
        public event EventHandler SizeChanged;

        public ConnectionViewModel ConnectionViewModel => _connectionViewModel;

        public bool IsDragging
        {
            get => _isDragging;
            set
            {
                if (_isDragging != value)
                {
                    _isDragging = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsResizing
        {
            get => _isResizing;
            set
            {
                if (_isResizing != value)
                {
                    _isResizing = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public SeceModule Module
        {
            get => _module;
            set
            {
                if (_module != value)
                {
                    if (_module != null)
                    {
                        _module.PropertyChanged -= ModulePropertyChanged;
                    }
                    _module = value;
                    if (_module != null)
                    {
                        _module.PropertyChanged += ModulePropertyChanged;
                    }
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Constructor for DraggableModuleViewModel
        /// </summary>
        /// <param name="draggingService">Service for handling module dragging operations</param>
        /// <param name="mainViewModel">Main view model reference</param>
        /// <exception cref="ArgumentNullException">Thrown when draggingService or mainViewModel is null</exception>
        public DraggableModuleViewModel(IDraggingService draggingService, MainViewModel mainViewModel)
        {
            _draggingService = draggingService ?? throw new ArgumentNullException(nameof(draggingService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _connectionViewModel = new ConnectionViewModel(mainViewModel);
        }

        /// <summary>
        /// Handles property change events from the associated module
        /// </summary>
        /// <param name="sender">Source of the property change event</param>
        /// <param name="e">Event arguments containing property information</param>
        private void ModulePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SeceModule.IsSelected))
            {
                IsSelected = Module.IsSelected;
            }
        }

        /// <summary>
        /// Initiates the dragging operation for the module
        /// </summary>
        /// <param name="mousePosition">Initial mouse position where dragging started</param>
        public void StartDragging(Point mousePosition)
        {
            if (!IsResizing && Module != null)
            {
                IsDragging = true;
                _draggingService.StartDragging(Module, mousePosition, Module.X, Module.Y);
            }
        }

        /// <summary>
        /// Updates the module position during dragging
        /// </summary>
        /// <param name="mousePosition">Current mouse position</param>
        /// <param name="canvasWidth">Width of the canvas</param>
        /// <param name="canvasHeight">Height of the canvas</param>
        public void HandleDragging(Point mousePosition, double canvasWidth, double canvasHeight)
        {
            if (IsDragging && Module != null)
            {
                _draggingService.HandleDragging(Module, mousePosition, canvasWidth, canvasHeight);
            }
        }

        /// <summary>
        /// Completes the dragging operation and updates the final position
        /// </summary>
        /// <param name="mousePosition">Final mouse position</param>
        public void EndDragging(Point mousePosition)
        {
            if (IsDragging && Module != null)
            {
                _draggingService.EndDragging(Module, mousePosition);
                IsDragging = false;
                OnLocationChanged();
            }
        }

        /// <summary>
        /// Initiates the resizing operation for the module
        /// </summary>
        /// <param name="resizeHandle">Handle being used for resizing</param>
        /// <param name="startPoint">Initial point where resizing started</param>
        /// <param name="currentLeft">Current left position of the module</param>
        /// <param name="currentTop">Current top position of the module</param>
        public void StartResizing(FrameworkElement resizeHandle, Point startPoint, double currentLeft, double currentTop)
        {
            if (Module != null)
            {
                IsResizing = true;
                _currentResizeHandle = resizeHandle;
                _resizeStartPoint = startPoint;
                _originalWidth = Module.Width;
                _originalHeight = Module.Height;
                _originalLeft = currentLeft;
                _originalTop = currentTop;
            }
        }

        /// <summary>
        /// Updates the module size and position during resizing
        /// </summary>
        /// <param name="currentPoint">Current mouse position during resize</param>
        public void HandleResizing(Point currentPoint)
        {
            if (!IsResizing || Module == null || _currentResizeHandle == null) return;

            var diff = currentPoint - _resizeStartPoint;
            double newWidth = _originalWidth;
            double newHeight = _originalHeight;
            double newX = _originalLeft;
            double newY = _originalTop;

            switch (_currentResizeHandle.Name)
            {
                case "TopLeftResize":
                    newWidth = Math.Max(100, _originalWidth - diff.X);
                    newHeight = Math.Max(50, _originalHeight - diff.Y);
                    newX = _originalLeft + (_originalWidth - newWidth);
                    newY = _originalTop + (_originalHeight - newHeight);
                    break;
                case "TopRightResize":
                    newWidth = Math.Max(100, _originalWidth + diff.X);
                    newHeight = Math.Max(50, _originalHeight - diff.Y);
                    newY = _originalTop + (_originalHeight - newHeight);
                    break;
                case "BottomLeftResize":
                    newWidth = Math.Max(100, _originalWidth - diff.X);
                    newHeight = Math.Max(50, _originalHeight + diff.Y);
                    newX = _originalLeft + (_originalWidth - newWidth);
                    break;
                case "BottomRightResize":
                    newWidth = Math.Max(100, _originalWidth + diff.X);
                    newHeight = Math.Max(50, _originalHeight + diff.Y);
                    break;
            }

            // Update module size and position
            _mainViewModel?.UpdateModulePositionCommand.Execute((Module, newX, newY));
            _mainViewModel?.UpdateModuleSizeCommand.Execute((Module, newWidth, newHeight));
            OnSizeChanged();
        }

        /// <summary>
        /// Completes the resizing operation and cleans up temporary resources
        /// </summary>
        public void EndResizing()
        {
            IsResizing = false;
            _currentResizeHandle = null;
        }

        /// <summary>
        /// Selects the current module in the main view model
        /// </summary>
        public void SelectModule()
        {
            _mainViewModel.SelectModuleCommand.Execute(Module);
        }

        /// <summary>
        /// Updates the positions of all connection lines connected to this module
        /// </summary>
        /// <param name="canvas">Canvas containing the connection lines</param>
        public void UpdateConnections(Canvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is ConnectionLine connection)
                {
                    // Update source connector
                    if (connection.SourceConnector != null)
                    {
                        connection.StartPoint = connection.SourceConnector.TranslatePoint(
                            new Point(connection.SourceConnector.Width / 2,
                                    connection.SourceConnector.Height / 2),
                            canvas);
                    }

                    // Update target connector
                    if (connection.TargetConnector != null)
                    {
                        connection.EndPoint = connection.TargetConnector.TranslatePoint(
                            new Point(connection.TargetConnector.Width / 2,
                                    connection.TargetConnector.Height / 2),
                            canvas);
                    }
                }
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the property that changed</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the LocationChanged event
        /// </summary>
        protected virtual void OnLocationChanged()
        {
            LocationChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the SizeChanged event
        /// </summary>
        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

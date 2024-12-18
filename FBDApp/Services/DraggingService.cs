using System;
using System.Windows;
using System.Windows.Controls;
using FBDApp.Models;
using FBDApp.ViewModels;

namespace FBDApp.Services
{
    /// <summary>
    /// Service responsible for managing module dragging operations within the canvas.
    /// Handles drag start, movement, and end operations while ensuring modules stay within canvas bounds.
    /// </summary>
    public class DraggingService : IDraggingService
    {
        private readonly MainViewModel _mainViewModel;
        private Point _dragOffset;

        /// <summary>
        /// Gets the current drag offset from the mouse position to the module's origin
        /// </summary>
        public Point DragOffset => _dragOffset;

        /// <summary>
        /// Initializes a new instance of the DraggingService
        /// </summary>
        /// <param name="mainViewModel">The main view model instance for updating module positions</param>
        /// <exception cref="ArgumentNullException">Thrown when mainViewModel is null</exception>
        public DraggingService(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        /// <summary>
        /// Initiates the dragging operation for a module
        /// </summary>
        /// <param name="module">The module being dragged</param>
        /// <param name="mousePosition">Current mouse position</param>
        /// <param name="originalLeft">Original X coordinate of the module</param>
        /// <param name="originalTop">Original Y coordinate of the module</param>
        public void StartDragging(SeceModule module, Point mousePosition, double originalLeft, double originalTop)
        {
            _dragOffset = new Point(mousePosition.X - originalLeft, mousePosition.Y - originalTop);
        }

        /// <summary>
        /// Handles the continuous dragging operation of a module
        /// Updates the module's position while ensuring it stays within canvas bounds
        /// </summary>
        /// <param name="module">The module being dragged</param>
        /// <param name="mousePosition">Current mouse position</param>
        /// <param name="canvasWidth">Width of the canvas</param>
        /// <param name="canvasHeight">Height of the canvas</param>
        public void HandleDragging(SeceModule module, Point mousePosition, double canvasWidth, double canvasHeight)
        {
            // Calculate new position based on mouse position and drag offset
            double newX = mousePosition.X - _dragOffset.X;
            double newY = mousePosition.Y - _dragOffset.Y;

            // Constrain position within canvas bounds
            newX = Math.Max(0, Math.Min(newX, canvasWidth - module.Width));
            newY = Math.Max(0, Math.Min(newY, canvasHeight - module.Height));

            // Update module position
            module.X = newX;
            module.Y = newY;
        }

        /// <summary>
        /// Completes the dragging operation and updates the final position of the module
        /// </summary>
        /// <param name="module">The module being dragged</param>
        /// <param name="mousePosition">Final mouse position</param>
        public void EndDragging(SeceModule module, Point mousePosition)
        {
            // Update final position through the view model command
            _mainViewModel.UpdateModulePositionCommand.Execute((module, module.X, module.Y));
        }
    }
}

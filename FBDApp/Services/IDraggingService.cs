using System.Windows;
using System.Windows.Controls;
using FBDApp.Models;

namespace FBDApp.Services
{
    public interface IDraggingService
    {
        Point DragOffset { get; }
        void StartDragging(SeceModule module, Point mousePosition, double originalLeft, double originalTop);
        void HandleDragging(SeceModule module, Point mousePosition, double canvasWidth, double canvasHeight);
        void EndDragging(SeceModule module, Point mousePosition);
    }
}

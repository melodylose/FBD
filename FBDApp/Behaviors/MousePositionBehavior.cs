using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using System.Diagnostics;

namespace FBDApp.Behaviors
{
    public class MousePositionBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty UpdatePositionCommandProperty =
            DependencyProperty.Register(
                nameof(UpdatePositionCommand),
                typeof(ICommand),
                typeof(MousePositionBehavior),
                new PropertyMetadata(null, OnUpdatePositionCommandChanged));

        private static void OnUpdatePositionCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MousePositionBehavior)d;
            Debug.WriteLine($"UpdatePositionCommand changed: {e.NewValue != null}");
        }

        public ICommand UpdatePositionCommand
        {
            get => (ICommand)GetValue(UpdatePositionCommandProperty);
            set => SetValue(UpdatePositionCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Debug.WriteLine($"MousePositionBehavior attached to {AssociatedObject.GetType().Name}");
            AssociatedObject.MouseMove += OnMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove -= OnMouseMove;
            Debug.WriteLine("MousePositionBehavior detached");
            base.OnDetaching();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (UpdatePositionCommand != null)
            {
                var position = e.GetPosition(AssociatedObject);
                
                if (UpdatePositionCommand.CanExecute(position))
                {
                    UpdatePositionCommand.Execute(position);
                }
                else
                {
                    Debug.WriteLine("UpdatePositionCommand cannot execute");
                }
            }
            else
            {
                Debug.WriteLine("UpdatePositionCommand is null");
            }
        }
    }
}

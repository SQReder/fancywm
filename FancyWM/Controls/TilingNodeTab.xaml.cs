using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using FancyWM.Utilities;

namespace FancyWM.Controls
{
    /// <summary>
    /// Interaction logic for TilingNodeTab.xaml
    /// </summary>
    public partial class TilingNodeTab : UserControl
    {
        private Point? m_pressPosition;
        private bool m_isDragging;

        public TilingNodeTab()
        {
            InitializeComponent();
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            // Not handled, so that a plain click still focuses the node.
            m_pressPosition = e.GetPosition(this);
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            if (m_isDragging)
            {
                RaiseEvent(new RoutedEventArgs(Draggable.DraggingEvent, this));
                return;
            }

            if (m_pressPosition is not Point pressPosition || e.LeftButton != MouseButtonState.Pressed)
            {
                m_pressPosition = null;
                return;
            }

            // Only start dragging past the system threshold, so that clicks stay clicks.
            var offset = e.GetPosition(this) - pressPosition;
            if (Math.Abs(offset.X) < System.Windows.SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(offset.Y) < System.Windows.SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            // Takes the capture over from the tab button. Its LostMouseCapture bubbles
            // through here while m_isDragging is still false, and is ignored.
            if (CaptureMouse())
            {
                m_isDragging = true;
                Cursor = Cursors.SizeAll;
                RaiseEvent(new RoutedEventArgs(Draggable.DragStartedEvent, this));
            }
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            m_pressPosition = null;
            if (m_isDragging)
            {
                // Completes the drag through OnLostMouseCapture.
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (m_isDragging)
            {
                m_isDragging = false;
                ClearValue(CursorProperty);
                RaiseEvent(new RoutedEventArgs(Draggable.DragCompletedEvent, this));
            }
        }
    }
}

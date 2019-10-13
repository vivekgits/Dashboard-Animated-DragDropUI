using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace DragDropAnimated.Controls.Utilities
{
	/// <summary>
	/// Provides access to the mouse location by calling unmanaged code.
	/// </summary>
	/// <remarks>
	public class MouseUtilities
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		};

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(ref Win32Point pt);

		[DllImport("user32.dll")]
		private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point pt);

		/// <summary>
		/// Returns the mouse cursor location.  This method is necessary during 
		/// a drag-drop operation because the WPF mechanisms for retrieving the
		/// cursor coordinates are unreliable.
		/// </summary>
		/// <param name="relativeTo">The Visual to which the mouse coordinates will be relative.</param>
		public static Point GetMousePosition(Visual relativeTo)
		{
			Win32Point mouse = new Win32Point();
			GetCursorPos(ref mouse);
			if (relativeTo != null)
			{
				return relativeTo.PointFromScreen(new Point((double)mouse.X, (double)mouse.Y));
			}
			else return new Point(0,0);
			#region Commented Out
			//System.Windows.Interop.HwndSource presentationSource =
			//	(System.Windows.Interop.HwndSource)PresentationSource.FromVisual(relativeTo);
			//ScreenToClient(presentationSource.Handle, ref mouse);
			//GeneralTransform transform = relativeTo.TransformToAncestor(presentationSource.RootVisual);
			//Point offset = transform.Transform(new Point(0, 0));
			//return new Point(mouse.X - offset.X, mouse.Y - offset.Y);
			#endregion // Commented Out
		}

		public static bool InDistance(Point DraggingPos, Point SwapPos, double distance)
		{
			double diffX = Math.Abs(DraggingPos.X - SwapPos.X);
			double diffY = Math.Abs(DraggingPos.Y - SwapPos.Y);
			return diffX <= distance && diffY <= distance;
		}
	}
}

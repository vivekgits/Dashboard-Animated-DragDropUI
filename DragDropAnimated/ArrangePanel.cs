using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WAM3._1POC.Controls.Utilities;
using WAM3._1POC.DragDropManager.Adorners;

namespace WAM3._1POC
{
    public class ArrangePanel : WrapPanel
	{
        private UIElement _draggingObject;
        private Vector _delta;
        private Point _startPosition;
        private readonly ILayoutStrategy _strategy = new TableLayoutStrategy();
		private ListView SourceListView;
		//class wamProcess { };
		wamProcess itemUnderDragCursor;
		bool showDragAdorner;
		bool isDragInProgress;
		bool canInitiateDrag;
		Point ptMouseDown;
		int indexToSelect;
		double dragAdornerOpacity;
		DragAdorner dragAdorner;

		#region IsMouseOverScrollbar

		/// <summary>
		/// Returns true if the mouse cursor is over a scrollbar in the SourceListView.
		/// </summary>
		bool IsMouseOverScrollbar
		{
			get
			{
				Point ptMouse = MouseUtilities.GetMousePosition(this.SourceListView);
				HitTestResult res = VisualTreeHelper.HitTest(this.SourceListView, ptMouse);
				if (res == null)
					return false;

				DependencyObject depObj = res.VisualHit;
				while (depObj != null)
				{
					if (depObj is ScrollBar)
						return true;

					// VisualTreeHelper works with objects of type Visual or Visual3D.
					// If the current object is not derived from Visual or Visual3D,
					// then use the LogicalTreeHelper to find the parent element.
					if (depObj is Visual || depObj is System.Windows.Media.Media3D.Visual3D)
						depObj = VisualTreeHelper.GetParent(depObj);
					else
						depObj = LogicalTreeHelper.GetParent(depObj);
				}

				return false;
			}
		}

		#endregion // IsMouseOverScrollbar

		#region ItemUnderDragCursor

		wamProcess ItemUnderDragCursor
		{
			get { return this.itemUnderDragCursor; }
			set
			{
				if (this.itemUnderDragCursor == value)
					return;

				// The first pass handles the previous item under the cursor.
				// The second pass handles the new one.
				for (int i = 0; i < 2; ++i)
				{
					if (i == 1)
						this.itemUnderDragCursor = value;

					if (this.itemUnderDragCursor != null)
					{
						ListViewItem listViewItem = this.GetListViewItem(this.itemUnderDragCursor);
						if (listViewItem != null)
							ListViewItemDragState.SetIsUnderDragCursor(listViewItem, i == 1);
					}
				}
			}
		}

		#endregion // ItemUnderDragCursor

		#region GetListViewItem

		ListViewItem GetListViewItem(int index)
		{
			if (this.SourceListView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
				return null;

			return this.SourceListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
		}

		ListViewItem GetListViewItem(wamProcess dataItem)
		{
			if (this.SourceListView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
				return null;

			return this.SourceListView.ItemContainerGenerator.ContainerFromItem(dataItem) as ListViewItem;
		}

		#endregion // GetListViewItem

		#region IsDragInProgress

		/// <summary>
		/// Returns true if there is currently a drag operation being managed.
		/// </summary>
		public bool IsDragInProgress
		{
			get { return this.isDragInProgress; }
			private set { this.isDragInProgress = value; }
		}

		#endregion // IsDragInProgress

		#region ShowDragAdorner

		/// <summary>
		/// Gets/sets whether a visual representation of the ListViewItem being dragged
		/// follows the mouse cursor during a drag operation.  The default value is true.
		/// </summary>
		public bool ShowDragAdorner
		{
			get { return this.showDragAdorner; }
			set
			{
				if (this.IsDragInProgress)
					throw new InvalidOperationException("Cannot set the ShowDragAdorner property during a drag operation.");

				this.showDragAdorner = value;
			}
		}

		#endregion // ShowDragAdorner

		#region ListViewItemDragState

		/// <summary>
		/// Exposes attached properties used in conjunction with the ListViewDragDropManager class.
		/// Those properties can be used to allow triggers to modify the appearance of ListViewItems
		/// in a ListView during a drag-drop operation.
		/// </summary>
		public static class ListViewItemDragState
		{
			#region IsBeingDragged

			/// <summary>
			/// Identifies the ListViewItemDragState's IsBeingDragged attached property.  
			/// This field is read-only.
			/// </summary>
			public static readonly DependencyProperty IsBeingDraggedProperty =
				DependencyProperty.RegisterAttached(
					"IsBeingDragged",
					typeof(bool),
					typeof(ListViewItemDragState),
					new UIPropertyMetadata(false));

			/// <summary>
			/// Returns true if the specified ListViewItem is being dragged, else false.
			/// </summary>
			/// <param name="item">The ListViewItem to check.</param>
			public static bool GetIsBeingDragged(ListViewItem item)
			{
				return (bool)item.GetValue(IsBeingDraggedProperty);
			}

			/// <summary>
			/// Sets the IsBeingDragged attached property for the specified ListViewItem.
			/// </summary>
			/// <param name="item">The ListViewItem to set the property on.</param>
			/// <param name="value">Pass true if the element is being dragged, else false.</param>
			internal static void SetIsBeingDragged(ListViewItem item, bool value)
			{
				item.SetValue(IsBeingDraggedProperty, value);
			}

			#endregion // IsBeingDragged

			#region IsUnderDragCursor

			/// <summary>
			/// Identifies the ListViewItemDragState's IsUnderDragCursor attached property.  
			/// This field is read-only.
			/// </summary>
			public static readonly DependencyProperty IsUnderDragCursorProperty =
				DependencyProperty.RegisterAttached(
					"IsUnderDragCursor",
					typeof(bool),
					typeof(ListViewItemDragState),
					new UIPropertyMetadata(false));

			/// <summary>
			/// Returns true if the specified ListViewItem is currently underneath the cursor 
			/// during a drag-drop operation, else false.
			/// </summary>
			/// <param name="item">The ListViewItem to check.</param>
			public static bool GetIsUnderDragCursor(ListViewItem item)
			{
				return (bool)item.GetValue(IsUnderDragCursorProperty);
			}

			/// <summary>
			/// Sets the IsUnderDragCursor attached property for the specified ListViewItem.
			/// </summary>
			/// <param name="item">The ListViewItem to set the property on.</param>
			/// <param name="value">Pass true if the element is underneath the drag cursor, else false.</param>
			internal static void SetIsUnderDragCursor(ListViewItem item, bool value)
			{
				item.SetValue(IsUnderDragCursorProperty, value);
			}

			#endregion // IsUnderDragCursor
		}

		#endregion

		#region PerformDragOperation

		void PerformDragOperation()
		{
			wamProcess selectedItem = this.SourceListView.SelectedItem as wamProcess;
			DragDropEffects allowedEffects = DragDropEffects.None | DragDropEffects.Move | DragDropEffects.Link;
			if (DragDrop.DoDragDrop(this.SourceListView, selectedItem, allowedEffects) != DragDropEffects.None)
			{
				// The item was dropped into a new location,
				// so make it the new selected item.
				this.SourceListView.SelectedItem = selectedItem;
			}
		}

		#endregion // PerformDragOperation

		#region ShowDragAdornerResolved

		bool ShowDragAdornerResolved
		{
			get { return this.ShowDragAdorner && this.DragAdornerOpacity > 0.0; }
		}

		#endregion // ShowDragAdornerResolved

		#region IsMouseOver

		bool IsMouseOver(Visual target)
		{
			// We need to use MouseUtilities to figure out the cursor
			// coordinates because, during a drag-drop operation, the WPF
			// mechanisms for getting the coordinates behave strangely.

			Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
			Point mousePos = MouseUtilities.GetMousePosition(target);
			return bounds.Contains(mousePos);
		}

		#endregion // IsMouseOver

		#region IndexUnderDragCursor

		/// <summary>
		/// Returns the index of the ListViewItem underneath the
		/// drag cursor, or -1 if the cursor is not over an item.
		/// </summary>
		int IndexUnderDragCursor
		{
			get
			{
				int index = -1;
				for (int i = 0; i < this.SourceListView.Items.Count; ++i)
				{
					ListViewItem item = this.GetListViewItem(i);
					if (this.IsMouseOver(item))
					{
						index = i;
						break;
					}
				}
				return index;
			}
		}

		#endregion // IndexUnderDragCursor

		#region InitializeDragOperation

		void InitializeDragOperation(ListViewItem itemToDrag)
		{
			// Set some flags used during the drag operation.
			this.IsDragInProgress = true;
			this.canInitiateDrag = false;

			// Let the ListViewItem know that it is being dragged.
			ListViewItemDragState.SetIsBeingDragged(itemToDrag, true);
		}

		#endregion // InitializeDragOperation

		#region FinishDragOperation

		void FinishDragOperation(ListViewItem draggedItem, AdornerLayer adornerLayer)
		{
			// Let the ListViewItem know that it is not being dragged anymore.
			ListViewItemDragState.SetIsBeingDragged(draggedItem, false);

			this.IsDragInProgress = false;

			if (this.ItemUnderDragCursor != null)
				this.ItemUnderDragCursor = null;

			// Remove the drag adorner from the adorner layer.
			if (adornerLayer != null)
			{
				adornerLayer.Remove(this.dragAdorner);
				this.dragAdorner = null;
			}
		}

		#endregion // FinishDragOperation

		#region HasCursorLeftDragThreshold

		bool HasCursorLeftDragThreshold
		{
			get
			{
				if (this.indexToSelect < 0)
					return false;

				ListViewItem item = this.GetListViewItem(this.indexToSelect);
				Rect bounds = VisualTreeHelper.GetDescendantBounds(item);
				Point ptInItem = this.SourceListView.TranslatePoint(this.ptMouseDown, item);

				// In case the cursor is at the very top or bottom of the ListViewItem
				// we want to make the vertical threshold very small so that dragging
				// over an adjacent item does not select it.
				double topOffset = Math.Abs(ptInItem.Y);
				double btmOffset = Math.Abs(bounds.Height - ptInItem.Y);
				double vertOffset = Math.Min(topOffset, btmOffset);

				double width = SystemParameters.MinimumHorizontalDragDistance * 2;
				double height = Math.Min(SystemParameters.MinimumVerticalDragDistance, vertOffset) * 2;
				Size szThreshold = new Size(width, height);

				Rect rect = new Rect(this.ptMouseDown, szThreshold);
				rect.Offset(szThreshold.Width / -2, szThreshold.Height / -2);
				Point ptInListView = MouseUtilities.GetMousePosition(this.SourceListView);
				return !rect.Contains(ptInListView);
			}
		}

		#endregion // HasCursorLeftDragThreshold

		#region CanStartDragOperation

		bool CanStartDragOperation
		{
			get
			{
				if (Mouse.LeftButton != MouseButtonState.Pressed)
					return false;

				if (!this.canInitiateDrag)
					return false;

				if (this.indexToSelect == -1)
					return false;

				if (!this.HasCursorLeftDragThreshold)
					return false;

				return true;
			}
		}

		#endregion // CanStartDragOperation

		#region DragAdornerOpacity

		/// <summary>
		/// Gets/sets the opacity of the drag adorner.  This property has no
		/// effect if ShowDragAdorner is false. The default value is 0.7
		/// </summary>
		public double DragAdornerOpacity
		{
			get { return this.dragAdornerOpacity; }
			set
			{
				if (this.IsDragInProgress)
					throw new InvalidOperationException("Cannot set the DragAdornerOpacity property during a drag operation.");

				if (value < 0.0 || value > 1.0)
					throw new ArgumentOutOfRangeException("DragAdornerOpacity", value, "Must be between 0 and 1.");

				this.dragAdornerOpacity = value;
			}
		}

		#endregion // DragAdornerOpacity

		#region InitializeAdornerLayer

		AdornerLayer InitializeAdornerLayer(ListViewItem itemToDrag)
		{
			// Create a brush which will paint the ListViewItem onto
			// a visual in the adorner layer.
			VisualBrush brush = new VisualBrush(itemToDrag);

			// Create an element which displays the source item while it is dragged.
			this.dragAdorner = new DragAdorner(this.SourceListView, itemToDrag.RenderSize, brush);

			// Set the drag adorner's opacity.		
			this.dragAdorner.Opacity = this.DragAdornerOpacity;

			AdornerLayer layer = AdornerLayer.GetAdornerLayer(this.SourceListView);
			layer.Add(dragAdorner);
			//layer.Cursor = new Cursor(@"D:\CodeSamples\WAM3.1POC\WAM3.1POC\cursor-move.cur");
			// Save the location of the cursor when the left mouse button was pressed.
			this.ptMouseDown = MouseUtilities.GetMousePosition(this.SourceListView);

			return layer;
		}

		#endregion // InitializeAdornerLayer

		#region UpdateDragAdornerLocation

		void UpdateDragAdornerLocation()
		{
			if (this.dragAdorner != null)
			{
				Point ptCursor = MouseUtilities.GetMousePosition(this.SourceListView);

				double left = ptCursor.X - this.ptMouseDown.X;

				// Made the top offset relative to the item being dragged.
				ListViewItem itemBeingDragged = this.GetListViewItem(this.indexToSelect);
				Point itemLoc = itemBeingDragged.TranslatePoint(new Point(0, 0), this.SourceListView);
				double top = itemLoc.Y + ptCursor.Y - this.ptMouseDown.Y;
				left = itemLoc.X + ptCursor.X - this.ptMouseDown.X;
				this.dragAdorner.SetOffsets(left, top);
			}
		}

		#endregion // UpdateDragAdornerLocation

		public ArrangePanel()
		{
			this.canInitiateDrag = false;
			this.dragAdornerOpacity = 0.7;
			this.indexToSelect = -1;
			this.showDragAdorner = true;

			this.Loaded += panel_Loaded;
		}

		void panel_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			var allChild = Children.OfType<UIElement>();
			if (allChild.Count() > 0)
			{
				SourceListView = GetSourceListView(allChild.First());
				//Ite = typeof(wamProcess);
				if (SourceListView != null)
				{
					SourceListView.PreviewMouseLeftButtonDown -= this.listView_PreviewMouseLeftButtonDown;
					//SourceListView.PreviewMouseLeftButtonUp -= this.listView_PreviewMouseLeftButtonUp;
					SourceListView.PreviewMouseMove -= this.listView_PreviewMouseMove;
					SourceListView.DragEnter -= this.listView_DragEnter;
					SourceListView.DragOver -= this.listView_DragOver;
					SourceListView.DragLeave -= this.listView_DragLeave;
					SourceListView.Drop -= this.listView_Drop;
					//SourceListView.MouseLeave -= this.listView_OnMouseLeave;
					SourceListView.PreviewMouseLeftButtonDown += this.listView_PreviewMouseLeftButtonDown;
					//SourceListView.MouseLeftButtonUp += this.listView_PreviewMouseLeftButtonUp;
					SourceListView.MouseMove += this.listView_PreviewMouseMove;
					SourceListView.DragEnter += this.listView_DragEnter;
					SourceListView.DragOver += this.listView_DragOver;
					SourceListView.DragLeave += this.listView_DragLeave;
					SourceListView.Drop += this.listView_Drop;
					//SourceListView.MouseLeave += this.listView_OnMouseLeave;
				}
			}
		}

		void listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (this.IsMouseOverScrollbar)
			{
				// Set the flag to false when cursor is over scrollbar.
				this.canInitiateDrag = false;
				return;
			}

			int index = this.IndexUnderDragCursor;
			this.canInitiateDrag = index > -1;

			if (this.canInitiateDrag)
			{
				// Remember the location and index of the ListViewItem the user clicked on for later.
				this.ptMouseDown = MouseUtilities.GetMousePosition(this.SourceListView);
				this.indexToSelect = index;
			}
			else
			{
				this.ptMouseDown = new Point(-10000, -10000);
				this.indexToSelect = -1;
			}
		}

		protected void listView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			StopReordering();
		}

		protected void listView_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (!this.CanStartDragOperation)
				return;

			// Select the item the user clicked on.
			if (this.SourceListView.SelectedIndex != this.indexToSelect)
				this.SourceListView.SelectedIndex = this.indexToSelect;

			// If the item at the selected index is null, there's nothing
			// we can do, so just return;
			if (this.SourceListView.SelectedItem == null)
				return;

			ListViewItem itemToDrag = this.GetListViewItem(this.SourceListView.SelectedIndex);
			if (itemToDrag == null)
				return;

			AdornerLayer adornerLayer = this.ShowDragAdornerResolved ? this.InitializeAdornerLayer(itemToDrag) : null;

			this.InitializeDragOperation(itemToDrag);
			this.PerformDragOperation();
			this.FinishDragOperation(itemToDrag, adornerLayer);
		}

		#region listView_DragOver

		void listView_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.Move;

			if (this.ShowDragAdornerResolved)
				this.UpdateDragAdornerLocation();

			// Update the item which is known to be currently under the drag cursor.
			int index = this.IndexUnderDragCursor;
			this.ItemUnderDragCursor = index < 0 ? null : this.SourceListView.Items[index] as wamProcess;
		}

		#endregion // listView_DragOver

		#region listView_DragLeave

		void listView_DragLeave(object sender, DragEventArgs e)
		{
			if (!this.IsMouseOver(this.SourceListView))
			{
				if (this.ItemUnderDragCursor != null)
					this.ItemUnderDragCursor = null;

				if (this.dragAdorner != null)
					this.dragAdorner.Visibility = Visibility.Collapsed;
			}
		}

		#endregion // listView_DragLeave

		#region listView_DragEnter

		void listView_DragEnter(object sender, DragEventArgs e)
		{
			if (this.dragAdorner != null && this.dragAdorner.Visibility != Visibility.Visible)
			{
				// Update the location of the adorner and then show it.				
				this.UpdateDragAdornerLocation();
				this.dragAdorner.Visibility = Visibility.Visible;
			}
		}

		#endregion // listView_DragEnter

		#region listView_Drop

		void listView_Drop(object sender, DragEventArgs e)
		{
			if (this.ItemUnderDragCursor != null)
				this.ItemUnderDragCursor = null;

			e.Effects = DragDropEffects.None;

			if (!e.Data.GetDataPresent(typeof(wamProcess)))
				return;

			// Get the data object which was dropped.
			wamProcess data = e.Data.GetData(typeof(wamProcess)) as wamProcess;
			if (data == null)
				return;

			// Get the ObservableCollection<ItemType> which contains the dropped data object.
			ObservableCollection<wamProcess> itemsSource = this.SourceListView.ItemsSource as ObservableCollection<wamProcess>;
			if (itemsSource == null)
				throw new Exception(
					"A ListView managed by ListViewDragManager must have its ItemsSource set to an ObservableCollection<ItemType>.");

			int oldIndex = itemsSource.IndexOf(data);
			int newIndex = this.IndexUnderDragCursor;

			if (newIndex < 0)
			{
				// The drag started somewhere else, and our ListView is empty
				// so make the new item the first in the list.
				if (itemsSource.Count == 0)
					newIndex = 0;

				// The drag started somewhere else, but our ListView has items
				// so make the new item the last in the list.
				else if (oldIndex < 0)
					newIndex = itemsSource.Count;

				// The user is trying to drop an item from our ListView into
				// our ListView, but the mouse is not over an item, so don't
				// let them drop it.
				else
					return;
			}

			// Dropping an item back onto itself is not considered an actual 'drop'.
			if (oldIndex == newIndex)
				return;

			//if (this.ProcessDrop != null)
			//{
			//	// Let the client code process the drop.
			//	ProcessDropEventArgs<ItemType> args = new ProcessDropEventArgs<ItemType>(itemsSource, data, oldIndex, newIndex, e.AllowedEffects);
			//	this.ProcessDrop(this, args);
			//	e.Effects = args.Effects;
			//}
			//else
			//{
				// Move the dragged data object from it's original index to the
				// new index (according to where the mouse cursor is).  If it was
				// not previously in the ListBox, then insert the item.
				if (oldIndex > -1)
					itemsSource.Move(oldIndex, newIndex);
				else
					itemsSource.Insert(newIndex, data);

				// Set the Effects property so that the call to DoDragDrop will return 'Move'.
				e.Effects = DragDropEffects.Move;
			//}
		}

		#endregion // listView_Drop


		protected void listView_OnMouseLeave(object sender, MouseEventArgs e)
		{
			StopReordering();
			base.OnMouseLeave(e);
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //StartReordering(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            //StopReordering();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //if (_draggingObject != null)
            //{
            //    if (e.LeftButton == MouseButtonState.Released)
            //        StopReordering();
            //    else
            //        DoReordering(e);
            //}
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //StopReordering();
            //base.OnMouseLeave(e);
        }

        private void StartReordering(MouseEventArgs e)
        {
            _startPosition = e.GetPosition(this);
            _draggingObject = GetMyChildOfUiElement((UIElement) e.OriginalSource);
            _draggingObject.SetValue(ZIndexProperty, 100);
            var position = GetPosition(_draggingObject);
            _delta = position.TopLeft - _startPosition;
            _draggingObject.BeginAnimation(PositionProperty, null);
            SetPosition(_draggingObject, position);
        }

        private void DoReordering(MouseEventArgs e)
        {
            e.Handled = true;
            Point mousePosition = e.GetPosition(this);
            var index = _strategy.GetIndex(mousePosition);
            SetOrder(_draggingObject, index);
            var topLeft = mousePosition + _delta;
            var newPosition = new Rect(topLeft, GetPosition(_draggingObject).Size);
            SetPosition(_draggingObject, newPosition);
        }

        private void StopReordering()
        {
            if (_draggingObject == null) return;

            _draggingObject.ClearValue(ZIndexProperty);
            InvalidateMeasure();
            AnimateToPosition(_draggingObject, GetDesiredPosition(_draggingObject));
            _draggingObject = null;
        }

        private UIElement GetMyChildOfUiElement(UIElement e)
        {
            var obj = e;
            var parent = (UIElement)VisualTreeHelper.GetParent(obj);
            while (parent != null && parent != this)
            {
                obj = parent;
                parent = (UIElement)VisualTreeHelper.GetParent(obj);
            }
            return obj;
        }

        //protected override Size MeasureOverride(Size availableSize)
        //{
        //    InitializeEmptyOrder();
        //    if (_draggingObject != null)
        //    {
        //        ReorderOthers();
        //    }

        //    var measures = MeasureChildren();

        //    _strategy.Calculate(availableSize, measures);

        //    var index = -1;
        //    foreach (var child in Children.OfType<UIElement>().OrderBy(GetOrder))
        //    {
        //        index++;
        //        if (child == _draggingObject) continue;
        //        SetDesiredPosition(child, _strategy.GetPosition(index));
        //    }

        //    return _strategy.ResultSize;
        //}

        //protected override Size ArrangeOverride(Size finalSize)
        //{
        //    foreach (var child in Children.OfType<UIElement>().OrderBy(GetOrder))
        //    {
        //        var position = GetPosition(child);
        //        if (double.IsNaN(position.Top))
        //            position = GetDesiredPosition(child);
        //        child.Arrange(position);
        //    }
        //    return _strategy.ResultSize;
        //}


        private Size[] MeasureChildren()
        {
            if (_measures == null || Children.Count != _measures.Length)
            {
                _measures = new Size[Children.Count];

                var infinitSize = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

                foreach (UIElement child in Children)
                {
                    child.Measure(infinitSize);
                }


                var i = 0;
                foreach (var measure in Children.OfType<UIElement>().OrderBy(GetOrder).Select(ch => ch.DesiredSize))
                {
                    _measures[i] = measure;
                    i++;
                }
            }
            return _measures;
        }

        private void ReorderOthers()
        {
            var s = GetOrder(_draggingObject);
            var i = 0;
            foreach (var child in Children.OfType<UIElement>().OrderBy(GetOrder))
            {
                if (i == s) i++;
                if (child == _draggingObject) continue;
                var current = GetOrder(child);
                if (i != current)
                {
                    SetOrder(child, i);
                }
                i++;
            }
        }

        private void InitializeEmptyOrder()
        {
			var allChild = Children.OfType<UIElement>();
			if (allChild.Count()>0)
			{
				var next = allChild.Max(ch => GetOrder(ch)) + 1;
				foreach (var child in allChild.Where(child => GetOrder(child) == -1))
				{
					SetOrder(child, next);
					next++;
				}
			}
        }

		public static readonly DependencyProperty SourceListViewProperty;
		public static readonly DependencyProperty OrderProperty;
        public static readonly DependencyProperty PositionProperty;
        public static readonly DependencyProperty DesiredPositionProperty;
        private Size[] _measures;

        static ArrangePanel()
        {
			SourceListViewProperty = DependencyProperty.RegisterAttached(
				"SourceListView",
				typeof(ListView),
				typeof(ArrangePanel),
				new FrameworkPropertyMetadata(
					null,
					FrameworkPropertyMetadataOptions.AffectsParentArrange));

			PositionProperty = DependencyProperty.RegisterAttached(
                "Position",
                typeof(Rect),
                typeof(ArrangePanel),
                new FrameworkPropertyMetadata(
                    new Rect(double.NaN, double.NaN, double.NaN, double.NaN),
                    FrameworkPropertyMetadataOptions.AffectsParentArrange));

            DesiredPositionProperty = DependencyProperty.RegisterAttached(
                "DesiredPosition",
                typeof(Rect),
                typeof(ArrangePanel),
                new FrameworkPropertyMetadata(
                    new Rect(double.NaN, double.NaN, double.NaN, double.NaN),
                    OnDesiredPositionChanged));

            OrderProperty = DependencyProperty.RegisterAttached(
                "Order",
                typeof(int),
                typeof(ArrangePanel),
                new FrameworkPropertyMetadata(
                    -1,
                    FrameworkPropertyMetadataOptions.AffectsParentMeasure));
        }

        private static void OnDesiredPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var desiredPosition = (Rect)e.NewValue;
            AnimateToPosition(d, desiredPosition);
        }

        private static void AnimateToPosition(DependencyObject d, Rect desiredPosition)
        {
            var position = GetPosition(d);
            if (double.IsNaN(position.X))
            {
                SetPosition(d, desiredPosition);
                return;
            }

            var distance = Math.Max(
                (desiredPosition.TopLeft - position.TopLeft).Length, 
                (desiredPosition.BottomRight-position.BottomRight).Length);

            var animationTime = TimeSpan.FromMilliseconds(distance*2);
            var animation = new RectAnimation(position, desiredPosition, new Duration(animationTime));
            animation.DecelerationRatio = 1;
            ((UIElement) d).BeginAnimation(PositionProperty, animation);
        }

		public static ListView GetSourceListView(DependencyObject obj)
		{
			return (ListView)obj.GetValue(SourceListViewProperty);
		}

		public static void SetSourceListView(DependencyObject obj, ListView value)
		{
			obj.SetValue(SourceListViewProperty, value);
		}

		public static int GetOrder(DependencyObject obj)
        {
            return (int)obj.GetValue(OrderProperty);
        }

        public static void SetOrder(DependencyObject obj, int value)
        {
            obj.SetValue(OrderProperty, value);
        }

        public static Rect GetPosition(DependencyObject obj)
        {
            return (Rect)obj.GetValue(PositionProperty);
        }

        public static void SetPosition(DependencyObject obj, Rect value)
        {
            obj.SetValue(PositionProperty, value);
        }

        public static Rect GetDesiredPosition(DependencyObject obj)
        {
            return (Rect)obj.GetValue(DesiredPositionProperty);
        }

        public static void SetDesiredPosition(DependencyObject obj, Rect value)
        {
            obj.SetValue(DesiredPositionProperty, value);
        }
    }
}
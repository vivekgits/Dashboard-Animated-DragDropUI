using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using DragDropAnimated.DragDropManager.UI;
using Xceed.Wpf.Toolkit;

namespace DragDropAnimated
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		ListViewDragDropManager<Apps> dragMgr;
		ListViewDragDropManager<Apps> dragMgr2;
		string[] colors = new string[] { "#FF4343","#567C73", "#69797E", "#7A7574", "#E74856", "#EF6950", "#F7630C", "#FFB900", "#498205", "#00CC6A", "#00B294", "#00B7C3",
		"#0099BC","#0078D7","#8E8CD8","#8764B8","#B164C2","#C239B3","#E300BC","#EA005E","#647C64","#847545","#68768A","#767676"};

		public MainWindow()
		{
			InitializeComponent();
			
			this.dragMgr = new ListViewDragDropManager<Apps>(null);
			this.dragMgr2 = new ListViewDragDropManager<Apps>(null);
			var Apps = GetAllApps();
			var allPrimary = Apps.Where(
								app => app.Type.ToUpper() == "PRIMARY").ToList();
			PrimaryApps = new EditableApps(allPrimary, false);
			var allSecondary = Apps.Where(
							 app => app.Type.ToUpper() == "SECONDARY").ToList();
			SecondaryApps = new EditableApps(allSecondary, false);
			
			CustomColors = new ObservableCollection<ColorItem>();
			colors.ToList().ForEach(x => CustomColors.Add(new ColorItem((Color)ColorConverter.ConvertFromString(x), x)));
			
			DataContext = this;
			Loaded += MainWindow_Loaded;
			GridMain.SizeChanged += GridMain_SizeChanged;
		}

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			NotifyPropertyChanged("PrimaryAppPanelWidth");

			this.PrimaryAppContainer.DragEnter += OnListViewDragEnter;
			this.SecondaryAppContainer.DragEnter += OnListViewDragEnter;
			this.PrimaryAppContainer.Drop += OnListViewDrop;
			this.SecondaryAppContainer.Drop += OnListViewDrop;
			((INotifyCollectionChanged)this.PrimaryAppContainer.ItemsSource).CollectionChanged +=
			new NotifyCollectionChangedEventHandler(PListCollectionChanged);
			((INotifyCollectionChanged)this.SecondaryAppContainer.ItemsSource).CollectionChanged +=
			new NotifyCollectionChangedEventHandler(SListCollectionChanged);
			this.chkManageDragging.Checked += delegate { if (this.dragMgr.ListView == null) { this.dragMgr.ListView = this.PrimaryAppContainer; }if (this.dragMgr2.ListView == null) { dragMgr2.ListView = SecondaryAppContainer; }
				if (PrimaryApps.Apps.Count > 1) { PrimaryApps.IsEditMode = true; }if (SecondaryApps.Apps.Count > 1) { SecondaryApps.IsEditMode = true; }
			};
			this.chkManageDragging.Unchecked += delegate { if (this.dragMgr.ListView != null) { this.dragMgr.ListView = null; } if (this.dragMgr2.ListView != null) { dragMgr2.ListView = null; } PrimaryApps.IsEditMode = false; SecondaryApps.IsEditMode = false; };
		}

		private void SListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var count = (sender as Collection<Apps>).Count;
			if (count < 2)
			{
				this.SecondaryApps.IsEditMode = false;
				return;
			}

			this.SecondaryApps.IsEditMode = true;
		}

		private void PListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var count = (sender as Collection<Apps>).Count;
			if (count < 2)
			{
				this.PrimaryApps.IsEditMode = false;
				return;
			}

			this.PrimaryApps.IsEditMode = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		// This method is called by the Set accessor of each property.
		// The CallerMemberName attribute that is applied to the optional propertyName
		// parameter causes the property name of the caller to be substituted as an argument.
		private void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private EditableApps _AppsPrimary;
		public EditableApps PrimaryApps
		{
			set
			{
				if (_AppsPrimary != value)
				{
					_AppsPrimary = value;
					NotifyPropertyChanged("PrimaryApps");
				}
			}
			get { return _AppsPrimary; }
		}

		private EditableApps _AppsSecondary;
		public EditableApps SecondaryApps
		{
			set
			{
				if (_AppsSecondary != value)
				{
					_AppsSecondary = value;
					NotifyPropertyChanged("SecondaryApps");
				}
			}
			get { return _AppsSecondary; }
		}

		private ObservableCollection<ColorItem> _CustomColors;
		public ObservableCollection<ColorItem> CustomColors
		{
			set
			{
				if (_CustomColors != value)
				{
					_CustomColors = value;
					NotifyPropertyChanged("CustomColors");
				}
			}
			get { return _CustomColors; }
		}

		public class EditableApps : INotifyPropertyChanged
		{
			public EditableApps(List<Apps> _Apps, bool _IsEditable)
			{
				this.Apps = new ObservableCollection<Apps>(_Apps);
				this.IsEditMode = _IsEditable;
			}

			private ObservableCollection<Apps> _Apps;
			public ObservableCollection<Apps> Apps
			{
				set
				{
					if (_Apps != value)
					{
						_Apps = value;
						NotifyPropertyChanged("Apps");
						IsEditMode = (_Apps.Count>1)?true: false;
						NotifyPropertyChanged("IsEditMode");
					}
				}
				get { return _Apps; }
			}

			private bool _IsEditMode;
			public bool IsEditMode
			{
				get { return _IsEditMode; }
				set { _IsEditMode = value; NotifyPropertyChanged("IsEditMode"); }
			}

			public event PropertyChangedEventHandler PropertyChanged;

			// This method is called by the Set accessor of each property.
			// The CallerMemberName attribute that is applied to the optional propertyName
			// parameter causes the property name of the caller to be substituted as an argument.
			private void NotifyPropertyChanged(String propertyName = "")
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}
		public ObservableCollection<Apps> GetAllApps()
		{
			try
			{
				XmlSerializer myDeserializer = new XmlSerializer(typeof(ObservableCollection<Apps>));
				FileStream myFileStream = new FileStream(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Apps.xml"), FileMode.Open, FileAccess.Read);
				var loadedData = (ObservableCollection<Apps>)myDeserializer.Deserialize(myFileStream);
				myFileStream.Close();
				return loadedData;
			}
			catch (Exception ex) { throw ex; }
		}

		public double PrimaryAppPanelWidth
		{
			get
			{
				return this.ActualWidth - SecondaryAppContainer.ActualWidth - 36;
			}
		}

		public Point CursorCenter
		{
			get
			{
				return new Point(100, 20);
			}
		}

		public double MainGridActualWidth
		{
			set { NotifyPropertyChanged("PrimaryAppPanelWidth"); }
		}

		public void GridMain_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.MainGridActualWidth = e.NewSize.Width;
		}

		#region dragMgr_ProcessDrop

		// Performs custom drop logic for the top ListView.
		void dragMgr_ProcessDrop(object sender, ProcessDropEventArgs<Apps> e)
		{
			// This shows how to customize the behavior of a drop.
			// Here we perform a swap, instead of just moving the dropped item.

			int higherIdx = Math.Max(e.OldIndex, e.NewIndex);
			int lowerIdx = Math.Min(e.OldIndex, e.NewIndex);
			
			if (lowerIdx < 0)
			{
				// The item came from the lower ListView
				// so just insert it.
				e.ItemsSource.Insert(higherIdx, e.DataItem);
			}
			else
			{
				// null values will cause an error when calling Move.
				// It looks like a bug in ObservableCollection to me.
				if (e.ItemsSource[lowerIdx] == null ||
					e.ItemsSource[higherIdx] == null)
					return;

				// The item came from the ListView into which
				// it was dropped, so swap it with the item
				// at the target index.
				e.ItemsSource.Move(lowerIdx, higherIdx);
				e.ItemsSource.Move(higherIdx - 1, lowerIdx);
			}

			// Set this to 'Move' so that the OnListViewDrop knows to 
			// remove the item from the other ListView.
			e.Effects = DragDropEffects.Move;
		}

		#endregion // dragMgr_ProcessDrop

		#region OnListViewDragEnter

		// Handles the DragEnter event for both ListViews.
		void OnListViewDragEnter(object sender, DragEventArgs e)
		{
			PrimaryAppContainer.ApplyTemplate();
			e.Effects = DragDropEffects.Move;
		}

		#endregion // OnListViewDragEnter

		#region OnListViewDrop

		// Handles the Drop event for both ListViews.
		void OnListViewDrop(object sender, DragEventArgs e)
		{
			if (e.Effects == DragDropEffects.None)
				return;

			Apps task = e.Data.GetData(typeof(Apps)) as Apps;
			if (sender == this.PrimaryAppContainer)
			{
				var source = this.SecondaryAppContainer.ItemsSource as ObservableCollection<Apps>;
				if (this.dragMgr.IsDragInProgress)
					return;

				// An item was dragged from the bottom ListView into the top ListView
				// so remove that item from the bottom ListView.
				(source).Remove(task);
			}
			else
			{
				var source = this.PrimaryAppContainer.ItemsSource as ObservableCollection<Apps>;
				if (this.dragMgr2.IsDragInProgress)
					return;

				// An item was dragged from the top ListView into the bottom ListView
				// so remove that item from the top ListView.
				(source).Remove(task);
			}
		}

		#endregion // OnListViewDrop
	}
}

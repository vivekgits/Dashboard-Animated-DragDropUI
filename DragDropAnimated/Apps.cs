using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace DragDropAnimated
{
	public class Apps : ICloneable
	{
		public string ProcessName { set; get; }
		public string ProcessFullPath { set; get; }
		public string CommandArguements { set; get; }
		
		public string Type { set; get; }
		public string BackColor { set; get; }
		public string ImagePath { get; set; }

		//cloning methods
		public Apps Clone()
		{ return (Apps)this.MemberwiseClone(); }
		object ICloneable.Clone()
		{ return Clone(); }

		[XmlIgnore]
		public DrawingGroup ImageSource
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ImagePath))
				{
					try
					{
						return Application.Current.FindResource(ImagePath) as DrawingGroup;
					}
					catch (Exception) { throw; }
				}
				else return null;
			}
		}
	}
}

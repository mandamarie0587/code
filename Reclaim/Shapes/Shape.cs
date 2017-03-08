using System;
using Newtonsoft.Json;

namespace Reclaim
{
	[Serializable]
	[JsonObject(IsReference = true)]
	public class Shape
	{
		public Shape(string id, string shape_type)
		{
			this.ID = id;
			this.FillColor = new RGB();
			this.ShapeType = shape_type;
		}

		[JsonProperty("shapeType")]
		public string ShapeType
		{
			get;
			set; 
		}

		[JsonProperty("id")]
		public string ID
		{
			get;
			set;
		}

		[JsonProperty("fillColor")]
		public RGB FillColor
		{
			get;
			set;
		}
	}
}

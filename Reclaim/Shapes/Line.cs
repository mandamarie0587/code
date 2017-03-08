using System;
using Newtonsoft.Json; 

namespace Reclaim
{
	[Serializable]
	[JsonObject(IsReference=true)]
	public class Line: Shape
	{
		public Line(string id) : base(id, "line")
		{
			this.X1 = 0;
			this.Y1 = 0;
			this.X2 = 0;
			this.Y2 = 0;
			this.Thickness = 1; 
		}

		[JsonProperty("x1")]
		public int X1
		{
			get;
			set;
		}

		[JsonProperty("y1")]
		public int Y1
		{
			get;
			set; 
		}

		[JsonProperty("x2")]
		public int X2
		{
			get;
			set; 
		}

		[JsonProperty("y2")]
		public int Y2
		{
			get;
			set; 
		}

		[JsonProperty("thickness")]
		public int Thickness
		{
			get;
			set; 
		}
	}
}

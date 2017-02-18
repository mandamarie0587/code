using System;
using Newtonsoft.Json; 

namespace Reclaim
{
	[Serializable]
	[JsonObject(IsReference=true)]
	public class Rectangle
	{
		public Rectangle(string id)
		{
			this.ID = id;
			this.BorderRadius = new Radius();
			this.BackgroundColor = new RGB();
			this.BorderColor = new RGB(); 
			this.BorderWidth = 1; 

		}

		[JsonProperty("id")]
		public string ID
		{
			get;
			set; 
		}

		[JsonProperty("backgroundColor")]
		public RGB BackgroundColor
		{
			get;
			set;
		}

		[JsonProperty("borderColor")]
		public RGB BorderColor
		{
			get;
			set; 
		}

		[JsonProperty("borderWidth")]
		public int BorderWidth
		{
			get;
			set; 
		}

		[JsonProperty("borderRadius")]
		public Radius BorderRadius
		{
			get;
			set; 
		}
	}

	[Serializable]
	[JsonObject]
	public class RGB
	{
		public RGB(int r=255, int g=255, int b=255)
		{
			this.Red = r;
			this.Green = g;
			this.Blue = b; 
		}

		[JsonProperty("r")]
		public int Red
		{
			get;
			set; 
		}

		[JsonProperty("g")]
		public int Green
		{
			get;
			set; 
		}

		[JsonProperty("b")]
		public int Blue
		{
			get;
			set; 
		}
	}

	[Serializable]
	[JsonObject]
	public class Radius
	{
		public Radius(int topLeft = 0, int topRight = 0, int bottomLeft = 0, int bottomRight = 0)
		{
			this.TopLeft = topLeft;
			this.TopRight = topRight;
			this.BottomLeft = bottomLeft;
			this.BottomRight = bottomRight; 
		}

		[JsonProperty("topLeft")]
		public int TopLeft
		{
			get;
			set; 
		}

		[JsonProperty("topRight")]
		public int TopRight
		{
			get;
			set; 
		}

		[JsonProperty("bottomLeft")]
		public int BottomLeft
		{
			get;
			set; 
		}

		[JsonProperty("bottomRight")]
		public int BottomRight
		{
			get;
			set; 
		}
	}
}

using System;
using Newtonsoft.Json;

namespace Reclaim
{
	[Serializable]
	[JsonObject(IsReference = true)]
	public class Shape
	{
		public Shape(string id)
		{
			this.ID = id;
			this.FillColor = new RGB();
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

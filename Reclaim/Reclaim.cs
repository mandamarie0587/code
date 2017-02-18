
using System;
using Prefab;
using System.Collections.Generic;


using PrefabIdentificationLayers.Models;
using PrefabIdentificationLayers.Prototypes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Reclaim
{
	public class ReclaimBitmap
	{
		public static RGB GetColor(Bitmap bitmap)
		{
			int numPixels = bitmap.Pixels.Length;
			int r = 0;
			int g = 0;
			int b = 0;
			for (int i = 0; i < bitmap.Pixels.Length; i++)
			{
				int pixelValue = bitmap.Pixels[i];
				r += Bitmap.Red(pixelValue);
				g += Bitmap.Green(pixelValue);
				b += Bitmap.Blue(pixelValue);
			}

			r = (r / numPixels);
			g = (g / numPixels);
			b = (b / numPixels);
			return new RGB(r, g, b);
		}

		public static RGB GetAverageColor(List<RGB> rgbs)
		{
			int r = 0;
			int g = 0;
			int b = 0;
			for (int i = 0; i < rgbs.Count; i++)
			{
				r += rgbs[i].Red;
				g += rgbs[i].Green;
				b += rgbs[i].Blue;
			}

			r = (r / rgbs.Count);
			g = (g / rgbs.Count);
			b = (b / rgbs.Count);
			return new RGB(r, g, b);
		}
	}

	public class Reclaim
	{
		public static int Main(string[] args)
		{
			if (args.Length > 0)
			{
				List<Rectangle> results = new List<Rectangle>();

				for (int i = 0; i < args.Length; i++)
				{
					// Each argument is a separate image file to be processed
					try
					{
						// Create a new Bitmap 
						Bitmap bitmap = Bitmap.FromFile(args[i]);
						BuildPrototypeArgs ptypeArgs = GetPrototypeArgsForImage(bitmap);
						Ptype.Mutable result = Ptype.BuildFromExamples(ptypeArgs);
						if (result != null)
						{
							string id = GetIDFromFilename(args[i]);
							if (id.Length > 0)
							{
								Rectangle newRectangle = ConvertPrototypeToJson(result, id);
								results.Add(newRectangle);
							}

						}
					}
					catch (Exception e)
					{
						FailedToLoad(args[i], e);
					}
				}

				string output = JsonConvert.SerializeObject(results);
				Console.WriteLine(output);
			}

			return 0;
		}

		public static string GetIDFromFilename(string filename)
		{
			string[] chunks = filename.Split('_');
			if (chunks.Length > 0)
			{
				string last = chunks[chunks.Length - 1];
				// Split again by . separator to remove the extension
				string[] withExt = last.Split('.');
				if (withExt.Length > 0)
				{
					string first = withExt[0];
					return first;
				}
			}

			return "";
		}

		// construct prototype args from thew bitmap image
		public static BuildPrototypeArgs GetPrototypeArgsForImage(Bitmap bitmap)
		{
			string id = Guid.NewGuid().ToString();

			// Build list of examples. There will just be 
			List<Bitmap> positives = new List<Bitmap>();
			List<Bitmap> negatives = new List<Bitmap>();
			positives.Add(bitmap);
			Examples examples = new Examples(positives, negatives);

			BuildPrototypeArgs args = new BuildPrototypeArgs(examples, ModelInstances.NinePart, id);
			return args;
		}

		public static Rectangle ConvertPrototypeToJson(Ptype.Mutable prototype, string id)
		{
			// Convert features topleft,topright,bottomleft,bottomright to their corresponding corner radius values
			Rectangle myRectangle = new Rectangle(id);

			// topleft corner radius
			Bitmap topLeft = null;
			if (prototype.Features.TryGetValue("topleft", out topLeft))
			{
				int topLeftRadius = (topLeft.Height + topLeft.Width) / 2;
				myRectangle.BorderRadius.TopLeft = topLeftRadius;
			}

			// bottomLeft corner radius
			Bitmap bottomLeft = null;
			if (prototype.Features.TryGetValue("bottomleft", out bottomLeft))
			{
				int bottomLeftRadius = (bottomLeft.Height + bottomLeft.Width) / 2;
				myRectangle.BorderRadius.BottomLeft = bottomLeftRadius;
			}

			// topRight corner radius
			Bitmap topRight = null;
			if (prototype.Features.TryGetValue("topright", out topRight))
			{
				int topRightRadius = (topRight.Height + topRight.Width) / 2;
				myRectangle.BorderRadius.TopRight = topRightRadius;
			}

			// bottomRight corner radius
			Bitmap bottomRight = null;
			if (prototype.Features.TryGetValue("bottomright", out bottomRight))
			{
				int bottomRightRadius = (bottomRight.Height + bottomRight.Width) / 2;
				myRectangle.BorderRadius.BottomRight = bottomRightRadius;
			}

			//  Get border width, border color from the extracted side regions
			// Height
			PrefabIdentificationLayers.Regions.Region bottom = null;
			int bottomHeight = 1;
			List<RGB> borderColors = new List<RGB>();
			if (prototype.Regions.TryGetValue("bottom", out bottom))
			{
				bottomHeight = bottom.Bitmap.Height;
				borderColors.Add(ReclaimBitmap.GetColor(bottom.Bitmap));
			}

			PrefabIdentificationLayers.Regions.Region top = null;
			int topHeight = 1;
			if (prototype.Regions.TryGetValue("top", out top))
			{
				topHeight = top.Bitmap.Height;
				borderColors.Add(ReclaimBitmap.GetColor(top.Bitmap));
			}

			// Width
			PrefabIdentificationLayers.Regions.Region left = null;
			int leftWidth = 1;
			if (prototype.Regions.TryGetValue("left", out left))
			{
				leftWidth = left.Bitmap.Width;
				borderColors.Add(ReclaimBitmap.GetColor(left.Bitmap));
			}

			PrefabIdentificationLayers.Regions.Region right = null;
			int rightWidth = 1;
			if (prototype.Regions.TryGetValue("right", out right))
			{
				rightWidth = right.Bitmap.Width;
				borderColors.Add(ReclaimBitmap.GetColor(right.Bitmap));
			}

			// Average all border widths because they can't be set separately anyway
			myRectangle.BorderWidth = (topHeight + bottomHeight + leftWidth + rightWidth) / 4;
			myRectangle.BorderColor = ReclaimBitmap.GetAverageColor(borderColors);

			// Get the background color
			PrefabIdentificationLayers.Regions.Region interior = null;
			RGB backgroundColor = new RGB();
			if (prototype.Regions.TryGetValue("interior", out interior))
			{
				backgroundColor = ReclaimBitmap.GetColor(interior.Bitmap);
			}
			myRectangle.BackgroundColor = backgroundColor;

			return myRectangle;
		}

		private static void FailedToLoad(string filename, Exception e)
		{
			Console.WriteLine(e.StackTrace);
		}
	}
}

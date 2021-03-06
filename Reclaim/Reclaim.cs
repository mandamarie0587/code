﻿
using System;
using Prefab;
using System.Collections.Generic;
using PrefabModels = PrefabIdentificationLayers.Models;
using PrefabIdentificationLayers.Prototypes;
using ReclaimModels = Reclaim.Models;
using Newtonsoft.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Reclaim
{
	public class ReclaimBitmap
	{
		public static RGB GetColor(Prefab.Bitmap bitmap)
		{
			int numPixels = bitmap.Pixels.Length;
			int r = 0;
			int g = 0;
			int b = 0;
			for (int i = 0; i < bitmap.Pixels.Length; i++)
			{
				int pixelValue = bitmap.Pixels[i];
				r += Prefab.Bitmap.Red(pixelValue);
				g += Prefab.Bitmap.Green(pixelValue);
				b += Prefab.Bitmap.Blue(pixelValue);
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
				List<Shape> results = new List<Shape>();

				for (int i = 0; i < args.Length; i++)
				{
					// Each argument is a separate image file to be processed
					try
					{
						// Create a new Bitmap 
						Prefab.Bitmap bitmap = Prefab.Bitmap.FromFile(args[i]);
						bool wasResized = false;
						double resizeRatio = 1.0; 
						if(bitmap.Width > bitmap.Height) 
						{
							if (bitmap.Width > 100 && bitmap.Height > 10) {
								int newWidth = 100;
								resizeRatio = (double)newWidth/(double)bitmap.Width;
								int newHeight = (int)(resizeRatio*bitmap.Height);

								if(newWidth > 0 && newHeight > 0) {
									System.Drawing.Bitmap orig = Prefab.Bitmap.ToSystemDrawingBitmap(bitmap);
									System.Drawing.Bitmap resized = ResizeImage(orig, newWidth, newHeight);
									bitmap = Prefab.Bitmap.FromSystemDrawingBitmap(resized);									
								}
							}
						}
						else 
						{
							if (bitmap.Height > 100 && bitmap.Width > 10) {
								int newHeight = 100;
								resizeRatio = (double)newHeight/(double)bitmap.Height;

								int newWidth = (int)resizeRatio*bitmap.Width;
								if(newWidth > 0 && newHeight > 0) {
									System.Drawing.Bitmap orig = Prefab.Bitmap.ToSystemDrawingBitmap(bitmap);
									System.Drawing.Bitmap resized = ResizeImage(orig, newWidth, newHeight);
									bitmap = Prefab.Bitmap.FromSystemDrawingBitmap(resized);
								}
							}
						}


						List<string> models = new List<string>();
						models.Add("sixpart");
						//models.Add("line"); 
						foreach(string model in models)
						{
							var watch = System.Diagnostics.Stopwatch.StartNew(); 

							PrefabModels.BuildPrototypeArgs ptypeArgs = GetPrototypeArgsForImage(bitmap, ReclaimModels.ModelInstances.Get(model));
							Ptype.Mutable result = Ptype.BuildFromExamples(ptypeArgs);

							if (result != null)
							{
								string id = GetIDFromFilename(args[i]);
								if (id.Length > 0)
								{
									Shape newShape = ConvertPrototypeToJson(model, result, id, resizeRatio);
									results.Add(newShape);
									watch.Stop();
									var elapsedS = (watch.ElapsedMilliseconds / 1000);
									break;
								}

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
		public static PrefabModels.BuildPrototypeArgs GetPrototypeArgsForImage(Prefab.Bitmap bitmap, PrefabModels.Model model)
		{
			string id = Guid.NewGuid().ToString();

			// Build list of examples. There will just be 
			List<Prefab.Bitmap> positives = new List<Prefab.Bitmap>();
			List<Prefab.Bitmap> negatives = new List<Prefab.Bitmap>();
			positives.Add(bitmap);
			PrefabModels.Examples examples = new PrefabModels.Examples(positives, negatives); 
			PrefabModels.BuildPrototypeArgs args = new PrefabModels.BuildPrototypeArgs(examples, model, id);

			return args;
		}

		public static Shape ConvertPrototypeToJson(string model, Ptype.Mutable prototype, string id, double resizeRatio=1.0)
		{
			if (model == "sixpart")
			{
				return ConvertRectangleToJson(prototype, new Rectangle(id), resizeRatio);
			}
			else if (model == "line")
			{
				return ConvertLineToJson(prototype, new Line(id), resizeRatio);
			}

			return null; 
		}

		public static Line ConvertLineToJson(Ptype.Mutable prototype, Line myLine, double resizeRatio=1.0)
		{
			// Get the thickness of the line
			PrefabIdentificationLayers.Regions.Region interior = null;
			RGB backgroundColor = new RGB();
			int thickness = 1; 
			if (prototype.Regions.TryGetValue("interior", out interior))
			{
				backgroundColor = ReclaimBitmap.GetColor(interior.Bitmap);
				thickness = interior.Bitmap.Height;
			}
			myLine.FillColor = backgroundColor;
			myLine.Thickness = thickness; 

			return myLine; 
		}

		public static Rectangle ConvertRectangleToJson(Ptype.Mutable prototype, Rectangle myRectangle, double resizeRatio=1.0)
		{
			// Corner radius
			Prefab.Bitmap corner = null;
			if (prototype.Features.TryGetValue("corner", out corner))
			{
				int cornerRadius = (corner.Height + corner.Width) / 2;
				if(resizeRatio != 1.0) 
				{
					cornerRadius = (int)((double)cornerRadius/resizeRatio);
				}

				myRectangle.BorderRadius.Corner = cornerRadius;
			}

			//  Get border width, border color from the extracted side regions
			// Height
			PrefabIdentificationLayers.Regions.Region bottom = null;
			int bottomHeight = 0;
			List<RGB> borderColors = new List<RGB>();
			if (prototype.Regions.TryGetValue("bottom", out bottom))
			{
				bottomHeight = bottom.Bitmap.Height;
				borderColors.Add(ReclaimBitmap.GetColor(bottom.Bitmap));
			}

			PrefabIdentificationLayers.Regions.Region top = null;
			int topHeight = 0;
			if (prototype.Regions.TryGetValue("top", out top))
			{
				topHeight = top.Bitmap.Height;
				borderColors.Add(ReclaimBitmap.GetColor(top.Bitmap));
			}

			// Width
			PrefabIdentificationLayers.Regions.Region left = null;
			int leftWidth = 0;
			if (prototype.Regions.TryGetValue("left", out left))
			{
				leftWidth = left.Bitmap.Width;
				borderColors.Add(ReclaimBitmap.GetColor(left.Bitmap));
			}

			PrefabIdentificationLayers.Regions.Region right = null;
			int rightWidth = 0;
			if (prototype.Regions.TryGetValue("right", out right))
			{
				rightWidth = right.Bitmap.Width;
				borderColors.Add(ReclaimBitmap.GetColor(right.Bitmap));
			}

			// Average all border widths because they can't be set separately anyway
			myRectangle.BorderWidth = (topHeight + bottomHeight + leftWidth + rightWidth) / 4;
			if(resizeRatio != 1.0) 
			{
				myRectangle.BorderWidth = (int)((double)myRectangle.BorderWidth/resizeRatio);
			}

			if (borderColors.Count > 0)
			{
				myRectangle.BorderColor = ReclaimBitmap.GetAverageColor(borderColors);
			}
			else
			{
				myRectangle.BorderColor = new RGB();
			}

			// Get the background color
			PrefabIdentificationLayers.Regions.Region interior = null;
			RGB fillColor = new RGB();
			if (prototype.Regions.TryGetValue("interior", out interior))
			{
				fillColor = ReclaimBitmap.GetColor(interior.Bitmap);
			}
			myRectangle.FillColor = fillColor;

			if (myRectangle.FillColor.Red == myRectangle.BorderColor.Red && myRectangle.FillColor.Green == myRectangle.BorderColor.Green 
				&&  myRectangle.FillColor.Blue == myRectangle.BorderColor.Blue) 
			{
				// Collapse the border into the rectangle if they are the exact same color
				myRectangle.BorderWidth = 0;
				myRectangle.BorderColor = null;
			}

			return myRectangle; 
		}

		private static void FailedToLoad(string filename, Exception e)
		{
			Console.WriteLine(e.StackTrace);
		}

		/// <summary>
		/// Resize the image to the specified width and height.
		/// </summary>
		/// <param name="image">The image to resize.</param>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		public static System.Drawing.Bitmap ResizeImage(System.Drawing.Bitmap image, int width, int height)
		{
	    	Console.WriteLine(width); 
		    Console.WriteLine(height);

		    var destRect = new System.Drawing.Rectangle(0, 0, width, height);
		    var destImage = new System.Drawing.Bitmap(width, height);

		    destImage.SetResolution(width,height);
	
		    using (var graphics = Graphics.FromImage(destImage))
		    {
		        graphics.CompositingMode = CompositingMode.SourceCopy;
		        graphics.CompositingQuality = CompositingQuality.HighQuality;
		        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		        graphics.SmoothingMode = SmoothingMode.HighQuality;
		        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

		        using (var wrapMode = new ImageAttributes())
		        {
		            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
		            graphics.DrawImage(image, destRect, 0, 0, image.Width,image.Height, GraphicsUnit.Pixel, wrapMode);
		        }
		    }

		    return destImage;
		}
	}
}

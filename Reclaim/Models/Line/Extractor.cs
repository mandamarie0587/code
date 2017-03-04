using System;
using System.Collections.Generic;
using PrefabIdentificationLayers.Models;
using PrefabIdentificationLayers.Models.NinePart;
using PrefabIdentificationLayers;
using Prefab;
using PrefabIdentificationLayers.Regions;
using System.Linq;

namespace Reclaim.Models.Line
{
	public class Extractor
	{
		/// <summary>
		/// Uses the assigned value to a part to parameterize that part from examples.
		/// It caches the parameterizations, and checks the cache to see if it has
		/// made the parameterization beforeache.
		/// </summary>
		/// <param name="name">Name of the part to parameterize.</param>
		/// <param name="value">The part's value.</param>
		/// <param name="assignment">The whole assignment.</param>
		/// <param name="positives">Positive examples</param>
		/// <param name="negatives">Negative examples</param>
		/// <param name="cache">The cache foreach storing parameterizations to check foreach subsequent calls.</param>
		/// <returns>The parameterized part (either a feature or region)</returns>
		public static double ExtractPartAndGetCost(string name, object value,
			Dictionary<string, Part> assignment, IEnumerable<Bitmap> positives, IEnumerable<Bitmap> negatives, Dictionary<object, object> cache)
		{

			Tuple<string, object> key = new Tuple<string, object>(name, value);

			Object extracted = Extractor.ExtractPart(key, name, value, assignment, positives, negatives, cache); 

			if (extracted == null)
			{
				if (Equals(name, "interior"))
				{
					return Double.PositiveInfinity;
				}
				else 
				{
					return 0; // If the part could not be extracted, it has no size which we want to reward in this model . 
				}
			}

			//if we did extract a part - let's return it's values
			double partcost = 0;

			if (extracted is Bitmap)
				partcost = ((Bitmap)extracted).PixelCount();
			else if (extracted is Region)
				partcost = ((Region)extracted).Bitmap.PixelCount();
			else 
			{
				// Replacing cost function to penalize by the distance away from the height of the example bitmap so that it tries to find the maximum height repeating pattern that it can 
				partcost = ((BackgroundResults)extracted).Missed + ((BackgroundResults)extracted).Region.Bitmap.PixelCount();//ComputeInteriorCost(((BackgroundResults)extracted), positives);
			}

			return partcost;
		}

		public static int ComputeInteriorCost(BackgroundResults backgroundRegion, IEnumerable<Bitmap> positives)
		{
			Region region = backgroundRegion.Region;
			int cost = 0; 
			if (region.MatchStrategy == "horizontal" || region.MatchStrategy == "single")
			{
				int height = region.Bitmap.Height;
				int maxHeight = -1; 
				foreach (Bitmap positive in positives)
				{
					if (maxHeight == -1 || positive.Height > maxHeight)
					{
						maxHeight = positive.Height; 
					}
				}

				cost += (maxHeight - height); 
			}
			else if (region.MatchStrategy == "vertical")
			{
				int width = region.Bitmap.Width; 
				int maxWidth = -1;
				foreach (Bitmap positive in positives)
				{
					if (maxWidth == -1 || positive.Width > maxWidth)
					{
						maxWidth = positive.Width;
					}
				}

				cost += (maxWidth - width); 
			}

			return cost; 
		}

		public static object ExtractPart(Tuple<string, object> key, string name, object value,
	Dictionary<string, Part> assignment, IEnumerable<Bitmap> positives, IEnumerable<Bitmap> negatives, Dictionary<object, object> cache)
		{
			//Look in our cache to see if we extracted the part beforeache
			object extracted = null;
			Size size = null;
			RegionParameters rps = null;
			BackgroundValue bv = null;
			if (value is Size)
				size = (Size)value;
			else if (value is RegionParameters)
				rps = (RegionParameters)value;
			else if (value is string)
			{
				bv = GetBackgroundValue((String)value, assignment);
				value = bv;
			}

			if (!cache.ContainsKey(key))
			{
				switch (name)
				{
					case "corner":
						extracted = CropFromManyRelativeTopLeft(0, 0, size.Width, size.Height, positives);
						break;

					case "top":
						extracted = GetHorizontal(true, rps.Start, rps.End, rps.Depth, positives);
						break;

					case "bottom":
						extracted = GetHorizontal(false, rps.Start, rps.End, rps.Depth, positives);
						break;

					case "left":
						extracted = GetVertical(true, rps.Start, rps.End, rps.Depth, positives);
						break;

					case "right":
						extracted = GetVertical(false, rps.Start, rps.End, rps.Depth, positives);
						break;

					case "interior":
						extracted = GetInterior(bv, positives);
						break;
				}

				//Features that have all one value are likely to be background. So we disallow it.
				//Features that are all transparent can't be found (obviously). So we disallow it.
				if (extracted != null && extracted is Bitmap && Bitmap.AllTransparent((Bitmap)extracted))
				{
					extracted = null;
				}

				cache.Add(key, extracted);
			}
			else
				extracted = cache[key];

			return extracted; 
		}

		private static object GetInterior(BackgroundValue bp, IEnumerable<Bitmap> positives)
		{


			//Get the actual pattern
			Bitmap pattern = null;
			switch (bp.Type)
			{
			case "single":
				pattern = MostFrequentPixel(bp, positives);
				break;

			case "horizontal":
				pattern = MostFrequentColumn(bp, positives);
				break;

			case "vertical":
				pattern = MostFrequentRow(bp, positives);
				break;

			}

			if (pattern == null)
				return null;

			//Get the matching strategy and what was missed
			string matcher = null;
			int missed = 0;
			switch (bp.Type)
			{
			case "single":
			case "horizontal":
				matcher = "horizontal";
				foreach (Bitmap bmp in positives)
					missed += HorizontalPatternMatcher.Missed(pattern, bmp, bp);
				break;

			case "vertical":
				matcher = "vertical";
				foreach (Bitmap bmp in positives)
					missed += VerticalPatternMatcher.Missed(pattern, bmp, bp);
				break;
			}

			BackgroundResults results = new BackgroundResults();
			results.Region = new Region(matcher, pattern);
			results.Missed = missed;
			return results;
		}

		private static Bitmap MostFrequentColumn(BackgroundValue bv, IEnumerable<Bitmap> positives)
		{
			if (!AllSameHeight(positives))
				return null;

			Dictionary<Bitmap, int> freqs = new Dictionary<Bitmap, int>();
			foreach (Bitmap bmp in positives)
			{
				Bitmap currCol = null;
				for (int column = bv.TopLeft.Width; column < bmp.Width - bv.TopRight.Width; column++)
				{
					currCol = Bitmap.Crop(bmp, column, bv.Top,  1, bmp.Height - bv.Bottom - bv.Top);

					if (!freqs.ContainsKey(currCol))
						freqs.Add(currCol, 1);
					else
						freqs[currCol] = freqs[currCol] + 1;
				}
			}

			int max = int.MinValue;
			Bitmap key = null;
			foreach(KeyValuePair<Bitmap, int> pair in freqs)
			{
				if(pair.Value > max){
					max = pair.Value;
					key = pair.Key;
				}

			}

			return key;
		}

		private static bool AllSameHeight(IEnumerable<Bitmap> positives)
		{
			int height = positives.First ().Height;
			foreach (Bitmap bmp in positives)
				if (bmp.Height != height)
					return false;

			return true;
		}

		private static bool AllSameWidth(IEnumerable<Bitmap> positives)
		{
			int width = positives.First().Width;
			foreach (Bitmap bmp in positives)
				if (bmp.Width != width)
					return false;

			return true;
		}

		private static Bitmap MostFrequentRow(BackgroundValue bv, IEnumerable<Bitmap> positives)
		{
			if (!AllSameWidth(positives))
				return null;

			Dictionary<Bitmap, int> freqs = new Dictionary<Bitmap, int>();
			foreach (Bitmap bmp in positives)
			{
				Bitmap currRow;
				for (int row = bv.TopLeft.Height; row < bmp.Height - bv.BottomLeft.Height; row++)
				{
					currRow = Bitmap.Crop(bmp, bv.Left, row,  bmp.Width - bv.Left - bv.Right, 1);

					if (!freqs.ContainsKey(currRow))
						freqs.Add(currRow, 1);
					else
						freqs[currRow] =  freqs[currRow] + 1;
				}
			}

			int max = int.MaxValue;
			Bitmap mostFrequent = null;

			foreach(KeyValuePair<Bitmap, int> pair in freqs){

				if(max < pair.Value)
				{
					mostFrequent = pair.Key;
					max = pair.Value;
				}
			}

			return mostFrequent;
		}

		private static Bitmap MostFrequentPixel(BackgroundValue bp, IEnumerable<Bitmap> positives)
		{
			Dictionary<int, int> frequencies = new Dictionary<int,int>();

			foreach (Bitmap pos in positives)
			{
				BoundingBox inside = new BoundingBox(bp.TopLeft.Width, bp.Top, pos.Width - bp.TopRight.Width - bp.TopLeft.Width, pos.Height - bp.Top - bp.Bottom);
				GetPixelFrequencies(frequencies, inside, pos);

				inside = new BoundingBox(bp.Left, bp.TopLeft.Height, bp.TopLeft.Width - bp.Left, pos.Height - bp.TopLeft.Height - bp.TopRight.Height);
				GetPixelFrequencies(frequencies, inside, pos);

				inside = new BoundingBox(pos.Width - bp.Right, bp.TopRight.Height, bp.TopRight.Width - bp.Right, pos.Height - bp.TopRight.Height - bp.BottomRight.Height);
				GetPixelFrequencies(frequencies, inside, pos);
			}

			int max = int.MinValue;
			int mostFrequent = 0;

			foreach(KeyValuePair<int, int> pair in frequencies){

				if(max < pair.Value)
				{
					mostFrequent = pair.Key;
					max = pair.Value;
				}
			}


			return Bitmap.FromPixels(1, 1, new int[] { mostFrequent });
		}

		private static void GetPixelFrequencies(Dictionary<int,int> frequencies, IBoundingBox bb, Bitmap bmp)
		{

			for (int row = bb.Top; row < bb.Top + bb.Height; row++)
			{
				for (int col = bb.Left; col < bb.Left + bb.Width; col++)
				{
								int value = bmp[row, col];
					if (frequencies.ContainsKey(value))
						frequencies[value] = frequencies[value] + 1;
					else
						frequencies.Add(value, 1);
				}
			}


		}

		private static Region GetHorizontal(bool top, int left, int right, int height, IEnumerable<Bitmap> positives)
		{
			Bitmap prev = null;
			foreach (Bitmap example in positives)
			{
				int toploc = 0;
				if (!top)
					toploc = example.Height - height;

				Bitmap pattern = null;
				if (height > 0)
				{
					pattern = HorizontalPatternMatcher.ShortestPattern(example, toploc, left, example.Width - right - 1, height);
				}

				if (pattern == null || (prev != null && !prev.Equals(pattern)))
					return null;

				prev = pattern;
			}

			return new Region("horizontal", prev);
		}

		private static Region GetVertical(bool left, int top, int bottom, int width, IEnumerable<Bitmap> positives)
		{
			Bitmap prev = null;
			foreach (Bitmap example in positives)
			{
				int leftloc = 0;
				if (!left)
					leftloc = example.Width - width;

				Bitmap pattern = null;
				if (width > 0)
				{
					pattern = VerticalPatternMatcher.ShortestPattern(example, leftloc, top, example.Height - bottom - 1, width);
				}

				if (pattern == null || (prev != null && !prev.Equals(pattern)))
					return null;

				prev = pattern;
			}

			return new Region("vertical", prev);
		}

		private static BackgroundValue GetBackgroundValue(String value, Dictionary <string, Part> assignment)
		{
			BackgroundValue bv = new BackgroundValue();
			bv.Type = value;
						bv.TopLeft = (Size)assignment["corner"].AssignedValue;
						bv.TopRight = (Size)assignment["corner"].AssignedValue;
						bv.BottomLeft = (Size)assignment["corner"].AssignedValue;
						bv.BottomRight = (Size)assignment["corner"].AssignedValue;
						bv.Top = ((RegionParameters)assignment["top"].AssignedValue).Depth;
						bv.Left = ((RegionParameters)assignment["left"].AssignedValue).Depth;
						bv.Bottom = ((RegionParameters)assignment["bottom"].AssignedValue).Depth;
						bv.Right = ((RegionParameters)assignment["right"].AssignedValue).Depth;

			return bv;
		}

		public static object CropFromManyRelativeTopLeft(int top, int left, int width, int height, IEnumerable<Bitmap> examples) {
			List<Bitmap> cropped = new List<Bitmap>();

			foreach (Bitmap example in examples)
			{
				if (width > 0 && height > 0)
				{
					cropped.Add(Bitmap.Crop(example, left, top, width, height));
				}
			}

			if (cropped.Count == 0)
			{
				return null; 
			}

			return Utils.CombineBitmapsAndMakeDifferencesTransparent(cropped);
		}

		public static object CropFromManyRelativeBottomLeft(int frombottom, int left, int width, int height, IEnumerable<Bitmap> examples) {
			List<Bitmap> cropped = new List<Bitmap>();
			foreach (Bitmap example in examples)
			{
				if (height > 0 && width > 0)
				{
					cropped.Add(Bitmap.Crop(example, left, example.Height - frombottom, width, height));
				}
			}

			if (cropped.Count == 0)
			{
				return null;
			}

			return Utils.CombineBitmapsAndMakeDifferencesTransparent(cropped);
		}

		public static object CropFromManyRelativeTopRight(int top, int fromright, int width, int height, IEnumerable<Bitmap> examples) {

			List<Bitmap> cropped = new List<Bitmap>();
			foreach (Bitmap example in examples)
			{
				if (height > 0 && width > 0)
				{
					cropped.Add(Bitmap.Crop(example, example.Width - fromright, top, width, height));
				}
			}

			if (cropped.Count == 0)
			{
				return null;
			}

			return Utils.CombineBitmapsAndMakeDifferencesTransparent(cropped);
		}

		public static object CropFromManyRelativeBottomRight(int frombottom, int fromright, int width, int height, IEnumerable<Bitmap> examples) {
			List<Bitmap> cropped = new List<Bitmap>();
			foreach (Bitmap example in examples)
			{
				if (height > 0 && width > 0)
				{
					cropped.Add(Bitmap.Crop(example, example.Width - fromright, example.Height - frombottom, width, height));
				}
			}

			if (cropped.Count == 0)
			{
				return null;
			}

			return Utils.CombineBitmapsAndMakeDifferencesTransparent(cropped);

		}

	}
}


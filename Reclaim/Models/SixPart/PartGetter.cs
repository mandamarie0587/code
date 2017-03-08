using System;
using System.Collections.Generic;
using Prefab;
using System.Collections;
using PrefabIdentificationLayers.Models;
using PrefabIdentificationLayers;

namespace Reclaim.Models.SixPart
{
	internal class PartGetter : IPartGetter, IConstraintGetter
	{
		int c_minCornerSize = 1;

		public static readonly PartGetter Instance = new PartGetter();
		private PartGetter() { }

		#region Parts
		public Dictionary<string, Part> GetParts(IEnumerable<Bitmap> positives, IEnumerable<Bitmap> negatives)
		{
			Dictionary<string, Part> parts = new Dictionary<string, Part>();

			List<string> edgetypes = new List<string>() { "repeating" };
			ArrayList interiortypes = new ArrayList { "horizontal", "vertical", "single" };

			int smallestWidth = int.MaxValue;
			int smallestHeight = int.MaxValue;
			int largestWidth = -1;
			int largestHeight = -1;

			foreach (Bitmap example in positives)
			{
				if (example.Width < smallestWidth)
				{
					smallestWidth = example.Width;
				}

				if (example.Height < smallestHeight)
				{
					smallestHeight = example.Height;
				}

				if (example.Width > largestWidth)
				{
					largestWidth = example.Width;
				}

				if (example.Height > largestHeight)
				{
					largestHeight = example.Height;
				}
			}

			smallestHeight -= 2;
			smallestWidth -= 2;

			int maxCornerSize = GetMaxCornerSize(largestHeight, largestWidth);
			//int maxCornerSize = 6;
			RegionParameters minhoriz = new RegionParameters(null, c_minCornerSize, c_minCornerSize, 1);
			RegionParameters maxhoriz = new RegionParameters(null, maxCornerSize, maxCornerSize, maxCornerSize);
			RegionParameters minvert = new RegionParameters(null, c_minCornerSize, c_minCornerSize, 1);
			RegionParameters maxvert = new RegionParameters(null, maxCornerSize, maxCornerSize, maxCornerSize);

			maxhoriz.Depth = (int)Math.Min(smallestHeight / 2, maxhoriz.Depth);
			maxhoriz.Start = (int)Math.Min(smallestWidth / 2, maxhoriz.Start);
			maxhoriz.End = (int)Math.Min(smallestWidth / 2, maxhoriz.End);
			maxvert.Depth = (int)Math.Min(smallestWidth / 2, maxvert.Depth);
			maxvert.Start = (int)Math.Min(smallestHeight / 2, maxvert.Start);
			maxvert.End = (int)Math.Min(smallestHeight / 2, maxvert.End);

			parts.Add("corner", new Part(GetCornerValues(smallestWidth, smallestHeight, maxCornerSize)));

			parts.Add("top", new Part(GetEdgeValues(edgetypes, minhoriz, maxhoriz)));
			parts.Add("bottom", new Part(GetEdgeValues(edgetypes, minhoriz, maxhoriz)));
			parts.Add("left", new Part(GetEdgeValues(edgetypes, minvert, maxvert)));
			parts.Add("right", new Part(GetEdgeValues(edgetypes, minvert, maxvert)));

			parts.Add("interior", new Part(interiortypes));

			return parts;
		}

		private ArrayList GetCornerValues(int smallestWidth, int smallestHeight, int maxCornerSize)
		{
			Size max = new Size(maxCornerSize, maxCornerSize);
			Size min = new Size(c_minCornerSize, c_minCornerSize);

			GetMaxSize(max, smallestWidth, smallestHeight);

			ArrayList values = new ArrayList();
			for (int height = min.Height; height <= max.Height; height++)
			{
				for (int width = min.Width; width <= max.Width; width++)
				{
					// Restrict all width and height for corner values to be the same. 
					if (width == height)
					{
						object value = new Size(width, height);
						values.Add(value);
					}
				}

			}
			return values;
		}

		private ArrayList GetEdgeValues(List<string> types, RegionParameters min, RegionParameters max)
		{
			ArrayList values = new ArrayList();
			foreach (string type in types)
			{
				for (int depth = min.Depth; depth <= max.Depth; depth++)
				{
					for (int left = min.Start; left <= max.Start; left++)
					{
						for (int right = min.End; right <= max.End; right++)
						{
							object value = new RegionParameters(type, left, right, depth);
							values.Add(value);
						}
					}
				}
			}

			return values;
		}

		private Size GetMaxSize(Size currSize, int smallestWidth, int smallestHeight)
		{
			int width = (int)Math.Min(smallestWidth / 2, currSize.Width);
			int height = (int)Math.Min(smallestHeight / 2, currSize.Height);
			return new Size(width, height);
		}

		// Get the maximum corner size based on the height and width of the rectangle. This assumes that the height and width of corner dimensions are the same
		private int GetMaxCornerSize(int maxHeight, int maxWidth)
		{
			// Find the smaller side because that will determine the maximum corner radius
			int smallerSide = maxWidth;
			if (maxHeight < maxWidth)
			{
				smallerSide = maxHeight;
			}

			return smallerSide / 4;
		}


		#endregion

		#region Constraints


		public IEnumerable<Constraint> GetConstraints(Dictionary<string, Part> parts, IEnumerable<Bitmap> positives, IEnumerable<Bitmap> negatives)
		{
			List<Constraint> constraints = new List<Constraint>();

			// Same Edge Depth of top and bottom
			Constraint topSymmetricToBottom = new Constraint(SameEdgeDepth, parts["top"], parts["bottom"]);
			constraints.Add(topSymmetricToBottom);

			// Same Edge Depth of left and right
			Constraint leftSymmetricToRight = new Constraint(SameEdgeDepth, parts["left"], parts["right"]);
			constraints.Add(leftSymmetricToRight);

			// Width
			Constraint cornerAdjacentToTopRegion = new Constraint(RegionStartEqualsWidth, parts["top"], parts["corner"]);
			constraints.Add(cornerAdjacentToTopRegion);

			Constraint cornerAdjacentToBottomRegion = new Constraint(RegionStartEqualsWidth, parts["bottom"], parts["corner"]);
			constraints.Add(cornerAdjacentToBottomRegion);

			// Height
			Constraint cornerAdjacentToLeftRegion = new Constraint(RegionStartEqualsHeight, parts["left"], parts["corner"]);
			constraints.Add(cornerAdjacentToLeftRegion);

			Constraint cornerAdjacentToRightRegion = new Constraint(RegionStartEqualsHeight, parts["right"], parts["corner"]);
			constraints.Add(cornerAdjacentToRightRegion);

			Constraint cornerIsSquare = new Constraint(FeatureIsSquare, parts["corner"], parts["corner"]);
			constraints.Add(cornerIsSquare);

			// Ensure the corner height is > than the height of the border region
			Constraint cornerIsLessThanOrEqualToHeight = new Constraint(DepthIsSmallHeight, parts["top"], parts["corner"]);
			constraints.Add(cornerIsLessThanOrEqualToHeight);

			Constraint cornerIsLessThanOrEqualToWidth = new Constraint(DepthIsSmallWidth, parts["left"], parts["corner"]);
			constraints.Add(cornerIsLessThanOrEqualToWidth);

			Constraint topStartEqualsEnd = new Constraint(RegionStartEqualsEnd, parts["top"], parts["top"]);
			constraints.Add(topStartEqualsEnd);

			Constraint bottomStartEqualsEnd = new Constraint(RegionStartEqualsEnd, parts["bottom"], parts["bottom"]);
			constraints.Add(bottomStartEqualsEnd);

			Constraint leftStartEqualsEnd = new Constraint(RegionStartEqualsEnd, parts["left"], parts["left"]);
			constraints.Add(leftStartEqualsEnd);

			Constraint rightStartEqualsEnd = new Constraint(RegionStartEqualsEnd, parts["right"], parts["right"]);
			constraints.Add(rightStartEqualsEnd);

			return constraints;
		}

		public static Tuple<Size, RegionParameters> GetFromValues(object v1, object v2)
		{
			Size size = v1 as Size;
			RegionParameters parameters = v2 as RegionParameters;
			if (size == null)
			{
				size = v2 as Size;
				parameters = v1 as RegionParameters;
			}

			return new Tuple<Size, RegionParameters>(size, parameters);
		}

		private static Tuple<Utils.RectData, Size> GetFomValuesContentEdge(object v1, object v2)
		{
			Utils.RectData content = v1 as Utils.RectData;
			Size feature = v2 as Size;

			if (content == null)
			{
				content = v2 as Utils.RectData;
				feature = v1 as Size;
			}


			return new Tuple<Utils.RectData, Size>(content, feature);
		}

		public static bool DepthIsSmallHeight(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return size.Height == 0 || parameters.Depth <= size.Height;
		}

		public static bool SameEdgeDepth(object v1, object v2)
		{
			return ((RegionParameters)v1).Depth == ((RegionParameters)v2).Depth;
		}

		public static bool DepthIsSmallWidth(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return size.Width == 0 || parameters.Depth <= size.Width;
		}

		public static bool PartsAreSameSizeOrZero(object v1, object v2)
		{
			Size s1 = (Size)v1;
			Size s2 = (Size)v2;

			if (s1.Height == 0 || s2.Height == 0 || s1.Width == 0 || s2.Width == 0)
				return true;

			return s1.Equals(s2);
		}

		public static bool RegionStartEqualsEnd(object v1, object v2)
		{
			RegionParameters r1 = (RegionParameters)v1;

			return r1.Start == 0 || r1.End == 0 || r1.Start == r1.End;
		}

		public static bool RegionStartEqualsWidth(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return parameters.Start == size.Width;
		}

		public static bool RegionEndEqualsWidth(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return parameters.End == size.Width;
		}

		public static bool RegionStartEqualsHeight(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size rect = (Size)pair.Item1;
			RegionParameters parameters = pair.Item2;

			return rect.Height == parameters.Start;
		}

		public static bool RegionEndEqualsHeight(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return size.Height == parameters.End;
		}

		public static bool RegionDepthEqualsHeight(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return size.Height == parameters.Depth;

		}

		public static bool RegionDepthEqualsWidth(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return size.Width == parameters.Depth;
		}

		public static bool RegionDepthLessThanHeight(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return parameters.Depth < size.Height;
		}

		public static bool RegionDepthLessThanWidth(object v1, object v2)
		{
			Tuple<Size, RegionParameters> pair = GetFromValues(v1, v2);
			Size size = pair.Item1;
			RegionParameters parameters = pair.Item2;

			return parameters.Depth < size.Width;
		}

		public static bool FeatureIsSquare(object v1, object v2)
		{
			Size size = (Size)v1;
			return size.Width == size.Height;
		}

		public static bool IgnoreValueEqual(object v1, object v2)
		{
			Int32 value1 = (Int32)v1;
			Int32 value2 = (Int32)v2;
			return value1.Equals(value2);
		}

		private static bool ContentLeftAligned(object v1, object v2)
		{
			Tuple<Utils.RectData, Size> pair = GetFomValuesContentEdge(v1, v2);
			Utils.RectData contentParam = pair.Item1;
			Size featureParam = pair.Item2;

			return featureParam.Width == contentParam.Left;
		}

		private bool ContentTopAligned(object v1, object v2)
		{
			Tuple<Utils.RectData, Size> pair = GetFomValuesContentEdge(v1, v2);
			Utils.RectData contentParam = pair.Item1;
			Size featureParam = pair.Item2;

			return featureParam.Height == contentParam.Top;
		}

		private bool ContentBottomAligned(object v1, object v2)
		{
			Tuple<Utils.RectData, Size> pair = GetFomValuesContentEdge(v1, v2);
			Utils.RectData contentParam = pair.Item1;
			Size featureParam = pair.Item2;

			return featureParam.Height == contentParam.Bottom;
		}

		private bool ContentRightAligned(object v1, object v2)
		{
			Tuple<Utils.RectData, Size> pair = GetFomValuesContentEdge(v1, v2);
			Utils.RectData contentParam = pair.Item1;
			Size featureParam = pair.Item2;

			return featureParam.Width == contentParam.Right;
		}

		#endregion
	}
}
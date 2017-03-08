using PrefabIdentificationLayers.Models;
using PrefabModel = PrefabIdentificationLayers.Models.NinePart;
using SixPartModel = Reclaim.Models.SixPart;
using LineModel = Reclaim.Models.Line; 

namespace Reclaim.Models
{
	public class ModelInstances
	{      
		private ModelInstances(){}

		public static readonly Model SixPart = new Model( "sixpart",
			new SearchPtypeBuilder("sixpart",
		        SixPartModel.CostFunction.Instance,
		        SixPartModel.PartGetter.Instance, //parts
		        SixPartModel.PartGetter.Instance, //constraints
				NextPartSelecter.Instance, //how to select parts in search
		        SixPartModel.Builder.Instance), //how to build a ptype from full assignment
			PrefabModel.Finder.Instance); //how to find ptype occurrences

		public static readonly Model Line = new Model("line",
			new SearchPtypeBuilder("line",
				LineModel.CostFunction.Instance,
				LineModel.PartGetter.Instance, //parts
		        LineModel.PartGetter.Instance, //constraints
				NextPartSelecter.Instance, //how to select parts in search
				LineModel.Builder.Instance), //how to build a ptype from full assignment
			PrefabModel.Finder.Instance); //how to find ptype occurrences

		public static readonly Model NinePart = new Model("ninepart",
			new SearchPtypeBuilder("ninepart",
		        PrefabModel.CostFunction.Instance,
		        PrefabModel.PartGetter.Instance, //parts
		        PrefabModel.PartGetter.Instance, //constraints
				NextPartSelecter.Instance, //how to select parts in search
		         PrefabModel.Builder.Instance), //how to build a ptype from full assignment
			PrefabModel.Finder.Instance); //how to find ptype occurrences


		public static readonly Model[] All = { SixPart, Line, NinePart };

		public static Model Get(string name){
			foreach(Model m in All)
				if(m.Name.Equals(name))
					return m;

			return null;
		}
	}
}


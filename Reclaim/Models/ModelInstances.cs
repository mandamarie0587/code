using Reclaim.Models.NinePart;
using PrefabIdentificationLayers.Models;
using PrefabModel = PrefabIdentificationLayers.Models.NinePart;
using ReclaimModel = Reclaim.Models.SixPart; 

namespace Reclaim.Models
{
	public class ModelInstances
	{      
		private ModelInstances(){}

		public static readonly Model SixPart = new Model( "sixpart",
			new SearchPtypeBuilder("sixpart",
		        ReclaimModel.CostFunction.Instance,
				PartGetter.Instance, //parts
				PartGetter.Instance, //constraints
				NextPartSelecter.Instance, //how to select parts in search
		        ReclaimModel.Builder.Instance), //how to build a ptype from full assignment
			PrefabModel.Finder.Instance); //how to find ptype occurrences

		public static readonly Model[] All = { SixPart };

		public static Model Get(string name){
			foreach(Model m in All)
				if(m.Name.Equals(name))
					return m;

			return null;
		}
	}
}


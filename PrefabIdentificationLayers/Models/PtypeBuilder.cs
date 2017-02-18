using System;
using PrefabIdentificationLayers.Prototypes;
using PrefabIdentificationLayers.Models; 
namespace PrefabIdentificationLayers.Models
{
	public interface PtypeBuilder
	{
		Ptype.Mutable BuildPrototype(IBuildPrototypeArgs args);
	}
}


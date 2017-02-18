
/**
 * Created with IntelliJ IDEA.
 * User: mdixon
 * Date: 9/23/13
 * Time: 10:14 PM
 * To change this template use File | Settings | File Templates.
 */
using PrefabIdentificationLayers;


namespace PrefabIdentificationLayers.Models{
	public class BuildPrototypeArgs : IBuildPrototypeArgs {

		private Examples examples;
		public BuildPrototypeArgs(Examples examples, Model model, string id)
		{
			// Model will create a prototype
			// 9 part model will generate a 9-part prototype
			// Give it an example
			// It will create a prototype for you 
			// Need to specifiy the model you want to build from 
			// Use the Nine Part model for now
			// Take a simple rectangle and build a nine part model 
			this.Id = id;
			this.examples = examples;
			this.Model = model;
		}

		/// <summary>
		/// The positive examples used to build the prototype.
		/// </summary>

		public Examples Examples{
			get{
				return examples;
			}
		}


		/// <summary>
		/// The model.
		/// </summary>
		public readonly Model Model;


		/// <summary>
		/// The Guid to assign to the built prototype.
		/// </summary>
		public readonly string Id;


	}
}

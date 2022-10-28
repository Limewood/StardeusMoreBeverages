using MoreBeverages;
using System;
using System.Collections.Generic;
using Game.Constants;
using Game.Data;
using Game.AI.Needs;
using Game.AI.Recreation;
using Game.AI.Traits;
using KL.Collections;
using KL.Randomness;
using UnityEngine;

namespace MoreBeverages.AI.Traits
{
	public class TraitTeaAddict : TraitBeverageAddict
	{
		public const string Id = "beverage_addict_tea";

		protected override string Icon => "Icons/Color/TeaLeaves";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			Trait.Add("beverage_addict_tea", new TraitTeaAddict());
		}

		public TraitTeaAddict()
		{
			base.Incompatible = new HashSet<string> { "beverage_addict_coffee" };
			base.BeverageOfChoice = "Tea";
		}
	}
}

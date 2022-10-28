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
	public class TraitCoffeeAddict : TraitBeverageAddict
	{
		public const string Id = "beverage_addict_coffee";

		protected override string Icon => "Icons/Color/CoffeeCup";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			Trait.Add("beverage_addict_coffee", new TraitCoffeeAddict());
		}

		public TraitCoffeeAddict()
		{
			// TODO Make more flexible
			base.Incompatible = new HashSet<string> { "beverage_addict_tea" };
			base.BeverageOfChoice = "Coffee";
		}
	}
}

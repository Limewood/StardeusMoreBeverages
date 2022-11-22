using MoreBeverages;
using MoreBeverages.AI.Traits;
using System;
using System.Collections.Generic;
using Game.Constants;
using Game.Data;
using Game.Utils;
using Game.AI;
using Game.AI.Recreation;
using Game.AI.Traits;
using KL.Randomness;
using KL.Utils;
using UnityEngine;

namespace MoreBeverages.AI.Recreation {
    public sealed class RecConsumeBeverage : RecreationActivity
	{
		public static string DrinkBeverage = "Drink beverage";
		
		public const string Id = "RecConsumeBeverage";

		protected override string Icon => "Icons/Color/CoffeeCup";

		protected override string Title => DrinkBeverage;

		protected override string ActionType => "Drink";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			RecreationActivity.Add("RecConsumeBeverage", new RecConsumeBeverage());
			// TODO How to remove RecConsumeCoffee?
		}

		public static void Init() {
			DrinkBeverage = "beverage.drink".T();
		}

		protected override void PostProcessAd(Advert ad)
		{
			Being being = ad.S.FindEntity<Being>(ad.EntityId);
			string drinkType;
			TraitBeverageAddict trait = GetBeverageAddictionTrait(being);
			if (trait != null)
			{
				drinkType = trait.BeverageOfChoice;
			} else {
				List<MatType> group = MatType.GetGroup(MatGroup.Drink);
				drinkType = ad.S.Rng.From(group).Id;
			}
			ad.Vars.SetString("DrinkType", drinkType);
			ad.Vars.SetStringSet("Effects", new string[4] { "Sleep", "Toilet", "Fun", "Rest" });
			ad.Vars.SetFloatSet("Effects", new float[4] { 1f, -1f, trait != null ? 2f : 1f, 3f });
			being.Mood.AddEffect(MoodEffect.Create(being.S.Ticks, MoodEffect.Duration2h, MoreBeveragesMod.HadFavouriteBeverage(MatType.Get(drinkType)), 3));
		}

		public override bool IsAvailableFor(Being being, out float priority)
		{
			priority = 0f;
			if (being.Persona.Species.Type != SpeciesType.Human)
			{
				return false;
			}
			TraitBeverageAddict addictionTrait = GetBeverageAddictionTrait(being);
			bool isBeverageAddict = addictionTrait != null;
			if (isBeverageAddict)
			{
				MatType drinkMat = MatType.Get(addictionTrait.BeverageOfChoice);
				if (!being.S.Sys.Inventory.HasAny(drinkMat))
				{
					return false;
				}
			}
			else
			{
				if (!being.S.Sys.Inventory.HasAnyOf(MatType.GetGroup(MatGroup.Drink)))
				{
					return false;
				}
			}
			
			float num = being.Needs.GetNeed(NeedId.Sleep).Demand;
			if (being.Needs.TryGetNeed(NeedId.Rest, out var need))
			{
				num = Math.Max(num, need.Demand);
			}
			priority = 1f - (isBeverageAddict ? num / 2f : num);
			return true;
		}

		public TraitBeverageAddict GetBeverageAddictionTrait(Being being)
		{
			foreach (Trait trait in being.Traits.Traits.Values) {
				if (trait is TraitBeverageAddict) {
					return trait as TraitBeverageAddict;
				}
			}
			return null;
		}
	}
}

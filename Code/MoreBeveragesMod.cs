using MoreBeverages.AI.Actions;
using MoreBeverages.AI.Recreation;
using MoreBeverages.AI.Traits;
using System.Collections.Generic;
using Game;
using Game.Constants;
using Game.Data;
using Game.Utils;
using Game.AI.Recreation;
using Game.AI.Actions;
using Game.AI.Traits;
using Game.Systems;
using KL.Utils;
using UnityEngine;
// Temporary imports
using System;
using System.Reflection;

namespace MoreBeverages {
	public sealed class MoreBeveragesMod {
		public static string NoBeverage(MatType drinkMat) {
			return "beverage.lack".T(drinkMat.NameT);
		}

		public static string NoMorningBeverage(MatType drinkMat) {
			return "beverage.lack.morning".T(drinkMat.NameT);
		}

		public static string HadFavouriteBeverage(MatType drinkMat) {
			return "beverage.drank".T(drinkMat.NameT);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register() {
			The.SysSig.GameIdChanged.AddListener((_) => OnInitialize());
		}

		private static void OnInitialize() {
			ActSleepWithSignal.BeingWokeUp.AddListener(OnBeingWokeUp);
			// Temporary fix until overriding strings works
			Type t = typeof(Game.Utils.Translations);
        	FieldInfo field = t.GetField("fallback", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
			Dictionary<int, string> fallback = (Dictionary<int, string>) field.GetValue(null);
			fallback[Animator.StringToHash("objects.devices.coffeemaker")] = "objects.devices.beveragemaker".T();
			fallback[Animator.StringToHash("objects.devices.coffeemaker.desc")] = "objects.devices.beveragemaker.desc".T();
			fallback[Animator.StringToHash("research.coffee")] = "research.beverages".T();
			RecConsumeBeverage.Init();
		}

		private static void OnBeingWokeUp(Being worker) {
			if (worker.Persona.Species.IsHumanoid) {
				float t = Mathf.InverseLerp(100f, 20f, worker.Needs.GetNeed(NeedId.Sleep).Value);
				float v = Mathf.Lerp(0.2f, 1f, t);
				if (worker.S.Rng.Chance(v)) {
					RecConsumeBeverage recreationActivity = (RecConsumeBeverage) RecreationActivity.Find("RecConsumeBeverage");
					if (recreationActivity.IsAvailableFor(worker, out var _)) {
						recreationActivity.ExecuteFor(worker, immediately: false);
					}
					else {
						TraitBeverageAddict trait = recreationActivity.GetBeverageAddictionTrait(worker);
						int moodChange;
						string beverage;
						if (trait != null) {
							moodChange = -6;
							beverage = trait.BeverageOfChoice;
						} else {
							moodChange = -3;
							List<MatType> group = MatType.GetGroup(MatGroup.Drink);
							beverage = worker.S.Rng.From(group).Id;
						}
						MatType bevType = MatType.Get(beverage);
						worker.Mood.AddEffect(MoodEffect.Create(worker.S.Ticks, MoodEffect.Duration4h, NoMorningBeverage(bevType), moodChange));
					}
				}
			}
		}
	}
}

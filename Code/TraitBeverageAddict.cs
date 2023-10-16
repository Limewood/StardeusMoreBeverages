using MoreBeverages;
using System;
using System.Collections.Generic;
using Game.Constants;
using Game.Data;
using Game.AI;
using Game.AI.Needs;
using Game.AI.Recreation;
using Game.AI.Traits;
using Game.Utils;
using KL.Collections;
using KL.Randomness;
using KL.Utils;
using UnityEngine;

namespace MoreBeverages.AI.Traits
{
	public abstract class TraitBeverageAddict : Trait, ITickableTrait
	{
		private const int beverageSeekDelay = 540; // ~3 hours
		
		public string BeverageOfChoice { get; protected set; }

		private readonly Dictionary<int, long> ticksSinceLastBeverage = new Dictionary<int, long>();

		public override float Rarity => 0.15f;

		public override void Reset()
		{
			ticksSinceLastBeverage.Clear();
		}

		public override bool IsCompatibleWith(Being being)
		{
			return being.Persona.Species.Type == SpeciesType.Human;
		}

		public override void ApplyAfterCreate(Being being)
		{
		}

		private bool MaybeGetBeverage(Being being, long ticks)
		{
			if (being.Needs.HasUrgentNeeds)
			{
				return false;
			}
			if (being.Brain.CurrentAd != null)
			{
				if (being.Brain.CurrentAd.JobTypeHash == JobType.HRecreation)
				{
					if (being.Brain.CurrentAd.CurrentAction.TypeHash == Animator.StringToHash("Drink")) {
						ticksSinceLastBeverage[being.Id] = ticks;
					}
					return false;
				}
				// Always a change to not want a beverage if doing something else
				if (being.S.Rng.Chance(0.5f))
				{
					return false;
				}
			}
			int delay = beverageSeekDelay;
			long savedTicks = 0;
			if (ticksSinceLastBeverage.TryGetValue(being.Id, out var value)) {
				savedTicks = value;
			}
			if (savedTicks == 0L)
			{
				savedTicks = ticks + Rng.URange(0, delay);
				ticksSinceLastBeverage[being.Id] = savedTicks;
			}
			if (ticks > savedTicks + delay)
			{
				Need sleepNeed = being.Needs.GetNeed(NeedIdH.Sleep);
				float t = Mathf.InverseLerp(100f, 20f, sleepNeed.Value);
				float v = Mathf.Lerp(0.2f, 1f, t);
				if (being.S.Rng.Chance(v))
				{
					ticksSinceLastBeverage[being.Id] = ticks;
					GetBeverage(being);
					return true;
				}
				return false;
			}
			return false;
		}

		public void GetBeverage(Being being)
		{
			RecreationActivity recreationActivity = RecreationActivity.Find("RecConsumeBeverage");
			if (recreationActivity.IsAvailableFor(being, out var _))
			{
				if (being.Brain.CurrentAd != null)
				{
					being.Brain.DropCurrentAdAsImpossible("beverage.addict".T(BeverageOfChoice));
				}
				recreationActivity.ExecuteFor(being, immediately: false);
			}
			else
			{
				MatType bevType = MatType.Get(BeverageOfChoice);
				being.Mood.AddEffect(MoodEffect.Create(being.S.Ticks, MoodEffect.Duration2h,
					MoreBeveragesMod.NoBeverage(bevType), -2));
			}
		}

		public void Tick(long ticks, List<Being> beingsWithThisTrait)
		{
			for (int i = 0; i < beingsWithThisTrait.Count; i++)
			{
				Being being = beingsWithThisTrait[i];
				if (Trait.CanTick(being))
				{
					MaybeGetBeverage(being, ticks);
				}
			}
		}
	}
}

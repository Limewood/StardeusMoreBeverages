using MoreBeverages;
using MoreBeverages.Utils;
using System;
using System.Collections.Generic;
using Game;
using Game.AI;
using Game.AI.Actions;
using Game.AI.Needs;
using Game.AI.Recreation;
using Game.AI.Traits;
using Game.Components;
using Game.Constants;
using Game.Data;
using Game.Data.Relationships;
using Game.Systems;
using KL.Randomness;
using KL.Signals;
using KL.Utils;
using UnityEngine;

namespace MoreBeverages.AI.Actions
{
	public sealed class ActSleepWithSignal : ActionExecutor
	{
		public const string ActType = "Sleep";

		public static readonly int TypeHash = Animator.StringToHash("Sleep");

		public static Signal1<Being> BeingWokeUp = new Signal1<Being>("BeingWokeUp");

		private ActSleep sleepAction;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			ActionExecutor.RegisterExecutor("Sleep", (Advert ad, Being being) => new ActSleepWithSignal(ad, being));
		}

		public ActSleepWithSignal(Advert ad, Being worker)
			: base(ad, worker)
		{
			initAction(ad, worker);
		}

		public override void OnSave()
		{
			sleepAction.OnSave();
		}

		private void initAction(Advert ad, Being worker) {
			if (sleepAction == null) {
				sleepAction = new ActSleep(ad, worker);
			}
		}

		protected override void OnLoad(ComponentData data)
		{
			// Need to call this here since the virtual method OnLoad is called in the base constructor
			initAction(ad, worker);
			ReflectionUtils.CallMethod(typeof(ActSleep), sleepAction, "OnLoad", new object[]{data});
		}

		public override void Cleanup()
		{
			Type type = typeof(ActSleep);
			bool isSleepComplete = (bool) ReflectionUtils.GetValue(type, sleepAction, "isSleepComplete");
			float sleepNeedBefore = (float) ReflectionUtils.GetValue(type, sleepAction, "sleepNeedBefore");
			if (!isSleepComplete && sleepNeedBefore < worker.Needs.GetNeed(NeedIdH.Sleep).Value - 3f)
			{
				ReflectionUtils.CallMethod(typeof(ActSleep), sleepAction, "ApplySleepInterrupted");
			}
			worker.Brain.IsUnconscious = false;
			bool isEmergencySleep = (bool) ReflectionUtils.GetValue(type, sleepAction, "isEmergencySleep");
			if (isEmergencySleep)
			{
				isEmergencySleep = false;
				ReflectionUtils.CallMethod(typeof(ActSleep), sleepAction, "ApplyNoBedPenalty");
			}
			Need sleepNeed = (Need) ReflectionUtils.GetValue(type, sleepAction, "sleepNeed");
			Need toiletNeed = (Need) ReflectionUtils.GetValue(type, sleepAction, "toiletNeed");
			Need hungerNeed = (Need) ReflectionUtils.GetValue(type, sleepAction, "hungerNeed");
			Need restNeed = (Need) ReflectionUtils.GetValue(type, sleepAction, "restNeed");
			if (sleepNeed != null)
			{
				sleepNeed.DropMultiplier = 1f;
				toiletNeed.DropMultiplier = 1f;
				hungerNeed.DropMultiplier = 1f;
				restNeed.DropMultiplier = 1f;
				// Woke up
				BeingWokeUp.Send(worker);

				sleepNeed = null;
			}
			ISleepable targetBed = (ISleepable) ReflectionUtils.GetValue(type, sleepAction, "targetBed");
			Slot slot = (Slot) ReflectionUtils.GetValue(type, sleepAction, "slot");
			if (targetBed != null)
			{
				targetBed.Tile.Damageable.AddWear();
				if (targetBed.HideBeing)
				{
					worker.Graphics.SetVisible();
				}
				Slot ownedBed = (Slot) ReflectionUtils.GetValue(type, sleepAction, "ownedBed");
				Attachment ownedBedAtt = (Attachment) ReflectionUtils.GetValue(type, sleepAction, "ownedBedAtt");
				if (ownedBed == null)
				{
					int BedAttachmentType = (int) ReflectionUtils.GetPropertyValue(type, sleepAction, "BedAttachmentType");
					ownedBedAtt = Attachment.BeingToSlot(S.Ticks, worker, slot, BedAttachmentType, 0f, isOwnership: true);
					if (ownedBedAtt != null)
					{
						ownedBedAtt.IsOwnership = true;
						S.Sys.Att.Add(ownedBedAtt);
					}
				}
				else if (slot == ownedBed)
				{
					ownedBedAtt?.AddStrength(0.01f);
				}
				targetBed = null;
			}
			if (slot != null)
			{
				slot.Unreserve(worker);
				slot.Remove(worker);
				slot = null;
			}
			ParticlesSys.Data particles = (ParticlesSys.Data) ReflectionUtils.GetValue(type, sleepAction, "particles");
			if (particles != null)
			{
				S.Sys.Particles.RemoveParticles(particles);
				particles = null;
			}
			worker.Graphics.SetVertical(isVertical: true);
			worker.Graphics.SetOffset(Vector2.zero);
		}

		protected override ExecutionResult DoWork()
		{
			return (ExecutionResult) ReflectionUtils.CallMethod(typeof(ActSleep), sleepAction, "DoWork");
		}
	}
}

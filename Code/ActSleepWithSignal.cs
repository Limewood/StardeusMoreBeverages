using MoreBeverages;
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

		private bool hasReachedSleepSpot;

		private Slot slot;

		private ISleepable targetBed;

		private Attachment ownedBedAtt;

		private Slot ownedBed;

		private bool checkedForOwnedBed;

		private long lastTick;

		private float sleepNeedBefore;

		private bool isSleepComplete;

		private Need sleepNeed;

		private Need toiletNeed;

		private Need hungerNeed;

		private Need restNeed;

		private int emergencyWakeupAt;

		private bool isEmergencySleep;

		private bool isUsingBed;

		private int slotIdx = -1;

		private ParticlesSys.Data particles;

		private int showParticlesIn;

		private List<int> blacklistedBeds;

		private int BedAttachmentType
		{
			get
			{
				if (!worker.Persona.Species.IsPet)
				{
					return AttachmentType.PersonBed;
				}
				return AttachmentType.PetBed;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			ActionExecutor.RegisterExecutor("Sleep", (Advert ad, Being being) => new ActSleepWithSignal(ad, being));
		}

		public ActSleepWithSignal(Advert ad, Being worker)
			: base(ad, worker)
		{
		}

		public override void OnSave()
		{
			ComponentData vars = ad.Vars;
			vars.SetBool("ReachedSpot", hasReachedSleepSpot);
			vars.SetComponent("BedId", "BedComp", targetBed);
			vars.SetFloat("SleepBefore", sleepNeedBefore);
			vars.SetBool("EmergencySleep", isEmergencySleep);
			vars.SetInt("SlotIdx", slotIdx);
			vars.SetInt("SlotNum", slot?.Number ?? 0);
		}

		protected override void OnLoad(ComponentData data)
		{
			sleepNeedBefore = data.GetFloat("SleepBefore", 0f);
			hasReachedSleepSpot = data.GetBool("ReachedSpot", hasReachedSleepSpot);
			isEmergencySleep = data.GetBool("EmergencySleep", def: false);
			targetBed = data.GetComponent<ISleepable>(S, "BedId", "BedComp");
			ownedBedAtt = S.Sys.Att.BeingToTileFirst(worker.Id, BedAttachmentType);
			ownedBed = ownedBedAtt?.Slot;
			if (targetBed != null)
			{
				Entity entity = targetBed.Entity;
				for (int i = 0; i < entity.Components.Length; i++)
				{
					if (entity.Components[i] is ISleepable sleepable && sleepable.IsCompatibleWith(worker))
					{
						targetBed = sleepable;
						slotIdx = data.GetInt("SlotIdx", 0);
						int @int = data.GetInt("SlotNum", 0);
						slot = targetBed.GetSlot(slotIdx, @int);
						if (slot == null)
						{
							D.Err("Could not find slot {0} in bed with id: {1}", slotIdx, targetBed.EntityId);
							targetBed = null;
						}
						else if (!slot.Reserve(worker))
						{
							D.Err("Trying to reserve a bed during load, but it is reserved by someone else! {0}", this);
							targetBed = null;
						}
						break;
					}
				}
				if (targetBed != null)
				{
					if (hasReachedSleepSpot)
					{
						if (!UseBed())
						{
							targetBed = null;
						}
					}
					else
					{
						MoveToTarget();
					}
				}
			}
			if (isEmergencySleep)
			{
				EmergencySleep();
			}
		}

		public override void Cleanup()
		{
			if (!isSleepComplete && sleepNeedBefore < worker.Needs.GetNeed(NeedId.Sleep).Value - 3f)
			{
				ApplySleepInterrupted();
			}
			worker.Brain.IsUnconscious = false;
			if (isEmergencySleep)
			{
				isEmergencySleep = false;
				ApplyNoBedPenalty();
			}
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
			if (targetBed != null)
			{
				targetBed.Tile.Damageable.AddWear();
				if (targetBed.HideBeing)
				{
					worker.Graphics.SetVisible();
				}
				if (ownedBed == null)
				{
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
			if (sleepNeed == null)
			{
				if (worker.Needs == null)
				{
					D.Err("Worker has null needs in sleep act: {0}", worker);
					return Failure(T.AdRejUndefined);
				}
				InitializeNeeds();
				if (sleepNeed == null)
				{
					D.Err("Worker has no sleep need, but trying to do ActSleep! {0}", worker);
					return Failure(T.AdRejUndefined);
				}
				showParticlesIn = Mathf.RoundToInt(3f * Rng.URange(2f, 5f));
			}
			if (!isEmergencySleep && targetBed == null)
			{
				return DoRegularSleep();
			}
			if (subAction != null)
			{
				return RunSubAction();
			}
			DoSleep();
			_ = S.Ticks;
			_ = lastTick;
			if (isEmergencySleep)
			{
				if (emergencyWakeupAt == 0)
				{
					emergencyWakeupAt = S.Rng.Range(40, 60);
				}
				if (sleepNeed.Value > (float)emergencyWakeupAt)
				{
					ApplyPrematureWakeup();
					isSleepComplete = true;
					return Success;
				}
			}
			else if (hasReachedSleepSpot)
			{
				if (slot == null)
				{
					slot = targetBed?.Slots?.SafeGet(0);
				}
				if (!UseBed())
				{
					return Failure(T.AdRejCouldNotReserve);
				}
				ISleepable sleepable = targetBed;
				if (sleepable == null || !sleepable.Entity.IsActive)
				{
					ApplyPrematureWakeup();
					return Failure(T.AdRejInterrupted);
				}
				_ = sleepNeed.Value;
				if (sleepNeed.IsSatisfied)
				{
					ApplyBedBonus();
					isSleepComplete = true;
					return Success;
				}
			}
			return StillWorking;
		}

		private void DoSleep()
		{
			sleepNeed.DropMultiplier = 0f;
			toiletNeed.DropMultiplier = 0.25f;
			hungerNeed.DropMultiplier = 0.25f;
			restNeed.DropMultiplier = 2f;
			worker.Brain.IsUnconscious = true;
			sleepNeed.Add(5f / 72f);
			if (particles == null && showParticlesIn-- < 0)
			{
				particles = new ParticlesSys.Data
				{
					Owner = worker,
					IsEmitOnly = true,
					EmitChance = 1f,
					PrefabName = "SleepFX",
					Play = true
				};
				S.Sys.Particles.AddParticles(particles);
				worker.Brain.HideAllInfo();
			}
		}

		private ExecutionResult RunSubAction()
		{
			ExecutionResult result = subAction.Execute();
			if (result.IsFinished)
			{
				if (result.IsSuccess)
				{
					subAction = null;
					hasReachedSleepSpot = true;
					lastTick = S.Ticks;
					return StillWorking;
				}
				subAction = null;
				if (slot != null)
				{
					slot.Unreserve(worker);
					slot = null;
				}
				if (targetBed != null)
				{
					(blacklistedBeds ?? (blacklistedBeds = new List<int>())).Add(targetBed.EntityId);
					targetBed = null;
				}
				return StillWorking;
			}
			return result;
		}

		private ExecutionResult DoRegularSleep()
		{
			if (!Ready.AreasInitial)
			{
				return StillWorking;
			}
			int num = S.Sys.Areas.IslandAt(worker.PosIdx);
			if (num == 0)
			{
				if (Ready.Areas)
				{
					D.ErrSoft("Being is without an island! Pos {0} {1}", worker.PosIdx, worker);
				}
				int num2 = S.Sys.Areas.At(worker.PosIdx);
				if (num2 != 0)
				{
					D.Err("No island, but room exists: {0}", num2);
					return StillWorking;
				}
				D.ErrSoft("No room either!");
				num = S.Sys.Areas.IslandAt(worker.PosIdx);
			}
			if (!FindSleepSpot(num))
			{
				return Failure(T.AdRejCouldNotReserve);
			}
			if (targetBed == null)
			{
				if ((sleepNeed.Value < -50f || sleepNeed.Demand > 0.99f) && S.Rng.Chance(0.8f))
				{
					EmergencySleep();
					return StillWorking;
				}
				ApplyNoBedPenalty();
				return Failure(T.AdRejLackingBed);
			}
			return StillWorking;
		}

		private void InitializeNeeds()
		{
			sleepNeed = worker.Needs.GetNeed(NeedId.Sleep);
			hungerNeed = worker.Needs.GetNeed(NeedId.Hunger);
			toiletNeed = worker.Needs.GetNeed(NeedId.Toilet);
			restNeed = worker.Needs.GetNeed(NeedId.Rest);
			if (Mathf.Approximately(sleepNeedBefore, 0f))
			{
				sleepNeedBefore = sleepNeed.Value;
			}
		}

		private bool FindSleepSpot(int workerIsland)
		{
			if (slot != null)
			{
				return true;
			}
			if (!checkedForOwnedBed)
			{
				checkedForOwnedBed = true;
				ownedBedAtt = S.Sys.Att.BeingToTileFirst(worker.Id, BedAttachmentType);
				if (ownedBedAtt != null)
				{
					ISleepable sleepable = S.Components.Find<ISleepable>(ownedBedAtt.TileId, ownedBedAtt.SlotComp);
					ownedBed = sleepable?.GetSlot(ownedBedAtt.SlotPosIdx, ownedBedAtt.SlotNumber);
					if (ownedBed != null)
					{
						slot = ownedBed;
						slotIdx = ownedBed.SlotPosIdx;
						targetBed = sleepable;
						return MoveToTarget();
					}
				}
			}
			slot = S.Sys.Slots.FindForSleep(worker, workerIsland, blacklistedBeds);
			if (slot == null)
			{
				return true;
			}
			slotIdx = slot.SlotPosIdx;
			targetBed = slot.Parent as ISleepable;
			return MoveToTarget();
		}

		private bool MoveToTarget()
		{
			int slotPosIdx = slot.SlotPosIdx;
			if (!slot.Reserve(worker))
			{
				return false;
			}
			if (slotPosIdx != worker.PosIdx)
			{
				subAction = ActMoveToPos.SubActionTo(slotPosIdx, worker, updateAnchor: true, groundedSoftFail: false);
			}
			else
			{
				hasReachedSleepSpot = true;
			}
			return true;
		}

		private void EmergencySleep()
		{
			isEmergencySleep = true;
			worker.Graphics.SetVertical(isVertical: false);
		}

		private bool UseBed()
		{
			if (isUsingBed && worker.Health.IsDamaged)
			{
				float num = 1f - hungerNeed.Demand;
				if (num > 0.33f)
				{
					worker.Health.RepairDamage(0.005f * num);
				}
			}
			if (!isUsingBed || Rng.UChance(0.001f))
			{
				if (!isUsingBed && !slot.Put(worker))
				{
					return false;
				}
				if (!isUsingBed && targetBed.HideBeing)
				{
					worker.Graphics.SetHidden();
					worker.Graphics.SetOffset(slot.ContainedPosition - worker.Position);
				}
				else
				{
					worker.Graphics.SetVertical(isVertical: false);
					if (worker.Persona.Species.IsPet)
					{
						worker.Graphics.SetFacing(Rng.UFrom(Facing.Types));
						worker.Graphics.SetRotation(Rng.URange(-45f, 45f));
					}
					else
					{
						worker.Graphics.SetFacing(targetBed.SpotRotation);
						worker.Graphics.SetRotation(Rng.URange(-10f, 10f));
					}
					if (!isUsingBed)
					{
						Vector2 vector = EntityUtils.CenterOf(worker.PosIdx) - worker.Position;
						worker.Graphics.SetOffset(slot.ContainedOffset + vector);
					}
				}
				isUsingBed = true;
			}
			return true;
		}

		private void ApplySleepInterrupted()
		{
			worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration8h, T.SleepInterrupted, -5));
		}

		private void ApplyPrematureWakeup()
		{
			worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration8h, T.SleptTooShort, -5));
		}

		private void ApplyNoBedPenalty()
		{
			worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration8h, T.NowhereToSleep, -5));
		}

		private void ApplyBedBonus()
		{
			if (ownedBed == slot)
			{
				worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration4h, T.SleptInFamiliarBed, 5));
			}
			else if (ownedBed != slot)
			{
				worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration4h, T.SleptInUnfamiliarBed, -1));
			}
			else
			{
				worker.Mood.AddEffect(MoodEffect.Create(S.Ticks, MoodEffect.Duration4h, T.SleptInBed, 3));
			}
		}
	}
}

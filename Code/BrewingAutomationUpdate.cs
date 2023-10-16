using Game.Components;
using Game.Components.Upgrades;
using Game.Constants;
using Game.Data;
using Game.Utils;
using KL.Utils;
using UnityEngine;
// Reflection imports
using System;
using System.Reflection;

namespace MoreBeverages.Components.Upgrades
{
	public sealed class BrewingAutomationUpgrade : Upgrade
	{
		public const string Id = "BrewingAutomation";

        private const string BrewingMachineDefId = "Objects/Devices/CoffeeMaker";

		public override string Icon => "Obj/Upgrades/BrewingAutomationUpgrade";

		public override string Title => "obj.upgrades.brewingautomationupgrade".T();

		public override string UpgradeId => "BrewingAutomation";

		public override string MatTypeId => "BrewingAutomationUpgrade";

		public override int InstallDifficulty => 1;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Register()
		{
			Upgrade.Add("BrewingAutomation", new BrewingAutomationUpgrade());
		}

		public override bool IsCompatibleWith(Entity entity)
		{
			return entity.HasComponent<CrafterComp>()
                && entity.DefinitionId == BrewingMachineDefId
                && (entity.Upgrades.Upgrades == null || Array.IndexOf(entity.Upgrades.Upgrades, Id) == -1);
		}

		public override void OnInstall(Entity entity)
		{
			if (entity is Tile tile && tile.TryGetComponent<CrafterComp>(out var comp) && entity.DefinitionId == BrewingMachineDefId)
			{
				SetOperatorRequired(comp, false);
				if (tile.TryGetComponent<OperatableDeviceComp>(out var comp2))
				{
					comp2.IsAutomated = true;
					comp2.DismissOperator();
				}
			}
		}

		public override void OnUninstall(Entity entity)
		{
			if (entity is Tile tile && tile.TryGetComponent<CrafterComp>(out var comp) && entity.DefinitionId == BrewingMachineDefId)
			{
				SetOperatorRequired(comp, true);
				if (tile.TryGetComponent<OperatableDeviceComp>(out var comp2))
				{
					comp2.IsAutomated = false;
				}
			}
		}

        private void SetOperatorRequired(CrafterComp comp, bool required) {
            Type t = typeof(CrafterComp);
            FieldInfo field = null;
            try {
        	    field = t.GetField("isOperatorRequired", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            } catch(Exception e) {
                D.Warn("Not able to get private field isOperatorRequired: " + e.Message);
                try {
			        field = t.GetField("IsOperatorRequired", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                } catch (Exception ex) {
                    D.Warn("Not able to get public field IsOperatorRequired: " + ex.Message);
                }
            }
            field?.SetValue(comp, required);
        }
	}
}

using System;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;

namespace Assets.Scripts.Inventory__Items__Pickups.Items
{
	public static class ItemFactory
	{
		public static ItemBase CreateItem(EItem eItem, ItemInventory inventory)
		{
			if (DataManager.Instance != null && DataManager.Instance.TryGetItem(eItem, out var registered) && registered is OpenBonkItemData external)
				return new OpenBonkStackingItem(external, inventory);
			switch (eItem)
			{
				case EItem.Key: return new ItemKey(inventory);
				case EItem.Beer: return new ItemBeer(inventory);
				case EItem.SpikyShield: return new ItemSpikyShield(inventory);
				case EItem.Bonker: return new ItemBonker(inventory);
				case EItem.SlipperyRing: return new ItemSlipperyRing(inventory);
				case EItem.CowardsCloak: return new ItemCowardsCloak(inventory);
				case EItem.GymSauce: return new ItemGymSauce(inventory);
				case EItem.Battery: return new ItemBattery(inventory);
				case EItem.PhantomShroud: return new ItemPhantomShroud(inventory);
				case EItem.ForbiddenJuice: return new ItemForbiddenJuice(inventory);
				case EItem.DemonBlade: return new ItemDemonBlade(inventory);
				case EItem.GrandmasSecretTonic: return new ItemGrandmasSecretTonic(inventory);
				case EItem.GiantFork: return new ItemGiantFork(inventory);
				case EItem.MoldyCheese: return new ItemMoldyCheese(inventory);
				case EItem.GoldenSneakers: return new ItemGoldenSneakers(inventory);
				case EItem.SpicyMeatball: return new ItemSpicyMeatball(inventory);
				case EItem.Chonkplate: return new ItemChonkplate(inventory);
				case EItem.LightningOrb: return new ItemLightningOrb(inventory);
				case EItem.IceCube: return new ItemIceCube(inventory);
				case EItem.DemonicBlood: return new ItemDemonicBlood(inventory);
				case EItem.DemonicSoul: return new ItemDemonicSoul(inventory);
				case EItem.BeefyRing: return new ItemBeefyRing(inventory);
				case EItem.Dragonfire: return new ItemDragonfire(inventory);
				case EItem.GoldenGlove: return new ItemGoldenGlove(inventory);
				case EItem.GoldenShield: return new ItemGoldenShield(inventory);
				case EItem.ZaWarudo: return new ItemZaWarudo(inventory);
				case EItem.OverpoweredLamp: return new ItemOverpoweredLamp(inventory);
				case EItem.Feathers: return new ItemFlappyFeathers(inventory);
				case EItem.Ghost: return new ItemGhost(inventory);
				case EItem.SluttyCannon: return new ItemSluttyCannon(inventory);
				case EItem.TurboSocks: return new ItemTurboSocks(inventory);
				case EItem.ShatteredWisdom: return new ItemShatteredWisdom(inventory);
				case EItem.EchoShard: return new ItemEchoShard(inventory);
				case EItem.SuckyMagnet: return new ItemSuckyHoof(inventory);
				case EItem.Backpack: return new ItemBackpack(inventory);
				case EItem.Clover: return new ItemClover(inventory);
				case EItem.Campfire: return new ItemCampfire(inventory);
				case EItem.Rollerblades: return new ItemRollerblades(inventory);
				case EItem.Skuleg: return new ItemSkuleg(inventory);
				case EItem.EagleClaw: return new ItemEagleClaw(inventory);
				case EItem.Scarf: return new ItemScarf(inventory);
				case EItem.Anvil: return new ItemAnvil(inventory);
				case EItem.Oats: return new ItemOats(inventory);
				case EItem.CursedDoll: return new ItemCursedDoll(inventory);
				case EItem.EnergyCore: return new ItemEnergyCore(inventory);
				case EItem.ElectricPlug: return new ItemElectricPlug(inventory);
				case EItem.BobDead: return new ItemBobDead(inventory);
				case EItem.SoulHarvester: return new ItemSoulHarvester(inventory);
				case EItem.Mirror: return new ItemMirror(inventory);
				case EItem.JoesDagger: return new ItemJoesDagger(inventory);
				case EItem.WeebHeadset: return new ItemWeebHeadset(inventory);
				case EItem.SpeedBoi: return new ItemSpeedBoi(inventory);
				case EItem.Gasmask: return new ItemGasmask(inventory);
				case EItem.ToxicBarrel: return new ItemToxicBarrel(inventory);
				case EItem.HolyBook: return new ItemHolyBook(inventory);
				case EItem.BrassKnuckles: return new ItemBrassKnuckles(inventory);
				case EItem.IdleJuice: return new ItemIdleJuice(inventory);
				case EItem.Kevin: return new ItemKevin(inventory);
				case EItem.Borgar: return new ItemBorgor(inventory);
				case EItem.Medkit: return new ItemMedkit(inventory);
				case EItem.GamerGoggles: return new ItemGamerGoggles(inventory);
				case EItem.UnstableTransfusion: return new ItemUnstableTransfusion(inventory);
				case EItem.BloodyCleaver: return new ItemBloodyCleaver(inventory);
				case EItem.CreditCardRed: return new ItemCreditCardRed(inventory);
				case EItem.CreditCardGreen: return new ItemCreditCardGreen(inventory);
				case EItem.BossBuster: return new ItemBossBuster(inventory);
				case EItem.LeechingCrystal: return new ItemLeechingCrystal(inventory);
				case EItem.TacticalGlasses: return new ItemTacticalGlasses(inventory);
				case EItem.Cactus: return new ItemCactus(inventory);
				case EItem.CageKey: return new ItemNoImplementation(inventory);
				case EItem.IceCrystal: return new ItemIceCrystal(inventory);
				case EItem.TimeBracelet: return new ItemTimeBracelet(inventory);
				case EItem.GloveLightning: return new ItemGlovesLightning(inventory);
				case EItem.GlovePoison: return new ItemGlovesPoison(inventory);
				case EItem.GloveBlood: return new ItemGlovesBlood(inventory);
				case EItem.GloveCurse: return new ItemGlovesCursed(inventory);
				case EItem.GlovePower: return new ItemGlovesPower(inventory);
				case EItem.Wrench: return new ItemWrench(inventory);
				case EItem.Beacon: return new ItemBeacon(inventory);
				case EItem.GoldenRing: return new ItemNoImplementation(inventory);
				case EItem.QuinsMask: return new ItemQuinsMask(inventory);
				case EItem.CryptKey: return new ItemNoImplementation(inventory);
				case EItem.OldMask: return new ItemOldMask(inventory);
				case EItem.Snek: return new ItemSnek(inventory);
				case EItem.Pot: return new ItemPotSteel(inventory);
				case EItem.BobsLantern: return new ItemBobLantern(inventory);
				case EItem.Pumpkin: return new ItemPumpkin(inventory);
				case EItem.WizardsHat: return new ItemWizardsHat(inventory);
				default: throw new ArgumentOutOfRangeException(nameof(eItem), eItem, null);
			}
		}
	}
}

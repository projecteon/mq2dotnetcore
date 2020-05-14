using System;

namespace MQ2DotNetCore.MQ2Api
{
#pragma warning disable 1591

	/// <summary>
	/// A character class
	/// </summary>
	[Flags]
	public enum EQCharacterClassType
	{
		Warrior = 0x1,
		Cleric = 0x2,
		Paladin = 0x4,
		Ranger = 0x8,
		Shadowknight = 0x10,
		Druid = 0x20,
		Monk = 0x40,
		Bard = 0x80,
		Rogue = 0x100,
		Shaman = 0x200,
		Necromancer = 0x400,
		Wizard = 0x800,
		Mage = 0x1000,
		Enchanter = 0x2000,
		Beastlord = 0x4000,
		Berserker = 0x8000,
		Mercenary = 0x10000,

		Tank = Warrior | Paladin | Shadowknight,
		Priest = Cleric | Druid | Shaman,
		Caster = Wizard | Mage | Enchanter | Necromancer,
		Melee = Beastlord | Berserker | Bard | Rogue | Ranger | Monk,

		All = Tank | Priest | Caster | Melee
	}

#pragma warning restore 1591
}

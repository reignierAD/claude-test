using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SuikodenLike.Core;
using SuikodenLike.Data;

namespace SuikodenLike.EditorTools
{
    /// <summary>
    /// Creates the sample game's content assets — characters, skills, items,
    /// enemies, conversations and the encounter table. Every one of these is
    /// an ordinary asset you can open in the Inspector and rewrite.
    /// </summary>
    public static class SampleContentBuilder
    {
        public const string DataFolder = "Assets/Content/Data";

        public class Content
        {
            public CharacterData nadia, corin, yves, sable;
            public CharacterData elder, villager;
            public ItemData medicine, ether, ironBlade, kiteShield, gateKey;
            public SkillData powerStrike, fireBolt, gale, mend, acidSpit;
            public EnemyData slime, greatSlime;
            public EncounterTable grassland;
            public DialogueData elderTalk, villagerTalk, villagerAfterQuest, chestFound;
        }

        static T Create<T>(string name) where T : ScriptableObject
        {
            PlaceholderArt.EnsureFolder(DataFolder);
            string path = DataFolder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static Content Build(PlaceholderArt.Baked art)
        {
            var c = new Content();

            // ---------------- skills ----------------
            c.powerStrike = Create<SkillData>("Skill_PowerStrike");
            c.powerStrike.skillId = "power_strike";
            c.powerStrike.displayName = "Power Strike";
            c.powerStrike.description = "A heavy overhead blow.";
            c.powerStrike.kind = SkillKind.PhysicalDamage;
            c.powerStrike.target = SkillTarget.SingleEnemy;
            c.powerStrike.mpCost = 3;
            c.powerStrike.power = 14;

            c.fireBolt = Create<SkillData>("Skill_FireBolt");
            c.fireBolt.skillId = "fire_bolt";
            c.fireBolt.displayName = "Fire Bolt";
            c.fireBolt.description = "A lance of flame at one foe.";
            c.fireBolt.kind = SkillKind.MagicDamage;
            c.fireBolt.target = SkillTarget.SingleEnemy;
            c.fireBolt.mpCost = 4;
            c.fireBolt.power = 12;

            c.gale = Create<SkillData>("Skill_Gale");
            c.gale.skillId = "gale";
            c.gale.displayName = "Gale";
            c.gale.description = "Cutting wind across the enemy line.";
            c.gale.kind = SkillKind.MagicDamage;
            c.gale.target = SkillTarget.AllEnemies;
            c.gale.mpCost = 8;
            c.gale.power = 10;

            c.mend = Create<SkillData>("Skill_Mend");
            c.mend.skillId = "mend";
            c.mend.displayName = "Mend";
            c.mend.description = "Closes an ally's wounds.";
            c.mend.kind = SkillKind.Heal;
            c.mend.target = SkillTarget.SingleAlly;
            c.mend.mpCost = 3;
            c.mend.power = 20;

            c.acidSpit = Create<SkillData>("Skill_AcidSpit");
            c.acidSpit.skillId = "acid_spit";
            c.acidSpit.displayName = "Acid Spit";
            c.acidSpit.description = "A caustic spray.";
            c.acidSpit.kind = SkillKind.MagicDamage;
            c.acidSpit.target = SkillTarget.SingleEnemy;
            c.acidSpit.mpCost = 2;
            c.acidSpit.power = 8;

            // ---------------- items ----------------
            c.medicine = Create<ItemData>("Item_Medicine");
            c.medicine.itemId = "medicine";
            c.medicine.displayName = "Medicine";
            c.medicine.description = "Restores 50 HP to one ally.";
            c.medicine.icon = art.potion;
            c.medicine.type = ItemType.Consumable;
            c.medicine.effect = ItemEffect.HealHP;
            c.medicine.effectAmount = 50;
            c.medicine.buyPrice = 100;
            c.medicine.sellPrice = 50;

            c.ether = Create<ItemData>("Item_Ether");
            c.ether.itemId = "ether";
            c.ether.displayName = "Ether";
            c.ether.description = "Restores 20 MP to one ally.";
            c.ether.icon = art.potion;
            c.ether.type = ItemType.Consumable;
            c.ether.effect = ItemEffect.HealMP;
            c.ether.effectAmount = 20;
            c.ether.buyPrice = 200;
            c.ether.sellPrice = 100;

            c.ironBlade = Create<ItemData>("Item_IronBlade");
            c.ironBlade.itemId = "iron_blade";
            c.ironBlade.displayName = "Iron Blade";
            c.ironBlade.description = "A steadier sword. Attack +4.";
            c.ironBlade.icon = art.sword;
            c.ironBlade.type = ItemType.Weapon;
            c.ironBlade.stackable = false;
            c.ironBlade.usableInBattle = false;
            c.ironBlade.usableInField = false;
            c.ironBlade.attackBonus = 4;
            c.ironBlade.buyPrice = 320;
            c.ironBlade.sellPrice = 160;

            c.kiteShield = Create<ItemData>("Item_KiteShield");
            c.kiteShield.itemId = "kite_shield";
            c.kiteShield.displayName = "Kite Shield";
            c.kiteShield.description = "Battered but honest. Defence +3.";
            c.kiteShield.icon = art.shield;
            c.kiteShield.type = ItemType.Armor;
            c.kiteShield.stackable = false;
            c.kiteShield.usableInBattle = false;
            c.kiteShield.usableInField = false;
            c.kiteShield.defenseBonus = 3;
            c.kiteShield.buyPrice = 260;
            c.kiteShield.sellPrice = 130;

            c.gateKey = Create<ItemData>("Item_GateKey");
            c.gateKey.itemId = "gate_key";
            c.gateKey.displayName = "Gate Key";
            c.gateKey.description = "Opens the north gate.";
            c.gateKey.icon = art.key;
            c.gateKey.type = ItemType.KeyItem;
            c.gateKey.stackable = false;
            c.gateKey.usableInBattle = false;
            c.gateKey.usableInField = false;
            c.gateKey.buyPrice = 0;
            c.gateKey.sellPrice = 0;

            // ---------------- party ----------------
            c.nadia = Character("Char_Nadia", "Nadia", art.heroBlue, art.portraitHero,
                hp: 30, mp: 6, str: 7, def: 4, mag: 4, mdef: 3, spd: 7, tec: 6, lck: 5);
            c.nadia.skills = new List<SkillData> { c.powerStrike };
            c.nadia.bio = "Levelled the sword at the wrong people once. Trying to make it count now.";

            c.corin = Character("Char_Corin", "Corin", art.heroRed, art.portraitHero,
                hp: 36, mp: 4, str: 8, def: 6, mag: 2, mdef: 3, spd: 4, tec: 5, lck: 4);
            c.corin.skills = new List<SkillData> { c.powerStrike };
            c.corin.bio = "Slow, immovable, and entirely unbothered about it.";

            c.yves = Character("Char_Yves", "Yves", art.heroTeal, art.portraitHero,
                hp: 24, mp: 14, str: 5, def: 3, mag: 7, mdef: 6, spd: 6, tec: 7, lck: 6);
            c.yves.skills = new List<SkillData> { c.mend, c.fireBolt };
            c.yves.bio = "Field medic. Keeps everyone upright, complains the whole time.";

            c.sable = Character("Char_Sable", "Sable", art.heroPurple, art.portraitHero,
                hp: 20, mp: 22, str: 3, def: 2, mag: 10, mdef: 7, spd: 8, tec: 8, lck: 7);
            c.sable.skills = new List<SkillData> { c.fireBolt, c.gale, c.mend };
            c.sable.bio = "Reads weather like other people read faces.";

            // NPCs still get character assets — that's how portraits work.
            c.elder = Character("Char_ElderMarn", "Elder Marn", art.elder, art.portraitElder,
                hp: 12, mp: 0, str: 2, def: 2, mag: 2, mdef: 2, spd: 2, tec: 2, lck: 2);
            c.villager = Character("Char_Villager", "Villager", art.villager, art.portraitVillager,
                hp: 12, mp: 0, str: 2, def: 2, mag: 2, mdef: 2, spd: 2, tec: 2, lck: 2);

            // ---------------- enemies ----------------
            c.slime = Create<EnemyData>("Enemy_Slime");
            c.slime.enemyId = "slime";
            c.slime.displayName = "Slime";
            c.slime.sprite = art.slimeGreen;
            c.slime.stats = Stats(18, 0, 5, 2, 2, 2, 4, 3, 3);
            c.slime.skills = new List<SkillData>();
            c.slime.expReward = 6;
            c.slime.goldReward = 8;
            c.slime.loot = new List<LootDrop> {
                new LootDrop { item = c.medicine, chance = 0.25f }
            };

            c.greatSlime = Create<EnemyData>("Enemy_GreatSlime");
            c.greatSlime.enemyId = "great_slime";
            c.greatSlime.displayName = "Great Slime";
            c.greatSlime.sprite = art.slimeRed;
            c.greatSlime.stats = Stats(46, 10, 9, 5, 7, 5, 5, 5, 4);
            c.greatSlime.skills = new List<SkillData> { c.acidSpit };
            c.greatSlime.expReward = 22;
            c.greatSlime.goldReward = 30;
            c.greatSlime.loot = new List<LootDrop> {
                new LootDrop { item = c.medicine, chance = 0.5f },
                new LootDrop { item = c.kiteShield, chance = 0.1f }
            };

            // ---------------- encounters ----------------
            c.grassland = Create<EncounterTable>("Encounters_Grassland");
            c.grassland.averageStepsBetweenEncounters = 6f;
            c.grassland.variance = 0.5f;
            c.grassland.groups = new List<EncounterGroup> {
                new EncounterGroup { label = "Lone Slime", weight = 3f,
                    enemies = new List<EnemyData> { c.slime } },
                new EncounterGroup { label = "Slime Pair", weight = 2f,
                    enemies = new List<EnemyData> { c.slime, c.slime } },
                new EncounterGroup { label = "Great Slime", weight = 1f,
                    enemies = new List<EnemyData> { c.slime, c.greatSlime, c.slime } }
            };

            // ---------------- conversations ----------------
            // Branch B sits before branch A so branch A can simply run off the
            // end of the list; branch B ends with an explicit gotoLine of -1.
            c.elderTalk = Create<DialogueData>("Dialogue_ElderMarn");
            c.elderTalk.lines = new List<DialogueLine> {
                new DialogueLine {
                    speakerName = "Elder Marn", portrait = art.portraitElder,
                    text = "The bandits took the north road at dawn."
                },
                new DialogueLine {
                    speakerName = "Elder Marn", portrait = art.portraitElder,
                    text = "If you mean to follow them, you'll want a steadier sword than that.",
                    choices = new List<DialogueChoice> {
                        new DialogueChoice { text = "We'll go after them.", gotoLine = 3 },
                        new DialogueChoice { text = "Not our fight.", gotoLine = 2 }
                    }
                },
                new DialogueLine {
                    speakerName = "Elder Marn", portrait = art.portraitElder,
                    text = "Suit yourself. The road will still be there tomorrow.",
                    choices = new List<DialogueChoice> {
                        new DialogueChoice { text = "(leave)", gotoLine = -1 }
                    }
                },
                new DialogueLine {
                    speakerName = "Elder Marn", portrait = art.portraitElder,
                    text = "Then take this. It served me well enough.",
                    giveItem = c.ironBlade, setFlag = "quest_started"
                },
                new DialogueLine {
                    speakerName = "Elder Marn", portrait = art.portraitElder,
                    text = "Follow the road north. And mind the tall grass — it bites."
                }
            };

            c.villagerTalk = Create<DialogueData>("Dialogue_Villager");
            c.villagerTalk.lines = new List<DialogueLine> {
                new DialogueLine {
                    speakerName = "Villager", portrait = art.portraitVillager,
                    text = "The elder's been pacing since sunup. Go and talk to him."
                }
            };

            c.villagerAfterQuest = Create<DialogueData>("Dialogue_VillagerAfterQuest");
            c.villagerAfterQuest.lines = new List<DialogueLine> {
                new DialogueLine {
                    speakerName = "Villager", portrait = art.portraitVillager,
                    text = "So you're really going. Here — you'll want this more than I do.",
                    giveItem = c.medicine
                },
                new DialogueLine {
                    speakerName = "Villager", portrait = art.portraitVillager,
                    text = "Come back in one piece."
                }
            };

            // Persist everything.
            var all = new Object[] {
                c.powerStrike, c.fireBolt, c.gale, c.mend, c.acidSpit,
                c.medicine, c.ether, c.ironBlade, c.kiteShield, c.gateKey,
                c.nadia, c.corin, c.yves, c.sable, c.elder, c.villager,
                c.slime, c.greatSlime, c.grassland,
                c.elderTalk, c.villagerTalk, c.villagerAfterQuest
            };
            foreach (var o in all) if (o != null) EditorUtility.SetDirty(o);
            AssetDatabase.SaveAssets();
            return c;
        }

        static StatBlock Stats(int hp, int mp, int str, int def, int mag, int mdef, int spd, int tec, int lck)
        {
            return new StatBlock {
                maxHP = hp, maxMP = mp, strength = str, defense = def, magic = mag,
                magicDefense = mdef, speed = spd, technique = tec, luck = lck
            };
        }

        static CharacterData Character(string assetName, string display, Sprite sprite, Sprite portrait,
            int hp, int mp, int str, int def, int mag, int mdef, int spd, int tec, int lck)
        {
            var ch = Create<CharacterData>(assetName);
            ch.characterId = display.ToLowerInvariant().Replace(' ', '_');
            ch.displayName = display;
            ch.sprite = sprite;
            ch.portrait = portrait;
            ch.baseStats = Stats(hp, mp, str, def, mag, mdef, spd, tec, lck);
            ch.startingLevel = 1;
            ch.hpPerLevel = 4;
            ch.mpPerLevel = 2;
            ch.strengthPerLevel = 1;
            ch.defensePerLevel = 1;
            if (ch.skills == null) ch.skills = new List<SkillData>();
            return ch;
        }
    }
}

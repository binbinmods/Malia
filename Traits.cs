using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Obeliskial_Content;
using UnityEngine;
using static Malia.CustomFunctions;
using static Malia.Plugin;
using static Malia.DescriptionFunctions;
using static Malia.CharacterFunctions;
using System.Text;
using TMPro;
using Obeliskial_Essentials;
using System.Data.Common;

namespace Malia
{
    [HarmonyPatch]
    internal class Traits
    {
        // list of your trait IDs

        public static string[] simpleTraitList = ["trait0", "trait1a", "trait1b", "trait2a", "trait2b", "trait3a", "trait3b", "trait4a", "trait4b"];

        public static string[] myTraitList = simpleTraitList.Select(trait => subclassname.ToLower() + trait).ToArray(); // Needs testing

        public static string trait0 = myTraitList[0];
        // static string trait1b = myTraitList[1];
        public static string trait2a = myTraitList[3];
        public static string trait2b = myTraitList[4];
        public static string trait4a = myTraitList[7];
        public static string trait4b = myTraitList[8];

        // public static int infiniteProctection = 0;
        // public static int bleedInfiniteProtection = 0;
        public static bool isDamagePreviewActive = false;

        public static bool isCalculateDamageActive = false;
        public static int infiniteProctection = 0;

        public static string debugBase = "Binbin - Testing " + heroName + " ";

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Trait), "DoTrait")]
        public static bool DoTrait(Enums.EventActivation _theEvent, string _trait, Character _character, Character _target, int _auxInt, string _auxString, CardData _castedCard, ref Trait __instance)
        {
            if ((UnityEngine.Object)MatchManager.Instance == (UnityEngine.Object)null)
                return false;
            if (Content.medsCustomTraitsSource.Contains(_trait) && myTraitList.Contains(_trait))
            {
                DoCustomTrait(_trait, ref __instance, ref _theEvent, ref _character, ref _target, ref _auxInt, ref _auxString, ref _castedCard);
                return false;
            }
            return true;
        }

        public static void DoCustomTrait(string _trait, ref Trait __instance, ref Enums.EventActivation _theEvent, ref Character _character, ref Character _target, ref int _auxInt, ref string _auxString, ref CardData _castedCard)
        {
            // get info you may need
            TraitData traitData = Globals.Instance.GetTraitData(_trait);
            List<CardData> cardDataList = [];
            List<string> heroHand = MatchManager.Instance.GetHeroHand(_character.HeroIndex);
            Hero[] teamHero = MatchManager.Instance.GetTeamHero();
            NPC[] teamNpc = MatchManager.Instance.GetTeamNPC();

            if (!IsLivingHero(_character))
            {
                return;
            }
            string traitName = traitData.TraitName;
            string traitId = _trait;


            if (_trait == trait0)
            {
                // trait0:
            }


            else if (_trait == trait2a)
            {
                // trait2a
                // At the start of your turn, if you have more than 10 Poison, gain Stanza 1.
                if (_character.HaveTrait(_trait) && _character.GetAuraCharges("poison") > 10)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    _character.SetAuraTrait(_character, "stanzai", 1);
                    _character?.HeroItem?.ScrollCombatText(traitName, Enums.CombatScrollEffectType.Trait);
                }
            }



            else if (_trait == trait2b)
            {
                // trait2b:
                // At the start of your turn, every 4 Stacks of Reinforce on you gain 1 Infuse and restore 5% of your Max Health.
                LogDebug($"Handling Trait {traitId}: {traitName}");
                int nRepeats = _character.GetAuraCharges("reinforce") / 3;
                if (nRepeats <= 0)
                {
                    return;
                }
                _character.SetAuraTrait(_character, "infuse", nRepeats);
                _character.IndirectHeal(Mathf.RoundToInt(_character.GetMaxHP() * 0.05f * nRepeats));
                _character?.HeroItem?.ScrollCombatText(traitName, Enums.CombatScrollEffectType.Trait);
            }

            else if (_trait == trait4a)
            {
                // trait 4a;
                // When you play a Defense, advance your Stanza. Advancing past Stanza 3 grants 4 Powerful, 2 Inspire and Stanza 1 to all Heroes but you suffer 2 Shackles (once per turn).

                if (_castedCard.HasCardType(Enums.CardType.Defense) && CanIncrementTraitActivations(traitId))
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");
                    ProgressStanza(_character);
                    IncrementTraitActivations(traitId);

                    if (_character.HasEffect("stanzai") || _character.HasEffect("stanzaii") || _character.HasEffect("stanzaiii"))
                    {
                        return;
                    }
                    ApplyAuraCurseToAll("powerful", 4, AppliesTo.Heroes, _character, true);
                    ApplyAuraCurseToAll("inspire", 2, AppliesTo.Heroes, _character, true);
                    ApplyAuraCurseToAll("stanzai", 1, AppliesTo.Heroes, _character, true);
                    _character.SetAuraTrait(_character, "shackle", 2);
                    _character?.HeroItem?.ScrollCombatText(traitName, Enums.CombatScrollEffectType.Trait);
                }
            }

            else if (_trait == trait4b)
            {
                // trait 4b:
                // Immune to Slow. Chill no longer reduces your Speed. When you play a Defense with cost >=3, dispel Chill and Slow on all other heroes (once per turn).
                if (_castedCard.HasCardType(Enums.CardType.Defense) && CanIncrementTraitActivations(traitId) && MatchManager.Instance.energyJustWastedByHero >= 3)
                {
                    LogDebug($"Handling Trait {traitId}: {traitName}");

                    for (int i = 0; i < teamHero.Length; i++)
                    {
                        if (IsLivingHero(teamHero[i]) && teamHero[i] != _character)
                        {
                            teamHero[i].HealAuraCurse(GetAuraCurseData("chill"));
                            teamHero[i].HealAuraCurse(GetAuraCurseData("slow"));
                        }
                    }
                }
            }

        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(AtOManager), "GlobalAuraCurseModificationByTraitsAndItems")]
        // [HarmonyPriority(Priority.Last)]
        public static void GlobalAuraCurseModificationByTraitsAndItemsPostfix(ref AtOManager __instance, ref AuraCurseData __result, string _type, string _acId, Character _characterCaster, Character _characterTarget)
        {
            // LogInfo($"GACM {subclassName}");

            Character characterOfInterest = _type == "set" ? _characterTarget : _characterCaster;
            string traitOfInterest;
            switch (_acId)
            {
                // trait0:
                // Poison no longer deals Damage to you, instead it reduces Block gained by 1 and increases Max HP by 2 per charge. 
                // When you gain Block, suffer that much Poison -this is not affected by modifiers-

                // trait2a:

                // trait2b:

                // trait 4a;

                // trait 4b:
                // Immune to Slow. Chill no longer reduces your Speed. When you play a Defense with cost >=3, dispel Chill and Slow on all other heroes (once per turn).

                case "poison":
                    traitOfInterest = trait0;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                        __result.Removable = false;
                        __result.DamageWhenConsumedPerCharge = 0;
                        __result.CharacterStatModified = Enums.CharacterStat.Hp;
                        __result.CharacterStatModifiedValuePerStack = 2 * GetRustMultiplier(characterOfInterest, _acId);
                    }
                    break;
                case "chill":
                    traitOfInterest = trait4b;
                    if (IfCharacterHas(characterOfInterest, CharacterHas.Trait, traitOfInterest, AppliesTo.ThisHero))
                    {
                        __result.CharacterStatModified = Enums.CharacterStat.None;
                        __result.CharacterStatModifiedValuePerStack = 0;
                        __result.CharacterStatChargesMultiplierNeededForOne = 1;
                        __result.ChargesAuxNeedForOne2 = 0;
                    }
                    break;
            }
        }

        // [HarmonyPrefix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePrefix(ref Character __instance, AuraCurseData AC, ref int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth"))
        //     {
        //         __state = Mathf.FloorToInt(__instance.GetAuraCharges("stealth") * 0.25f);
        //         // __instance.SetAuraTrait(null, "stealth", 1);

        //     }

        // }

        // [HarmonyPostfix]
        // [HarmonyPatch(typeof(Character), "HealAuraCurse")]
        // public static void HealAuraCursePostfix(ref Character __instance, AuraCurseData AC, int __state)
        // {
        //     LogInfo($"HealAuraCursePrefix {subclassName}");
        //     string traitOfInterest = trait4b;
        //     if (IsLivingHero(__instance) && __instance.HaveTrait(traitOfInterest) && AC == GetAuraCurseData("stealth") && __state > 0)
        //     {
        //         // __state = __instance.GetAuraCharges("stealth");
        //         __instance.SetAuraTrait(null, "stealth", __state);
        //     }

        // }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterItem), nameof(CharacterItem.CalculateDamagePrePostForThisCharacter))]
        public static void CalculateDamagePrePostForThisCharacterPostfix()
        {
            isDamagePreviewActive = false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPrefix()
        {
            isDamagePreviewActive = true;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MatchManager), nameof(MatchManager.SetDamagePreview))]
        public static void SetDamagePreviewPostfix()
        {
            isDamagePreviewActive = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.SetEvent))]
        public static void SetEventPostfix(Character __instance, Enums.EventActivation theEvent, Character target = null, int auxInt = 0, string auxString = "", Character caster = null)
        {
            // Poison no longer deals Damage to you, instead it reduces Block gained by 1 and increases Max HP by 2 per charge. When you gain Block, suffer that much Poison -this is not affected by modifiers-
            if (theEvent == Enums.EventActivation.AuraCurseSet && IsLivingHero(target) && target.HaveTrait(trait0) && auxString == "block")
            {
                LogDebug($"Handling Living Poison");

                target?.SetAura(target, GetAuraCurseData("poison"), auxInt, useCharacterMods: false);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), nameof(Character.SetAura))]
        public static void SetAuraPrefix(ref Character __instance, Character theCaster, ref AuraCurseData _acData, ref int charges, bool fromTrait = false, Enums.CardClass CC = Enums.CardClass.None, bool useCharacterMods = true, bool canBePreventable = true)
        {
            // if (IsLivingHero(__instance) && __instance.HaveTrait(trait0) && _acData.Id == "block")
            // {
            //     charges -= __instance.GetAuraCharges("poison");
            //     if (charges < 0)
            //         charges = 0;
            // }
            if (IsLivingHero(__instance) && __instance.HaveTrait(trait0) && _acData.Id.ToLower() == "block")
            {
                AuraCurseData auraCurseData = AtOManager.Instance.GlobalAuraCurseModificationByTraitsAndItems("set", _acData.Id, theCaster, __instance);
                if (auraCurseData == null)
                {
                    auraCurseData = Globals.Instance.GetAuraCurseData(_acData.Id);
                }
                if (auraCurseData == null)
                {
                    return;
                }
                if (!auraCurseData.IsAura && __instance.IsInvulnerable() && auraCurseData.Id.ToLower() != "doom")
                {
                    return;
                }
                if (theCaster != null && useCharacterMods)
                {
                    charges += theCaster.GetAuraCurseQuantityModification(auraCurseData.Id, CC);
                }
                _acData = Globals.Instance.GetAuraCurseData("poison");
                charges = Functions.FuncRoundToInt(charges * 0.5f);
            }
        }






    }
}


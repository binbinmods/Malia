
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using static Obeliskial_Essentials.Essentials;
using System;
using static Malia.CustomFunctions;
using static Malia.Plugin;
using static Malia.Traits;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;


// Make sure your namespace is the same everywhere
namespace Malia
{

    // [HarmonyPatch] //DO NOT REMOVE/CHANGE

    public class DescriptionFunctions
    {

    }
    public class CharacterFunctions
    {
        public static void ProgressStanza(Character character)
        {
            if (character == null || !character.Alive)
            {
                return;
            }
            if (character.HasEffect("stanzaiii"))
            {
                character.HealAuraCurse(GetAuraCurseData("stanzaiii"));
            }
            else if (character.HasEffect("stanzaii"))
            {
                character.SetAura(character, GetAuraCurseData("stanzaiii"), 1);
            }
            else if (character.HasEffect("stanzai"))
            {
                character.SetAura(character, GetAuraCurseData("stanzaii"), 1);
            }
            else
            {
                character.SetAura(character, GetAuraCurseData("stanzai"), 1);
            }

        }
    }
}
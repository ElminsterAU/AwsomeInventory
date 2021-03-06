﻿// <copyright file="AddHumanlikeOrders_AwesomeInventory_Patch.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using AwesomeInventory.Jobs;
using AwesomeInventory.Resources;
using AwesomeInventory.UI;
using AwesomeInventory.Utilities;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AwesomeInventory.Common.HarmonyPatches
{
    /// <summary>
    /// Add options to context menu when right-click on items on map.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class AddHumanlikeOrders_AwesomeInventory_Patch
    {
        static AddHumanlikeOrders_AwesomeInventory_Patch()
        {
            MethodInfo original = AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders");
            MethodInfo postfix = AccessTools.Method(typeof(AddHumanlikeOrders_AwesomeInventory_Patch), "Postfix");
            Utility.Harmony.Patch(original, null, new HarmonyMethod(postfix));
        }

        /// <summary>
        /// Add options to context menu when right-click on items on map.
        /// </summary>
        /// <param name="clickPos"> Position of the mouse when right-click. </param>
        /// <param name="pawn"> Currently focused pawn. </param>
        /// <param name="opts"> Options displayed in context menu. </param>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Harmony patch")]
        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 position = IntVec3.FromVector3(clickPos);

            // Add options for equipment.
            if (pawn.equipment != null)
            {
                List<Thing> things = position.GetThingList(pawn.Map);
                foreach (Thing thing in things)
                {
                    if (thing.TryGetComp<CompEquippable>() != null)
                    {
                        ThingWithComps equipment = (ThingWithComps)thing;

                        if (equipment.def.IsWeapon
                            && !pawn.WorkTagIsDisabled(WorkTags.Violent)
                            && pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly)
                            && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                            && !(pawn.IsQuestLodger() && (!equipment.def.IsWeapon || pawn.equipment.Primary != null))
                            && EquipmentUtility.CanEquip(equipment, pawn, out _))
                        {
                            string text3 = UIText.AIEquip.Translate(thing.LabelShort);
                            if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                            {
                                text3 += " " + UIText.EquipWarningBrawler.Translate();
                            }

                            var option = FloatMenuUtility.DecoratePrioritizedTask(
                                new FloatMenuOption(
                                    text3,
                                    () =>
                                    {
                                        TaggedString equipWeaponConfirmationDialogText = ThingRequiringRoyalPermissionUtility.GetEquipWeaponConfirmationDialogText(equipment, pawn);
                                        CompBladelinkWeapon compBladelinkWeapon = equipment.TryGetComp<CompBladelinkWeapon>();
                                        if (compBladelinkWeapon != null && compBladelinkWeapon.bondedPawn != pawn)
                                        {
                                            if (!equipWeaponConfirmationDialogText.NullOrEmpty())
                                            {
                                                equipWeaponConfirmationDialogText += "\n\n";
                                            }

                                            equipWeaponConfirmationDialogText += "BladelinkEquipWarning".Translate();
                                        }

                                        if (!equipWeaponConfirmationDialogText.NullOrEmpty())
                                        {
                                            equipWeaponConfirmationDialogText += "\n\n" + "RoyalWeaponEquipConfirmation".Translate();
                                            Find.WindowStack.Add(
                                                new Dialog_MessageBox(
                                                    equipWeaponConfirmationDialogText,
                                                    "Yes".Translate(),
                                                    () =>
                                                    {
                                                        Equip();
                                                    },
                                                    "No".Translate()));
                                        }
                                        else
                                        {
                                            Equip();
                                        }
                                    },
                                    MenuOptionPriority.High),
                                pawn,
                                equipment);
                            opts.Add(option);
                        }

                        void Equip()
                        {
                            equipment.SetForbidden(value: false);
                            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(AwesomeInventory_JobDefOf.AwesomeInventory_MapEquip, equipment));
                            MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                        }
                    }
                }
            }

            // Add options for apparel.
            if (pawn.apparel != null)
            {
                Apparel apparel = pawn.Map.thingGrid.ThingAt<Apparel>(position);
                if (apparel != null)
                {
                    if (pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly)
                        && !apparel.IsBurning()
                        && ApparelOptionUtility.CanWear(pawn, apparel))
                    {
                        FloatMenuOption option = FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                UIText.AIForceWear.Translate(apparel.LabelShort),
                                () =>
                                {
                                    DressJob dressJob = SimplePool<DressJob>.Get();
                                    dressJob.def = AwesomeInventory_JobDefOf.AwesomeInventory_Dress;
                                    dressJob.targetA = apparel;
                                    dressJob.ForceWear = true;

                                    apparel.SetForbidden(value: false);
                                    pawn.jobs.TryTakeOrderedJob(dressJob);
                                },
                                MenuOptionPriority.High),
                            pawn,
                            apparel);
                        opts.Add(option);

                        option = FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                UIText.AIWear.Translate(apparel.LabelShort),
                                () =>
                                {
                                    DressJob dressJob = SimplePool<DressJob>.Get();
                                    dressJob.def = AwesomeInventory_JobDefOf.AwesomeInventory_Dress;
                                    dressJob.targetA = apparel;
                                    dressJob.ForceWear = false;

                                    apparel.SetForbidden(value: false);
                                    pawn.jobs.TryTakeOrderedJob(dressJob);
                                },
                                MenuOptionPriority.High),
                            pawn,
                            apparel);
                        opts.Add(option);
                    }
                }
            }
        }
    }
}

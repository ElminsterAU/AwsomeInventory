﻿// <copyright file="JobGiver_AwesomeInventory_FindWeapon.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeInventory.Loadout;
using RimWorld;
using Verse;
using Verse.AI;

namespace AwesomeInventory.Jobs
{
    /// <summary>
    /// Find weapon that fits the whitelist of loadout.
    /// </summary>
    public class JobGiver_AwesomeInventory_FindWeapon : ThinkNode_JobGiver
    {
        private JobGiver_FindItemByRadius _parent;

        /// <summary>
        /// Try to give a job to <paramref name="pawn"/>.
        /// </summary>
        /// <param name="pawn"> Pawn that will be assigned a job to. </param>
        /// <returns> A job assigned to <paramref name="pawn"/>. </returns>
        protected override Job TryGiveJob(Pawn pawn)
        {
            ValidateArg.NotNull(pawn, nameof(pawn));

            if (!pawn.Faction.IsPlayer)
            {
                return null;
            }

            if (!pawn.RaceProps.Humanlike)
            {
                return null;
            }

            if (pawn.Drafted)
            {
                return null;
            }

            if (pawn.equipment == null)
            {
                return null;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                ||
                pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return null;
            }

            if (pawn.Map == null)
            {
                return null;
            }

            if (_parent == null)
            {
                if (parent is JobGiver_FindItemByRadius p)
                {
                    _parent = p;
                }
                else
                {
                    throw new InvalidOperationException(ErrorText.WrongTypeParentThinkNode);
                }
            }

            if (pawn.TryGetComp<CompAwesomeInventoryLoadout>() is CompAwesomeInventoryLoadout compLoadout)
            {
                if (compLoadout.NeedRestock)
                {
                    foreach (KeyValuePair<ThingGroupSelector, int> pair in compLoadout.ItemsToRestock)
                    {
                        if (pair.Key.AllowedThing.IsWeapon)
                        {
                            ThingWithComps targetThingA =
                                _parent.FindItem(
                                    pawn
                                    , pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
                                    , (thing) => pair.Key.Allows(thing, out _)
                                      &&
                                      !compLoadout.Loadout.IncludedInBlacklist(thing))
                                as ThingWithComps;

                            if (targetThingA != null)
                                return JobMaker.MakeJob(AwesomeInventory_JobDefOf.AwesomeInventory_MapEquip, targetThingA);
                        }
                    }
                }
            }

            return null;
        }
    }
}

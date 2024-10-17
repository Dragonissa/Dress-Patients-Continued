using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DressPatient
{
    [StaticConstructorOnStartup]
    static class Order_DressPatientOrCorpse
    {

        static Order_DressPatientOrCorpse()
        {
            Harmony harmony = new Harmony("eagle0600.dressPatients");
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), prefix: null,
                postfix: new HarmonyMethod(typeof(Order_DressPatientOrCorpse), nameof(DressPatientFloatMenuOption)));
        }

        public static TargetingParameters TargetParametersBody
        {
            get
            {
                if (targetParametersBody == null)
                {
                    targetParametersBody = new TargetingParameters()
                    {
                        canTargetPawns = true,
                        canTargetItems = true,
                        canTargetMutants = false,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        validator = (target => {
                            if (!target.HasThing)
                                return false;

                            if (target.Thing is Pawn pawn)
                            {
                                return pawn.apparel != null && pawn.IsPatient();
                            }

                            //Blame Thathitmann
                            if (!DressPatientUtility.IsHumanCorpse(target.Thing, out Pawn deadPawn)) return false;
                            return deadPawn.apparel != null;
                        })
                    };
                }
                return targetParametersBody;
            }
        }

        private static TargetingParameters targetParametersBody = null;

        public static TargetingParameters TargetParemetersApparel(LocalTargetInfo targetBody)
        {
            return new TargetingParameters()
            {
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = (target =>
                {
                    if (!target.HasThing)
                        return false;
                    Apparel apparel = target.Thing as Apparel;
                    if (apparel == null)
                        return false;
                    if (!targetBody.HasThing)
                    {
                        Log.ErrorOnce("Attempted to find apparel to dress nonexistent body.", 972365687);
                        return false;
                    }
                    Pawn targetPawn = targetBody.Thing as Pawn;
                    if (targetBody.Thing is Corpse targetCorpse)
                        targetPawn = targetCorpse.InnerPawn;
                    if (targetPawn == null)
                    {
                        Log.ErrorOnce("Attempted to find apparel to dress nonexistent body.", 972365687);
                        return false;
                    }
                    if (!ApparelUtility.HasPartsToWear(targetPawn, apparel.def))
                        return false;

                    // If using Humanoid Alien Races 2.0, ensure that the target pawn's race can wear the apparel.
                    // Otherwise, default to true, and try not to generate any errors.
                    if (DressPatientUtility.usingHumanoidAlienRaces)
                    {
                        try
                        {
                            bool canWear = true;
                            ((Action)(() =>
                            {
                                bool CheckRaceCanWear()
                                {
                                    return AlienRace.RaceRestrictionSettings.CanWear(apparel.def, targetPawn.def);
                                }
                                if (!CheckRaceCanWear())
                                    canWear = false;
                            }))();
                            if (!canWear)
                                return false;
                        }
                        catch (TypeLoadException) { }
                    }
                    return true;
                })
            };
        }

        private static void DressPatientFloatMenuOption(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            // If the pawn in question cannot take jobs, don't bother.
            if (pawn.jobs == null)
                return;

            
            // Find valid patients.
            foreach (LocalTargetInfo targetBody in GenUI.TargetsAt(clickPos, TargetParametersBody))
            {

                // Ensure target is reachable.
                if (!pawn.CanReach(targetBody, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    //option = new FloatMenuOption("CannotDress".Translate(targetBody.Thing.LabelCap, targetBody.Thing) + " (" + "NoPath".Translate() + ")", null);
                    continue;
                }
                
                //Thathitmann did this
                //Skip if nothing is there
                //Ensure pawn is either patient or human corpse
                if (targetBody.Thing is Pawn targetPawn)
                {
                    //Skip non-patient pawns
                    if (!targetPawn.IsPatient()) continue;
                }
                else if (targetBody.Thing is Corpse corpse)
                {
                    //Skip non-human corpses (neither ghoul nor animal nor mechanoid)
                    if (!corpse.IsHumanCorpse(out _)) continue;
                    targetPawn = corpse.InnerPawn;
                }
                else
                {
	                //Skip if it's neither a pawn nor a corpse
	                continue;
                }
                
                
                // Add menu option to dress patient. User will be asked to select a target.
                FloatMenuOption option = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("DressOther".Translate(targetBody.Thing.LabelCap, targetBody.Thing), delegate()
                {
                    Find.Targeter.BeginTargeting(TargetParemetersApparel(targetBody), targetApparel =>
                    {
	                    Thing apparel = targetApparel.Thing;
                        apparel.SetForbidden(false);
                        if (!ApparelUtility.HasPartsToWear(targetPawn, apparel.def) || !((Apparel)apparel).PawnCanWear(targetPawn, true))
                        {
	                        //Do nothing and error if pawn cannot wear
	                        Messages.Message("CannotDressIncompatibleApparrel".Translate(targetPawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                        }
                        else if (CompBiocodable.IsBiocoded(apparel) && !CompBiocodable.IsBiocodedFor(apparel, targetPawn))
                        {
	                        //Do nothing and error if clothing is biocoded to someone else
	                        Messages.Message("CannotDressBiocoded".Translate(targetPawn.Name.ToStringShort), MessageTypeDefOf.RejectInput);
                        }
                        else
                        {
	                        pawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("DressPatient"), targetBody, targetApparel));
                        }
                    });
                }, MenuOptionPriority.High), pawn, targetBody);
                opts.Add(option);
            }
        }
    }
}

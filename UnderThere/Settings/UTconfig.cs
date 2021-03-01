using Mutagen.Bethesda;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
using Mutagen.Bethesda.Synthesis.Settings;

namespace UnderThere.Settings
{
    public class UTconfig
    {
        public const string Poor = "Poor";
        public const string Medium = "Medium";
        public const string Rich = "Rich";

        [SynthesisOrder]
        public AssignmentMode AssignmentMode { get; set; } = AssignmentMode.Faction;

        [SynthesisOrder]
        public bool PatchMales { get; set; } = true;

        [SynthesisOrder]
        public bool PatchFemales { get; set; } = true;

        [SynthesisOrder]
        public bool PatchNakedNPCs { get; set; } = true;

        [SynthesisOrder]
        public bool PatchSummonedNPCs { get; set; }

        [SynthesisOrder]
        public bool PatchGhosts { get; set; } = true;

        [SynthesisOrder]
        public bool MakeItemsEquippable { get; set; } = true;

        [SynthesisOrder]
        public HashSet<FormLink<IRaceGetter>> PatchableRaces { get; set; } = new()
        #region Defaults
        {
            Skyrim.Race.HighElfRace,
            Skyrim.Race.ArgonianRace,
            Skyrim.Race.WoodElfRace,
            Skyrim.Race.BretonRace,
            Skyrim.Race.DarkElfRace,
            Skyrim.Race.ImperialRace,
            Skyrim.Race.KhajiitRace,
            Skyrim.Race.NordRace,
            Skyrim.Race.OrcRace,
            Skyrim.Race.RedguardRace,
            Skyrim.Race.ElderRace,
            Skyrim.Race.HighElfRaceVampire,
            Skyrim.Race.ArgonianRaceVampire,
            Skyrim.Race.WoodElfRaceVampire,
            Skyrim.Race.BretonRaceVampire,
            Skyrim.Race.DarkElfRaceVampire,
            Skyrim.Race.ImperialRaceVampire,
            Skyrim.Race.KhajiitRaceVampire,
            Skyrim.Race.NordRaceVampire,
            Skyrim.Race.OrcRaceVampire,
            Skyrim.Race.RedguardRaceVampire,
            Skyrim.Race.ElderRaceVampire,
        };
        #endregion

        [SynthesisOrder]
        public HashSet<FormLink<IRaceGetter>> NonPatchableRaces { get; set; } = new()
        #region Defaults
        {
            Skyrim.Race.DraugrRace,
            Dragonborn.Race.DLC2HulkingDraugrRace,
            Skyrim.Race.FalmerRace,
            Skyrim.Race.HorseRace,
            Skyrim.Race.SkeletonRace,
            Skyrim.Race.SkeletonNecroRace,
            Skyrim.Race.FrostbiteSpiderRace,
            Dragonborn.Race.DLC2ExpSpiderBaseRace,
            Skyrim.Race.DragonRace,
            Skyrim.Race.DragonPriestRace,
            Dragonborn.Race.DLC2AcolyteDragonPriestRace,
            Dragonborn.Race.DLC2DremoraRace,
            Skyrim.Race.ManakinRace,
        };
        #endregion

        [SynthesisOrder]
        public Dictionary<string, HashSet<FormLink<IClassGetter>>> ClassDefinitions { get; set; } = new()
        #region Defaults
        {
            { Poor, new HashSet<FormLink<IClassGetter>>()
            {
                Skyrim.Class.CombatThief,
                Skyrim.Class.Prisoner,
                Skyrim.Class.Miner,
                Skyrim.Class.Beggar,
                Skyrim.Class.EncClassBanditMissile,
                Skyrim.Class.EncClassBanditMelee,
                Skyrim.Class.EncClassForsworn,
                Skyrim.Class.EncClassForswornShaman,
                Skyrim.Class.EncClassForswornMissile,
                Skyrim.Class.MQAncientNord
            }
            },
            { Medium, new HashSet<FormLink<IClassGetter>>()
            {
                Skyrim.Class.CombatWarrior1H,
                Skyrim.Class.CombatSpellsword,
                Skyrim.Class.CombatWitchblade,
                Skyrim.Class.CombatMageElemental,
                Skyrim.Class.CombatNightblade,
                Skyrim.Class.CombatScout,
                Skyrim.Class.CombatAssassin,
                Skyrim.Class.CombatRogue,
                Skyrim.Class.CombatRanger,
                Skyrim.Class.CombatMonk,
                Skyrim.Class.VendorFood,
                Skyrim.Class.VendorBlacksmith,
                Skyrim.Class.VendorApothecary,
                Skyrim.Class.VendorFletcher,
                Skyrim.Class.VendorPawnbroker,
                Skyrim.Class.Jailor,
                Skyrim.Class.Citizen,
                Skyrim.Class.Farmer,
                Skyrim.Class.Lumberjack,
                Skyrim.Class.SoldierImperialNotGuard,
                Skyrim.Class.SoldierSonsSkyrimNotGuard,
                Skyrim.Class.CombatWarrior2H,
                Skyrim.Class.CombatBarbarian,
                Skyrim.Class.CombatMageDestruction,
                Skyrim.Class.Blade,
                Skyrim.Class.GuardImperial,
                Skyrim.Class.GuardSonsSkyrim,
                Skyrim.Class.GuardOrc1H,
                Skyrim.Class.EncClassBanditWizard,
                Skyrim.Class.EncClassAlikrMelee,
                Skyrim.Class.EncClassAlikrMissile,
                Skyrim.Class.EncClassAlikrWizard,
                Skyrim.Class.EncClassPenitusOculatus,
                Skyrim.Class.EncClassWerewolf,
                Skyrim.Class.EncClassWerewolfMage,
                Skyrim.Class.EncClassWerewolfBoss,
                Skyrim.Class.TrainerSmithingJourneyman,
                Skyrim.Class.TrainerLightArmorJourneyman,
                Skyrim.Class.TrainerRestorationJourneyman,
                Skyrim.Class.CombatMageNecro,
                Skyrim.Class.TrainerDestructionJourneyman,
                Skyrim.Class.TrainerAlchemyJourneyman,
                Skyrim.Class.TrainerOneHandedJourneyman,
                Skyrim.Class.TrainerAlchemyJourneyman,
                Skyrim.Class.TrainerOneHandedJourneyman,
                Skyrim.Class.TrainerSneakJourneyman,
                Skyrim.Class.TrainerSpeechcraftJourneyman,
                Skyrim.Class.TrainerConjurationJourneyman,
                Skyrim.Class.EncClassDremoraMelee,
                Dragonborn.Class.DLC2EncClassBanditBoss,
                Dragonborn.Class.DLC2dunHaknirClass,
                Dragonborn.Class.DLC2dunKolbjornRalisClass,
                Dragonborn.Class.DLC2csFrea,
                Dragonborn.Class.DLC2NelothClassTrainer,
                Skyrim.Class.EncClassVampire,
                Skyrim.Class.AAAPlayerSpellswordClass,
            }
            },
            { Rich, new HashSet<FormLink<IClassGetter>>()
            {
                Skyrim.Class.CombatSorcerer,
                Skyrim.Class.VendorSpells,
                Skyrim.Class.Bard,
                Skyrim.Class.VendorTailor,
                Skyrim.Class.Priest,
                Skyrim.Class.CombatMageConjurer,
                Skyrim.Class.TrainerSmithingExpert,
                Skyrim.Class.TrainerSmithingMaster,
                Skyrim.Class.TrainerMarksmanJourneyman,
                Skyrim.Class.TrainerMarksmanExpert,
                Skyrim.Class.TrainerMarksmanMaster,
                Skyrim.Class.TrainerOneHandedExpert,
                Skyrim.Class.TrainerOneHandedMaster,
                Skyrim.Class.TrainerTwoHandedExpert,
                Skyrim.Class.TrainerTwoHandedMaster,
                Skyrim.Class.TrainerIllusionExpert,
                Skyrim.Class.TrainerIllusionMaster,
                Skyrim.Class.TrainerBlockExpert,
                Skyrim.Class.TrainerBlockMaster,
                Skyrim.Class.TrainerPickpocketExpert,
                Skyrim.Class.TrainerPickpocketMaster,
                Skyrim.Class.TrainerDestructionExpert,
                Skyrim.Class.TrainerDestructionMaster,
                Skyrim.Class.TrainerHeavyArmorExpert,
                Skyrim.Class.TrainerHeavyArmorMaster,
                Skyrim.Class.TrainerLightArmorExpert,
                Skyrim.Class.TrainerLightArmorMaster,
                Skyrim.Class.TrainerSneakExpert,
                Skyrim.Class.TrainerSneakMaster,
                Skyrim.Class.TrainerSpeechcraftExpert,
                Skyrim.Class.TrainerSpeechcraftMaster,
                Skyrim.Class.TrainerLockpickExpert,
                Skyrim.Class.TrainerLockpickMaster,
                Skyrim.Class.TrainerAlchemyExpert,
                Skyrim.Class.TrainerAlchemyMaster,
                Skyrim.Class.TrainerAlterationExpert,
                Skyrim.Class.TrainerAlterationMaster,
                Skyrim.Class.TrainerConjurationExpert,
                Skyrim.Class.TrainerConjurationMaster,
                Skyrim.Class.TrainerRestorationExpert,
                Skyrim.Class.TrainerRestorationMaster,
                Skyrim.Class.TrainerEnchantingExpert,
                Skyrim.Class.TrainerEnchantingMaster,
                Skyrim.Class.EncClassThalmorWizard,
                Skyrim.Class.EncClassThalmorMissile,
                Skyrim.Class.EncClassThalmorMelee,
                Skyrim.Class.CombatNightingale,
                Skyrim.Class.CWSoldierClass,
                Skyrim.Class.Vigilant1hMeleeClass,
                Skyrim.Class.Vigilant2hMeleeClass,
                Skyrim.Class.GuardOrc2H,
                Skyrim.Class.NPCclassBelrand,
                Dragonborn.Class.DLC2NPCClassTeldryn,
                Dragonborn.Class.DLC2EbonyWarriorClass,
                HearthFires.Class.BYOHHousecarlHjaalmarchClass,
                Dawnguard.Class.DLC1EncClassKatria,
                Skyrim.Class.EncClassDragonPriest,
                Skyrim.Class.EncClassPenitusOculatus,
                Skyrim.Class.TrainerBlockMaster,
                Dragonborn.Class.DLC2EncClassMiraak,
                Dawnguard.Class.DLC1CClassVyrthur,
                Skyrim.Class.CombatMystic,
            }
            }
        };
        #endregion

        [SynthesisOrder]
        public Dictionary<string, HashSet<FormLink<IFactionGetter>>> FactionDefinitions { get; set; } = new()
        #region Defaults
        {
            { Poor, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.BanditFaction,
                Dragonborn.Faction.DLC2TribalWerebearFaction,
                Dragonborn.Faction.DLC2PillarBuilderFaction,
                Dragonborn.Faction.DLC2BanditDialogueFaction,
                Dragonborn.Faction.DLC2CrimeRavenRockFaction,
                Skyrim.Faction.WerewolfFaction,
                Dragonborn.Faction.DLC2RRBeggarFaction,
                Dragonborn.Faction.DLC2WaterStoneSailors,
                Dragonborn.Faction.DLC2dunKolbjornMinerFaction,
                Dragonborn.Faction.DLC2dunFrostmoonWerewolvesFaction,
                Dragonborn.Faction.DLC2dunFrostmoonWerewolvesVendorFaction,
                Skyrim.Faction.JobHostlerFaction,
                Skyrim.Faction.JobMinerFaction,
                Skyrim.Faction.TownCidhnaMinePrisonerFaction,
                Skyrim.Faction.MG02MinerFaction,
                Skyrim.Faction.RiftenRatwayFactionNeutral,
                Skyrim.Faction.MS07BanditFaction,
                Skyrim.Faction.WIThiefFaction,
                Skyrim.Faction.dunBlackreachFalmerServantFaction,
                Skyrim.Faction.dunFellglow_PrisonerFaction,
                Skyrim.Faction.MQ101LokirFaction,
                Skyrim.Faction.RiftenRatwayFactionEnemy
            }
            },
            { Medium, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.JobTrainerFaction,
                Skyrim.Faction.JobBardFaction,
                Skyrim.Faction.JobBlacksmithFaction,
                Skyrim.Faction.JobAnimalTrainerFaction,
                Skyrim.Faction.JobCarriageFaction,
                Skyrim.Faction.JobFarmerFaction,
                Skyrim.Faction.JobFenceFaction,
                Skyrim.Faction.JobFletcherFaction,
                Skyrim.Faction.Favor104QuestGiverFaction,
                Skyrim.Faction.KynesgroveDravyneaFaction,
                Skyrim.Faction.ServicesKynesgroveDravynea,
                Dragonborn.Faction.DLC2RavenRockGuardFaction,
                Dragonborn.Faction.DLC2RRBulwarkFaction,
                Dragonborn.Faction.DLC2ThirskNordFaction,
                Dragonborn.Faction.DLC2NorthernMaidenFaction,
                Dragonborn.Faction.DLC2CultistFaction,
                Skyrim.Faction.WarlockFaction,
                Skyrim.Faction.IsGuardFaction,
                Skyrim.Faction.HunterFaction,
                Skyrim.Faction.WEServicesHunterFaction,
                Dragonborn.Faction.DLC2HunterFaction,
                Dragonborn.Faction.DLC2ExpSpiderEnemyFaction,
                Dragonborn.Faction.DLC2AshSpawnFaction,
                Skyrim.Faction.JobInnServer,
                Skyrim.Faction.JobLumberjackFaction,
                Skyrim.Faction.JobStreetVendorFaction,
                Skyrim.Faction.JobTailorFaction,
                Dragonborn.Faction.DLC2HirelingTeldrynCrimeFaction,
                Dragonborn.Faction.DLC2BendWillImmuneFaction,
                Dragonborn.Faction.DLC2MoragTongFaction,
                Skyrim.Faction.VigilantOfStendarrFaction,
                HearthFires.Faction.BYOHHousecarlFaction,
                Dawnguard.Faction.DLC1HunterFaction,
                Skyrim.Faction.WEFarmerFaction,
                Skyrim.Faction.FishermanFaction,
                Skyrim.Faction.DA14CultistFaction,
                Skyrim.Faction.DunAlftandSullaHostileFaction,
                Skyrim.Faction.DunAlftandUmanaHostileFaction,
                Skyrim.Faction.HirelingJenassaCrimeFaction,
            }
            },
            { Rich, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.JobCourtWizardFaction,
                Skyrim.Faction.JobMerchantFaction,
                Skyrim.Faction.JobTrainerAlterationFaction,
                Dragonborn.Faction.dlc2MerchMerchantFaction,
                Skyrim.Faction.WEServiceMiscMerchant,
                Dragonborn.Faction.DLC2MiraakFaction,
                Skyrim.Faction.ThalmorFaction,
                Skyrim.Faction.JobGuardCaptainFaction,
                Skyrim.Faction.JobHousecarlFaction,
                Skyrim.Faction.JobInnkeeperFaction,
                Skyrim.Faction.JobJarlFaction,
                Skyrim.Faction.JobJewelerFaction,
                Skyrim.Faction.JobJusticiar,
                Skyrim.Faction.JobOrcChiefFaction,
                Skyrim.Faction.JobOrcWiseWomanFaction,
                Skyrim.Faction.JobPriestFaction,
                Skyrim.Faction.JobSpellFaction,
                Skyrim.Faction.JobStewardFaction,
                Dawnguard.Faction.DLC1VampireFaction,
                Skyrim.Faction.MQ304SovngardeHeroFaction,
                Skyrim.Faction.MQAncientHeroFaction,
                Skyrim.Faction.PenitusOculatusFaction,
                Skyrim.Faction.DarkBrotherhoodFaction,
                Dawnguard.Faction.DLC1GeleborFaction,
                Skyrim.Faction.WEDL03Faction,
                Dawnguard.Faction.DLC1SeranaFaction,
                Dawnguard.Faction.DLC1ValericaFaction,
                Skyrim.Faction.TsunFaction,
                Skyrim.Faction.dunLinweFaction,
            }
            }
        };
        #endregion

        [SynthesisOrder]
        public string QualityForNoFaction = Medium;

        [SynthesisOrder]
        public Dictionary<string, HashSet<FormLink<IFactionGetter>>> FallBackFactionDefinitions { get; set; } = new()
        #region Defaults
        {
            { Poor, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.CrimeFactionCidhnaMine,
                Dawnguard.Faction.DLC1VampireFeedNoCrimeFaction,
                Skyrim.Faction.ForswornFaction,
                Skyrim.Faction.DA13AfflictedFaction,
                Skyrim.Faction.MS09NorthwatchPrisonerFaction,
                Skyrim.Faction.HagravenFaction,
                Skyrim.Faction.MS10BloodHorkerFaction,
                Skyrim.Faction.BanditAllyFaction,
                Skyrim.Faction.dunHonningbrewMeaderyCreatureFaction,
                Skyrim.Faction.dunBloatedMan_SindingFaction,
                Skyrim.Faction.VampireThrallFaction,
                Dawnguard.Faction.DLC1ThrallFaction,
                Skyrim.Faction.DA11CannibalFaction
            }
            },
            { Medium, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.TownAngasMillFaction,
                Skyrim.Faction.TownBarleydarkFarmFaction,
                Skyrim.Faction.TownDarkwaterCrossingFaction,
                Skyrim.Faction.TownDragonBridgeFaction,
                Skyrim.Faction.TownDushnikhYalFaction,
                Skyrim.Faction.TownFalkreathFaction,
                Skyrim.Faction.TownHalfMoonMillFaction,
                Skyrim.Faction.TownHeartwoodMillFaction,
                Skyrim.Faction.TownHelgenFaction,
                Skyrim.Faction.TownIrontreeMillFaction,
                Skyrim.Faction.TownIvarsteadFaction,
                Skyrim.Faction.TownKarthwastenFaction,
                Skyrim.Faction.TownKolskeggrMineFaction,
                Skyrim.Faction.TownKynesgroveFaction,
                Skyrim.Faction.TownLargashburFaction,
                Skyrim.Faction.TownLeftHandMineFaction,
                Skyrim.Faction.TownLoreiusFarmFaction,
                Skyrim.Faction.TownMerryfairFarmFaction,
                Skyrim.Faction.TownMorKhazgurFaction,
                Skyrim.Faction.TownMorthalFaction,
                Skyrim.Faction.TownNarzulburFaction,
                Skyrim.Faction.TownOldHroldanFaction,
                Skyrim.Faction.TownRiverwoodFaction,
                Skyrim.Faction.TownRoriksteadFaction,
                Skyrim.Faction.TownSalviusFarmFaction,
                Skyrim.Faction.TownSarethiFarmFaction,
                Skyrim.Faction.TownShorsStoneFaction,
                Skyrim.Faction.TownSnowShodFarmFaction,
                Skyrim.Faction.TownSoljundsSinkholeFaction,
                Skyrim.Faction.TownStonehillsFaction,
                Skyrim.Faction.TownWinterholdFaction,
                Dragonborn.Faction.DLC2CrimeRavenRockFaction,
                Skyrim.Faction.CrimeFactionEastmarch,
                Skyrim.Faction.CrimeFactionFalkreath,
                Skyrim.Faction.CrimeFactionHaafingar,
                Skyrim.Faction.CrimeFactionHjaalmarch,
                Skyrim.Faction.CrimeFactionImperial,
                Skyrim.Faction.CrimeFactionKhajiitCaravans,
                Skyrim.Faction.CrimeFactionNull,
                Skyrim.Faction.CrimeFactionOrcs,
                Skyrim.Faction.CrimeFactionPale,
                Skyrim.Faction.CrimeFactionReach,
                Skyrim.Faction.CrimeFactionRift,
                Skyrim.Faction.CrimeFactionSons,
                Skyrim.Faction.CrimeFactionThievesGuild,
                Skyrim.Faction.CrimeFactionWhiterun,
                Skyrim.Faction.CrimeFactionWinterhold,
                Dragonborn.Faction.DLC2SkaalVillageCitizenFaction,
                Skyrim.Faction.VampireFaction,
                Dawnguard.Faction.SoulCairnSoulFaction,
                Dawnguard.Faction.DLC1_WESC09Faction,
                Skyrim.Faction.CWSonsFaction,
                Skyrim.Faction.CWImperialFaction,
                Skyrim.Faction.NecromancerFaction,
                Skyrim.Faction.SilverHandFaction,
                Skyrim.Faction.DraugrFaction,
                Skyrim.Faction.CarriageSystemFaction,
                Skyrim.Faction.TG09NightingaleEnemyFaction,
                Skyrim.Faction.DunPlayerAllyFaction,
                Skyrim.Faction.DA16VaerminaHostileFaction,
                Skyrim.Faction.POIVolcanicHunterFaction,
                Skyrim.Faction.DA05HuntersOfHircineFaction,
                Skyrim.Faction.MS08AlikrFaction,
                Skyrim.Faction.DraugrAllyFaction,
                Skyrim.Faction.dunKilkreathFaction,
                Skyrim.Faction.SailorFaction,
                Skyrim.Faction.dunBluePalacePelagiusArenaFaction,
                Skyrim.Faction.KarthwastenSilverFishGuards,
                Skyrim.Faction.DragonBridgeFourShieldsInnFaction,
                Skyrim.Faction.DA01MalynVarenFaction,
                Skyrim.Faction.MS06CultistSceneFaction,
                Skyrim.Faction.DunAnsilvundLuahFaction,
                Skyrim.Faction.TGNoPickpocketFaction,
                Dawnguard.Faction.DLC1RuunvaldFaction,
                Skyrim.Faction.MS09PlayerAllyFaction,
                Dawnguard.Faction.DLC1LD_KatriaFaction,
                Skyrim.Faction.dunDeadMensRespiteBardFaction,
                Skyrim.Faction.BanditFriendFaction,
                Skyrim.Faction.dunIronbindAdventurerFaction,
                Skyrim.Faction.dunDawnstarSanctuaryGuardianFaction,
                Skyrim.Faction.dunFrostmereCryptRajirrFaction,
                Dawnguard.Faction.DLC1DexionThrall,
                Skyrim.Faction.FrostRiverFarmFaction,
                Skyrim.Faction.dunFrostmereCryptEisaFaction,
                Skyrim.Faction.WIGenericCrimeFaction,
                Skyrim.Faction.dunBluePalaceFaction,
                Skyrim.Faction.DBRecuringContact1CrimeFaction,
                Skyrim.Faction.DBRecuringContact2CrimeFaction,
                Skyrim.Faction.DBRecuringContact3CrimeFaction,
                Skyrim.Faction.DBRecuringContact4CrimeFaction,
                Skyrim.Faction.DBRecuringContact5CrimeFaction,
                Skyrim.Faction.DBRecuringContact6CrimeFaction,
                Skyrim.Faction.DBRecuringContact7CrimeFaction,
                Skyrim.Faction.DBRecuringContact8CrimeFaction,
                Skyrim.Faction.DBRecuringContact9CrimeFaction,
                Skyrim.Faction.DBRecuringContact10CrimeFaction,
                Skyrim.Faction.DBRecuringTarget1CrimeFaction,
                Skyrim.Faction.DBRecuringTarget2CrimeFaction,
                Skyrim.Faction.DBRecuringTarget3CrimeFaction,
                Skyrim.Faction.DBRecuringTarget4CrimeFaction,
                Skyrim.Faction.DBRecuringTarget5CrimeFaction,
                Skyrim.Faction.DBRecuringTarget6CrimeFaction,
                Skyrim.Faction.DBRecuringTarget7CrimeFaction,
                Skyrim.Faction.DBRecuringTarget8CrimeFaction,
                Skyrim.Faction.DBRecuringTarget9CrimeFaction,
                Skyrim.Faction.DBRecuringTarget10CrimeFaction,
                Skyrim.Faction.FavorExcludedFaction,
                Skyrim.Faction.PotentialFollowerFaction,
            }
            },
            { Rich, new HashSet<FormLink<IFactionGetter>>()
            {
                Skyrim.Faction.TownGoldenglowEstateFaction,
                Skyrim.Faction.TownMarkarthFaction,
                Skyrim.Faction.TownRiftenFaction,
                Skyrim.Faction.TownDawnstarFaction,
                Skyrim.Faction.TownSolitudeFaction,
                Skyrim.Faction.TownWhiterunFaction,
                Skyrim.Faction.TownWindhelmFaction,
                Skyrim.Faction.CrimeFactionGreybeard,
                Skyrim.Faction.MQSovngardeCombatDialogueFaction,
                Skyrim.Faction.WICraftItem02AdditionalEnchanterFaction,
                Skyrim.Faction.dunBluePalaceEnemyFaction,
                Skyrim.Faction.DB09TowerFaction,
                Skyrim.Faction.DA16VaerminaDreamFaction,
                Skyrim.Faction.MarriageLoveInterestWitnessFaction,
                Skyrim.Faction.dunPrisonerFaction,
                Skyrim.Faction.dunEldergleamFaction,
                Skyrim.Faction.DA16PriestFaction,
                Skyrim.Faction.DBAmaundMotierreFaction,
                Skyrim.Faction.MQ201PartyGuestFaction,
                Skyrim.Faction.MGThalmorFaction
            }
            }
        };
        #endregion

        [SynthesisOrder]
        public string QualityForNoFactionFallback = string.Empty;

        [SynthesisOrder]
        public HashSet<FormLink<IFactionGetter>> IgnoreFactionsWhenScoring { get; set; } = new HashSet<FormLink<IFactionGetter>>()
        #region Defaults
        {
            Skyrim.Faction.PotentialMarriageFaction,
            Skyrim.Faction.TownKynesgroveFaction,
            Skyrim.Faction.CurrentHireling,
            Skyrim.Faction.PotentialFollowerFaction,
            Skyrim.Faction.PotentialHireling,
            Dragonborn.Faction.DLC2MQ02Faction,
            Skyrim.Faction.WINeverFillAliasesFaction,
            Dragonborn.Faction.DLC2ApocryphaFaction,
            Dragonborn.Faction.DLC2SV02ThalmorCrimeFaction,
            Dragonborn.Faction.DLC2ServicesThirskHalbarn
        };
        #endregion

        [SynthesisOrder]
        public List<NPCassignment> SpecificNPCs { get; set; } = new List<NPCassignment>();

        [SynthesisOrder]
        public List<NPCassignment> BlockedNPCs { get; set; } = new List<NPCassignment>();

        [SynthesisOrder]
        public UTSet DefaultSet = new UTSet()
        #region Defaults
        {
            Name = "Default Variant",
            Items = new List<UTitem>()
            {
                new UTitem()
                {
                     DispName = "Undergarment",
                     IsBottom = true,
                     Weight = 0.5f,
                     Value = 25,
                     Gender = GenderTarget.Mutual,
                     Record = underwearforeveryone.Armor.UFE_UnderwearBottom
                },
                new UTitem()
                {
                    DispName = "Undergarment",
                    Weight = 0.5f,
                    Value = 25,
                    Gender = GenderTarget.Female,
                    Record = underwearforeveryone.Armor.UFE_UnderwearTop
                }
            }
        };
        #endregion

        [SynthesisOrder]
        public List<UTSet> Sets { get; set; } = new()
        #region Defaults
        {
            new UTSet()
            {
                Name = "Poor Variant 1",
                Category = Poor,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_0,
                    },
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_0,
                    },
                },
            },
            new UTSet()
            {
                Name = "Poor Variant 2",
                Category = Poor,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_1,
                    },
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_1,
                    },
                },
            },
            new UTSet()
            {
                Name = "Poor Variant 3",
                Category = Poor,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_2,
                    },
                    new UTitem()
                    {
                        DispName = "Cheap Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_2,
                    },
                },
            },
            new UTSet()
            {
                Name = "Medium Variant 1",
                Category = Medium,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_3,
                    },
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_3,
                    },
                },
            },
            new UTSet()
            {
                Name = "Medium Variant 2",
                Category = Medium,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_4,
                    },
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_4,
                    },
                },
            },
            new UTSet()
            {
                Name = "Medium Variant 3",
                Category = Medium,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_5,
                    },
                    new UTitem()
                    {
                        DispName = "Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_5,
                    },
                },
            },
            new UTSet()
            {
                Name = "Rich Variant 1",
                Category = Rich,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_6,
                    },
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_6,
                    },
                },
            },
            new UTSet()
            {
                Name = "Rich Variant 2",
                Category = Rich,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_7,
                    },
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_7,
                    },
                },
            },
            new UTSet()
            {
                Name = "Rich Variant 3",
                Category = Rich,
                Items = new List<UTitem>()
                {
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = true,
                        Gender = GenderTarget.Mutual,
                        Record = underwearforeveryone.Armor.UFE_UnderwearBottom_8,
                    },
                    new UTitem()
                    {
                        DispName = "Fine Undergarment",
                        IsBottom = false,
                        Gender = GenderTarget.Female,
                        Record = underwearforeveryone.Armor.UFE_UnderwearTop_8,
                    },
                },
            },
        };
        #endregion
        public IEnumerable<UTSet> AllSets => Sets.And(DefaultSet);


        [SynthesisOrder]
        public bool VerboseMode { get; set; }

        [SynthesisOrder]
        public int RandomSeed { get; set; } = 1753;
    }
}

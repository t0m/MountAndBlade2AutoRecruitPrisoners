using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using MountAndBlade.CampaignBehaviors;
using System.Collections.Generic;

namespace AutoRecruitPrisoners
{

    public class AutoRecruitPrisonersCampaignBehavior : CampaignBehaviorBase, ICampaignBehavior
    {

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
        }

        public override void SyncData(IDataStore dataStore)
        {
            return;
        }

        private void DailyTick()
        {
            MobileParty mainParty = MobileParty.MainParty;
            TroopRoster memberRoster = mainParty.MemberRoster;
            if (memberRoster.TotalManCount >= mainParty.Party.PartySizeLimit)
            {
                return;
            }

            TroopRoster prisonRoster = mainParty.PrisonRoster;
            if (prisonRoster.TotalManCount == 0)
            {
                return;
            }

            IRecruitPrisonersCampaignBehavior recruitPrisonerBehavior = Campaign.Current.GetCampaignBehavior<IRecruitPrisonersCampaignBehavior>();
            if (recruitPrisonerBehavior == null)
            {
                return;
            }

            List<Tuple<CharacterObject, int>> recruitablePrisoners = new List<Tuple<CharacterObject, int>>();
            for (int i = 0; i < prisonRoster.Count; i++)
            {

                CharacterObject prisoner = prisonRoster.GetCharacterAtIndex(i);
                int numRecruitable = recruitPrisonerBehavior.GetRecruitableNumber(prisoner);

                if (numRecruitable > 0)
                {
                    recruitablePrisoners.Add(new Tuple<CharacterObject, int>(prisoner, numRecruitable));
                }
            }

            recruitablePrisoners.Sort((x, y) => y.Item1.Tier.CompareTo(x.Item1.Tier));

            for (int i=0; i<recruitablePrisoners.Count; i++)
            {
                CharacterObject prisoner = recruitablePrisoners[i].Item1;
                int numRecruitable = recruitablePrisoners[i].Item2;
                while (numRecruitable > 0)
                {
                    recruitPrisonerBehavior.SetRecruitableNumber(prisoner, --numRecruitable);
                    prisonRoster.AddToCounts(prisoner, -1, false, 0, 0, true, -1);
                    mainParty.MemberRoster.AddToCounts(prisoner, 1, false, 0, 0, true, -1);
                    if (memberRoster.TotalManCount >= mainParty.Party.PartySizeLimit)
                    {
                        break;
                    }
                }
                if (memberRoster.TotalManCount >= mainParty.Party.PartySizeLimit)
                {
                    break;
                }
            }
        }

    }

    public class Main : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            if (!(game.GameType is Campaign))
                return;

            CampaignGameStarter campaignGame = (CampaignGameStarter) gameStarter;
            campaignGame.AddBehavior(new AutoRecruitPrisonersCampaignBehavior());
        }

    }
}

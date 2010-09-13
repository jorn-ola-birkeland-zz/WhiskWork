#region

using System;
using System.Collections.Specialized;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;
using WhiskWork.Synchronizer;

#endregion

namespace WhiskWork.Synchronization.UnitTest
{
    [TestClass]
    public class ChangeRequestSynchronizerTest : EManagerSynchronizerTestBase
    {
        [TestMethod]
        public void ShouldNotDeleteFromWhiskWorkIfItStartWithB()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                    {
                        var cr =
                            WorkItem.New("2404", "/cmsdev/analysis/inprocess").AddClass("cr").UpdateOrdinal(123).
                                UpdateProperties(CreateProperties());
                        var bug = WorkItem.New("B9765", "/done").AddClass("cr");
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {bug, cr});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());
                    });

            Playback();
        }

        [TestMethod]
        public void ShouldUpdateResponsible()
        {
            StubWhiskWorkAndMockDomino();

            var properties = CreateProperties();
            properties.Add("responsible", "Some Person");

            Record(
                () =>
                {
                    var currentWorkItem =
                        WorkItem.New("2404", "/cmsdev/analysis/inprocess").AddClass("cr").UpdateOrdinal(123).
                            UpdateProperties(properties);
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] { currentWorkItem });
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());

                    DominoRepository.UpdateField("unid1", "CurrentPerson", "Some Person");
                });

            Playback();
        }

        [TestMethod, Ignore]
        public void ShouldReverseSynchronizeStatus()
        {
            StubDominoAndMockWhiskWork();

            DateTime now = DateTime.Now;

            Record(
                () =>
                {
                    var cr =
                        WorkItem.New("2404", "/cmsdev/scheduled").AddClass("cr").UpdateOrdinal(123).UpdateProperties(
                            CreateProperties()).UpdateLastMoved(now);
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] { cr });
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData(now.AddSeconds(1)));
                    WhiskWorkRepository.PostWorkItem(cr.UpdateLastMoved(now.AddSeconds(1)));
                });

            Playback();

        }


        protected override EManagerWhiskWorkSynchronizer CreateSynchronizer(IWhiskWorkRepository whiskWorkRepository,
                                                                            IDominoRepository dominoRepository)
        {
            return new ChangeRequestSynchronizer(whiskWorkRepository, dominoRepository);
        }

        private static DataTable CreateDominoData()
        {
            return CreateDominoData(DateTime.MinValue);
        }

        private static DataTable CreateDominoData(DateTime timeStamp)
        {
            var dominoData = new DataTable();
            dominoData.Columns.Add("Release");
            dominoData.Columns.Add("LeanStatus");
            dominoData.Columns.Add("Team");
            dominoData.Columns.Add("Id");
            dominoData.Columns.Add("Title");
            dominoData.Columns.Add("Project");
            dominoData.Columns.Add("Unid");
            dominoData.Columns.Add("Status");
            dominoData.Columns.Add("Ordinal");
            dominoData.Columns.Add("Person");
            dominoData.Columns.Add("TimeStamp");

            var timeStampText = DominoFormatDateTime(timeStamp);

            dominoData.Rows.Add("Release20091212", "Lean1", "Team1", "2404", "A cr", "proj1", "unid1", "2 - Development", "123", "A Person",timeStampText);
            return dominoData;
        }

        private static NameValueCollection CreateProperties()
        {
            return new NameValueCollection
                       {
                        {"name", "2404"},
                        {"unid", "unid1"},
                        {"title", "A cr"},
                        {"team", "Team1"},
                        {"release", "Release20091212"},
                        {"project", "proj1"},
                        {"leanstatus","Lean1"},
                        {"priority","123"},
                       };
        }


    }
}
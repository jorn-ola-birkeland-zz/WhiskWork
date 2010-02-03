using System;
using System.Collections.Specialized;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;
using WhiskWork.Synchronizer;
using System.Globalization;

namespace WhiskWork.Synchronization.UnitTest
{
    [TestClass]
    public class BugSynchronizerTest : EManagerSynchronizerTestBase
    {
        [TestMethod]
        public void ShouldCreateWorkItemAndMoveItToCorrectStep()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                    {
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new WorkItem[0]);
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());

                        WhiskWorkRepository.PostWorkItem(WorkItem.New("B2404", "/cmsdev/scheduled"));
                        WhiskWorkRepository.PostWorkItem(WorkItem.New("B2404", "/cmsdev/analysis/inprocess"));
                    }
                );

            Playback();
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemWithIdZero()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                {
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new WorkItem[0]);
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData("0"));
                }
                );

            Playback();
            
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemWithWEmptyId()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                {
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new WorkItem[0]);
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData(string.Empty));
                }
                );

            Playback();
        }


        [TestMethod]
        public void ShouldUpdateProperties()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                    {
                        var currentWorkItem = WorkItem.New("B2404", "/cmsdev/analysis/inprocess");
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[]
                                                                                       {currentWorkItem.AddClass("cr")});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());


                        WhiskWorkRepository.PostWorkItem(
                            currentWorkItem.UpdateProperties(CreateProperties()).UpdateOrdinal(-3));
                    });

            Playback();
        }

        [TestMethod]
        public void ShouldUpdateOrdinalToMinusOneWhenPriorityIsThree()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                {
                    var currentWorkItem = WorkItem.New("B2404", "/cmsdev/analysis/inprocess");
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] { currentWorkItem.AddClass("cr") });
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData("2404","3"));


                    WhiskWorkRepository.PostWorkItem(
                        currentWorkItem.UpdateProperties(CreateProperties("B2404", "3")).UpdateOrdinal(-1));
                });

            Playback();
        }


        [TestMethod]
        public void ShouldUpdateDominoStatus()
        {
            StubWhiskWorkAndMockDomino();

            Record(
                () =>
                    {
                        var bug =
                            WorkItem.New("B2404", "/done").AddClass("cr").UpdateOrdinal(1).UpdateProperties(
                                CreateProperties());
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {bug});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());

                        DominoRepository.UpdateField("unid1", "Status", "10");
                    });

            Playback();
        }

        [TestMethod]
        public void ShouldNotUpdateDominoStatusWhenWorkItemLastMovedIsOlderThanDominoTimestamp()
        {
            StubWhiskWorkAndMockDomino();

            DateTime now = DateTime.Now;

            Record(
                () =>
                {
                    var bug =
                        WorkItem.New("B2404", "/done").AddClass("cr").UpdateOrdinal(1).UpdateProperties(
                            CreateProperties()).UpdateLastMoved(now);
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] { bug });
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData(now.AddSeconds(1)));
                });

            Playback();
        }

        [TestMethod]
        public void ShouldUpdateWhiskWorkStatusWhenWorkItemLastMovedIsOlderThanDominoTimestamp()
        {
            StubDominoAndMockWhiskWork();

            DateTime now = DateTime.Now;

            Record(
                () =>
                {
                    var bug =
                        WorkItem.New("B2404", "/cmsdev/scheduled").AddClass("cr").UpdateOrdinal(-3).UpdateProperties(
                            CreateProperties()).UpdateLastMoved(now);
                    SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] { bug });
                    SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData(now.AddSeconds(1)));

                    WhiskWorkRepository.PostWorkItem(WorkItem.New("B2404","/cmsdev/analysis/inprocess"));
                });

            Playback();
        }



        [TestMethod]
        public void ShouldNotUpdateDominoIfIdDoesntStartWithB()
        {
            StubWhiskWorkAndMockDomino();

            Record(
                () =>
                    {
                        var cr = WorkItem.New("9765", "/done");
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {cr});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());
                    });

            Playback();
        }

        [TestMethod]
        public void ShouldDeleteExtraWorkItemFromWhiskWork()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                    {
                        var bug =
                            WorkItem.New("B2404", "/cmsdev/analysis/inprocess").AddClass("cr").UpdateOrdinal(-3).
                                UpdateProperties(CreateProperties());
                        var removedBug = WorkItem.New("B2405", "/done").AddClass("cr");
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {bug, removedBug});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());

                        WhiskWorkRepository.DeleteWorkItem(removedBug.RemoveClass("cr"));
                    });

            Playback();
        }


        [TestMethod]
        public void ShouldNotDeleteFromWhiskWorkIfIdDoesntStartWithB()
        {
            StubDominoAndMockWhiskWork();

            Record(
                () =>
                    {
                        var bug =
                            WorkItem.New("B2404", "/cmsdev/analysis/inprocess").AddClass("cr").UpdateOrdinal(-3).
                                UpdateProperties(CreateProperties());
                        var cr = WorkItem.New("9765", "/done").AddClass("cr");
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {bug, cr});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());
                    });

            Playback();
        }
        

        [TestMethod]
        public void ShouldNotUpdateResponsible()
        {
            StubWhiskWorkAndMockDomino();

            var properties = CreateProperties();
            properties.Add("responsible","Some Person");

            Record(
                () =>
                    {
                        var currentWorkItem =
                            WorkItem.New("B2404", "/cmsdev/analysis/inprocess").AddClass("cr").UpdateOrdinal(1).
                                UpdateProperties(properties);
                        SetupResult.For(WhiskWorkRepository.GetWorkItems()).Return(new[] {currentWorkItem});
                        SetupResult.For(DominoRepository.OpenTable()).Return(CreateDominoData());
                    });

            Playback();
        }

        protected override EManagerWhiskWorkSynchronizer CreateSynchronizer(IWhiskWorkRepository whiskWorkRepository, IDominoRepository dominoRepository)
        {
            return new BugSynchronizer(whiskWorkRepository, dominoRepository);
        }

        private static DataTable CreateDominoData()
        {
            return CreateDominoData("2404","1", DateTime.MinValue);
        }

        private static DataTable CreateDominoData(string id)
        {
            return CreateDominoData(id, "1",DateTime.MinValue);
        }

        private static DataTable CreateDominoData(DateTime timeStamp)
        {
            return CreateDominoData("2404", "1", timeStamp);
        }

        private static DataTable CreateDominoData(string id, string priority)
        {
            return CreateDominoData(id, priority, DateTime.MinValue);
        }

        private static DataTable CreateDominoData(string id, string priority, DateTime timeStamp)
        {
            var dominoData = new DataTable();
            dominoData.Columns.Add("Release");
            dominoData.Columns.Add("ApplicationId");
            dominoData.Columns.Add("Id");
            dominoData.Columns.Add("Title");
            dominoData.Columns.Add("Unid");
            dominoData.Columns.Add("Status");
            dominoData.Columns.Add("Priority");
            dominoData.Columns.Add("Severity");
            dominoData.Columns.Add("TimeStamp");

            string timeStampText = DominoFormatDateTime(timeStamp);

            dominoData.Rows.Add("Release20091212", "1", id, "A bug", "unid1", "2", priority, "1", timeStampText);
            return dominoData;
        }

        private static NameValueCollection CreateProperties()
        {
            return CreateProperties("B2404","1");
        }

        private static NameValueCollection CreateProperties(string name, string priority)
        {
            return new NameValueCollection
                       {
                           {"name",name},
                           {"unid","unid1"},
                           {"title","A bug"},
                           {"applicationid","1"},
                           {"release","Release20091212"},
                           {"severity","1"},
                           {"priority",priority},
                           {"type","bug"}
                       };
        }


    }

}
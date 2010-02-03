#region

using System;
using System.Collections.Generic;
using System.Data;
using WhiskWork.Core.Synchronization;

#endregion

namespace WhiskWork.Synchronizer
{
    internal class EManagerSynchronizationAgent : ISynchronizationAgent
    {
        private readonly IDominoRepository _dominoRepository;
        private readonly Converter<DataRow, SynchronizationEntry> _mapper;

        public EManagerSynchronizationAgent(IDominoRepository dominoRepository, Converter<DataRow,SynchronizationEntry> mapper)
        {
            _dominoRepository = dominoRepository;
            _mapper = mapper;
        }

        public string DataViewUrl { get; set; }

        #region ISynchronizationAgent Members

        public IEnumerable<SynchronizationEntry> GetAll()
        {
            DataTable table = _dominoRepository.OpenTable();

            var entries = new List<SynchronizationEntry>();
            foreach (DataRow row in table.Rows)
            {
                SynchronizationEntry entry = _mapper(row);
                if(entry!=null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public void UpdateStatus(SynchronizationEntry entry)
        {
            var unid = entry.Properties["unid"];
            
            _dominoRepository.UpdateField(unid,"Status",entry.Status);
        }

        public void Create(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void Delete(SynchronizationEntry entry)
        {
            throw new NotImplementedException();
        }

        public void UpdateData(SynchronizationEntry entry)
        {
            var unid = entry.Properties["unid"];

            foreach (var keyValue in entry.Properties)
            {
                if(keyValue.Key=="unid")
                {
                    continue;
                }

                _dominoRepository.UpdateField(unid,keyValue.Key,keyValue.Value);
            }
        }

        #endregion

    }
}
using System;
using System.Data;

namespace WhiskWork.Synchronizer
{
    internal class ReadOnlyDominoRepository : IDominoRepository
    {
        private readonly IDominoRepository _innerRepository;

        public ReadOnlyDominoRepository(IDominoRepository innerRepository)
        {
            _innerRepository = innerRepository;
        }

        public void UpdateField(string unid, string fieldName, string value)
        {
            Console.WriteLine("Dry run. UpdateField unid='{0}', fieldName='{1}', value='{2}' ",unid,fieldName,value);
        }

        public DataTable OpenTable()
        {
            return _innerRepository.OpenTable();
        }
    }
}
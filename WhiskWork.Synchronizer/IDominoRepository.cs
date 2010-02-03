using System.Data;

namespace WhiskWork.Synchronizer
{
    public interface IDominoRepository
    {
        void UpdateField(string unid, string fieldName, string value);
        DataTable OpenTable();
    }
}
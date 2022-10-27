using HC.Patient.Data;
using HC.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using HC.Patient.Entity;
using HC.Patient.Repositories.IRepositories;
using HC.Model;
using System.Linq;

namespace HC.Patient.Repositories.Repositories
{
    public class TelehealthRecordingRepository : RepositoryBase<TelehealthRecordingDetail>, ITelehealthRecordingRepository
    {
        private HCOrganizationContext _context;
        public TelehealthRecordingRepository(HCOrganizationContext context) : base(context)
        {
            this._context = context;
        }
        public TelehealthRecordingDetail SaveVideoArchived(TelehealthRecordingDetail telehealthRecordingDetail, TokenModel tokenModel)
        {
            _context.TelehealthRecordingDetails.Add(telehealthRecordingDetail);
            if (_context.SaveChanges() > 0)
            {
                return telehealthRecordingDetail;
            }
            else
                return null;
        }
        public TelehealthRecordingDetail GetVideoArchivedDetail(string archiveId, TokenModel tokenModel)
        {
            return _context.TelehealthRecordingDetails.Where(x => x.ArchiveId == archiveId && x.OrganizationId == tokenModel.OrganizationID && x.IsDeleted == false && x.IsActive == true).FirstOrDefault();

        }
    }
}

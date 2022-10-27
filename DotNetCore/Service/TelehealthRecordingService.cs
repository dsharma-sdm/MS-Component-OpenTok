using HC.Model;
using HC.Patient.Repositories.IRepositories;
using HC.Service;
using System;
using System.Collections.Generic;
using System.Text;
using HC.Patient.Entity;
using HC.Patient.Service.IServices;
using HC.Common.HC.Common;
using System.Net;

namespace HC.Patient.Service.Services
{
    public class TelehealthRecordingService : BaseService, ITelehealthRecordingService
    {
        private readonly ITelehealthRecordingRepository _telehealthRecordingRepository;

        public TelehealthRecordingService(ITelehealthRecordingRepository telehealthRecordingRepository)
        {
            _telehealthRecordingRepository = telehealthRecordingRepository;
        }
        public JsonModel SaveVideoArchived(string archivedId, int sessionId, TokenModel tokenModel)
        {
            var telehealthRecording = _telehealthRecordingRepository.GetVideoArchivedDetail(archivedId, tokenModel);
            if (telehealthRecording == null)
            {
                TelehealthRecordingDetail telehealthRecordingDetail = new TelehealthRecordingDetail()
                {
                    ArchiveId = archivedId,
                    CreatedBy = tokenModel.UserID,
                    CreatedDate = DateTime.UtcNow,
                    OrganizationId = tokenModel.OrganizationID,
                    IsActive = true,
                    IsDeleted = false,
                    TelehealthSessionDetailID = sessionId
                };

                var response = _telehealthRecordingRepository.SaveVideoArchived(telehealthRecordingDetail, tokenModel);
                if (response == null)
                    return new JsonModel(null, StatusMessage.CallRecordingNotSaved, (int)HttpStatusCode.InternalServerError);
                return new JsonModel(response, StatusMessage.CallRecordingSaved, (int)HttpStatusCode.OK);
            }
            else
                return new JsonModel(telehealthRecording, StatusMessage.CallRecordingExisted, (int)HttpStatusCode.Created);
        }
    }
}

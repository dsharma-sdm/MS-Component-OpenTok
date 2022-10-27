using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using HC.Model;
using HC.Patient.Data;
using HC.Patient.Entity;
using HC.Patient.Model.MasterData;
using HC.Patient.Repositories.IRepositories.Telehealth;
using HC.Repositories;
using static HC.Common.Enums.CommonEnum;

namespace HC.Patient.Repositories.Repositories.Telehealth
{
    public class TelehealthRepository : RepositoryBase<TelehealthSessionDetails>, ITelehealthRepository
    {
        private HCOrganizationContext _context;
        public TelehealthRepository(HCOrganizationContext context) : base(context)
        {
            this._context = context;
        }

        public TelehealthSessionDetails GetTelehealthSession(int patientID, int staffID, DateTime startTime, DateTime endTime)
        {
            try
            {
                TelehealthSessionDetails telehealthSessionDetails = _context.TelehealthSessionDetails
                .Where(k => k.IsActive == true && k.IsDeleted == false && k.PatientID == patientID &&
                k.StaffId == staffID
                && k.StartTime == startTime && k.EndTime == endTime
                ).FirstOrDefault();
                return telehealthSessionDetails;
            }
            catch (Exception ex)
            {
                TelehealthSessionDetails telehealthSessionDetails = new TelehealthSessionDetails();
                telehealthSessionDetails.exception = ex;
                return telehealthSessionDetails;
            }
        }

        public TelehealthTokenDetails GetTelehealthToken(int id, TokenModel tokenModel)
        {
            try
            {
                UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                {
                    TelehealthTokenDetails telehealthTokenDetails = _context.TelehealthTokenDetails
                        .Where(k => k.IsActive == true && k.IsDeleted == false && k.TelehealthSessionDetailID == id && k.IsStaffToken == false && k.InvitationId == null).FirstOrDefault();
                    return telehealthTokenDetails;
                }
                else if (userroles.UserType.ToLower() == UserTypeEnum.DEPENDENT.ToString().ToLower())
                    {
                        TelehealthTokenDetails telehealthTokenDetails = _context.TelehealthTokenDetails
                            .Where(k => k.IsActive == true && k.IsDeleted == false && k.TelehealthSessionDetailID == id && k.IsStaffToken == false && k.InvitationId == null).FirstOrDefault();
                        return telehealthTokenDetails;
                    }
                else
                {
                    TelehealthTokenDetails telehealthTokenDetails = _context.TelehealthTokenDetails
                        .Where(k => k.IsActive == true && k.IsDeleted == false && k.TelehealthSessionDetailID == id && k.IsStaffToken == true && k.InvitationId == null).FirstOrDefault();
                    return telehealthTokenDetails;
                }
            }
            catch (Exception ex)
            {
                TelehealthTokenDetails telehealthTokenDetails = new TelehealthTokenDetails();
                telehealthTokenDetails.exception = ex;
                return telehealthTokenDetails;
            }
        }

        public TelehealthTokenDetails GetTelehealthToken(int sessionId, Common.Enums.CommonEnum.UserTypeEnum userTypeEnum, TokenModel tokenModel)
        {
            try
            {
                // UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                if (userTypeEnum.ToString().ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                {
                    TelehealthTokenDetails telehealthTokenDetails = _context.TelehealthTokenDetails
                        .Where(k => k.IsActive == true && k.IsDeleted == false && k.TelehealthSessionDetailID == sessionId && k.IsStaffToken == false && k.InvitationId == null).FirstOrDefault();
                    return telehealthTokenDetails;
                }
                else
                {
                    TelehealthTokenDetails telehealthTokenDetails = _context.TelehealthTokenDetails
                        .Where(k => k.IsActive == true && k.IsDeleted == false && k.TelehealthSessionDetailID == sessionId && k.IsStaffToken == true && k.InvitationId == null).FirstOrDefault();
                    return telehealthTokenDetails;
                }
            }
            catch (Exception ex)
            {
                TelehealthTokenDetails telehealthTokenDetails = new TelehealthTokenDetails();
                telehealthTokenDetails.exception = ex;
                return telehealthTokenDetails;
            }
        }

        public TelehealthTokenDetails CreateTelehealthToken(int telehealthSessionDetailID, string token, double duration, TokenModel tokenModel, int? invitationId = null)
        {
            using (var transaction = _context.Database.BeginTransaction()) //TO DO do this with SP
            {
                try
                {
                    bool IsStaffToken = true;
                    if (tokenModel.RoleID != 0)
                    {
                        UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                        if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower() || userroles.UserType.ToLower() == UserTypeEnum.DEPENDENT.ToString().ToLower())
                        {
                            IsStaffToken = false;
                        }
                    }
                    else
                        IsStaffToken = false;

                    var telehealthTokenDetails = new TelehealthTokenDetails()
                    {
                        CreatedBy = tokenModel.UserID,
                        CreatedDate = DateTime.UtcNow,
                        TelehealthSessionDetailID = telehealthSessionDetailID,
                        IsActive = true,
                        IsDeleted = false,
                        Token = token,
                        TokenExpiry = duration,
                        IsStaffToken = IsStaffToken,
                        OrganizationId = tokenModel.OrganizationID,
                        InvitationId = invitationId
                    };
                    _context.TelehealthTokenDetails.Add(telehealthTokenDetails);
                    int result = _context.SaveChanges();
                    transaction.Commit();
                    telehealthTokenDetails.result = result;
                    return telehealthTokenDetails;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TelehealthTokenDetails telehealthTokenDetails = new TelehealthTokenDetails();
                    telehealthTokenDetails.exception = ex;
                    return telehealthTokenDetails;
                }
            }
        }
        public bool UpdateTelehealthCallDuration(int telehealthSessionDetailID, DateTime startTime, DateTime endTime, LocationModel locationModal, TokenModel tokenModel, string action = "add")
        {
            bool isSuccess = false;
            using (var transaction = _context.Database.BeginTransaction()) //TO DO do this with SP
            {
                try
                {
                    TelehealthSessionDetails telehealthSessionDetails = _context.TelehealthSessionDetails.Where(x => x.Id == telehealthSessionDetailID && x.IsActive == true && x.IsDeleted == false).FirstOrDefault();
                    if(telehealthSessionDetails != null && telehealthSessionDetails.Id > 0)
                    {
                        
                        if (action.ToLower() == "add")
                        {
                            telehealthSessionDetails.StartTime = startTime;
                            telehealthSessionDetails.EndTime = endTime.AddMinutes(15);
                        }
                        else
                        {
                            telehealthSessionDetails.EndTime = endTime;
                        }

                        telehealthSessionDetails.UpdatedBy = tokenModel.UserID;
                        telehealthSessionDetails.UpdatedDate = DateTime.UtcNow;

                        _context.TelehealthSessionDetails.Update(telehealthSessionDetails);
                        int result = _context.SaveChanges();
                        
                        
                        transaction.Commit();
                        telehealthSessionDetails.result = result;
                        isSuccess = true;
                    }
                    
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TelehealthSessionDetails telehealthSessionDetails = new TelehealthSessionDetails();
                    telehealthSessionDetails.exception = ex;
                    return isSuccess;
                }
            }
            return isSuccess;
        }
        public TelehealthTokenDetails CreateTelehealthToken(int telehealthSessionDetailID, string token, int userId, Common.Enums.CommonEnum.UserTypeEnum userTypeEnum, double duration, TokenModel tokenModel)
        {
            using (var transaction = _context.Database.BeginTransaction()) //TO DO do this with SP
            {
                try
                {
                    bool IsStaffToken = true;
                    if (userTypeEnum.ToString().ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                    {
                        IsStaffToken = false;
                    }

                    var telehealthTokenDetails = new TelehealthTokenDetails()
                    {
                        CreatedBy = userId,
                        CreatedDate = DateTime.UtcNow,
                        TelehealthSessionDetailID = telehealthSessionDetailID,
                        IsActive = true,
                        IsDeleted = false,
                        Token = token,
                        TokenExpiry = duration,
                        IsStaffToken = IsStaffToken,
                        OrganizationId = tokenModel.OrganizationID,
                        InvitationId = null
                    };
                    _context.TelehealthTokenDetails.Add(telehealthTokenDetails);
                    int result = _context.SaveChanges();
                    transaction.Commit();
                    telehealthTokenDetails.result = result;
                    return telehealthTokenDetails;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TelehealthTokenDetails telehealthTokenDetails = new TelehealthTokenDetails();
                    telehealthTokenDetails.exception = ex;
                    return telehealthTokenDetails;
                }
            }
        }
        public TelehealthSessionDetails CreateTelehealthSession(string sessionID, int? patientID, int? staffID, DateTime? startTime, DateTime? endTime, int appointmentId, TokenModel tokenModel)
        {
            using (var transaction = _context.Database.BeginTransaction()) //TO DO do this with SP
            {
                try
                {
                    TelehealthSessionDetails telehealthSessionDetails = new TelehealthSessionDetails()
                    {
                        CreatedBy = tokenModel.UserID,
                        CreatedDate = DateTime.UtcNow,
                        PatientID = patientID,
                        StaffId = staffID,
                        IsActive = true,
                        IsDeleted = false,
                        EndTime = endTime,
                        StartTime = startTime,
                        SessionID = sessionID,
                        AppointmentId = appointmentId,
                        OrganizationId = tokenModel.OrganizationID
                    };
                    _context.TelehealthSessionDetails.Add(telehealthSessionDetails);
                    int result = _context.SaveChanges();
                    transaction.Commit();
                    telehealthSessionDetails.result = result;
                    return telehealthSessionDetails;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TelehealthSessionDetails telehealthSessionDetails = new TelehealthSessionDetails();
                    telehealthSessionDetails.exception = ex;
                    return telehealthSessionDetails;
                }
            }
        }

        public string GetUserNameByUserID(TokenModel tokenModel)
        {
            try
            {
                UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                string UserName = string.Empty;
                if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                {
                    var patient = _context.Patients.Where(l => l.UserID == tokenModel.UserID).FirstOrDefault();
                    if (patient != null)
                    {
                        UserName = patient.FirstName + " " + patient.LastName;
                    }
                }
                else
                {
                    var staff = _context.Staffs.Where(l => l.UserID == tokenModel.UserID).FirstOrDefault();
                    if (staff != null)
                    {
                        UserName = staff.FirstName + " " + staff.LastName;
                    }
                }
                return UserName;
            }
            catch
            {
                return "";
            }
        }

        public string GetUserNameByUserID(int userId, Common.Enums.CommonEnum.UserTypeEnum userTypeEnum)
        {
            try
            {
                //UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                string UserName = string.Empty;
                if (userTypeEnum.ToString() == UserTypeEnum.CLIENT.ToString().ToLower())
                {
                    var patient = _context.Patients.Where(l => l.UserID == userId).FirstOrDefault();
                    if (patient != null)
                    {
                        UserName = patient.FirstName + " " + patient.LastName;
                    }
                }
                else
                {
                    var staff = _context.Staffs.Where(l => l.UserID == userId).FirstOrDefault();
                    if (staff != null)
                    {
                        UserName = staff.FirstName + " " + staff.LastName;
                    }
                }
                return UserName;
            }
            catch
            {
                return "";
            }
        }
        public TelehealthSessionDetails GetOTSessionById(int id, TokenModel tokenModel)
        {
            return _context.TelehealthSessionDetails
             .Where(k =>
             k.Id == id
             && k.OrganizationId == tokenModel.OrganizationID
             && k.IsActive == true
             && k.IsDeleted == false
             ).FirstOrDefault();

        }
        public TelehealthSessionDetails GetOTSession(int? invitationId, TokenModel tokenModel)
        {
            return _context.TelehealthSessionDetails
                .Join(_context.TelehealthTokenDetails,
                session => session.Id,
                token => token.TelehealthSessionDetailID,
                (session, token) => new { session, token }
                )
            .Where(k => k.session.IsActive == true && k.session.IsDeleted == false && k.token.IsActive == true && k.token.IsDeleted == false && k.token.InvitationId == invitationId
            ).Select(x =>
                x.session).FirstOrDefault();

        }
        public TelehealthTokenDetails GetTelehealthTokenByInvitationId(int? invitationId, TokenModel tokenModel)
        {
            return _context.TelehealthTokenDetails
            .Where(k =>
            k.InvitationId == invitationId
            && k.OrganizationId == tokenModel.OrganizationID
            && k.IsActive == true
            && k.IsDeleted == false
            ).FirstOrDefault();
        }
        public TelehealthSessionDetails GetTelehealthSessionByAppointmentId(int? appointmentId, TokenModel tokenModel)
        {
            return _context.TelehealthSessionDetails
            .Where(k =>
            k.AppointmentId == appointmentId
            && k.OrganizationId == tokenModel.OrganizationID
            && k.IsActive == true
            && k.IsDeleted == false
            ).FirstOrDefault();
        }
        public TelehealthSessionDetails GetOTSessionBySessionId(string sessionId, TokenModel tokenModel)
        {
            return _context.TelehealthSessionDetails
             .Where(k =>
             k.SessionID == sessionId
             && k.OrganizationId == tokenModel.OrganizationID
             && k.IsActive == true
             && k.IsDeleted == false
             ).FirstOrDefault();

        }
        public IQueryable<TelehealthTokenDetails> GetOTTokenByAppointmentId(int appointmentId, int userId, TokenModel tokenModel)
        {
            return _context.TelehealthTokenDetails
                .Join(_context.TelehealthSessionDetails,
                (token) => token.TelehealthSessionDetailID,
                (session) => session.Id,
                (token, session) => new { token, session }
                ).Where(k =>
             k.session.AppointmentId == appointmentId
             && k.session.OrganizationId == tokenModel.OrganizationID
             && k.session.IsActive == true
             && k.session.IsDeleted == false
             && k.token.IsActive == true
             && k.token.IsDeleted == false
             )
             .Select(s => s.token).AsQueryable();
        }

        
    }
}

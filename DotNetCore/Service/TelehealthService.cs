using HC.Common;
using HC.Common.HC.Common;
using HC.Model;
using HC.Patient.Data;
using HC.Patient.Entity;
using HC.Patient.Model;
using HC.Patient.Model.Availability;
using HC.Patient.Model.Chat;
using HC.Patient.Model.MasterData;
using HC.Patient.Model.PatientAppointment;
using HC.Patient.Model.Telehealth;
using HC.Patient.Model.Users;
using HC.Patient.Repositories.IRepositories;
using HC.Patient.Repositories.IRepositories.Appointment;
using HC.Patient.Repositories.IRepositories.Telehealth;
using HC.Patient.Repositories.IRepositories.User;
using HC.Patient.Service.IServices;
using HC.Patient.Service.IServices.Chats;
using HC.Patient.Service.IServices.MasterData;
using HC.Patient.Service.IServices.PatientAppointment;
using HC.Patient.Service.IServices.Telehealth;
using HC.Patient.Service.IServices.User;
using HC.Service;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using OpenTok.Server;
using OpenTok.Server.Util;
using PushSharp.Apple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static HC.Common.Enums.CommonEnum;

namespace HC.Patient.Service.Services.Telehealth
{
    public class TelehealthService : BaseService, ITelehealthService
    {
        private readonly ITelehealthRepository _telehealthRepository;
        private readonly HCOrganizationContext _context;
        private readonly IOpenTokSettingsService _openTokSettingsService;
        //private readonly IPatientAppointmentService _patientAppointmentService;
        private readonly IGroupSessionInvitationRepository _groupSessionInvitationRepository;
        private readonly ITelehealthRecordingService _telehealthRecordingService;
        private readonly IChatService _chatService;
        private readonly IChatRoomRepository _chatRoomRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUserService _userService;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IHostingEnvironment _env;
        private readonly ILocationService _locationService;

        public TelehealthService(IHostingEnvironment env,
            ITelehealthRepository telehealthRepository,
            HCOrganizationContext context,
            IOpenTokSettingsService openTokSettingsService,
            //IPatientAppointmentService patientAppointmentService,
            IGroupSessionInvitationRepository groupSessionInvitationRepository,
            ITelehealthRecordingService telehealthRecordingService,
            IChatService chatService,
            IChatRoomRepository chatRoomRepository,
            IAppointmentRepository appointmentRepository,
            IUserService userService,
            IUserRoleRepository userRoleRepository,
            ILocationService locationService
            )
        {
            _telehealthRepository = telehealthRepository;
            _context = context;
            _openTokSettingsService = openTokSettingsService;
            // _patientAppointmentService = patientAppointmentService;
            _groupSessionInvitationRepository = groupSessionInvitationRepository;
            _telehealthRecordingService = telehealthRecordingService;
            _chatService = chatService;
            _chatRoomRepository = chatRoomRepository;
            _appointmentRepository = appointmentRepository;
            _userService = userService;
            _userRoleRepository = userRoleRepository;
            _env = env;
            _locationService = locationService;
        }
        public JsonModel GetTelehealthSessionForInvitedAppointmentId(int invitedAppointmentId, TokenModel tokenModel)
        {
            var groupSession = _groupSessionInvitationRepository.GetGroupSessionByInvitaionAppointmentId(invitedAppointmentId, tokenModel);
            if (groupSession == null)
                return new JsonModel(null, StatusMessage.NotFound, (int)HttpStatusCodes.NotFound);
            return GetOTSession(CommonMethods.Encrypt(groupSession.InvitaionId.ToString()), tokenModel);

        }

        public JsonModel GetTelehealthSession(int appointmentId, TokenModel tokenModel, bool isMobile = false)
        {
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            //OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(OpenTokAPIDetails.APIKey, OpenTokAPIDetails.APISecret, OpenTokAPIDetails.APIUrl)
            //{
            //    Client = new HttpClient(OpenTokAPIDetails.APIKey, OpenTokAPIDetails.APISecret, OpenTokAPIDetails.APIUrl)
            //};
            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            var UserName = _telehealthRepository.GetUserNameByUserID(tokenModel);
            openTokModel.Name = UserName;

            //TelehealthSessionDetails telehealthSessionDetails = _telehealthRepository.GetTelehealthSession(patientID, staffID, startTime, endTime);
            TelehealthSessionDetails telehealthSessionDetails = _telehealthRepository.GetTelehealthSessionByAppointmentId(appointmentId, tokenModel);
            if (telehealthSessionDetails != null)
            {
                TelehealthTokenDetails telehealthTokenDetails = _telehealthRepository.GetTelehealthToken(telehealthSessionDetails.Id, tokenModel);
                if (telehealthTokenDetails != null)
                {
                    UpdateTelehealthCallDuration(telehealthSessionDetails.Id, tokenModel);
                    openTokModel = new OpenTokModel()
                    {
                        ApiKey = openTokSettingModel.APIKey,
                        SessionID = telehealthSessionDetails.SessionID,
                        Token = telehealthTokenDetails.Token,
                        Id = telehealthSessionDetails.Id,
                        AppointmentId = appointmentId
                    };
                    if (isMobile)
                        this.voip(appointmentId, tokenModel);
                    return new JsonModel()
                    {
                        data = openTokModel,
                        Message = StatusMessage.FetchMessage,
                        StatusCode = (int)HttpStatusCodes.OK//Success
                    };
                }
                else
                {
                    DateTime d = DateTime.Now.AddDays(25);
                    var epoch = DateTime.UtcNow;///new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                     var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string token = string.Empty;
                    UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                    if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                        token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);
                    else
                        token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);
                    
                    telehealthTokenDetails = _telehealthRepository.CreateTelehealthToken(telehealthSessionDetails.Id, token, duration, tokenModel);
                    if (telehealthTokenDetails.result > 0)
                    {
                        openTokModel = new OpenTokModel()
                        {
                            ApiKey = openTokSettingModel.APIKey,
                            SessionID = telehealthSessionDetails.SessionID,
                            Token = telehealthTokenDetails.Token,
                            Id = telehealthSessionDetails.Id,
                            AppointmentId = appointmentId
                        };
                        if (isMobile)
                            this.voip(appointmentId, tokenModel);
                        return new JsonModel()
                        {
                            data = openTokModel,
                            Message = StatusMessage.FetchMessage,
                            StatusCode = (int)HttpStatusCodes.OK//Success
                        };
                    }
                    else
                    {
                        if (telehealthTokenDetails.exception != null)
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = duration.ToString() + ' ' + d + ' ' + epoch,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                        else
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = StatusMessage.ErrorOccured,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                    }
                }
            }
            else
            {
                var session = opentok.CreateSession(string.Empty, MediaMode.ROUTED);
                telehealthSessionDetails = _telehealthRepository.CreateTelehealthSession(session.Result.Id, null, null, null, null, appointmentId, tokenModel);
                if (telehealthSessionDetails.result > 0)
                {
                    DateTime d = DateTime.Now.AddDays(25);
                    var epoch = DateTime.UtcNow;// new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                     var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    string token = string.Empty;
                    UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                    if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower() || userroles.UserType.ToLower() == UserTypeEnum.DEPENDENT.ToString().ToLower())
                        token = opentok.GenerateToken(session.Result.Id, Role.PUBLISHER, duration);
                    else
                        token = opentok.GenerateToken(session.Result.Id, Role.PUBLISHER, duration);

                    TelehealthTokenDetails telehealthTokenDetails = _telehealthRepository.CreateTelehealthToken(telehealthSessionDetails.Id, token, duration, tokenModel);
                    if (telehealthTokenDetails.result > 0)
                    {
                        openTokModel = new OpenTokModel()
                        {
                            ApiKey = openTokSettingModel.APIKey,
                            SessionID = telehealthSessionDetails.SessionID,
                            Token = token,
                            Id = telehealthSessionDetails.Id,
                            AppointmentId = appointmentId
                        };
                        if (isMobile)
                            this.voip(appointmentId, tokenModel);
                        return new JsonModel()
                        {
                            data = openTokModel,
                            Message = StatusMessage.FetchMessage,
                            StatusCode = (int)HttpStatusCodes.OK//Success
                        };
                    }
                    else
                    {
                        if (telehealthTokenDetails.exception != null)
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = telehealthTokenDetails.exception.Message,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };

                        }
                        else
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = StatusMessage.ErrorOccured,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };

                        }
                    }
                }
                else
                {
                    if (telehealthSessionDetails.exception != null)
                    {
                        return new JsonModel()
                        {
                            data = new object(),
                            Message = telehealthSessionDetails.exception.Message,
                            StatusCode = (int)HttpStatusCodes.InternalServerError
                        };
                    }
                    else
                    {
                        return new JsonModel()
                        {
                            data = new object(),
                            Message = StatusMessage.ErrorOccured,
                            StatusCode = (int)HttpStatusCodes.InternalServerError
                        };

                    }
                }
            }
        }

        public JsonModel UpdateTelehealthCallDuration(int telehealthSessionDetailID, TokenModel tokenModel, DateTime? endDateTime = null)
        {
            string action = endDateTime != null ? "update" : "add";
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;
            LocationModel locationModal = _locationService.GetLocationOffsets(tokenModel.LocationID, tokenModel);
            startTime = CommonMethods.ConvertToUtcTimeWithOffset(startTime, locationModal.DaylightOffset, locationModal.StandardOffset, locationModal.TimeZoneName);
            endTime = CommonMethods.ConvertToUtcTimeWithOffset(endTime, locationModal.DaylightOffset, locationModal.StandardOffset, locationModal.TimeZoneName);
            bool isSuccess = _telehealthRepository.UpdateTelehealthCallDuration(telehealthSessionDetailID, startTime, endTime, locationModal, tokenModel, action);
            if (isSuccess)
            {
                return new JsonModel()
                {
                    data = new object(),
                    Message = StatusMessage.Success,
                    StatusCode = (int)HttpStatusCodes.OK
                };
            }
            else
            {
                return new JsonModel()
                {
                    data = new object(),
                    Message = StatusMessage.ErrorOccured,
                    StatusCode = (int)HttpStatusCodes.InternalServerError
                };
            }
        }

        public JsonModel getOpenTokSession(TokenModel tokenModel)
        {
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);
            //telehealthSessionDetails = _telehealthRepository.CreateTelehealthSession(session.Id, patientID, staffID, startTime, endTime, tokenModel);
            //UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
            OpenTokModel openTokModel = new OpenTokModel();
            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTokSDK.OpenTok openTok = new OpenTokSDK.OpenTok(apiKey, openTokSettingModel.APISecret);
            var session = openTok.CreateSession(string.Empty, OpenTokSDK.MediaMode.ROUTED);
            DateTime d = DateTime.Now.AddDays(25);
            var epoch = DateTime.UtcNow;/// new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
             var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string token = string.Empty;
            token = openTok.GenerateToken(session.Id, OpenTokSDK.Role.PUBLISHER);
            //if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
            //    token = openTok.GenerateToken(session.Id, Role.PUBLISHER, duration);
            //else
            //try
            //{

            //}
            //catch(Exception ex) {
            //    return new JsonModel()
            //    {
            //        data = openTokModel.SessionID,
            //        Message = ex.Message,
            //        StatusCode = (int) HttpStatusCodes.InternalServerError
            //    };
            //}
            //token = openTok.GenerateToken(session.Id, Role.MODERATOR, duration);

            openTokModel = new OpenTokModel()
            {
                ApiKey = openTokSettingModel.APIKey,
                SessionID = session.Id,
                Token = token
            };
            return new JsonModel()
            {
                data = openTokModel,
                Message = StatusMessage.FetchMessage,
                StatusCode = (int)HttpStatusCodes.OK//Success
            };
        }
        public JsonModel GetOTSession(string invitationId, TokenModel tokenModel)
        {
            if (string.IsNullOrEmpty(invitationId))
                return new JsonModel(null, StatusMessage.BadRequest, (int)HttpStatusCodes.BadRequest);
            Guid.TryParse(CommonMethods.Decrypt(invitationId.Replace(" ", "+")), out Guid invId);
            var groupSessionInvitation = _groupSessionInvitationRepository.GetGroupSessionByInvitaionId(invId, tokenModel);
            if (groupSessionInvitation == null)
                return new JsonModel(null, StatusMessage.NotFound, (int)HttpStatusCodes.NotFound);
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);
            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            TelehealthSessionDetails telehealthSessionDetails = _telehealthRepository.GetOTSessionById((int)groupSessionInvitation.SessionId, tokenModel);
            if (telehealthSessionDetails != null)
            {
                TelehealthTokenDetails telehealthTokenDetails = _telehealthRepository.GetTelehealthTokenByInvitationId(groupSessionInvitation.Id, tokenModel);
                int.TryParse(telehealthSessionDetails.AppointmentId.ToString(), out int AppointmentId);
                PatientAppointmentModel patientAppointmentModel = _appointmentRepository.GetAppointmentDetails<PatientAppointmentModel>(AppointmentId).FirstOrDefault();
                if(patientAppointmentModel.PatientEncounterId == null && groupSessionInvitation.IsViewedStatus == false)
                {

                if (telehealthTokenDetails != null)
                {
                    
                    openTokModel = new OpenTokModel()
                    {
                        ApiKey = openTokSettingModel.APIKey,
                        SessionID = telehealthSessionDetails.SessionID,
                        Token = telehealthTokenDetails.Token,
                        Id = telehealthSessionDetails.Id,
                        AppointmentId = (int)groupSessionInvitation.AppointmentId,
                        UserId = (int)groupSessionInvitation.UserID
                    };
                        groupSessionInvitation.IsViewedStatus = true;
                        var updateResult = _groupSessionInvitationRepository.UpdateGroupSessionInvitation(groupSessionInvitation, tokenModel);

                        return new JsonModel()
                    {
                        data = openTokModel,
                        Message = StatusMessage.FetchMessage,
                        StatusCode = (int)HttpStatusCodes.OK//Success
                    };
                }
                else
                {
                    DateTime d = DateTime.Now.AddDays(25);
                    var epoch = DateTime.UtcNow;/// new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                     var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string token = string.Empty;
                    UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                    token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);

                    telehealthTokenDetails = _telehealthRepository.CreateTelehealthToken(telehealthSessionDetails.Id, token, duration, tokenModel, groupSessionInvitation.Id);
                    if (telehealthTokenDetails.result > 0)
                    {
                        openTokModel = new OpenTokModel()
                        {
                            ApiKey = openTokSettingModel.APIKey,
                            SessionID = telehealthSessionDetails.SessionID,
                            Token = telehealthTokenDetails.Token,
                            Id = telehealthSessionDetails.Id,
                            AppointmentId = (int)groupSessionInvitation.AppointmentId,
                            UserId = (int)groupSessionInvitation.UserID
                        };
                        return new JsonModel()
                        {
                            data = openTokModel,
                            Message = StatusMessage.FetchMessage,
                            StatusCode = (int)HttpStatusCodes.OK//Success
                        };
                    }
                    else
                    {
                        if (telehealthTokenDetails.exception != null)
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = telehealthTokenDetails.exception.Message,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                        else
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = StatusMessage.ErrorOccured,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                    }
                }
                    //DateTime d = DateTime.Now.AddMinutes(60);
                    //var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    //var duration = (d.ToUniversalTime() - epoch).TotalSeconds;

                    //string token = string.Empty;
                    //token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);
                    //openTokModel = new OpenTokModel()
                    //{
                    //    ApiKey = openTokSettingModel.APIKey,
                    //    SessionID = telehealthSessionDetails.SessionID,
                    //    Token = token
                    //};
                    //return new JsonModel()
                    //{
                    //    data = openTokModel,
                    //    Message = StatusMessage.FetchMessage,
                    //    StatusCode = (int)HttpStatusCodes.OK
                    //};

                }
            }
            return new JsonModel()
            {
                data = openTokModel,
                Message = StatusMessage.NotFound,
                StatusCode = (int)HttpStatusCodes.NotFound
            };
        }

        public JsonModel GetOTSessionByAppointmentId(int appointmentId, TokenModel tokenModel)
        {
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            var sessionDetails = _telehealthRepository.GetTelehealthSessionByAppointmentId(appointmentId, tokenModel);
            if (sessionDetails != null)
            {
                var openTokModel = new OpenTokModel()
                {
                    ApiKey = openTokSettingModel.APIKey,
                    SessionID = sessionDetails.SessionID,
                    Token = "",
                    Id = sessionDetails.Id
                };
                return new JsonModel(openTokModel, StatusMessage.FetchMessage, (int)HttpStatusCodes.OK);
            }
            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            var appointmentDetail = _appointmentRepository.GetByID(appointmentId); //(PatientAppointmentModel)_patientAppointmentService.GetAppointmentDetails(appointmentId, tokenModel).data;
            if (appointmentDetail == null)
                return new JsonModel(null, StatusMessage.AppointmentNotExists, (int)HttpStatusCodes.NotFound);
            var session = opentok.CreateSession(string.Empty, MediaMode.ROUTED);
            var telehealthSessionDetails = _telehealthRepository.CreateTelehealthSession(session.Result.Id, (int)appointmentDetail.PatientID, tokenModel.StaffID, appointmentDetail.StartDateTime, appointmentDetail.EndDateTime, appointmentId, tokenModel);
            if (telehealthSessionDetails.result > 0)
            {
                if (telehealthSessionDetails != null)
                {
                    var openTokModel = new OpenTokModel()
                    {
                        ApiKey = openTokSettingModel.APIKey,
                        SessionID = telehealthSessionDetails.SessionID,
                        Token = "",
                        Id = telehealthSessionDetails.Id
                    };
                    return new JsonModel()
                    {
                        data = openTokModel,
                        Message = StatusMessage.FetchMessage,
                        StatusCode = (int)HttpStatusCodes.OK
                    };
                }
            }

            return new JsonModel()
            {
                data = null,
                Message = StatusMessage.NotFound,
                StatusCode = (int)HttpStatusCodes.NotFound
            };
        }

        public async Task<JsonModel> StartVideoRecordingAsync(string sessionId, TokenModel tokenModel)
        {
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            var archive = await opentok.StartArchive(sessionId, "Appointment Recording");
            if (archive.Id != null)
            {
                var telehealthSession = _telehealthRepository.GetOTSessionBySessionId(sessionId, tokenModel);
                if (telehealthSession == null)
                    return new JsonModel(archive, StatusMessage.RecordingStartedButNotSaved, (int)HttpStatusCodes.InternalServerError);

                var videoSavedResult = _telehealthRecordingService.SaveVideoArchived(archive.Id.ToString(), telehealthSession.Id, tokenModel);
                if (videoSavedResult.StatusCode == (int)HttpStatusCodes.OK || videoSavedResult.StatusCode == (int)HttpStatusCodes.Created)
                    return new JsonModel(archive, StatusMessage.RecordingStartedAndSaved, (int)HttpStatusCodes.OK);
                else
                    return new JsonModel(archive, StatusMessage.RecordingStartedButNotSaved, (int)HttpStatusCodes.InternalServerError);

            }
            else
                return new JsonModel(archive, StatusMessage.RecordingNotStarted, (int)HttpStatusCodes.InternalServerError);
        }
        public async Task<JsonModel> StopVideoRecordingAsync(string archiveId, int appointmentId, TokenModel tokenModel)
        {
            JsonModel response;
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            Archive archive = await opentok.StopArchive(archiveId);
            if (archive == null)
                return new JsonModel(null, StatusMessage.RecordingNotStopped, (int)HttpStatusCodes.InternalServerError);
            List<ChatFileResponseModel> ChatFileResponseModels = new List<ChatFileResponseModel>
            {
                new ChatFileResponseModel()
                {
                    FileName = "Call Recording" ,
                    Message =archiveId,
                    FileType = ".rec",
                    MessageType = (int)Common.Enums.CommonEnum.MessageType.Recording,
                }
            };
            var telehealthSession = _telehealthRepository.GetTelehealthSessionByAppointmentId(appointmentId, tokenModel);
            if (telehealthSession == null)
                return new JsonModel(ChatFileResponseModels, StatusMessage.RecordingStoppedButNotSentInChat, (int)HttpStatusCodes.InternalServerError);
            var videoSavedResult = _telehealthRecordingService.SaveVideoArchived(archiveId, telehealthSession.Id, tokenModel);
            if (videoSavedResult.StatusCode == (int)HttpStatusCodes.OK || videoSavedResult.StatusCode == (int)HttpStatusCodes.Created)
            {
                var chatRoom = _chatRoomRepository.GetChatRoomByName("App-" + appointmentId.ToString(), tokenModel);
                if (chatRoom == null)
                    return response = new JsonModel(ChatFileResponseModels, StatusMessage.RecordingStoppedButNotSentInChat, (int)HttpStatusCodes.InternalServerError);
                ChatModel chatModel = new ChatModel()
                {
                    ChatDate = DateTime.UtcNow,
                    FromUserId = tokenModel.UserID,
                    RoomId = chatRoom.Id,
                    IsSeen = false,
                    Message = archiveId,
                    FileName = CommonMethods.Encrypt(archiveId),
                    FileType = CommonMethods.Encrypt(".rec"),
                    MessageType = (int)Common.Enums.CommonEnum.MessageType.Recording
                };
                var result = _chatService.SaveChat(chatModel, tokenModel);
                if (result.StatusCode == (int)HttpStatusCodes.OK)
                {
                    ChatFileResponseModels.ForEach(chatFile =>
                    {
                        chatFile.RoomId = chatRoom.Id;
                    });
                    response = new JsonModel(ChatFileResponseModels, StatusMessage.RecordingStoppedAndSendInChat, (int)HttpStatusCodes.OK);
                }
                else
                    response = new JsonModel(ChatFileResponseModels, StatusMessage.RecordingStoppedButNotSentInChat, (int)HttpStatusCodes.InternalServerError);
            }
            else
                response = new JsonModel(ChatFileResponseModels, StatusMessage.RecordingStoppedButNotSaved, (int)HttpStatusCodes.OK);
            return response;
        }

        public async Task<JsonModel> GetVideoRecordingAsync(string archiveId, TokenModel tokenModel)
        {
            //JsonModel response;
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            Archive archive = await opentok.GetArchive(archiveId);
            if (archive == null)
                return new JsonModel(archive, StatusMessage.CallRecordingNotFound, (int)HttpStatusCodes.NotFound);
            return new JsonModel(archive, StatusMessage.CallRecordingFound, (int)HttpStatusCodes.OK);
        }

        public JsonModel CreateTelehealthSession(int appointmentId, int userId, UserTypeEnum userTypeEnum, TokenModel tokenModel)
        {
            OpenTokModel openTokModel = new OpenTokModel();
            OpenTokSettingModel openTokSettingModel = _openTokSettingsService.GetOpenTokSettingsByOrganizationId(tokenModel);
            if (openTokSettingModel == null)
                return new JsonModel(null, StatusMessage.OTKeyNotFound, (int)HttpStatusCodes.NotFound);

            int.TryParse(openTokSettingModel.APIKey, out int apiKey);
            OpenTok.Server.OpenTok opentok = new OpenTok.Server.OpenTok(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            {
                Client = new HttpClient(apiKey, openTokSettingModel.APISecret, openTokSettingModel.APIUrl)
            };
            var UserName = _telehealthRepository.GetUserNameByUserID(userId, userTypeEnum);
            openTokModel.Name = UserName;

            //TelehealthSessionDetails telehealthSessionDetails = _telehealthRepository.GetTelehealthSession(patientID, staffID, startTime, endTime);
            TelehealthSessionDetails telehealthSessionDetails = _telehealthRepository.GetTelehealthSessionByAppointmentId(appointmentId, tokenModel);
            if (telehealthSessionDetails != null)
            {
                TelehealthTokenDetails telehealthTokenDetails = _telehealthRepository.GetTelehealthToken(telehealthSessionDetails.Id, userTypeEnum, tokenModel);
                if (telehealthTokenDetails != null)
                {
                    openTokModel = new OpenTokModel()
                    {
                        ApiKey = openTokSettingModel.APIKey,
                        SessionID = telehealthSessionDetails.SessionID,
                        Token = telehealthTokenDetails.Token,
                        Id = telehealthSessionDetails.Id,
                        AppointmentId = appointmentId
                    };
                    return new JsonModel()
                    {
                        data = openTokModel,
                        Message = StatusMessage.FetchMessage,
                        StatusCode = (int)HttpStatusCodes.OK//Success
                    };
                }
                else
                {
                    DateTime d = DateTime.Now.AddDays(25);
                    var epoch = DateTime.UtcNow; ///new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    //var duration = (d.ToUniversalTime() - epoch).TotalSeconds;
                    var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string token = string.Empty;
                    //UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                    if (userTypeEnum.ToString().ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                        token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);
                    else
                        token = opentok.GenerateToken(telehealthSessionDetails.SessionID, Role.PUBLISHER, duration);

                    telehealthTokenDetails = _telehealthRepository.CreateTelehealthToken(telehealthSessionDetails.Id, token, duration, tokenModel);
                    if (telehealthTokenDetails.result > 0)
                    {
                        openTokModel = new OpenTokModel()
                        {
                            ApiKey = openTokSettingModel.APIKey,
                            SessionID = telehealthSessionDetails.SessionID,
                            Token = telehealthTokenDetails.Token,
                            Id = telehealthSessionDetails.Id,
                            AppointmentId = appointmentId
                        };
                        return new JsonModel()
                        {
                            data = openTokModel,
                            Message = StatusMessage.FetchMessage,
                            StatusCode = (int)HttpStatusCodes.OK//Success
                        };
                    }
                    else
                    {
                        if (telehealthTokenDetails.exception != null)
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = telehealthTokenDetails.exception.Message,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                        else
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = StatusMessage.ErrorOccured,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };
                        }
                    }
                }
            }
            else
            {
                var session = opentok.CreateSession(string.Empty, MediaMode.ROUTED);
                telehealthSessionDetails = _telehealthRepository.CreateTelehealthSession(session.Result.Id, null, null, null, null, appointmentId, tokenModel);
                if (telehealthSessionDetails.result > 0)
                {
                    DateTime d = DateTime.Now.AddDays(25);
                    var epoch = DateTime.UtcNow; /// new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                     var duration = (DateTime.UtcNow.Add(TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    string token = string.Empty;
                    UserRoles userroles = _context.UserRoles.Where(j => j.Id == tokenModel.RoleID).FirstOrDefault();
                    if (userroles.UserType.ToLower() == UserTypeEnum.CLIENT.ToString().ToLower())
                        token = opentok.GenerateToken(session.Result.Id, Role.PUBLISHER, duration);
                    else
                        token = opentok.GenerateToken(session.Result.Id, Role.PUBLISHER, duration);

                    TelehealthTokenDetails telehealthTokenDetails = _telehealthRepository.CreateTelehealthToken(telehealthSessionDetails.Id, token, duration, tokenModel);
                    if (telehealthTokenDetails.result > 0)
                    {
                        openTokModel = new OpenTokModel()
                        {
                            ApiKey = openTokSettingModel.APIKey,
                            SessionID = telehealthSessionDetails.SessionID,
                            Token = token,
                            Id = telehealthSessionDetails.Id,
                            AppointmentId = appointmentId
                        };
                        return new JsonModel()
                        {
                            data = openTokModel,
                            Message = StatusMessage.FetchMessage,
                            StatusCode = (int)HttpStatusCodes.OK//Success
                        };
                    }
                    else
                    {
                        if (telehealthTokenDetails.exception != null)
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = telehealthTokenDetails.exception.Message,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };

                        }
                        else
                        {
                            return new JsonModel()
                            {
                                data = new object(),
                                Message = StatusMessage.ErrorOccured,
                                StatusCode = (int)HttpStatusCodes.InternalServerError
                            };

                        }
                    }
                }
                else
                {
                    if (telehealthSessionDetails.exception != null)
                    {
                        return new JsonModel()
                        {
                            data = new object(),
                            Message = telehealthSessionDetails.exception.Message,
                            StatusCode = (int)HttpStatusCodes.InternalServerError
                        };
                    }
                    else
                    {
                        return new JsonModel()
                        {
                            data = new object(),
                            Message = StatusMessage.ErrorOccured,
                            StatusCode = (int)HttpStatusCodes.InternalServerError
                        };

                    }
                }
            }
        }
        public void voip(int appointmentId, TokenModel tokenModel)
        {
            int failed = 0;
            int succeeded = 0;
            string certificateFilePath = string.Empty;
            string certificatePassword = "123456"; // We keep password empty
            UserRoles userRoles = _userRoleRepository.Get(x => x.Id == tokenModel.RoleID && x.OrganizationID == tokenModel.OrganizationID && x.IsDeleted == false);
            if (userRoles != null && userRoles.UserType == UserTypeEnum.PROVIDER.ToString())
            {
                certificateFilePath = _env.WebRootPath + "\\Voip\\VoipCertificates_Patient.p12";
            }
            else {
                certificateFilePath = _env.WebRootPath + "\\Voip\\VoipCertificates_Doctor.p12";
            }
            
            X509Certificate2 certificate = new X509Certificate2(certificateFilePath, certificatePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Sandbox, certificate, false);
            var broker = new ApnsServiceBroker(config);
            broker.OnNotificationFailed += (notification, exception) =>
            {
                failed++;
            };
            broker.OnNotificationSucceeded += (notification) =>
            {
                succeeded++;
            };
            var userDetails = _userService.GetApnToken(appointmentId, tokenModel.RoleID);
            broker.Start();

            broker.QueueNotification(new ApnsNotification
            {
                DeviceToken = userDetails.ApnToken,
                Payload = JObject.Parse("{ \"aps\" : { \"name\" : \" " + userDetails.Name + " \",\"appointmentId\" : \" " + appointmentId + " \" } }")
                //Payload = JObject.Parse("{ \"aps\" : { \"alert\" : \"Hello PushSharp!\" } }")
            });
            //}

            broker.Stop();
        }
    }
}

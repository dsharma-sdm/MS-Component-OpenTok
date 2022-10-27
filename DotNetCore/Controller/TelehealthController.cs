using HC.Model;
using HC.Patient.Model.Chat;
using HC.Patient.Repositories.IRepositories;
using HC.Patient.Service.IServices.Chats;
using HC.Patient.Service.IServices.Telehealth;
using HC.Patient.Web.Filters;
using HC.Patient.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Net;
using System.Threading.Tasks;
using static HC.Common.Enums.CommonEnum;
using System.Linq;
using System.Collections.Generic;
using HC.Common;
using HC.Patient.Model.PatientAppointment;

namespace HC.Patient.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/Telehealth")]
    [ActionFilter]
    public class TelehealthController : BaseController
    {
        //private JsonModel response;
        private ITelehealthService _telehealthService;
        IHubContext<ChatHub> _chatHubContext;
        private readonly IChatService _chatService;
        private readonly IChatRoomUserRepository _chatRoomUserRepository;
        private readonly IChatRoomRepository _chatRoomRepository;

        public TelehealthController(
            ITelehealthService telehealthService,
            IHubContext<ChatHub> chatHubContext,
            IChatService chatService,
            IChatRoomUserRepository chatRoomUserRepository,
            IChatRoomRepository chatRoomRepository
            )
        {
            _telehealthService = telehealthService;
            _chatHubContext = chatHubContext;
            _chatService = chatService;
            _chatRoomUserRepository = chatRoomUserRepository;
            _chatRoomRepository = chatRoomRepository;
        }
        // GET: api/Telehealth
        //changed appointmentmentId from int to string
        [HttpGet("GetTelehealthSession")]
        public JsonResult GetTelehealthSession(string appointmentId, bool isMobile = false)
        {
            int id = CommonMethods.GetIdFromDecryptedId(appointmentId);
            return Json(_telehealthService.ExecuteFunctions(() => _telehealthService.GetTelehealthSession(id, GetToken(HttpContext),isMobile)));
        }
       
        [HttpGet("GetTelehealthSessionByInvitedAppointmentId")]
        public JsonResult GetTelehealthSessionByInvitedAppointmentId(int appointmentId)
        {
            return Json(_telehealthService.ExecuteFunctions(() => _telehealthService.GetTelehealthSessionForInvitedAppointmentId(appointmentId, GetToken(HttpContext))));
        }
        
        [HttpPost("UpdateTelehealthCallDuration")]
        public JsonResult UpdateTelehealthCallDuration([FromBody]UpdateCallDurationModel updateCallDuration)
        {
            return Json(_telehealthService.ExecuteFunctions(() => _telehealthService.UpdateTelehealthCallDuration(updateCallDuration.TelehealthSessionDetailID, GetToken(HttpContext), updateCallDuration.EndDateTime)));
        }

        [HttpGet("GetOTSessionByApptId")]
        public JsonResult GetOTSessionByApptId(int appointmentId)
        {
            return Json(_telehealthService.ExecuteFunctions(() => _telehealthService.GetOTSessionByAppointmentId(appointmentId, GetToken(HttpContext))));
        }
        [HttpGet("{id}", Name = "GetTelehealthSession")]
        public string Get(int id)
        {
            return "value";
        }
        [HttpGet]
        [Route("StartVideoRecording")]
        public async Task<JsonResult> StartVideoRecording(string sessionId)
        {
            //return await _telehealthService.StartVideoRecording(sessionId, GetToken(HttpContext));
            return Json(await _telehealthService.ExecuteFunctions<Task<JsonModel>>(() => _telehealthService.StartVideoRecordingAsync(sessionId, GetToken(HttpContext))));
        }
        [HttpGet]
        [Route("StopVideoRecording")]
        public async Task<JsonResult> StopVideoRecording(string archiveId, int appointmentId)
        {
            TokenModel tokenModel = GetToken(HttpContext);
            var jsonModel = await _telehealthService.ExecuteFunctions<Task<JsonModel>>(() => _telehealthService.StopVideoRecordingAsync(archiveId, appointmentId, tokenModel));
            if (jsonModel.StatusCode == (int)HttpStatusCode.OK)
            {
                string connectionId = _chatService.GetConnectionId(tokenModel.UserID);

                if (connectionId != null)
                {

                    var roomId = 0;
                    var chatFileResponseModels = ((List<ChatFileResponseModel>)jsonModel.data);
                    if (chatFileResponseModels.Count > 0)
                        roomId = chatFileResponseModels[0].RoomId;
                    if (roomId > 0)
                    {
                        var dataToRecorderUserList = (List<ChatFileResponseModel>)jsonModel.data;
                        dataToRecorderUserList.ForEach(dataToRecorderUser =>
                        {
                            dataToRecorderUser.isRecieved = false;
                        });
                        await _chatHubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", dataToRecorderUserList, tokenModel.UserID, roomId);
                        var connectionIds = _chatRoomUserRepository.GetConectionIdInParticularRoom<RoomConnectionModel>(roomId, tokenModel.UserID, tokenModel);
                        var dataToOtherUserList = (List<ChatFileResponseModel>)jsonModel.data;
                        dataToOtherUserList.ForEach(dataToOtherUser =>
                        {
                            dataToOtherUser.isRecieved = true;
                        });
                        if (connectionIds != null && connectionIds.ToList().Count > 0)
                        {
                            connectionIds.ToList().ForEach(async conctionId =>
                            {
                                await _chatHubContext.Clients.Client(conctionId.ConnectionId).SendAsync("ReceiveMessage", dataToOtherUserList, tokenModel.UserID, roomId);
                            });

                            //await Clients.All.SendAsync("ReceiveMessage", chatFileResponseModels, UserId, currentRoomId);
                        }
                    }
                    //await _chatHubContext.Clients.All.SendAsync("ReceiveMessage", jsonModel.data, userId, roomId);
                }
            }
            return Json(jsonModel);
        }

        // POST: api/Telehealth
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/Telehealth/5
        [HttpPatch("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

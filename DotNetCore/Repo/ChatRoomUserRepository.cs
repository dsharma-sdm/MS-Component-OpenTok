using System;
using System.Collections.Generic;
using System.Text;
using HC.Model;
using HC.Patient.Data;
using HC.Patient.Entity;
using HC.Repositories;
using System.Linq;
using HC.Patient.Repositories.IRepositories;
using System.Threading.Tasks;
using HC.Patient.Model.Chat;
using System.Data.SqlClient;
using static HC.Common.Enums.CommonEnum;

namespace HC.Patient.Repositories.Repositories.Chats
{
    public class ChatRoomUserRepository : RepositoryBase<ChatRoomUser>, IChatRoomUserRepository
    {
        private HCOrganizationContext _context;
        public ChatRoomUserRepository(HCOrganizationContext context) : base(context)
        {
            _context = context;
        }
        public ChatRoomUser SaveNewChatRoomUser(ChatRoomUser chatRoomUser, TokenModel tokenModel)
        {
            _context.ChatRoomUsers.Add(chatRoomUser);
            var result = _context.SaveChanges();
            if (result > 0)
                return chatRoomUser;
            else
                return null;
        }

        public ChatRoomUser GetRoomInfoByRoomIdAndUserId(ChatRoomUserModel chatRoomUserModel, TokenModel tokenModel)
        {
            return _context.ChatRoomUsers.Where(s => s.RoomId == chatRoomUserModel.RoomId && s.UserId == chatRoomUserModel.UserId && s.IsActive == true && s.IsDeleted == false).FirstOrDefault();
        }

        public IQueryable<ChatRoomUser> GetRoomInfoByRoomId(int roomId, TokenModel tokenModel)
        {
            return _context.ChatRoomUsers.Where(s => s.RoomId == roomId && s.IsActive == true && s.IsDeleted == false).AsQueryable();
        }
        public IQueryable<T> GetConectionIdInParticularRoom<T>(int roomId,int userId, TokenModel tokenModel) where T : class, new()
        {
            SqlParameter[] parameters = { new SqlParameter("@roomId", roomId),
                                          new SqlParameter("@organizationId", tokenModel.OrganizationID),
                                          new SqlParameter("@userId",userId)
            };
            return _context.ExecStoredProcedureListWithOutput<T>(SQLObjects.CHT_GetUserConnectedIdInRoom.ToString(), parameters.Length, parameters).AsQueryable();
        }

    }
}
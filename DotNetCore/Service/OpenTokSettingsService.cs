using HC.Service;
using System;
using System.Collections.Generic;
using System.Text;
using HC.Patient.Repositories.IRepositories;
using AutoMapper;
using HC.Model;
using HC.Common.HC.Common;
using HC.Patient.Model;
using HC.Patient.Service.IServices;

namespace HC.Patient.Service.Services
{
    public class OpenTokSettingsService : BaseService, IOpenTokSettingsService
    {

        private readonly IOpenTokSettingsRepository _openTokSettingsRepository;
        private readonly IMapper _mapper;
        public OpenTokSettingsService(IOpenTokSettingsRepository openTokSettingsRepository, IMapper mapper)
        {
            _openTokSettingsRepository = openTokSettingsRepository;
            _mapper = mapper;
        }
        public OpenTokSettingModel GetOpenTokSettingsByOrganizationId(TokenModel tokenModel)
        {
            var openTokSettings = _openTokSettingsRepository.GetOpenTokKeysByOrganizationId(tokenModel);
            if (openTokSettings == null)
                return null;

            var openTokModel = _mapper.Map<OpenTokSettingModel>(openTokSettings);
            return openTokModel;
        }
    }
}

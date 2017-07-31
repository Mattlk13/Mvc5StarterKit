﻿using Izenda.BI.Logic.CustomConfiguration;
using Izenda.BI.Framework.Models.UserManagement;
using Rhino.Licensing;

namespace Mvc5StarterKit
{
    public static class IzendaConfig
    {
        public static void RegisterLoginLogic()
        {
            //This is used for exporting only
            UserIntegrationConfig.GetAccessToken = (args) =>
            {
                return IzendaBoundary.IzendaTokenAuthorization.GetToken(new Models.UserInfo()
                {
                    UserName = args.UserName,
                    TenantUniqueName = args.TenantId
                });
            };

            UserIntegrationConfig.ValidateToken = (ValidateTokenArgs args) =>
            {
                var token = args.AccessToken;
                var user = IzendaBoundary.IzendaTokenAuthorization.GetUserInfo(token);

                // TenantUniqueName corresponds to the 'TenantID' field in the IzendaTenant table
                return new ValidateTokenResult { UserName = user.UserName, TenantUniqueName = user.TenantUniqueName };
            };
        }
    }
}

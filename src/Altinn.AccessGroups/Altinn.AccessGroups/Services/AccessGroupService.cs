﻿using Altinn.AccessGroups.Core.Models;
using Altinn.AccessGroups.Interfaces;

namespace Altinn.AccessGroups.Services
{
    public class AccessGroupService : IAccessGroup
    {
        public Task<bool> CreateGroup(AccessGroup accessGroup)
        {
            throw new NotImplementedException();
        }

        public Task<List<AccessGroup>> ExportAccessGroups()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ImportAccessGroups(List<AccessGroup> accessGroups)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateGroup(AccessGroup accessGroup)
        {
            throw new NotImplementedException();
        }
    }
}
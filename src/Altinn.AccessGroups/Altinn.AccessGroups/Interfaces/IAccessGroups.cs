﻿using Altinn.AccessGroups.Core.Models;

namespace Altinn.AccessGroups.Interfaces
{
    public interface IAccessGroup
    {
        Task<bool> CreateGroup(AccessGroup accessGroup);

        Task<bool> UpdateGroup(AccessGroup accessGroup);

        Task<List<AccessGroup>> ExportAccessGroups();

        Task<bool> ImportAccessGroups(List<AccessGroup> accessGroups);
    }
}
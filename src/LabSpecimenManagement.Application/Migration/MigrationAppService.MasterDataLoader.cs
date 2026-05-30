using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp.Data;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    private async Task<MasterData> LoadMasterDataAsync()
    {
        var masterData = new MasterData();

        var indicators = await _indicatorRepository.GetListAsync();
        masterData.IndicatorByName = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var testMethods = await _testMethodRepository.GetListAsync();
        masterData.MethodByName = testMethods
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var users = await _userRepository.GetListAsync();
        foreach (var user in users)
        {
            var fullName = $"{user.Surname} {user.Name}".Trim();
            var key = StringHelper.NormalizeAndTrim(fullName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(key) && !masterData.UserByFullName.ContainsKey(key))
            {
                masterData.UserByFullName[key] = user.Id;
            }

            var deptId = user.GetProperty<Guid?>("DepartmentId");
            masterData.DeptIdByUserId[user.Id] = deptId;
        }

        var laboratories = await _laboratoryRepository.GetListAsync();
        foreach (var lab in laboratories)
        {
            masterData.LabByDeptId[lab.DepartmentId] = lab.Id;

            var codeKey = StringHelper.NormalizeAndTrim(lab.Code).ToLowerInvariant();
            if (!string.IsNullOrEmpty(codeKey))
                masterData.LabByCode[codeKey] = lab.Id;
        }

        var existingRequests = await _testRequestRepository.GetListAsync(x => x.ContractNumber != null);
        masterData.ExistingContracts = new HashSet<string>(
            existingRequests
                .Where(x => !string.IsNullOrWhiteSpace(x.ContractNumber))
                .Select(x => x.ContractNumber!.Trim()),
            StringComparer.OrdinalIgnoreCase
        );

        return masterData;
    }
}

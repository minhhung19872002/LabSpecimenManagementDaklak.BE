using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

public partial class TestRequestAppService
{
    private const string IndicatorCodePrefix = "CT";
    private const string DashTestMethodName = "-";

    [Authorize(LabSpecimenManagementPermissions.TestRequests.Default)]
    public async Task<QuickCreateIndicatorsFromTestRequestResultDto> QuickCreateIndicatorsAsync(
        QuickCreateIndicatorsFromTestRequestDto input)
    {
        if (input.Indicators.Count == 0)
        {
            throw new UserFriendlyException("Vui lòng thêm ít nhất 1 chỉ tiêu");
        }

        var group = await _indicatorGroupRepository.GetAsync(input.IndicatorGroupId);

        var specimenTypeIds = input.Indicators.Select(i => i.SpecimenTypeId).Distinct().ToList();
        var specimenTypes = await _specimenTypeRepository.GetListAsync(s => specimenTypeIds.Contains(s.Id));
        var specimenTypeMap = specimenTypes.ToDictionary(s => s.Id);

        var dashMethod = (await _testMethodRepository.GetListAsync(m => m.Name == DashTestMethodName && m.IsActive))
            .FirstOrDefault();

        var existingCodes = await GetAllExistingIndicatorCodesAsync();
        var nextSequence = GetMaxIndicatorSequence(existingCodes) + 1;

        var results = new List<QuickCreateIndicatorResultItemDto>();

        foreach (var item in input.Indicators)
        {
            if (!specimenTypeMap.TryGetValue(item.SpecimenTypeId, out var specimenType))
            {
                throw new EntityNotFoundException(typeof(SpecimenType), item.SpecimenTypeId);
            }

            var normalizedName = StringHelper.NormalizeAndTrim(item.Name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                continue;
            }

            await EnsureIndicatorDoesNotExistAsync(item.SpecimenTypeId, normalizedName);

            var parentCode = _codeGenerator.GenerateCode(IndicatorCodePrefix, nextSequence - 1);
            existingCodes.Add(parentCode);
            nextSequence++;

            var parentIndicator = new Indicator(
                GuidGenerator.Create(),
                parentCode,
                normalizedName,
                input.IndicatorGroupId,
                input.LaboratoryId)
            {
                IsActive = true,
                FormattedName = SanitizeIndicatorHtml(item.FormattedName),
                NeedsMethodSetup = true,
            };

            await _indicatorRepository.InsertAsync(parentIndicator, autoSave: true);

            await _specimenTypeIndicatorRepository.InsertAsync(
                new SpecimenTypeIndicator(GuidGenerator.Create(), item.SpecimenTypeId, parentIndicator.Id),
                autoSave: true);

            if (dashMethod != null)
            {
                await _testMethodIndicatorRepository.InsertAsync(
                    new TestMethodIndicator(
                        GuidGenerator.Create(),
                        dashMethod.Id,
                        parentIndicator.Id,
                        item.UnitPrice,
                        item.SpecimenTypeId),
                    autoSave: true);
            }

            var parentResult = new QuickCreateIndicatorResultItemDto
            {
                Indicator = MapToIndicatorDto(parentIndicator, group),
                SpecimenTypeId = specimenType.Id,
                SpecimenTypeName = specimenType.Name,
                TestMethodId = dashMethod?.Id,
                TestMethodName = dashMethod?.Name,
                UnitPrice = item.UnitPrice,
            };

            var subSequence = 1;
            foreach (var subItem in item.SubIndicators)
            {
                var normalizedSubName = StringHelper.NormalizeAndTrim(subItem.Name);
                if (string.IsNullOrWhiteSpace(normalizedSubName))
                {
                    continue;
                }

                var subCode = $"{parentCode}-{subSequence:D2}";
                existingCodes.Add(subCode);
                subSequence++;

                var subIndicator = new Indicator(
                    GuidGenerator.Create(),
                    subCode,
                    normalizedSubName,
                    input.IndicatorGroupId,
                    input.LaboratoryId,
                    parentIndicator.Id)
                {
                    IsActive = true,
                    FormattedName = SanitizeIndicatorHtml(subItem.FormattedName),
                    SortOrder = subSequence,
                    NeedsMethodSetup = true,
                };

                await _indicatorRepository.InsertAsync(subIndicator, autoSave: true);

                await _specimenTypeIndicatorRepository.InsertAsync(
                    new SpecimenTypeIndicator(GuidGenerator.Create(), item.SpecimenTypeId, subIndicator.Id),
                    autoSave: true);

                if (dashMethod != null)
                {
                    await _testMethodIndicatorRepository.InsertAsync(
                        new TestMethodIndicator(
                            GuidGenerator.Create(),
                            dashMethod.Id,
                            subIndicator.Id,
                            0,
                            item.SpecimenTypeId),
                        autoSave: true);
                }

                parentResult.SubIndicatorResults.Add(new QuickCreateIndicatorResultItemDto
                {
                    Indicator = MapToIndicatorDto(subIndicator, group),
                    SpecimenTypeId = specimenType.Id,
                    SpecimenTypeName = specimenType.Name,
                    TestMethodId = dashMethod?.Id,
                    TestMethodName = dashMethod?.Name,
                    UnitPrice = 0,
                });
            }

            results.Add(parentResult);
        }

        return new QuickCreateIndicatorsFromTestRequestResultDto { Results = results };
    }

    private static IndicatorDto MapToIndicatorDto(Indicator indicator, IndicatorGroup group)
    {
        return new IndicatorDto
        {
            Id = indicator.Id,
            Code = indicator.Code,
            Name = indicator.Name,
            FormattedName = indicator.FormattedName,
            IndicatorGroupId = group.Id,
            IndicatorGroupName = group.Name,
            IsActive = indicator.IsActive,
            SortOrder = indicator.SortOrder,
            CanDelete = true,
            ParentIndicatorId = indicator.ParentIndicatorId,
        };
    }

    private async Task EnsureIndicatorDoesNotExistAsync(Guid specimenTypeId, string normalizedName)
    {
        var specimenTypeIndicatorQuery = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var linkedIndicatorIds = specimenTypeIndicatorQuery
            .Where(x => x.SpecimenTypeId == specimenTypeId)
            .Select(x => x.IndicatorId)
            .Distinct()
            .ToList();

        if (linkedIndicatorIds.Count == 0)
        {
            return;
        }

        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var alreadyExists = indicatorQuery.Any(x =>
            linkedIndicatorIds.Contains(x.Id) &&
            x.Name.ToLower() == normalizedName.ToLower());

        if (alreadyExists)
        {
            throw new UserFriendlyException($"Chỉ tiêu \"{normalizedName}\" đã tồn tại trong nền mẫu này");
        }
    }

    private async Task<HashSet<string>> GetAllExistingIndicatorCodesAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _indicatorRepository.GetQueryableAsync();
            return query
                .Where(x => x.Code.StartsWith("CT-"))
                .Select(x => x.Code)
                .ToHashSet();
        }
    }

    private static int GetMaxIndicatorSequence(HashSet<string> codes)
    {
        if (!codes.Any())
        {
            return 0;
        }

        return codes
            .Select(code =>
            {
                var parts = code.Replace("CT-", "").Split('-');
                return int.TryParse(parts[0], out var num) ? num : 0;
            })
            .DefaultIfEmpty(0)
            .Max();
    }

    private static string? SanitizeIndicatorHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var allowedTags = new[] { "b", "strong", "i", "em", "u", "sub", "sup", "br" };
        var result = html;

        result = Regex.Replace(
            result,
            @"<script[^>]*>[\s\S]*?</script>",
            "",
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"<style[^>]*>[\s\S]*?</style>",
            "",
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"\s*on\w+\s*=\s*[""'][^""']*[""']",
            "",
            RegexOptions.IgnoreCase);

        result = Regex.Replace(
            result,
            @"javascript\s*:",
            "",
            RegexOptions.IgnoreCase);

        var tagPattern = @"</?([a-zA-Z][a-zA-Z0-9]*)[^>]*>";
        result = Regex.Replace(result, tagPattern, match =>
        {
            var tagName = match.Groups[1].Value.ToLowerInvariant();
            if (allowedTags.Contains(tagName))
            {
                var isClosing = match.Value.StartsWith("</", StringComparison.Ordinal);
                return isClosing ? $"</{tagName}>" : $"<{tagName}>";
            }

            return string.Empty;
        });

        return result.Trim();
    }
}

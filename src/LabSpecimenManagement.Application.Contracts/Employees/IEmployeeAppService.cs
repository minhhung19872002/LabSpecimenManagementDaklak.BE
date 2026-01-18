using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Employees;

public interface IEmployeeAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDto>> GetListAsync(GetEmployeesInput input);
    Task<EmployeeDto> GetAsync(Guid id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto input);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto input);
    Task DeleteAsync(Guid id);
    Task<EmployeeDto> ToggleActiveAsync(Guid id, bool isActive);
    Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id, ResetPasswordDto input);
}

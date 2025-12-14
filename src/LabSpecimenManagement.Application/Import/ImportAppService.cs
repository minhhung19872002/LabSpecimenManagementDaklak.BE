using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Specimens;
using LabSpecimenManagement.TestRequests;
using MiniExcelLibs;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Import;

[RemoteService(IsEnabled = false)]
public class ImportAppService : ApplicationService, IImportAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ImportAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Department, Guid> departmentRepository,
        IGuidGenerator guidGenerator)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _customerRepository = customerRepository;
        _testMethodRepository = testMethodRepository;
        _departmentRepository = departmentRepository;
        _guidGenerator = guidGenerator;
    }

    #region Preview Methods

    public async Task<ImportPreviewDto<TestRequestImportRow>> PreviewTestRequestsAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<TestRequestImportRow>(fileStream);
        var validationResult = await ValidateTestRequestRowsAsync(rows);

        return new ImportPreviewDto<TestRequestImportRow>
        {
            ValidationResult = validationResult,
            ValidData = rows.Where((_, i) => !validationResult.Errors.Any(e => e.RowNumber == i + 2)).ToList()
        };
    }

    public async Task<ImportPreviewDto<SpecimenImportRow>> PreviewSpecimensAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<SpecimenImportRow>(fileStream);
        var validationResult = await ValidateSpecimenRowsAsync(rows);

        return new ImportPreviewDto<SpecimenImportRow>
        {
            ValidationResult = validationResult,
            ValidData = rows.Where((_, i) => !validationResult.Errors.Any(e => e.RowNumber == i + 2)).ToList()
        };
    }

    public Task<ImportPreviewDto<CustomerImportRow>> PreviewCustomersAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<CustomerImportRow>(fileStream);
        var validationResult = ValidateRows(rows);

        return Task.FromResult(new ImportPreviewDto<CustomerImportRow>
        {
            ValidationResult = validationResult,
            ValidData = rows.Where((_, i) => !validationResult.Errors.Any(e => e.RowNumber == i + 2)).ToList()
        });
    }

    public async Task<ImportPreviewDto<TestMethodImportRow>> PreviewTestMethodsAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<TestMethodImportRow>(fileStream);
        var validationResult = await ValidateTestMethodRowsAsync(rows);

        return new ImportPreviewDto<TestMethodImportRow>
        {
            ValidationResult = validationResult,
            ValidData = rows.Where((_, i) => !validationResult.Errors.Any(e => e.RowNumber == i + 2)).ToList()
        };
    }

    #endregion

    #region Import Methods

    public async Task<ImportResultDto> ImportTestRequestsAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<TestRequestImportRow>(fileStream);
        var validationResult = await ValidateTestRequestRowsAsync(rows);

        if (!validationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = validationResult.TotalRows,
                ImportedRows = 0,
                FailedRows = validationResult.InvalidRows,
                Errors = validationResult.Errors,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var errors = new List<ImportRowError>();

        for (var i = 0; i < rows.Count; i++)
        {
            try
            {
                var row = rows[i];
                var requestNumber = await GenerateRequestNumberAsync();

                var entity = new TestRequest(
                    _guidGenerator.Create(),
                    requestNumber,
                    DateTime.Now,
                    row.CustomerName,
                    row.CustomerEmail,
                    row.CustomerPhone
                );

                entity.CustomerInfo = row.CustomerAddress;
                entity.Notes = row.Notes;
                entity.Subcontractor = row.Subcontractor;

                if (row.TatDays.HasValue)
                {
                    entity.ExpectedCompletionDate = DateTime.Now.AddDays(row.TatDays.Value);
                }

                await _testRequestRepository.InsertAsync(entity);
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "General",
                    ErrorMessage = $"Lỗi khi import: {ex.Message}"
                });
            }
        }

        return new ImportResultDto
        {
            Success = errors.Count == 0,
            TotalRows = rows.Count,
            ImportedRows = importedCount,
            FailedRows = errors.Count,
            Errors = errors,
            Message = errors.Count == 0
                ? $"Import thành công {importedCount} yêu cầu thử nghiệm."
                : $"Import thành công {importedCount}/{rows.Count} yêu cầu. Có {errors.Count} lỗi."
        };
    }

    public async Task<ImportResultDto> ImportSpecimensAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<SpecimenImportRow>(fileStream);
        var validationResult = await ValidateSpecimenRowsAsync(rows);

        if (!validationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = validationResult.TotalRows,
                ImportedRows = 0,
                FailedRows = validationResult.InvalidRows,
                Errors = validationResult.Errors,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var errors = new List<ImportRowError>();

        // Cache test requests
        var testRequests = await _testRequestRepository.GetListAsync();
        var testRequestDict = testRequests.ToDictionary(x => x.RequestNumber, x => x.Id);

        for (var i = 0; i < rows.Count; i++)
        {
            try
            {
                var row = rows[i];

                if (!testRequestDict.TryGetValue(row.TestRequestNumber, out var testRequestId))
                {
                    errors.Add(new ImportRowError
                    {
                        RowNumber = i + 2,
                        ColumnName = "TestRequestNumber",
                        ErrorMessage = $"Không tìm thấy yêu cầu '{row.TestRequestNumber}'",
                        Value = row.TestRequestNumber
                    });
                    continue;
                }

                var specimenCode = await GenerateSpecimenCodeAsync();

                var entity = new Specimen(
                    _guidGenerator.Create(),
                    testRequestId,
                    specimenCode,
                    row.Name,
                    row.Type,
                    row.ReceivedDate ?? DateTime.Now
                );

                entity.StorageLocation = row.StorageLocation;
                entity.Quantity = row.Quantity;
                entity.Unit = row.Unit;
                entity.Deviation = row.Deviation;
                entity.Notes = row.Notes;

                await _specimenRepository.InsertAsync(entity);
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "General",
                    ErrorMessage = $"Lỗi khi import: {ex.Message}"
                });
            }
        }

        return new ImportResultDto
        {
            Success = errors.Count == 0,
            TotalRows = rows.Count,
            ImportedRows = importedCount,
            FailedRows = errors.Count,
            Errors = errors,
            Message = errors.Count == 0
                ? $"Import thành công {importedCount} mẫu."
                : $"Import thành công {importedCount}/{rows.Count} mẫu. Có {errors.Count} lỗi."
        };
    }

    public async Task<ImportResultDto> ImportCustomersAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<CustomerImportRow>(fileStream);
        var validationResult = ValidateRows(rows);

        if (!validationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = validationResult.TotalRows,
                ImportedRows = 0,
                FailedRows = validationResult.InvalidRows,
                Errors = validationResult.Errors,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var skippedCount = 0;
        var errors = new List<ImportRowError>();

        // Check for existing customers by name
        var existingCustomers = await _customerRepository.GetListAsync();
        var existingNames = existingCustomers.Select(x => x.Name.ToLower()).ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            try
            {
                var row = rows[i];

                // Skip if customer already exists
                if (existingNames.Contains(row.Name.ToLower()))
                {
                    skippedCount++;
                    continue;
                }

                var code = await GenerateCustomerCodeAsync();

                var entity = new Customer(_guidGenerator.Create(), code, row.Name);
                entity.Email = row.Email;
                entity.Phone = row.Phone;
                entity.Address = row.Address;
                entity.TaxCode = row.TaxCode;
                entity.ContactPerson = row.ContactPerson;
                entity.ContactPhone = row.ContactPhone;

                await _customerRepository.InsertAsync(entity);
                existingNames.Add(row.Name.ToLower());
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "General",
                    ErrorMessage = $"Lỗi khi import: {ex.Message}"
                });
            }
        }

        return new ImportResultDto
        {
            Success = errors.Count == 0,
            TotalRows = rows.Count,
            ImportedRows = importedCount,
            SkippedRows = skippedCount,
            FailedRows = errors.Count,
            Errors = errors,
            Message = errors.Count == 0
                ? $"Import thành công {importedCount} khách hàng. Bỏ qua {skippedCount} khách hàng đã tồn tại."
                : $"Import thành công {importedCount}/{rows.Count} khách hàng. Có {errors.Count} lỗi."
        };
    }

    public async Task<ImportResultDto> ImportTestMethodsAsync(Stream fileStream)
    {
        var rows = ReadExcelRows<TestMethodImportRow>(fileStream);
        var validationResult = await ValidateTestMethodRowsAsync(rows);

        if (!validationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = validationResult.TotalRows,
                ImportedRows = 0,
                FailedRows = validationResult.InvalidRows,
                Errors = validationResult.Errors,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var skippedCount = 0;
        var errors = new List<ImportRowError>();

        // Cache departments and existing methods
        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments.ToDictionary(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var existingMethods = await _testMethodRepository.GetListAsync();
        var existingCodes = existingMethods.Select(x => x.Code.ToLower()).ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            try
            {
                var row = rows[i];

                // Skip if method code already exists
                if (existingCodes.Contains(row.Code.ToLower()))
                {
                    skippedCount++;
                    continue;
                }

                Guid? departmentId = null;
                if (!string.IsNullOrWhiteSpace(row.DepartmentCode))
                {
                    departmentDict.TryGetValue(row.DepartmentCode, out var deptId);
                    departmentId = deptId == Guid.Empty ? null : deptId;
                }

                var entity = new TestMethod(_guidGenerator.Create(), row.Code, row.Name);
                entity.Standard = row.Standard;
                entity.DepartmentId = departmentId;

                await _testMethodRepository.InsertAsync(entity);
                existingCodes.Add(row.Code.ToLower());
                importedCount++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "General",
                    ErrorMessage = $"Lỗi khi import: {ex.Message}"
                });
            }
        }

        return new ImportResultDto
        {
            Success = errors.Count == 0,
            TotalRows = rows.Count,
            ImportedRows = importedCount,
            SkippedRows = skippedCount,
            FailedRows = errors.Count,
            Errors = errors,
            Message = errors.Count == 0
                ? $"Import thành công {importedCount} phương pháp. Bỏ qua {skippedCount} phương pháp đã tồn tại."
                : $"Import thành công {importedCount}/{rows.Count} phương pháp. Có {errors.Count} lỗi."
        };
    }

    #endregion

    #region Helper Methods

    private List<T> ReadExcelRows<T>(Stream fileStream) where T : class, new()
    {
        return fileStream.Query<T>().ToList();
    }

    private ImportValidationResult ValidateRows<T>(List<T> rows) where T : class
    {
        var result = new ImportValidationResult
        {
            TotalRows = rows.Count
        };

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = row 1)

            var validationContext = new ValidationContext(row);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(row, validationContext, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    result.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = validationResult.MemberNames.FirstOrDefault() ?? "Unknown",
                        ErrorMessage = validationResult.ErrorMessage ?? "Lỗi không xác định"
                    });
                }
            }
        }

        result.ValidRows = rows.Count - result.Errors.Select(e => e.RowNumber).Distinct().Count();
        return result;
    }

    private async Task<ImportValidationResult> ValidateTestRequestRowsAsync(List<TestRequestImportRow> rows)
    {
        var result = ValidateRows(rows);

        // Additional business validation
        var duplicateNames = rows
            .GroupBy(x => x.CustomerName?.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Any())
        {
            for (var i = 0; i < rows.Count; i++)
            {
                if (duplicateNames.Contains(rows[i].CustomerName?.ToLower()))
                {
                    // This is just a warning - we allow duplicate customer names
                }
            }
        }

        return await Task.FromResult(result);
    }

    private async Task<ImportValidationResult> ValidateSpecimenRowsAsync(List<SpecimenImportRow> rows)
    {
        var result = ValidateRows(rows);

        // Validate TestRequestNumber exists
        var testRequests = await _testRequestRepository.GetListAsync();
        var validRequestNumbers = testRequests.Select(x => x.RequestNumber).ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (!string.IsNullOrWhiteSpace(row.TestRequestNumber) &&
                !validRequestNumbers.Contains(row.TestRequestNumber))
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "TestRequestNumber",
                    ErrorMessage = $"Mã yêu cầu '{row.TestRequestNumber}' không tồn tại trong hệ thống",
                    Value = row.TestRequestNumber
                });
            }
        }

        result.ValidRows = rows.Count - result.Errors.Select(e => e.RowNumber).Distinct().Count();
        return result;
    }

    private async Task<ImportValidationResult> ValidateTestMethodRowsAsync(List<TestMethodImportRow> rows)
    {
        var result = ValidateRows(rows);

        // Check for duplicate codes in the file
        var duplicateCodes = rows
            .GroupBy(x => x.Code?.ToLower())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            if (duplicateCodes.Contains(rows[i].Code?.ToLower()))
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "Code",
                    ErrorMessage = $"Mã phương pháp '{rows[i].Code}' bị trùng trong file",
                    Value = rows[i].Code
                });
            }
        }

        // Validate department codes if provided
        var departments = await _departmentRepository.GetListAsync();
        var validDeptCodes = departments.Select(x => x.Code.ToLower()).ToHashSet();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (!string.IsNullOrWhiteSpace(row.DepartmentCode) &&
                !validDeptCodes.Contains(row.DepartmentCode.ToLower()))
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = i + 2,
                    ColumnName = "DepartmentCode",
                    ErrorMessage = $"Mã phòng ban '{row.DepartmentCode}' không tồn tại",
                    Value = row.DepartmentCode
                });
            }
        }

        result.ValidRows = rows.Count - result.Errors.Select(e => e.RowNumber).Distinct().Count();
        return result;
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        var today = DateTime.Now;
        var prefix = $"REQ-{today:yyyyMMdd}";
        var count = await _testRequestRepository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }

    private async Task<string> GenerateSpecimenCodeAsync()
    {
        var today = DateTime.Now;
        var prefix = $"SPE-{today:yyyyMMdd}";
        var count = await _specimenRepository.CountAsync(x => x.Code.StartsWith(prefix));
        return $"{prefix}-{(count + 1):D4}";
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        var count = await _customerRepository.CountAsync();
        return $"CUST-{(count + 1):D4}";
    }

    #endregion

    #region Template Methods

    public byte[] GetTestRequestTemplate()
    {
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "CustomerName", "Công ty ABC" },
                { "CustomerEmail", "abc@example.com" },
                { "CustomerPhone", "0901234567" },
                { "CustomerAddress", "123 Đường XYZ, TP.HCM" },
                { "TatDays", 7 },
                { "Subcontractor", "" },
                { "Notes", "Ghi chú mẫu" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    public byte[] GetSpecimenTemplate()
    {
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "TestRequestNumber", "REQ-20241201-0001" },
                { "Name", "Mẫu nước thải" },
                { "Type", "Nước thải công nghiệp" },
                { "ReceivedDate", DateTime.Now.ToString("yyyy-MM-dd") },
                { "StorageLocation", "Kho A - Tầng 1" },
                { "Quantity", 1.5 },
                { "Unit", "lít" },
                { "Deviation", "" },
                { "Notes", "Ghi chú mẫu" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    public byte[] GetCustomerTemplate()
    {
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "Name", "Công ty TNHH ABC" },
                { "Email", "contact@abc.com" },
                { "Phone", "0281234567" },
                { "Address", "456 Đường DEF, Quận 1, TP.HCM" },
                { "TaxCode", "0123456789" },
                { "ContactPerson", "Nguyễn Văn A" },
                { "ContactPhone", "0901234567" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    public byte[] GetTestMethodTemplate()
    {
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "Code", "PM-001" },
                { "Name", "Phương pháp đo pH" },
                { "Standard", "TCVN 6492:2011" },
                { "DepartmentCode", "DEPT-001" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    #endregion
}

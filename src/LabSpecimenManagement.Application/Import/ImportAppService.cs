using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Helpers;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Specimens;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Hosting;
using MiniExcelLibs;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
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
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<TestRequestSample, Guid> _testRequestSampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _testRequestSampleIndicatorRepository;
    private readonly IRepository<Organization, Guid> _organizationRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<SampleTypeCategory, Guid> _sampleTypeCategoryRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IDataFilter _dataFilter;

    public ImportAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<TestRequestSample, Guid> testRequestSampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> testRequestSampleIndicatorRepository,
        IRepository<Organization, Guid> organizationRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<SampleTypeCategory, Guid> sampleTypeCategoryRepository,
        IGuidGenerator guidGenerator,
        ICodeGeneratorService codeGenerator,
        IWebHostEnvironment webHostEnvironment,
        IDataFilter dataFilter)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _customerRepository = customerRepository;
        _testMethodRepository = testMethodRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _departmentRepository = departmentRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _testRequestSampleRepository = testRequestSampleRepository;
        _testRequestSampleIndicatorRepository = testRequestSampleIndicatorRepository;
        _organizationRepository = organizationRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _sampleTypeCategoryRepository = sampleTypeCategoryRepository;
        _guidGenerator = guidGenerator;
        _codeGenerator = codeGenerator;
        _webHostEnvironment = webHostEnvironment;
        _dataFilter = dataFilter;
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
                    row.CustomerName
                );

                entity.Phone = row.CustomerPhone;
                entity.Address = row.CustomerAddress;
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

        // Cache existing methods
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

                var entity = new TestMethod(_guidGenerator.Create(), row.Code, row.Name, row.Unit ?? "");

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

    private Task<ImportValidationResult> ValidateTestMethodRowsAsync(List<TestMethodImportRow> rows)
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

        result.ValidRows = rows.Count - result.Errors.Select(e => e.RowNumber).Distinct().Count();
        return Task.FromResult(result);
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("REQ");
            var count = await _testRequestRepository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("REQ", count);
        }
    }

    private async Task<string> GenerateSpecimenCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("SPE");
            var count = await _specimenRepository.CountAsync(x => x.Code.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("SPE", count);
        }
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var count = await _customerRepository.CountAsync();
            return _codeGenerator.GenerateCode("CUST", count);
        }
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

    public byte[] GetTestMethodWithIndicatorsTemplate()
    {
        // Try to read from static template file first
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "Phương pháp.xlsx");

        if (File.Exists(templatePath))
        {
            return File.ReadAllBytes(templatePath);
        }

        // Fallback: Generate template dynamically
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "Tên phương pháp", "Phương pháp đo pH" },
                { "Mô tả", "" },
                { "Đơn vị tính", "mg/L" },
                { "Chỉ tiêu", "pH" },
                { "Đơn giá", 50000 }
            },
            new()
            {
                { "Tên phương pháp", "Phương pháp đo pH" },
                { "Mô tả", "" },
                { "Đơn vị tính", "mg/L" },
                { "Chỉ tiêu", "Độ đục" },
                { "Đơn giá", 60000 }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    #endregion

    #region TestMethod with Indicators Import

    public async Task<TestMethodImportPreviewDto> PreviewTestMethodsWithIndicatorsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewTestMethodsWithIndicatorsInternalAsync(memoryStream);
    }

    private async Task<TestMethodImportPreviewDto> PreviewTestMethodsWithIndicatorsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadExcelRowsWithVietnameseHeaders(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation (use GroupBy to handle duplicate names after normalization)
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var result = new TestMethodImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        // Group by TestMethodName
        var groups = rows
            .Select((row, idx) => (Row: row, RowNumber: idx + 2))
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Row.TestMethodName).ToLowerInvariant())
            .ToList();

        foreach (var group in groups)
        {
            var firstRow = group.First().Row;
            var testMethodKey = group.Key;

            // Validate TestMethodName not empty
            if (string.IsNullOrWhiteSpace(testMethodKey))
            {
                foreach (var (row, rowNumber) in group)
                {
                    var error = "Tên phương pháp không được để trống";
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Tên phương pháp",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                continue;
            }

            var previewGroup = new TestMethodImportPreviewGroup
            {
                TestMethodName = firstRow.TestMethodName?.Trim() ?? "",
                FormattedName = firstRow.FormattedName,
                Description = firstRow.Description?.Trim(),
                Unit = firstRow.Unit?.Trim() ?? "",
                IsNew = !testMethodDict.ContainsKey(testMethodKey)
            };

            if (!previewGroup.IsNew)
            {
                previewGroup.ExistingId = testMethodDict[testMethodKey];
            }

            // Check for duplicate indicators within the group
            var indicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (row, rowNumber) in group)
            {
                var indicatorKey = StringHelper.NormalizeAndTrim(row.IndicatorName).ToLowerInvariant();
                var item = new TestMethodIndicatorImportItem
                {
                    RowNumber = rowNumber,
                    IndicatorName = row.IndicatorName?.Trim() ?? "",
                    UnitPrice = row.UnitPrice,
                    IsValid = true
                };

                var rowErrorMessages = new List<string>();

                // Validate indicator name not empty
                if (string.IsNullOrWhiteSpace(indicatorKey))
                {
                    item.IsValid = false;
                    var error = "Tên chỉ tiêu không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Chỉ tiêu",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else
                {
                    // Validate indicator exists (only if name is not empty)
                    if (!indicatorDict.TryGetValue(indicatorKey, out var indicatorId))
                    {
                        item.IsValid = false;
                        var error = $"Chỉ tiêu '{row.IndicatorName}' không tồn tại trong hệ thống";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Chỉ tiêu",
                            ErrorMessage = error,
                            Value = row.IndicatorName
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                    else
                    {
                        item.IndicatorId = indicatorId;

                        // Check duplicate indicator in same test method
                        if (!indicatorNames.Add(indicatorKey))
                        {
                            item.IsValid = false;
                            var error = $"Chỉ tiêu '{row.IndicatorName}' bị trùng trong phương pháp này";
                            rowErrorMessages.Add(error);
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = rowNumber,
                                ColumnName = "Chỉ tiêu",
                                ErrorMessage = error,
                                Value = row.IndicatorName
                            });
                            AddRowError(rowErrors, rowNumber, error);
                        }
                    }
                }

                // Validate UnitPrice (always check regardless of other errors)
                if (row.UnitPrice < 0)
                {
                    var error = "Đơn giá phải >= 0";
                    item.IsValid = false;
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Đơn giá",
                        ErrorMessage = error,
                        Value = row.UnitPrice.ToString()
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }

                // Validate Unit not empty (always check regardless of other errors)
                if (string.IsNullOrWhiteSpace(row.Unit))
                {
                    var error = "Đơn vị tính không được để trống";
                    item.IsValid = false;
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Đơn vị tính",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }

                // Set combined error message for display
                if (rowErrorMessages.Count > 0)
                {
                    item.ErrorMessage = string.Join("; ", rowErrorMessages);
                }

                previewGroup.Indicators.Add(item);
            }

            result.Groups.Add(previewGroup);
        }

        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalTestMethods = result.Groups.Count;
        result.NewTestMethods = result.Groups.Count(g => g.IsNew);
        result.UpdateTestMethods = result.Groups.Count(g => !g.IsNew);
        result.TotalIndicators = result.Groups.Sum(g => g.Indicators.Count);

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_PhuongPhap_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    public async Task<ImportResultDto> ImportTestMethodsWithIndicatorsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewTestMethodsWithIndicatorsInternalAsync(memoryStream);

        if (!preview.ValidationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Errors = preview.ValidationResult.Errors,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var updatedCount = 0;
        var codeOffset = 0; // Track number of codes generated in this session

        try
        {
            foreach (var group in preview.Groups)
            {
                TestMethod testMethod;

                if (group.IsNew)
                {
                    // Create new TestMethod
                    var code = await GenerateTestMethodCodeAsync(codeOffset);
                    codeOffset++; // Increment offset for next code
                    testMethod = new TestMethod(
                        _guidGenerator.Create(),
                        code,
                        group.TestMethodName,
                        group.Unit
                    )
                    {
                        // Use FormattedName (with HTML formatting) from Excel
                        FormattedName = group.FormattedName
                    };
                    await _testMethodRepository.InsertAsync(testMethod);
                    importedCount++;
                }
                else
                {
                    // Update existing TestMethod
                    testMethod = await _testMethodRepository.GetAsync(group.ExistingId!.Value);
                    testMethod.Unit = group.Unit;
                    // Update FormattedName if provided
                    if (!string.IsNullOrWhiteSpace(group.FormattedName))
                    {
                        testMethod.FormattedName = group.FormattedName;
                    }
                    await _testMethodRepository.UpdateAsync(testMethod);
                    updatedCount++;
                }

                // Remove existing indicators
                var existingIndicators = await _testMethodIndicatorRepository
                    .GetListAsync(x => x.TestMethodId == testMethod.Id);
                if (existingIndicators.Any())
                {
                    await _testMethodIndicatorRepository.DeleteManyAsync(existingIndicators);
                }

                // Add indicators
                var sortOrder = 0;
                foreach (var item in group.Indicators.Where(i => i.IsValid && i.IndicatorId.HasValue))
                {
                    var indicator = new TestMethodIndicator(
                        _guidGenerator.Create(),
                        testMethod.Id,
                        item.IndicatorId!.Value,
                        item.UnitPrice
                    )
                    {
                        SortOrder = sortOrder++
                    };
                    await _testMethodIndicatorRepository.InsertAsync(indicator);
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = importedCount,
                SkippedRows = updatedCount,
                Message = $"Import thành công: {importedCount} phương pháp mới, {updatedCount} phương pháp cập nhật."
            };
        }
        catch (Exception ex)
        {
            // Transaction will be rolled back automatically by UnitOfWork
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}. Đã rollback tất cả thay đổi."
            };
        }
    }

    private List<TestMethodWithIndicatorImportRow> ReadExcelRowsWithVietnameseHeaders(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var result = new List<TestMethodWithIndicatorImportRow>();

        // Find header row and column indices
        var headerRow = worksheet.Row(1);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var headerName = cell.GetString()?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(headerName))
            {
                columnMap[headerName] = cell.Address.ColumnNumber;
            }
        }

        // Find column indices for each field
        int testMethodCol = FindColumnIndex(columnMap, "tên phương pháp", "tenpp", "ten phuong phap", "tên pp", "ten pp");
        int descriptionCol = FindColumnIndex(columnMap, "mô tả", "mo ta", "mota", "description");
        int unitCol = FindColumnIndex(columnMap, "đơn vị tính", "don vi tinh", "dvt", "đơn vị", "don vi", "unit", "đvt");
        int indicatorCol = FindColumnIndex(columnMap, "chỉ tiêu", "chi tieu", "chitieu", "indicator", "tên chỉ tiêu", "ten chi tieu");
        int priceCol = FindColumnIndex(columnMap, "đơn giá", "don gia", "dongia", "gia", "giá", "unit price", "price");

        // Read data rows (starting from row 2)
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (!row.CellsUsed().Any())
                continue;

            var importRow = new TestMethodWithIndicatorImportRow
            {
                TestMethodName = testMethodCol > 0 ? row.Cell(testMethodCol).GetString()?.Trim() ?? "" : "",
                FormattedName = testMethodCol > 0 ? GetCellRichTextAsHtml(row.Cell(testMethodCol)) : null,
                Description = descriptionCol > 0 ? row.Cell(descriptionCol).GetString()?.Trim() : null,
                Unit = unitCol > 0 ? row.Cell(unitCol).GetString()?.Trim() ?? "" : "",
                IndicatorName = indicatorCol > 0 ? row.Cell(indicatorCol).GetString()?.Trim() ?? "" : "",
                UnitPrice = priceCol > 0 ? GetCellDecimalValue(row.Cell(priceCol)) : 0
            };

            result.Add(importRow);
        }

        return result;
    }

    private int FindColumnIndex(Dictionary<string, int> columnMap, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            if (columnMap.TryGetValue(name.ToLowerInvariant(), out var index))
            {
                return index;
            }
        }
        return 0;
    }

    private decimal GetCellDecimalValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return 0;

        if (cell.DataType == XLDataType.Number)
        {
            return (decimal)cell.GetDouble();
        }

        var strValue = cell.GetString()?.Trim() ?? "";
        if (decimal.TryParse(strValue, out var result))
        {
            return result;
        }
        return 0;
    }

    /// <summary>
    /// Convert Excel cell rich text to HTML (bold, italic, underline, subscript, superscript)
    /// </summary>
    private string? GetCellRichTextAsHtml(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        // Check if cell has rich text
        if (!cell.HasRichText || cell.GetRichText().Count <= 1)
        {
            // Single format - check if the whole cell has formatting
            var font = cell.Style.Font;
            var text = cell.GetString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(text)) return null;

            // Apply formatting if exists
            var hasFormatting = font.Bold || font.Italic || font.Underline != XLFontUnderlineValues.None ||
                               font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript ||
                               font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript;

            if (!hasFormatting) return text;

            return ApplyHtmlFormatting(text, font.Bold, font.Italic,
                font.Underline != XLFontUnderlineValues.None,
                font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript,
                font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript);
        }

        // Multiple rich text runs
        var sb = new System.Text.StringBuilder();
        foreach (var richText in cell.GetRichText())
        {
            var text = richText.Text;
            if (string.IsNullOrEmpty(text)) continue;

            var formatted = ApplyHtmlFormatting(text,
                richText.Bold,
                richText.Italic,
                richText.Underline != XLFontUnderlineValues.None,
                richText.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript,
                richText.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript);

            sb.Append(formatted);
        }

        var result = sb.ToString().Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private string ApplyHtmlFormatting(string text, bool bold, bool italic, bool underline, bool subscript, bool superscript)
    {
        // Escape HTML special characters
        text = System.Web.HttpUtility.HtmlEncode(text);

        if (subscript) text = $"<sub>{text}</sub>";
        if (superscript) text = $"<sup>{text}</sup>";
        if (underline) text = $"<u>{text}</u>";
        if (italic) text = $"<i>{text}</i>";
        if (bold) text = $"<b>{text}</b>";

        return text;
    }

    private Dictionary<string, object> NormalizeKeys(IDictionary<string, object> dict)
    {
        var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in dict)
        {
            var normalizedKey = kvp.Key?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(normalizedKey) && !normalized.ContainsKey(normalizedKey))
            {
                normalized[normalizedKey] = kvp.Value;
            }
        }
        return normalized;
    }

    private string GetStringValueNormalized(Dictionary<string, object> dict, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (dict.TryGetValue(key.ToLowerInvariant(), out var value) && value != null)
            {
                return value.ToString()?.Trim() ?? "";
            }
        }
        return "";
    }

    private decimal GetDecimalValueNormalized(Dictionary<string, object> dict, params string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (dict.TryGetValue(key.ToLowerInvariant(), out var value) && value != null)
            {
                if (value is decimal d) return d;
                if (value is double dbl) return (decimal)dbl;
                if (value is int i) return i;
                if (value is long l) return l;
                if (decimal.TryParse(value.ToString(), out var parsed)) return parsed;
            }
        }
        return 0;
    }

    private string GetStringValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            return value.ToString()?.Trim() ?? "";
        }
        return "";
    }

    private decimal GetDecimalValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is decimal d) return d;
            if (value is double dbl) return (decimal)dbl;
            if (value is int i) return i;
            if (value is long l) return l;
            if (decimal.TryParse(value.ToString(), out var parsed)) return parsed;
        }
        return 0;
    }

    private async Task<string> GenerateTestMethodCodeAsync(int offset = 0)
    {
        var today = DateTime.Now;
        var datePrefix = $"PP{today:ddMMyyyy}";

        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _testMethodRepository.GetQueryableAsync();
            var maxCode = query
                .Where(x => x.Code.StartsWith(datePrefix))
                .Select(x => x.Code)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            int nextNumber = 1;
            if (maxCode != null)
            {
                var numberPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(numberPart, out var num))
                {
                    nextNumber = num + 1;
                }
            }

            // Add offset to handle multiple inserts in the same batch/session
            return $"{datePrefix}{(nextNumber + offset):D3}";
        }
    }

    private void AddRowError(Dictionary<int, List<string>> rowErrors, int rowNumber, string error)
    {
        if (!rowErrors.ContainsKey(rowNumber))
        {
            rowErrors[rowNumber] = new List<string>();
        }
        rowErrors[rowNumber].Add(error);
    }

    private byte[] GenerateErrorFile(
        List<TestMethodWithIndicatorImportRow> rows,
        Dictionary<int, List<string>> rowErrors,
        List<ImportRowError> allErrors)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Import Errors");

        // Column mapping
        var columns = new[] { "Tên phương pháp", "Mô tả", "Đơn vị tính", "Chỉ tiêu", "Đơn giá", "Lỗi" };
        var columnNameMapping = new Dictionary<string, int>
        {
            { "Tên phương pháp", 1 },
            { "Mô tả", 2 },
            { "Đơn vị tính", 3 },
            { "Chỉ tiêu", 4 },
            { "Đơn giá", 5 },
            { "Lỗi", 6 }
        };

        // Write header row with styling
        for (int col = 0; col < columns.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Build error lookup: rowNumber -> list of error column names
        var errorColumnsByRow = new Dictionary<int, HashSet<string>>();
        foreach (var error in allErrors)
        {
            if (!errorColumnsByRow.ContainsKey(error.RowNumber))
            {
                errorColumnsByRow[error.RowNumber] = new HashSet<string>();
            }
            errorColumnsByRow[error.RowNumber].Add(error.ColumnName);
        }

        // Write data rows
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRowNumber = i + 2; // Excel row (1-based, header = 1)
            var dataRowNumber = i + 2; // Row number used in validation (matching rowNumber in errors)

            var hasError = rowErrors.ContainsKey(dataRowNumber);
            var errorColumnsForRow = errorColumnsByRow.GetValueOrDefault(dataRowNumber, new HashSet<string>());

            // Column 1: Tên phương pháp
            var cell1 = worksheet.Cell(excelRowNumber, 1);
            cell1.Value = row.TestMethodName ?? "";
            if (errorColumnsForRow.Contains("Tên phương pháp"))
            {
                ApplyErrorStyle(cell1);
            }

            // Column 2: Mô tả
            var cell2 = worksheet.Cell(excelRowNumber, 2);
            cell2.Value = row.Description ?? "";
            if (errorColumnsForRow.Contains("Mô tả"))
            {
                ApplyErrorStyle(cell2);
            }

            // Column 3: Đơn vị tính
            var cell3 = worksheet.Cell(excelRowNumber, 3);
            cell3.Value = row.Unit ?? "";
            if (errorColumnsForRow.Contains("Đơn vị tính"))
            {
                ApplyErrorStyle(cell3);
            }

            // Column 4: Chỉ tiêu
            var cell4 = worksheet.Cell(excelRowNumber, 4);
            cell4.Value = row.IndicatorName ?? "";
            if (errorColumnsForRow.Contains("Chỉ tiêu"))
            {
                ApplyErrorStyle(cell4);
            }

            // Column 5: Đơn giá
            var cell5 = worksheet.Cell(excelRowNumber, 5);
            cell5.Value = row.UnitPrice;
            if (errorColumnsForRow.Contains("Đơn giá"))
            {
                ApplyErrorStyle(cell5);
            }

            // Column 6: Lỗi
            var cell6 = worksheet.Cell(excelRowNumber, 6);
            if (rowErrors.TryGetValue(dataRowNumber, out var errors) && errors.Count > 0)
            {
                cell6.Value = string.Join("; ", errors);
                cell6.Style.Font.FontColor = XLColor.Red;
                cell6.Style.Font.Bold = true;
            }

            // Highlight entire row background if has errors
            if (hasError)
            {
                var rowRange = worksheet.Range(excelRowNumber, 1, excelRowNumber, 6);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Make error column wider if needed
        if (worksheet.Column(6).Width < 50)
        {
            worksheet.Column(6).Width = 50;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ApplyErrorStyle(IXLCell cell)
    {
        cell.Style.Fill.BackgroundColor = XLColor.LightPink;
        cell.Style.Font.FontColor = XLColor.DarkRed;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cell.Style.Border.OutsideBorderColor = XLColor.Red;
    }

    #endregion

    #region Standard with Indicators Import

    // Valid comparison operators
    private static readonly HashSet<string> ValidComparisonOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "<", ">", "<=", ">=", "=", "Khoảng", "khoảng", "KHOẢNG"
    };

    private static readonly HashSet<string> ValidLowerOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        ">", ">="
    };

    private static readonly HashSet<string> ValidUpperOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "<", "<="
    };

    public byte[] GetStandardWithIndicatorsTemplate()
    {
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "Tiêu chuẩn.xlsx");

        if (File.Exists(templatePath))
        {
            return File.ReadAllBytes(templatePath);
        }

        // Fallback: Generate template dynamically
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "Tên tiêu chuẩn", "QCVN 01:2018/BYT" },
                { "Chỉ tiêu", "pH" },
                { "Phương pháp", "TCVN 6492:2011" },
                { "Đơn vị", "mg/L" },
                { "So sánh", "<=" },
                { "Giới hạn", "6.5" },
                { "So sánh giới hạn dưới", "" },
                { "Giới hạn dưới", "" },
                { "So sánh giới hạn trên", "" },
                { "Giới hạn trên", "" }
            },
            new()
            {
                { "Tên tiêu chuẩn", "QCVN 01:2018/BYT" },
                { "Chỉ tiêu", "Độ đục" },
                { "Phương pháp", "TCVN 6492:2011" },
                { "Đơn vị", "NTU" },
                { "So sánh", "Khoảng" },
                { "Giới hạn", "" },
                { "So sánh giới hạn dưới", ">=" },
                { "Giới hạn dưới", "0" },
                { "So sánh giới hạn trên", "<=" },
                { "Giới hạn trên", "5" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    public async Task<StandardImportPreviewDto> PreviewStandardsWithIndicatorsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewStandardsWithIndicatorsInternalAsync(memoryStream);
    }

    private async Task<StandardImportPreviewDto> PreviewStandardsWithIndicatorsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadStandardExcelRows(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation (use GroupBy to handle duplicate names after normalization)
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var standards = await _standardRepository.GetListAsync();
        var standardDict = standards
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var result = new StandardImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        // Group by StandardName
        var groups = rows
            .Select((row, idx) => (Row: row, RowNumber: idx + 2))
            .GroupBy(x => x.Row.StandardName?.Trim().ToLowerInvariant() ?? "")
            .ToList();

        foreach (var group in groups)
        {
            var firstRow = group.First().Row;
            var standardKey = group.Key;

            // Validate StandardName not empty
            if (string.IsNullOrWhiteSpace(standardKey))
            {
                foreach (var (row, rowNumber) in group)
                {
                    var error = "Tên tiêu chuẩn không được để trống";
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Tên tiêu chuẩn",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                continue;
            }

            var previewGroup = new StandardImportPreviewGroup
            {
                StandardName = firstRow.StandardName?.Trim() ?? "",
                IsNew = !standardDict.ContainsKey(standardKey)
            };

            if (!previewGroup.IsNew)
            {
                previewGroup.ExistingId = standardDict[standardKey];
            }

            // Check for duplicate indicators within the group (same Indicator + TestMethod)
            var indicatorMethodPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (row, rowNumber) in group)
            {
                var indicatorKey = StringHelper.NormalizeAndTrim(row.IndicatorName).ToLowerInvariant();
                var testMethodKey = StringHelper.NormalizeAndTrim(row.TestMethodName).ToLowerInvariant();
                var pairKey = $"{indicatorKey}||{testMethodKey}";

                var item = new StandardIndicatorImportItem
                {
                    RowNumber = rowNumber,
                    IndicatorName = row.IndicatorName?.Trim() ?? "",
                    TestMethodName = row.TestMethodName?.Trim() ?? "",
                    Unit = row.Unit?.Trim() ?? "",
                    ComparisonOperator = row.ComparisonOperator?.Trim() ?? "",
                    AllowedLimit = row.AllowedLimit?.Trim(),
                    LowerOperator = row.LowerOperator?.Trim(),
                    LowerLimit = row.LowerLimit?.Trim(),
                    UpperOperator = row.UpperOperator?.Trim(),
                    UpperLimit = row.UpperLimit?.Trim(),
                    IsValid = true
                };

                var rowErrorMessages = new List<string>();

                // Validate indicator name not empty
                if (string.IsNullOrWhiteSpace(indicatorKey))
                {
                    item.IsValid = false;
                    var error = "Tên chỉ tiêu không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Chỉ tiêu",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!indicatorDict.TryGetValue(indicatorKey, out var indicator))
                {
                    item.IsValid = false;
                    var error = $"Chỉ tiêu '{row.IndicatorName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Chỉ tiêu",
                        ErrorMessage = error,
                        Value = row.IndicatorName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else
                {
                    item.IndicatorId = indicator.Id;
                }

                // Validate test method name not empty
                if (string.IsNullOrWhiteSpace(testMethodKey))
                {
                    item.IsValid = false;
                    var error = "Tên phương pháp không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Phương pháp",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!testMethodDict.TryGetValue(testMethodKey, out var testMethod))
                {
                    item.IsValid = false;
                    var error = $"Phương pháp '{row.TestMethodName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Phương pháp",
                        ErrorMessage = error,
                        Value = row.TestMethodName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else
                {
                    item.TestMethodId = testMethod.Id;
                }

                // Validate Unit not empty
                if (string.IsNullOrWhiteSpace(row.Unit))
                {
                    item.IsValid = false;
                    var error = "Đơn vị không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Đơn vị",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }

                // Validate duplicate Indicator + TestMethod in same Standard
                if (!string.IsNullOrWhiteSpace(indicatorKey) && !string.IsNullOrWhiteSpace(testMethodKey))
                {
                    if (!indicatorMethodPairs.Add(pairKey))
                    {
                        item.IsValid = false;
                        var error = $"Chỉ tiêu '{row.IndicatorName}' + Phương pháp '{row.TestMethodName}' bị trùng trong tiêu chuẩn này";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Chỉ tiêu",
                            ErrorMessage = error,
                            Value = $"{row.IndicatorName} | {row.TestMethodName}"
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                }

                // Validate ComparisonOperator
                var comparison = row.ComparisonOperator?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(comparison))
                {
                    item.IsValid = false;
                    var error = "Ký hiệu so sánh không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "So sánh",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!ValidComparisonOperators.Contains(comparison))
                {
                    item.IsValid = false;
                    var error = $"Ký hiệu so sánh '{comparison}' không hợp lệ. Chỉ được: <, >, <=, >=, =, Khoảng";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "So sánh",
                        ErrorMessage = error,
                        Value = comparison
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else
                {
                    // Validate based on comparison type
                    var isRange = comparison.Equals("Khoảng", StringComparison.OrdinalIgnoreCase);

                    if (!isRange)
                    {
                        // Single limit validation: AllowedLimit required
                        if (string.IsNullOrWhiteSpace(row.AllowedLimit))
                        {
                            item.IsValid = false;
                            var error = "Giới hạn không được để trống khi So sánh không phải 'Khoảng'";
                            rowErrorMessages.Add(error);
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = rowNumber,
                                ColumnName = "Giới hạn",
                                ErrorMessage = error
                            });
                            AddRowError(rowErrors, rowNumber, error);
                        }
                    }
                    else
                    {
                        // Range validation
                        ValidateRangeFields(row, rowNumber, item, rowErrorMessages, validationResult, rowErrors);
                    }
                }

                // Set combined error message for display
                if (rowErrorMessages.Count > 0)
                {
                    item.ErrorMessage = string.Join("; ", rowErrorMessages);
                }

                previewGroup.Indicators.Add(item);
            }

            result.Groups.Add(previewGroup);
        }

        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalStandards = result.Groups.Count;
        result.NewStandards = result.Groups.Count(g => g.IsNew);
        result.UpdateStandards = result.Groups.Count(g => !g.IsNew);
        result.TotalIndicators = result.Groups.Sum(g => g.Indicators.Count);

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateStandardErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_TieuChuan_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    private void ValidateRangeFields(
        StandardWithIndicatorImportRow row,
        int rowNumber,
        StandardIndicatorImportItem item,
        List<string> rowErrorMessages,
        ImportValidationResult validationResult,
        Dictionary<int, List<string>> rowErrors)
    {
        // LowerOperator required
        var lowerOp = row.LowerOperator?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(lowerOp))
        {
            item.IsValid = false;
            var error = "So sánh giới hạn dưới là bắt buộc khi So sánh = 'Khoảng'";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "So sánh giới hạn dưới",
                ErrorMessage = error
            });
            AddRowError(rowErrors, rowNumber, error);
        }
        else if (!ValidLowerOperators.Contains(lowerOp))
        {
            item.IsValid = false;
            var error = $"So sánh giới hạn dưới '{lowerOp}' không hợp lệ. Chỉ được: >, >=";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "So sánh giới hạn dưới",
                ErrorMessage = error,
                Value = lowerOp
            });
            AddRowError(rowErrors, rowNumber, error);
        }

        // LowerLimit required
        if (string.IsNullOrWhiteSpace(row.LowerLimit))
        {
            item.IsValid = false;
            var error = "Giới hạn dưới là bắt buộc khi So sánh = 'Khoảng'";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "Giới hạn dưới",
                ErrorMessage = error
            });
            AddRowError(rowErrors, rowNumber, error);
        }

        // UpperOperator required
        var upperOp = row.UpperOperator?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(upperOp))
        {
            item.IsValid = false;
            var error = "So sánh giới hạn trên là bắt buộc khi So sánh = 'Khoảng'";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "So sánh giới hạn trên",
                ErrorMessage = error
            });
            AddRowError(rowErrors, rowNumber, error);
        }
        else if (!ValidUpperOperators.Contains(upperOp))
        {
            item.IsValid = false;
            var error = $"So sánh giới hạn trên '{upperOp}' không hợp lệ. Chỉ được: <, <=";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "So sánh giới hạn trên",
                ErrorMessage = error,
                Value = upperOp
            });
            AddRowError(rowErrors, rowNumber, error);
        }

        // UpperLimit required
        if (string.IsNullOrWhiteSpace(row.UpperLimit))
        {
            item.IsValid = false;
            var error = "Giới hạn trên là bắt buộc khi So sánh = 'Khoảng'";
            rowErrorMessages.Add(error);
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                ColumnName = "Giới hạn trên",
                ErrorMessage = error
            });
            AddRowError(rowErrors, rowNumber, error);
        }

        // Validate LowerLimit < UpperLimit (if both are numbers)
        if (!string.IsNullOrWhiteSpace(row.LowerLimit) && !string.IsNullOrWhiteSpace(row.UpperLimit))
        {
            if (decimal.TryParse(row.LowerLimit, out var lowerVal) && decimal.TryParse(row.UpperLimit, out var upperVal))
            {
                if (lowerVal >= upperVal)
                {
                    item.IsValid = false;
                    var error = $"Giới hạn dưới ({lowerVal}) phải nhỏ hơn Giới hạn trên ({upperVal})";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Giới hạn",
                        ErrorMessage = error,
                        Value = $"{lowerVal} - {upperVal}"
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
            }
        }
    }

    public async Task<ImportResultDto> ImportStandardsWithIndicatorsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewStandardsWithIndicatorsInternalAsync(memoryStream);

        if (!preview.ValidationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var importedCount = 0;
        var updatedCount = 0;
        var codeOffset = 0;

        try
        {
            foreach (var group in preview.Groups)
            {
                Standard standard;

                if (group.IsNew)
                {
                    // Create new Standard
                    var code = await GenerateStandardCodeAsync(codeOffset);
                    codeOffset++;
                    standard = new Standard(
                        _guidGenerator.Create(),
                        code,
                        group.StandardName
                    );
                    await _standardRepository.InsertAsync(standard);
                    importedCount++;
                }
                else
                {
                    // Update existing Standard (just update LastModificationTime by touching it)
                    standard = await _standardRepository.GetAsync(group.ExistingId!.Value);
                    await _standardRepository.UpdateAsync(standard);
                    updatedCount++;
                }

                // Remove existing indicators
                var existingIndicators = await _standardIndicatorRepository
                    .GetListAsync(x => x.StandardId == standard.Id);
                if (existingIndicators.Any())
                {
                    await _standardIndicatorRepository.DeleteManyAsync(existingIndicators);
                }

                // Add indicators from import
                var sortOrder = 0;
                foreach (var item in group.Indicators.Where(i => i.IsValid && i.IndicatorId.HasValue && i.TestMethodId.HasValue))
                {
                    var isRange = item.ComparisonOperator?.Equals("Khoảng", StringComparison.OrdinalIgnoreCase) == true;

                    var standardIndicator = new StandardIndicator(
                        _guidGenerator.Create(),
                        standard.Id,
                        item.IndicatorId!.Value,
                        item.TestMethodId!.Value,
                        item.Unit
                    )
                    {
                        ComparisonOperator = isRange ? null : item.ComparisonOperator,
                        AllowedLimit = isRange ? null : item.AllowedLimit,
                        LowerOperator = isRange ? item.LowerOperator : null,
                        LowerLimit = isRange ? item.LowerLimit : null,
                        UpperOperator = isRange ? item.UpperOperator : null,
                        UpperLimit = isRange ? item.UpperLimit : null,
                        IsActive = true,
                        SortOrder = sortOrder++
                    };
                    await _standardIndicatorRepository.InsertAsync(standardIndicator);
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = importedCount,
                SkippedRows = updatedCount,
                Message = $"Import thành công: {importedCount} tiêu chuẩn mới, {updatedCount} tiêu chuẩn cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}. Đã rollback tất cả thay đổi."
            };
        }
    }

    private List<StandardWithIndicatorImportRow> ReadStandardExcelRows(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var result = new List<StandardWithIndicatorImportRow>();

        // Find header row and column indices
        var headerRow = worksheet.Row(1);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var headerName = cell.GetString()?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(headerName))
            {
                columnMap[headerName] = cell.Address.ColumnNumber;
            }
        }

        // Find column indices for each field
        int standardNameCol = FindColumnIndex(columnMap, "tên tiêu chuẩn", "tiêu chuẩn", "ten tieu chuan", "tieu chuan", "standard");
        int indicatorCol = FindColumnIndex(columnMap, "chỉ tiêu", "chi tieu", "chitieu", "indicator", "tên chỉ tiêu");
        int testMethodCol = FindColumnIndex(columnMap, "phương pháp", "phuong phap", "pp", "test method", "method");
        int unitCol = FindColumnIndex(columnMap, "đơn vị", "don vi", "đvt", "dvt", "unit");
        int comparisonCol = FindColumnIndex(columnMap, "so sánh", "so sanh", "comparison", "operator");
        int limitCol = FindColumnIndex(columnMap, "giới hạn", "gioi han", "limit", "allowed limit");
        int lowerOpCol = FindColumnIndex(columnMap, "so sánh giới hạn dưới", "so sanh gioi han duoi", "lower operator", "lower op");
        int lowerLimitCol = FindColumnIndex(columnMap, "giới hạn dưới", "gioi han duoi", "lower limit");
        int upperOpCol = FindColumnIndex(columnMap, "so sánh giới hạn trên", "so sanh gioi han tren", "upper operator", "upper op");
        int upperLimitCol = FindColumnIndex(columnMap, "giới hạn trên", "gioi han tren", "upper limit");

        // Read data rows (starting from row 2)
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (!row.CellsUsed().Any())
                continue;

            var importRow = new StandardWithIndicatorImportRow
            {
                StandardName = standardNameCol > 0 ? row.Cell(standardNameCol).GetString()?.Trim() ?? "" : "",
                IndicatorName = indicatorCol > 0 ? row.Cell(indicatorCol).GetString()?.Trim() ?? "" : "",
                TestMethodName = testMethodCol > 0 ? row.Cell(testMethodCol).GetString()?.Trim() ?? "" : "",
                Unit = unitCol > 0 ? row.Cell(unitCol).GetString()?.Trim() ?? "" : "",
                ComparisonOperator = comparisonCol > 0 ? row.Cell(comparisonCol).GetString()?.Trim() ?? "" : "",
                AllowedLimit = limitCol > 0 ? row.Cell(limitCol).GetString()?.Trim() : null,
                LowerOperator = lowerOpCol > 0 ? row.Cell(lowerOpCol).GetString()?.Trim() : null,
                LowerLimit = lowerLimitCol > 0 ? row.Cell(lowerLimitCol).GetString()?.Trim() : null,
                UpperOperator = upperOpCol > 0 ? row.Cell(upperOpCol).GetString()?.Trim() : null,
                UpperLimit = upperLimitCol > 0 ? row.Cell(upperLimitCol).GetString()?.Trim() : null
            };

            result.Add(importRow);
        }

        return result;
    }

    private async Task<string> GenerateStandardCodeAsync(int offset = 0)
    {
        var today = DateTime.Now;
        var datePrefix = $"TC{today:ddMMyyyy}";

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _standardRepository.GetQueryableAsync();
            var maxCode = query
                .Where(x => x.Code.StartsWith(datePrefix))
                .Select(x => x.Code)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            int nextNumber = 1;
            if (maxCode != null)
            {
                var numberPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(numberPart, out var num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{datePrefix}{(nextNumber + offset):D3}";
        }
    }

    private byte[] GenerateStandardErrorFile(
        List<StandardWithIndicatorImportRow> rows,
        Dictionary<int, List<string>> rowErrors,
        List<ImportRowError> allErrors)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Import Errors");

        // Column headers
        var columns = new[] {
            "Tên tiêu chuẩn", "Chỉ tiêu", "Phương pháp", "Đơn vị", "So sánh",
            "Giới hạn", "So sánh giới hạn dưới", "Giới hạn dưới",
            "So sánh giới hạn trên", "Giới hạn trên", "Lỗi"
        };

        // Write header row with styling
        for (int col = 0; col < columns.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Build error lookup
        var errorColumnsByRow = new Dictionary<int, HashSet<string>>();
        foreach (var error in allErrors)
        {
            if (!errorColumnsByRow.ContainsKey(error.RowNumber))
            {
                errorColumnsByRow[error.RowNumber] = new HashSet<string>();
            }
            errorColumnsByRow[error.RowNumber].Add(error.ColumnName);
        }

        // Write data rows
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRowNumber = i + 2;
            var dataRowNumber = i + 2;

            var hasError = rowErrors.ContainsKey(dataRowNumber);

            worksheet.Cell(excelRowNumber, 1).Value = row.StandardName ?? "";
            worksheet.Cell(excelRowNumber, 2).Value = row.IndicatorName ?? "";
            worksheet.Cell(excelRowNumber, 3).Value = row.TestMethodName ?? "";
            worksheet.Cell(excelRowNumber, 4).Value = row.Unit ?? "";
            worksheet.Cell(excelRowNumber, 5).Value = row.ComparisonOperator ?? "";
            worksheet.Cell(excelRowNumber, 6).Value = row.AllowedLimit ?? "";
            worksheet.Cell(excelRowNumber, 7).Value = row.LowerOperator ?? "";
            worksheet.Cell(excelRowNumber, 8).Value = row.LowerLimit ?? "";
            worksheet.Cell(excelRowNumber, 9).Value = row.UpperOperator ?? "";
            worksheet.Cell(excelRowNumber, 10).Value = row.UpperLimit ?? "";

            // Error column
            var cell11 = worksheet.Cell(excelRowNumber, 11);
            if (rowErrors.TryGetValue(dataRowNumber, out var errors) && errors.Count > 0)
            {
                cell11.Value = string.Join("; ", errors);
                cell11.Style.Font.FontColor = XLColor.Red;
                cell11.Style.Font.Bold = true;
            }

            // Highlight row if has errors
            if (hasError)
            {
                var rowRange = worksheet.Range(excelRowNumber, 1, excelRowNumber, 11);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Make error column wider
        if (worksheet.Column(11).Width < 50)
        {
            worksheet.Column(11).Width = 50;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Organization + TestRequest Import (Legacy Data)

    public byte[] GetOrganizationTestRequestTemplate()
    {
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "Thông tin cơ sở doanh nghiệp.xlsx");

        if (File.Exists(templatePath))
        {
            return File.ReadAllBytes(templatePath);
        }

        // Fallback: Generate template dynamically
        var templateData = new List<Dictionary<string, object>>
        {
            new()
            {
                { "Tên cơ sở", "Công ty TNHH ABC" },
                { "Người liên hệ", "Nguyễn Văn A" },
                { "Điện thoại", "0901234567" },
                { "Địa chỉ", "123 Đường XYZ" },
                { "Tỉnh/thành", "Đắk Lắk" },
                { "Phường/xã", "Tân An" },
                { "Mã số thuế", "0123456789" },
                { "Số hợp đồng", "HĐ-2024-001" },
                { "Tên mẫu", "Nước sinh hoạt" },
                { "Tình trạng", "Bình thường" },
                { "Mã số mẫu", "M001" },
                { "Chỉ tiêu", "pH, Độ đục, COD" }
            }
        };

        using var stream = new MemoryStream();
        stream.SaveAs(templateData);
        return stream.ToArray();
    }

    public async Task<OrganizationTestRequestImportPreviewDto> PreviewOrganizationTestRequestsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewOrganizationTestRequestsInternalAsync(memoryStream);
    }

    private async Task<OrganizationTestRequestImportPreviewDto> PreviewOrganizationTestRequestsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadOrganizationTestRequestExcelRows(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation (use GroupBy to handle duplicate names after normalization)
        var organizations = await _organizationRepository.GetListAsync();
        var organizationDict = organizations
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);

        // Load existing TestRequests by ContractNumber for upsert check
        // Use GroupBy + First to handle duplicate ContractNumbers (take the first one)
        var testRequests = await _testRequestRepository.GetListAsync(tr => tr.ContractNumber != null);
        var testRequestDict = testRequests
            .Where(tr => !string.IsNullOrWhiteSpace(tr.ContractNumber))
            .GroupBy(x => x.ContractNumber!.Trim().ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.First());

        var result = new OrganizationTestRequestImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newOrgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateOrgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalIndicatorCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            // Try to find Province/Ward code from name (for legacy import data)
            var provinceName = row.Province?.Trim() ?? "";
            var wardName = row.Ward?.Trim() ?? "";
            var provinceCode = VietnamLocationHelper.FindProvinceCode(provinceName);
            var wardCode = provinceCode != null
                ? VietnamLocationHelper.FindWardCode(wardName, provinceCode)
                : VietnamLocationHelper.FindWardCode(wardName);

            var item = new OrganizationTestRequestPreviewItem
            {
                RowNumber = rowNumber,
                OrganizationName = row.OrganizationName?.Trim() ?? "",
                ContactPerson = row.ContactPerson?.Trim() ?? "",
                Phone = row.Phone?.Trim(),
                Address = row.Address?.Trim() ?? "",
                Province = provinceName, // Display name for preview
                ProvinceCode = provinceCode, // Code for saving
                Ward = wardName, // Display name for preview
                WardCode = wardCode, // Code for saving
                TaxCode = row.TaxCode?.Trim() ?? "",
                ContractNumber = row.ContractNumber?.Trim(),
                SampleName = row.SampleName?.Trim(),
                SampleCode = row.SampleCode?.Trim(),
                SampleStatus = row.SampleStatus?.Trim(),
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.OrganizationName))
            {
                item.IsValid = false;
                var error = "Tên cơ sở không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên cơ sở",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.ContactPerson))
            {
                item.IsValid = false;
                var error = "Người liên hệ không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Người liên hệ",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Address))
            {
                item.IsValid = false;
                var error = "Địa chỉ không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Địa chỉ",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Province))
            {
                item.IsValid = false;
                var error = "Tỉnh/thành không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tỉnh/thành",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Ward))
            {
                item.IsValid = false;
                var error = "Phường/xã không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Phường/xã",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.TaxCode))
            {
                item.IsValid = false;
                var error = "Mã số thuế không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Mã số thuế",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            // Check if organization exists
            var organizationKey = StringHelper.NormalizeAndTrim(row.OrganizationName).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(organizationKey))
            {
                if (organizationDict.TryGetValue(organizationKey, out var existingOrg))
                {
                    item.IsNewOrganization = false;
                    item.ExistingOrganizationId = existingOrg.Id;
                    updateOrgNames.Add(organizationKey);
                }
                else
                {
                    item.IsNewOrganization = true;
                    newOrgNames.Add(organizationKey);
                }
            }

            // Check if TestRequest exists (by ContractNumber)
            var contractKey = row.ContractNumber?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrWhiteSpace(contractKey))
            {
                if (testRequestDict.TryGetValue(contractKey, out var existingTestRequest))
                {
                    item.IsNewTestRequest = false;
                    item.ExistingTestRequestId = existingTestRequest.Id;
                }
                else
                {
                    item.IsNewTestRequest = true;
                }
            }
            else
            {
                // No ContractNumber = always new TestRequest
                item.IsNewTestRequest = true;
            }

            // Parse and validate indicators
            if (!string.IsNullOrWhiteSpace(row.Indicators))
            {
                var indicatorNames = row.Indicators.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToList();

                foreach (var indicatorName in indicatorNames)
                {
                    item.IndicatorNames.Add(indicatorName);
                    var indicatorKey = StringHelper.NormalizeAndTrim(indicatorName).ToLowerInvariant();

                    if (indicatorDict.TryGetValue(indicatorKey, out var indicatorId))
                    {
                        item.IndicatorIds.Add(indicatorId);
                        totalIndicatorCount++;
                    }
                    else
                    {
                        item.InvalidIndicators.Add(indicatorName);
                        item.IsValid = false;
                        var error = $"Chỉ tiêu '{indicatorName}' không tồn tại trong hệ thống";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Chỉ tiêu",
                            ErrorMessage = error,
                            Value = indicatorName
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                }
            }

            // Set combined error message
            if (rowErrorMessages.Count > 0)
            {
                item.Errors = rowErrorMessages;
            }

            result.Items.Add(item);
        }

        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalRows = rows.Count;
        result.ValidRows = result.ValidationResult.ValidRows;
        result.InvalidRows = result.ValidationResult.InvalidRows;
        result.NewOrganizations = newOrgNames.Count;
        result.UpdateOrganizations = updateOrgNames.Count;

        // Count unique ContractNumbers for TestRequests (rows with same ContractNumber = 1 TestRequest)
        var validItems = result.Items.Where(i => i.IsValid).ToList();

        // Group items by ContractNumber
        var itemsWithContract = validItems
            .Where(i => !string.IsNullOrWhiteSpace(i.ContractNumber))
            .GroupBy(i => i.ContractNumber!.Trim().ToLowerInvariant())
            .ToList();

        var itemsWithoutContract = validItems.Count(i => string.IsNullOrWhiteSpace(i.ContractNumber));

        // Count new vs update TestRequests
        var newTestRequestCount = itemsWithContract.Count(g => g.First().IsNewTestRequest) + itemsWithoutContract; // Items without contract are always new
        var updateTestRequestCount = itemsWithContract.Count(g => !g.First().IsNewTestRequest);

        result.TotalTestRequests = itemsWithContract.Count + itemsWithoutContract;
        result.NewTestRequests = newTestRequestCount;
        result.UpdateTestRequests = updateTestRequestCount;
        result.TotalIndicators = totalIndicatorCount;

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateOrganizationTestRequestErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_CoSo_PhieuYeuCau_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    public async Task<ImportResultDto> ImportOrganizationTestRequestsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewOrganizationTestRequestsInternalAsync(memoryStream);

        if (!preview.ValidationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var newOrgCount = 0;
        var updatedOrgCount = 0;
        var testRequestCount = 0;
        var requestNumberOffset = 0;

        // Track processed organizations to avoid duplicate operations in same batch
        var processedOrganizations = new Dictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Load existing organizations
            var existingOrganizations = await _organizationRepository.GetListAsync();
            foreach (var org in existingOrganizations)
            {
                var key = StringHelper.NormalizeAndTrim(org.Name).ToLowerInvariant();
                if (!processedOrganizations.ContainsKey(key))
                {
                    processedOrganizations[key] = org;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            // First pass: Process all organizations (upsert)
            var processedOrgKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in validItems)
            {
                var organizationKey = StringHelper.NormalizeAndTrim(item.OrganizationName).ToLowerInvariant();

                // Skip if already processed in this batch
                if (processedOrgKeys.Contains(organizationKey))
                    continue;

                processedOrgKeys.Add(organizationKey);

                // Organization upsert
                // Use ProvinceCode/WardCode if found, otherwise fallback to Province/Ward name (legacy)
                var orgProvinceCode = item.ProvinceCode ?? item.Province;
                var orgWardCode = item.WardCode ?? item.Ward;

                if (processedOrganizations.TryGetValue(organizationKey, out var existingOrg))
                {
                    // Update existing organization
                    existingOrg.ContactPerson = item.ContactPerson;
                    existingOrg.Phone = item.Phone ?? "";
                    existingOrg.Address = item.Address;
                    existingOrg.ProvinceCode = orgProvinceCode;
                    existingOrg.WardCode = orgWardCode;
                    existingOrg.TaxCode = item.TaxCode;
                    await _organizationRepository.UpdateAsync(existingOrg);

                    if (item.IsNewOrganization)
                    {
                        newOrgCount++;
                    }
                    else
                    {
                        updatedOrgCount++;
                    }
                }
                else
                {
                    // Create new organization
                    var organization = new Organization(_guidGenerator.Create(), item.OrganizationName)
                    {
                        ContactPerson = item.ContactPerson,
                        Phone = item.Phone ?? "",
                        Address = item.Address,
                        ProvinceCode = orgProvinceCode,
                        WardCode = orgWardCode,
                        TaxCode = item.TaxCode,
                        IsActive = true
                    };
                    await _organizationRepository.InsertAsync(organization);
                    processedOrganizations[organizationKey] = organization;
                    newOrgCount++;
                }
            }

            // Second pass: Group items by ContractNumber and create/update TestRequests
            // Items with same ContractNumber = 1 TestRequest with multiple Samples
            // Items without ContractNumber = each item is a separate TestRequest
            var itemsWithContract = validItems
                .Where(i => !string.IsNullOrWhiteSpace(i.ContractNumber))
                .GroupBy(i => i.ContractNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var itemsWithoutContract = validItems
                .Where(i => string.IsNullOrWhiteSpace(i.ContractNumber))
                .ToList();

            // Load existing TestRequests by ContractNumber for upsert
            // Use GroupBy + First to handle duplicate ContractNumbers (take the first one)
            var contractNumbers = itemsWithContract.Select(g => g.Key).ToList();
            var existingTestRequests = await _testRequestRepository.GetListAsync(
                tr => tr.ContractNumber != null && contractNumbers.Contains(tr.ContractNumber));
            var existingTestRequestDict = existingTestRequests
                .Where(tr => !string.IsNullOrWhiteSpace(tr.ContractNumber))
                .GroupBy(tr => tr.ContractNumber!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var newTestRequestCount = 0;
            var updatedTestRequestCount = 0;

            // Process grouped items (by ContractNumber)
            foreach (var group in itemsWithContract)
            {
                var firstItem = group.First();
                var contractKey = firstItem.ContractNumber!.Trim().ToLowerInvariant();
                TestRequest testRequest;

                // Count samples in this group (items with sample info)
                var sampleCount = group.Count(item =>
                    !string.IsNullOrWhiteSpace(item.SampleName) || !string.IsNullOrWhiteSpace(item.SampleCode));

                // Check if TestRequest with this ContractNumber already exists
                if (existingTestRequestDict.TryGetValue(contractKey, out var existingTestRequest))
                {
                    // Update existing TestRequest
                    existingTestRequest.CustomerName = firstItem.OrganizationName;
                    existingTestRequest.ContactPerson = firstItem.ContactPerson;
                    existingTestRequest.Phone = firstItem.Phone;
                    existingTestRequest.Address = firstItem.Address;
                    existingTestRequest.ProvinceCode = firstItem.ProvinceCode;
                    existingTestRequest.Province = firstItem.Province;
                    existingTestRequest.WardCode = firstItem.WardCode;
                    existingTestRequest.Ward = firstItem.Ward;
                    existingTestRequest.TaxCode = firstItem.TaxCode;
                    existingTestRequest.TotalSampleCount += sampleCount; // Add new samples to existing count
                    await _testRequestRepository.UpdateAsync(existingTestRequest);
                    testRequest = existingTestRequest;
                    updatedTestRequestCount++;
                }
                else
                {
                    // Create new TestRequest
                    var requestNumber = await GenerateRequestNumberForImportAsync(requestNumberOffset);
                    requestNumberOffset++;

                    testRequest = new TestRequest(
                        _guidGenerator.Create(),
                        requestNumber,
                        DateTime.Now,
                        firstItem.OrganizationName
                    )
                    {
                        Status = TestRequestStatus.Completed,
                        ContractNumber = firstItem.ContractNumber,
                        ContactPerson = firstItem.ContactPerson,
                        Phone = firstItem.Phone,
                        Address = firstItem.Address,
                        ProvinceCode = firstItem.ProvinceCode,
                        Province = firstItem.Province,
                        WardCode = firstItem.WardCode,
                        Ward = firstItem.Ward,
                        TaxCode = firstItem.TaxCode,
                        TotalSampleCount = sampleCount,
                        CompletedDate = DateTime.Now
                    };
                    await _testRequestRepository.InsertAsync(testRequest);
                    newTestRequestCount++;
                }

                // Create samples for each item in the group
                foreach (var item in group)
                {
                    if (!string.IsNullOrWhiteSpace(item.SampleName) || !string.IsNullOrWhiteSpace(item.SampleCode))
                    {
                        var sample = new TestRequestSample(
                            _guidGenerator.Create(),
                            testRequest.Id,
                            item.SampleCode ?? "",
                            item.SampleName ?? ""
                        )
                        {
                            StatusText = item.SampleStatus,
                            Status = SampleStatus.Completed
                        };
                        await _testRequestSampleRepository.InsertAsync(sample);

                        // Create TestRequestSampleIndicator for each indicator
                        foreach (var indicatorId in item.IndicatorIds)
                        {
                            var sampleIndicator = new TestRequestSampleIndicator(
                                _guidGenerator.Create(),
                                sample.Id,
                                indicatorId
                            );
                            await _testRequestSampleIndicatorRepository.InsertAsync(sampleIndicator);
                        }
                    }
                }

                testRequestCount++;
            }

            // Process items without ContractNumber (each item = 1 TestRequest, always create new)
            foreach (var item in itemsWithoutContract)
            {
                var requestNumber = await GenerateRequestNumberForImportAsync(requestNumberOffset);
                requestNumberOffset++;

                // Check if this item has sample info
                var hasSample = !string.IsNullOrWhiteSpace(item.SampleName) || !string.IsNullOrWhiteSpace(item.SampleCode);

                var testRequest = new TestRequest(
                    _guidGenerator.Create(),
                    requestNumber,
                    DateTime.Now,
                    item.OrganizationName
                )
                {
                    Status = TestRequestStatus.Completed,
                    ContractNumber = item.ContractNumber,
                    ContactPerson = item.ContactPerson,
                    Phone = item.Phone,
                    Address = item.Address,
                    ProvinceCode = item.ProvinceCode,
                    Province = item.Province,
                    WardCode = item.WardCode,
                    Ward = item.Ward,
                    TaxCode = item.TaxCode,
                    TotalSampleCount = hasSample ? 1 : 0,
                    CompletedDate = DateTime.Now
                };
                await _testRequestRepository.InsertAsync(testRequest);
                newTestRequestCount++;

                // Create TestRequestSample if sample info provided
                if (hasSample)
                {
                    var sample = new TestRequestSample(
                        _guidGenerator.Create(),
                        testRequest.Id,
                        item.SampleCode ?? "",
                        item.SampleName ?? ""
                    )
                    {
                        StatusText = item.SampleStatus,
                        Status = SampleStatus.Completed
                    };
                    await _testRequestSampleRepository.InsertAsync(sample);

                    // Create TestRequestSampleIndicator for each indicator
                    foreach (var indicatorId in item.IndicatorIds)
                    {
                        var sampleIndicator = new TestRequestSampleIndicator(
                            _guidGenerator.Create(),
                            sample.Id,
                            indicatorId
                        );
                        await _testRequestSampleIndicatorRepository.InsertAsync(sampleIndicator);
                    }
                }

                testRequestCount++;
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = testRequestCount,
                Message = $"Import thành công: {newOrgCount} cơ sở mới, {updatedOrgCount} cơ sở cập nhật, {newTestRequestCount} phiếu yêu cầu mới, {updatedTestRequestCount} phiếu yêu cầu cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}. Đã rollback tất cả thay đổi."
            };
        }
    }

    private List<OrganizationTestRequestImportRow> ReadOrganizationTestRequestExcelRows(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var result = new List<OrganizationTestRequestImportRow>();

        // Find header row and column indices
        var headerRow = worksheet.Row(1);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var headerName = cell.GetString()?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(headerName))
            {
                columnMap[headerName] = cell.Address.ColumnNumber;
            }
        }

        // Find column indices for each field
        int orgNameCol = FindColumnIndex(columnMap, "tên cơ sở", "ten co so", "cơ sở", "co so", "organization", "tên khách hàng", "ten khach hang");
        int contactPersonCol = FindColumnIndex(columnMap, "người liên hệ", "nguoi lien he", "contact person", "contact", "liên hệ", "lien he");
        int phoneCol = FindColumnIndex(columnMap, "điện thoại", "dien thoai", "phone", "sđt", "sdt", "số điện thoại");
        int addressCol = FindColumnIndex(columnMap, "địa chỉ", "dia chi", "address");
        int provinceCol = FindColumnIndex(columnMap, "tỉnh/thành", "tỉnh/ thành", "tinh/thanh", "tinh/ thanh", "tỉnh thành", "tinh thanh", "province", "tỉnh", "tinh");
        int wardCol = FindColumnIndex(columnMap, "phường/xã", "phường/ xã", "phuong/xa", "phuong/ xa", "phường xã", "phuong xa", "ward", "xã", "xa", "phường", "phuong");
        int taxCodeCol = FindColumnIndex(columnMap, "mã số thuế", "ma so thue", "mst", "tax code", "taxcode");
        int contractCol = FindColumnIndex(columnMap, "số hợp đồng", "so hop dong", "hợp đồng", "hop dong", "contract");
        int sampleNameCol = FindColumnIndex(columnMap, "tên mẫu", "ten mau", "sample name", "mẫu", "mau");
        int sampleStatusCol = FindColumnIndex(columnMap, "tình trạng", "tinh trang", "status", "trạng thái", "trang thai");
        int sampleCodeCol = FindColumnIndex(columnMap, "mã số mẫu", "ma so mau", "mã mẫu", "ma mau", "sample code");
        int indicatorsCol = FindColumnIndex(columnMap, "chỉ tiêu", "chi tieu", "indicators", "indicator");

        // Read data rows (starting from row 2)
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (!row.CellsUsed().Any())
                continue;

            var importRow = new OrganizationTestRequestImportRow
            {
                OrganizationName = orgNameCol > 0 ? row.Cell(orgNameCol).GetString()?.Trim() ?? "" : "",
                ContactPerson = contactPersonCol > 0 ? row.Cell(contactPersonCol).GetString()?.Trim() ?? "" : "",
                Phone = phoneCol > 0 ? row.Cell(phoneCol).GetString()?.Trim() : null,
                Address = addressCol > 0 ? row.Cell(addressCol).GetString()?.Trim() ?? "" : "",
                Province = provinceCol > 0 ? row.Cell(provinceCol).GetString()?.Trim() ?? "" : "",
                Ward = wardCol > 0 ? row.Cell(wardCol).GetString()?.Trim() ?? "" : "",
                TaxCode = taxCodeCol > 0 ? row.Cell(taxCodeCol).GetString()?.Trim() ?? "" : "",
                ContractNumber = contractCol > 0 ? row.Cell(contractCol).GetString()?.Trim() : null,
                SampleName = sampleNameCol > 0 ? row.Cell(sampleNameCol).GetString()?.Trim() : null,
                SampleStatus = sampleStatusCol > 0 ? row.Cell(sampleStatusCol).GetString()?.Trim() : null,
                SampleCode = sampleCodeCol > 0 ? row.Cell(sampleCodeCol).GetString()?.Trim() : null,
                Indicators = indicatorsCol > 0 ? row.Cell(indicatorsCol).GetString()?.Trim() : null
            };

            result.Add(importRow);
        }

        return result;
    }

    private async Task<string> GenerateCustomerCodeForImportAsync(int offset = 0)
    {
        var today = DateTime.Now;
        var datePrefix = $"CS{today:ddMMyyyy}";

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _customerRepository.GetQueryableAsync();
            var maxCode = query
                .Where(x => x.Code.StartsWith(datePrefix))
                .Select(x => x.Code)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            int nextNumber = 1;
            if (maxCode != null)
            {
                var numberPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(numberPart, out var num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{datePrefix}{(nextNumber + offset):D3}";
        }
    }

    private async Task<string> GenerateRequestNumberForImportAsync(int offset = 0)
    {
        var today = DateTime.Now;
        var datePrefix = $"YC{today:ddMMyyyy}";

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var query = await _testRequestRepository.GetQueryableAsync();
            var maxCode = query
                .Where(x => x.RequestNumber.StartsWith(datePrefix))
                .Select(x => x.RequestNumber)
                .OrderByDescending(x => x)
                .FirstOrDefault();

            int nextNumber = 1;
            if (maxCode != null)
            {
                var numberPart = maxCode.Substring(datePrefix.Length);
                if (int.TryParse(numberPart, out var num))
                {
                    nextNumber = num + 1;
                }
            }

            return $"{datePrefix}{(nextNumber + offset):D3}";
        }
    }

    private byte[] GenerateOrganizationTestRequestErrorFile(
        List<OrganizationTestRequestImportRow> rows,
        Dictionary<int, List<string>> rowErrors,
        List<ImportRowError> allErrors)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Import Errors");

        // Column headers
        var columns = new[] {
            "Tên cơ sở", "Người liên hệ", "Điện thoại", "Địa chỉ", "Tỉnh/thành", "Phường/xã",
            "Mã số thuế", "Số hợp đồng", "Tên mẫu", "Tình trạng", "Mã số mẫu", "Chỉ tiêu", "Lỗi"
        };

        // Write header row with styling
        for (int col = 0; col < columns.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Build error lookup
        var errorColumnsByRow = new Dictionary<int, HashSet<string>>();
        foreach (var error in allErrors)
        {
            if (!errorColumnsByRow.ContainsKey(error.RowNumber))
            {
                errorColumnsByRow[error.RowNumber] = new HashSet<string>();
            }
            errorColumnsByRow[error.RowNumber].Add(error.ColumnName);
        }

        // Write data rows
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRowNumber = i + 2;
            var dataRowNumber = i + 2;

            var hasError = rowErrors.ContainsKey(dataRowNumber);
            var errorColumnsForRow = errorColumnsByRow.GetValueOrDefault(dataRowNumber, new HashSet<string>());

            // Write cells
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 1, row.OrganizationName ?? "", errorColumnsForRow.Contains("Tên cơ sở"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 2, row.ContactPerson ?? "", errorColumnsForRow.Contains("Người liên hệ"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 3, row.Phone ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 4, row.Address ?? "", errorColumnsForRow.Contains("Địa chỉ"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 5, row.Province ?? "", errorColumnsForRow.Contains("Tỉnh/thành"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 6, row.Ward ?? "", errorColumnsForRow.Contains("Phường/xã"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 7, row.TaxCode ?? "", errorColumnsForRow.Contains("Mã số thuế"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 8, row.ContractNumber ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 9, row.SampleName ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 10, row.SampleStatus ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 11, row.SampleCode ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 12, row.Indicators ?? "", errorColumnsForRow.Contains("Chỉ tiêu"));

            // Error column
            var errorCell = worksheet.Cell(excelRowNumber, 13);
            if (rowErrors.TryGetValue(dataRowNumber, out var errors) && errors.Count > 0)
            {
                errorCell.Value = string.Join("; ", errors);
                errorCell.Style.Font.FontColor = XLColor.Red;
                errorCell.Style.Font.Bold = true;
            }

            // Highlight row if has errors
            if (hasError)
            {
                var rowRange = worksheet.Range(excelRowNumber, 1, excelRowNumber, 13);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Make error column wider
        if (worksheet.Column(13).Width < 50)
        {
            worksheet.Column(13).Width = 50;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void WriteCellWithErrorHighlight(IXLWorksheet worksheet, int row, int col, string value, bool hasError)
    {
        var cell = worksheet.Cell(row, col);
        cell.Value = value;
        if (hasError)
        {
            ApplyErrorStyle(cell);
        }
    }

    #endregion

    #region IndicatorGroup + Indicator Import

    public byte[] GetIndicatorGroupTemplate()
    {
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "nhóm chỉ tiêu.xlsx");
        return File.ReadAllBytes(templatePath);
    }

    public async Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewIndicatorGroupsInternalAsync(memoryStream);
    }

    private async Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadIndicatorGroupExcelRows(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation
        var indicatorGroups = await _indicatorGroupRepository.GetListAsync();
        var indicatorGroupDict = indicatorGroups
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var departments = await _departmentRepository.GetListAsync();
        var departmentDict = departments
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new IndicatorGroupImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            var item = new IndicatorGroupImportPreviewItem
            {
                RowNumber = rowNumber,
                IndicatorGroupName = row.IndicatorGroupName?.Trim() ?? "",
                DepartmentName = row.DepartmentName?.Trim(),
                IndicatorName = row.IndicatorName?.Trim() ?? "",
                FormattedIndicatorName = row.FormattedIndicatorName,
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.IndicatorGroupName))
            {
                item.IsValid = false;
                var error = "Tên nhóm chỉ tiêu không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên nhóm chỉ tiêu",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.IndicatorName))
            {
                item.IsValid = false;
                var error = "Tên chỉ tiêu không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên chỉ tiêu",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            // Check if IndicatorGroup exists
            var groupKey = StringHelper.NormalizeAndTrim(row.IndicatorGroupName).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                if (indicatorGroupDict.TryGetValue(groupKey, out var existingGroup))
                {
                    item.IsNewIndicatorGroup = false;
                    item.ExistingIndicatorGroupId = existingGroup.Id;
                    updateGroupNames.Add(groupKey);
                }
                else
                {
                    item.IsNewIndicatorGroup = true;
                    newGroupNames.Add(groupKey);
                }
            }

            // Check if Department exists (if provided)
            if (!string.IsNullOrWhiteSpace(row.DepartmentName))
            {
                var deptKey = StringHelper.NormalizeAndTrim(row.DepartmentName).ToLowerInvariant();
                if (departmentDict.TryGetValue(deptKey, out var existingDept))
                {
                    item.DepartmentId = existingDept.Id;
                    item.IsDepartmentInvalid = false;
                }
                else
                {
                    item.IsDepartmentInvalid = true;
                    item.IsValid = false;
                    var error = $"Khoa phòng '{row.DepartmentName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Khoa phòng thực hiện",
                        ErrorMessage = error,
                        Value = row.DepartmentName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
            }

            // Check if Indicator exists
            var indicatorKey = StringHelper.NormalizeAndTrim(row.IndicatorName).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(indicatorKey))
            {
                if (indicatorDict.TryGetValue(indicatorKey, out var existingIndicator))
                {
                    item.IsNewIndicator = false;
                    item.ExistingIndicatorId = existingIndicator.Id;
                    updateIndicatorNames.Add(indicatorKey);
                }
                else
                {
                    item.IsNewIndicator = true;
                    newIndicatorNames.Add(indicatorKey);
                }
            }

            // Set combined error message
            if (rowErrorMessages.Count > 0)
            {
                item.Errors = rowErrorMessages;
            }

            result.Items.Add(item);
        }

        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalRows = rows.Count;
        result.ValidRows = result.ValidationResult.ValidRows;
        result.InvalidRows = result.ValidationResult.InvalidRows;
        result.NewIndicatorGroups = newGroupNames.Count;
        result.UpdateIndicatorGroups = updateGroupNames.Count;
        result.NewIndicators = newIndicatorNames.Count;
        result.UpdateIndicators = updateIndicatorNames.Count;

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateIndicatorGroupErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_NhomChiTieu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    public async Task<ImportResultDto> ImportIndicatorGroupsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewIndicatorGroupsInternalAsync(memoryStream);

        if (!preview.ValidationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var newGroupCount = 0;
        var updatedGroupCount = 0;
        var newIndicatorCount = 0;
        var updatedIndicatorCount = 0;

        // Track processed entities to avoid duplicate operations in same batch
        var processedGroups = new Dictionary<string, IndicatorGroup>(StringComparer.OrdinalIgnoreCase);
        var processedIndicators = new Dictionary<string, Indicator>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Load existing data
            var existingGroups = await _indicatorGroupRepository.GetListAsync();
            foreach (var group in existingGroups)
            {
                var key = StringHelper.NormalizeAndTrim(group.Name).ToLowerInvariant();
                if (!processedGroups.ContainsKey(key))
                {
                    processedGroups[key] = group;
                }
            }

            var existingIndicators = await _indicatorRepository.GetListAsync();
            foreach (var indicator in existingIndicators)
            {
                var key = StringHelper.NormalizeAndTrim(indicator.Name).ToLowerInvariant();
                if (!processedIndicators.ContainsKey(key))
                {
                    processedIndicators[key] = indicator;
                }
            }

            // Calculate next indicator sequence for code generation
            var indicatorNextSequence = 1;
            var existingIndicatorCodes = existingIndicators
                .Where(x => x.Code != null && x.Code.StartsWith("CT-"))
                .Select(x => x.Code)
                .ToList();
            if (existingIndicatorCodes.Any())
            {
                indicatorNextSequence = existingIndicatorCodes
                    .Select(code =>
                    {
                        var numberPart = code.Replace("CT-", "");
                        return int.TryParse(numberPart, out var num) ? num : 0;
                    })
                    .Max() + 1;
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            // Process each row
            foreach (var item in validItems)
            {
                var groupKey = StringHelper.NormalizeAndTrim(item.IndicatorGroupName).ToLowerInvariant();
                var indicatorKey = StringHelper.NormalizeAndTrim(item.IndicatorName).ToLowerInvariant();

                IndicatorGroup indicatorGroup;

                // IndicatorGroup upsert
                if (processedGroups.TryGetValue(groupKey, out var existingGroup))
                {
                    // Update existing group
                    if (item.DepartmentId.HasValue)
                    {
                        existingGroup.LaboratoryId = item.DepartmentId;
                    }
                    await _indicatorGroupRepository.UpdateAsync(existingGroup);
                    indicatorGroup = existingGroup;

                    if (item.IsNewIndicatorGroup)
                    {
                        newGroupCount++;
                    }
                    else
                    {
                        updatedGroupCount++;
                    }
                }
                else
                {
                    // Create new group
                    indicatorGroup = new IndicatorGroup(
                        _guidGenerator.Create(),
                        item.IndicatorGroupName,
                        item.DepartmentId
                    );
                    await _indicatorGroupRepository.InsertAsync(indicatorGroup);
                    processedGroups[groupKey] = indicatorGroup;
                    newGroupCount++;
                }

                // Indicator upsert
                if (processedIndicators.TryGetValue(indicatorKey, out var existingIndicator))
                {
                    // Update existing indicator - assign to group and update FormattedName if provided
                    existingIndicator.IndicatorGroupId = indicatorGroup.Id;
                    if (!string.IsNullOrWhiteSpace(item.FormattedIndicatorName))
                    {
                        existingIndicator.FormattedName = item.FormattedIndicatorName;
                    }
                    await _indicatorRepository.UpdateAsync(existingIndicator);
                    updatedIndicatorCount++;
                }
                else
                {
                    // Create new indicator
                    var code = _codeGenerator.GenerateCode("CT", indicatorNextSequence - 1);
                    indicatorNextSequence++;
                    var indicator = new Indicator(
                        _guidGenerator.Create(),
                        code,
                        item.IndicatorName,
                        indicatorGroup.Id
                    )
                    {
                        // Set FormattedName from Excel rich text (bold, italic, underline)
                        FormattedName = !string.IsNullOrWhiteSpace(item.FormattedIndicatorName)
                            ? item.FormattedIndicatorName
                            : item.IndicatorName
                    };
                    await _indicatorRepository.InsertAsync(indicator);
                    processedIndicators[indicatorKey] = indicator;
                    newIndicatorCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = validItems.Count,
                Message = $"Import thành công: {newGroupCount} nhóm chỉ tiêu mới, {updatedGroupCount} nhóm cập nhật, {newIndicatorCount} chỉ tiêu mới, {updatedIndicatorCount} chỉ tiêu cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}. Đã rollback tất cả thay đổi."
            };
        }
    }

    private List<IndicatorGroupImportRow> ReadIndicatorGroupExcelRows(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var result = new List<IndicatorGroupImportRow>();

        // Find header row and column indices
        var headerRow = worksheet.Row(1);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var headerName = cell.GetString()?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(headerName))
            {
                columnMap[headerName] = cell.Address.ColumnNumber;
            }
        }

        // Find column indices for each field
        int groupNameCol = FindColumnIndex(columnMap, "tên nhóm chỉ tiêu", "ten nhom chi tieu", "nhóm chỉ tiêu", "nhom chi tieu", "indicator group", "group name");
        int departmentCol = FindColumnIndex(columnMap, "khoa phòng thực hiện", "khoa phong thuc hien", "khoa phòng", "khoa phong", "department", "phòng", "phong");
        int indicatorCol = FindColumnIndex(columnMap, "tên chỉ tiêu", "ten chi tieu", "chỉ tiêu", "chi tieu", "indicator", "indicator name");

        // Read data rows (starting from row 2)
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (!row.CellsUsed().Any())
                continue;

            var importRow = new IndicatorGroupImportRow
            {
                IndicatorGroupName = groupNameCol > 0 ? row.Cell(groupNameCol).GetString()?.Trim() ?? "" : "",
                DepartmentName = departmentCol > 0 ? row.Cell(departmentCol).GetString()?.Trim() : null,
                IndicatorName = indicatorCol > 0 ? row.Cell(indicatorCol).GetString()?.Trim() ?? "" : "",
                FormattedIndicatorName = indicatorCol > 0 ? GetCellRichTextAsHtml(row.Cell(indicatorCol)) : null
            };

            // Skip if all values are empty
            if (string.IsNullOrWhiteSpace(importRow.IndicatorGroupName) &&
                string.IsNullOrWhiteSpace(importRow.DepartmentName) &&
                string.IsNullOrWhiteSpace(importRow.IndicatorName))
            {
                continue;
            }

            result.Add(importRow);
        }

        return result;
    }

    private byte[] GenerateIndicatorGroupErrorFile(
        List<IndicatorGroupImportRow> rows,
        Dictionary<int, List<string>> rowErrors,
        List<ImportRowError> errors)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Lỗi Import");

        // Headers
        worksheet.Cell(1, 1).Value = "Dòng";
        worksheet.Cell(1, 2).Value = "Tên nhóm chỉ tiêu";
        worksheet.Cell(1, 3).Value = "Khoa phòng thực hiện";
        worksheet.Cell(1, 4).Value = "Tên chỉ tiêu";
        worksheet.Cell(1, 5).Value = "Lỗi";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;
            var dataRow = i + 2;

            worksheet.Cell(dataRow, 1).Value = rowNumber;
            worksheet.Cell(dataRow, 2).Value = row.IndicatorGroupName;
            worksheet.Cell(dataRow, 3).Value = row.DepartmentName ?? "";
            worksheet.Cell(dataRow, 4).Value = row.IndicatorName;

            if (rowErrors.TryGetValue(rowNumber, out var errorList))
            {
                worksheet.Cell(dataRow, 5).Value = string.Join("; ", errorList);
                // Highlight error row
                var errorRow = worksheet.Range(dataRow, 1, dataRow, 5);
                errorRow.Style.Fill.BackgroundColor = XLColor.LightPink;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #endregion

    #region Specimen for TestRequest Import

    public async Task<SpecimenForTestRequestImportPreviewDto> PreviewSpecimensForTestRequestAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var rows = ReadSpecimenForTestRequestExcelRows(memoryStream);

        // Load indicators and their test methods for validation
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        // Load test method indicators to get default test method for each indicator
        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods.ToDictionary(t => t.Id);

        // Build indicator -> default test method map (get first by SortOrder)
        var indicatorTestMethodMap = testMethodIndicators
            .GroupBy(tmi => tmi.IndicatorId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.SortOrder).FirstOrDefault()
            );

        var result = new SpecimenForTestRequestImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };
        var rowErrors = new Dictionary<int, List<string>>();

        // Group rows by SampleName
        var specimenMap = new Dictionary<string, SpecimenImportPreviewItem>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            var sampleName = row.SampleName?.Trim() ?? "";
            var indicatorName = row.IndicatorName?.Trim() ?? "";

            // Validate required fields
            if (string.IsNullOrWhiteSpace(sampleName))
            {
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên mẫu",
                    ErrorMessage = "Tên mẫu là bắt buộc"
                });
                AddRowError(rowErrors, rowNumber, "Tên mẫu là bắt buộc");
                continue;
            }

            if (string.IsNullOrWhiteSpace(indicatorName))
            {
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Chỉ tiêu kiểm nghiệm",
                    ErrorMessage = "Chỉ tiêu kiểm nghiệm là bắt buộc"
                });
                AddRowError(rowErrors, rowNumber, "Chỉ tiêu kiểm nghiệm là bắt buộc");
                continue;
            }

            // Get or create specimen preview item
            var specimenKey = StringHelper.NormalizeAndTrim(sampleName).ToLowerInvariant();
            if (!specimenMap.TryGetValue(specimenKey, out var specimen))
            {
                specimen = new SpecimenImportPreviewItem
                {
                    SampleName = sampleName,
                    SpecimenTypeName = row.SpecimenTypeName?.Trim(),
                    SamplingAddress = row.SamplingAddress?.Trim(),
                    SampleQuantity = row.SampleQuantity?.Trim(),
                    StatusText = row.StatusText?.Trim(),
                    IsValid = true
                };
                specimenMap[specimenKey] = specimen;
            }

            // Check if indicator exists
            var indicatorKey = StringHelper.NormalizeAndTrim(indicatorName).ToLowerInvariant();
            Indicator? indicator = null;
            var indicatorIsValid = indicatorDict.TryGetValue(indicatorKey, out indicator);

            if (!indicatorIsValid)
            {
                var errorMsg = $"Chỉ tiêu \"{indicatorName}\" không tồn tại trong hệ thống";
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Chỉ tiêu kiểm nghiệm",
                    ErrorMessage = errorMsg,
                    Value = indicatorName
                });
                AddRowError(rowErrors, rowNumber, errorMsg);
            }

            // Check duplicate indicator in same specimen
            var existingIndicator = specimen.Indicators.FirstOrDefault(
                ind => ind.IndicatorName.Equals(indicatorName, StringComparison.OrdinalIgnoreCase)
            );
            if (existingIndicator != null)
            {
                var errorMsg = $"Chỉ tiêu \"{indicatorName}\" bị trùng trong mẫu \"{sampleName}\"";
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Chỉ tiêu kiểm nghiệm",
                    ErrorMessage = errorMsg,
                    Value = indicatorName
                });
                AddRowError(rowErrors, rowNumber, errorMsg);
                continue;
            }

            // Get default test method for indicator
            Guid? testMethodId = null;
            string? testMethodName = null;
            if (indicator != null && indicatorTestMethodMap.TryGetValue(indicator.Id, out var defaultTmi) && defaultTmi != null)
            {
                testMethodId = defaultTmi.TestMethodId;
                if (testMethodDict.TryGetValue(defaultTmi.TestMethodId, out var testMethod))
                {
                    testMethodName = testMethod.Name;
                }
            }

            // Add indicator to specimen
            var indicatorItem = new SpecimenIndicatorImportItem
            {
                RowNumber = rowNumber,
                IndicatorName = indicatorName,
                IndicatorId = indicator?.Id,
                TestMethodId = testMethodId,
                TestMethodName = testMethodName,
                IsValid = indicatorIsValid,
                ErrorMessage = indicatorIsValid ? null : "Chỉ tiêu không tồn tại"
            };
            specimen.Indicators.Add(indicatorItem);

            // Update specimen validity
            if (!indicatorIsValid)
            {
                specimen.IsValid = false;
                specimen.Errors.Add($"Chỉ tiêu \"{indicatorName}\" không tồn tại");
            }
        }

        // Build result
        var specimens = specimenMap.Values.ToList();
        result.Specimens = specimens;
        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalRows = rows.Count;
        result.TotalSpecimens = specimens.Count;
        result.ValidSpecimens = specimens.Count(s => s.IsValid);
        result.TotalIndicators = specimens.Sum(s => s.Indicators.Count);

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateSpecimenForTestRequestErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_Mau_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    private List<SpecimenForTestRequestImportRow> ReadSpecimenForTestRequestExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<SpecimenForTestRequestImportRow>();

        memoryStream.Position = 0;
        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets.First();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2) return rows; // No data rows

        // Expected columns: Tên mẫu, Loại mẫu, Địa chỉ lấy mẫu, Lượng mẫu, Tình trạng mẫu, Chỉ tiêu kiểm nghiệm
        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var importRow = new SpecimenForTestRequestImportRow
            {
                SampleName = row.Cell(1).GetString()?.Trim() ?? "",
                SpecimenTypeName = row.Cell(2).GetString()?.Trim(),
                SamplingAddress = row.Cell(3).GetString()?.Trim(),
                SampleQuantity = row.Cell(4).GetString()?.Trim(),
                StatusText = row.Cell(5).GetString()?.Trim(),
                IndicatorName = row.Cell(6).GetString()?.Trim() ?? ""
            };

            // Skip rows with no sample name and no indicator
            if (string.IsNullOrWhiteSpace(importRow.SampleName) && string.IsNullOrWhiteSpace(importRow.IndicatorName))
                continue;

            rows.Add(importRow);
        }

        return rows;
    }

    private byte[] GenerateSpecimenForTestRequestErrorFile(
        List<SpecimenForTestRequestImportRow> rows,
        Dictionary<int, List<string>> rowErrors,
        List<ImportRowError> allErrors)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Import Errors");

        // Column mapping: Tên mẫu, Loại mẫu, Địa chỉ lấy mẫu, Lượng mẫu, Tình trạng mẫu, Chỉ tiêu kiểm nghiệm, Lỗi
        var columns = new[] { "Tên mẫu", "Loại mẫu", "Địa chỉ lấy mẫu", "Lượng mẫu", "Tình trạng mẫu", "Chỉ tiêu kiểm nghiệm", "Lỗi" };

        // Write header row with styling
        for (int col = 0; col < columns.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = columns[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        // Build error lookup: rowNumber -> list of error column names
        var errorColumnsByRow = new Dictionary<int, HashSet<string>>();
        foreach (var error in allErrors)
        {
            if (!errorColumnsByRow.ContainsKey(error.RowNumber))
            {
                errorColumnsByRow[error.RowNumber] = new HashSet<string>();
            }
            errorColumnsByRow[error.RowNumber].Add(error.ColumnName);
        }

        // Write data rows
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var excelRowNumber = i + 2; // Excel row (1-based, header = 1)
            var dataRowNumber = i + 2; // Row number used in validation

            var hasError = rowErrors.ContainsKey(dataRowNumber);
            var errorColumnsForRow = errorColumnsByRow.GetValueOrDefault(dataRowNumber, new HashSet<string>());

            // Column 1: Tên mẫu
            var cell1 = worksheet.Cell(excelRowNumber, 1);
            cell1.Value = row.SampleName ?? "";
            if (errorColumnsForRow.Contains("Tên mẫu"))
            {
                ApplyErrorStyle(cell1);
            }

            // Column 2: Loại mẫu
            var cell2 = worksheet.Cell(excelRowNumber, 2);
            cell2.Value = row.SpecimenTypeName ?? "";

            // Column 3: Địa chỉ lấy mẫu
            var cell3 = worksheet.Cell(excelRowNumber, 3);
            cell3.Value = row.SamplingAddress ?? "";

            // Column 4: Lượng mẫu
            var cell4 = worksheet.Cell(excelRowNumber, 4);
            cell4.Value = row.SampleQuantity ?? "";

            // Column 5: Tình trạng mẫu
            var cell5 = worksheet.Cell(excelRowNumber, 5);
            cell5.Value = row.StatusText ?? "";

            // Column 6: Chỉ tiêu kiểm nghiệm
            var cell6 = worksheet.Cell(excelRowNumber, 6);
            cell6.Value = row.IndicatorName ?? "";
            if (errorColumnsForRow.Contains("Chỉ tiêu kiểm nghiệm"))
            {
                ApplyErrorStyle(cell6);
            }

            // Column 7: Lỗi
            var cell7 = worksheet.Cell(excelRowNumber, 7);
            if (hasError)
            {
                cell7.Value = string.Join("; ", rowErrors[dataRowNumber]);
                cell7.Style.Font.FontColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    #endregion

    #region SampleTypeCategory Import

    public byte[] GetSampleTypeCategoryTemplate()
    {
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", "Danh muc loai mau.xlsx");
        return File.ReadAllBytes(templatePath);
    }

    public async Task<SampleTypeCategoryImportPreviewDto> PreviewSampleTypeCategoriesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewSampleTypeCategoriesInternalAsync(memoryStream);
    }

    private async Task<SampleTypeCategoryImportPreviewDto> PreviewSampleTypeCategoriesInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadSampleTypeCategoryExcelRows(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation (using normalized name for matching)
        var existingCategories = await _sampleTypeCategoryRepository.GetListAsync();
        var categoryDict = existingCategories
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new SampleTypeCategoryImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newCategoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateCategoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            var item = new SampleTypeCategoryImportPreviewItem
            {
                RowNumber = rowNumber,
                Name = row.Name?.Trim() ?? "",
                Description = row.Description?.Trim(),
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.Name))
            {
                item.IsValid = false;
                var error = "Tên danh mục loại không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên Danh mục loại",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else
            {
                // Check if category exists (for upsert logic)
                var normalizedName = StringHelper.NormalizeAndTrim(row.Name).ToLowerInvariant();
                if (categoryDict.TryGetValue(normalizedName, out var existingCategory))
                {
                    item.IsNew = false;
                    item.ExistingId = existingCategory.Id;
                    updateCategoryNames.Add(normalizedName);
                }
                else
                {
                    item.IsNew = true;
                    newCategoryNames.Add(normalizedName);
                }
            }

            item.Errors = rowErrorMessages;
            result.Items.Add(item);

            if (item.IsValid)
            {
                validationResult.ValidRows++;
            }
        }

        result.ValidationResult = validationResult;
        result.TotalRows = rows.Count;
        result.ValidRows = validationResult.ValidRows;
        result.InvalidRows = validationResult.InvalidRows;
        result.NewCategories = newCategoryNames.Count;
        result.UpdateCategories = updateCategoryNames.Count;

        // Generate error file if there are errors
        if (!validationResult.IsValid)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateSampleTypeCategoryErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Danh_muc_loai_mau_Loi.xlsx";
        }

        return result;
    }

    private List<SampleTypeCategoryImportRow> ReadSampleTypeCategoryExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<SampleTypeCategoryImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Start from row 2 (skip header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var name = row.Cell(1).GetString()?.Trim();
            var description = row.Cell(2).GetString()?.Trim();

            // Skip if all cells are empty
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
                continue;

            rows.Add(new SampleTypeCategoryImportRow
            {
                Name = name ?? "",
                Description = description
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportSampleTypeCategoriesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewSampleTypeCategoriesInternalAsync(memoryStream);

        if (!preview.ValidationResult.IsValid)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var newCount = 0;
        var updatedCount = 0;

        // Track processed entities to avoid duplicate operations in same batch
        var processedCategories = new Dictionary<string, SampleTypeCategory>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Load existing data
            var existingCategories = await _sampleTypeCategoryRepository.GetListAsync();
            foreach (var category in existingCategories)
            {
                var key = StringHelper.NormalizeAndTrim(category.Name).ToLowerInvariant();
                if (!processedCategories.ContainsKey(key))
                {
                    processedCategories[key] = category;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            // Process each row
            foreach (var item in validItems)
            {
                var categoryKey = StringHelper.NormalizeAndTrim(item.Name).ToLowerInvariant();

                SampleTypeCategory category;

                // SampleTypeCategory upsert
                if (processedCategories.TryGetValue(categoryKey, out var existingCategory))
                {
                    // Update existing category - use Normalize for both fields
                    existingCategory.Name = StringHelper.Normalize(item.Name);
                    existingCategory.Description = StringHelper.Normalize(item.Description);
                    await _sampleTypeCategoryRepository.UpdateAsync(existingCategory);
                    category = existingCategory;
                    updatedCount++;
                }
                else
                {
                    // Create new category - use Normalize for both fields
                    category = new SampleTypeCategory(
                        _guidGenerator.Create(),
                        StringHelper.Normalize(item.Name),
                        StringHelper.Normalize(item.Description)
                    );
                    await _sampleTypeCategoryRepository.InsertAsync(category);
                    processedCategories[categoryKey] = category;
                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = newCount + updatedCount,
                Message = $"Import thành công! Đã tạo mới {newCount} danh mục, cập nhật {updatedCount} danh mục."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateSampleTypeCategoryErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        originalStream.Position = 0;
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        // Add error column header
        var headerRow = worksheet.Row(1);
        var errorColumnIndex = 3; // Column C for errors
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Fill.BackgroundColor = XLColor.Yellow;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int excelRowNumber = 2; excelRowNumber <= lastRowUsed; excelRowNumber++)
        {
            var dataRowNumber = excelRowNumber;
            var hasError = rowErrors.ContainsKey(dataRowNumber);

            // Highlight error cells
            var errorColumnsForRow = new HashSet<string>();
            if (hasError)
            {
                foreach (var errorMsg in rowErrors[dataRowNumber])
                {
                    if (errorMsg.Contains("Tên danh mục loại"))
                        errorColumnsForRow.Add("Tên Danh mục loại");
                }
            }

            var row = worksheet.Row(excelRowNumber);

            // Column 1: Tên Danh mục loại
            if (errorColumnsForRow.Contains("Tên Danh mục loại"))
            {
                ApplyErrorStyle(row.Cell(1));
            }

            // Column 3: Lỗi
            if (hasError)
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", rowErrors[dataRowNumber]);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    #endregion
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using LabSpecimenManagement.Common;
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
        var prefix = _codeGenerator.GetTodayPrefix("REQ");
        var count = await _testRequestRepository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
        return _codeGenerator.GenerateCodeWithDate("REQ", count);
    }

    private async Task<string> GenerateSpecimenCodeAsync()
    {
        var prefix = _codeGenerator.GetTodayPrefix("SPE");
        var count = await _specimenRepository.CountAsync(x => x.Code.StartsWith(prefix));
        return _codeGenerator.GenerateCodeWithDate("SPE", count);
    }

    private async Task<string> GenerateCustomerCodeAsync()
    {
        var count = await _customerRepository.CountAsync();
        return _codeGenerator.GenerateCode("CUST", count);
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

        // Load existing data for validation
        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorDict = indicators.ToDictionary(
            x => x.Name.Trim().ToLowerInvariant(),
            x => x.Id);

        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods.ToDictionary(
            x => x.Name.Trim().ToLowerInvariant(),
            x => x.Id);

        var result = new TestMethodImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        // Group by TestMethodName
        var groups = rows
            .Select((row, idx) => (Row: row, RowNumber: idx + 2))
            .GroupBy(x => x.Row.TestMethodName?.Trim().ToLowerInvariant() ?? "")
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
                var indicatorKey = row.IndicatorName?.Trim().ToLowerInvariant() ?? "";
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
}

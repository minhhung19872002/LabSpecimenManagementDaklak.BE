using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.Text;
using System.Text.Json;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Helpers;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Specimens;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.WarehouseImports;
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
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<SpecimenTypeIndicator, Guid> _specimenTypeIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Note, Guid> _noteRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly IRepository<TestResultValue, Guid> _testResultValueRepository;
    private readonly IRepository<TestResultValueIndicator, Guid> _testResultValueIndicatorRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _warehouseImportReceiptItemRepository;
    private readonly IInventoryBarcodeRepository _inventoryBarcodeRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IRepository<MaterialConsumptionItem, Guid> _materialConsumptionItemRepository;
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
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<SpecimenTypeIndicator, Guid> specimenTypeIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Note, Guid> noteRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<Manufacturer, Guid> manufacturerRepository,
        IRepository<TestResultValue, Guid> testResultValueRepository,
        IRepository<TestResultValueIndicator, Guid> testResultValueIndicatorRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<WarehouseImportReceipt, Guid> warehouseImportReceiptRepository,
        IRepository<WarehouseImportReceiptItem, Guid> warehouseImportReceiptItemRepository,
        IInventoryBarcodeRepository inventoryBarcodeRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IRepository<MaterialConsumptionItem, Guid> materialConsumptionItemRepository,
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
        _specimenTypeRepository = specimenTypeRepository;
        _specimenTypeIndicatorRepository = specimenTypeIndicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _noteRepository = noteRepository;
        _warehouseRepository = warehouseRepository;
        _manufacturerRepository = manufacturerRepository;
        _testResultValueRepository = testResultValueRepository;
        _testResultValueIndicatorRepository = testResultValueIndicatorRepository;
        _productRepository = productRepository;
        _unitRepository = unitRepository;
        _warehouseImportReceiptRepository = warehouseImportReceiptRepository;
        _warehouseImportReceiptItemRepository = warehouseImportReceiptItemRepository;
        _inventoryBarcodeRepository = inventoryBarcodeRepository;
        _productSkuRepository = productSkuRepository;
        _materialConsumptionItemRepository = materialConsumptionItemRepository;
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

    public async Task<byte[]> GetTestMethodWithIndicatorsTemplateAsync()
    {
        var methodQuery = await _testMethodRepository.GetQueryableAsync();
        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();

        var query = from tmi in tmiQuery
                    join tm in methodQuery on tmi.TestMethodId equals tm.Id
                    join ind in indicatorQuery on tmi.IndicatorId equals ind.Id
                    join st in specimenTypeQuery on tmi.SpecimenTypeId equals st.Id into stj
                    from st in stj.DefaultIfEmpty()
                    where tm.IsActive
                    orderby tm.Name, ind.Name
                    select new
                    {
                        TestMethodName = tm.Name,
                        tm.FormattedName,
                        Unit = tm.Unit ?? "",
                        tm.FormattedUnit,
                        SpecimenTypeName = st != null ? st.Name : "",
                        IndicatorName = ind.Name,
                        tmi.UnitPrice
                    };

        var rows = await AsyncExecuter.ToListAsync(query);

        // Fallback to static template if no data
        if (rows.Count == 0)
            return ReadStaticTemplate("Phương pháp.xlsx");

        // Use ClosedXML to preserve rich text formatting (FormattedName, FormattedUnit)
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        // Headers
        var headers = new[] { "Tên phương pháp", "Mô tả", "Đơn vị tính", "Nền mẫu", "Chỉ tiêu", "Đơn giá" };
        for (int i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        // Data rows with rich text
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var rowNum = r + 2;

            // Tên phương pháp: rich text from FormattedName
            if (!string.IsNullOrEmpty(row.FormattedName) && row.FormattedName.Contains('<'))
                WriteHtmlToCell(worksheet.Cell(rowNum, 1), row.FormattedName);
            else
                worksheet.Cell(rowNum, 1).Value = row.TestMethodName ?? "";

            worksheet.Cell(rowNum, 2).Value = ""; // Mô tả

            // Đơn vị tính: rich text from FormattedUnit
            if (!string.IsNullOrEmpty(row.FormattedUnit) && row.FormattedUnit.Contains('<'))
                WriteHtmlToCell(worksheet.Cell(rowNum, 3), row.FormattedUnit);
            else
                worksheet.Cell(rowNum, 3).Value = row.Unit ?? "";

            worksheet.Cell(rowNum, 4).Value = row.SpecimenTypeName ?? "";
            worksheet.Cell(rowNum, 5).Value = row.IndicatorName ?? "";
            worksheet.Cell(rowNum, 6).Value = row.UnitPrice;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
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

        // Load existing TestMethods for upsert check
        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().Id);

        var result = new TestMethodImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        // Each row = one TestMethod (no grouping needed)
        foreach (var (row, rowNumber) in rows.Select((r, i) => (Row: r, RowNumber: i + 2)))
        {
            var testMethodKey = StringHelper.NormalizeAndTrim(row.TestMethodName).ToLowerInvariant();

            // Validate TestMethodName not empty
            if (string.IsNullOrWhiteSpace(testMethodKey))
            {
                var error = "Tên phương pháp không được để trống";
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên phương pháp",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
                continue;
            }

            var previewGroup = new TestMethodImportPreviewGroup
            {
                TestMethodName = row.TestMethodName?.Trim() ?? "",
                FormattedName = row.FormattedName,
                Description = row.Description?.Trim(),
                IsNew = !testMethodDict.ContainsKey(testMethodKey)
            };

            if (!previewGroup.IsNew)
            {
                previewGroup.ExistingId = testMethodDict[testMethodKey];
            }

            result.Groups.Add(previewGroup);
        }

        result.ValidationResult = validationResult;
        result.ValidationResult.ValidRows = rows.Count - validationResult.Errors.Select(e => e.RowNumber).Distinct().Count();
        result.TotalTestMethods = result.Groups.Count;
        result.NewTestMethods = result.Groups.Count(g => g.IsNew);
        result.UpdateTestMethods = result.Groups.Count(g => !g.IsNew);

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
        var codeOffset = 0;

        try
        {
            foreach (var group in preview.Groups)
            {
                if (group.IsNew)
                {
                    var code = await GenerateTestMethodCodeAsync(codeOffset);
                    codeOffset++;
                    var testMethod = new TestMethod(
                        _guidGenerator.Create(),
                        code,
                        group.TestMethodName,
                        null // Unit is now nullable
                    )
                    {
                        FormattedName = group.FormattedName,
                        ExtraProperties = { ["Description"] = group.Description ?? "" }
                    };
                    await _testMethodRepository.InsertAsync(testMethod);
                    importedCount++;
                }
                else
                {
                    var testMethod = await _testMethodRepository.GetAsync(group.ExistingId!.Value);
                    testMethod.FormattedName = group.FormattedName;
                    testMethod.ExtraProperties["Description"] = group.Description ?? "";
                    // Unit intentionally left unchanged for existing records
                    await _testMethodRepository.UpdateAsync(testMethod);
                    updatedCount++;
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
        if (!cell.HasRichText)
        {
            // Plain text cell - check cell-level formatting
            var font = cell.Style.Font;
            var text = cell.GetString()?.Trim() ?? "";

            if (string.IsNullOrEmpty(text)) return null;

            var hasFormatting = font.Bold || font.Italic || font.Underline != XLFontUnderlineValues.None ||
                               font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript ||
                               font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript;

            if (!hasFormatting) return text;

            return ApplyHtmlFormatting(text, font.Bold, font.Italic,
                font.Underline != XLFontUnderlineValues.None,
                font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript,
                font.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript);
        }

        if (cell.GetRichText().Count <= 1)
        {
            // Single rich text run — formatting is on the RUN, not cell.Style.Font
            var singleRun = cell.GetRichText().FirstOrDefault();
            if (singleRun == null)
            {
                var text = cell.GetString()?.Trim() ?? "";
                return string.IsNullOrEmpty(text) ? null : text;
            }

            var runText = singleRun.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(runText)) return null;

            var runHasFormatting = singleRun.Bold || singleRun.Italic ||
                                  singleRun.Underline != XLFontUnderlineValues.None ||
                                  singleRun.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript ||
                                  singleRun.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript;

            if (!runHasFormatting) return runText;

            return ApplyHtmlFormatting(runText, singleRun.Bold, singleRun.Italic,
                singleRun.Underline != XLFontUnderlineValues.None,
                singleRun.VerticalAlignment == XLFontVerticalTextAlignmentValues.Subscript,
                singleRun.VerticalAlignment == XLFontVerticalTextAlignmentValues.Superscript);
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
        if (italic) text = $"<em>{text}</em>";
        if (bold) text = $"<strong>{text}</strong>";

        return text;
    }

    private string NormalizeHtmlTags(string html)
    {
        return html
            .Replace("<i>", "<em>").Replace("</i>", "</em>")
            .Replace("<b>", "<strong>").Replace("</b>", "</strong>");
    }

    private string NormalizeForComparison(string html)
    {
        return System.Web.HttpUtility.HtmlDecode(NormalizeHtmlTags(html))
            .Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Write HTML-formatted text to Excel cell as rich text.
    /// Reverse of GetCellRichTextAsHtml: converts HTML tags (b, i, u, sub, sup) to Excel formatting.
    /// If html is null/empty or has no tags, writes plain text.
    /// </summary>
    private void WriteHtmlToCell(IXLCell cell, string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            cell.Value = "";
            return;
        }

        // Check if there are any HTML tags
        if (!html.Contains('<'))
        {
            cell.Value = System.Web.HttpUtility.HtmlDecode(html);
            return;
        }

        // Parse HTML into segments with formatting
        var richText = cell.CreateRichText();
        ParseHtmlToRichText(richText, html, false, false, false, false, false);
    }

    private void ParseHtmlToRichText(IXLRichText richText, string html,
        bool bold, bool italic, bool underline, bool subscript, bool superscript)
    {
        var pos = 0;
        while (pos < html.Length)
        {
            var tagStart = html.IndexOf('<', pos);
            if (tagStart < 0)
            {
                // Remaining plain text
                var remaining = System.Web.HttpUtility.HtmlDecode(html.Substring(pos));
                if (!string.IsNullOrEmpty(remaining))
                {
                    var run = richText.AddText(remaining);
                    ApplyRunFormatting(run, bold, italic, underline, subscript, superscript);
                }
                break;
            }

            // Text before tag
            if (tagStart > pos)
            {
                var text = System.Web.HttpUtility.HtmlDecode(html.Substring(pos, tagStart - pos));
                if (!string.IsNullOrEmpty(text))
                {
                    var run = richText.AddText(text);
                    ApplyRunFormatting(run, bold, italic, underline, subscript, superscript);
                }
            }

            var tagEnd = html.IndexOf('>', tagStart);
            if (tagEnd < 0) break;

            var tag = html.Substring(tagStart + 1, tagEnd - tagStart - 1).Trim().ToLower();
            pos = tagEnd + 1;

            // Find closing tag and recurse
            string? closingTag = null;
            bool newBold = bold, newItalic = italic, newUnderline = underline, newSub = subscript, newSup = superscript;

            switch (tag)
            {
                case "b": case "strong": closingTag = tag == "b" ? "</b>" : "</strong>"; newBold = true; break;
                case "i": case "em": closingTag = tag == "i" ? "</i>" : "</em>"; newItalic = true; break;
                case "u": closingTag = "</u>"; newUnderline = true; break;
                case "sub": closingTag = "</sub>"; newSub = true; break;
                case "sup": closingTag = "</sup>"; newSup = true; break;
                case "br": case "br/": case "br /":
                    richText.AddText("\n");
                    continue;
                default:
                    if (tag.StartsWith("/")) continue; // skip closing tags handled below
                    continue;
            }

            if (closingTag != null)
            {
                var closeIdx = html.IndexOf(closingTag, pos, StringComparison.OrdinalIgnoreCase);
                if (closeIdx >= 0)
                {
                    var innerHtml = html.Substring(pos, closeIdx - pos);
                    ParseHtmlToRichText(richText, innerHtml, newBold, newItalic, newUnderline, newSub, newSup);
                    pos = closeIdx + closingTag.Length;
                }
            }
        }
    }

    private void ApplyRunFormatting(IXLRichString run, bool bold, bool italic, bool underline, bool subscript, bool superscript)
    {
        if (bold) run.Bold = true;
        if (italic) run.Italic = true;
        if (underline) run.Underline = XLFontUnderlineValues.Single;
        if (subscript) run.VerticalAlignment = XLFontVerticalTextAlignmentValues.Subscript;
        if (superscript) run.VerticalAlignment = XLFontVerticalTextAlignmentValues.Superscript;
    }

    /// <summary>
    /// Read static template file from wwwroot/templates/Meta Data Template/.
    /// Used as fallback when no data exists to export.
    /// </summary>
    private byte[] ReadStaticTemplate(string filename)
    {
        var templatePath = Path.Combine(_webHostEnvironment.WebRootPath,
            "templates", "Meta Data Template", filename);

        if (File.Exists(templatePath))
            return File.ReadAllBytes(templatePath);

        // If static file also missing, return empty Excel with headers
        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, new List<Dictionary<string, object>>());
        return stream.ToArray();
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
        var columns = new[] { "Tên phương pháp", "Mô tả", "Lỗi" };
        var columnNameMapping = new Dictionary<string, int>
        {
            { "Tên phương pháp", 1 },
            { "Mô tả", 2 },
            { "Lỗi", 3 }
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

            // Column 3: Lỗi
            var cell3 = worksheet.Cell(excelRowNumber, 3);
            if (rowErrors.TryGetValue(dataRowNumber, out var errors) && errors.Count > 0)
            {
                cell3.Value = string.Join("; ", errors);
                cell3.Style.Font.FontColor = XLColor.Red;
                cell3.Style.Font.Bold = true;
            }

            // Highlight entire row background if has errors
            if (hasError)
            {
                var rowRange = worksheet.Range(excelRowNumber, 1, excelRowNumber, 3);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.OutsideBorderColor = XLColor.Red;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Make error column wider if needed
        if (worksheet.Column(3).Width < 50)
        {
            worksheet.Column(3).Width = 50;
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

    public async Task<byte[]> GetStandardWithIndicatorsTemplateAsync()
    {
        var standardQuery = await _standardRepository.GetQueryableAsync();
        var siQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var methodQuery = await _testMethodRepository.GetQueryableAsync();
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();

        var query = from si in siQuery
                    join std in standardQuery on si.StandardId equals std.Id
                    join ind in indicatorQuery on si.IndicatorId equals ind.Id
                    join tm in methodQuery on si.TestMethodId equals tm.Id into tmj
                    from tm in tmj.DefaultIfEmpty()
                    join st in specimenTypeQuery on si.SpecimenTypeId equals st.Id into stj
                    from st in stj.DefaultIfEmpty()
                    where std.IsActive
                    orderby std.Name, ind.Name
                    select new
                    {
                        StandardName = std.Name,
                        SpecimenTypeName = st != null ? st.Name : "",
                        IndicatorName = ind.Name,
                        SubIndicatorName = "",
                        TestMethodName = tm != null ? tm.Name : "",
                        si.Unit,
                        si.ComparisonOperator,
                        si.AllowedLimit,
                        si.LowerOperator,
                        si.LowerLimit,
                        si.UpperOperator,
                        si.UpperLimit
                    };

        var rows = await AsyncExecuter.ToListAsync(query);

        if (rows.Count == 0)
            return ReadStaticTemplate("Tiêu chuẩn.xlsx");

        // Use ClosedXML to preserve rich text in limit fields (AllowedLimit, LowerLimit, UpperLimit)
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        var headers = new[] { "Tên tiêu chuẩn", "Nền mẫu", "Chỉ tiêu", "Chỉ tiêu phụ", "Phương pháp", "Đơn vị",
            "So sánh", "Giới hạn", "So sánh giới hạn dưới", "Giới hạn dưới", "So sánh giới hạn trên", "Giới hạn trên" };
        for (int i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var rowNum = r + 2;

            worksheet.Cell(rowNum, 1).Value = row.StandardName ?? "";
            worksheet.Cell(rowNum, 2).Value = row.SpecimenTypeName ?? "";
            worksheet.Cell(rowNum, 3).Value = row.IndicatorName ?? "";
            worksheet.Cell(rowNum, 4).Value = row.SubIndicatorName ?? "";
            worksheet.Cell(rowNum, 5).Value = row.TestMethodName ?? "";
            worksheet.Cell(rowNum, 6).Value = row.Unit ?? "";
            worksheet.Cell(rowNum, 7).Value = row.ComparisonOperator ?? "";
            WriteHtmlToCell(worksheet.Cell(rowNum, 8), row.AllowedLimit);
            worksheet.Cell(rowNum, 9).Value = row.LowerOperator ?? "";
            WriteHtmlToCell(worksheet.Cell(rowNum, 10), row.LowerLimit);
            worksheet.Cell(rowNum, 11).Value = row.UpperOperator ?? "";
            WriteHtmlToCell(worksheet.Cell(rowNum, 12), row.UpperLimit);
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
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

        // Load SpecimenTypes for validation
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        // Load SpecimenTypeIndicators for validating Indicator belongs to SpecimenType
        var specimenTypeIndicators = await _specimenTypeIndicatorRepository.GetListAsync();

        // Load TestMethodIndicators for validating TestMethod has the Indicator
        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();

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

            // Check for duplicate indicators within the group (same SpecimenType + Indicator/SubIndicator + TestMethod)
            var indicatorMethodPairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (row, rowNumber) in group)
            {
                var specimenTypeKey = StringHelper.NormalizeAndTrim(row.SpecimenTypeName).ToLowerInvariant();
                var indicatorKey = StringHelper.NormalizeAndTrim(row.IndicatorName).ToLowerInvariant();
                var subIndicatorKey = !string.IsNullOrWhiteSpace(row.SubIndicatorName)
                    ? StringHelper.NormalizeAndTrim(row.SubIndicatorName).ToLowerInvariant()
                    : null;
                var testMethodKey = StringHelper.NormalizeAndTrim(row.TestMethodName).ToLowerInvariant();

                // Duplicate key: if SubIndicator provided, use SubIndicator; otherwise use Indicator
                var effectiveIndicatorKey = subIndicatorKey ?? indicatorKey;
                var pairKey = $"{specimenTypeKey}||{effectiveIndicatorKey}||{testMethodKey}";

                var item = new StandardIndicatorImportItem
                {
                    RowNumber = rowNumber,
                    SpecimenTypeName = row.SpecimenTypeName?.Trim() ?? "",
                    IndicatorName = row.IndicatorName?.Trim() ?? "",
                    SubIndicatorName = row.SubIndicatorName?.Trim(),
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

                // 1. Validate SpecimenType not empty and exists
                if (string.IsNullOrWhiteSpace(specimenTypeKey))
                {
                    item.IsValid = false;
                    var error = "Nền mẫu không được để trống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Nền mẫu",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!specimenTypeDict.TryGetValue(specimenTypeKey, out var specimenType))
                {
                    item.IsValid = false;
                    var error = $"Nền mẫu '{row.SpecimenTypeName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Nền mẫu",
                        ErrorMessage = error,
                        Value = row.SpecimenTypeName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else
                {
                    item.SpecimenTypeId = specimenType.Id;
                }

                // 2. Validate indicator name not empty and exists
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

                    // 3. Validate Indicator belongs to SpecimenType
                    if (item.SpecimenTypeId.HasValue)
                    {
                        var belongsToSpecimenType = specimenTypeIndicators.Any(sti =>
                            sti.SpecimenTypeId == item.SpecimenTypeId &&
                            sti.IndicatorId == item.IndicatorId &&
                            sti.IsActive);

                        if (!belongsToSpecimenType)
                        {
                            item.IsValid = false;
                            var error = $"Chỉ tiêu '{row.IndicatorName}' không thuộc nền mẫu '{row.SpecimenTypeName}'";
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

                // 4. Validate SubIndicator (if provided)
                if (!string.IsNullOrWhiteSpace(subIndicatorKey))
                {
                    if (!indicatorDict.TryGetValue(subIndicatorKey, out var subIndicator))
                    {
                        item.IsValid = false;
                        var error = $"Chỉ tiêu phụ '{row.SubIndicatorName}' không tồn tại trong hệ thống";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Chỉ tiêu phụ",
                            ErrorMessage = error,
                            Value = row.SubIndicatorName
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                    else
                    {
                        // Validate SubIndicator belongs to parent Indicator
                        if (item.IndicatorId.HasValue && subIndicator.ParentIndicatorId != item.IndicatorId)
                        {
                            item.IsValid = false;
                            var error = $"Chỉ tiêu phụ '{row.SubIndicatorName}' không thuộc chỉ tiêu '{row.IndicatorName}'";
                            rowErrorMessages.Add(error);
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = rowNumber,
                                ColumnName = "Chỉ tiêu phụ",
                                ErrorMessage = error,
                                Value = row.SubIndicatorName
                            });
                            AddRowError(rowErrors, rowNumber, error);
                        }
                        else
                        {
                            item.SubIndicatorId = subIndicator.Id;
                        }
                    }
                }

                // 5. Validate test method name not empty and exists
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

                    // 6. Validate TestMethod has the PARENT Indicator (not SubIndicator)
                    // TestMethodIndicator only stores parent indicators, sub-indicators are children
                    if (item.IndicatorId.HasValue)
                    {
                        var testMethodIndicator = testMethodIndicators.FirstOrDefault(tmi =>
                            tmi.TestMethodId == item.TestMethodId &&
                            tmi.IndicatorId == item.IndicatorId && // Always use parent indicator
                            (!item.SpecimenTypeId.HasValue || tmi.SpecimenTypeId == item.SpecimenTypeId));

                        if (testMethodIndicator == null)
                        {
                            item.IsValid = false;
                            var error = $"Phương pháp '{row.TestMethodName}' không có chỉ tiêu '{row.IndicatorName}'" +
                                (item.SpecimenTypeId.HasValue ? $" với nền mẫu '{row.SpecimenTypeName}'" : "");
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
                            // 7. Validate Unit matches TestMethod's Unit
                            var expectedUnit = testMethod.Unit ?? "";
                            var inputUnit = row.Unit?.Trim() ?? "";
                            if (!string.Equals(expectedUnit, inputUnit, StringComparison.OrdinalIgnoreCase))
                            {
                                item.IsValid = false;
                                var error = $"Đơn vị '{inputUnit}' không khớp với đơn vị của phương pháp '{row.TestMethodName}' (đơn vị đúng: '{expectedUnit}')";
                                rowErrorMessages.Add(error);
                                validationResult.Errors.Add(new ImportRowError
                                {
                                    RowNumber = rowNumber,
                                    ColumnName = "Đơn vị",
                                    ErrorMessage = error,
                                    Value = inputUnit
                                });
                                AddRowError(rowErrors, rowNumber, error);
                            }
                        }
                    }
                }

                // Validate Unit not empty (keep as fallback check)
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

                // Validate duplicate SpecimenType + Indicator/SubIndicator + TestMethod in same Standard
                if (!string.IsNullOrWhiteSpace(specimenTypeKey) && !string.IsNullOrWhiteSpace(effectiveIndicatorKey) && !string.IsNullOrWhiteSpace(testMethodKey))
                {
                    if (!indicatorMethodPairs.Add(pairKey))
                    {
                        var indicatorDisplay = subIndicatorKey != null ? row.SubIndicatorName : row.IndicatorName;
                        item.IsValid = false;
                        var error = $"Nền mẫu '{row.SpecimenTypeName}' + Chỉ tiêu '{indicatorDisplay}' + Phương pháp '{row.TestMethodName}' bị trùng trong tiêu chuẩn này";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Chỉ tiêu",
                            ErrorMessage = error,
                            Value = $"{row.SpecimenTypeName} | {indicatorDisplay} | {row.TestMethodName}"
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                }

                // Validate ComparisonOperator - optional, only validate format if provided
                var comparison = row.ComparisonOperator?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(comparison))
                {
                    if (!ValidComparisonOperators.Contains(comparison))
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

                        if (isRange)
                        {
                            // Range validation - only validate format if provided
                            ValidateRangeFields(row, rowNumber, item, rowErrorMessages, validationResult, rowErrors);
                        }
                        // Note: AllowedLimit is now optional for single comparison
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
        // LowerOperator - optional, only validate format if provided
        var lowerOp = row.LowerOperator?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(lowerOp) && !ValidLowerOperators.Contains(lowerOp))
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

        // UpperOperator - optional, only validate format if provided
        var upperOp = row.UpperOperator?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(upperOp) && !ValidUpperOperators.Contains(upperOp))
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
                var validItems = group.Indicators
                    .Where(i => i.IsValid && i.IndicatorId.HasValue && i.TestMethodId.HasValue)
                    .ToList();

                // Step 1: Create parent indicators for items that have sub-indicators
                // Collect unique parent keys (IndicatorId + TestMethodId + SpecimenTypeId)
                var parentKeys = new HashSet<string>();
                foreach (var item in validItems.Where(i => i.SubIndicatorId.HasValue))
                {
                    var parentKey = $"{item.IndicatorId}|{item.TestMethodId}|{item.SpecimenTypeId}";
                    if (parentKeys.Add(parentKey))
                    {
                        // Create parent indicator record (no limits, just placeholder for grouping)
                        var parentIndicator = new StandardIndicator(
                            _guidGenerator.Create(),
                            standard.Id,
                            item.IndicatorId!.Value,  // Parent indicator ID
                            item.TestMethodId!.Value,
                            item.Unit
                        )
                        {
                            SpecimenTypeId = item.SpecimenTypeId,
                            ComparisonOperator = null,
                            AllowedLimit = null,
                            LowerOperator = null,
                            LowerLimit = null,
                            UpperOperator = null,
                            UpperLimit = null,
                            IsActive = true,
                            SortOrder = sortOrder++
                        };
                        await _standardIndicatorRepository.InsertAsync(parentIndicator);
                    }
                }

                // Step 2: Create indicator records
                foreach (var item in validItems)
                {
                    var isRange = item.ComparisonOperator?.Equals("Khoảng", StringComparison.OrdinalIgnoreCase) == true;

                    // If SubIndicator is provided, save SubIndicatorId; otherwise save IndicatorId
                    var indicatorIdToSave = item.SubIndicatorId ?? item.IndicatorId!.Value;

                    // Skip if this is a parent indicator that was already created above
                    if (!item.SubIndicatorId.HasValue)
                    {
                        // Check if this indicator is a parent that has sub-indicators in this group
                        var hasSubInGroup = validItems.Any(i =>
                            i.SubIndicatorId.HasValue &&
                            i.IndicatorId == item.IndicatorId &&
                            i.TestMethodId == item.TestMethodId &&
                            i.SpecimenTypeId == item.SpecimenTypeId);

                        if (hasSubInGroup)
                        {
                            // Skip - parent was already created in Step 1
                            continue;
                        }
                    }

                    var standardIndicator = new StandardIndicator(
                        _guidGenerator.Create(),
                        standard.Id,
                        indicatorIdToSave,
                        item.TestMethodId!.Value,
                        item.Unit
                    )
                    {
                        SpecimenTypeId = item.SpecimenTypeId,
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
        int specimenTypeCol = FindColumnIndex(columnMap, "nền mẫu", "nen mau", "loại mẫu", "loai mau", "specimen type");
        int indicatorCol = FindColumnIndex(columnMap, "chỉ tiêu", "chi tieu", "chitieu", "indicator", "tên chỉ tiêu");
        int subIndicatorCol = FindColumnIndex(columnMap, "chỉ tiêu phụ", "chi tieu phu", "sub indicator", "subindicator");
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
                SpecimenTypeName = specimenTypeCol > 0 ? row.Cell(specimenTypeCol).GetString()?.Trim() ?? "" : "",
                IndicatorName = indicatorCol > 0 ? row.Cell(indicatorCol).GetString()?.Trim() ?? "" : "",
                SubIndicatorName = subIndicatorCol > 0 ? row.Cell(subIndicatorCol).GetString()?.Trim() : null,
                TestMethodName = testMethodCol > 0 ? row.Cell(testMethodCol).GetString()?.Trim() ?? "" : "",
                Unit = unitCol > 0 ? row.Cell(unitCol).GetString()?.Trim() ?? "" : "",
                ComparisonOperator = comparisonCol > 0 ? row.Cell(comparisonCol).GetString()?.Trim() ?? "" : "",
                AllowedLimit = limitCol > 0 ? GetCellRichTextAsHtml(row.Cell(limitCol)) : null,
                LowerOperator = lowerOpCol > 0 ? row.Cell(lowerOpCol).GetString()?.Trim() : null,
                LowerLimit = lowerLimitCol > 0 ? GetCellRichTextAsHtml(row.Cell(lowerLimitCol)) : null,
                UpperOperator = upperOpCol > 0 ? row.Cell(upperOpCol).GetString()?.Trim() : null,
                UpperLimit = upperLimitCol > 0 ? GetCellRichTextAsHtml(row.Cell(upperLimitCol)) : null
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

        // Column headers (added Nền mẫu and Chỉ tiêu phụ)
        var columns = new[] {
            "Tên tiêu chuẩn", "Nền mẫu", "Chỉ tiêu", "Chỉ tiêu phụ", "Phương pháp", "Đơn vị", "So sánh",
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
            worksheet.Cell(excelRowNumber, 2).Value = row.SpecimenTypeName ?? "";
            worksheet.Cell(excelRowNumber, 3).Value = row.IndicatorName ?? "";
            worksheet.Cell(excelRowNumber, 4).Value = row.SubIndicatorName ?? "";
            worksheet.Cell(excelRowNumber, 5).Value = row.TestMethodName ?? "";
            worksheet.Cell(excelRowNumber, 6).Value = row.Unit ?? "";
            worksheet.Cell(excelRowNumber, 7).Value = row.ComparisonOperator ?? "";
            worksheet.Cell(excelRowNumber, 8).Value = row.AllowedLimit ?? "";
            worksheet.Cell(excelRowNumber, 9).Value = row.LowerOperator ?? "";
            worksheet.Cell(excelRowNumber, 10).Value = row.LowerLimit ?? "";
            worksheet.Cell(excelRowNumber, 11).Value = row.UpperOperator ?? "";
            worksheet.Cell(excelRowNumber, 12).Value = row.UpperLimit ?? "";

            // Error column (now column 13)
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

    #endregion

    #region Organization Import (CS/DN)

    public async Task<byte[]> GetOrganizationTestRequestTemplateAsync()
    {
        var orgs = await _organizationRepository.GetListAsync();
        var activeOrgs = orgs.Where(o => o.IsActive).ToList();

        if (activeOrgs.Count == 0)
            return ReadStaticTemplate("CNDN.xlsx");

        var templateData = activeOrgs
            .OrderBy(o => o.Name)
            .Select(o => new Dictionary<string, object>
            {
                { "Tên cơ sở/doanh nghiệp", o.Name ?? "" },
                { "Địa chỉ", o.Address ?? "" },
                { "Tỉnh/thành", o.ProvinceCode ?? "" },
                { "Phường/xã", o.WardCode ?? "" },
                { "Người liên hệ", o.ContactPerson ?? "" },
                { "Số điện thoại", o.Phone ?? "" },
                { "Số điện thoại phụ", o.SecondaryPhone ?? "" },
                { "Mã số thuế", o.TaxCode ?? "" }
            }).ToList();

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
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

        // Load existing organizations for validation
        var organizations = await _organizationRepository.GetListAsync();
        var organizationDict = organizations
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new OrganizationTestRequestImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newOrgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateOrgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            // Try to find Province/Ward code from name
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
                SecondaryPhone = row.SecondaryPhone?.Trim(),
                Address = row.Address?.Trim() ?? "",
                Province = provinceName,
                ProvinceCode = provinceCode,
                Ward = wardName,
                WardCode = wardCode,
                TaxCode = row.TaxCode?.Trim() ?? "",
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

        // Generate error file if there are errors
        if (!validationResult.IsValid && rowErrors.Count > 0)
        {
            var errorFileBytes = GenerateOrganizationTestRequestErrorFile(rows, rowErrors, validationResult.Errors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = $"Loi_Import_CoSo_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        }

        return result;
    }

    public async Task<ImportResultDto> ImportOrganizationTestRequestsAsync(byte[] fileBytes, List<LocationCodeOverride>? codeOverrides = null)
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

        // Apply FE-resolved location code overrides (from new-vn-provinces library)
        if (codeOverrides != null && codeOverrides.Count > 0)
        {
            var overrideDict = codeOverrides.ToDictionary(o => o.RowNumber);
            foreach (var item in preview.Items)
            {
                if (overrideDict.TryGetValue(item.RowNumber, out var ovr))
                {
                    if (!string.IsNullOrWhiteSpace(ovr.ProvinceCode))
                        item.ProvinceCode = ovr.ProvinceCode;
                    if (!string.IsNullOrWhiteSpace(ovr.WardCode))
                        item.WardCode = ovr.WardCode;
                }
            }
        }

        var newOrgCount = 0;
        var updatedOrgCount = 0;

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

            // Process all organizations (upsert)
            var processedOrgKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in validItems)
            {
                var organizationKey = StringHelper.NormalizeAndTrim(item.OrganizationName).ToLowerInvariant();

                // Skip if already processed in this batch
                if (processedOrgKeys.Contains(organizationKey))
                    continue;

                processedOrgKeys.Add(organizationKey);

                // Use ProvinceCode/WardCode if found, otherwise fallback to Province/Ward name
                var orgProvinceCode = item.ProvinceCode ?? item.Province;
                var orgWardCode = item.WardCode ?? item.Ward;

                if (processedOrganizations.TryGetValue(organizationKey, out var existingOrg))
                {
                    // Update existing organization
                    existingOrg.ContactPerson = item.ContactPerson;
                    existingOrg.Phone = item.Phone ?? "";
                    existingOrg.SecondaryPhone = item.SecondaryPhone;
                    existingOrg.Address = item.Address;
                    existingOrg.ProvinceCode = orgProvinceCode;
                    existingOrg.WardCode = orgWardCode;
                    existingOrg.TaxCode = item.TaxCode;
                    await _organizationRepository.UpdateAsync(existingOrg);
                    updatedOrgCount++;
                }
                else
                {
                    // Create new organization
                    var organization = new Organization(_guidGenerator.Create(), item.OrganizationName)
                    {
                        ContactPerson = item.ContactPerson,
                        Phone = item.Phone ?? "",
                        SecondaryPhone = item.SecondaryPhone,
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

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newOrgCount + updatedOrgCount,
                Message = $"Import thành công: {newOrgCount} cơ sở mới, {updatedOrgCount} cơ sở cập nhật."
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

        // Find column indices for each field (Organization only)
        int orgNameCol = FindColumnIndex(columnMap, "tên cơ sở/doanh nghiệp", "tên cơ sở", "ten co so", "cơ sở", "co so", "organization", "tên khách hàng", "ten khach hang");
        int contactPersonCol = FindColumnIndex(columnMap, "người liên hệ", "nguoi lien he", "contact person", "contact", "liên hệ", "lien he");
        int phoneCol = FindColumnIndex(columnMap, "số điện thoại", "điện thoại", "dien thoai", "phone", "sđt", "sdt");
        int secondaryPhoneCol = FindColumnIndex(columnMap, "số điện thoại phụ", "sđt phụ", "sdt phu", "secondary phone");
        int addressCol = FindColumnIndex(columnMap, "địa chỉ", "dia chi", "address");
        int provinceCol = FindColumnIndex(columnMap, "tỉnh/thành", "tỉnh/ thành", "tinh/thanh", "tinh/ thanh", "tỉnh thành", "tinh thanh", "province", "tỉnh", "tinh");
        int wardCol = FindColumnIndex(columnMap, "phường/xã", "phường/ xã", "phuong/xa", "phuong/ xa", "phường xã", "phuong xa", "ward", "xã", "xa", "phường", "phuong");
        int taxCodeCol = FindColumnIndex(columnMap, "mã số thuế", "ma so thue", "mst", "tax code", "taxcode");

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
                SecondaryPhone = secondaryPhoneCol > 0 ? row.Cell(secondaryPhoneCol).GetString()?.Trim() : null,
                Address = addressCol > 0 ? row.Cell(addressCol).GetString()?.Trim() ?? "" : "",
                Province = provinceCol > 0 ? row.Cell(provinceCol).GetString()?.Trim() ?? "" : "",
                Ward = wardCol > 0 ? row.Cell(wardCol).GetString()?.Trim() ?? "" : "",
                TaxCode = taxCodeCol > 0 ? row.Cell(taxCodeCol).GetString()?.Trim() ?? "" : ""
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
            "Tên cơ sở/doanh nghiệp", "Địa chỉ", "Tỉnh thành", "Phường xã", "Người liên hệ", "Số điện thoại", "Số điện thoại phụ", "Mã số thuế", "Lỗi"
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
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 2, row.Address ?? "", errorColumnsForRow.Contains("Địa chỉ"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 3, row.Province ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 4, row.Ward ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 5, row.ContactPerson ?? "", errorColumnsForRow.Contains("Người liên hệ"));
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 6, row.Phone ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 7, row.SecondaryPhone ?? "", false);
            WriteCellWithErrorHighlight(worksheet, excelRowNumber, 8, row.TaxCode ?? "", false);

            // Error column
            var errorCell = worksheet.Cell(excelRowNumber, 9);
            if (rowErrors.TryGetValue(dataRowNumber, out var errors) && errors.Count > 0)
            {
                errorCell.Value = string.Join("; ", errors);
                errorCell.Style.Font.FontColor = XLColor.Red;
                errorCell.Style.Font.Bold = true;
            }

            // Highlight row if has errors
            if (hasError)
            {
                var rowRange = worksheet.Range(excelRowNumber, 1, excelRowNumber, 9);
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

    public async Task<byte[]> GetIndicatorGroupTemplateAsync()
    {
        var groupQuery = await _indicatorGroupRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var labQuery = await _laboratoryRepository.GetQueryableAsync();
        var deptQuery = await _departmentRepository.GetQueryableAsync();
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var testMethodIndicatorQuery = await _testMethodIndicatorRepository.GetQueryableAsync();

        var groups = await AsyncExecuter.ToListAsync(
            from g in groupQuery
            where g.IsActive
            orderby g.Name
            select new { g.Id, g.Name }
        );

        var labs = await AsyncExecuter.ToListAsync(
            from lab in labQuery
            join dept in deptQuery on lab.DepartmentId equals dept.Id into dj
            from dept in dj.DefaultIfEmpty()
            select new { lab.Id, DeptName = dept != null ? dept.Name : "" }
        );
        var labDict = labs.ToDictionary(l => l.Id, l => l.DeptName);

        var indicators = await AsyncExecuter.ToListAsync(
            from ind in indicatorQuery
            where ind.IsActive
            orderby ind.IndicatorGroupId, ind.SortOrder, ind.Name
            select new { ind.Id, ind.Name, ind.FormattedName, ind.IndicatorGroupId, ind.LaboratoryId, ind.ParentIndicatorId }
        );

        // Export should reflect current linked master data shown in FE,
        // including entries that are temporarily inactive.
        var specimenTypes = await AsyncExecuter.ToListAsync(
            from st in specimenTypeQuery
            select new { st.Id, st.Name }
        );
        var specimenTypeDict = specimenTypes.ToDictionary(x => x.Id, x => x.Name);

        var testMethods = await AsyncExecuter.ToListAsync(
            from tm in testMethodQuery
            select new { tm.Id, tm.Name }
        );
        var testMethodDict = testMethods.ToDictionary(x => x.Id, x => x.Name);

        var testMethodIndicators = await AsyncExecuter.ToListAsync(
            from tmi in testMethodIndicatorQuery
            select new
            {
                tmi.IndicatorId,
                tmi.TestMethodId,
                tmi.SpecimenTypeId,
                tmi.Unit,
                tmi.FormattedUnit,
                tmi.UnitPrice,
                tmi.IsSubcontract
            }
        );
        var tmiByIndicatorId = testMethodIndicators
            .GroupBy(x => x.IndicatorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        var headers = new[]
        {
            "Tên nhóm chỉ tiêu",
            "Khoa phòng thực hiện",
            "Nền mẫu",
            "Chỉ tiêu",
            "Chỉ tiêu phụ",
            "Tên phương pháp",
            "Đơn vị tính",
            "Đơn giá",
            "Hợp đồng phụ"
        };
        for (int i = 0; i < headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = headers[i];

        var rowNum = 2;

        void WriteTemplateRow(
            string groupName,
            string deptName,
            dynamic parentIndicator,
            dynamic? subIndicator,
            dynamic? methodLink)
        {
            worksheet.Cell(rowNum, 1).Value = groupName;
            worksheet.Cell(rowNum, 2).Value = deptName;

            if (methodLink?.SpecimenTypeId is Guid specimenTypeId && specimenTypeDict.TryGetValue(specimenTypeId, out var specimenName))
            {
                worksheet.Cell(rowNum, 3).Value = specimenName;
            }

            if (!string.IsNullOrEmpty(parentIndicator.FormattedName) && parentIndicator.FormattedName.Contains('<'))
            {
                WriteHtmlToCell(worksheet.Cell(rowNum, 4), parentIndicator.FormattedName);
            }
            else
            {
                worksheet.Cell(rowNum, 4).Value = parentIndicator.Name ?? "";
            }

            if (subIndicator != null)
            {
                if (!string.IsNullOrEmpty(subIndicator.FormattedName) && subIndicator.FormattedName.Contains('<'))
                {
                    WriteHtmlToCell(worksheet.Cell(rowNum, 5), subIndicator.FormattedName);
                }
                else
                {
                    worksheet.Cell(rowNum, 5).Value = subIndicator.Name ?? "";
                }
            }

            if (methodLink != null)
            {
                if (methodLink.TestMethodId is Guid testMethodId && testMethodDict.TryGetValue(testMethodId, out var methodName))
                {
                    worksheet.Cell(rowNum, 6).Value = methodName;
                }

                if (!string.IsNullOrWhiteSpace(methodLink.FormattedUnit) && methodLink.FormattedUnit.Contains('<'))
                {
                    WriteHtmlToCell(worksheet.Cell(rowNum, 7), methodLink.FormattedUnit);
                }
                else
                {
                    worksheet.Cell(rowNum, 7).Value = methodLink.Unit ?? "";
                }

                worksheet.Cell(rowNum, 8).Value = methodLink.UnitPrice;
                worksheet.Cell(rowNum, 9).Value = methodLink.IsSubcontract == true ? "Y" : "N";
            }

            rowNum++;
        }

        foreach (var group in groups)
        {
            var groupIndicators = indicators.Where(i => i.IndicatorGroupId == group.Id && i.ParentIndicatorId == null).ToList();

            if (groupIndicators.Count == 0)
            {
                continue;
            }

            foreach (var ind in groupIndicators)
            {
                var deptName = labDict.TryGetValue(ind.LaboratoryId, out var dn) ? dn : "";
                var subIndicators = indicators.Where(i => i.ParentIndicatorId == ind.Id).ToList();
                var methodLinks = tmiByIndicatorId.TryGetValue(ind.Id, out var links)
                    ? links.Cast<object>().ToList()
                    : new List<object>();

                if (methodLinks.Count == 0)
                {
                    methodLinks.Add(null!);
                }

                if (subIndicators.Count == 0)
                {
                    foreach (var methodLink in methodLinks)
                    {
                        WriteTemplateRow(group.Name ?? "", deptName, ind, null, methodLink);
                    }
                }
                else
                {
                    foreach (var sub in subIndicators)
                    {
                        foreach (var methodLink in methodLinks)
                        {
                            WriteTemplateRow(group.Name ?? "", deptName, ind, sub, methodLink);
                        }
                    }
                }
            }
        }

        {
            var validationSheet = workbook.AddWorksheet("_Validation");
            validationSheet.Cell(1, 1).Value = "Y";
            validationSheet.Cell(2, 1).Value = "N";
            validationSheet.Hide();

            var subcontractRange = worksheet.Range(2, 9, 1048576, 9);
            subcontractRange.CreateDataValidation().List("'_Validation'!$A$1:$A$2", true);
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewIndicatorGroupsInternalAsync(memoryStream);
    }

    private async Task<IndicatorGroupImportPreviewDto> PreviewIndicatorGroupsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadIndicatorGroupExcelRows(memoryStream);

        var rowErrors = new Dictionary<int, List<string>>();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        static string NormalizeKey(string? value)
            => StringHelper.NormalizeAndTrim(value).ToLowerInvariant();

        static string BuildParentIndicatorKey(Guid groupId, string indicatorName)
            => $"{groupId:N}|{NormalizeKey(indicatorName)}";

        static string BuildSubIndicatorKey(Guid groupId, Guid parentIndicatorId, string subIndicatorName)
            => $"{groupId:N}|{parentIndicatorId:N}|{NormalizeKey(subIndicatorName)}";

        static string BuildMethodLinkKey(Guid testMethodId, Guid indicatorId, Guid specimenTypeId)
            => $"{testMethodId:N}|{indicatorId:N}|{specimenTypeId:N}";

        static string BuildPreviewMethodLinkKey(string groupName, string indicatorName, string testMethodName, string specimenTypeName)
            => $"{NormalizeKey(groupName)}|{NormalizeKey(indicatorName)}|{NormalizeKey(testMethodName)}|{NormalizeKey(specimenTypeName)}";

        var indicatorGroups = await _indicatorGroupRepository.GetListAsync();
        var indicatorGroupDict = indicatorGroups
            .GroupBy(x => NormalizeKey(x.Name))
            .ToDictionary(g => g.Key, g => g.First());

        var indicators = await _indicatorRepository.GetListAsync();
        var parentIndicatorDict = indicators
            .Where(x => !x.ParentIndicatorId.HasValue)
            .GroupBy(x => BuildParentIndicatorKey(x.IndicatorGroupId, x.Name))
            .ToDictionary(g => g.Key, g => g.First());
        var subIndicatorDict = indicators
            .Where(x => x.ParentIndicatorId.HasValue)
            .GroupBy(x => BuildSubIndicatorKey(x.IndicatorGroupId, x.ParentIndicatorId!.Value, x.Name))
            .ToDictionary(g => g.Key, g => g.First());

        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes
            .Where(x => x.IsActive)
            .GroupBy(x => NormalizeKey(x.Name))
            .ToDictionary(g => g.Key, g => g.First());

        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods
            .GroupBy(x => NormalizeKey(x.Name))
            .ToDictionary(g => g.Key, g => g.First());

        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
        var testMethodIndicatorDict = testMethodIndicators
            .Where(x => x.SpecimenTypeId.HasValue)
            .GroupBy(x => BuildMethodLinkKey(x.TestMethodId, x.IndicatorId, x.SpecimenTypeId!.Value))
            .ToDictionary(g => g.Key, g => g.First());

        var laboratories = await _laboratoryRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var departmentNameById = departments.ToDictionary(d => d.Id, d => d.Name);

        var laboratoryByDeptName = laboratories
            .Where(lab => departmentNameById.ContainsKey(lab.DepartmentId))
            .GroupBy(lab => NormalizeKey(departmentNameById[lab.DepartmentId]))
            .ToDictionary(g => g.Key, g => g.First());

        var result = new IndicatorGroupImportPreviewDto();

        var newGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newSubIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateSubIndicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var newMethodLinkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateMethodLinkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;

            decimal parsedUnitPrice = 0;
            if (!TryParseFlexibleDecimal(row.UnitPriceText, out parsedUnitPrice))
            {
                parsedUnitPrice = 0;
            }

            var item = new IndicatorGroupImportPreviewItem
            {
                RowNumber = rowNumber,
                IndicatorGroupName = row.IndicatorGroupName?.Trim() ?? "",
                DepartmentName = row.DepartmentName?.Trim(),
                IndicatorName = row.IndicatorName?.Trim() ?? "",
                FormattedIndicatorName = row.FormattedIndicatorName,
                SubIndicatorName = row.SubIndicatorName?.Trim(),
                FormattedSubIndicatorName = row.FormattedSubIndicatorName,
                HasSubIndicator = !string.IsNullOrWhiteSpace(row.SubIndicatorName),
                SpecimenTypeName = row.SpecimenTypeName?.Trim() ?? "",
                TestMethodName = row.TestMethodName?.Trim() ?? "",
                Unit = row.Unit?.Trim(),
                FormattedUnit = row.FormattedUnit,
                UnitPrice = parsedUnitPrice,
                IsSubcontract = row.IsSubcontract,
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
                var error = "Chỉ tiêu không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Chỉ tiêu",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            var hasSpecimen = !string.IsNullOrWhiteSpace(row.SpecimenTypeName);
            var hasMethod = !string.IsNullOrWhiteSpace(row.TestMethodName);
            if (hasSpecimen != hasMethod)
            {
                item.IsValid = false;
                var error = hasSpecimen
                    ? "Có Nền mẫu nhưng thiếu Tên phương pháp"
                    : "Có Tên phương pháp nhưng thiếu Nền mẫu";
                var colName = hasSpecimen ? "Tên phương pháp" : "Nền mẫu";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = colName,
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (!string.IsNullOrWhiteSpace(row.UnitPriceText) && !TryParseFlexibleDecimal(row.UnitPriceText, out _))
            {
                item.IsValid = false;
                var error = "Đơn giá không đúng định dạng số";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Đơn giá",
                    ErrorMessage = error,
                    Value = row.UnitPriceText
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            var groupKey = NormalizeKey(row.IndicatorGroupName);
            IndicatorGroup? existingGroup = null;
            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                if (indicatorGroupDict.TryGetValue(groupKey, out existingGroup))
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

            if (!string.IsNullOrWhiteSpace(row.DepartmentName))
            {
                var deptKey = NormalizeKey(row.DepartmentName);
                if (laboratoryByDeptName.TryGetValue(deptKey, out var existingLab))
                {
                    item.DepartmentId = existingLab.Id;
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

            var specimenTypeKey = NormalizeKey(row.SpecimenTypeName);
            if (!string.IsNullOrWhiteSpace(specimenTypeKey))
            {
                if (specimenTypeDict.TryGetValue(specimenTypeKey, out var specimenType))
                {
                    item.SpecimenTypeId = specimenType.Id;
                    item.IsSpecimenTypeInvalid = false;
                }
                else
                {
                    item.IsSpecimenTypeInvalid = true;
                    item.IsValid = false;
                    var error = $"Nền mẫu '{row.SpecimenTypeName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Nền mẫu",
                        ErrorMessage = error,
                        Value = row.SpecimenTypeName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
            }

            var testMethodKey = NormalizeKey(row.TestMethodName);
            if (!string.IsNullOrWhiteSpace(testMethodKey))
            {
                if (testMethodDict.TryGetValue(testMethodKey, out var testMethod))
                {
                    item.TestMethodId = testMethod.Id;
                    item.IsTestMethodInvalid = false;
                }
                else
                {
                    item.IsTestMethodInvalid = true;
                    item.IsValid = false;
                    var error = $"Phương pháp '{row.TestMethodName}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Tên phương pháp",
                        ErrorMessage = error,
                        Value = row.TestMethodName
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
            }

            var indicatorKey = NormalizeKey(row.IndicatorName);
            Indicator? existingParentIndicator = null;
            if (!string.IsNullOrWhiteSpace(indicatorKey))
            {
                if (existingGroup != null)
                {
                    var parentKey = BuildParentIndicatorKey(existingGroup.Id, row.IndicatorName ?? "");
                    if (parentIndicatorDict.TryGetValue(parentKey, out existingParentIndicator))
                    {
                        item.IsNewIndicator = false;
                        item.ExistingIndicatorId = existingParentIndicator.Id;
                        updateIndicatorNames.Add($"{groupKey}|{indicatorKey}");
                    }
                    else
                    {
                        item.IsNewIndicator = true;
                        newIndicatorNames.Add($"{groupKey}|{indicatorKey}");
                    }
                }
                else
                {
                    item.IsNewIndicator = true;
                    newIndicatorNames.Add($"{groupKey}|{indicatorKey}");
                }
            }

            if (!string.IsNullOrWhiteSpace(row.SubIndicatorName))
            {
                var subIndicatorKey = NormalizeKey(row.SubIndicatorName);
                if (existingGroup != null && existingParentIndicator != null)
                {
                    var subKey = BuildSubIndicatorKey(existingGroup.Id, existingParentIndicator.Id, row.SubIndicatorName ?? "");
                    if (subIndicatorDict.TryGetValue(subKey, out var existingSubIndicator))
                    {
                        item.IsNewSubIndicator = false;
                        item.ExistingSubIndicatorId = existingSubIndicator.Id;
                        updateSubIndicatorNames.Add($"{groupKey}|{indicatorKey}|{subIndicatorKey}");
                    }
                    else
                    {
                        item.IsNewSubIndicator = true;
                        newSubIndicatorNames.Add($"{groupKey}|{indicatorKey}|{subIndicatorKey}");
                    }
                }
                else
                {
                    item.IsNewSubIndicator = true;
                    newSubIndicatorNames.Add($"{groupKey}|{indicatorKey}|{subIndicatorKey}");
                }
            }

            if (item.TestMethodId.HasValue && item.SpecimenTypeId.HasValue)
            {
                if (existingParentIndicator != null)
                {
                    var linkKey = BuildMethodLinkKey(item.TestMethodId.Value, existingParentIndicator.Id, item.SpecimenTypeId.Value);
                    if (testMethodIndicatorDict.ContainsKey(linkKey))
                    {
                        item.IsNewMethodLink = false;
                        updateMethodLinkKeys.Add(BuildPreviewMethodLinkKey(item.IndicatorGroupName, item.IndicatorName, item.TestMethodName, item.SpecimenTypeName));
                    }
                    else
                    {
                        item.IsNewMethodLink = true;
                        newMethodLinkKeys.Add(BuildPreviewMethodLinkKey(item.IndicatorGroupName, item.IndicatorName, item.TestMethodName, item.SpecimenTypeName));
                    }
                }
                else
                {
                    item.IsNewMethodLink = true;
                    newMethodLinkKeys.Add(BuildPreviewMethodLinkKey(item.IndicatorGroupName, item.IndicatorName, item.TestMethodName, item.SpecimenTypeName));
                }
            }

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
        result.NewSubIndicators = newSubIndicatorNames.Count;
        result.UpdateSubIndicators = updateSubIndicatorNames.Count;
        result.NewMethodLinks = newMethodLinkKeys.Count;
        result.UpdateMethodLinks = updateMethodLinkKeys.Count;

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

        static string NormalizeKey(string? value)
            => StringHelper.NormalizeAndTrim(value).ToLowerInvariant();

        static string BuildParentIndicatorKey(Guid groupId, string indicatorName)
            => $"{groupId:N}|{NormalizeKey(indicatorName)}";

        static string BuildSubIndicatorKey(Guid groupId, Guid parentIndicatorId, string subIndicatorName)
            => $"{groupId:N}|{parentIndicatorId:N}|{NormalizeKey(subIndicatorName)}";

        static string BuildMethodLinkKey(Guid testMethodId, Guid indicatorId, Guid specimenTypeId)
            => $"{testMethodId:N}|{indicatorId:N}|{specimenTypeId:N}";

        static string BuildSpecimenIndicatorKey(Guid specimenTypeId, Guid indicatorId)
            => $"{specimenTypeId:N}|{indicatorId:N}";

        var createdGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var createdIndicatorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedIndicatorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var createdSubIndicatorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedSubIndicatorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var createdMethodLinkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedMethodLinkKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var existingGroups = await _indicatorGroupRepository.GetListAsync();
            var groupDict = existingGroups
                .GroupBy(x => NormalizeKey(x.Name))
                .ToDictionary(g => g.Key, g => g.First());

            var existingIndicators = await _indicatorRepository.GetListAsync();
            var parentIndicatorDict = existingIndicators
                .Where(x => !x.ParentIndicatorId.HasValue)
                .GroupBy(x => BuildParentIndicatorKey(x.IndicatorGroupId, x.Name))
                .ToDictionary(g => g.Key, g => g.First());
            var subIndicatorDict = existingIndicators
                .Where(x => x.ParentIndicatorId.HasValue)
                .GroupBy(x => BuildSubIndicatorKey(x.IndicatorGroupId, x.ParentIndicatorId!.Value, x.Name))
                .ToDictionary(g => g.Key, g => g.First());

            var existingTestMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
            var testMethodIndicatorDict = existingTestMethodIndicators
                .Where(x => x.SpecimenTypeId.HasValue)
                .GroupBy(x => BuildMethodLinkKey(x.TestMethodId, x.IndicatorId, x.SpecimenTypeId!.Value))
                .ToDictionary(g => g.Key, g => g.First());

            var existingSpecimenTypeIndicators = await _specimenTypeIndicatorRepository.GetListAsync();
            var specimenTypeIndicatorDict = existingSpecimenTypeIndicators
                .GroupBy(x => BuildSpecimenIndicatorKey(x.SpecimenTypeId, x.IndicatorId))
                .ToDictionary(g => g.Key, g => g.First());

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

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

            foreach (var item in validItems)
            {
                var groupKey = NormalizeKey(item.IndicatorGroupName);
                var parentNameKey = NormalizeKey(item.IndicatorName);

                IndicatorGroup indicatorGroup;

                if (groupDict.TryGetValue(groupKey, out var existingGroup))
                {
                    await _indicatorGroupRepository.UpdateAsync(existingGroup);
                    indicatorGroup = existingGroup;
                    updatedGroupKeys.Add(groupKey);
                }
                else
                {
                    indicatorGroup = new IndicatorGroup(
                        _guidGenerator.Create(),
                        item.IndicatorGroupName
                    );
                    await _indicatorGroupRepository.InsertAsync(indicatorGroup);
                    groupDict[groupKey] = indicatorGroup;
                    createdGroupKeys.Add(groupKey);
                }

                var parentKey = BuildParentIndicatorKey(indicatorGroup.Id, item.IndicatorName);
                Indicator parentIndicator;
                if (parentIndicatorDict.TryGetValue(parentKey, out var existingParentIndicator))
                {
                    existingParentIndicator.IndicatorGroupId = indicatorGroup.Id;
                    if (item.DepartmentId.HasValue)
                    {
                        existingParentIndicator.LaboratoryId = item.DepartmentId.Value;
                    }
                    if (!string.IsNullOrWhiteSpace(item.FormattedIndicatorName))
                    {
                        existingParentIndicator.FormattedName = item.FormattedIndicatorName;
                    }
                    await _indicatorRepository.UpdateAsync(existingParentIndicator);
                    parentIndicator = existingParentIndicator;
                    updatedIndicatorKeys.Add($"{groupKey}|{parentNameKey}");
                }
                else
                {
                    var code = _codeGenerator.GenerateCode("CT", indicatorNextSequence - 1);
                    indicatorNextSequence++;
                    parentIndicator = new Indicator(
                        _guidGenerator.Create(),
                        code,
                        item.IndicatorName,
                        indicatorGroup.Id,
                        item.DepartmentId ?? Guid.Empty
                    )
                    {
                        // Set FormattedName from Excel rich text (bold, italic, underline)
                        FormattedName = !string.IsNullOrWhiteSpace(item.FormattedIndicatorName)
                            ? item.FormattedIndicatorName
                            : item.IndicatorName
                    };
                    await _indicatorRepository.InsertAsync(parentIndicator);
                    parentIndicatorDict[parentKey] = parentIndicator;
                    createdIndicatorKeys.Add($"{groupKey}|{parentNameKey}");
                }

                if (item.HasSubIndicator && !string.IsNullOrWhiteSpace(item.SubIndicatorName))
                {
                    var subNameKey = NormalizeKey(item.SubIndicatorName);
                    var subKey = BuildSubIndicatorKey(indicatorGroup.Id, parentIndicator.Id, item.SubIndicatorName);

                    if (subIndicatorDict.TryGetValue(subKey, out var existingSubIndicator))
                    {
                        existingSubIndicator.IndicatorGroupId = indicatorGroup.Id;
                        existingSubIndicator.ParentIndicatorId = parentIndicator.Id;
                        existingSubIndicator.LaboratoryId = item.DepartmentId ?? parentIndicator.LaboratoryId;
                        if (!string.IsNullOrWhiteSpace(item.FormattedSubIndicatorName))
                        {
                            existingSubIndicator.FormattedName = item.FormattedSubIndicatorName;
                        }
                        await _indicatorRepository.UpdateAsync(existingSubIndicator);
                        updatedSubIndicatorKeys.Add($"{groupKey}|{parentNameKey}|{subNameKey}");
                    }
                    else
                    {
                        var subCode = _codeGenerator.GenerateCode("CT", indicatorNextSequence - 1);
                        indicatorNextSequence++;
                        var subIndicator = new Indicator(
                            _guidGenerator.Create(),
                            subCode,
                            item.SubIndicatorName,
                            indicatorGroup.Id,
                            item.DepartmentId ?? parentIndicator.LaboratoryId,
                            parentIndicator.Id // ParentIndicatorId
                        )
                        {
                            FormattedName = !string.IsNullOrWhiteSpace(item.FormattedSubIndicatorName)
                                ? item.FormattedSubIndicatorName
                                : item.SubIndicatorName
                        };
                        await _indicatorRepository.InsertAsync(subIndicator);
                        subIndicatorDict[subKey] = subIndicator;
                        createdSubIndicatorKeys.Add($"{groupKey}|{parentNameKey}|{subNameKey}");
                    }
                }

                if (item.SpecimenTypeId.HasValue)
                {
                    var specimenIndicatorKey = BuildSpecimenIndicatorKey(item.SpecimenTypeId.Value, parentIndicator.Id);
                    if (!specimenTypeIndicatorDict.ContainsKey(specimenIndicatorKey))
                    {
                        var specimenTypeIndicator = new SpecimenTypeIndicator(
                            _guidGenerator.Create(),
                            item.SpecimenTypeId.Value,
                            parentIndicator.Id
                        );
                        await _specimenTypeIndicatorRepository.InsertAsync(specimenTypeIndicator);
                        specimenTypeIndicatorDict[specimenIndicatorKey] = specimenTypeIndicator;
                    }
                }

                if (item.TestMethodId.HasValue && item.SpecimenTypeId.HasValue)
                {
                    var methodLinkKey = BuildMethodLinkKey(item.TestMethodId.Value, parentIndicator.Id, item.SpecimenTypeId.Value);
                    if (testMethodIndicatorDict.TryGetValue(methodLinkKey, out var existingMethodIndicator))
                    {
                        existingMethodIndicator.Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit;
                        existingMethodIndicator.FormattedUnit = !string.IsNullOrWhiteSpace(item.FormattedUnit) ? item.FormattedUnit
                            : (!string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : null);
                        existingMethodIndicator.UnitPrice = item.UnitPrice;
                        existingMethodIndicator.IsSubcontract = item.IsSubcontract;
                        existingMethodIndicator.IsActive = true;
                        await _testMethodIndicatorRepository.UpdateAsync(existingMethodIndicator);
                        updatedMethodLinkKeys.Add(methodLinkKey);
                    }
                    else
                    {
                        var resolvedFormattedUnit = !string.IsNullOrWhiteSpace(item.FormattedUnit) ? item.FormattedUnit
                            : (!string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : null);
                        var methodIndicator = new TestMethodIndicator(
                            _guidGenerator.Create(),
                            item.TestMethodId.Value,
                            parentIndicator.Id,
                            item.UnitPrice,
                            item.SpecimenTypeId,
                            string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit,
                            resolvedFormattedUnit,
                            true
                        );
                        methodIndicator.IsSubcontract = item.IsSubcontract;
                        await _testMethodIndicatorRepository.InsertAsync(methodIndicator);
                        testMethodIndicatorDict[methodLinkKey] = methodIndicator;
                        createdMethodLinkKeys.Add(methodLinkKey);
                    }
                }
            }

            var messageParts = new List<string>();
            if (createdGroupKeys.Count > 0) messageParts.Add($"{createdGroupKeys.Count} nhóm chỉ tiêu mới");
            if (updatedGroupKeys.Count > 0) messageParts.Add($"{updatedGroupKeys.Count} nhóm cập nhật");
            if (createdIndicatorKeys.Count > 0) messageParts.Add($"{createdIndicatorKeys.Count} chỉ tiêu mới");
            if (updatedIndicatorKeys.Count > 0) messageParts.Add($"{updatedIndicatorKeys.Count} chỉ tiêu cập nhật");
            if (createdSubIndicatorKeys.Count > 0) messageParts.Add($"{createdSubIndicatorKeys.Count} chỉ tiêu phụ mới");
            if (updatedSubIndicatorKeys.Count > 0) messageParts.Add($"{updatedSubIndicatorKeys.Count} chỉ tiêu phụ cập nhật");
            if (createdMethodLinkKeys.Count > 0) messageParts.Add($"{createdMethodLinkKeys.Count} liên kết phương pháp mới");
            if (updatedMethodLinkKeys.Count > 0) messageParts.Add($"{updatedMethodLinkKeys.Count} liên kết phương pháp cập nhật");

            var importedRows = preview.Items.Count(i => i.IsValid);

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = importedRows,
                Message = messageParts.Count > 0
                    ? $"Import thành công: {string.Join(", ", messageParts)}."
                    : "Import thành công."
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

        int groupNameCol = FindColumnIndex(columnMap, "tên nhóm chỉ tiêu", "ten nhom chi tieu", "nhóm chỉ tiêu", "nhom chi tieu", "indicator group", "group name");
        int departmentCol = FindColumnIndex(columnMap, "khoa phòng thực hiện", "khoa phong thuc hien", "khoa phòng", "khoa phong", "department", "phòng", "phong");
        int specimenTypeCol = FindColumnIndex(columnMap, "nền mẫu", "nen mau", "loại mẫu", "loai mau", "specimen type");
        int indicatorCol = FindColumnIndex(columnMap, "chỉ tiêu", "chi tieu", "tên chỉ tiêu", "ten chi tieu", "indicator", "indicator name");
        int subIndicatorCol = FindColumnIndex(columnMap, "chỉ tiêu phụ", "chi tieu phu", "sub indicator", "sub-indicator", "subindicator");
        int testMethodCol = FindColumnIndex(columnMap, "tên phương pháp", "ten phuong phap", "phương pháp", "phuong phap", "test method", "method");
        int unitCol = FindColumnIndex(columnMap, "đơn vị tính", "don vi tinh", "đơn vị", "don vi", "unit");
        int unitPriceCol = FindColumnIndex(columnMap, "đơn giá", "don gia", "unit price", "price");
        int subcontractCol = FindColumnIndex(columnMap, "hợp đồng phụ", "hop dong phu", "hđ phụ", "hd phu", "subcontract");

        // Admin template ID columns (optional — not present in normal templates)
        int groupIdCol = FindColumnIndex(columnMap, "id nhóm chỉ tiêu", "id nhom chi tieu", "indicator group id", "group id");
        int indicatorIdCol = FindColumnIndex(columnMap, "id chỉ tiêu", "id chi tieu", "indicator id");
        int subIndicatorIdCol = FindColumnIndex(columnMap, "id chỉ tiêu phụ", "id chi tieu phu", "sub indicator id");

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            if (!row.CellsUsed().Any())
                continue;

            var importRow = new IndicatorGroupImportRow
            {
                IndicatorGroupName = groupNameCol > 0 ? row.Cell(groupNameCol).GetString()?.Trim() ?? "" : "",
                DepartmentName = departmentCol > 0 ? row.Cell(departmentCol).GetString()?.Trim() : null,
                SpecimenTypeName = specimenTypeCol > 0 ? row.Cell(specimenTypeCol).GetString()?.Trim() ?? "" : "",
                IndicatorName = indicatorCol > 0 ? row.Cell(indicatorCol).GetString()?.Trim() ?? "" : "",
                FormattedIndicatorName = indicatorCol > 0 ? GetCellRichTextAsHtml(row.Cell(indicatorCol)) : null,
                SubIndicatorName = subIndicatorCol > 0 ? row.Cell(subIndicatorCol).GetString()?.Trim() : null,
                FormattedSubIndicatorName = subIndicatorCol > 0 ? GetCellRichTextAsHtml(row.Cell(subIndicatorCol)) : null,
                TestMethodName = testMethodCol > 0 ? row.Cell(testMethodCol).GetString()?.Trim() ?? "" : "",
                Unit = unitCol > 0 ? row.Cell(unitCol).GetString()?.Trim() : null,
                FormattedUnit = unitCol > 0 ? GetCellRichTextAsHtml(row.Cell(unitCol)) : null,
                UnitPriceText = unitPriceCol > 0 ? row.Cell(unitPriceCol).GetFormattedString()?.Trim() : null,
                IsSubcontract = subcontractCol > 0 && string.Equals(row.Cell(subcontractCol).GetString()?.Trim(), "Y", StringComparison.OrdinalIgnoreCase),
                IndicatorGroupId = groupIdCol > 0 && Guid.TryParse(row.Cell(groupIdCol).GetString()?.Trim(), out var gId) ? gId : null,
                IndicatorId = indicatorIdCol > 0 && Guid.TryParse(row.Cell(indicatorIdCol).GetString()?.Trim(), out var iId) ? iId : null,
                SubIndicatorId = subIndicatorIdCol > 0 && Guid.TryParse(row.Cell(subIndicatorIdCol).GetString()?.Trim(), out var sId) ? sId : null,
            };

            if (string.IsNullOrWhiteSpace(importRow.IndicatorGroupName) &&
                string.IsNullOrWhiteSpace(importRow.DepartmentName) &&
                string.IsNullOrWhiteSpace(importRow.SpecimenTypeName) &&
                string.IsNullOrWhiteSpace(importRow.IndicatorName) &&
                string.IsNullOrWhiteSpace(importRow.SubIndicatorName) &&
                string.IsNullOrWhiteSpace(importRow.TestMethodName) &&
                string.IsNullOrWhiteSpace(importRow.Unit) &&
                string.IsNullOrWhiteSpace(importRow.UnitPriceText))
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
        worksheet.Cell(1, 4).Value = "Nền mẫu";
        worksheet.Cell(1, 5).Value = "Chỉ tiêu";
        worksheet.Cell(1, 6).Value = "Chỉ tiêu phụ";
        worksheet.Cell(1, 7).Value = "Tên phương pháp";
        worksheet.Cell(1, 8).Value = "Đơn vị tính";
        worksheet.Cell(1, 9).Value = "Đơn giá";
        worksheet.Cell(1, 10).Value = "Hợp đồng phụ";
        worksheet.Cell(1, 11).Value = "Lỗi";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 11);
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
            worksheet.Cell(dataRow, 4).Value = row.SpecimenTypeName;
            worksheet.Cell(dataRow, 5).Value = row.IndicatorName;
            worksheet.Cell(dataRow, 6).Value = row.SubIndicatorName ?? "";
            worksheet.Cell(dataRow, 7).Value = row.TestMethodName;
            worksheet.Cell(dataRow, 8).Value = row.Unit ?? "";
            worksheet.Cell(dataRow, 9).Value = row.UnitPriceText ?? "";
            worksheet.Cell(dataRow, 10).Value = row.IsSubcontract ? "Y" : "N";

            if (rowErrors.TryGetValue(rowNumber, out var errorList))
            {
                worksheet.Cell(dataRow, 11).Value = string.Join("; ", errorList);
                // Highlight error row
                var errorRow = worksheet.Range(dataRow, 1, dataRow, 11);
                errorRow.Style.Fill.BackgroundColor = XLColor.LightPink;
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private bool TryParseFlexibleDecimal(string? input, out decimal value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var raw = input.Trim().Replace(" ", "");

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out value))
        {
            return true;
        }

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        var normalized = raw.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    #endregion

    #region Admin Indicator Name Import (simplified — chỉ cập nhật tên chỉ tiêu theo ID)

    public async Task<byte[]> GetAdminIndicatorGroupTemplateAsync()
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        var groups = await _indicatorGroupRepository.GetListAsync();
        var groupDict = groups.Where(g => g.IsActive).ToDictionary(g => g.Id, g => g.Name ?? "");

        var indicators = (await _indicatorRepository.GetListAsync())
            .Where(i => i.IsActive && groupDict.ContainsKey(i.IndicatorGroupId))
            .OrderBy(i => i.IndicatorGroupId).ThenBy(i => i.SortOrder).ThenBy(i => i.Name)
            .ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        worksheet.Cell(1, 1).Value = "ID chỉ tiêu";
        worksheet.Cell(1, 2).Value = "Nhóm chỉ tiêu";
        worksheet.Cell(1, 3).Value = "Tên chỉ tiêu";
        for (int c = 1; c <= 3; c++)
        {
            worksheet.Cell(1, c).Style.Font.Bold = true;
            worksheet.Cell(1, c).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var rowNum = 2;
        foreach (var ind in indicators)
        {
            worksheet.Cell(rowNum, 1).Value = ind.Id.ToString();
            worksheet.Cell(rowNum, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 240, 240);
            worksheet.Cell(rowNum, 2).Value = groupDict[ind.IndicatorGroupId];
            worksheet.Cell(rowNum, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 240, 240);

            var nameToExport = ind.FormattedName ?? ind.Name ?? "";
            if (nameToExport.Contains('<'))
                WriteHtmlToCell(worksheet.Cell(rowNum, 3), nameToExport);
            else
                worksheet.Cell(rowNum, 3).Value = nameToExport;

            rowNum++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<AdminIndicatorNamePreviewDto> PreviewAdminIndicatorGroupsAsync(byte[] fileBytes)
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewAdminIndicatorNamesInternalAsync(memoryStream);
    }

    private async Task<AdminIndicatorNamePreviewDto> PreviewAdminIndicatorNamesInternalAsync(MemoryStream memoryStream)
    {
        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorById = indicators.ToDictionary(x => x.Id);

        var groups = await _indicatorGroupRepository.GetListAsync();
        var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name ?? "");

        var result = new AdminIndicatorNamePreviewDto();

        for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)
        {
            var idCell = worksheet.Cell(rowIdx, 1).GetString()?.Trim();
            var newFormattedName = GetCellRichTextAsHtml(worksheet.Cell(rowIdx, 3));
            var newPlainName = worksheet.Cell(rowIdx, 3).GetString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(idCell) && string.IsNullOrWhiteSpace(newPlainName))
                continue;

            var item = new AdminIndicatorNamePreviewItem { RowNumber = rowIdx, IsValid = true };

            if (!Guid.TryParse(idCell, out var indicatorId))
            {
                item.IsValid = false;
                item.Errors.Add($"ID chỉ tiêu '{idCell}' không hợp lệ");
                result.Items.Add(item);
                continue;
            }

            item.IndicatorId = indicatorId;

            if (!indicatorById.TryGetValue(indicatorId, out var existingIndicator))
            {
                item.IsValid = false;
                item.Errors.Add($"ID chỉ tiêu '{indicatorId}' không tồn tại trong hệ thống");
                result.Items.Add(item);
                continue;
            }

            item.GroupName = groupNameById.TryGetValue(existingIndicator.IndicatorGroupId, out var gName) ? gName : "";
            item.CurrentFormattedName = existingIndicator.FormattedName ?? existingIndicator.Name ?? "";

            var resolvedNewName = !string.IsNullOrWhiteSpace(newFormattedName) ? newFormattedName : newPlainName;

            if (string.IsNullOrWhiteSpace(resolvedNewName) || string.IsNullOrWhiteSpace(System.Text.RegularExpressions.Regex.Replace(resolvedNewName, "<[^>]+>", "").Trim()))
            {
                item.IsValid = false;
                item.Errors.Add("Tên chỉ tiêu không được để trống");
                result.Items.Add(item);
                continue;
            }

            item.NewFormattedName = resolvedNewName;

            item.IsChanged = NormalizeForComparison(item.CurrentFormattedName ?? "")
                          != NormalizeForComparison(resolvedNewName ?? "");

            result.Items.Add(item);
        }

        result.TotalRows = result.Items.Count;
        result.ChangedRows = result.Items.Count(i => i.IsValid && i.IsChanged);
        result.UnchangedRows = result.Items.Count(i => i.IsValid && !i.IsChanged);
        result.ErrorRows = result.Items.Count(i => !i.IsValid);

        return result;
    }

    public async Task<ImportResultDto> ImportAdminIndicatorGroupsAsync(byte[] fileBytes)
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewAdminIndicatorNamesInternalAsync(memoryStream);

        if (preview.ErrorRows > 0)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.TotalRows,
                Message = $"Có {preview.ErrorRows} dòng lỗi. Vui lòng sửa và thử lại."
            };
        }

        var changedItems = preview.Items.Where(i => i.IsValid && i.IsChanged).ToList();
        if (changedItems.Count == 0)
        {
            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = 0,
                Message = "Không có chỉ tiêu nào thay đổi tên."
            };
        }

        try
        {
            var indicatorIds = changedItems.Select(i => i.IndicatorId).ToHashSet();
            var indicators = (await _indicatorRepository.GetListAsync())
                .Where(i => indicatorIds.Contains(i.Id))
                .ToDictionary(i => i.Id);

            var updatedCount = 0;
            foreach (var item in changedItems)
            {
                if (!indicators.TryGetValue(item.IndicatorId, out var indicator)) continue;

                indicator.FormattedName = item.NewFormattedName;
                var plainName = System.Text.RegularExpressions.Regex.Replace(item.NewFormattedName, "<[^>]+>", "").Trim();
                if (!string.IsNullOrWhiteSpace(plainName))
                    indicator.Name = plainName;

                await _indicatorRepository.UpdateAsync(indicator);
                updatedCount++;
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = updatedCount,
                Message = $"[Admin] Cập nhật thành công {updatedCount} tên chỉ tiêu."
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

    #endregion

    #region Admin Consumption Import (tiêu hao theo Nền mẫu + Chỉ tiêu + Phương pháp)

    // Template "Tiêu hao.xlsx" (BA cung cấp), cột:
    // A Khoa phòng thực hiện (bỏ qua) | B Nền mẫu | C Chỉ tiêu | D Tên phương pháp
    // E Loại vật tư | F Tên vật tư | G Tiêu hao / mẫu | H Số mẫu dự tính
    private const int ConsumptionColSpecimenType = 2;
    private const int ConsumptionColIndicator = 3;
    private const int ConsumptionColTestMethod = 4;
    private const int ConsumptionColProductType = 5;
    private const int ConsumptionColProductName = 6;
    private const int ConsumptionColQuantity = 7;
    private const int ConsumptionColSamples = 8;

    /// <summary>
    /// Template tiêu hao có fill sẵn data hiện có: mỗi combination (Nền mẫu + Chỉ tiêu + Phương pháp) đang hoạt động
    /// xuất ra các dòng vật tư tiêu hao đã khai báo; combination chưa có tiêu hao xuất 1 dòng với cột E–H trống.
    /// Dùng file tĩnh "Tiêu hao.xlsx" làm khung để giữ nguyên header/định dạng của BA.
    /// </summary>
    public async Task<byte[]> GetAdminConsumptionTemplateAsync()
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        var templateBytes = ReadStaticTemplate("Tiêu hao.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var ws = workbook.Worksheets.First();

        // Xoá data mẫu của BA, giữ header dòng 1
        var lastUsed = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (lastUsed >= 2)
            ws.Rows(2, lastUsed).Delete();

        // ===== Master data =====
        var specimenTypeNameById = (await _specimenTypeRepository.GetListAsync())
            .ToDictionary(x => x.Id, x => x.Name ?? "");
        var testMethodNameById = (await _testMethodRepository.GetListAsync())
            .ToDictionary(x => x.Id, x => x.Name ?? "");
        var indicatorById = (await _indicatorRepository.GetListAsync())
            .ToDictionary(x => x.Id);

        var labQuery = await _laboratoryRepository.GetQueryableAsync();
        var deptQuery = await _departmentRepository.GetQueryableAsync();
        var labs = await AsyncExecuter.ToListAsync(
            from lab in labQuery
            join dept in deptQuery on lab.DepartmentId equals dept.Id into dj
            from dept in dj.DefaultIfEmpty()
            select new { lab.Id, DeptName = dept != null ? dept.Name : "" }
        );
        var labNameById = labs.ToDictionary(l => l.Id, l => l.DeptName ?? "");

        var productById = (await _productRepository.GetListAsync()).ToDictionary(p => p.Id);

        var tmis = (await _testMethodIndicatorRepository.GetListAsync())
            .Where(t => t.IsActive && t.SpecimenTypeId.HasValue
                        && indicatorById.ContainsKey(t.IndicatorId)
                        && testMethodNameById.ContainsKey(t.TestMethodId)
                        && specimenTypeNameById.ContainsKey(t.SpecimenTypeId.Value))
            .ToList();
        var tmiIds = tmis.Select(t => t.Id).ToHashSet();

        var consumptionByTmi = (await _materialConsumptionItemRepository.GetListAsync(x => x.ProductId != null))
            .Where(c => tmiIds.Contains(c.TestMethodIndicatorId))
            .GroupBy(c => c.TestMethodIndicatorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = tmis
            .Select(t =>
            {
                var ind = indicatorById[t.IndicatorId];
                return new
                {
                    Tmi = t,
                    LabName = labNameById.TryGetValue(ind.LaboratoryId, out var ln) ? ln : "",
                    SpecimenTypeName = specimenTypeNameById[t.SpecimenTypeId!.Value],
                    IndicatorName = ind.Name ?? "",
                    TestMethodName = testMethodNameById[t.TestMethodId],
                };
            })
            .OrderBy(r => r.LabName).ThenBy(r => r.SpecimenTypeName).ThenBy(r => r.IndicatorName).ThenBy(r => r.TestMethodName)
            .ToList();

        var rowNum = 2;
        foreach (var r in rows)
        {
            var items = consumptionByTmi.TryGetValue(r.Tmi.Id, out var list) ? list : new List<MaterialConsumptionItem>();
            if (items.Count == 0)
            {
                WriteConsumptionTemplateRow(ws, rowNum++, r.LabName, r.SpecimenTypeName, r.IndicatorName, r.TestMethodName, null, null, null, null);
                continue;
            }

            foreach (var c in items.OrderBy(c => productById.TryGetValue(c.ProductId!.Value, out var p0) ? p0.Name : ""))
            {
                if (!productById.TryGetValue(c.ProductId!.Value, out var product)) continue;
                WriteConsumptionTemplateRow(ws, rowNum++, r.LabName, r.SpecimenTypeName, r.IndicatorName, r.TestMethodName,
                    GetProductTypeLabel(product.ProductType), product.Name ?? "", c.Quantity, c.EstimatedSamplesPerYear);
            }
        }

        ws.Columns(1, ConsumptionColSamples).AdjustToContents();

        using var outStream = new MemoryStream();
        workbook.SaveAs(outStream);
        return outStream.ToArray();
    }

    private static void WriteConsumptionTemplateRow(IXLWorksheet ws, int rowNum,
        string labName, string specimenTypeName, string indicatorName, string testMethodName,
        string? productTypeLabel, string? productName, decimal? quantity, int? samples)
    {
        ws.Cell(rowNum, 1).Value = labName;
        ws.Cell(rowNum, ConsumptionColSpecimenType).Value = specimenTypeName;
        ws.Cell(rowNum, ConsumptionColIndicator).Value = indicatorName;
        ws.Cell(rowNum, ConsumptionColTestMethod).Value = testMethodName;
        if (productTypeLabel != null) ws.Cell(rowNum, ConsumptionColProductType).Value = productTypeLabel;
        if (productName != null) ws.Cell(rowNum, ConsumptionColProductName).Value = productName;
        if (quantity.HasValue) ws.Cell(rowNum, ConsumptionColQuantity).Value = (double)quantity.Value;
        if (samples.HasValue) ws.Cell(rowNum, ConsumptionColSamples).Value = samples.Value;
    }

    public async Task<AdminConsumptionPreviewDto> PreviewAdminConsumptionAsync(byte[] fileBytes)
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewAdminConsumptionInternalAsync(memoryStream);
    }

    private static string NormalizeLookupName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var plain = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", "");
        plain = System.Web.HttpUtility.HtmlDecode(plain);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\s+", " ");
        return plain.Trim().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>
    /// Đọc số thập phân từ cell: nhận cả cell số lẫn cell text ("10", "10.5", "10,5").
    /// </summary>
    private static bool TryReadDecimalCell(IXLCell cell, out decimal value, out string raw)
    {
        value = 0;
        raw = cell.GetString()?.Trim() ?? "";

        if (cell.DataType == XLDataType.Number)
        {
            value = (decimal)cell.GetDouble();
            return true;
        }

        if (string.IsNullOrEmpty(raw)) return false;

        var candidate = raw.Replace(" ", "");
        if (decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        // Kiểu vi-VN: dấu phẩy thập phân
        if (candidate.Count(c => c == ',') == 1 && !candidate.Contains('.')
            && decimal.TryParse(candidate.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        return false;
    }

    private async Task<AdminConsumptionPreviewDto> PreviewAdminConsumptionInternalAsync(MemoryStream memoryStream)
    {
        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        var headerSpecimen = worksheet.Cell(1, ConsumptionColSpecimenType).GetString()?.Trim() ?? "";
        var headerProduct = worksheet.Cell(1, ConsumptionColProductName).GetString()?.Trim() ?? "";
        if (!headerSpecimen.Equals("Nền mẫu", StringComparison.OrdinalIgnoreCase)
            || !headerProduct.Equals("Tên vật tư", StringComparison.OrdinalIgnoreCase))
        {
            throw new UserFriendlyException(
                "File không đúng template tiêu hao. Vui lòng tải template \"Tải template tiêu hao\" và điền theo đúng cột.");
        }

        // ===== Master data lookups =====
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeIdsByName = specimenTypes
            .GroupBy(x => NormalizeLookupName(x.Name))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var indicators = await _indicatorRepository.GetListAsync();
        var indicatorIdsByName = indicators
            .GroupBy(x => NormalizeLookupName(x.Name))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodIdsByName = testMethods
            .GroupBy(x => NormalizeLookupName(x.Name))
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();

        var products = await _productRepository.GetListAsync();
        // Tên vật tư: match chính xác, phân biệt hoa/thường (chỉ trim đầu/cuối)
        var productsByExactName = products
            .GroupBy(p => (p.Name ?? "").Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var productsByLooseName = products
            .GroupBy(p => NormalizeLookupName(p.Name))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new AdminConsumptionPreviewDto();

        for (int rowIdx = 2; rowIdx <= lastRow; rowIdx++)
        {
            var specimenTypeName = worksheet.Cell(rowIdx, ConsumptionColSpecimenType).GetString()?.Trim() ?? "";
            var indicatorName = worksheet.Cell(rowIdx, ConsumptionColIndicator).GetString()?.Trim() ?? "";
            var testMethodName = worksheet.Cell(rowIdx, ConsumptionColTestMethod).GetString()?.Trim() ?? "";
            var productTypeLabel = worksheet.Cell(rowIdx, ConsumptionColProductType).GetString()?.Trim() ?? "";
            var productName = worksheet.Cell(rowIdx, ConsumptionColProductName).GetString()?.Trim() ?? "";
            var quantityCell = worksheet.Cell(rowIdx, ConsumptionColQuantity);
            var samplesCell = worksheet.Cell(rowIdx, ConsumptionColSamples);
            var quantityRaw = quantityCell.GetString()?.Trim() ?? "";
            var samplesRaw = samplesCell.GetString()?.Trim() ?? "";

            var consumptionPartBlank = string.IsNullOrWhiteSpace(productTypeLabel) && string.IsNullOrWhiteSpace(productName)
                && string.IsNullOrWhiteSpace(quantityRaw) && string.IsNullOrWhiteSpace(samplesRaw);

            // Bỏ qua dòng trống hoàn toàn (không tính cột A)
            if (consumptionPartBlank && string.IsNullOrWhiteSpace(specimenTypeName)
                && string.IsNullOrWhiteSpace(indicatorName) && string.IsNullOrWhiteSpace(testMethodName))
                continue;

            // Combination được xuất sẵn từ template nhưng chưa điền vật tư (E–H trống) → bỏ qua, không đụng data hiện có
            if (consumptionPartBlank)
            {
                result.SkippedRows++;
                continue;
            }

            var item = new AdminConsumptionPreviewItem
            {
                RowNumber = rowIdx,
                SpecimenTypeName = specimenTypeName,
                IndicatorName = indicatorName,
                TestMethodName = testMethodName,
                ProductTypeLabel = productTypeLabel,
                ProductName = productName,
                IsValid = true,
            };

            // ----- Bắt buộc nhập -----
            if (string.IsNullOrWhiteSpace(specimenTypeName)) item.Errors.Add("Nền mẫu không được để trống");
            if (string.IsNullOrWhiteSpace(indicatorName)) item.Errors.Add("Chỉ tiêu không được để trống");
            if (string.IsNullOrWhiteSpace(testMethodName)) item.Errors.Add("Tên phương pháp không được để trống");
            if (string.IsNullOrWhiteSpace(productTypeLabel)) item.Errors.Add("Loại vật tư không được để trống");
            if (string.IsNullOrWhiteSpace(productName)) item.Errors.Add("Tên vật tư không được để trống");
            if (string.IsNullOrWhiteSpace(quantityRaw)) item.Errors.Add("Tiêu hao / mẫu không được để trống");
            if (string.IsNullOrWhiteSpace(samplesRaw)) item.Errors.Add("Số mẫu dự tính không được để trống");

            // ----- Combination: Nền mẫu + Chỉ tiêu + Phương pháp → TestMethodIndicator -----
            HashSet<Guid>? specimenTypeIds = null, indicatorIds = null, testMethodIds = null;

            if (!string.IsNullOrWhiteSpace(specimenTypeName)
                && !specimenTypeIdsByName.TryGetValue(NormalizeLookupName(specimenTypeName), out specimenTypeIds))
                item.Errors.Add($"Nền mẫu '{specimenTypeName}' không tồn tại trong hệ thống");

            if (!string.IsNullOrWhiteSpace(indicatorName)
                && !indicatorIdsByName.TryGetValue(NormalizeLookupName(indicatorName), out indicatorIds))
                item.Errors.Add($"Chỉ tiêu '{indicatorName}' không tồn tại trong hệ thống");

            if (!string.IsNullOrWhiteSpace(testMethodName)
                && !testMethodIdsByName.TryGetValue(NormalizeLookupName(testMethodName), out testMethodIds))
                item.Errors.Add($"Phương pháp '{testMethodName}' không tồn tại trong hệ thống");

            if (specimenTypeIds != null && indicatorIds != null && testMethodIds != null)
            {
                var matchedTmis = testMethodIndicators
                    .Where(t => t.SpecimenTypeId.HasValue
                                && specimenTypeIds.Contains(t.SpecimenTypeId.Value)
                                && indicatorIds.Contains(t.IndicatorId)
                                && testMethodIds.Contains(t.TestMethodId))
                    .ToList();

                // Chỉ nhận combination đang hoạt động (template cũng chỉ xuất combination đang hoạt động)
                var activeTmis = matchedTmis.Where(t => t.IsActive).ToList();

                if (matchedTmis.Count == 0)
                    item.Errors.Add($"Chỉ tiêu '{indicatorName}' chưa được khai báo với phương pháp '{testMethodName}' trên nền mẫu '{specimenTypeName}'");
                else if (activeTmis.Count == 0)
                    item.Errors.Add($"Combination '{specimenTypeName}' + '{indicatorName}' + '{testMethodName}' đang ngừng hoạt động");
                else if (activeTmis.Count > 1)
                    item.Errors.Add($"Combination Nền mẫu + Chỉ tiêu + Phương pháp không duy nhất ({activeTmis.Count} bản ghi), vui lòng kiểm tra dữ liệu chỉ tiêu");
                else
                    item.TestMethodIndicatorId = activeTmis[0].Id;
            }

            // ----- Loại vật tư + Tên vật tư -----
            var expectedProductType = ParseProductTypeLabel(productTypeLabel);
            if (!string.IsNullOrWhiteSpace(productTypeLabel) && expectedProductType == null)
                item.Errors.Add($"Loại vật tư '{productTypeLabel}' không hợp lệ (Hóa chất / Sinh phẩm / Vật tư tiêu hao / Chất chuẩn / Dung môi / Thiết bị phòng Lab / Dung dịch pha)");

            if (!string.IsNullOrWhiteSpace(productName))
            {
                if (!productsByExactName.TryGetValue(productName, out var exactMatches))
                {
                    var hint = productsByLooseName.TryGetValue(NormalizeLookupName(productName), out var loose)
                        ? $" (gần giống: '{loose[0].Name}' — tên phải khớp chính xác, phân biệt hoa/thường)"
                        : "";
                    item.Errors.Add($"Tên vật tư '{productName}' không tồn tại trong hệ thống{hint}");
                }
                else if (expectedProductType != null)
                {
                    var sameType = exactMatches.Where(p => p.ProductType == expectedProductType.Value).ToList();
                    if (sameType.Count == 0)
                    {
                        var actualTypes = string.Join(", ", exactMatches.Select(p => GetProductTypeLabel(p.ProductType)).Distinct());
                        item.Errors.Add($"Vật tư '{productName}' thuộc loại '{actualTypes}', không phải '{productTypeLabel}'");
                    }
                    else
                    {
                        var active = sameType.Where(p => p.IsActive).ToList();
                        if (active.Count == 0)
                            item.Errors.Add($"Vật tư '{productName}' đang ngừng hoạt động");
                        else if (active.Count > 1)
                            item.Errors.Add($"Tên vật tư '{productName}' không duy nhất ({active.Count} vật tư cùng tên và loại), vui lòng kiểm tra dữ liệu vật tư");
                        else
                        {
                            var product = active[0];
                            item.ProductId = product.Id;
                            // Đơn vị không có trong template: tự gán giống modal nhập tay (volumeUnit → baseUnit)
                            item.UnitId = product.VolumeUnitId ?? product.BaseUnitId;
                        }
                    }
                }
            }

            // ----- Tiêu hao / mẫu: số thập phân >= 0 -----
            decimal quantity = 0;
            var quantityOk = false;
            if (!string.IsNullOrWhiteSpace(quantityRaw))
            {
                if (!TryReadDecimalCell(quantityCell, out quantity, out _))
                    item.Errors.Add($"Tiêu hao / mẫu '{quantityRaw}' không phải là số");
                else if (quantity < 0)
                    item.Errors.Add("Tiêu hao / mẫu không được âm");
                else
                    quantityOk = true;
            }

            // ----- Số mẫu dự tính: số nguyên > 0 -----
            var samples = 0;
            var samplesOk = false;
            if (!string.IsNullOrWhiteSpace(samplesRaw))
            {
                if (!TryReadDecimalCell(samplesCell, out var samplesDec, out _))
                    item.Errors.Add($"Số mẫu dự tính '{samplesRaw}' không phải là số");
                else if (samplesDec != decimal.Truncate(samplesDec))
                    item.Errors.Add($"Số mẫu dự tính '{samplesRaw}' phải là số nguyên");
                else if (samplesDec <= 0)
                    item.Errors.Add("Số mẫu dự tính phải lớn hơn 0");
                else if (samplesDec > int.MaxValue)
                    item.Errors.Add("Số mẫu dự tính quá lớn");
                else
                {
                    samples = (int)samplesDec;
                    samplesOk = true;
                }
            }

            // Quantity/Avg lưu numeric không giới hạn → giữ nguyên số thập phân (có thể rất nhỏ)
            if (quantityOk) item.Quantity = quantity;
            if (samplesOk) item.EstimatedSamplesPerYear = samples;
            if (quantityOk && samplesOk)
                item.AvgConsumptionPerSample = item.Quantity / samples;

            item.IsValid = item.Errors.Count == 0;
            result.Items.Add(item);
        }

        // ----- Trùng vật tư trong cùng combination -----
        var duplicateGroups = result.Items
            .Where(i => i.IsValid && i.TestMethodIndicatorId.HasValue && i.ProductId.HasValue)
            .GroupBy(i => (i.TestMethodIndicatorId!.Value, i.ProductId!.Value))
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var rows = string.Join(", ", group.Select(i => i.RowNumber));
            foreach (var dup in group)
            {
                dup.IsValid = false;
                dup.Errors.Add($"Vật tư '{dup.ProductName}' bị lặp trong cùng combination (các dòng {rows})");
            }
        }

        result.TotalRows = result.Items.Count;
        result.ValidRows = result.Items.Count(i => i.IsValid);
        result.ErrorRows = result.Items.Count(i => !i.IsValid);
        result.AffectedIndicators = result.Items
            .Where(i => i.IsValid && i.TestMethodIndicatorId.HasValue)
            .Select(i => i.TestMethodIndicatorId!.Value)
            .Distinct()
            .Count();

        return result;
    }

    public async Task<ImportResultDto> ImportAdminConsumptionAsync(byte[] fileBytes)
    {
        if (!CurrentUser.IsInRole(AppConstants.RoleAdmin))
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Chỉ admin mới được sử dụng chức năng này");

        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewAdminConsumptionInternalAsync(memoryStream);

        if (preview.ErrorRows > 0)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.TotalRows,
                FailedRows = preview.ErrorRows,
                Errors = preview.Items
                    .Where(i => !i.IsValid)
                    .Select(i => new ImportRowError
                    {
                        RowNumber = i.RowNumber,
                        ErrorMessage = string.Join("; ", i.Errors)
                    })
                    .ToList(),
                Message = $"Có {preview.ErrorRows} dòng lỗi. Vui lòng sửa và thử lại."
            };
        }

        var validItems = preview.Items.Where(i => i.IsValid && i.TestMethodIndicatorId.HasValue && i.ProductId.HasValue).ToList();
        if (validItems.Count == 0)
        {
            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = 0,
                Message = "File không có dòng tiêu hao nào để nhập."
            };
        }

        try
        {
            var tmiIds = validItems.Select(i => i.TestMethodIndicatorId!.Value).Distinct().ToList();

            // Ghi đè phần vật tư của các combination có trong file; giữ nguyên dòng thiết bị (template không có thiết bị)
            var existing = await _materialConsumptionItemRepository.GetListAsync(
                x => tmiIds.Contains(x.TestMethodIndicatorId) && x.ProductId != null);
            if (existing.Count > 0)
                await _materialConsumptionItemRepository.DeleteManyAsync(existing);

            var newItems = validItems.Select(i => new MaterialConsumptionItem(
                _guidGenerator.Create(),
                i.TestMethodIndicatorId!.Value,
                i.ProductId,
                null,
                i.UnitId,
                i.Quantity,
                i.EstimatedSamplesPerYear,
                i.AvgConsumptionPerSample
            )).ToList();

            await _materialConsumptionItemRepository.InsertManyAsync(newItems);

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = newItems.Count,
                Message = $"[Admin] Nhập thành công {newItems.Count} dòng tiêu hao cho {tmiIds.Count} combination (Nền mẫu + Chỉ tiêu + Phương pháp)."
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

    #endregion

    #region Specimen for TestRequest Import

    public async Task<SpecimenForTestRequestImportPreviewDto> PreviewSpecimensForTestRequestAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var rows = ReadSpecimenForTestRequestExcelRows(memoryStream);

        // Load indicators and their test methods for validation
        var indicators = await _indicatorRepository.GetListAsync();
        // BUG 3 FIX: Dùng ToLookup thay vì ToDictionary để xử lý indicator trùng tên
        var indicatorLookup = indicators
            .ToLookup(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant());

        // Load test method indicators to get default test method for each indicator
        var testMethodIndicators = await _testMethodIndicatorRepository.GetListAsync();
        var testMethods = await _testMethodRepository.GetListAsync();
        var testMethodDict = testMethods.ToDictionary(t => t.Id);

        // Build test method name lookup (normalized name -> TestMethod)
        var testMethodNameLookup = testMethods
            .ToLookup(t => StringHelper.NormalizeAndTrim(t.Name).ToLowerInvariant());

        // Load SpecimenType master data for "Loại mẫu" validation
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeNameLookup = specimenTypes
            .ToLookup(st => StringHelper.NormalizeAndTrim(st.Name).ToLowerInvariant());

        // Build indicator -> list of TestMethodIndicator map (for matching by method name)
        var indicatorTmiListMap = testMethodIndicators
            .GroupBy(tmi => tmi.IndicatorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new SpecimenForTestRequestImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };
        var rowErrors = new Dictionary<int, List<string>>();

        // Group rows by SampleName
        var specimenMap = new Dictionary<string, SpecimenImportPreviewItem>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            // BUG 1 FIX: Dùng row number thật từ Excel thay vì i + 2
            var rowNumber = row.ExcelRowNumber;

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

            // Validate "Loại mẫu" required + existence
            var specimenTypeName = row.SpecimenTypeName?.Trim();
            SpecimenType? resolvedSpecimenType = null;
            var rowHasError = false;
            if (string.IsNullOrWhiteSpace(specimenTypeName))
            {
                rowHasError = true;
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Loại mẫu",
                    ErrorMessage = "Loại mẫu là bắt buộc"
                });
                AddRowError(rowErrors, rowNumber, "Loại mẫu là bắt buộc");
            }
            else
            {
                var stKey = StringHelper.NormalizeAndTrim(specimenTypeName).ToLowerInvariant();
                var matchedSpecimenTypes = specimenTypeNameLookup[stKey].ToList();
                if (matchedSpecimenTypes.Count == 0)
                {
                    rowHasError = true;
                    var errorMsg = $"Loại mẫu \"{specimenTypeName}\" không tồn tại trong hệ thống";
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Loại mẫu",
                        ErrorMessage = errorMsg,
                        Value = specimenTypeName
                    });
                    AddRowError(rowErrors, rowNumber, errorMsg);
                }
                else
                {
                    resolvedSpecimenType = matchedSpecimenTypes.First();
                }
            }

            // Validate "Phương pháp" required
            var userTestMethodName = row.TestMethodName?.Trim();
            if (string.IsNullOrWhiteSpace(userTestMethodName))
            {
                rowHasError = true;
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Phương pháp",
                    ErrorMessage = "Phương pháp là bắt buộc"
                });
                AddRowError(rowErrors, rowNumber, "Phương pháp là bắt buộc");
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

            // BUG 3 FIX: Check if indicator exists - xử lý trùng tên
            var indicatorKey = StringHelper.NormalizeAndTrim(indicatorName).ToLowerInvariant();
            var matchedIndicators = indicatorLookup[indicatorKey].ToList();
            Indicator? indicator = matchedIndicators.FirstOrDefault();
            var indicatorIsValid = indicator != null && !rowHasError;

            if (indicator == null)
            {
                indicatorIsValid = false;
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
            else if (matchedIndicators.Count > 1)
            {
                // Cảnh báo trùng tên - vẫn cho import nhưng thông báo
                var errorMsg = $"Chỉ tiêu \"{indicatorName}\" có {matchedIndicators.Count} kết quả trùng tên, sử dụng kết quả đầu tiên";
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Chỉ tiêu kiểm nghiệm",
                    ErrorMessage = errorMsg,
                    Value = indicatorName
                });
                AddRowError(rowErrors, rowNumber, errorMsg);
            }

            // BUG 4 FIX: Dùng normalized name để check duplicate (nhất quán với lookup)
            // Cho phép cùng indicator ở khác loại mẫu (specimenTypeId)
            var existingIndicator = specimen.Indicators.FirstOrDefault(
                ind => StringHelper.NormalizeAndTrim(ind.IndicatorName).ToLowerInvariant() == indicatorKey
                    && ind.SpecimenTypeId == resolvedSpecimenType?.Id
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

            // Resolve test method for indicator
            Guid? testMethodId = null;
            string? testMethodName = null;
            Guid? specimenTypeId = resolvedSpecimenType?.Id;

            if (indicator != null && !string.IsNullOrWhiteSpace(userTestMethodName))
            {
                // User specified a test method name → validate it
                var methodKey = StringHelper.NormalizeAndTrim(userTestMethodName).ToLowerInvariant();
                var matchedMethods = testMethodNameLookup[methodKey].ToList();

                if (matchedMethods.Count == 0)
                {
                    // Test method not found in system
                    indicatorIsValid = false;
                    var errorMsg = $"Phương pháp \"{userTestMethodName}\" không tồn tại trong hệ thống";
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Phương pháp",
                        ErrorMessage = errorMsg,
                        Value = userTestMethodName
                    });
                    AddRowError(rowErrors, rowNumber, errorMsg);
                }
                else
                {
                    // Found method(s) → check if any is linked to this indicator + specimenType
                    var matchedMethodIds = matchedMethods.Select(m => m.Id).ToHashSet();
                    var tmiList = indicatorTmiListMap.GetValueOrDefault(indicator.Id, new List<MasterData.TestMethodIndicator>());

                    // Match chính xác: indicator + method + specimenType
                    var matchedTmi = tmiList.FirstOrDefault(tmi =>
                        matchedMethodIds.Contains(tmi.TestMethodId) &&
                        tmi.SpecimenTypeId == resolvedSpecimenType?.Id);

                    // Fallback: match với specimenType null (chỉ tiêu không gắn loại mẫu)
                    matchedTmi ??= tmiList.FirstOrDefault(tmi =>
                        matchedMethodIds.Contains(tmi.TestMethodId) &&
                        !tmi.SpecimenTypeId.HasValue);

                    if (matchedTmi == null)
                    {
                        // Method exists but not linked to this indicator at this specimenType
                        indicatorIsValid = false;
                        var stName = specimenTypeName ?? "N/A";
                        var errorMsg = $"Phương pháp \"{userTestMethodName}\" không liên kết với chỉ tiêu \"{indicatorName}\" ở loại mẫu \"{stName}\"";
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Phương pháp",
                            ErrorMessage = errorMsg,
                            Value = userTestMethodName
                        });
                        AddRowError(rowErrors, rowNumber, errorMsg);
                    }
                    else
                    {
                        // Valid: use this specific test method
                        testMethodId = matchedTmi.TestMethodId;
                        specimenTypeId = matchedTmi.SpecimenTypeId ?? resolvedSpecimenType?.Id;
                        if (testMethodDict.TryGetValue(matchedTmi.TestMethodId, out var resolvedMethod))
                        {
                            testMethodName = resolvedMethod.Name;
                        }
                    }
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
                SpecimenTypeId = specimenTypeId,
                IsValid = indicatorIsValid,
                ErrorMessage = indicatorIsValid ? null : (indicator == null ? "Chỉ tiêu không tồn tại" : "Dữ liệu không hợp lệ")
            };
            specimen.Indicators.Add(indicatorItem);

            // Update specimen validity
            if (!indicatorIsValid)
            {
                specimen.IsValid = false;
                specimen.Errors.Add($"Dòng {rowNumber}: Dữ liệu không hợp lệ");
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

        // Expected columns: Loại mẫu, Tên mẫu, Lượng mẫu, Chỉ tiêu kiểm nghiệm, Tình trạng mẫu, Phương pháp, Địa chỉ lấy mẫu
        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var importRow = new SpecimenForTestRequestImportRow
            {
                ExcelRowNumber = rowNum,
                SpecimenTypeName = row.Cell(1).GetString()?.Trim(),
                SampleName = row.Cell(2).GetString()?.Trim() ?? "",
                SampleQuantity = row.Cell(3).GetString()?.Trim(),
                IndicatorName = row.Cell(4).GetString()?.Trim() ?? "",
                StatusText = row.Cell(5).GetString()?.Trim(),
                TestMethodName = row.Cell(6).GetString()?.Trim(),
                SamplingAddress = row.Cell(7).GetString()?.Trim()
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

        // Column mapping: Loại mẫu, Tên mẫu, Lượng mẫu, Chỉ tiêu kiểm nghiệm, Tình trạng mẫu, Phương pháp, Địa chỉ lấy mẫu, Lỗi
        var columns = new[] { "Loại mẫu", "Tên mẫu", "Lượng mẫu", "Chỉ tiêu kiểm nghiệm", "Tình trạng mẫu", "Phương pháp", "Địa chỉ lấy mẫu", "Lỗi" };

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
            var excelRowNumber = i + 2; // Row position in error file (1-based, header = 1)
            var dataRowNumber = row.ExcelRowNumber; // Actual Excel row number used in validation

            var hasError = rowErrors.ContainsKey(dataRowNumber);
            var errorColumnsForRow = errorColumnsByRow.GetValueOrDefault(dataRowNumber, new HashSet<string>());

            // Column 1: Loại mẫu
            var cell1 = worksheet.Cell(excelRowNumber, 1);
            cell1.Value = row.SpecimenTypeName ?? "";
            if (errorColumnsForRow.Contains("Loại mẫu"))
            {
                ApplyErrorStyle(cell1);
            }

            // Column 2: Tên mẫu
            var cell2 = worksheet.Cell(excelRowNumber, 2);
            cell2.Value = row.SampleName ?? "";
            if (errorColumnsForRow.Contains("Tên mẫu"))
            {
                ApplyErrorStyle(cell2);
            }

            // Column 3: Lượng mẫu
            var cell3 = worksheet.Cell(excelRowNumber, 3);
            cell3.Value = row.SampleQuantity ?? "";

            // Column 4: Chỉ tiêu kiểm nghiệm
            var cell4 = worksheet.Cell(excelRowNumber, 4);
            cell4.Value = row.IndicatorName ?? "";
            if (errorColumnsForRow.Contains("Chỉ tiêu kiểm nghiệm"))
            {
                ApplyErrorStyle(cell4);
            }

            // Column 5: Tình trạng mẫu
            var cell5 = worksheet.Cell(excelRowNumber, 5);
            cell5.Value = row.StatusText ?? "";

            // Column 6: Phương pháp
            var cell6 = worksheet.Cell(excelRowNumber, 6);
            cell6.Value = row.TestMethodName ?? "";
            if (errorColumnsForRow.Contains("Phương pháp"))
            {
                ApplyErrorStyle(cell6);
            }

            // Column 7: Địa chỉ lấy mẫu
            var cell7 = worksheet.Cell(excelRowNumber, 7);
            cell7.Value = row.SamplingAddress ?? "";

            // Column 8: Lỗi
            var cell8 = worksheet.Cell(excelRowNumber, 8);
            if (hasError)
            {
                cell8.Value = string.Join("; ", rowErrors[dataRowNumber]);
                cell8.Style.Font.FontColor = XLColor.Red;
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

    public async Task<byte[]> GetSampleTypeCategoryTemplateAsync()
    {
        var categories = await _sampleTypeCategoryRepository.GetListAsync();
        var activeCategories = categories.Where(c => c.IsActive).ToList();

        if (activeCategories.Count == 0)
            return ReadStaticTemplate("Danh muc loai mau.xlsx");

        var templateData = activeCategories
            .OrderBy(c => c.Name)
            .Select(c => new Dictionary<string, object>
            {
                { "Tên Danh mục loại", c.Name ?? "" },
                { "Mô tả", c.Description ?? "" }
            }).ToList();

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
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

    #region SpecimenType Import

    public async Task<byte[]> GetSpecimenTypeTemplateAsync()
    {
        var stQuery = await _specimenTypeRepository.GetQueryableAsync();
        var catQuery = await _sampleTypeCategoryRepository.GetQueryableAsync();
        var stiQuery = await _specimenTypeIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        // Get specimen types with category names and indicators
        var specimenTypes = await AsyncExecuter.ToListAsync(
            from st in stQuery
            join cat in catQuery on st.SampleTypeCategoryId equals cat.Id into cj
            from cat in cj.DefaultIfEmpty()
            where st.IsActive
            orderby cat.Name, st.Name
            select new { st.Id, st.Name, CategoryName = cat != null ? cat.Name : "" }
        );

        if (specimenTypes.Count == 0)
            return ReadStaticTemplate("Nen Mau.xlsx");

        var stiRows = await AsyncExecuter.ToListAsync(
            from sti in stiQuery
            join ind in indicatorQuery on sti.IndicatorId equals ind.Id
            where sti.IsActive
            select new { sti.SpecimenTypeId, IndicatorName = ind.Name }
        );
        var stiByType = stiRows.GroupBy(x => x.SpecimenTypeId).ToDictionary(g => g.Key, g => g.ToList());

        var templateData = new List<Dictionary<string, object>>();

        foreach (var st in specimenTypes)
        {
            if (stiByType.TryGetValue(st.Id, out var indicators) && indicators.Count > 0)
            {
                foreach (var ind in indicators)
                {
                    templateData.Add(new Dictionary<string, object>
                    {
                        { "Tên Danh mục loại", st.CategoryName ?? "" },
                        { "Nền mẫu", st.Name ?? "" },
                        { "Tên chỉ tiêu", ind.IndicatorName ?? "" }
                    });
                }
            }
            else
            {
                templateData.Add(new Dictionary<string, object>
                {
                    { "Tên Danh mục loại", st.CategoryName ?? "" },
                    { "Nền mẫu", st.Name ?? "" },
                    { "Tên chỉ tiêu", "" }
                });
            }
        }

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    public async Task<SpecimenTypeImportPreviewDto> PreviewSpecimenTypesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewSpecimenTypesInternalAsync(memoryStream);
    }

    private async Task<SpecimenTypeImportPreviewDto> PreviewSpecimenTypesInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadSpecimenTypeExcelRows(memoryStream);

        if (rows.Count == 0)
        {
            return new SpecimenTypeImportPreviewDto
            {
                ValidationResult = new ImportValidationResult { TotalRows = 0 }
            };
        }

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var existingCategories = await _sampleTypeCategoryRepository.GetListAsync();
            var categoryDict = existingCategories
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var existingIndicators = await _indicatorRepository.GetListAsync();
            var indicatorDict = existingIndicators
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var existingSpecimenTypes = await _specimenTypeRepository.GetListAsync();
            // Lookup by Name only (not CategoryId + Name) to avoid duplicates
            // when existing specimen type has different/null category
            var specimenTypeDict = existingSpecimenTypes
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // Get max existing code number for auto-generation
            var maxCodeNumber = existingSpecimenTypes
                .Where(x => x.Code.StartsWith("NM"))
                .Select(x =>
                {
                    var numPart = x.Code.Substring(2);
                    return int.TryParse(numPart, out var num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var result = new SpecimenTypeImportPreviewDto();
            var validationResult = new ImportValidationResult { TotalRows = rows.Count };

            // Group rows by (CategoryName, SpecimenTypeName)
            var groupedRows = rows
                .Select((row, index) => new { Row = row, Index = index, RowNumber = index + 2 })
                .GroupBy(x => (
                    CategoryKey: StringHelper.NormalizeAndTrim(x.Row.CategoryName).ToLowerInvariant(),
                    SpecimenTypeKey: StringHelper.NormalizeAndTrim(x.Row.SpecimenTypeName).ToLowerInvariant()
                ))
                .ToList();

            var codeCounter = maxCodeNumber;

            foreach (var group in groupedRows)
            {
                var firstRow = group.First();
                var categoryKey = group.Key.CategoryKey;
                var specimenTypeKey = group.Key.SpecimenTypeKey;

                var previewGroup = new SpecimenTypeImportPreviewGroup
                {
                    CategoryName = firstRow.Row.CategoryName?.Trim() ?? "",
                    SpecimenTypeName = firstRow.Row.SpecimenTypeName?.Trim() ?? "",
                    IsValid = true
                };

                // Validate category
                if (string.IsNullOrWhiteSpace(firstRow.Row.CategoryName))
                {
                    previewGroup.IsValid = false;
                    var error = "Tên danh mục loại không được để trống";
                    previewGroup.Errors.Add(error);
                    foreach (var item in group)
                    {
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = item.RowNumber,
                            ColumnName = "Tên Danh mục loại",
                            ErrorMessage = error
                        });
                        AddRowError(rowErrors, item.RowNumber, error);
                    }
                }
                else if (!categoryDict.TryGetValue(categoryKey, out var category))
                {
                    previewGroup.IsValid = false;
                    var error = $"Không tìm thấy danh mục loại: {firstRow.Row.CategoryName}";
                    previewGroup.Errors.Add(error);
                    foreach (var item in group)
                    {
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = item.RowNumber,
                            ColumnName = "Tên Danh mục loại",
                            ErrorMessage = error,
                            Value = firstRow.Row.CategoryName
                        });
                        AddRowError(rowErrors, item.RowNumber, error);
                    }
                }
                else
                {
                    previewGroup.CategoryId = category.Id;
                }

                // Validate specimen type name
                if (string.IsNullOrWhiteSpace(firstRow.Row.SpecimenTypeName))
                {
                    previewGroup.IsValid = false;
                    var error = "Nền mẫu không được để trống";
                    previewGroup.Errors.Add(error);
                    foreach (var item in group)
                    {
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = item.RowNumber,
                            ColumnName = "Nền mẫu",
                            ErrorMessage = error
                        });
                        AddRowError(rowErrors, item.RowNumber, error);
                    }
                }

                // Check if specimen type exists (for upsert logic) - match by Name only
                if (!string.IsNullOrWhiteSpace(specimenTypeKey))
                {
                    if (specimenTypeDict.TryGetValue(specimenTypeKey, out var existingSpecimenType))
                    {
                        previewGroup.IsNew = false;
                        previewGroup.ExistingId = existingSpecimenType.Id;
                        previewGroup.GeneratedCode = existingSpecimenType.Code;
                    }
                    else
                    {
                        previewGroup.IsNew = true;
                        codeCounter++;
                        previewGroup.GeneratedCode = $"NM{codeCounter:D4}";
                    }
                }

                // Validate indicators (indicator name can be empty - just create specimen type without indicator)
                foreach (var item in group)
                {
                    var indicatorItem = new SpecimenTypeIndicatorImportItem
                    {
                        RowNumber = item.RowNumber,
                        IndicatorName = item.Row.IndicatorName?.Trim() ?? "",
                        IsValid = true
                    };

                    // Empty indicator name is allowed - just skip adding indicator
                    if (!string.IsNullOrWhiteSpace(item.Row.IndicatorName))
                    {
                        var indicatorKey = StringHelper.NormalizeAndTrim(item.Row.IndicatorName).ToLowerInvariant();
                        if (!indicatorDict.TryGetValue(indicatorKey, out var indicator))
                        {
                            indicatorItem.IsValid = false;
                            indicatorItem.ErrorMessage = $"Không tìm thấy chỉ tiêu: {item.Row.IndicatorName}";
                            previewGroup.IsValid = false;
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = item.RowNumber,
                                ColumnName = "Tên chỉ tiêu",
                                ErrorMessage = indicatorItem.ErrorMessage,
                                Value = item.Row.IndicatorName
                            });
                            AddRowError(rowErrors, item.RowNumber, indicatorItem.ErrorMessage);
                        }
                        else
                        {
                            indicatorItem.IndicatorId = indicator.Id;
                        }
                    }

                    previewGroup.Indicators.Add(indicatorItem);
                }

                // Count valid rows
                if (previewGroup.IsValid)
                {
                    validationResult.ValidRows += group.Count();
                }

                result.Groups.Add(previewGroup);
            }

            result.ValidationResult = validationResult;
            result.TotalSpecimenTypes = result.Groups.Count;
            result.NewSpecimenTypes = result.Groups.Count(g => g.IsNew && g.IsValid);
            result.UpdateSpecimenTypes = result.Groups.Count(g => !g.IsNew && g.IsValid);
            result.TotalIndicators = result.Groups.Sum(g => g.Indicators.Count(i => i.IndicatorId.HasValue));

            // Generate error file if there are errors
            if (!validationResult.IsValid)
            {
                memoryStream.Position = 0;
                var errorFileBytes = GenerateSpecimenTypeErrorFile(memoryStream, rowErrors);
                result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
                result.ErrorFileName = "Nen_Mau_Loi.xlsx";
            }

            return result;
        }
    }

    private List<SpecimenTypeImportRow> ReadSpecimenTypeExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<SpecimenTypeImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Start from row 2 (skip header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var categoryName = row.Cell(1).GetString()?.Trim();
            var specimenTypeName = row.Cell(2).GetString()?.Trim();
            var indicatorName = row.Cell(3).GetString()?.Trim();

            // Skip if all cells are empty
            if (string.IsNullOrWhiteSpace(categoryName) &&
                string.IsNullOrWhiteSpace(specimenTypeName) &&
                string.IsNullOrWhiteSpace(indicatorName))
                continue;

            rows.Add(new SpecimenTypeImportRow
            {
                CategoryName = categoryName ?? "",
                SpecimenTypeName = specimenTypeName ?? "",
                IndicatorName = indicatorName ?? ""
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportSpecimenTypesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewSpecimenTypesInternalAsync(memoryStream);

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

        var newSpecimenCount = 0;
        var updatedSpecimenCount = 0;
        var newIndicatorCount = 0;

        try
        {
            // Get existing specimen type indicators for duplicate check
            var existingIndicatorLinks = await _specimenTypeIndicatorRepository.GetListAsync();
            var indicatorLinkSet = existingIndicatorLinks
                .Select(x => (x.SpecimenTypeId, x.IndicatorId))
                .ToHashSet();

            var validGroups = preview.Groups.Where(g => g.IsValid).ToList();

            foreach (var group in validGroups)
            {
                SpecimenType specimenType;

                if (group.IsNew)
                {
                    // Create new specimen type
                    specimenType = new SpecimenType(
                        _guidGenerator.Create(),
                        group.GeneratedCode!,
                        StringHelper.Normalize(group.SpecimenTypeName)
                    )
                    {
                        SampleTypeCategoryId = group.CategoryId
                    };
                    await _specimenTypeRepository.InsertAsync(specimenType);
                    newSpecimenCount++;
                }
                else
                {
                    // Update existing specimen type
                    specimenType = await _specimenTypeRepository.GetAsync(group.ExistingId!.Value);
                    specimenType.Name = StringHelper.Normalize(group.SpecimenTypeName);
                    specimenType.SampleTypeCategoryId = group.CategoryId;
                    await _specimenTypeRepository.UpdateAsync(specimenType);
                    updatedSpecimenCount++;
                }

                // Add indicators
                foreach (var indicatorItem in group.Indicators.Where(i => i.IsValid && i.IndicatorId.HasValue))
                {
                    var linkKey = (specimenType.Id, indicatorItem.IndicatorId!.Value);

                    // Skip if already exists
                    if (indicatorLinkSet.Contains(linkKey))
                        continue;

                    var specimenTypeIndicator = new SpecimenTypeIndicator(
                        _guidGenerator.Create(),
                        specimenType.Id,
                        indicatorItem.IndicatorId!.Value
                    );
                    await _specimenTypeIndicatorRepository.InsertAsync(specimenTypeIndicator);
                    indicatorLinkSet.Add(linkKey);
                    newIndicatorCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newSpecimenCount + updatedSpecimenCount,
                Message = $"Import thành công! Đã tạo mới {newSpecimenCount} loại mẫu, cập nhật {updatedSpecimenCount} loại mẫu, thêm {newIndicatorCount} chỉ tiêu."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateSpecimenTypeErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        originalStream.Position = 0;
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        // Add error column header
        var headerRow = worksheet.Row(1);
        var errorColumnIndex = 4; // Column D for errors
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
                    if (errorMsg.Contains("danh mục loại"))
                        errorColumnsForRow.Add("CategoryName");
                    if (errorMsg.Contains("Nền mẫu"))
                        errorColumnsForRow.Add("SpecimenTypeName");
                    if (errorMsg.Contains("chỉ tiêu"))
                        errorColumnsForRow.Add("IndicatorName");
                }
            }

            var row = worksheet.Row(excelRowNumber);

            // Column 1: Tên Danh mục loại
            if (errorColumnsForRow.Contains("CategoryName"))
            {
                ApplyErrorStyle(row.Cell(1));
            }

            // Column 2: Nền mẫu
            if (errorColumnsForRow.Contains("SpecimenTypeName"))
            {
                ApplyErrorStyle(row.Cell(2));
            }

            // Column 3: Tên chỉ tiêu
            if (errorColumnsForRow.Contains("IndicatorName"))
            {
                ApplyErrorStyle(row.Cell(3));
            }

            // Column 4: Lỗi
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

    #region Note Import

    public async Task<byte[]> GetNoteTemplateAsync()
    {
        var notes = await _noteRepository.GetListAsync();
        var activeNotes = notes.Where(n => n.IsActive).ToList();

        if (activeNotes.Count == 0)
            return ReadStaticTemplate("Ghi chú.xlsx");

        var templateData = activeNotes
            .OrderBy(n => n.SortOrder)
            .ThenBy(n => n.Content)
            .Select(n => new Dictionary<string, object>
            {
                { "Ghi chú", n.Content ?? "" }
            }).ToList();

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    public async Task<NoteImportPreviewDto> PreviewNotesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewNotesInternalAsync(memoryStream);
    }

    private async Task<NoteImportPreviewDto> PreviewNotesInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadNoteExcelRows(memoryStream);

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation (using normalized content for matching)
        var existingNotes = await _noteRepository.GetListAsync();
        var noteDict = existingNotes
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Content).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new NoteImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newNoteContents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateNoteContents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2; // Excel row (1-based, header = 1)

            var item = new NoteImportPreviewItem
            {
                RowNumber = rowNumber,
                Content = row.Content?.Trim() ?? "",
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.Content))
            {
                item.IsValid = false;
                var error = "Nội dung ghi chú không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Ghi chú",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else
            {
                // Check if note exists (for upsert logic)
                var normalizedContent = StringHelper.NormalizeAndTrim(row.Content).ToLowerInvariant();
                if (noteDict.TryGetValue(normalizedContent, out var existingNote))
                {
                    item.IsNew = false;
                    item.ExistingId = existingNote.Id;
                    updateNoteContents.Add(normalizedContent);
                }
                else
                {
                    item.IsNew = true;
                    newNoteContents.Add(normalizedContent);
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
        result.NewNotes = newNoteContents.Count;
        result.UpdateNotes = updateNoteContents.Count;

        // Generate error file if there are errors
        if (!validationResult.IsValid)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateNoteErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Ghi_chu_Loi.xlsx";
        }

        return result;
    }

    private List<NoteImportRow> ReadNoteExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<NoteImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Start from row 2 (skip header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var content = row.Cell(1).GetString()?.Trim();

            // Skip if cell is empty
            if (string.IsNullOrWhiteSpace(content))
                continue;

            rows.Add(new NoteImportRow
            {
                Content = content
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportNotesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewNotesInternalAsync(memoryStream);

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
        var processedNotes = new Dictionary<string, Note>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Load existing data
            var existingNotes = await _noteRepository.GetListAsync();
            foreach (var note in existingNotes)
            {
                var key = StringHelper.NormalizeAndTrim(note.Content).ToLowerInvariant();
                if (!processedNotes.ContainsKey(key))
                {
                    processedNotes[key] = note;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            // Get next code number for new notes
            var nextCodeNumber = await GetNextNoteCodeNumberAsync();

            // Process each row
            foreach (var item in validItems)
            {
                var noteKey = StringHelper.NormalizeAndTrim(item.Content).ToLowerInvariant();

                // Note upsert
                if (processedNotes.TryGetValue(noteKey, out var existingNote))
                {
                    // Update existing note - use Normalize for content
                    existingNote.Content = StringHelper.Normalize(item.Content);
                    await _noteRepository.UpdateAsync(existingNote);
                    updatedCount++;
                }
                else
                {
                    // Create new note with generated code
                    var code = $"GC-{nextCodeNumber:D4}";
                    nextCodeNumber++;

                    var newNote = new Note(
                        GuidGenerator.Create(),
                        code,
                        StringHelper.Normalize(item.Content)
                    )
                    {
                        IsActive = true,
                        SortOrder = 0
                    };
                    await _noteRepository.InsertAsync(newNote);
                    processedNotes[noteKey] = newNote;
                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newCount + updatedCount,
                Message = $"Import thành công: {newCount} ghi chú mới, {updatedCount} ghi chú cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateNoteErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        // Find or add error column (column 2 since we only have 1 column)
        var errorColumnIndex = 2;
        var headerRow = worksheet.Row(1);
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Add errors to rows
        int dataRowNumber = 2;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var content = row.Cell(1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(content)) continue;

            var hasError = rowErrors.ContainsKey(dataRowNumber);

            // Column 2: Lỗi
            if (hasError)
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", rowErrors[dataRowNumber]);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }

            dataRowNumber++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Get the next code number for new notes (GC-XXXX)
    /// </summary>
    private async Task<int> GetNextNoteCodeNumberAsync()
    {
        var existingNotes = await _noteRepository.GetListAsync();
        var codes = existingNotes
            .Where(n => n.Code.StartsWith("GC-"))
            .Select(n => n.Code)
            .ToList();

        int maxNumber = 0;
        foreach (var code in codes)
        {
            var numberPart = code.Replace("GC-", "");
            if (int.TryParse(numberPart, out var num) && num > maxNumber)
            {
                maxNumber = num;
            }
        }

        return maxNumber + 1;
    }

    #endregion

    #region Warehouse Import

    public async Task<byte[]> GetWarehouseTemplateAsync()
    {
        var warehouses = await _warehouseRepository.GetListAsync();
        var activeWarehouses = warehouses.Where(w => w.IsActive).ToList();

        var labIds = activeWarehouses.Where(w => w.LaboratoryId.HasValue)
            .Select(w => w.LaboratoryId!.Value).Distinct().ToList();
        var labNames = new Dictionary<Guid, string>();
        if (labIds.Count > 0)
        {
            var labQuery = await _laboratoryRepository.GetQueryableAsync();
            var labs = labQuery.Where(x => labIds.Contains(x.Id)).Select(x => new { x.Id, x.DepartmentId }).ToList();
            var deptIds = labs.Select(x => x.DepartmentId).Distinct().ToList();
            var deptRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<Department, Guid>>();
            var deptQuery = await deptRepo.GetQueryableAsync();
            var deptNameMap = deptQuery.Where(x => deptIds.Contains(x.Id)).ToList().ToDictionary(x => x.Id, x => x.Name);
            foreach (var lab in labs)
                if (deptNameMap.TryGetValue(lab.DepartmentId, out var deptName))
                    labNames[lab.Id] = deptName;
        }

        var templateBytes = ReadStaticTemplate("Danh muc kho.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(1);

        if (activeWarehouses.Count > 0)
        {
            var warehouseById = activeWarehouses.ToDictionary(w => w.Id);
            var sorted = activeWarehouses.OrderBy(w => w.Code).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = sorted[i].Code ?? "";
                worksheet.Cell(row, 2).Value = sorted[i].Name ?? "";
                var parentCode = sorted[i].ParentWarehouseId.HasValue
                    && warehouseById.TryGetValue(sorted[i].ParentWarehouseId.Value, out var parent)
                    ? parent.Code : "";
                worksheet.Cell(row, 3).Value = parentCode;
                worksheet.Cell(row, 4).Value = sorted[i].PhysicalLocation ?? "";
                var labName = sorted[i].LaboratoryId.HasValue
                    && labNames.TryGetValue(sorted[i].LaboratoryId.Value, out var ln) ? ln : "";
                worksheet.Cell(row, 5).Value = labName;
            }
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    public async Task<WarehouseImportPreviewDto> PreviewWarehousesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewWarehousesInternalAsync(memoryStream);
    }

    private async Task<WarehouseImportPreviewDto> PreviewWarehousesInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadWarehouseExcelRows(memoryStream);

        var rowErrors = new Dictionary<int, List<string>>();

        var existingWarehouses = await _warehouseRepository.GetListAsync();
        var warehouseByCode = existingWarehouses
            .GroupBy(x => x.Code.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new WarehouseImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;

            var item = new WarehouseImportPreviewItem
            {
                RowNumber = rowNumber,
                Code = row.Code?.Trim() ?? "",
                Name = row.Name?.Trim() ?? "",
                ParentWarehouseCode = row.ParentWarehouseCode?.Trim(),
                PhysicalLocation = row.PhysicalLocation?.Trim(),
                LaboratoryName = row.LaboratoryName?.Trim(),
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Code))
            {
                item.IsValid = false;
                var error = "Mã kho không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Mã kho",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else if (!seenCodes.Add(row.Code.Trim()))
            {
                item.IsValid = false;
                var error = $"Mã kho '{row.Code.Trim()}' bị trùng trong file";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Mã kho",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                item.IsValid = false;
                var error = "Tên kho không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên kho",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (!string.IsNullOrWhiteSpace(row.ParentWarehouseCode))
            {
                var parentCodeTrimmed = row.ParentWarehouseCode.Trim();
                var parentCodeLower = parentCodeTrimmed.ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(row.Code) &&
                    string.Equals(parentCodeTrimmed, row.Code.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    item.IsValid = false;
                    var error = "Kho cha không thể là chính kho này";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Kho cha",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!warehouseByCode.ContainsKey(parentCodeLower))
                {
                    item.IsValid = false;
                    var error = $"Mã kho cha '{parentCodeTrimmed}' không tồn tại trong hệ thống";
                    rowErrorMessages.Add(error);
                    validationResult.Errors.Add(new ImportRowError
                    {
                        RowNumber = rowNumber,
                        ColumnName = "Kho cha",
                        ErrorMessage = error
                    });
                    AddRowError(rowErrors, rowNumber, error);
                }
                else if (!string.IsNullOrWhiteSpace(row.Code))
                {
                    var codeLower = row.Code.Trim().ToLowerInvariant();
                    if (warehouseByCode.TryGetValue(codeLower, out var selfWarehouse))
                    {
                        var parentWarehouse = warehouseByCode[parentCodeLower];
                        var currentId = (Guid?)parentWarehouse.ParentWarehouseId;
                        var allWarehouses = existingWarehouses;
                        while (currentId != null)
                        {
                            if (currentId == selfWarehouse.Id)
                            {
                                item.IsValid = false;
                                var error = $"Không thể chọn kho con của chính kho này làm kho cha (vòng lặp)";
                                rowErrorMessages.Add(error);
                                validationResult.Errors.Add(new ImportRowError
                                {
                                    RowNumber = rowNumber,
                                    ColumnName = "Kho cha",
                                    ErrorMessage = error
                                });
                                AddRowError(rowErrors, rowNumber, error);
                                break;
                            }
                            currentId = allWarehouses.FirstOrDefault(x => x.Id == currentId)?.ParentWarehouseId;
                        }
                    }
                }
            }

            if (item.IsValid && !string.IsNullOrWhiteSpace(row.Code))
            {
                var codeKey = row.Code.Trim().ToLowerInvariant();
                if (warehouseByCode.TryGetValue(codeKey, out var existingWarehouse))
                {
                    item.IsNew = false;
                    item.ExistingId = existingWarehouse.Id;
                    updateCodes.Add(codeKey);
                }
                else
                {
                    item.IsNew = true;
                    newCodes.Add(codeKey);
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
        result.NewWarehouses = newCodes.Count;
        result.UpdateWarehouses = updateCodes.Count;

        var labIds = existingWarehouses.Where(w => w.LaboratoryId.HasValue)
            .Select(w => w.LaboratoryId!.Value).Distinct().ToList();
        var labNameMap = new Dictionary<Guid, string>();
        if (labIds.Count > 0)
        {
            var labQuery = await _laboratoryRepository.GetQueryableAsync();
            var labs = labQuery.Where(x => labIds.Contains(x.Id)).Select(x => new { x.Id, x.DepartmentId }).ToList();
            var deptIds = labs.Select(x => x.DepartmentId).Distinct().ToList();
            var deptRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<Department, Guid>>();
            var deptQuery = await deptRepo.GetQueryableAsync();
            var deptNames = deptQuery.Where(x => deptIds.Contains(x.Id)).ToList().ToDictionary(x => x.Id, x => x.Name);
            foreach (var lab in labs)
                if (deptNames.TryGetValue(lab.DepartmentId, out var dn))
                    labNameMap[lab.Id] = dn;
        }

        foreach (var item in result.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.LaboratoryName))
                continue;

            Guid? labId = null;
            var codeKey = item.Code.Trim().ToLowerInvariant();
            if (warehouseByCode.TryGetValue(codeKey, out var wh) && wh.LaboratoryId.HasValue)
                labId = wh.LaboratoryId;
            if (!labId.HasValue && !string.IsNullOrWhiteSpace(item.ParentWarehouseCode))
            {
                var pCode = item.ParentWarehouseCode.Trim().ToLowerInvariant();
                if (warehouseByCode.TryGetValue(pCode, out var parentWh))
                {
                    var curId = (Guid?)parentWh.Id;
                    while (curId.HasValue)
                    {
                        var anc = existingWarehouses.FirstOrDefault(w => w.Id == curId.Value);
                        if (anc == null) break;
                        if (anc.LaboratoryId.HasValue) { labId = anc.LaboratoryId; break; }
                        curId = anc.ParentWarehouseId;
                    }
                }
            }
            if (labId.HasValue && labNameMap.TryGetValue(labId.Value, out var labName))
                item.LaboratoryName = labName;
        }

        if (!validationResult.IsValid)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateWarehouseErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Danh_muc_kho_Loi.xlsx";
        }

        return result;
    }

    private List<WarehouseImportRow> ReadWarehouseExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<WarehouseImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var code = row.Cell(1).GetString()?.Trim();
            var name = row.Cell(2).GetString()?.Trim();
            var parentCode = row.Cell(3).GetString()?.Trim();
            var physicalLocation = row.Cell(4).GetString()?.Trim();
            var laboratoryName = row.Cell(5).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(name)
                && string.IsNullOrWhiteSpace(parentCode) && string.IsNullOrWhiteSpace(physicalLocation))
                continue;

            rows.Add(new WarehouseImportRow
            {
                Code = code ?? "",
                Name = name ?? "",
                ParentWarehouseCode = parentCode,
                PhysicalLocation = physicalLocation,
                LaboratoryName = laboratoryName
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportWarehousesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewWarehousesInternalAsync(memoryStream);

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

        try
        {
            var existingWarehouses = await _warehouseRepository.GetListAsync();
            var warehouseByCode = new Dictionary<string, Warehouse>(StringComparer.OrdinalIgnoreCase);
            foreach (var warehouse in existingWarehouses)
            {
                var key = warehouse.Code.Trim();
                if (!warehouseByCode.ContainsKey(key))
                {
                    warehouseByCode[key] = warehouse;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            var labNameToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var labQuery = await _laboratoryRepository.GetQueryableAsync();
            var allLabs = labQuery.Select(x => new { x.Id, x.DepartmentId }).ToList();
            if (allLabs.Count > 0)
            {
                var deptIds = allLabs.Select(x => x.DepartmentId).Distinct().ToList();
                var deptRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<Department, Guid>>();
                var deptQuery = await deptRepo.GetQueryableAsync();
                var deptNames = deptQuery.Where(x => deptIds.Contains(x.Id)).ToList().ToDictionary(x => x.Id, x => x.Name);
                foreach (var lab in allLabs)
                    if (deptNames.TryGetValue(lab.DepartmentId, out var dn))
                        labNameToId[dn] = lab.Id;
            }

            foreach (var item in validItems)
            {
                Guid? parentId = null;
                int level = 0;

                if (!string.IsNullOrWhiteSpace(item.ParentWarehouseCode))
                {
                    if (warehouseByCode.TryGetValue(item.ParentWarehouseCode.Trim(), out var parentWarehouse))
                    {
                        parentId = parentWarehouse.Id;
                        level = parentWarehouse.Level + 1;
                    }
                }

                Guid? labId = null;
                if (!string.IsNullOrWhiteSpace(item.LaboratoryName) && labNameToId.TryGetValue(item.LaboratoryName.Trim(), out var resolvedLabId))
                {
                    labId = resolvedLabId;
                }
                else if (parentId.HasValue)
                {
                    var currentId = parentId;
                    while (currentId.HasValue)
                    {
                        var ancestor = warehouseByCode.Values.FirstOrDefault(w => w.Id == currentId.Value);
                        if (ancestor == null) break;
                        if (ancestor.LaboratoryId.HasValue) { labId = ancestor.LaboratoryId; break; }
                        currentId = ancestor.ParentWarehouseId;
                    }
                }

                if (warehouseByCode.TryGetValue(item.Code.Trim(), out var existingWarehouse))
                {
                    existingWarehouse.Name = StringHelper.Normalize(item.Name);
                    existingWarehouse.PhysicalLocation = item.PhysicalLocation;
                    existingWarehouse.ParentWarehouseId = parentId;
                    existingWarehouse.Level = level;
                    if (labId.HasValue)
                        existingWarehouse.LaboratoryId = labId;
                    await _warehouseRepository.UpdateAsync(existingWarehouse);
                    updatedCount++;
                }
                else
                {
                    var newWarehouse = new Warehouse(
                        GuidGenerator.Create(),
                        item.Code.Trim(),
                        StringHelper.Normalize(item.Name),
                        labId
                    )
                    {
                        PhysicalLocation = item.PhysicalLocation,
                        ParentWarehouseId = parentId,
                        Level = level,
                        IsActive = true,
                        SortOrder = 0
                    };
                    await _warehouseRepository.InsertAsync(newWarehouse);
                    warehouseByCode[item.Code.Trim()] = newWarehouse;
                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newCount + updatedCount,
                Message = $"Import thành công: {newCount} kho mới, {updatedCount} kho cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateWarehouseErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        var errorColumnIndex = 6;
        var headerRow = worksheet.Row(1);
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        int dataRowNumber = 2;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var c1 = row.Cell(1).GetString()?.Trim();
            var c2 = row.Cell(2).GetString()?.Trim();
            var c3 = row.Cell(3).GetString()?.Trim();
            var c4 = row.Cell(4).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(c1) && string.IsNullOrWhiteSpace(c2)
                && string.IsNullOrWhiteSpace(c3) && string.IsNullOrWhiteSpace(c4))
                continue;

            if (rowErrors.TryGetValue(dataRowNumber, out var errors))
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", errors);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }

            dataRowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    #endregion

    #region TestResultValue Import

    public async Task<byte[]> GetTestResultValueTemplateAsync()
    {
        var trvQuery = await _testResultValueRepository.GetQueryableAsync();
        var trviQuery = await _testResultValueIndicatorRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();

        var resultValues = await AsyncExecuter.ToListAsync(
            from trv in trvQuery
            where trv.IsActive
            orderby trv.Name
            select new { trv.Id, trv.Name, trv.Description }
        );

        if (resultValues.Count == 0)
            return ReadStaticTemplate("Kết quả.xlsx");

        var links = await AsyncExecuter.ToListAsync(
            from trvi in trviQuery
            join ind in indicatorQuery on trvi.IndicatorId equals ind.Id
            join st in specimenTypeQuery on trvi.SpecimenTypeId equals st.Id into stj
            from st in stj.DefaultIfEmpty()
            where trvi.IsActive
            select new { trvi.TestResultValueId, IndicatorName = ind.Name, SpecimenTypeName = st != null ? st.Name : "" }
        );
        var linksByValue = links.GroupBy(x => x.TestResultValueId).ToDictionary(g => g.Key, g => g.ToList());

        var templateData = new List<Dictionary<string, object>>();

        foreach (var rv in resultValues)
        {
            if (linksByValue.TryGetValue(rv.Id, out var rvLinks) && rvLinks.Count > 0)
            {
                foreach (var link in rvLinks)
                {
                    templateData.Add(new Dictionary<string, object>
                    {
                        { "Tên kết quả", rv.Name ?? "" },
                        { "Mô tả", rv.Description ?? "" },
                        { "Nền mẫu", link.SpecimenTypeName ?? "" },
                        { "Chỉ tiêu", link.IndicatorName ?? "" }
                    });
                }
            }
            else
            {
                templateData.Add(new Dictionary<string, object>
                {
                    { "Tên kết quả", rv.Name ?? "" },
                    { "Mô tả", rv.Description ?? "" },
                    { "Nền mẫu", "" },
                    { "Chỉ tiêu", "" }
                });
            }
        }

        using var stream = new MemoryStream();
        MiniExcelLibs.MiniExcel.SaveAs(stream, templateData);
        return stream.ToArray();
    }

    public async Task<TestResultValueImportPreviewDto> PreviewTestResultValuesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewTestResultValuesInternalAsync(memoryStream);
    }

    private async Task<TestResultValueImportPreviewDto> PreviewTestResultValuesInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadTestResultValueExcelRows(memoryStream);

        if (rows.Count == 0)
        {
            return new TestResultValueImportPreviewDto
            {
                ValidationResult = new ImportValidationResult { TotalRows = 0 }
            };
        }

        // Store row errors for error file generation
        var rowErrors = new Dictionary<int, List<string>>();

        // Load existing data for validation
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var existingSpecimenTypes = await _specimenTypeRepository.GetListAsync();
            var specimenTypeDict = existingSpecimenTypes
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var existingIndicators = await _indicatorRepository.GetListAsync();
            var indicatorDict = existingIndicators
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // Get specimen type indicators for validation (indicator must belong to specimen type)
            var specimenTypeIndicators = await _specimenTypeIndicatorRepository.GetListAsync();
            var specimenTypeIndicatorSet = specimenTypeIndicators
                .Where(x => x.IsActive)
                .Select(x => (x.SpecimenTypeId, x.IndicatorId))
                .ToHashSet();

            var existingTestResultValues = await _testResultValueRepository.GetListAsync();
            var testResultValueDict = existingTestResultValues
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // Get existing TestResultValue-Indicator links
            var existingLinks = await _testResultValueIndicatorRepository.GetListAsync();
            var existingLinkSet = existingLinks
                .Select(x => (x.TestResultValueId, x.IndicatorId, x.SpecimenTypeId))
                .ToHashSet();

            var result = new TestResultValueImportPreviewDto();
            var validationResult = new ImportValidationResult { TotalRows = rows.Count };

            // Group rows by ResultName (Tên kết quả)
            var groupedRows = rows
                .Select((row, index) => new { Row = row, Index = index, RowNumber = index + 2 })
                .GroupBy(x => StringHelper.NormalizeAndTrim(x.Row.ResultName).ToLowerInvariant())
                .ToList();

            foreach (var group in groupedRows)
            {
                var firstRow = group.First();
                var resultNameKey = group.Key;

                var previewGroup = new TestResultValueImportPreviewGroup
                {
                    ResultName = firstRow.Row.ResultName?.Trim() ?? "",
                    Description = firstRow.Row.Description?.Trim()
                };

                // Validate result name (required)
                if (string.IsNullOrWhiteSpace(firstRow.Row.ResultName))
                {
                    foreach (var item in group)
                    {
                        var error = "Tên kết quả không được để trống";
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = item.RowNumber,
                            ColumnName = "Tên kết quả",
                            ErrorMessage = error
                        });
                        AddRowError(rowErrors, item.RowNumber, error);
                    }
                    // Skip this group as result name is invalid
                    continue;
                }

                // Check if result exists (for upsert logic)
                if (testResultValueDict.TryGetValue(resultNameKey, out var existingResult))
                {
                    previewGroup.IsNew = false;
                    previewGroup.ExistingId = existingResult.Id;
                }
                else
                {
                    previewGroup.IsNew = true;
                }

                var groupIsValid = true;

                // Validate indicators for each row in the group
                foreach (var item in group)
                {
                    // Skip rows without SpecimenType AND Indicator (they just define the result itself)
                    if (string.IsNullOrWhiteSpace(item.Row.SpecimenTypeName) &&
                        string.IsNullOrWhiteSpace(item.Row.IndicatorName))
                    {
                        continue;
                    }

                    var indicatorItem = new TestResultValueIndicatorImportItem
                    {
                        RowNumber = item.RowNumber,
                        SpecimenTypeName = item.Row.SpecimenTypeName?.Trim(),
                        IndicatorName = item.Row.IndicatorName?.Trim(),
                        IsValid = true
                    };

                    // If indicator is provided, specimen type must also be provided
                    if (!string.IsNullOrWhiteSpace(item.Row.IndicatorName) &&
                        string.IsNullOrWhiteSpace(item.Row.SpecimenTypeName))
                    {
                        indicatorItem.IsValid = false;
                        indicatorItem.ErrorMessage = "Cần chọn nền mẫu khi có chỉ tiêu";
                        groupIsValid = false;
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = item.RowNumber,
                            ColumnName = "Nền mẫu",
                            ErrorMessage = indicatorItem.ErrorMessage
                        });
                        AddRowError(rowErrors, item.RowNumber, indicatorItem.ErrorMessage);
                        previewGroup.Indicators.Add(indicatorItem);
                        continue;
                    }

                    // Validate specimen type exists
                    SpecimenType? specimenType = null;
                    if (!string.IsNullOrWhiteSpace(item.Row.SpecimenTypeName))
                    {
                        var specimenTypeKey = StringHelper.NormalizeAndTrim(item.Row.SpecimenTypeName).ToLowerInvariant();
                        if (!specimenTypeDict.TryGetValue(specimenTypeKey, out specimenType))
                        {
                            indicatorItem.IsValid = false;
                            indicatorItem.ErrorMessage = $"Không tìm thấy nền mẫu: {item.Row.SpecimenTypeName}";
                            groupIsValid = false;
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = item.RowNumber,
                                ColumnName = "Nền mẫu",
                                ErrorMessage = indicatorItem.ErrorMessage,
                                Value = item.Row.SpecimenTypeName
                            });
                            AddRowError(rowErrors, item.RowNumber, indicatorItem.ErrorMessage);
                            previewGroup.Indicators.Add(indicatorItem);
                            continue;
                        }
                        indicatorItem.SpecimenTypeId = specimenType.Id;
                    }

                    // Validate indicator exists
                    Indicator? indicator = null;
                    if (!string.IsNullOrWhiteSpace(item.Row.IndicatorName))
                    {
                        var indicatorKey = StringHelper.NormalizeAndTrim(item.Row.IndicatorName).ToLowerInvariant();
                        if (!indicatorDict.TryGetValue(indicatorKey, out indicator))
                        {
                            indicatorItem.IsValid = false;
                            indicatorItem.ErrorMessage = $"Không tìm thấy chỉ tiêu: {item.Row.IndicatorName}";
                            groupIsValid = false;
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = item.RowNumber,
                                ColumnName = "Chỉ tiêu",
                                ErrorMessage = indicatorItem.ErrorMessage,
                                Value = item.Row.IndicatorName
                            });
                            AddRowError(rowErrors, item.RowNumber, indicatorItem.ErrorMessage);
                            previewGroup.Indicators.Add(indicatorItem);
                            continue;
                        }
                        indicatorItem.IndicatorId = indicator.Id;

                        // Validate indicator belongs to specimen type
                        if (specimenType != null && !specimenTypeIndicatorSet.Contains((specimenType.Id, indicator.Id)))
                        {
                            indicatorItem.IsValid = false;
                            indicatorItem.ErrorMessage = $"Chỉ tiêu '{item.Row.IndicatorName}' không thuộc nền mẫu '{item.Row.SpecimenTypeName}'";
                            groupIsValid = false;
                            validationResult.Errors.Add(new ImportRowError
                            {
                                RowNumber = item.RowNumber,
                                ColumnName = "Chỉ tiêu",
                                ErrorMessage = indicatorItem.ErrorMessage,
                                Value = item.Row.IndicatorName
                            });
                            AddRowError(rowErrors, item.RowNumber, indicatorItem.ErrorMessage);
                        }
                    }

                    previewGroup.Indicators.Add(indicatorItem);
                }

                // Count valid rows
                if (groupIsValid)
                {
                    validationResult.ValidRows += group.Count();
                }

                result.Groups.Add(previewGroup);
            }

            result.ValidationResult = validationResult;
            result.TotalResults = result.Groups.Count;
            result.NewResults = result.Groups.Count(g => g.IsNew);
            result.UpdateResults = result.Groups.Count(g => !g.IsNew);
            result.TotalIndicators = result.Groups.Sum(g => g.Indicators.Count(i => i.IsValid && i.IndicatorId.HasValue));

            // Generate error file if there are errors
            if (!validationResult.IsValid)
            {
                memoryStream.Position = 0;
                var errorFileBytes = GenerateTestResultValueErrorFile(memoryStream, rowErrors);
                result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
                result.ErrorFileName = "Ket_Qua_Loi.xlsx";
            }

            return result;
        }
    }

    private List<TestResultValueImportRow> ReadTestResultValueExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<TestResultValueImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        // Start from row 2 (skip header)
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty()) continue;

            var resultName = row.Cell(1).GetString()?.Trim();
            var description = row.Cell(2).GetString()?.Trim();
            var specimenTypeName = row.Cell(3).GetString()?.Trim();
            var indicatorName = row.Cell(4).GetString()?.Trim();

            // Skip if all cells are empty
            if (string.IsNullOrWhiteSpace(resultName) &&
                string.IsNullOrWhiteSpace(description) &&
                string.IsNullOrWhiteSpace(specimenTypeName) &&
                string.IsNullOrWhiteSpace(indicatorName))
                continue;

            rows.Add(new TestResultValueImportRow
            {
                ResultName = resultName ?? "",
                Description = description,
                SpecimenTypeName = specimenTypeName,
                IndicatorName = indicatorName
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportTestResultValuesAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewTestResultValuesInternalAsync(memoryStream);

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

        var newResultCount = 0;
        var updatedResultCount = 0;
        var newIndicatorLinkCount = 0;

        try
        {
            // Get existing TestResultValue-Indicator links for duplicate check
            var existingLinks = await _testResultValueIndicatorRepository.GetListAsync();
            var linkSet = existingLinks
                .Select(x => (x.TestResultValueId, x.IndicatorId, x.SpecimenTypeId))
                .ToHashSet();

            // Get max code number for auto-generation
            var existingTestResultValues = await _testResultValueRepository.GetListAsync();
            var maxCodeNumber = existingTestResultValues
                .Where(x => x.Code.StartsWith("KQ-"))
                .Select(x =>
                {
                    var numPart = x.Code.Substring(3);
                    return int.TryParse(numPart, out var num) ? num : 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            var codeCounter = maxCodeNumber;

            var validGroups = preview.Groups.Where(g =>
                !string.IsNullOrWhiteSpace(g.ResultName) &&
                g.Indicators.All(i => i.IsValid || (!i.IndicatorId.HasValue && !i.SpecimenTypeId.HasValue))
            ).ToList();

            foreach (var group in validGroups)
            {
                TestResultValue testResultValue;

                if (group.IsNew)
                {
                    // Generate new code
                    codeCounter++;
                    var code = $"KQ-{codeCounter:D4}";

                    // Create new test result value
                    testResultValue = new TestResultValue(
                        _guidGenerator.Create(),
                        code,
                        StringHelper.Normalize(group.ResultName)
                    )
                    {
                        Description = group.Description != null ? StringHelper.Normalize(group.Description) : null,
                        IsActive = true,
                        SortOrder = 0
                    };
                    await _testResultValueRepository.InsertAsync(testResultValue);
                    newResultCount++;
                }
                else
                {
                    // Update existing test result value
                    testResultValue = await _testResultValueRepository.GetAsync(group.ExistingId!.Value);
                    testResultValue.Name = StringHelper.Normalize(group.ResultName);
                    testResultValue.Description = group.Description != null ? StringHelper.Normalize(group.Description) : null;
                    await _testResultValueRepository.UpdateAsync(testResultValue);
                    updatedResultCount++;
                }

                // Add indicator links
                foreach (var indicatorItem in group.Indicators.Where(i => i.IsValid && i.IndicatorId.HasValue && i.SpecimenTypeId.HasValue))
                {
                    var linkKey = (testResultValue.Id, indicatorItem.IndicatorId!.Value, indicatorItem.SpecimenTypeId!.Value);

                    // Skip if already exists
                    if (linkSet.Contains(linkKey))
                        continue;

                    var indicator = new TestResultValueIndicator(
                        _guidGenerator.Create(),
                        testResultValue.Id,
                        indicatorItem.IndicatorId!.Value,
                        indicatorItem.SpecimenTypeId!.Value
                    );
                    await _testResultValueIndicatorRepository.InsertAsync(indicator);
                    linkSet.Add(linkKey);
                    newIndicatorLinkCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newResultCount + updatedResultCount,
                Message = $"Import thành công! Đã tạo mới {newResultCount} kết quả, cập nhật {updatedResultCount} kết quả, thêm {newIndicatorLinkCount} liên kết chỉ tiêu."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateTestResultValueErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        originalStream.Position = 0;
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        // Add error column header
        var headerRow = worksheet.Row(1);
        var errorColumnIndex = 5; // Column E for errors (after 4 data columns)
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
                    if (errorMsg.Contains("Tên kết quả"))
                        errorColumnsForRow.Add("ResultName");
                    if (errorMsg.Contains("nền mẫu") || errorMsg.Contains("Nền mẫu"))
                        errorColumnsForRow.Add("SpecimenTypeName");
                    if (errorMsg.Contains("chỉ tiêu") || errorMsg.Contains("Chỉ tiêu"))
                        errorColumnsForRow.Add("IndicatorName");
                }
            }

            var row = worksheet.Row(excelRowNumber);

            // Column 1: Tên kết quả
            if (errorColumnsForRow.Contains("ResultName"))
            {
                ApplyErrorStyle(row.Cell(1));
            }

            // Column 3: Nền mẫu
            if (errorColumnsForRow.Contains("SpecimenTypeName"))
            {
                ApplyErrorStyle(row.Cell(3));
            }

            // Column 4: Chỉ tiêu
            if (errorColumnsForRow.Contains("IndicatorName"))
            {
                ApplyErrorStyle(row.Cell(4));
            }

            // Column 5: Lỗi
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

    #region Manufacturer Import

    public async Task<byte[]> GetManufacturerTemplateAsync()
    {
        var manufacturers = await _manufacturerRepository.GetListAsync();
        var activeManufacturers = manufacturers.Where(m => m.IsActive).ToList();

        var templateBytes = ReadStaticTemplate("Danh muc hang san xuat.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(1);

        if (activeManufacturers.Count > 0)
        {
            var sorted = activeManufacturers.OrderBy(m => m.Name).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = sorted[i].Name ?? "";
                worksheet.Cell(row, 2).Value = sorted[i].Description ?? "";
            }
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    public async Task<ManufacturerImportPreviewDto> PreviewManufacturersAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewManufacturersInternalAsync(memoryStream);
    }

    private async Task<ManufacturerImportPreviewDto> PreviewManufacturersInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadManufacturerExcelRows(memoryStream);

        var rowErrors = new Dictionary<int, List<string>>();

        var existingManufacturers = await _manufacturerRepository.GetListAsync();
        var manufacturerDict = existingManufacturers
            .GroupBy(x => StringHelper.NormalizeAndTrim(x.Name).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new ManufacturerImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var newNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;

            var item = new ManufacturerImportPreviewItem
            {
                RowNumber = rowNumber,
                Name = row.Name?.Trim() ?? "",
                Description = row.Description?.Trim(),
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                item.IsValid = false;
                var error = "Tên hãng sản xuất không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên hãng sản xuất",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else
            {
                var normalizedName = StringHelper.NormalizeAndTrim(row.Name).ToLowerInvariant();
                if (manufacturerDict.TryGetValue(normalizedName, out var existing))
                {
                    item.IsNew = false;
                    item.ExistingId = existing.Id;
                    updateNames.Add(normalizedName);
                }
                else
                {
                    item.IsNew = true;
                    newNames.Add(normalizedName);
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
        result.NewManufacturers = newNames.Count;
        result.UpdateManufacturers = updateNames.Count;

        if (!validationResult.IsValid)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateManufacturerErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Danh_muc_hang_san_xuat_Loi.xlsx";
        }

        return result;
    }

    private List<ManufacturerImportRow> ReadManufacturerExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<ManufacturerImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var name = row.Cell(1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            rows.Add(new ManufacturerImportRow
            {
                Name = name,
                Description = row.Cell(2).GetString()?.Trim()
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportManufacturersAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewManufacturersInternalAsync(memoryStream);

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

        var processedManufacturers = new Dictionary<string, Manufacturer>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var existingManufacturers = await _manufacturerRepository.GetListAsync();
            foreach (var manufacturer in existingManufacturers)
            {
                var key = StringHelper.NormalizeAndTrim(manufacturer.Name).ToLowerInvariant();
                if (!processedManufacturers.ContainsKey(key))
                {
                    processedManufacturers[key] = manufacturer;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            var nextCodeNumber = await GetNextManufacturerCodeNumberAsync();

            foreach (var item in validItems)
            {
                var manufacturerKey = StringHelper.NormalizeAndTrim(item.Name).ToLowerInvariant();

                if (processedManufacturers.TryGetValue(manufacturerKey, out var existing))
                {
                    existing.Name = StringHelper.Normalize(item.Name);
                    existing.Description = item.Description;
                    await _manufacturerRepository.UpdateAsync(existing);
                    updatedCount++;
                }
                else
                {
                    var code = $"HSX-{nextCodeNumber:D3}";
                    nextCodeNumber++;

                    var newManufacturer = new Manufacturer(
                        GuidGenerator.Create(),
                        code,
                        StringHelper.Normalize(item.Name)
                    )
                    {
                        Description = item.Description,
                        IsActive = true
                    };
                    await _manufacturerRepository.InsertAsync(newManufacturer);
                    processedManufacturers[manufacturerKey] = newManufacturer;
                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newCount + updatedCount,
                Message = $"Import thành công: {newCount} hãng SX mới, {updatedCount} hãng SX cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateManufacturerErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        var errorColumnIndex = 3;
        var headerRow = worksheet.Row(1);
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        int dataRowNumber = 2;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var name = row.Cell(1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var hasError = rowErrors.ContainsKey(dataRowNumber);

            if (hasError)
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", rowErrors[dataRowNumber]);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }

            dataRowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    private async Task<int> GetNextManufacturerCodeNumberAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var existingManufacturers = await _manufacturerRepository.GetListAsync();
            var codes = existingManufacturers
                .Where(m => m.Code.StartsWith("HSX"))
                .Select(m => m.Code)
                .ToList();

            int maxNumber = 0;
            foreach (var code in codes)
            {
                var numberPart = code.Replace("HSX-", "").Replace("HSX", "");
                if (int.TryParse(numberPart, out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }

            return maxNumber + 1;
        }
    }

    #endregion

    #region Unit Import

    private static readonly Dictionary<int, MasterData.UnitCategory> ExcelCategoryToEnum = new()
    {
        { 1, MasterData.UnitCategory.ConsumableUnit },
        { 2, MasterData.UnitCategory.StorageUnit },
        { 3, MasterData.UnitCategory.PurchaseUnit },
    };

    private static readonly Dictionary<MasterData.UnitCategory, int> EnumCategoryToExcel = new()
    {
        { MasterData.UnitCategory.ConsumableUnit, 1 },
        { MasterData.UnitCategory.StorageUnit, 2 },
        { MasterData.UnitCategory.PurchaseUnit, 3 },
    };

    private Dictionary<int, string> ReadUnitCategoryLabelsFromTemplate()
    {
        var labels = new Dictionary<int, string>();
        try
        {
            var templateBytes = ReadStaticTemplate("Đơn vị hàng hóa.xlsx");
            using var stream = new MemoryStream(templateBytes);
            using var workbook = new XLWorkbook(stream);

            IXLWorksheet? mappingSheet = null;
            try { mappingSheet = workbook.Worksheet("Hướng dẫn mapping"); }
            catch { return labels; }

            var lastRow = mappingSheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int r = 2; r <= lastRow; r++)
            {
                var label = mappingSheet.Cell(r, 1).GetString()?.Trim();
                var idStr = mappingSheet.Cell(r, 2).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(label) && int.TryParse(idStr, out var id))
                {
                    labels[id] = label;
                }
            }
        }
        catch { }
        return labels;
    }

    public async Task<byte[]> GetUnitTemplateAsync()
    {
        var units = await _unitRepository.GetListAsync();
        var activeUnits = units.Where(u => u.IsActive && u.Category.HasValue
            && EnumCategoryToExcel.ContainsKey(u.Category.Value)).ToList();

        var templateBytes = ReadStaticTemplate("Đơn vị hàng hóa.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(1);

        if (activeUnits.Count > 0)
        {
            var sorted = activeUnits.OrderBy(u => u.Code).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var row = i + 2;
                worksheet.Cell(row, 1).Value = EnumCategoryToExcel[sorted[i].Category!.Value];
                worksheet.Cell(row, 2).Value = sorted[i].Code ?? "";
                worksheet.Cell(row, 3).Value = sorted[i].Name ?? "";
            }
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    public async Task<UnitImportPreviewDto> PreviewUnitsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewUnitsInternalAsync(memoryStream);
    }

    private async Task<UnitImportPreviewDto> PreviewUnitsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadUnitExcelRows(memoryStream);

        var rowErrors = new Dictionary<int, List<string>>();

        var existingUnits = await _unitRepository.GetListAsync();
        var unitByCode = existingUnits
            .GroupBy(x => x.Code.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var categoryLabels = ReadUnitCategoryLabelsFromTemplate();

        var result = new UnitImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;

            int? parsedCategory = null;
            MasterData.UnitCategory? enumCategory = null;
            var categoryLabel = "";

            if (!string.IsNullOrWhiteSpace(row.CategoryValue))
            {
                if (int.TryParse(row.CategoryValue.Trim(), out var catInt) && ExcelCategoryToEnum.TryGetValue(catInt, out var mapped))
                {
                    parsedCategory = catInt;
                    enumCategory = mapped;
                    categoryLabels.TryGetValue(catInt, out categoryLabel);
                    categoryLabel ??= "";
                }
            }

            var item = new UnitImportPreviewItem
            {
                RowNumber = rowNumber,
                Category = parsedCategory,
                CategoryLabel = categoryLabel,
                Code = row.Code?.Trim() ?? "",
                Name = row.Name?.Trim() ?? "",
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            if (string.IsNullOrWhiteSpace(row.CategoryValue))
            {
                item.IsValid = false;
                var error = "Phân loại không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Phân loại",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else if (parsedCategory == null)
            {
                item.IsValid = false;
                var error = "Phân loại không hợp lệ";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Phân loại",
                    ErrorMessage = error,
                    Value = row.CategoryValue?.Trim()
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Code))
            {
                item.IsValid = false;
                var error = "Mã đơn vị tính không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Mã đơn vị tính",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }
            else if (!seenCodes.Add(row.Code.Trim()))
            {
                item.IsValid = false;
                var error = $"Mã đơn vị tính '{row.Code.Trim()}' bị trùng trong file";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Mã đơn vị tính",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                item.IsValid = false;
                var error = "Tên đơn vị tính không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Tên đơn vị tính",
                    ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (item.IsValid && !string.IsNullOrWhiteSpace(row.Code) && enumCategory.HasValue)
            {
                var codeKey = row.Code.Trim().ToLowerInvariant();
                if (unitByCode.TryGetValue(codeKey, out var existingUnit))
                {
                    if (existingUnit.Category == enumCategory.Value)
                    {
                        item.IsValid = false;
                        var error = "Đơn vị tính này đã tồn tại";
                        rowErrorMessages.Add(error);
                        validationResult.Errors.Add(new ImportRowError
                        {
                            RowNumber = rowNumber,
                            ColumnName = "Mã đơn vị tính",
                            ErrorMessage = error
                        });
                        AddRowError(rowErrors, rowNumber, error);
                    }
                    else
                    {
                        item.IsNew = false;
                        item.ExistingId = existingUnit.Id;
                    }
                }
                else
                {
                    item.IsNew = true;
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
        result.NewUnits = result.Items.Count(i => i.IsValid && i.IsNew);
        result.UpdateUnits = result.Items.Count(i => i.IsValid && !i.IsNew);

        if (!validationResult.IsValid)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateUnitErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Don_vi_hang_hoa_Loi.xlsx";
        }

        return result;
    }

    private List<UnitImportRow> ReadUnitExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<UnitImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var category = row.Cell(1).GetString()?.Trim();
            var code = row.Cell(2).GetString()?.Trim();
            var name = row.Cell(3).GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(code)
                && string.IsNullOrWhiteSpace(name))
                continue;

            rows.Add(new UnitImportRow
            {
                CategoryValue = category ?? "",
                Code = code ?? "",
                Name = name ?? ""
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportUnitsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewUnitsInternalAsync(memoryStream);

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

        try
        {
            var existingUnits = await _unitRepository.GetListAsync();
            var unitByCode = new Dictionary<string, MasterData.Unit>(StringComparer.OrdinalIgnoreCase);
            foreach (var unit in existingUnits)
            {
                var key = unit.Code.Trim();
                if (!unitByCode.ContainsKey(key))
                {
                    unitByCode[key] = unit;
                }
            }

            var validItems = preview.Items.Where(i => i.IsValid).ToList();

            foreach (var item in validItems)
            {
                var enumCategory = item.Category.HasValue && ExcelCategoryToEnum.TryGetValue(item.Category.Value, out var mapped)
                    ? mapped : (MasterData.UnitCategory?)null;

                if (unitByCode.TryGetValue(item.Code.Trim(), out var existingUnit))
                {
                    existingUnit.Name = StringHelper.Normalize(item.Name);
                    existingUnit.Category = enumCategory;
                    await _unitRepository.UpdateAsync(existingUnit);
                    updatedCount++;
                }
                else
                {
                    var newUnit = new MasterData.Unit(
                        GuidGenerator.Create(),
                        item.Code.Trim(),
                        StringHelper.Normalize(item.Name)
                    )
                    {
                        Category = enumCategory,
                        IsActive = true
                    };
                    await _unitRepository.InsertAsync(newUnit);
                    unitByCode[item.Code.Trim()] = newUnit;
                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.ValidationResult.TotalRows,
                ImportedRows = newCount + updatedCount,
                Message = $"Import thành công: {newCount} đơn vị mới, {updatedCount} đơn vị cập nhật."
            };
        }
        catch (Exception ex)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.ValidationResult.TotalRows,
                Message = $"Lỗi khi import: {ex.Message}"
            };
        }
    }

    private byte[] GenerateUnitErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        var errorColumnIndex = 4;
        var headerRow = worksheet.Row(1);
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        int dataRowNumber = 2;
        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var c1 = row.Cell(1).GetString()?.Trim();
            var c2 = row.Cell(2).GetString()?.Trim();
            var c3 = row.Cell(3).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(c1) && string.IsNullOrWhiteSpace(c2)
                && string.IsNullOrWhiteSpace(c3))
                continue;

            if (rowErrors.TryGetValue(dataRowNumber, out var errors))
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", errors);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }

            dataRowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    #endregion

    #region Product Import (Stock Data Import)

    public async Task<byte[]> GetProductTemplateAsync(Guid warehouseId)
    {
        var warehouse = await _warehouseRepository.GetAsync(warehouseId);

        var templateBytes = ReadStaticTemplate("Danh sach vat tu.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(1);

        var queryable = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var receiptQueryable = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var existingItems = (from item in queryable
                             join receipt in receiptQueryable
                                 on item.WarehouseImportReceiptId equals receipt.Id
                             where receipt.Status == WarehouseImportReceiptStatus.Completed
                                   && (item.StorageWarehouseId == warehouseId || receipt.WarehouseId == warehouseId)
                                   && item.ReceivedBaseQuantity > item.ConsumedBaseQuantity
                             select new
                             {
                                 item.ProductId,
                                 item.ProductName,
                                 item.LotNumber,
                                 item.LotExpiryDate,
                                 item.ReceivedBaseQuantity,
                                 item.ConsumedBaseQuantity,
                                 item.UnitName,
                                 item.SkuId,
                                 item.ManufacturerName,
                                 item.Notes,
                                 receipt.ActualImportDate,
                             }).ToList();

        var products = await _productRepository.GetListAsync();
        var productDict = products.ToDictionary(p => p.Id);

        var skuIds = existingItems
            .Where(x => x.SkuId.HasValue)
            .Select(x => x.SkuId!.Value)
            .Distinct().ToList();
        var skuQueryable = await _productSkuRepository.GetQueryableAsync();
        var skus = skuIds.Count > 0
            ? skuQueryable.Where(s => skuIds.Contains(s.Id)).ToList()
            : new List<ProductSku>();
        var skuDict = skus.ToDictionary(s => s.Id);

        var storageUnitIds = skus
            .Where(s => s.PackagingUnitId.HasValue)
            .Select(s => s.PackagingUnitId!.Value)
            .Distinct().ToList();
        var units = storageUnitIds.Count > 0
            ? await _unitRepository.GetListAsync(u => storageUnitIds.Contains(u.Id))
            : new List<Unit>();
        var unitNameDict = units.ToDictionary(u => u.Id, u => u.Name);

        var row = 2;
        foreach (var item in existingItems)
        {
            if (!productDict.TryGetValue(item.ProductId, out var product)) continue;

            var storageUnitName = item.UnitName ?? "";
            if (item.SkuId.HasValue && skuDict.TryGetValue(item.SkuId.Value, out var sku)
                && sku.PackagingUnitId.HasValue
                && unitNameDict.TryGetValue(sku.PackagingUnitId.Value, out var pkgName))
            {
                storageUnitName = pkgName;
            }

            worksheet.Cell(row, 1).Value = warehouseId.ToString();
            worksheet.Cell(row, 2).Value = warehouse.Name ?? "";
            worksheet.Cell(row, 3).Value = GetProductTypeLabel(product.ProductType);
            worksheet.Cell(row, 4).Value = product.IdentificationCode ?? "";
            worksheet.Cell(row, 5).Value = product.Name ?? "";
            var currentStock = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity;
            var openingStorage = currentStock;
            var consumedStorage = 0m;
            var endingStorage = currentStock;
            // F(6): Số lượng đầu kỳ, G(7): ĐVT lưu kho (đơn vị lưu trữ)
            worksheet.Cell(row, 6).Value = (double)openingStorage;
            worksheet.Cell(row, 7).Value = storageUnitName;
            // H(8): Sô lượng tiêu hao
            worksheet.Cell(row, 8).Value = (double)consumedStorage;
            // I(9): Hạn sử dụng, J(10): Ngày nhận
            if (item.LotExpiryDate.HasValue)
                worksheet.Cell(row, 9).Value = item.LotExpiryDate.Value;
            worksheet.Cell(row, 10).Value = item.ActualImportDate;
            // K(11): Tồn kho cuối kỳ
            worksheet.Cell(row, 11).Value = (double)endingStorage;
            // L(12): Code/Lot, M(13): Nhà sản xuất
            worksheet.Cell(row, 12).Value = item.LotNumber ?? "";
            worksheet.Cell(row, 13).Value = item.ManufacturerName ?? "";
            // N(14): Ghi chú
            worksheet.Cell(row, 14).Value = item.Notes ?? "";
            row++;
        }

        if (row == 2)
        {
            worksheet.Cell(2, 1).Value = warehouseId.ToString();
            worksheet.Cell(2, 2).Value = warehouse.Name ?? "";
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    public async Task<ProductImportPreviewDto> PreviewProductsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewProductsInternalAsync(memoryStream);
    }

    private async Task<ProductImportPreviewDto> PreviewProductsInternalAsync(MemoryStream memoryStream)
    {
        var rows = ReadProductExcelRows(memoryStream);
        var rowErrors = new Dictionary<int, List<string>>();

        var result = new ProductImportPreviewDto();
        var validationResult = new ImportValidationResult { TotalRows = rows.Count };

        if (rows.Count == 0)
        {
            result.ValidationResult = validationResult;
            return result;
        }

        var firstWarehouseId = rows[0].WarehouseId?.Trim();
        Guid warehouseGuid;
        Warehouse? warehouse = null;

        if (!Guid.TryParse(firstWarehouseId, out warehouseGuid))
        {
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = 2,
                ColumnName = "ID Kho",
                ErrorMessage = "ID Kho không hợp lệ"
            });
            result.ValidationResult = validationResult;
            return result;
        }

        warehouse = await _warehouseRepository.FindAsync(warehouseGuid);
        if (warehouse == null)
        {
            validationResult.Errors.Add(new ImportRowError
            {
                RowNumber = 2,
                ColumnName = "ID Kho",
                ErrorMessage = $"Không tìm thấy kho với ID: {firstWarehouseId}"
            });
            result.ValidationResult = validationResult;
            return result;
        }

        result.WarehouseId = warehouseGuid;
        result.WarehouseName = warehouse.Name;

        var allProducts = await _productRepository.GetListAsync();
        var productByCode = allProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.IdentificationCode))
            .GroupBy(p => StringHelper.NormalizeAndTrim(p.IdentificationCode!).ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var existingItemsQueryable = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var receiptQueryable = await _warehouseImportReceiptRepository.GetQueryableAsync();
        var existingLots = (from item in existingItemsQueryable
                            join receipt in receiptQueryable
                                on item.WarehouseImportReceiptId equals receipt.Id
                            where receipt.Status == WarehouseImportReceiptStatus.Completed
                                  && (item.StorageWarehouseId == warehouseGuid || receipt.WarehouseId == warehouseGuid)
                            select new { item.ProductId, item.LotNumber, item.Id, item.ReceivedBaseQuantity })
                           .ToList();
        var existingLotDict = existingLots
            .Where(x => !string.IsNullOrWhiteSpace(x.LotNumber))
            .GroupBy(x => $"{x.ProductId}|{x.LotNumber!.Trim().ToLowerInvariant()}")
            .ToDictionary(g => g.Key, g => g.First());

        var skippedCount = 0;
        var newCount = 0;
        var updateCount = 0;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 2;

            var item = new ProductImportPreviewItem
            {
                RowNumber = rowNumber,
                IdentificationCode = row.IdentificationCode?.Trim() ?? "",
                ProductName = row.ProductName?.Trim() ?? "",
                ProductTypeName = row.ProductTypeName?.Trim(),
                LotNumber = row.LotNumber?.Trim(),
                Quantity = row.Quantity ?? 0,
                StorageUnitName = row.StorageUnitName?.Trim(),
                ConsumedStorageQty = row.ConsumedStorageQty,
                EndingStorageQty = row.EndingStorageQty,
                ExpiryDate = row.ExpiryDate,
                ReceivedDate = row.ReceivedDate,
                ManufacturerName = row.ManufacturerName?.Trim(),
                Notes = row.Notes?.Trim(),
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            if (string.IsNullOrWhiteSpace(row.IdentificationCode))
            {
                item.IsValid = false;
                var error = "Mã nhận dạng không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber, ColumnName = "Mã nhận dạng", ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (string.IsNullOrWhiteSpace(row.LotNumber))
            {
                item.IsValid = false;
                var error = "Số lô không được để trống";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber, ColumnName = "Code/ Lot", ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (row.Quantity.HasValue && row.Quantity.Value < 0)
            {
                item.IsValid = false;
                var error = "Số lượng không được âm";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber, ColumnName = "Số lượng đầu kỳ", ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (row.ConsumedStorageQty.HasValue && row.Quantity.HasValue
                && row.ConsumedStorageQty.Value > row.Quantity.Value)
            {
                item.IsValid = false;
                var error = "Số lượng tiêu hao không được lớn hơn số lượng đầu kỳ";
                rowErrorMessages.Add(error);
                validationResult.Errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber, ColumnName = "Số lượng tiêu hao", ErrorMessage = error
                });
                AddRowError(rowErrors, rowNumber, error);
            }

            if (item.IsValid && !string.IsNullOrWhiteSpace(row.IdentificationCode))
            {
                var normalizedCode = StringHelper.NormalizeAndTrim(row.IdentificationCode).ToLowerInvariant();
                if (productByCode.TryGetValue(normalizedCode, out var product))
                {
                    item.ExistingProductId = product.Id;
                    item.ProductName = string.IsNullOrWhiteSpace(item.ProductName) ? product.Name : item.ProductName;

                    var lotKey = $"{product.Id}|{row.LotNumber!.Trim().ToLowerInvariant()}";
                    if (existingLotDict.ContainsKey(lotKey))
                    {
                        item.Status = ProductImportItemStatus.Update;
                        updateCount++;
                    }
                    else
                    {
                        item.Status = ProductImportItemStatus.New;
                        newCount++;
                    }
                }
                else
                {
                    item.Status = ProductImportItemStatus.Skipped;
                    item.SkipReason = "Mã nhận dạng không tồn tại trong master data";
                    item.IsValid = false;
                    skippedCount++;
                }
            }

            item.Errors = rowErrorMessages;
            result.Items.Add(item);

            if (item.IsValid)
                validationResult.ValidRows++;
        }

        result.ValidationResult = validationResult;
        result.TotalRows = rows.Count;
        result.ValidRows = validationResult.ValidRows;
        result.InvalidRows = rows.Count - validationResult.ValidRows - skippedCount;
        result.SkippedRows = skippedCount;
        result.NewItems = newCount;
        result.UpdateItems = updateCount;

        if (validationResult.Errors.Count > 0)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateProductErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Danh_sach_vat_tu_Loi.xlsx";
        }

        return result;
    }

    private List<ProductImportRow> ReadProductExcelRows(MemoryStream memoryStream)
    {
        var rows = new List<ProductImportRow>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);
        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var identificationCode = row.Cell(4).GetString()?.Trim();
            var productName = row.Cell(5).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(identificationCode) && string.IsNullOrWhiteSpace(productName))
                continue;

            DateTime? expiryDate = null;
            try
            {
                if (row.Cell(9).TryGetValue<DateTime>(out var ed))
                    expiryDate = ed;
            }
            catch { /* ignore parse errors */ }

            DateTime? receivedDate = null;
            try
            {
                if (row.Cell(10).TryGetValue<DateTime>(out var rd))
                    receivedDate = rd;
            }
            catch { /* ignore parse errors */ }

            decimal? quantity = TryReadDecimalCell(row, 6);            // F(6): Số lượng đầu kỳ
            decimal? consumedStorageQty = TryReadDecimalCell(row, 8);  // H(8): Sô lượng tiêu hao
            decimal? endingStorageQty = TryReadDecimalCell(row, 11);   // K(11): Tồn kho cuối kỳ

            rows.Add(new ProductImportRow
            {
                WarehouseId = row.Cell(1).GetString()?.Trim(),        // A(1)
                WarehouseName = row.Cell(2).GetString()?.Trim(),      // B(2)
                ProductTypeName = row.Cell(3).GetString()?.Trim(),    // C(3)
                IdentificationCode = identificationCode,               // D(4)
                ProductName = productName,                             // E(5)
                Quantity = quantity,                                    // F(6)
                StorageUnitName = row.Cell(7).GetString()?.Trim(),    // G(7)
                ConsumedStorageQty = consumedStorageQty,               // H(8)
                ExpiryDate = expiryDate,                               // I(9)
                ReceivedDate = receivedDate,                           // J(10)
                EndingStorageQty = endingStorageQty,                   // K(11)
                LotNumber = row.Cell(12).GetString()?.Trim(),         // L(12)
                ManufacturerName = row.Cell(13).GetString()?.Trim(),  // M(13)
                Notes = row.Cell(14).GetString()?.Trim(),             // N(14)
            });
        }

        return rows;
    }

    public async Task<ImportResultDto> ImportProductsAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewProductsInternalAsync(memoryStream);

        if (preview.ValidationResult.Errors.Count > 0 && preview.ValidRows == 0)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var newCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        try
        {
            var warehouseId = preview.WarehouseId!.Value;
            var warehouse = await _warehouseRepository.GetAsync(warehouseId);

            var allProducts = await _productRepository.GetListAsync();
            var productByCode = allProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.IdentificationCode))
                .GroupBy(p => StringHelper.NormalizeAndTrim(p.IdentificationCode!).ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var existingItemsQueryable = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
            var receiptQueryable = await _warehouseImportReceiptRepository.GetQueryableAsync();
            var existingLots = (from item in existingItemsQueryable
                                join r in receiptQueryable
                                    on item.WarehouseImportReceiptId equals r.Id
                                where r.Status == WarehouseImportReceiptStatus.Completed
                                      && (item.StorageWarehouseId == warehouseId || r.WarehouseId == warehouseId)
                                select item)
                               .ToList();
            var existingLotDict = existingLots
                .Where(x => !string.IsNullOrWhiteSpace(x.LotNumber))
                .GroupBy(x => $"{x.ProductId}|{x.LotNumber!.Trim().ToLowerInvariant()}")
                .ToDictionary(g => g.Key, g => g.First());

            var receiptCode = await GenerateImportReceiptCodeAsync();
            var receipt = new WarehouseImportReceipt(
                GuidGenerator.Create(),
                receiptCode,
                WarehouseImportReceiptType.DataImport)
            {
                WarehouseId = warehouseId,
                WarehouseName = warehouse.Name,
                ActualImportDate = DateTime.Now,
            };

            var validItems = preview.Items
                .Where(i => i.IsValid && i.Status != ProductImportItemStatus.Skipped)
                .ToList();

            var newReceiptItems = new List<WarehouseImportReceiptItem>();

            foreach (var previewItem in validItems)
            {
                var normalizedCode = StringHelper.NormalizeAndTrim(previewItem.IdentificationCode).ToLowerInvariant();
                if (!productByCode.TryGetValue(normalizedCode, out var product))
                {
                    skippedCount++;
                    continue;
                }

                var baseQty = previewItem.Quantity;
                var consumedQty = previewItem.ConsumedStorageQty ?? 0;
                var lotKey = $"{product.Id}|{previewItem.LotNumber!.Trim().ToLowerInvariant()}";

                if (previewItem.Status == ProductImportItemStatus.Update
                    && existingLotDict.TryGetValue(lotKey, out var existingItem))
                {
                    var oldNet = existingItem.ReceivedBaseQuantity - existingItem.ConsumedBaseQuantity;
                    var newNet = baseQty - consumedQty;
                    var delta = newNet - oldNet;
                    existingItem.ReceivedBaseQuantity = baseQty;
                    existingItem.ConsumedBaseQuantity = consumedQty;
                    if (previewItem.ExpiryDate.HasValue)
                        existingItem.LotExpiryDate = previewItem.ExpiryDate;
                    existingItem.ProductName = product.Name;
                    existingItem.ManufacturerName = previewItem.ManufacturerName;
                    existingItem.UnitName = previewItem.StorageUnitName;
                    existingItem.Notes = previewItem.Notes;

                    product.AvailableQuantity += delta;
                    await _productRepository.UpdateAsync(product, autoSave: true);
                    await _warehouseImportReceiptItemRepository.UpdateAsync(existingItem, autoSave: true);

                    if (delta > 0)
                    {
                        var existingBarcodeCount = await _inventoryBarcodeRepository
                            .CountAsync(x => x.OriginWarehouseImportReceiptItemId == existingItem.Id);
                        var newBarcodeCount = Math.Max(0, (int)Math.Ceiling(baseQty) - (int)existingBarcodeCount);
                        var startSeq = (int)existingBarcodeCount + 1;
                        var takenCodes = await LoadBarcodeCodesWithPrefixAsync(
                            product.IdentificationCode,
                            receipt.ActualImportDate,
                            previewItem.LotNumber);
                        var barcodesToInsert = new List<InventoryBarcode>(newBarcodeCount);
                        for (var seq = startSeq; seq < startSeq + newBarcodeCount; seq++)
                        {
                            var code = BuildProductBarcodeCode(
                                product.IdentificationCode,
                                receipt.ActualImportDate,
                                previewItem.LotNumber,
                                seq);
                            var uniqueCode = code;
                            var suffix = 1;
                            while (takenCodes.Contains(uniqueCode))
                                uniqueCode = $"{code}-{suffix++:D2}";

                            takenCodes.Add(uniqueCode);
                            barcodesToInsert.Add(new InventoryBarcode(
                                GuidGenerator.Create(),
                                uniqueCode,
                                product.Id,
                                existingItem.Id,
                                warehouseId));
                        }
                        await _inventoryBarcodeRepository.BulkInsertAsync(barcodesToInsert);
                    }

                    updatedCount++;
                }
                else
                {
                    var receiptItem = new WarehouseImportReceiptItem(
                        GuidGenerator.Create(),
                        receipt.Id,
                        product.Id,
                        product.Name)
                    {
                        LotNumber = previewItem.LotNumber,
                        LotExpiryDate = previewItem.ExpiryDate,
                        ActualReceivedQuantity = baseQty,
                        ReceivedBaseQuantity = baseQty,
                        ConsumedBaseQuantity = consumedQty,
                        OrderedQuantity = baseQty,
                        UnitName = previewItem.StorageUnitName,
                        StorageWarehouseId = warehouseId,
                        StorageWarehouseName = warehouse.Name,
                        ManufacturerName = previewItem.ManufacturerName,
                        Notes = previewItem.Notes,
                        Status = WarehouseImportLineStatus.Checked,
                    };

                    newReceiptItems.Add(receiptItem);

                    product.AvailableQuantity += (baseQty - consumedQty);
                    await _productRepository.UpdateAsync(product, autoSave: true);

                    newCount++;
                }
            }

            if (newReceiptItems.Count > 0)
            {
                await _warehouseImportReceiptRepository.InsertAsync(receipt, autoSave: true);

                foreach (var receiptItem in newReceiptItems)
                {
                    await _warehouseImportReceiptItemRepository.InsertAsync(receiptItem, autoSave: true);

                    var product = productByCode.Values.FirstOrDefault(p => p.Id == receiptItem.ProductId);
                    var barcodeCount = Math.Max(1, (int)Math.Ceiling(receiptItem.ReceivedBaseQuantity));
                    var barcodeCodes = new List<string>();
                    var takenCodes = await LoadBarcodeCodesWithPrefixAsync(
                        product?.IdentificationCode,
                        receipt.ActualImportDate,
                        receiptItem.LotNumber);
                    var barcodesToInsert = new List<InventoryBarcode>(barcodeCount);
                    for (var seq = 1; seq <= barcodeCount; seq++)
                    {
                        var code = BuildProductBarcodeCode(
                            product?.IdentificationCode,
                            receipt.ActualImportDate,
                            receiptItem.LotNumber,
                            seq);
                        var uniqueCode = code;
                        var suffix = 1;
                        while (takenCodes.Contains(uniqueCode))
                            uniqueCode = $"{code}-{suffix++:D2}";

                        takenCodes.Add(uniqueCode);
                        barcodesToInsert.Add(new InventoryBarcode(
                            GuidGenerator.Create(),
                            uniqueCode,
                            receiptItem.ProductId,
                            receiptItem.Id,
                            warehouseId));
                        barcodeCodes.Add(uniqueCode);
                    }
                    await _inventoryBarcodeRepository.BulkInsertAsync(barcodesToInsert);
                    receiptItem.BarcodeValues = JsonSerializer.Serialize(barcodeCodes);
                    await _warehouseImportReceiptItemRepository.UpdateAsync(receiptItem, autoSave: true);
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = newCount + updatedCount,
                SkippedRows = skippedCount + preview.SkippedRows,
                Message = $"Import thành công: {newCount} mới, {updatedCount} cập nhật, {skippedCount + preview.SkippedRows} bỏ qua."
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

    private byte[] GenerateProductErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var worksheet = workbook.Worksheet(1);

        var errorColumnIndex = 16; // Column P (first empty after O)
        var headerRow = worksheet.Row(1);
        headerRow.Cell(errorColumnIndex).Value = "Lỗi";
        headerRow.Cell(errorColumnIndex).Style.Font.Bold = true;
        headerRow.Cell(errorColumnIndex).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int dataRowNumber = 2;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var identificationCode = row.Cell(4).GetString()?.Trim();
            var productName = row.Cell(5).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(identificationCode) && string.IsNullOrWhiteSpace(productName))
                continue;

            if (rowErrors.ContainsKey(dataRowNumber))
            {
                var errorCell = row.Cell(errorColumnIndex);
                errorCell.Value = string.Join("; ", rowErrors[dataRowNumber]);
                errorCell.Style.Font.FontColor = XLColor.Red;
            }

            dataRowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Every code BuildProductBarcodeCode can return for this lot (with or without a -NN suffix)
    /// starts with this prefix, so checking this set is the same as checking the table.
    /// </summary>
    private async Task<HashSet<string>> LoadBarcodeCodesWithPrefixAsync(
        string? identificationCode,
        DateTime importDate,
        string? lotNumber)
    {
        var codePrefix = InventoryBarcodeCodeBuilder.BuildPrefix(identificationCode, importDate, lotNumber);
        var query = await _inventoryBarcodeRepository.GetQueryableAsync();
        return (await AsyncExecuter.ToListAsync(query
                .Where(x => x.Code.StartsWith(codePrefix))
                .Select(x => x.Code)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string BuildProductBarcodeCode(
        string? identificationCode,
        DateTime importDate,
        string? lotNumber,
        int sequence)
    {
        var parts = new[]
        {
            identificationCode,
            importDate.ToString("ddMMyyyy"),
            lotNumber,
            sequence.ToString("D4")
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var normalized = string.Concat(parts).Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(c => c >= 32 && c <= 126)
            .ToArray());
    }

    private async Task<string> GenerateImportReceiptCodeAsync()
    {
        const string prefix = "DI";
        const string dateFormat = "ddMMyyyy";
        const int sequencePadding = 2;
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix(prefix, dateFormat);
            var query = await _warehouseImportReceiptRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate(prefix, count, dateFormat, sequencePadding);
        }
    }

    private static decimal? TryReadDecimalCell(IXLRow row, int col)
    {
        try
        {
            if (row.Cell(col).TryGetValue<double>(out var d))
                return (decimal)d;
            if (row.Cell(col).TryGetValue<decimal>(out var dm))
                return dm;
        }
        catch { /* ignore parse errors */ }
        return null;
    }

    private static string GetProductTypeLabel(Products.ProductType productType)
    {
        return productType switch
        {
            Products.ProductType.Chemical => "Hóa chất",
            Products.ProductType.Biological => "Sinh phẩm",
            Products.ProductType.Supply => "Vật tư tiêu hao",
            Products.ProductType.Standard => "Chất chuẩn",
            Products.ProductType.Solvent => "Dung môi",
            Products.ProductType.Equipment => "Thiết bị phòng Lab",
            Products.ProductType.PreparedSolution => "Dung dịch pha",
            _ => ""
        };
    }

    private static Products.ProductType? ParseProductTypeLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;
        return label.Trim() switch
        {
            "Hóa chất" => Products.ProductType.Chemical,
            "Sinh phẩm" => Products.ProductType.Biological,
            "Vật tư tiêu hao" => Products.ProductType.Supply,
            "Chất chuẩn" => Products.ProductType.Standard,
            "Dung môi" => Products.ProductType.Solvent,
            "Thiết bị phòng Lab" => Products.ProductType.Equipment,
            "Dung dịch pha" => Products.ProductType.PreparedSolution,
            _ => null
        };
    }

    #endregion

    #region Product Master Data Import/Export (Admin)

    public async Task<byte[]> ExportProductMasterDataAsync()
    {
        var templateBytes = ReadStaticTemplate("Vật tư.xlsx");
        using var templateStream = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(templateStream);
        var ws = workbook.Worksheet(1);

        var products = (await _productRepository.GetQueryableAsync())
            .Where(p => !p.IsDraft)
            .OrderBy(p => p.ProductType)
            .ThenBy(p => p.Name)
            .ToList();

        var skuQuery = await _productSkuRepository.GetQueryableAsync();
        var allSkus = skuQuery
            .Where(s => products.Select(p => p.Id).Contains(s.ProductId))
            .ToList();
        var skusByProduct = allSkus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var units = (await _unitRepository.GetQueryableAsync()).ToList();
        var unitNameMap = units.ToDictionary(u => u.Id, u => u.Name);

        var manufacturers = (await _manufacturerRepository.GetQueryableAsync()).ToList();
        var mfgNameMap = manufacturers.ToDictionary(m => m.Id, m => m.Name);

        var productNameMap = products.ToDictionary(p => p.Id, p => p.Name);

        var maxSkuCount = skusByProduct.Values.Any()
            ? skusByProduct.Values.Max(s => s.Count)
            : 1;
        if (maxSkuCount < 1) maxSkuCount = 1;

        var productTypeLabels = new[]
        {
            "Hóa chất", "Sinh phẩm", "Vật tư tiêu hao", "Chất chuẩn",
            "Dung môi", "Thiết bị phòng Lab", "Dung dịch pha"
        };
        var unitNames = units.Select(u => u.Name).Distinct().OrderBy(n => n).ToList();
        var mfgNames = manufacturers.Select(m => m.Name).Distinct().OrderBy(n => n).ToList();

        var validationSheet = workbook.AddWorksheet("_Validation");
        for (int i = 0; i < productTypeLabels.Length; i++)
            validationSheet.Cell(i + 1, 1).Value = productTypeLabels[i];
        for (int i = 0; i < unitNames.Count; i++)
            validationSheet.Cell(i + 1, 2).Value = unitNames[i];
        for (int i = 0; i < mfgNames.Count; i++)
            validationSheet.Cell(i + 1, 3).Value = mfgNames[i];
        validationSheet.Cell(1, 4).Value = "Y";
        validationSheet.Cell(2, 4).Value = "N";
        validationSheet.Hide();

        // Ensure headers for additional SKU groups exist
        for (int g = 2; g <= maxSkuCount; g++)
        {
            var baseCol = 26 + (g - 1) * 5; // Z=26 for group 1, then +5 per group
            ws.Cell(1, baseCol).Value = $"Hãng sản xuất {g}";
            ws.Cell(1, baseCol + 1).Value = $"đơn vị mua hàng {g}";
            ws.Cell(1, baseCol + 2).Value = $"Quy cách đóng gói {g}";
            ws.Cell(1, baseCol + 3).Value = $"đơn vi lưu kho {g}";
            ws.Cell(1, baseCol + 4).Value = $"Dung tích mỗi đơn vị {g}";
        }

        // Data validation dropdowns
        var maxRow = 1048576;
        var ptRange = $"'_Validation'!$A$1:$A${productTypeLabels.Length}";
        var unitRange = $"'_Validation'!$B$1:$B${Math.Max(1, unitNames.Count)}";
        var mfgRange = $"'_Validation'!$C$1:$C${Math.Max(1, mfgNames.Count)}";
        var ynRange = "'_Validation'!$D$1:$D$2";

        ws.Range(2, 1, maxRow, 1).CreateDataValidation().List(ptRange, true);   // Col A: Loại vật tư
        ws.Range(2, 3, maxRow, 3).CreateDataValidation().List(unitRange, true);  // Col C: Đơn vị tiêu hao
        ws.Range(2, 15, maxRow, 15).CreateDataValidation().List(ynRange, true); // Col O: Trạng thái vô trùng
        ws.Range(2, 16, maxRow, 16).CreateDataValidation().List(ynRange, true);  // Col P: Khả năng tái sử dụng

        for (int g = 1; g <= maxSkuCount; g++)
        {
            var baseCol = 26 + (g - 1) * 5;
            ws.Range(2, baseCol, maxRow, baseCol).CreateDataValidation().List(mfgRange, true);       // Hãng SX
            ws.Range(2, baseCol + 1, maxRow, baseCol + 1).CreateDataValidation().List(unitRange, true); // ĐV mua hàng
            ws.Range(2, baseCol + 3, maxRow, baseCol + 3).CreateDataValidation().List(unitRange, true); // ĐV lưu kho
        }

        // Fill data
        var row = 2;
        foreach (var product in products)
        {
            ws.Cell(row, 1).Value = GetProductTypeLabel(product.ProductType);   // A
            ws.Cell(row, 2).Value = product.Name ?? "";                         // B
            ws.Cell(row, 3).Value = product.VolumeUnitId.HasValue               // C
                ? unitNameMap.GetValueOrDefault(product.VolumeUnitId.Value, "") : "";
            ws.Cell(row, 4).Value = product.IdentificationCode ?? "";           // D
            if (product.MinStockLevel.HasValue)
                ws.Cell(row, 5).Value = (double)product.MinStockLevel.Value;    // E

            // F: Cấu hình kỹ thuật — Equipment uses MainTechnicalSpecs, others use FormattedConcentration/Concentration
            if (product.ProductType == Products.ProductType.Equipment)
                ws.Cell(row, 6).Value = product.MainTechnicalSpecs ?? "";
            else
                WriteHtmlToCell(ws.Cell(row, 6), product.FormattedConcentration ?? product.Concentration);

            ws.Cell(row, 7).Value = product.Categories ?? "";                   // G

            WriteHtmlToCell(ws.Cell(row, 8), product.FormattedStorageConditions ?? product.StorageConditions);  // H

            // I: Công thức hóa học — Chemical uses plain ChemicalFormula, Standard/Solvent/PreparedSolution use FormattedChemicalFormula
            if (product.ProductType == Products.ProductType.Chemical)
                ws.Cell(row, 9).Value = product.ChemicalFormula ?? "";
            else
                WriteHtmlToCell(ws.Cell(row, 9), product.FormattedChemicalFormula ?? product.ChemicalFormula);
            ws.Cell(row, 10).Value = product.CASNumber ?? "";                   // J
            WriteHtmlToCell(ws.Cell(row, 11), product.FormattedOrigin ?? product.Origin);                       // K
            WriteHtmlToCell(ws.Cell(row, 12), product.GhsCode ?? "");           // L
            WriteHtmlToCell(ws.Cell(row, 13), product.FormattedPackagingSpec ?? product.PackagingSpec);         // M
            WriteHtmlToCell(ws.Cell(row, 14), product.FormattedMaterialComposition ?? product.MaterialComposition); // N
            ws.Cell(row, 15).Value = product.IsSterile.HasValue                 // O
                ? (product.IsSterile.Value ? "Y" : "N") : "";
            ws.Cell(row, 16).Value = product.IsReusable.HasValue                // P
                ? (product.IsReusable.Value ? "Y" : "N") : "";
            if (product.MaxReuseCount.HasValue)
                ws.Cell(row, 17).Value = product.MaxReuseCount.Value;           // Q

            WriteHtmlToCell(ws.Cell(row, 18), product.FormattedCertifiedValue ?? product.CertifiedValue);                         // R
            WriteHtmlToCell(ws.Cell(row, 19), product.FormattedMeasurementUncertainty ?? product.MeasurementUncertainty);         // S
            ws.Cell(row, 20).Value = product.StandardLink ?? "";                // T
            ws.Cell(row, 21).Value = product.AnalysisGrade ?? "";               // U
            ws.Cell(row, 22).Value = product.UsagePurpose ?? "";                // V
            if (product.ShelfLifeDays.HasValue)
                ws.Cell(row, 23).Value = product.ShelfLifeDays.Value;           // W

            if (product.BaseProductId.HasValue)                                  // X, Y
            {
                ws.Cell(row, 24).Value = productNameMap.GetValueOrDefault(product.BaseProductId.Value, "");
                ws.Cell(row, 25).Value = product.BaseProductId.Value.ToString();
            }

            // SKU groups
            if (skusByProduct.TryGetValue(product.Id, out var skus))
            {
                for (int i = 0; i < skus.Count; i++)
                {
                    var sku = skus[i];
                    var baseCol = 26 + i * 5;
                    ws.Cell(row, baseCol).Value = mfgNameMap.GetValueOrDefault(sku.ManufacturerId, "");
                    ws.Cell(row, baseCol + 1).Value = sku.BaseUnitId.HasValue
                        ? unitNameMap.GetValueOrDefault(sku.BaseUnitId.Value, "") : "";
                    if (sku.PackagingQuantity.HasValue)
                        ws.Cell(row, baseCol + 2).Value = sku.PackagingQuantity.Value;
                    ws.Cell(row, baseCol + 3).Value = sku.PackagingUnitId.HasValue
                        ? unitNameMap.GetValueOrDefault(sku.PackagingUnitId.Value, "") : "";
                    if (sku.VolumePerUnit.HasValue)
                        ws.Cell(row, baseCol + 4).Value = sku.VolumePerUnit.Value;
                }
            }

            row++;
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    public async Task<ProductMasterDataImportPreviewDto> PreviewProductMasterDataAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        return await PreviewProductMasterDataInternalAsync(memoryStream);
    }

    private async Task<ProductMasterDataImportPreviewDto> PreviewProductMasterDataInternalAsync(MemoryStream memoryStream)
    {
        using var workbook = new XLWorkbook(memoryStream);
        var ws = workbook.Worksheet(1);
        var lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 1;

        var result = new ProductMasterDataImportPreviewDto();
        var validationResult = new ImportValidationResult();
        var rowErrors = new Dictionary<int, List<string>>();

        // Detect SKU group count from headers
        var skuGroupCount = 0;
        for (int col = 26; col <= ws.LastColumnUsed()?.ColumnNumber(); col += 5)
        {
            var header = ws.Cell(1, col).GetString()?.Trim() ?? "";
            if (header.StartsWith("Hãng sản xuất", StringComparison.OrdinalIgnoreCase))
                skuGroupCount++;
            else
                break;
        }
        if (skuGroupCount == 0) skuGroupCount = 1;

        // Pre-load lookup data
        var allProducts = (await _productRepository.GetQueryableAsync()).ToList();
        var productByCode = allProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.IdentificationCode))
            .GroupBy(p => p.IdentificationCode!.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var productById = allProducts.ToDictionary(p => p.Id);

        var units = (await _unitRepository.GetQueryableAsync()).ToList();
        var unitByName = units
            .GroupBy(u => u.Name.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var unitByCategoryAndName = units
            .GroupBy(u => (u.Category, u.Name.Trim().ToLowerInvariant()))
            .ToDictionary(g => g.Key, g => g.First());
        var unitNameMap = units.ToDictionary(u => u.Id, u => u.Name);

        var manufacturers = (await _manufacturerRepository.GetQueryableAsync()).ToList();
        var mfgByName = manufacturers
            .GroupBy(m => m.Name.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        var mfgNameMap = manufacturers.ToDictionary(m => m.Id, m => m.Name);

        var allSkus = (await _productSkuRepository.GetQueryableAsync())
            .Where(s => allProducts.Select(p => p.Id).Contains(s.ProductId))
            .ToList();
        var skusByProduct = allSkus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var newCount = 0;
        var updateCount = 0;
        var skippedCount = 0;

        for (int rowNum = 2; rowNum <= lastRowUsed; rowNum++)
        {
            var rowObj = ws.Row(rowNum);
            if (rowObj.IsEmpty()) continue;

            var productTypeName = ws.Cell(rowNum, 1).GetString()?.Trim();  // A
            var name = ws.Cell(rowNum, 2).GetString()?.Trim();              // B
            var identificationCode = ws.Cell(rowNum, 4).GetString()?.Trim(); // D

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(identificationCode))
                continue;

            var item = new ProductMasterDataImportPreviewItem
            {
                RowNumber = rowNum,
                IdentificationCode = identificationCode ?? "",
                ProductName = name ?? "",
                ProductTypeName = productTypeName,
                IsValid = true
            };

            var rowErrorMessages = new List<string>();

            void AddError(string colName, string msg)
            {
                item.IsValid = false;
                rowErrorMessages.Add(msg);
                validationResult.Errors.Add(new ImportRowError { RowNumber = rowNum, ColumnName = colName, ErrorMessage = msg });
                AddRowError(rowErrors, rowNum, msg);
            }

            // Validate ProductType (required)
            var productType = ParseProductTypeLabel(productTypeName);
            if (productType == null)
                AddError("Loại vật tư", $"Loại vật tư không hợp lệ: '{productTypeName}'");

            var isEquipment = productType == Products.ProductType.Equipment;

            // Validate Đơn vị tiêu hao (lookup — only if provided)
            var volumeUnitName = ws.Cell(rowNum, 3).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(volumeUnitName)
                && !unitByCategoryAndName.ContainsKey((UnitCategory.ConsumableUnit, volumeUnitName.ToLowerInvariant()))
                && !unitByName.ContainsKey(volumeUnitName.ToLowerInvariant()))
                AddError("Đơn vị tiêu hao", $"Đơn vị tiêu hao không tồn tại: '{volumeUnitName}'");

            // Validate MinStockLevel — must be >= 0 if provided
            var minStockStr = ws.Cell(rowNum, 5).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(minStockStr))
            {
                if (!decimal.TryParse(minStockStr, out var minStockVal))
                    AddError("Tồn kho tối thiểu", "Tồn kho tối thiểu phải là số");
                else if (minStockVal < 0)
                    AddError("Tồn kho tối thiểu", "Tồn kho tối thiểu không được âm");
            }

            // Validate IsSterile (Y/N)
            var isSterileValidateStr = ws.Cell(rowNum, 15).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(isSterileValidateStr)
                && !isSterileValidateStr.Equals("Y", StringComparison.OrdinalIgnoreCase)
                && !isSterileValidateStr.Equals("N", StringComparison.OrdinalIgnoreCase))
                AddError("Trạng thái vô trùng", "Trạng thái vô trùng phải là Y hoặc N");

            // Validate IsReusable (Y/N)
            var isReusableStr = ws.Cell(rowNum, 16).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(isReusableStr)
                && !isReusableStr.Equals("Y", StringComparison.OrdinalIgnoreCase)
                && !isReusableStr.Equals("N", StringComparison.OrdinalIgnoreCase))
                AddError("Khả năng tái sử dụng", "Khả năng tái sử dụng phải là Y hoặc N");

            // Validate MaxReuseCount (integer, min 1)
            var maxReuseStr = ws.Cell(rowNum, 17).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(maxReuseStr))
            {
                if (!int.TryParse(maxReuseStr, out var maxReuseVal))
                    AddError("Số lần tái sử dụng", "Số lần tái sử dụng phải là số nguyên");
                else if (maxReuseVal < 1)
                    AddError("Số lần tái sử dụng", "Số lần tái sử dụng tối thiểu 1");
            }

            // Validate ShelfLifeDays (integer, min 1)
            var shelfLifeStr = ws.Cell(rowNum, 23).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(shelfLifeStr))
            {
                if (!int.TryParse(shelfLifeStr, out var shelfLifeVal))
                    AddError("Hạn sử dụng", "Hạn sử dụng phải là số nguyên");
                else if (shelfLifeVal < 1)
                    AddError("Hạn sử dụng", "Hạn sử dụng tối thiểu 1 ngày");
            }

            // Validate BaseProductId
            var baseProductIdStr = ws.Cell(rowNum, 25).GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(baseProductIdStr))
            {
                if (!Guid.TryParse(baseProductIdStr, out var baseProductGuid)
                    || !productById.ContainsKey(baseProductGuid))
                    AddError("ID chất nền", "Vật tư/ Hóa chất không tồn tại");
            }

            // Validate SKU groups
            var skuMfgIds = new HashSet<string>();
            for (int g = 0; g < skuGroupCount; g++)
            {
                var baseCol = 26 + g * 5;
                var mfgName = ws.Cell(rowNum, baseCol).GetString()?.Trim();
                var buyUnitName = ws.Cell(rowNum, baseCol + 1).GetString()?.Trim();
                var pkgQtyStr = ws.Cell(rowNum, baseCol + 2).GetString()?.Trim();
                var storageUnitName = ws.Cell(rowNum, baseCol + 3).GetString()?.Trim();
                var vpuStr = ws.Cell(rowNum, baseCol + 4).GetString()?.Trim();

                var hasAnyValue = !string.IsNullOrWhiteSpace(mfgName)
                    || !string.IsNullOrWhiteSpace(buyUnitName)
                    || !string.IsNullOrWhiteSpace(pkgQtyStr)
                    || !string.IsNullOrWhiteSpace(storageUnitName)
                    || !string.IsNullOrWhiteSpace(vpuStr);
                if (!hasAnyValue) continue;

                var groupSuffix = g + 1;

                // Manufacturer required + lookup
                if (string.IsNullOrWhiteSpace(mfgName))
                {
                    AddError($"Hãng sản xuất {groupSuffix}", "Vui lòng chọn hãng sản xuất");
                }
                else if (!mfgByName.ContainsKey(mfgName.ToLowerInvariant()))
                {
                    AddError($"Hãng sản xuất {groupSuffix}", $"Hãng sản xuất không tồn tại: '{mfgName}'");
                }
                else
                {
                    var mfgKey = mfgName.ToLowerInvariant();
                    if (!skuMfgIds.Add(mfgKey))
                        AddError($"Hãng sản xuất {groupSuffix}", "Mỗi hãng sản xuất chỉ được chọn một lần cho mỗi sản phẩm");
                }

                // Buy unit lookup (optional but must exist if provided)
                if (!string.IsNullOrWhiteSpace(buyUnitName)
                    && !unitByCategoryAndName.ContainsKey((UnitCategory.PurchaseUnit, buyUnitName.ToLowerInvariant()))
                    && !unitByName.ContainsKey(buyUnitName.ToLowerInvariant()))
                    AddError($"Đơn vị mua hàng {groupSuffix}", $"Đơn vị mua hàng không tồn tại: '{buyUnitName}'");

                // PackagingQuantity — required, integer, min 1
                if (string.IsNullOrWhiteSpace(pkgQtyStr))
                {
                    AddError($"Quy cách đóng gói {groupSuffix}", "Vui lòng nhập số lượng");
                }
                else if (!int.TryParse(pkgQtyStr, out var pkgQtyVal))
                {
                    AddError($"Quy cách đóng gói {groupSuffix}", "Quy cách đóng gói phải là số nguyên");
                }
                else if (pkgQtyVal < 1)
                {
                    AddError($"Quy cách đóng gói {groupSuffix}", "Quy cách đóng gói tối thiểu 1");
                }

                // Storage unit — required + lookup
                if (string.IsNullOrWhiteSpace(storageUnitName))
                {
                    AddError($"Đơn vị lưu kho {groupSuffix}", "Vui lòng chọn đơn vị lưu kho");
                }
                else if (!unitByCategoryAndName.ContainsKey((UnitCategory.StorageUnit, storageUnitName.ToLowerInvariant()))
                    && !unitByName.ContainsKey(storageUnitName.ToLowerInvariant()))
                {
                    AddError($"Đơn vị lưu kho {groupSuffix}", $"Đơn vị lưu kho không tồn tại: '{storageUnitName}'");
                }

                // VolumePerUnit — required, integer, min 1
                if (string.IsNullOrWhiteSpace(vpuStr))
                {
                    AddError($"Dung tích mỗi đơn vị {groupSuffix}", "Vui lòng nhập dung tích");
                }
                else if (!int.TryParse(vpuStr, out var vpuVal))
                {
                    AddError($"Dung tích mỗi đơn vị {groupSuffix}", "Dung tích phải là số nguyên");
                }
                else if (vpuVal < 1)
                {
                    AddError($"Dung tích mỗi đơn vị {groupSuffix}", "Dung tích tối thiểu 1");
                }
            }

            // Determine status: New or Update
            if (item.IsValid && !string.IsNullOrWhiteSpace(identificationCode))
            {
                // Match by IdentificationCode (primary key for upsert)
                var normalizedCode = identificationCode.Trim().ToLowerInvariant();
                if (productByCode.TryGetValue(normalizedCode, out var existingProduct))
                {
                    item.ExistingProductId = existingProduct.Id;
                    item.Status = ProductMasterDataImportItemStatus.Update;
                    updateCount++;
                }
                else
                {
                    item.Status = ProductMasterDataImportItemStatus.New;
                    newCount++;
                }
            }
            else if (item.IsValid && string.IsNullOrWhiteSpace(identificationCode)
                && productType.HasValue && !string.IsNullOrWhiteSpace(name))
            {
                // No identificationCode → fallback match by Name + ProductType
                var normalizedName = name.Trim().ToLowerInvariant();
                var matchByName = allProducts.FirstOrDefault(p =>
                    p.Name.Trim().ToLowerInvariant() == normalizedName
                    && p.ProductType == productType.Value);
                if (matchByName != null)
                {
                    item.ExistingProductId = matchByName.Id;
                    item.Status = ProductMasterDataImportItemStatus.Update;
                    updateCount++;
                }
                else
                {
                    item.Status = ProductMasterDataImportItemStatus.New;
                    newCount++;
                }
            }
            else if (item.IsValid)
            {
                item.Status = ProductMasterDataImportItemStatus.New;
                newCount++;
            }

            // For Update items: detect if anything actually changed vs existing product
            if (item.IsValid && item.Status == ProductMasterDataImportItemStatus.Update
                && item.ExistingProductId.HasValue && productById.TryGetValue(item.ExistingProductId.Value, out var existProd))
            {
                bool Eq(string? a, string? b) =>
                    string.Equals(a?.Trim() ?? "", b?.Trim() ?? "", StringComparison.Ordinal);

                // For rich text fields: Excel GetString() returns plain text (no HTML, no newlines from <p> tags).
                // DB stores both plain text (with newlines) and FormattedX (with HTML tags).
                // Normalize both sides by stripping HTML tags and collapsing whitespace.
                string NormalizePlainText(string? s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return "";
                    var text = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "");
                    text = System.Net.WebUtility.HtmlDecode(text);
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "");
                    return text;
                }

                bool EqRichText(string? excelPlainText, string? dbPlainText, string? dbFormattedHtml)
                {
                    var excelNorm = NormalizePlainText(excelPlainText);
                    var dbNorm = NormalizePlainText(dbFormattedHtml ?? dbPlainText);
                    return string.Equals(excelNorm, dbNorm, StringComparison.Ordinal);
                }

                var changed = false;

                // Basic fields
                if (!Eq(name, existProd.Name)) changed = true;
                if (productType != existProd.ProductType) changed = true;
                if (!Eq(identificationCode, existProd.IdentificationCode)) changed = true;

                // VolumeUnitId (col C)
                var exVolUnitName = existProd.VolumeUnitId.HasValue
                    ? unitNameMap.GetValueOrDefault(existProd.VolumeUnitId.Value, "") : "";
                if (!Eq(ws.Cell(rowNum, 3).GetString(), exVolUnitName)) changed = true;

                // MinStockLevel (col E)
                var importMinStock = TryReadDecimalCell(ws.Row(rowNum), 5);
                if (importMinStock != existProd.MinStockLevel) changed = true;

                // Col F: Cấu hình kỹ thuật
                var colFText = ws.Cell(rowNum, 6).GetString()?.Trim() ?? "";
                if (isEquipment)
                {
                    if (!Eq(colFText, existProd.MainTechnicalSpecs)) changed = true;
                }
                else
                {
                    if (!EqRichText(colFText, existProd.Concentration, existProd.FormattedConcentration)) changed = true;
                }

                // Col G: Categories (plain text)
                if (!Eq(ws.Cell(rowNum, 7).GetString(), existProd.Categories)) changed = true;
                // Col H: StorageConditions (rich text)
                if (!EqRichText(ws.Cell(rowNum, 8).GetString(), existProd.StorageConditions, existProd.FormattedStorageConditions)) changed = true;

                // Col I: ChemicalFormula
                var colIText = ws.Cell(rowNum, 9).GetString()?.Trim() ?? "";
                if (productType == Products.ProductType.Chemical)
                {
                    if (!Eq(colIText, existProd.ChemicalFormula)) changed = true;
                }
                else
                {
                    if (!EqRichText(colIText, existProd.ChemicalFormula, existProd.FormattedChemicalFormula)) changed = true;
                }

                if (!Eq(ws.Cell(rowNum, 10).GetString(), existProd.CASNumber)) changed = true;
                // Col K: Origin (rich text)
                if (!EqRichText(ws.Cell(rowNum, 11).GetString(), existProd.Origin, existProd.FormattedOrigin)) changed = true;
                // Col L: GhsCode (rich text — stored as HTML, no separate plain text field)
                if (!EqRichText(ws.Cell(rowNum, 12).GetString(), existProd.GhsCode, existProd.GhsCode)) changed = true;
                // Col M: PackagingSpec (rich text)
                if (!EqRichText(ws.Cell(rowNum, 13).GetString(), existProd.PackagingSpec, existProd.FormattedPackagingSpec)) changed = true;
                // Col N: MaterialComposition (rich text)
                if (!EqRichText(ws.Cell(rowNum, 14).GetString(), existProd.MaterialComposition, existProd.FormattedMaterialComposition)) changed = true;

                // IsSterile (col O)
                var isSterileStr = ws.Cell(rowNum, 15).GetString()?.Trim();
                var exIsSterileStr = existProd.IsSterile.HasValue
                    ? (existProd.IsSterile.Value ? "Y" : "N") : "";
                if (!Eq(isSterileStr, exIsSterileStr)) changed = true;

                // IsReusable (col P)
                var isReusableStr2 = ws.Cell(rowNum, 16).GetString()?.Trim();
                var exIsReusableStr = existProd.IsReusable.HasValue
                    ? (existProd.IsReusable.Value ? "Y" : "N") : "";
                if (!Eq(isReusableStr2, exIsReusableStr)) changed = true;

                // MaxReuseCount (col Q)
                var maxReuseStr2 = ws.Cell(rowNum, 17).GetString()?.Trim();
                var exMaxReuseStr = existProd.MaxReuseCount?.ToString() ?? "";
                if (!Eq(maxReuseStr2, exMaxReuseStr)) changed = true;

                // Col R: CertifiedValue (rich text)
                if (!EqRichText(ws.Cell(rowNum, 18).GetString(), existProd.CertifiedValue, existProd.FormattedCertifiedValue)) changed = true;
                // Col S: MeasurementUncertainty (rich text)
                if (!EqRichText(ws.Cell(rowNum, 19).GetString(), existProd.MeasurementUncertainty, existProd.FormattedMeasurementUncertainty)) changed = true;
                if (!Eq(ws.Cell(rowNum, 20).GetString(), existProd.StandardLink)) changed = true;
                if (!Eq(ws.Cell(rowNum, 21).GetString(), existProd.AnalysisGrade)) changed = true;
                if (!Eq(ws.Cell(rowNum, 22).GetString(), existProd.UsagePurpose)) changed = true;

                // ShelfLifeDays (col W)
                var shelfStr2 = ws.Cell(rowNum, 23).GetString()?.Trim();
                var exShelfStr = existProd.ShelfLifeDays?.ToString() ?? "";
                if (!Eq(shelfStr2, exShelfStr)) changed = true;

                // BaseProductId (col Y)
                var baseIdStr2 = ws.Cell(rowNum, 25).GetString()?.Trim();
                var exBaseIdStr = existProd.BaseProductId?.ToString() ?? "";
                if (!Eq(baseIdStr2, exBaseIdStr)) changed = true;

                // Compare SKUs
                if (!changed)
                {
                    var existingSkus = skusByProduct.GetValueOrDefault(existProd.Id) ?? new List<ProductSku>();
                    var importSkuKeys = new List<string>();
                    for (int g = 0; g < skuGroupCount; g++)
                    {
                        var baseCol = 26 + g * 5;
                        var mfgN = ws.Cell(rowNum, baseCol).GetString()?.Trim() ?? "";
                        if (string.IsNullOrWhiteSpace(mfgN)) continue;
                        var buyU = ws.Cell(rowNum, baseCol + 1).GetString()?.Trim() ?? "";
                        var pkgQ = ws.Cell(rowNum, baseCol + 2).GetString()?.Trim() ?? "";
                        var storU = ws.Cell(rowNum, baseCol + 3).GetString()?.Trim() ?? "";
                        var vpuS = ws.Cell(rowNum, baseCol + 4).GetString()?.Trim() ?? "";
                        importSkuKeys.Add($"{mfgN}|{buyU}|{pkgQ}|{storU}|{vpuS}".ToLowerInvariant());
                    }

                    var existingSkuKeys = existingSkus.Select(s =>
                    {
                        var mN = mfgNameMap.GetValueOrDefault(s.ManufacturerId, "");
                        var bU = s.BaseUnitId.HasValue ? unitNameMap.GetValueOrDefault(s.BaseUnitId.Value, "") : "";
                        var pQ = s.PackagingQuantity?.ToString() ?? "";
                        var sU = s.PackagingUnitId.HasValue ? unitNameMap.GetValueOrDefault(s.PackagingUnitId.Value, "") : "";
                        var vU = s.VolumePerUnit?.ToString() ?? "";
                        return $"{mN}|{bU}|{pQ}|{sU}|{vU}".ToLowerInvariant();
                    }).ToList();

                    if (importSkuKeys.Count != existingSkuKeys.Count
                        || !importSkuKeys.OrderBy(k => k).SequenceEqual(existingSkuKeys.OrderBy(k => k)))
                        changed = true;
                }

                if (!changed)
                {
                    item.Status = ProductMasterDataImportItemStatus.Skipped;
                    item.SkipReason = "Không có thay đổi";
                    updateCount--;
                    skippedCount++;
                }
            }

            // Check duplicate name within same ProductType (only for New items — Update already matched)
            if (item.IsValid && item.Status == ProductMasterDataImportItemStatus.New
                && productType.HasValue && !string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = name.Trim().ToLowerInvariant();
                var existsInDb = allProducts.Any(p =>
                    p.Name.Trim().ToLowerInvariant() == normalizedName
                    && p.ProductType == productType.Value);
                if (existsInDb)
                    AddError("Tên vật tư", "Tên sản phẩm đã tồn tại trong cùng loại sản phẩm");
            }

            // Check duplicate identificationCode within the file (across rows)
            if (item.IsValid && !string.IsNullOrWhiteSpace(identificationCode))
            {
                var normalizedCode = identificationCode.Trim().ToLowerInvariant();
                var duplicateInFile = result.Items.Any(i =>
                    i.IdentificationCode.Trim().ToLowerInvariant() == normalizedCode);
                if (duplicateInFile)
                    AddError("Mã nhận dạng", "Mã nhận dạng bị trùng trong file");
            }

            // Check duplicate name+productType within the file for rows matched by name (no identificationCode)
            if (item.IsValid && string.IsNullOrWhiteSpace(identificationCode)
                && item.ExistingProductId.HasValue && productType.HasValue && !string.IsNullOrWhiteSpace(name))
            {
                var dupByName = result.Items.Any(i =>
                    i.ExistingProductId == item.ExistingProductId);
                if (dupByName)
                    AddError("Tên vật tư", "Trùng sản phẩm trong file (cùng tên và loại vật tư)");
            }

            item.Errors = rowErrorMessages;
            result.Items.Add(item);

            if (item.IsValid && item.Status != ProductMasterDataImportItemStatus.Skipped)
                validationResult.ValidRows++;
        }

        validationResult.TotalRows = result.Items.Count;
        result.ValidationResult = validationResult;
        result.TotalRows = result.Items.Count;
        result.ValidRows = validationResult.ValidRows;
        result.InvalidRows = result.Items.Count - validationResult.ValidRows - skippedCount;
        result.SkippedRows = skippedCount;
        result.NewItems = newCount;
        result.UpdateItems = updateCount;

        if (validationResult.Errors.Count > 0)
        {
            memoryStream.Position = 0;
            var errorFileBytes = GenerateProductMasterDataErrorFile(memoryStream, rowErrors);
            result.ErrorFileBase64 = Convert.ToBase64String(errorFileBytes);
            result.ErrorFileName = "Vat_tu_Loi.xlsx";
        }

        return result;
    }

    public async Task<ImportResultDto> ImportProductMasterDataAsync(byte[] fileBytes)
    {
        using var memoryStream = new MemoryStream(fileBytes);
        var preview = await PreviewProductMasterDataInternalAsync(memoryStream);

        if (preview.ValidationResult.Errors.Count > 0 && preview.ValidRows == 0)
        {
            return new ImportResultDto
            {
                Success = false,
                TotalRows = preview.TotalRows,
                Errors = preview.ValidationResult.Errors,
                ErrorFileBase64 = preview.ErrorFileBase64,
                ErrorFileName = preview.ErrorFileName,
                Message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra và sửa lỗi."
            };
        }

        var newCount = 0;
        var updatedCount = 0;

        try
        {
            memoryStream.Position = 0;
            using var workbook = new XLWorkbook(memoryStream);
            var ws = workbook.Worksheet(1);

            var units = (await _unitRepository.GetQueryableAsync()).ToList();
            var unitByName = units
                .GroupBy(u => u.Name.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());
            var unitByCategoryAndName = units
                .GroupBy(u => (u.Category, u.Name.Trim().ToLowerInvariant()))
                .ToDictionary(g => g.Key, g => g.First());

            var manufacturers = (await _manufacturerRepository.GetQueryableAsync()).ToList();
            var mfgByName = manufacturers
                .GroupBy(m => m.Name.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            var allProducts = (await _productRepository.GetQueryableAsync()).ToList();
            var productByCode = allProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.IdentificationCode))
                .GroupBy(p => p.IdentificationCode!.Trim().ToLowerInvariant())
                .ToDictionary(g => g.Key, g => g.First());

            // Detect SKU group count from headers
            var skuGroupCount = 0;
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 30;
            for (int col = 26; col <= lastCol; col += 5)
            {
                var header = ws.Cell(1, col).GetString()?.Trim() ?? "";
                if (header.StartsWith("Hãng sản xuất", StringComparison.OrdinalIgnoreCase))
                    skuGroupCount++;
                else
                    break;
            }
            if (skuGroupCount == 0) skuGroupCount = 1;

            // Pre-count existing codes with today's prefix for all product types (avoids N roundtrips)
            int dbCodeCount;
            using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
            {
                var dateStr = DateTime.Now.ToString("yyyyMMdd");
                var query = await _productRepository.GetQueryableAsync();
                dbCodeCount = query.Count(x => x.Code.Contains(dateStr));
            }

            foreach (var previewItem in preview.Items.Where(i => i.IsValid && i.Status != ProductMasterDataImportItemStatus.Skipped))
            {
                var rowNum = previewItem.RowNumber;
                var productType = ParseProductTypeLabel(ws.Cell(rowNum, 1).GetString()?.Trim());
                if (!productType.HasValue) continue;

                var name = ws.Cell(rowNum, 2).GetString()?.Trim() ?? "";
                var formattedName = GetCellRichTextAsHtml(ws.Cell(rowNum, 2)) ?? name;
                var volumeUnitName = ws.Cell(rowNum, 3).GetString()?.Trim();
                var identificationCode = ws.Cell(rowNum, 4).GetString()?.Trim();

                Guid? volumeUnitId = null;
                if (!string.IsNullOrWhiteSpace(volumeUnitName))
                {
                    if (unitByCategoryAndName.TryGetValue((UnitCategory.ConsumableUnit, volumeUnitName.ToLowerInvariant()), out var vu))
                        volumeUnitId = vu.Id;
                    else if (unitByName.TryGetValue(volumeUnitName.ToLowerInvariant(), out var vuFallback))
                        volumeUnitId = vuFallback.Id;
                }

                decimal? minStockLevel = TryReadDecimalCell(ws.Row(rowNum), 5);

                // F: Cấu hình kỹ thuật — Equipment → MainTechnicalSpecs, others → Concentration/FormattedConcentration
                string? mainTechnicalSpecs = null;
                string? concentration = null;
                string? formattedConcentration = null;
                if (productType == Products.ProductType.Equipment)
                {
                    mainTechnicalSpecs = ws.Cell(rowNum, 6).GetString()?.Trim();
                }
                else
                {
                    formattedConcentration = GetCellRichTextAsHtml(ws.Cell(rowNum, 6));
                    concentration = ws.Cell(rowNum, 6).GetString()?.Trim();
                }

                var categories = ws.Cell(rowNum, 7).GetString()?.Trim();

                var formattedStorageConditions = GetCellRichTextAsHtml(ws.Cell(rowNum, 8));
                var storageConditions = ws.Cell(rowNum, 8).GetString()?.Trim();

                // I: Công thức hóa học — Chemical uses plain text only, Standard/Solvent/PreparedSolution use rich text
                string? formattedChemicalFormula = null;
                string? chemicalFormula = null;
                if (productType == Products.ProductType.Chemical)
                {
                    chemicalFormula = ws.Cell(rowNum, 9).GetString()?.Trim();
                }
                else
                {
                    formattedChemicalFormula = GetCellRichTextAsHtml(ws.Cell(rowNum, 9));
                    chemicalFormula = ws.Cell(rowNum, 9).GetString()?.Trim();
                }

                var casNumber = ws.Cell(rowNum, 10).GetString()?.Trim();

                var formattedOrigin = GetCellRichTextAsHtml(ws.Cell(rowNum, 11));
                var origin = ws.Cell(rowNum, 11).GetString()?.Trim();

                var ghsCode = GetCellRichTextAsHtml(ws.Cell(rowNum, 12)) ?? ws.Cell(rowNum, 12).GetString()?.Trim();

                var formattedPackagingSpec = GetCellRichTextAsHtml(ws.Cell(rowNum, 13));
                var packagingSpec = ws.Cell(rowNum, 13).GetString()?.Trim();

                var formattedMaterialComposition = GetCellRichTextAsHtml(ws.Cell(rowNum, 14));
                var materialComposition = ws.Cell(rowNum, 14).GetString()?.Trim();

                var isSterileStr = ws.Cell(rowNum, 15).GetString()?.Trim();
                bool? isSterile = string.IsNullOrWhiteSpace(isSterileStr) ? null
                    : isSterileStr.Equals("Y", StringComparison.OrdinalIgnoreCase);

                var isReusableStr = ws.Cell(rowNum, 16).GetString()?.Trim();
                bool? isReusable = string.IsNullOrWhiteSpace(isReusableStr) ? null
                    : isReusableStr.Equals("Y", StringComparison.OrdinalIgnoreCase);

                int? maxReuseCount = null;
                var maxReuseStr = ws.Cell(rowNum, 17).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(maxReuseStr) && int.TryParse(maxReuseStr, out var mrc))
                    maxReuseCount = mrc;

                var formattedCertifiedValue = GetCellRichTextAsHtml(ws.Cell(rowNum, 18));
                var certifiedValue = ws.Cell(rowNum, 18).GetString()?.Trim();

                var formattedMeasurementUncertainty = GetCellRichTextAsHtml(ws.Cell(rowNum, 19));
                var measurementUncertainty = ws.Cell(rowNum, 19).GetString()?.Trim();

                var standardLink = ws.Cell(rowNum, 20).GetString()?.Trim();
                var analysisGrade = ws.Cell(rowNum, 21).GetString()?.Trim();
                var usagePurpose = ws.Cell(rowNum, 22).GetString()?.Trim();

                int? shelfLifeDays = null;
                var shelfLifeStr = ws.Cell(rowNum, 23).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(shelfLifeStr) && int.TryParse(shelfLifeStr, out var sld))
                    shelfLifeDays = sld;

                Guid? baseProductId = null;
                var baseProductIdStr = ws.Cell(rowNum, 25).GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(baseProductIdStr) && Guid.TryParse(baseProductIdStr, out var bpg))
                    baseProductId = bpg;

                // Parse SKU groups
                var skuInputs = new List<(Guid ManufacturerId, Guid? BaseUnitId, int? PkgQty, Guid? PkgUnitId, int? Vpu)>();
                for (int g = 0; g < skuGroupCount; g++)
                {
                    var baseCol = 26 + g * 5;
                    var mfgName = ws.Cell(rowNum, baseCol).GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(mfgName)) continue;
                    if (!mfgByName.TryGetValue(mfgName.ToLowerInvariant(), out var mfg)) continue;

                    var buyUnitName = ws.Cell(rowNum, baseCol + 1).GetString()?.Trim();
                    Guid? buyUnitId = null;
                    if (!string.IsNullOrWhiteSpace(buyUnitName))
                    {
                        if (unitByCategoryAndName.TryGetValue((UnitCategory.PurchaseUnit, buyUnitName.ToLowerInvariant()), out var bu))
                            buyUnitId = bu.Id;
                        else if (unitByName.TryGetValue(buyUnitName.ToLowerInvariant(), out var buFallback))
                            buyUnitId = buFallback.Id;
                    }

                    int? pkgQty = null;
                    var pkgQtyStr = ws.Cell(rowNum, baseCol + 2).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(pkgQtyStr) && int.TryParse(pkgQtyStr, out var pq))
                        pkgQty = pq;

                    var storUnitName = ws.Cell(rowNum, baseCol + 3).GetString()?.Trim();
                    Guid? storUnitId = null;
                    if (!string.IsNullOrWhiteSpace(storUnitName))
                    {
                        if (unitByCategoryAndName.TryGetValue((UnitCategory.StorageUnit, storUnitName.ToLowerInvariant()), out var su))
                            storUnitId = su.Id;
                        else if (unitByName.TryGetValue(storUnitName.ToLowerInvariant(), out var suFallback))
                            storUnitId = suFallback.Id;
                    }

                    int? vpu = null;
                    var vpuStr = ws.Cell(rowNum, baseCol + 4).GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(vpuStr) && int.TryParse(vpuStr, out var v))
                        vpu = v;

                    skuInputs.Add((mfg.Id, buyUnitId, pkgQty, storUnitId, vpu));
                }

                if (previewItem.Status == ProductMasterDataImportItemStatus.Update
                    && previewItem.ExistingProductId.HasValue)
                {
                    var product = await _productRepository.GetAsync(previewItem.ExistingProductId.Value);
                    product.Name = name;
                    product.FormattedName = formattedName;
                    product.ProductType = productType.Value;
                    product.IdentificationCode = identificationCode;
                    product.VolumeUnitId = volumeUnitId;
                    product.MinStockLevel = minStockLevel;
                    product.MainTechnicalSpecs = mainTechnicalSpecs;
                    product.Concentration = concentration;
                    product.FormattedConcentration = formattedConcentration;
                    product.Categories = categories;
                    product.StorageConditions = storageConditions;
                    product.FormattedStorageConditions = formattedStorageConditions;
                    product.ChemicalFormula = chemicalFormula;
                    product.FormattedChemicalFormula = formattedChemicalFormula;
                    product.CASNumber = casNumber;
                    product.Origin = origin;
                    product.FormattedOrigin = formattedOrigin;
                    product.GhsCode = ghsCode;
                    product.PackagingSpec = packagingSpec;
                    product.FormattedPackagingSpec = formattedPackagingSpec;
                    product.MaterialComposition = materialComposition;
                    product.FormattedMaterialComposition = formattedMaterialComposition;
                    product.IsSterile = isSterile;
                    product.IsReusable = isReusable;
                    product.MaxReuseCount = maxReuseCount;
                    product.CertifiedValue = certifiedValue;
                    product.FormattedCertifiedValue = formattedCertifiedValue;
                    product.MeasurementUncertainty = measurementUncertainty;
                    product.FormattedMeasurementUncertainty = formattedMeasurementUncertainty;
                    product.StandardLink = standardLink;
                    product.AnalysisGrade = analysisGrade;
                    product.UsagePurpose = usagePurpose;
                    product.ShelfLifeDays = shelfLifeDays;
                    product.BaseProductId = baseProductId;

                    await _productRepository.UpdateAsync(product, autoSave: false);

                    var existingSkus = (await _productSkuRepository.GetQueryableAsync())
                        .Where(s => s.ProductId == product.Id).ToList();
                    foreach (var es in existingSkus)
                        await _productSkuRepository.DeleteAsync(es, autoSave: false);

                    foreach (var skuInput in skuInputs)
                    {
                        var sku = new ProductSku(
                            _guidGenerator.Create(), product.Id, skuInput.ManufacturerId,
                            skuInput.BaseUnitId, skuInput.PkgQty, skuInput.PkgUnitId,
                            skuInput.Vpu, volumeUnitId);
                        await _productSkuRepository.InsertAsync(sku, autoSave: false);
                    }

                    updatedCount++;
                }
                else
                {
                    var code = _codeGenerator.GenerateCodeWithDate(
                        productType.Value switch
                        {
                            Products.ProductType.Chemical => "CHE",
                            Products.ProductType.Biological => "BIO",
                            Products.ProductType.Supply => "SUP",
                            Products.ProductType.Equipment => "EQP",
                            _ => "PRD"
                        },
                        dbCodeCount + newCount);
                    var product = new Product(
                        _guidGenerator.Create(), code, name, productType.Value, null)
                    {
                        FormattedName = formattedName,
                        IdentificationCode = identificationCode,
                        VolumeUnitId = volumeUnitId,
                        MinStockLevel = minStockLevel,
                        MainTechnicalSpecs = mainTechnicalSpecs,
                        Concentration = concentration,
                        FormattedConcentration = formattedConcentration,
                        Categories = categories,
                        StorageConditions = storageConditions,
                        FormattedStorageConditions = formattedStorageConditions,
                        ChemicalFormula = chemicalFormula,
                        FormattedChemicalFormula = formattedChemicalFormula,
                        CASNumber = casNumber,
                        Origin = origin,
                        FormattedOrigin = formattedOrigin,
                        GhsCode = ghsCode,
                        PackagingSpec = packagingSpec,
                        FormattedPackagingSpec = formattedPackagingSpec,
                        MaterialComposition = materialComposition,
                        FormattedMaterialComposition = formattedMaterialComposition,
                        IsSterile = isSterile,
                        IsReusable = isReusable,
                        MaxReuseCount = maxReuseCount,
                        CertifiedValue = certifiedValue,
                        FormattedCertifiedValue = formattedCertifiedValue,
                        MeasurementUncertainty = measurementUncertainty,
                        FormattedMeasurementUncertainty = formattedMeasurementUncertainty,
                        StandardLink = standardLink,
                        AnalysisGrade = analysisGrade,
                        UsagePurpose = usagePurpose,
                        ShelfLifeDays = shelfLifeDays,
                        BaseProductId = baseProductId,
                        IsActive = true,
                        IsDraft = false,
                    };

                    await _productRepository.InsertAsync(product, autoSave: false);

                    foreach (var skuInput in skuInputs)
                    {
                        var sku = new ProductSku(
                            _guidGenerator.Create(), product.Id, skuInput.ManufacturerId,
                            skuInput.BaseUnitId, skuInput.PkgQty, skuInput.PkgUnitId,
                            skuInput.Vpu, volumeUnitId);
                        await _productSkuRepository.InsertAsync(sku, autoSave: false);
                    }

                    newCount++;
                }
            }

            return new ImportResultDto
            {
                Success = true,
                TotalRows = preview.TotalRows,
                ImportedRows = newCount + updatedCount,
                SkippedRows = preview.SkippedRows,
                Message = $"Import thành công: {newCount} mới, {updatedCount} cập nhật."
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

    private async Task<string> GenerateProductMasterDataCodeAsync(Products.ProductType productType)
    {
        var prefix = productType switch
        {
            Products.ProductType.Chemical => "CHE",
            Products.ProductType.Biological => "BIO",
            Products.ProductType.Supply => "SUP",
            Products.ProductType.Equipment => "EQP",
            _ => "PRD"
        };

        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix(prefix);
            var query = await _productRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate(prefix, count);
        }
    }

    private byte[] GenerateProductMasterDataErrorFile(MemoryStream originalStream, Dictionary<int, List<string>> rowErrors)
    {
        using var workbook = new XLWorkbook(originalStream);
        var ws = workbook.Worksheet(1);

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 30;
        var errorCol = lastCol + 1;
        ws.Cell(1, errorCol).Value = "Lỗi";
        ws.Cell(1, errorCol).Style.Font.Bold = true;
        ws.Cell(1, errorCol).Style.Font.FontColor = XLColor.Red;

        var lastRowUsed = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int r = 2; r <= lastRowUsed; r++)
        {
            if (rowErrors.TryGetValue(r, out var errors))
            {
                ws.Cell(r, errorCol).Value = string.Join("; ", errors);
                ws.Cell(r, errorCol).Style.Font.FontColor = XLColor.Red;
            }
        }

        using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);
        return outputStream.ToArray();
    }

    #endregion
}

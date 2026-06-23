namespace LabSpecimenManagement.Common;

/// <summary>
/// Constants for sorting field names used across the application.
/// These are used in switch expressions for dynamic sorting.
/// </summary>
public static class SortingFields
{
    // Date fields
    public const string TransferDate = "transferdate";
    public const string TransferDateDesc = "transferdate desc";
    public const string RequestCompletionDate = "requestcompletiondate";
    public const string RequestCompletionDateDesc = "requestcompletiondate desc";
    public const string TestDate = "testdate";
    public const string TestDateDesc = "testdate desc";
    public const string CreationTime = "creationtime";
    public const string CreationTimeDesc = "creationtime desc";
    public const string ContractDate = "contractdate";
    public const string ContractDateDesc = "contractdate desc";
    public const string LatestApprovalDate = "latestapprovaldate";
    public const string LatestApprovalDateDesc = "latestapprovaldate desc";
    public const string ExpectedCompletionDate = "expectedcompletiondate";
    public const string ExpectedCompletionDateDesc = "expectedcompletiondate desc";
    public const string ResultDeliveryDate = "resultdeliverydate";
    public const string ResultDeliveryDateDesc = "resultdeliverydate desc";

    // Status fields
    public const string Status = "status";
    public const string StatusDesc = "status desc";
    public const string TestingStatus = "testingstatus";
    public const string TestingStatusDesc = "testingstatus desc";

    // Text fields
    public const string ContractNumber = "contractnumber";
    public const string ContractNumberDesc = "contractnumber desc";
    public const string CustomerName = "customername";
    public const string CustomerNameDesc = "customername desc";
    public const string Address = "address";
    public const string AddressDesc = "address desc";
    public const string TaxCode = "taxcode";
    public const string TaxCodeDesc = "taxcode desc";
    public const string ContactPerson = "contactperson";
    public const string ContactPersonDesc = "contactperson desc";
    public const string Phone = "phone";
    public const string PhoneDesc = "phone desc";
    public const string Notes = "notes";
    public const string NotesDesc = "notes desc";
    public const string ReceiptNumber = "receiptnumber";
    public const string ReceiptNumberDesc = "receiptnumber desc";

    // Location fields
    public const string ProvinceCode = "provincecode";
    public const string ProvinceCodeDesc = "provincecode desc";
    public const string Province = "province";
    public const string ProvinceDesc = "province desc";
    public const string DistrictCode = "districtcode";
    public const string DistrictCodeDesc = "districtcode desc";
    public const string District = "district";
    public const string DistrictDesc = "district desc";
    public const string WardCode = "wardcode";
    public const string WardCodeDesc = "wardcode desc";
    public const string Ward = "ward";
    public const string WardDesc = "ward desc";

    // Numeric fields
    public const string TotalCost = "totalcost";
    public const string TotalCostDesc = "totalcost desc";
    public const string TotalCostInWords = "totalcostinwords";
    public const string TotalCostInWordsDesc = "totalcostinwords desc";
    public const string ResultCopyCount = "resultcopycount";
    public const string ResultCopyCountDesc = "resultcopycount desc";

    // Boolean/Option fields
    public const string MethodWestHighlandInstitute = "methodwesthighlandinstitute";
    public const string MethodWestHighlandInstituteDesc = "methodwesthighlandinstitute desc";
    public const string MethodCustomer = "methodcustomer";
    public const string MethodCustomerDesc = "methodcustomer desc";
    public const string MethodCustomerDetail = "methodcustomerdetail";
    public const string MethodCustomerDetailDesc = "methodcustomerdetail desc";
    public const string ResultLanguage = "resultlanguage";
    public const string ResultLanguageDesc = "resultlanguage desc";
    public const string SeparatePassedResults = "separatepassedresults";
    public const string SeparatePassedResultsDesc = "separatepassedresults desc";
    public const string ConformityDeclaration = "conformitydeclaration";
    public const string ConformityDeclarationDesc = "conformitydeclaration desc";
    public const string OtherRequirements = "otherrequirements";
    public const string OtherRequirementsDesc = "otherrequirements desc";
    public const string Subcontract = "subcontract";
    public const string SubcontractDesc = "subcontract desc";
    public const string SamplingRequirement = "samplingrequirement";
    public const string SamplingRequirementDesc = "samplingrequirement desc";
    public const string DeliveryDirectPickup = "deliverydirectpickup";
    public const string DeliveryDirectPickupDesc = "deliverydirectpickup desc";
    public const string DeliveryPostal = "deliverypostal";
    public const string DeliveryPostalDesc = "deliverypostal desc";
    public const string ReturnStoredSample = "returnstoredsample";
    public const string ReturnStoredSampleDesc = "returnstoredsample desc";

    // Sample type fields
    public const string SampleTypeService = "sampletypeservice";
    public const string SampleTypeServiceDesc = "sampletypeservice desc";
    public const string SampleTypeContract = "sampletypecontract";
    public const string SampleTypeContractDesc = "sampletypecontract desc";
    public const string SampleTypeTask = "sampletypetask";
    public const string SampleTypeTaskDesc = "sampletypetask desc";
    public const string SampleTypeSubcontract = "sampletypesubcontract";
    public const string SampleTypeSubcontractDesc = "sampletypesubcontract desc";
    public const string SampleStorage = "samplestorage";
    public const string SampleStorageDesc = "samplestorage desc";

    // Workflow fields
    public const string SentToAccounting = "senttoaccounting";
    public const string SentToAccountingDesc = "senttoaccounting desc";
    public const string PaymentReceived = "paymentreceived";
    public const string PaymentReceivedDesc = "paymentreceived desc";
    public const string SentToDepartment = "senttodepartment";
    public const string SentToDepartmentDesc = "senttodepartment desc";
    public const string ResultReturned = "resultreturned";
    public const string ResultReturnedDesc = "resultreturned desc";
    public const string AgreeTerms = "agreeterms";
    public const string AgreeTermsDesc = "agreeterms desc";
    public const string ChangeRequest = "changerequest";
    public const string ChangeRequestDesc = "changerequest desc";

    // Reference fields
    public const string SupervisorId = "supervisorid";
    public const string SupervisorIdDesc = "supervisorid desc";
    public const string DirectorId = "directorid";
    public const string DirectorIdDesc = "directorid desc";
    public const string Subcontractor = "subcontractor";
    public const string SubcontractorDesc = "subcontractor desc";
}

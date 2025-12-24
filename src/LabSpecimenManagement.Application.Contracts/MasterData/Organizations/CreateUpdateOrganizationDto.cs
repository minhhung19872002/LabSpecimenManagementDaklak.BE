using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData.Organizations;

public class CreateUpdateOrganizationDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên CS/DN")]
    [StringLength(256, ErrorMessage = "Tên không được quá 256 ký tự")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn tỉnh thành")]
    [StringLength(20)]
    public string ProvinceCode { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phường xã")]
    [StringLength(20)]
    public string WardCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
    public string Address { get; set; }

    [StringLength(20, ErrorMessage = "Mã số thuế không được quá 20 ký tự")]
    public string? TaxCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập người liên hệ")]
    [StringLength(100, ErrorMessage = "Tên người liên hệ không được quá 100 ký tự")]
    public string ContactPerson { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [StringLength(20)]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

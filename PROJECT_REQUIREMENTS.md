# Lab Specimen Management System - Yêu Cầu Chức Năng

## Tổng Quan Dự Án
Hệ thống quản lý mẫu thử nghiệm phòng thí nghiệm tuân thủ tiêu chuẩn ISO/IEC 17025:2017

**Công nghệ:**
- Backend: .NET 9.0 + ABP Framework 9.0.0 + PostgreSQL 15
- Frontend: React 18 + TypeScript + Vite + Ant Design
- Architecture: Domain-Driven Design (DDD)

phần mềm quản lý hệ thống kiểm nghiệm theo tiêu chuẩn ISO 17025:2017    
"I.YÊU CẦU CHUNG: 
Đáp ứng cho trên 20 người dùng (users) truy cập cùng lúc
Có Tài liệu hướng dẫn sử dụng theo ngôn ngữ tiếng Việt
Quy trình, biểu mẫu và thiết kế phần mềm, biểu mẫu hoạt động theo tiêu chuẩn quản lý phòng thí nghiệm ISO/IEC 17025:2017
Import dữ liệu đầu vào từ file excel theo mẫu định sẵn: 
Khai báo sẵn các ngân hàng dữ liệu để tái sử dụng.
Thiết lập giao diện theo cài đặt của mỗi người dùng.
Lưu lịch sử sửa đổi dữ liệu, đảm bảo truy xuất lịch sử theo chuẩn mực 7.11 ISO/IEC 17025:2017.
Ngôn ngữ sử dụng trong ứng dụng là Tiếng Việt, bộ mã tiếng Việt TCVN 6909:2001 (Unicode).
Bắt buộc có tính năng cài đặt sao lưu dữ liệu tự động
Đáp ứng tiêu chuẩn về bảo mật, khách quan:
- Quản trị tài khoản với quy định tên tài khoản và mật khẩu đảm bảo tính bảo mật.
- Hiển thị lịch sử thay đổi dữ liệu trong các giao diện của phần mềm, đặc biệt tại công việc từ tạo phiếu yêu cầu – trả kết quả.
- Dữ liệu bị sai sót ở công đoạn nào cần trả về đúng công đoạn đó để xử lý;
II.YÊU CẦU CHỨC NĂNG ỨNG DỤNG PHẦN MỀM:
Quy trình tiếp nhận yêu cầu thử nghiệm:
- Tạo phiếu yêu cầu thử nghiệm điện tử.
- Mã hóa mẫu, mã hóa dữ liệu tự động.
- Đính kèm hình ảnh mẫu, biên bản, hồ sơ do khách hàng cung cấp.
- Quản lý phép thử thực hiện bởi nhà thầu phụ và năng lực nhà thầu phụ.
- Tính toán thời gian thử nghiệm tự động từ TAT phép thử và có thể bỏ qua các ngày cuối tuần.
- Quản lý các phiếu yêu cầu, mẫu, phép thử bị huỷ, vẫn truy xuất và thống kê đầy đủ.
Quy trình phòng thí nghiệm:
- Giao việc, phân công công việc định kỳ hoặc hàng ngày.
- Tiếp nhận yêu cầu công việc
- Nhập và biểu diễn kết quả thử nghiệm tự động.
- Duyệt kết quả.
Quy trình báo cáo kết quả thử nghiệm:
- Tạo phiếu báo cáo thử nghiệm điện tử với trường hợp không có, có một hoặc nhiều tiêu chuẩn/quy chuẩn so sánh.
- Phê duyệt 02 bước đối với phiếu kết quả.
- Chuyển giao phiếu báo cáo kết quả thử nghiệm tự động đến tài khoản khách hàng và gửi tự động thông báo đến khách qua email của khách hàng.
- Mã QR code truy xuất nguồn gốc phiếu báo cáo kết quả thử nghiệm.
Quy trình công việc không phù hợp:
- Phân quyền trách nhiệm, quyền hạn xử lý công việc không phù hợp.
- Thực hiện các bước của quy trình công việc không phù hợp.
- Quản lý hồ sơ công việc không phù hợp.
- Quản lý các thống kê, báo cáo về công việc không phù hợp.
Quản lý mẫu thử nghiệm:
- Quản lý mẫu lưu, mẫu thanh lý, mẫu hủy, mẫu trả khách hàng.
- Theo dõi tình trạng mẫu, vị trí lưu mẫu và lịch sử đường đi của mẫu.
- Ghi nhận các sai lệch của mẫu.
Thống kê, báo cáo:
- Trích xuất dữ liệu yêu cầu thử nghiệm, mẫu, phép thử
- Theo dõi được tình trạng thử nghiệm, số lượng mẫu, phép thử qua thời gian, theo nền mẫu
- Theo dõi được tình trạng công việc của mỗi bộ phận.
III. YÊU CẦU VỀ DỊCH VỤ ĐÀO TẠO:
- Đào tạo tại chỗ cho các nhân sự sử dụng thành thạo
- Đào tạo từ xa cho các nhân sự trong quá trình sử dụng
IV. Yêu cầu bản quyền phần mềm:
- Sản phẩm phần mềm đã có chứng nhận quyền tác giả, sở hữu trí tuệ bởi cơ quan có thẩm quyền thuộc về nhà thầu.
- Cung cấp giấy ủy quyền phòng thí nghiệm sử dụng phần mềm.
- Ưu tiên các sản phẩm phần mềm được nghiên cứu, phát triển và xây dựng bởi các doanh nghiệp có pháp nhân tại Việt Nam
V.YÊU CẦU BẢO HÀNH:
- Thời gian bảo hành: 12 tháng kể từ ngày ký biên bản nghiệm thu phần mềm.
- Cam kết hỗ trợ, khắc phục sự cố trong vòng 24 giờ kể từ thời điểm thông báo sự cố.
- Hỗ trợ hướng dẫn sử dụng phần mềm từ xa.
- Người thực hiện bảo hành phải có trình độ phù hợp với chuyên ngành
- Cam kết hỗ trợ và cập nhật tối thiểu 10 năm"
---

## Phase 1: Thiết Kế Database & Entities ✅ HOÀN THÀNH

### 1.1. TestRequest (Phiếu Yêu Cầu Thử Nghiệm)
**Mục đích:** Quản lý phiếu yêu cầu từ khách hàng

**Thuộc tính:**
- `RequestNumber` (string): Số phiếu tự động (REQ-YYYYMMDD-XXXX)
- `RequestDate` (DateTime): Ngày yêu cầu
- `CustomerName` (string, required): Tên khách hàng
- `CustomerEmail` (string?, optional): Email
- `CustomerPhone` (string?, optional): Số điện thoại
- `CustomerInfo` (string?, optional): Thông tin liên hệ
- `Notes` (string?, optional): Ghi chú
- `Status` (enum): Trạng thái (Pending, InProgress, Completed, Cancelled)
- `ExpectedCompletionDate` (DateTime?): Ngày hoàn thành dự kiến
- `Subcontractor` (string?): Nhà thầu phụ (nếu có)

**Quan hệ:**
- One-to-Many với `Specimen` (Mẫu thử nghiệm)
- One-to-Many với `TestRequestAttachment` (Tài liệu đính kèm)

### 1.2. Specimen (Mẫu Thử Nghiệm)
**Mục đích:** Quản lý thông tin mẫu vật

**Thuộc tính:**
- `SpecimenCode` (string): Mã mẫu tự động (SPE-YYYYMMDD-XXXX)
- `Name` (string): Tên mẫu
- `Type` (string): Loại mẫu
- `Description` (string?): Mô tả
- `ReceivedDate` (DateTime): Ngày tiếp nhận
- `Status` (enum): Trạng thái (Received, Testing, Completed, Disposed, ReturnedToCustomer)
- `StorageLocation` (string?): Vị trí lưu trữ
- `StorageConditions` (string?): Điều kiện lưu trữ
- `Quantity` (decimal): Số lượng
- `Unit` (string): Đơn vị
- `ExpiryDate` (DateTime?): Ngày hết hạn
- `DisposalDate` (DateTime?): Ngày thanh lý

**Quan hệ:**
- Many-to-One với `TestRequest`
- One-to-Many với `SpecimenTest` (Thử nghiệm)
- One-to-Many với `SpecimenHistory` (Lịch sử)

### 1.3. SpecimenTest (Thử Nghiệm)
**Mục đích:** Quản lý các phép thử trên mẫu

**Thuộc tính:**
- `TestCode` (string): Mã thử nghiệm (TEST-YYYYMMDD-XXXX)
- `TestName` (string): Tên phép thử
- `TestMethod` (string): Phương pháp thử
- `Standard` (string?): Tiêu chuẩn áp dụng
- `AssignedTo` (Guid?): Người thực hiện
- `AssignedDate` (DateTime?): Ngày giao việc
- `Status` (enum): Trạng thái (Assigned, InProgress, Completed, Reviewed, Approved)
- `StartDate` (DateTime?): Ngày bắt đầu
- `CompletedDate` (DateTime?): Ngày hoàn thành
- `ReviewedBy` (Guid?): Người kiểm tra
- `ReviewedDate` (DateTime?): Ngày kiểm tra
- `ApprovedBy` (Guid?): Người phê duyệt
- `ApprovedDate` (DateTime?): Ngày phê duyệt

**Quan hệ:**
- Many-to-One với `Specimen`
- One-to-Many với `TestResult` (Kết quả)

### 1.4. TestResult (Kết Quả Thử Nghiệm)
**Mục đích:** Lưu kết quả chi tiết từng thử nghiệm

**Thuộc tính:**
- `ParameterName` (string): Tên thông số
- `Value` (string): Giá trị đo được
- `Unit` (string?): Đơn vị
- `AcceptanceCriteria` (string?): Tiêu chuẩn chấp nhận
- `IsPass` (bool?): Đạt/Không đạt
- `Remarks` (string?): Ghi chú

**Quan hệ:**
- Many-to-One với `SpecimenTest`

### 1.5. TestRequestAttachment (Tài Liệu Đính Kèm)
**Mục đích:** Lưu tài liệu liên quan đến yêu cầu

**Thuộc tính:**
- `FileName` (string): Tên file
- `FilePath` (string): Đường dẫn lưu trữ
- `FileSize` (long): Kích thước file
- `ContentType` (string): Loại file
- `Description` (string?): Mô tả

**Quan hệ:**
- Many-to-One với `TestRequest`

### 1.6. SpecimenHistory (Lịch Sử Mẫu)
**Mục đích:** Theo dõi toàn bộ hoạt động liên quan đến mẫu

**Thuộc tính:**
- `Action` (string): Hành động (Received, Stored, Tested, Disposed, etc.)
- `Description` (string): Mô tả chi tiết
- `PerformedBy` (Guid?): Người thực hiện

**Quan hệ:**
- Many-to-One với `Specimen`

### 1.7. NonConformanceReport (Báo Cáo Không Phù Hợp - NCR)
**Mục đích:** Quản lý các sự cố không phù hợp theo ISO 17025

**Thuộc tính:**
- `NcrNumber` (string): Số NCR (NCR-YYYYMMDD-XXXX)
- `NcrDate` (DateTime): Ngày phát sinh
- `Description` (string): Mô tả sự cố
- `RootCause` (string?): Nguyên nhân gốc rễ
- `CorrectiveAction` (string?): Hành động khắc phục
- `PreventiveAction` (string?): Hành động phòng ngừa
- `ResponsiblePerson` (Guid?): Người chịu trách nhiệm
- `DueDate` (DateTime?): Thời hạn xử lý
- `Status` (enum): Trạng thái (Open, InProgress, Closed)
- `ClosedDate` (DateTime?): Ngày đóng

---

## Phase 2: Test Request Management Module ✅ HOÀN THÀNH

### 2.1. Backend API
**Hoàn thành:**
- ✅ TestRequestDto, CreateTestRequestDto, UpdateTestRequestDto
- ✅ ITestRequestAppService interface
- ✅ TestRequestAppService implementation (CRUD + Request Number Generator)
- ✅ TestRequestController (REST API endpoints)
- ✅ AutoMapper configuration
- ✅ Database migration cho nullable fields
- ✅ Testing API với curl

**Endpoints:**
```
GET    /api/app/test-request        - Lấy danh sách phiếu yêu cầu
GET    /api/app/test-request/{id}   - Lấy chi tiết phiếu yêu cầu
POST   /api/app/test-request        - Tạo phiếu yêu cầu mới
PUT    /api/app/test-request/{id}   - Cập nhật phiếu yêu cầu
DELETE /api/app/test-request/{id}   - Xóa phiếu yêu cầu
```

**Request Number Format:**
- Pattern: `REQ-YYYYMMDD-XXXX`
- Example: `REQ-20251211-0001`
- Auto-increment theo ngày

### 2.2. Frontend UI
**Hoàn thành:**
- ✅ TestRequestList component (Table view với pagination, search, filter)
- ✅ TestRequestForm component (Create/Edit form với validation)
- ✅ Vietnamese i18n translations
- ✅ Routing configuration
- ✅ testRequestService.ts (API integration với axios)

**Chức năng:**
- Xem danh sách phiếu yêu cầu (table với columns: RequestNumber, CustomerName, RequestDate, Status, Actions)
- Tạo phiếu yêu cầu mới (form với validation)
- Sửa phiếu yêu cầu (reuse form component)
- Xóa phiếu yêu cầu (với confirmation)
- Tìm kiếm theo RequestNumber, CustomerName
- Filter theo Status

**UI Components:**
- Ant Design Table, Form, Input, DatePicker, Select, Button, Modal
- Responsive 2-column layout
- Vietnamese date format (DD/MM/YYYY)
- Success/Error notifications

---

## Phase 3: Laboratory Workflow Features (🔄 ĐANG CHUẨN BỊ)

### 3.1. Specimen Management
**Yêu cầu:**
- CRUD operations cho Specimen
- Link Specimen với TestRequest
- Quản lý trạng thái mẫu (lifecycle)
- Tìm kiếm và lọc mẫu
- In mã vạch/QR code cho mẫu

### 3.2. Test Assignment & Execution
**Yêu cầu:**
- Giao việc thử nghiệm cho nhân viên
- Tracking tiến độ thử nghiệm
- Nhập kết quả thử nghiệm
- Two-step approval process (Reviewer → Approver)
- Validation rules cho kết quả

### 3.3. Result Management
**Yêu cầu:**
- Quản lý TestResult chi tiết
- So sánh với tiêu chuẩn chấp nhận
- Đánh dấu Pass/Fail
- Export results to Excel/PDF

### 3.4. Report Generation
**Yêu cầu:**
- Template báo cáo kết quả (PDF)
- Hỗ trợ nhiều tiêu chuẩn so sánh
- QR code truy xuất nguồn gốc
- Email báo cáo cho khách hàng
- Digital signature

---

## Phase 4: Document & Quality Management (📋 CHƯA BẮT ĐẦU)

### 4.1. Document Management
**Yêu cầu:**
- Upload và quản lý tài liệu đính kèm
- Version control cho documents
- Access control theo role

### 4.2. NCR (Non-Conformance Report)
**Yêu cầu:**
- CRUD operations cho NCR
- Workflow xử lý NCR (Open → InProgress → Closed)
- Link NCR với Specimen/Test
- Tracking CAPA (Corrective & Preventive Actions)
- Báo cáo thống kê NCR

### 4.3. Audit Trail
**Yêu cầu:**
- Log tất cả hành động quan trọng
- Xem lịch sử thay đổi
- Export audit logs

---

## Phase 5: Advanced Features (🚀 TƯƠNG LAI)

### 5.1. Dashboard & Analytics
**Yêu cầu:**
- Tổng quan số liệu (Total requests, specimens, tests)
- Biểu đồ thống kê theo thời gian
- Performance metrics (TAT, throughput)
- Alerts và notifications

### 5.2. Integration & API
**Yêu cầu:**
- RESTful API documentation (Swagger)
- External system integration
- Webhook support

### 5.3. User & Permission Management
**Yêu cầu:**
- Role-based access control (RBAC)
- User management
- Permission management

---

## Tiêu Chuẩn ISO/IEC 17025:2017 Compliance

### Các yêu cầu chính:
1. ✅ **Traceability**: Request Number, Specimen Code, Test Code
2. ✅ **Record keeping**: Full audit trail với CreatedBy, CreatedAt, UpdatedBy, UpdatedAt
3. 🔄 **Sample handling**: Specimen status tracking, storage conditions
4. 🔄 **Result validation**: Two-step approval (Reviewer + Approver)
5. 🔄 **Non-conformance**: NCR module
6. 📋 **Document control**: Version control, access control
7. 📋 **Corrective actions**: CAPA tracking in NCR

---

## Technical Stack Details

### Backend (.NET 9.0)
- **Framework**: ABP Framework 9.0.0
- **Database**: PostgreSQL 15
- **ORM**: Entity Framework Core
- **Architecture**: DDD (Domain-Driven Design)
- **Authentication**: OpenIddict (OAuth 2.0 / OpenID Connect)
- **API Documentation**: Swagger/OpenAPI

### Frontend (React 18)
- **Build Tool**: Vite
- **Language**: TypeScript
- **UI Library**: Ant Design 5.x
- **State Management**: React hooks (useState, useEffect)
- **HTTP Client**: Axios
- **Routing**: React Router v6
- **i18n**: react-i18next (Vietnamese support)
- **Date Handling**: dayjs

### Database Schema
- PostgreSQL 15
- Naming convention: `App{EntityName}` (e.g., AppTestRequests)
- Soft delete support (IsDeleted column)
- Full audit columns (CreatorId, CreationTime, LastModifierId, LastModificationTime, DeleterId, DeletionTime)

---

## Notes
- File này sẽ được cập nhật liên tục khi có thêm yêu cầu mới
- Mỗi Phase sẽ có document chi tiết riêng khi bắt đầu implementation
- Tất cả changes phải được review và approve trước khi merge

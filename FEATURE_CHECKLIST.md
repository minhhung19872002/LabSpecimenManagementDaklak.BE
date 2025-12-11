# Lab Specimen Management System - Checklist Chức Năng

**Dựa trên yêu cầu ISO/IEC 17025:2017**

---

## I. YÊU CẦU CHUNG

### Hiệu Năng & Khả Năng
- [ ] Hỗ trợ **20+ người dùng** truy cập đồng thời
- [ ] Performance testing với concurrent users
- [ ] Load balancing configuration

### Documentation
- [x] Tài liệu hướng dẫn sử dụng **tiếng Việt**
  - [ ] User manual (đang soạn thảo)
  - [ ] Admin manual
  - [ ] Developer documentation

### Tuân Thủ Tiêu Chuẩn
- [x] Quy trình theo **ISO/IEC 17025:2017**
- [x] Biểu mẫu điện tử tuân thủ chuẩn
- [ ] Audit trail theo chuẩn mực 7.11 ISO/IEC 17025:2017

### Import/Export Dữ Liệu
- [ ] **Import từ Excel** theo template
  - [ ] Template Excel cho TestRequest
  - [ ] Template Excel cho Specimen
  - [ ] Template Excel cho Test Results
  - [ ] Validation dữ liệu import

### Master Data Management
- [ ] **Ngân hàng dữ liệu** tái sử dụng
  - [ ] Master data: Loại mẫu (Specimen Types)
  - [ ] Master data: Phương pháp thử (Test Methods)
  - [ ] Master data: Tiêu chuẩn (Standards)
  - [ ] Master data: Đơn vị đo (Units)
  - [ ] Master data: Khách hàng (Customers)
  - [ ] Master data: Nhà thầu phụ (Subcontractors)

### Personalization
- [ ] **Cài đặt giao diện** theo người dùng
  - [ ] Theme preferences (Light/Dark mode)
  - [ ] Language preference
  - [ ] Default filters
  - [ ] Column visibility settings
  - [ ] Dashboard widget configuration

### Lịch Sử & Audit Trail
- [x] Lưu **lịch sử sửa đổi** dữ liệu
  - [x] CreatedBy, CreatedAt (ABP Framework built-in)
  - [x] LastModifiedBy, LastModifiedAt (ABP Framework built-in)
  - [x] DeletedBy, DeletedAt (Soft delete enabled)
  - [ ] Detailed change history (field-level tracking)
  - [ ] Audit log UI for viewing history

### Ngôn Ngữ
- [x] **Tiếng Việt** TCVN 6909:2001 (Unicode)
  - [x] UI tiếng Việt với react-i18next
  - [x] Database support Unicode
  - [ ] Report templates tiếng Việt

### Backup & Recovery
- [ ] **Sao lưu tự động**
  - [ ] Scheduled database backup (daily/weekly)
  - [ ] Backup retention policy
  - [ ] Restore procedure
  - [ ] Disaster recovery plan

### Bảo Mật & Khách Quan
- [ ] **Quản trị tài khoản**
  - [ ] Strong password policy (min 8 chars, special chars, etc.)
  - [ ] Password encryption
  - [ ] Session timeout
  - [ ] Two-factor authentication (2FA) - optional
  - [ ] Role-based access control (RBAC)

- [x] **Hiển thị lịch sử thay đổi**
  - [x] Audit columns in all entities
  - [ ] History viewer UI in all forms
  - [ ] Track changes từ tạo phiếu → trả kết quả

- [ ] **Workflow xử lý sai sót**
  - [ ] Return to correct step when error occurs
  - [ ] Reject/Return mechanism
  - [ ] Comments/feedback on rejection

---

## II. YÊU CẦU CHỨC NĂNG ỨNG DỤNG

### 1. Quy Trình Tiếp Nhận Yêu Cầu Thử Nghiệm

#### 1.1. Phiếu Yêu Cầu Điện Tử
- [x] **Tạo phiếu yêu cầu thử nghiệm điện tử**
  - [x] TestRequest CRUD (Create, Read, Update, Delete)
  - [x] Backend API endpoints
  - [x] Frontend UI (List + Form)
  - [x] Validation rules

#### 1.2. Mã Hóa Tự Động
- [x] **Mã hóa mẫu, dữ liệu tự động**
  - [x] Request Number Generator (REQ-YYYYMMDD-XXXX)
  - [ ] Specimen Code Generator (SPE-YYYYMMDD-XXXX)
  - [ ] Test Code Generator (TEST-YYYYMMDD-XXXX)
  - [ ] Barcode generation
  - [ ] QR code generation

#### 1.3. Đính Kèm Tài Liệu
- [ ] **Đính kèm hình ảnh, biên bản, hồ sơ**
  - [ ] File upload (images, PDFs)
  - [ ] File preview
  - [ ] File download
  - [ ] TestRequestAttachment entity integration
  - [ ] Storage management (local/cloud)
  - [ ] File size limit & validation

#### 1.4. Quản Lý Nhà Thầu Phụ
- [x] **Quản lý phép thử thực hiện bởi nhà thầu phụ**
  - [x] Subcontractor field in TestRequest
  - [ ] Subcontractor master data
  - [ ] Subcontractor capability tracking
  - [ ] Subcontractor certificate management
  - [ ] Subcontractor performance evaluation

#### 1.5. Tính TAT (Turnaround Time)
- [ ] **Tính toán thời gian thử nghiệm tự động**
  - [ ] TAT configuration per test method
  - [ ] Business day calculation (skip weekends)
  - [ ] Holiday calendar configuration
  - [ ] Auto-calculate ExpectedCompletionDate
  - [ ] TAT tracking & alerts

#### 1.6. Quản Lý Hủy Bỏ
- [x] **Quản lý phiếu yêu cầu, mẫu, phép thử bị hủy**
  - [x] Soft delete (IsDeleted flag)
  - [x] Cancelled status
  - [ ] Cancellation reason tracking
  - [ ] Restore deleted items
  - [ ] Reports including cancelled items

---

### 2. Quy Trình Phòng Thí Nghiệm

#### 2.1. Giao Việc & Phân Công
- [ ] **Giao việc, phân công công việc**
  - [ ] Daily task assignment
  - [ ] Periodic task scheduling
  - [ ] Assign tests to users
  - [ ] Workload balancing
  - [ ] Task priority management

#### 2.2. Tiếp Nhận Công Việc
- [ ] **Tiếp nhận yêu cầu công việc**
  - [ ] Task inbox/queue for each user
  - [ ] Accept/Reject task
  - [ ] Task status tracking
  - [ ] Notifications for new tasks

#### 2.3. Nhập Kết Quả
- [ ] **Nhập và biểu diễn kết quả tự động**
  - [ ] Result entry form
  - [ ] Auto-calculation formulas
  - [ ] Data validation (range, format)
  - [ ] Charts/graphs for results
  - [ ] Result templates by test method

#### 2.4. Duyệt Kết Quả
- [ ] **Duyệt kết quả**
  - [ ] Review workflow
  - [ ] Approve/Reject result
  - [ ] Comments on review
  - [ ] Version control for results

---

### 3. Quy Trình Báo Cáo Kết Quả

#### 3.1. Tạo Báo Cáo Điện Tử
- [ ] **Tạo phiếu báo cáo thử nghiệm điện tử**
  - [ ] Report template design (no standard)
  - [ ] Report template với 1 tiêu chuẩn so sánh
  - [ ] Report template với nhiều tiêu chuẩn so sánh
  - [ ] Report number generation
  - [ ] Report preview

#### 3.2. Phê Duyệt 2 Bước
- [ ] **Phê duyệt 02 bước đối với phiếu kết quả**
  - [ ] Step 1: Reviewer approval
  - [ ] Step 2: Approver approval
  - [ ] Rejection workflow
  - [ ] Digital signature
  - [ ] Approval history tracking

#### 3.3. Chuyển Giao Tự Động
- [ ] **Chuyển giao phiếu báo cáo tự động**
  - [ ] Customer portal/account
  - [ ] Auto-email notification to customer
  - [ ] PDF attachment in email
  - [ ] Download link with expiry
  - [ ] Delivery confirmation

#### 3.4. QR Code Truy Xuất
- [x] **Mã QR code truy xuất nguồn gốc**
  - [ ] QR code generation on report
  - [ ] Public verification page
  - [ ] Scan & verify functionality
  - [ ] Tamper-proof validation

---

### 4. Quy Trình Công Việc Không Phù Hợp (NCR)

#### 4.1. Phân Quyền NCR
- [ ] **Phân quyền trách nhiệm, quyền hạn**
  - [ ] NCR roles (Reporter, Investigator, Approver)
  - [ ] Permission management for NCR
  - [ ] Notification to responsible persons

#### 4.2. Quy Trình NCR
- [ ] **Thực hiện các bước của quy trình NCR**
  - [x] NCR entity (NcrNumber, Description, RootCause, etc.)
  - [ ] NCR CRUD operations
  - [ ] NCR workflow (Open → InProgress → Closed)
  - [ ] CAPA (Corrective & Preventive Actions)
  - [ ] Effectiveness verification

#### 4.3. Quản Lý Hồ Sơ NCR
- [ ] **Quản lý hồ sơ công việc không phù hợp**
  - [ ] NCR attachments
  - [ ] Investigation reports
  - [ ] Evidence collection
  - [ ] NCR archive

#### 4.4. Thống Kê NCR
- [ ] **Quản lý thống kê, báo cáo về NCR**
  - [ ] NCR summary dashboard
  - [ ] NCR trends by time
  - [ ] NCR by type/category
  - [ ] Root cause analysis (Pareto chart)
  - [ ] Effectiveness rate

---

### 5. Quản Lý Mẫu Thử Nghiệm

#### 5.1. Quản Lý Lifecycle Mẫu
- [ ] **Quản lý mẫu lưu, thanh lý, hủy, trả khách**
  - [x] Specimen entity with Status enum
  - [ ] Specimen CRUD operations
  - [ ] Status transitions (Received → Testing → Completed → Disposed/Returned)
  - [ ] Storage management
  - [ ] Disposal procedure
  - [ ] Return to customer procedure

#### 5.2. Theo Dõi Mẫu
- [ ] **Theo dõi tình trạng mẫu, vị trí, lịch sử**
  - [ ] Real-time specimen location tracking
  - [ ] Storage location management
  - [ ] Transfer history
  - [x] SpecimenHistory entity
  - [ ] SpecimenHistory UI

#### 5.3. Ghi Nhận Sai Lệch
- [ ] **Ghi nhận sai lệch của mẫu**
  - [ ] Deviation tracking
  - [ ] Deviation types (quantity, condition, etc.)
  - [ ] Deviation handling workflow
  - [ ] Link deviation to NCR

---

### 6. Thống Kê & Báo Cáo

#### 6.1. Trích Xuất Dữ Liệu
- [ ] **Trích xuất dữ liệu yêu cầu, mẫu, phép thử**
  - [ ] Export to Excel (TestRequest, Specimen, Tests)
  - [ ] Export to PDF
  - [ ] Custom report builder
  - [ ] Scheduled reports

#### 6.2. Theo Dõi Tình Trạng
- [ ] **Theo dõi tình trạng thử nghiệm**
  - [ ] Dashboard: số lượng mẫu qua thời gian
  - [ ] Dashboard: số lượng phép thử
  - [ ] Charts by sample type
  - [ ] Workload distribution

#### 6.3. Theo Dõi Công Việc Bộ Phận
- [ ] **Theo dõi công việc của mỗi bộ phận**
  - [ ] Department performance dashboard
  - [ ] Individual user performance
  - [ ] Productivity metrics
  - [ ] Bottleneck identification

---

## III. YÊU CẦU DỊCH VỤ ĐÀO TẠO

### Đào Tạo
- [ ] **Đào tạo tại chỗ** cho nhân sự
  - [ ] Training materials preparation
  - [ ] Hands-on training sessions
  - [ ] Training completion tracking

- [ ] **Đào tạo từ xa**
  - [ ] Video tutorials
  - [ ] Online help system
  - [ ] FAQ documentation
  - [ ] Remote support tools

---

## IV. YÊU CẦU BẢN QUYỀN PHẦN MỀM

### Copyright & Licensing
- [ ] Chứng nhận quyền tác giả, sở hữu trí tuệ
- [ ] Giấy ủy quyền phòng thí nghiệm sử dụng phần mềm
- [x] Phát triển bởi doanh nghiệp pháp nhân tại Việt Nam

---

## V. YÊU CẦU BẢO HÀNH

### Bảo Hành & Hỗ Trợ
- [ ] **Thời gian bảo hành:** 12 tháng
- [ ] **SLA:** Khắc phục sự cố trong 24 giờ
- [ ] **Hỗ trợ từ xa:** Hướng dẫn sử dụng
- [ ] **Nhân sự:** Trình độ phù hợp chuyên ngành
- [ ] **Cam kết:** Hỗ trợ và cập nhật tối thiểu 10 năm

---

## 📊 Tổng Quan Tiến Độ

### Tính Theo Module

| Module | Total Features | Completed | In Progress | Not Started | % Complete |
|--------|----------------|-----------|-------------|-------------|------------|
| **I. Yêu Cầu Chung** | 12 | 3 | 0 | 9 | 25% |
| **II.1. Tiếp Nhận YC** | 6 | 2 | 0 | 4 | 33% |
| **II.2. Phòng Thí Nghiệm** | 4 | 0 | 0 | 4 | 0% |
| **II.3. Báo Cáo KQ** | 4 | 0 | 0 | 4 | 0% |
| **II.4. NCR** | 4 | 1 | 0 | 3 | 25% |
| **II.5. Quản Lý Mẫu** | 3 | 1 | 0 | 2 | 33% |
| **II.6. Thống Kê** | 3 | 0 | 0 | 3 | 0% |
| **III. Đào Tạo** | 2 | 0 | 0 | 2 | 0% |
| **IV. Bản Quyền** | 3 | 1 | 0 | 2 | 33% |
| **V. Bảo Hành** | 5 | 0 | 0 | 5 | 0% |
| **TỔNG CỘNG** | **46** | **8** | **0** | **38** | **17%** |

### Priority Roadmap

**🔴 HIGH PRIORITY (Phase 3 - Next)**
1. Specimen Management CRUD
2. Test Assignment & Execution
3. Result Entry & Review
4. Two-step Approval Workflow

**🟡 MEDIUM PRIORITY (Phase 4)**
1. Report Generation (PDF templates)
2. QR Code traceability
3. File upload/attachments
4. NCR workflow implementation
5. Master data management

**🟢 LOW PRIORITY (Phase 5)**
1. Import from Excel
2. Advanced statistics & charts
3. Custom dashboard configuration
4. Training materials
5. Performance optimization for 20+ users

---

**Cập nhật:** 11/12/2024
**Next Review:** Khi hoàn thành Phase 3

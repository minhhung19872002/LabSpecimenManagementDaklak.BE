# OpenIddict Token Signing & Encryption Keys

## ⚠️ QUAN TRỌNG - PHẢI ĐỌC TRƯỚC KHI DEPLOY

### Vấn đề
Khi deploy lại server, nếu không có persistent keys, OpenIddict sẽ tự động generate keys mới → **tất cả token cũ bị invalid ngay lập tức** → user phải login lại.

### Giải pháp
Hệ thống đã được cấu hình để **lưu signing/encryption keys** trong thư mục `Keys/`:
- `Keys/signing.key` - Signing key (512 bits)
- `Keys/encryption.key` - Encryption key (256 bits)

Các keys này sẽ được:
- **Tự động tạo lần đầu** khi chạy ứng dụng
- **Lưu vào file** để tái sử dụng giữa các lần deploy
- **Không commit lên Git** (đã thêm vào .gitignore)

## 📋 Hướng dẫn Deploy Production

### 1. Lần Deploy Đầu Tiên
```bash
# 1. Deploy code lên server
# 2. Chạy ứng dụng - keys sẽ tự động được tạo
dotnet run

# 3. SAO LƯU KEYS NGAY LẬP TỨC!
# Copy 2 files này ra ngoài server để backup
Keys/signing.key
Keys/encryption.key
```

### 2. Các Lần Deploy Tiếp Theo
```bash
# 1. ĐỪNG XOÁ thư mục Keys/ trên server
# 2. Deploy code mới (giữ nguyên Keys/)
# 3. Restart ứng dụng
dotnet run

# Nếu vô tình mất Keys/:
# - Copy 2 files backup vào Keys/
# - Restart ứng dụng
```

### 3. Trường Hợp Khẩn Cấp - Mất Keys
Nếu bạn **mất hoàn toàn** keys backup:

```bash
# 1. Cho phép ứng dụng tạo keys mới
rm -rf Keys/
dotnet run

# 2. TẤT CẢ USER PHẢI LOGIN LẠI (không thể tránh)

# 3. SAO LƯU KEYS MỚI NGAY!
```

## 🔒 Bảo Mật Keys

### ✅ Nên làm:
- Sao lưu keys ở nơi **an toàn, riêng tư** (không phải Git)
- **Mã hoá** files backup nếu có thể
- Giới hạn quyền truy cập: `chmod 600 Keys/*.key`
- Dùng **environment-specific keys** (dev ≠ prod)

### ❌ KHÔNG nên:
- ❌ Commit keys lên Git/GitHub
- ❌ Share keys qua email/chat
- ❌ Dùng chung keys giữa dev và production
- ❌ Để keys trong Docker image

## 📂 Cấu trúc Server

```
/var/www/LabSpecimenManagement/
├── Keys/                          # Keys folder (persistent)
│   ├── signing.key               # Signing key
│   └── encryption.key            # Encryption key
├── src/
│   └── LabSpecimenManagement.HttpApi.Host/
│       ├── appsettings.json
│       └── ...
└── backup/                        # Backup folder (tuỳ chọn)
    └── Keys_backup_2026-03-05/   # Keys backup với timestamp
        ├── signing.key
        └── encryption.key
```

## 🔄 Token Lifetimes

Hiện tại đang cấu hình:
- **Access Token**: 8 giờ (short-lived for security)
- **Refresh Token**: 90 ngày (long-lived to minimize re-login)

User chỉ cần login lại khi:
- Không dùng app trong 90 ngày
- Manual logout
- Clear browser cache
- **KHÔNG cần login lại** khi deploy/restart server (nhờ persistent keys)

## 🐳 Docker Deployment

Nếu deploy bằng Docker, mount Keys/ ra ngoài:

```yaml
services:
  backend:
    image: lab-specimen-backend
    volumes:
      - ./Keys:/app/Keys  # Mount Keys folder
      - ...
```

## ⚡ Troubleshooting

### Lỗi: "Token signature is invalid"
**Nguyên nhân**: Keys đã bị thay đổi (generate mới)  
**Giải pháp**: 
1. Restore keys từ backup
2. Hoặc để user login lại với keys mới

### Lỗi: "Could not open file Keys/signing.key"
**Nguyên nhân**: File permissions  
**Giải pháp**:
```bash
chmod 600 Keys/*.key
chown www-data:www-data Keys/*  # Ubuntu/nginx
```

### Lỗi: "The encryption key is invalid"
**Nguyên nhân**: File bị corrupt  
**Giải pháp**: Restore từ backup hoặc xoá để generate mới

## 📞 Liên hệ
Nếu có vấn đề với keys hoặc authentication, liên hệ team dev.

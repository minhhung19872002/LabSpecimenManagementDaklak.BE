# Auto-Generate và Persist OpenIddict Keys

## Cách hoạt động

Server sẽ **tự động generate keys** lần đầu tiên khi start, sau đó **tái sử dụng** keys đó cho các lần restart sau.

### Logic
1. **Lần đầu tiên start server** (chưa có keys):
   - Tự động generate 2 keys (encryption + signing)
   - Lưu vào folder `Keys/` trong thư mục backend
   - In ra console để backup (optional)

2. **Các lần restart sau**:
   - Load keys từ `Keys/encryption.key` và `Keys/signing.key`
   - Token của users vẫn valid, không cần đăng nhập lại ✅

### Kiểm tra logs
```
[OpenIddict] Using encryption key from file: /app/Keys/encryption.key
[OpenIddict] Using signing key from file: /app/Keys/signing.key
```

---

## Deployment Scenarios

### 1. Bare Metal / VM (Không dùng Docker)

**Không cần config gì thêm!** ✅

- Lần đầu deploy: Server tạo folder `Keys/` và generate keys
- Lần sau deploy: Server load keys từ folder `Keys/` (không tạo mới)
- Token users vẫn valid

### 2. Docker Container (Đơn giản nhất)

**Cần mount volume cho folder Keys/**

#### Dockerfile
Tạo folder Keys trong container:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY . .

# Create Keys directory for persistent storage
RUN mkdir -p /app/Keys

EXPOSE 80
ENTRYPOINT ["dotnet", "LabSpecimenManagement.HttpApi.Host.dll"]
```

#### Docker Run
```bash
docker run -d \
  -p 80:80 \
  -v /path/on/host/keys:/app/Keys \
  your-backend-image
```

#### Docker Compose
```yaml
services:
  backend:
    image: your-backend-image
    ports:
      - "80:80"
    volumes:
      - ./backend-keys:/app/Keys  # Persist keys locally
    environment:
      - ConnectionStrings__Default=...
```

**Lưu ý**: Folder `/path/on/host/keys` trên server sẽ lưu keys. Container restart → keys vẫn còn.

### 3. CI/CD Deploy (GitHub Actions / GitLab CI)

#### Scenario A: Deploy lên VM/Server trực tiếp
```yaml
# .github/workflows/deploy.yml
- name: Deploy Backend
  run: |
    # Copy code to server (giữ nguyên folder Keys/)
    rsync -avz --exclude 'Keys/' ./src/LabSpecimenManagement.HttpApi.Host/ user@server:/app/
    
    # Restart server
    ssh user@server 'systemctl restart backend'
```

**Keys folder sẽ:**
- Lần đầu deploy: Server tạo keys mới
- Lần sau deploy: Keys vẫn còn trong `/app/Keys/`, không bị overwrite

#### Scenario B: Deploy Docker Container
```yaml
# .github/workflows/deploy.yml
- name: Build and Push Docker
  run: |
    docker build -t registry.com/backend:latest .
    docker push registry.com/backend:latest

- name: Deploy on Server
  run: |
    ssh user@server << 'EOF'
      # Tạo folder keys trên host (lần đầu)
      mkdir -p ~/backend-keys
      
      # Pull và run container với volume mount
      docker pull registry.com/backend:latest
      docker stop backend || true
      docker rm backend || true
      docker run -d \
        --name backend \
        -p 80:80 \
        -v ~/backend-keys:/app/Keys \
        registry.com/backend:latest
    EOF
```

**Keys sẽ lưu ở `~/backend-keys` trên server** → Container restart → keys vẫn còn

### 4. Kubernetes / Docker Swarm

Dùng **PersistentVolume** để mount folder Keys:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: backend-keys-pvc
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 100Mi
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: backend
spec:
  template:
    spec:
      containers:
      - name: backend
        image: your-backend-image
        volumeMounts:
        - name: keys-storage
          mountPath: /app/Keys
      volumes:
      - name: keys-storage
        persistentVolumeClaim:
          claimName: backend-keys-pvc
```

---

## Backup Keys (Optional nhưng nên làm)

### Lần đầu deploy, copy keys sang nơi an toàn:

```bash
# SSH vào server
ssh user@server

# Copy keys từ container ra host
docker cp backend:/app/Keys ./backend-keys-backup

# Hoặc nếu không dùng Docker
cd /app/Keys
cp encryption.key signing.key ~/keys-backup/

# Lưu vào password manager hoặc nơi an toàn
```

### Restore keys khi cần:
```bash
# Copy keys vào lại server
scp -r ./backend-keys-backup user@server:/app/Keys/

# Restart backend
docker restart backend
# Hoặc: systemctl restart backend
```

---

## Troubleshooting

### Vấn đề: Users bị logout sau khi deploy

**Nguyên nhân**: Keys bị mất hoặc tạo mới

**Kiểm tra**:
```bash
# Check logs
docker logs backend | grep OpenIddict

# Nếu thấy "Generated NEW encryption key" → Keys bị tạo mới
# Nếu thấy "Using encryption key from file" → Keys được tái sử dụng ✅
```

**Giải pháp**:
1. Đảm bảo folder `Keys/` được persist (volume mount cho Docker)
2. Không xóa folder `Keys/` khi deploy
3. Backup keys và restore lại nếu bị mất

### Vấn đề: Keys không được tạo ra

**Nguyên nhân**: Permission denied

**Giải pháp**:
```bash
# Đảm bảo folder Keys có quyền write
chmod 755 /app/Keys

# Container: Run với user có quyền write
docker run -u $(id -u):$(id -g) ...
```

---

## So sánh với Environment Variables

| Phương pháp | Auto-Generate | Tự động Persist | Cần config CI/CD |
|-------------|---------------|-----------------|------------------|
| **File-based (Keys/)** | ✅ Tự động | ✅ Tự động (nếu mount volume) | ❌ Không cần (chỉ mount volume) |
| **Environment Variables** | ❌ Phải manual generate | ✅ Lưu ở CI/CD secrets | ✅ Phải config secrets |

**Khuyến nghị**: 
- **Development**: Dùng file-based (đơn giản)
- **Production**: Dùng file-based + backup keys, hoặc environment variables nếu muốn control chặt chẽ

---

## Tóm tắt

✅ Code đã sẵn sàng auto-generate keys  
✅ Chỉ cần đảm bảo folder `Keys/` được persist giữa các lần deploy  
✅ Docker: Mount volume cho `/app/Keys`  
✅ Bare metal: Keys tự động lưu, không cần config gì thêm  

**Lần đầu deploy** → Generate keys mới  
**Lần sau deploy** → Tái sử dụng keys cũ  
**Users** → Không bị logout ✅

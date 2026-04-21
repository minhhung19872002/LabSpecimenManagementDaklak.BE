# StarLab — Deployment Status

Trạng thái deployment tại thời điểm 2026-04-21. Đọc file này để biết app đang host ở đâu và còn gì chưa làm.

## Deployed Environment: Google Cloud VM

**Project ID**: `project-2dda251d-635d-4007-90c` (tên: StarLabUAT)
**Account GCP**: `phihung19872002@gmail.com`
**VM**: `starlab-vm` — `e2-standard-2` (2 vCPU / 8GB RAM) — zone `asia-southeast1-a` (Singapore)
**Public IP**: `136.110.9.243` (**static** — reserved name `starlab-static` ở region `asia-southeast1`, miễn phí khi còn gắn VM)
**Domain**: `tihelab.vn` — 3 A record trỏ về `136.110.9.243`: `uat`, `api-uat`, `collabora-uat`

### URLs (production)

| Service | URL | Ghi chú |
|---|---|---|
| Frontend | https://uat.tihelab.vn | React + Vite + nginx (qua Caddy) |
| Backend API | https://api-uat.tihelab.vn | .NET 9, ABP framework (qua Caddy) |
| Collabora | https://collabora-uat.tihelab.vn | Document editor (qua Caddy, WebSocket proxy) |
| Gotenberg | internal :3100 | PDF convert (không expose) |

HTTPS terminate tại Caddy container (`lab-specimen-caddy`, image `caddy:2-alpine`), auto cert Let's Encrypt (account `phihung19872002@gmail.com`), auto renew. Cert hiện tại: `notAfter=Jul 20 2026 GMT`.

### URLs (legacy, vẫn chạy)

Các IP+port cũ vẫn mở cho debug: `http://136.110.9.243:{8080,5002,8089}`. Đã include trong CORS allowlist. Có thể đóng sau khi domain ổn định (xóa `ports:` mapping của FE/BE/Collabora trong compose, chỉ để Caddy expose 80/443).

### SSH vào VM

```bash
ssh -i C:/Users/ADMIN/.ssh/starlab_gcp ubuntu@136.110.9.243
```

Private key: `C:/Users/ADMIN/.ssh/starlab_gcp`
Public key: `C:/Users/ADMIN/.ssh/starlab_gcp.pub`

### Stack trên VM

- Folder gốc: `/home/ubuntu/starlab/`
  - `docker-compose.yml` — đã overwrite bản UAT (2026-04-21): thêm service `caddy`, update CORS/RedirectAllowedUrls với domain HTTPS, bật `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` cho BE trust `X-Forwarded-Proto` từ Caddy. Backup cũ: `docker-compose.yml.bak-YYYYMMDD-HHMMSS`
  - `Caddyfile` — reverse proxy 3 subdomain → frontend/backend/collabora, email ACME `phihung19872002@gmail.com`
  - `.env` — `BACKEND_URL=https://api-uat.tihelab.vn`, `FRONTEND_API_URL=https://api-uat.tihelab.vn`; còn chứa passwords random (xem file trên VM để lấy). Backup: `.env.bak-YYYYMMDD-HHMMSS`
  - `lab-specimen-management-be/` — BE source (tar upload, **chưa phải git clone**)
  - `lab-specimen-management-fe/` — FE source (tar upload, **chưa phải git clone**)

- Containers: `postgres`, `redis`, `mongodb`, `gotenberg`, `collabora`, `db-migrator` (chạy migration rồi exit), `backend`, `frontend`, `caddy`. Tất cả healthy lần deploy cuối (2026-04-21).

- Bản local của file đã deploy: `C:/Source/StarLab/.deploy-uat/{Caddyfile,docker-compose.yml}` — nếu cần sửa, edit local rồi `scp` lên VM.

### Compose commands (chạy trên VM)

```bash
cd ~/starlab
sudo docker compose ps           # xem status
sudo docker compose logs -f backend
sudo docker compose restart backend
sudo docker compose up -d --build backend   # rebuild 1 service
sudo docker compose down         # dừng toàn bộ
```

## Source code

- **Local**: `C:/Source/StarLab/LabSpecimenManagementDaklak.{BE,FE}/`
- **GitHub (private)**:
  - BE: `github.com/homedew/LabSpecimenManagementDaklak.BE`
  - FE: `github.com/homedew/LabSpecimenManagementDaklak.FE`
- User không phải admin của 2 repo này — không tự thêm được Deploy keys / Secrets

## CI/CD — GitHub Actions → SSH VM (active)

**Repos (đã transfer sang user)**:
- BE: `github.com/minhhung19872002/LabSpecimenManagementDaklak.BE`
- FE: `github.com/minhhung19872002/LabSpecimenManagementDaklak.FE`

**Flow**: push branch `develop` → workflow trigger → SSH VM → `git pull` repo tương ứng + `docker compose up -d --build --no-deps <backend|frontend>`.

### SSH keys trên VM
- `~/.ssh/gh_be_deploy{,.pub}` — VM đọc BE repo (GitHub Deploy key `starlab-vm-deploy`, read-only)
- `~/.ssh/gh_fe_deploy{,.pub}` — VM đọc FE repo (GitHub Deploy key `starlab-vm-deploy`, read-only)
- `~/.ssh/actions_deploy{,.pub}` — GitHub Actions SSH vào VM. Public key trong `~/.ssh/authorized_keys` có prefix `restrict,command="/home/ubuntu/starlab/gh-deploy.sh"` → chỉ chạy được deploy script, không mở được shell. Private key được lưu làm GitHub Secret `VM_SSH_KEY` trên cả 2 repo.

SSH config `~/.ssh/config` có alias `github-be` + `github-fe` để git dùng đúng deploy key cho từng repo.

### Deploy script
`~/starlab/gh-deploy.sh` — nhận service qua `SSH_ORIGINAL_COMMAND` (`backend` | `frontend`), git fetch + reset --hard origin/develop trên repo tương ứng, rồi `docker compose up -d --build --no-deps <service>`. `--no-deps` quan trọng: tránh race condition khi BE và FE workflow chạy song song (cả 2 rebuild backend → conflict container name).

Local backup: `C:/Source/StarLab/.deploy-uat/gh-deploy.sh`.

### GitHub Secrets (cả 2 repo)
- `VM_HOST=136.110.9.243`
- `VM_USER=ubuntu`
- `VM_SSH_KEY` = private key `actions_deploy`

### Workflow files
- BE: `.github/workflows/backend-deploy-uat.yml` (đã overwrite file cũ)
- FE: `.github/workflows/frontend-deploy-uat.yml` (đã overwrite file cũ)
- Local copy: `C:/Source/StarLab/.deploy-uat/{backend,frontend}-deploy-uat.yml`
- Trigger: `push` branch `develop` với path filter (src/**, Dockerfile, nginx.conf, package*.json, workflow file); hoặc `workflow_dispatch` manual

### Workflow cũ còn tồn tại
- Cả 2 repo còn `deploy-azure.yml` trigger trên push `main` → Azure AKS. Nếu không dùng Azure nữa, delete. **Security**: FE `frontend-deploy-uat.yml` cũ (đã overwrite) có committed password `Vn8$$RkM9@aP2!L7Z` — vẫn nằm trong git history. Nếu reuse password đâu đó, rotate ngay. Để xoá khỏi history cần rewrite (BFG / git filter-repo) hoặc chấp nhận.

### Compose không nằm trong CI/CD
`docker-compose.yml` + `Caddyfile` chỉ sống trên VM + local `.deploy-uat/`, không versioned với BE/FE repo. Nếu cần sửa, edit local `.deploy-uat/` rồi `scp` lên VM. CI/CD không động vào 2 file này.

## TODO khác

- **Credit free**: account mới có $300 credit 90 ngày. `e2-standard-2` tốn ~$49/tháng → chạy free ~6 tháng. Sau đó cần gia hạn billing hoặc downgrade.
- **Dọn port cũ**: FE/BE/Collabora vẫn expose port 8080/5002/8089 ra public để backward compat. Sau khi domain HTTPS ổn định, có thể:
  - Xóa các `ports:` mapping của FE/BE/Collabora trong `docker-compose.yml` (giữ nội bộ qua Caddy)
  - Xóa tcp:5002/8080/8089 khỏi firewall rule `starlab-allow-http-https` (chỉ giữ 80/443)
  - Xóa origin `http://136.110.9.243:*` khỏi `App__CorsOrigins`/`App__RedirectAllowedUrls`
- **Collabora trong ABP**: nếu BE có config hardcode Collabora URL (endpoint WOPI discovery) thì phải check đã đổi sang `https://collabora-uat.tihelab.vn` chưa. Chưa verify lần deploy này — nếu editor document không load, grep source BE cho `8089` hoặc `collabora`.

## DONE — các mốc đã hoàn thành

- **2026-04-21** — Reserve static IP `136.110.9.243` (name `starlab-static`). VM không bị restart.
- **2026-04-21** — Setup HTTPS với Caddy + Let's Encrypt cho 3 subdomain `uat.tihelab.vn`, `api-uat.tihelab.vn`, `collabora-uat.tihelab.vn`. Cert 90 ngày auto renew.
- **2026-04-21** — Compose CORS đã clean: bỏ `34.59.178.231` và `starlab.bluestar.com.vn`, thêm HTTPS URLs.
- **2026-04-21** — VM chuyển từ tar-extracted sang git clone (SSH Deploy key read-only). Backup tar: `lab-specimen-management-{be,fe}.tarbak/` trên VM.
- **2026-04-21** — CI/CD GitHub Actions SSH → VM hoàn chỉnh. Push `develop` trên BE/FE repo auto deploy service tương ứng. Key restricted bằng `command=` trong authorized_keys.
- **2026-04-21** — Fix FE healthcheck: `localhost:8080` → `127.0.0.1:8080` (busybox wget trong alpine resolve IPv6 trước, nginx chỉ listen IPv4 → healthcheck report unhealthy dù FE chạy OK).

## TODO — Oracle Cloud (đã dừng)

User ban đầu muốn deploy lên Oracle Cloud Always Free (Ampere A1.Flex 4 OCPU/24GB, free vĩnh viễn).

Setup đã xong:
- File API key: `C:/Users/ADMIN/.oci/oci_api_key.pem` + config: `C:/Users/ADMIN/.oci/config_container`
- OCI CLI wrapper: `C:/Users/ADMIN/oci.sh` (dùng docker image `ghcr.io/oracle/oci-cli`)
- SSH key cho VM Oracle: `C:/Users/ADMIN/.ssh/starlab_vm{,.pub}`
- Account: `minhhung19872002@gmail.com`, region `ap-tokyo-1`, VCN `his-pacs-vcn` sẵn có

Đã dừng vì: Ampere ở Tokyo **hết capacity liên tục** (đã thử 20+ lần, luôn báo "Out of host capacity"). Always Free tier chỉ cho subscribe 1 region → không chuyển Singapore/Osaka được.

Nếu muốn quay lại Oracle: cần upgrade tenancy lên PAYG (add credit card, vẫn free nếu ở trong Always Free quota) → subscribe region khác có capacity.

Script retry có sẵn tại `C:/Users/ADMIN/oci_retry_launch.sh` (hiện không chạy).

## Lần sau muốn tiếp CI/CD thì làm gì

1. Quyết Option A hay Option B (khuyên A)
2. Nếu A + deploy key: bạn xin admin `homedew` đồng ý add deploy key, mình sẽ generate key trên VM, bạn gửi pubkey cho admin
3. Nếu A + PAT: bạn tự tạo PAT ở `github.com/settings/tokens` (classic, scope `repo`), gửi mình, mình config VM
4. Mình sẽ: re-clone 2 repo trên VM (thay tar-extracted), viết script poll + cron, test

Sau đó mỗi lần push code lên `main` → vài phút sau tự deploy lên `136.110.9.243`.

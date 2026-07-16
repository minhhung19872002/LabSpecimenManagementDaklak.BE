# StarLab — Deployment Status

Trạng thái deployment tại thời điểm 2026-04-21. Đọc file này để biết app đang host ở đâu và còn gì chưa làm.

---

## 🔵 OFFLINE-FIRST — In phiếu offline (cập nhật 2026-06-24) — ĐANG LÀM, MAI TIẾP

Mục tiêu: KTV in phiếu (Yêu cầu + Kết quả) khi MẤT MẠNG. Thay Collabora (server, không offline được) bằng editor client-side **eigenpal** (Apache-2.0). Tất cả ở **FE, nhánh `develop`** (đã push origin/develop, UAT auto-deploy). `main` KHÔNG đụng.

### Đã làm (FE develop, commit d24f57d → 53aa342)
- **Engine fill client-side**: `src/services/offline/` — `htmlToOoxml.ts` (HTML→OOXML: đậm/nghiêng/sub-sup/xuống dòng), `docxFill.ts` (docxtemplater: loop `{{#rows}}` + raw-xml `{{@field}}`), `resultFormBuilder.ts` (bảng kết quả cha/con), `resultTemplatePrep.ts` / `requestTemplatePrep.ts` (transform template→loop), `requestFormBuilder.ts` (bảng mẫu PHẲNG cho phiếu yêu cầu), `offlineResultService.ts` (cache approved indicators theo testRequestId + generateResultDraft), `connectivity.ts` (isServerReachable — ping api-uat).
- **Editor offline**: `src/components/OfflineDocxEditor/` (eigenpal). `DocumentEditor` thêm mode `?offlineDraft=<id>` (load từ IndexedDB `resultDrafts` store, db v5) + nút Lưu nháp / Tải xuống .docx.
- **Phát hiện offline tin cậy**: `navigator.onLine` KHÔNG đáng tin (Docker/VPN adapter, wifi-no-internet → true). `isServerReachable()` ping `${API}/health` cross-origin mode:'no-cors' (SW không cache cross-origin). Dùng ở handlePrintTestRequest + PrintIndicatorModal.
- **Auto-cache**: `useTestRequestList` khi online tự cache approved indicators các phiếu in-được (khỏi phải mở từng phiếu trước khi offline).
- **In phiếu KẾT QUẢ offline**: PrintIndicatorModal (nút "In KQ"/"In KQ Hỗn hợp") → offline → generateResultDraft → editor offline. Builder đầy đủ (cha/con/sub-sup), data từ getApprovedIndicators (QualityManagementIndicatorDto).
- **In phiếu YÊU CẦU offline**: TestRequestDetail "In phiếu TN" → offline → bảng mẫu PHẲNG (mỗi mẫu 1 dòng, chỉ tiêu+đơn giá dạng text) → editor offline. (Bảng nested gốc + split-run quá fragile nên dùng phẳng.)

### Đã FIX hạ tầng UAT (in ONLINE)
- BE UAT chạy `ASPNETCORE_ENVIRONMENT=Production` → đọc appsettings.Production → Collabora `collabora.tihelab.vn` (CSP `frame-ancestors` chặn subdomain `uat.tihelab.vn`) → editor trắng. **FIX**: thêm env backend trong `/home/ubuntu/starlab/docker-compose.yml` trên VM: `Collabora__ServerUrl=https://collabora-uat.tihelab.vn` + `Collabora__WopiBaseUrl=https://api-uat.tihelab.vn` → recreate backend. (collabora-uat frame-ancestors=* → cho uat nhúng.) Backup: `docker-compose.yml.bak-20260624-133730`. Đã đồng bộ vào `.deploy-uat/docker-compose.yml`. ⚠️ compose trên VM KHÔNG nằm trong CI/CD (gh-deploy chỉ reset source + rebuild) nên env này bền qua deploy code, nhưng nếu overwrite compose phải thêm lại.

### Đã verify
- In ONLINE UAT: OK (user xác nhận). File .docx offline mở Word: watermark/layout ĐÚNG.
- Engine fill + builder + transform: verify e2e bằng POC node tại `C:/Source/StarLab/poc-fill-offline/` (template BM01 kết quả + BM05 yêu cầu thật) — bảng/sub-sup/viền nét đứt giữ, không undefined.

### TODO — mai làm tiếp
- **User test lại OFFLINE trên UAT** sau bundle 53aa342 (nhớ update Service Worker: đóng hết tab/Update mới lấy bundle mới): In phiếu TN + In KQ offline → ra data đúng không.
- Phiếu KẾT QUẢ offline: cột "Phương pháp" hiện trống ({{TestMethod}} chưa map); header tối thiểu; offline mỗi chỉ tiêu 1 dòng (QualityManagementIndicatorDto thiếu parentIndicatorId → chưa gộp cha/con).
- Cân nhắc tối ưu auto-cache (đang gọi getApprovedIndicators cho từng phiếu in-được khi xem danh sách — có thể nặng nếu danh sách lớn; cân nhắc BE batch endpoint).
- Sau khi UAT OK → bàn merge develop→main (deploy prod Azure). Lưu ý prod Azure dùng appsettings.Production (collabora.tihelab.vn) — KHÔNG đụng (env override chỉ ở VM UAT).
- Chi tiết đầy đủ: memory `~/.claude/projects/C--Source-StarLab/memory/offline-docx-editor-decision.md`.

---

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
- ~~**Collabora trong ABP**: check Collabora URL đã đổi sang `https://collabora-uat.tihelab.vn` chưa.~~ ✅ **ĐÃ FIX 2026-06-24** — xem section "OFFLINE-FIRST" ở đầu file (thêm env `Collabora__ServerUrl`/`Collabora__WopiBaseUrl` vào backend trong docker-compose VM). In online UAT đã chạy.

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

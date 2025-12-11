# Lab Specimen Management System - Default Accounts

## 🔐 Default Login Credentials

### Admin Account
- **Username:** `admin`
- **Email:** `admin@abp.io`
- **Password:** `1q2w3E*`
- **Roles:** Admin (Full permissions)
- **Description:** Super administrator account with full system access

---

## 🧪 How to Test Login

### Option 1: Via Swagger UI (Backend API)
1. Open: http://localhost:5002/swagger
2. Click **"Authorize"** button at the top right
3. Fill in the form:
   ```
   username: admin
   password: 1q2w3E*
   grant_type: password
   scope: LabSpecimenManagement
   client_id: LabSpecimenManagement_App
   ```
4. Click **"Authorize"**
5. If successful, you'll receive an access token
6. You can now test all API endpoints

### Option 2: Via Frontend (Coming in Phase 2)
1. Open: http://localhost:3001/login
2. Enter credentials:
   - Username: `admin`
   - Password: `1q2w3E*`
3. Click "Đăng nhập" (Login)
4. Will redirect to Dashboard

### Option 3: Via cURL/Postman
```bash
curl -X POST http://localhost:5002/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "username=admin" \
  -d "password=1q2w3E*" \
  -d "client_id=LabSpecimenManagement_App" \
  -d "scope=LabSpecimenManagement"
```

**Expected Response:**
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 31536000,
  "scope": "LabSpecimenManagement"
}
```

---

## 📋 OAuth2 Configuration

### Client Configuration
- **Client ID:** `LabSpecimenManagement_App`
- **Grant Types:** `password`, `client_credentials`, `authorization_code`, `refresh_token`
- **Scopes:** `LabSpecimenManagement`
- **Token Endpoint:** `http://localhost:5002/connect/token`
- **Authorization Endpoint:** `http://localhost:5002/connect/authorize`
- **UserInfo Endpoint:** `http://localhost:5002/connect/userinfo`

---

## ⚠️ Important Notes

1. **Password is case-sensitive:** Must use `1q2w3E*` (capital E)
2. **Default account created automatically** by DbMigrator on first run
3. **Change password in production** for security reasons
4. **Token expires in:** 1 year (31536000 seconds) in development
5. **CORS enabled for:** `http://localhost:3001`, `http://localhost:3000`

---

## 🔧 Password Requirements (ABP Default)

- Minimum length: 6 characters
- Must contain:
  - At least 1 uppercase letter
  - At least 1 lowercase letter
  - At least 1 digit
  - At least 1 special character

Example valid passwords:
- `1q2w3E*` (default)
- `Admin123!`
- `Pass@word1`

---

## 👥 User Management (Coming Soon)

In future phases, you'll be able to:
- Create new users via UI
- Assign roles and permissions
- Manage user profiles
- Reset passwords
- Enable/disable accounts

---

## 🔒 Security Best Practices

For production deployment:
1. **Change default password immediately**
2. Enable 2FA (Two-Factor Authentication)
3. Set shorter token expiration time
4. Use HTTPS only
5. Implement password rotation policy
6. Enable account lockout after failed attempts
7. Use strong passwords (12+ characters)

---

## 📞 Support

If you encounter login issues:
1. Check backend API is running: http://localhost:5002/swagger
2. Verify database is accessible: `docker ps` (check lab_postgres)
3. Check DbMigrator ran successfully
4. Review logs in console output
5. Try password reset (coming in Phase 2)

---

*Last Updated: 2025-12-11*

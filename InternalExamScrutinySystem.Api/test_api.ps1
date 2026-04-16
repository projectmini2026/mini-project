$ErrorActionPreference = "Stop"

$loginBody = @{
    email = "hod@college.edu"
    password = "Password123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "https://localhost:7225/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -SkipCertificateCheck
$token = $loginResponse.data.token

$headers = @{
    Authorization = "Bearer $token"
}

$modules = Invoke-RestMethod -Uri "https://localhost:7225/api/hod/modules" -Headers $headers -SkipCertificateCheck
$modules.data | ConvertTo-Json -Depth 5 > modules_test.json

$faculties = Invoke-RestMethod -Uri "https://localhost:7225/api/hod/faculties" -Headers $headers -SkipCertificateCheck
$faculties.data | ConvertTo-Json -Depth 5 > faculties_test.json

Write-Host "Success"

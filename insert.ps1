$baseUrl = "http://localhost:5000/api"
$Headers = @{ "Content-Type" = "application/json" }

$regBody = @{
    nombre = "Admin"
    apellido = "Sistema"
    nroDocumento = "12345678"
    email = "admin@subastas.com"
    password = "Admin123!"
    idTipoPersona = 1
    idTipoDocumento = 1
    idRol = 1
} | ConvertTo-Json

try { 
    Invoke-RestMethod -Uri "$baseUrl/Auth/register" -Method Post -Body $regBody -Headers $Headers -ErrorAction Stop | Out-Null
    Write-Host "Usuario registrado."
} catch { 
    $err = $_.Exception.Message
    Write-Host "Usuario ya existe o falló: $err" 
}

$loginBody = @{ email = "admin@subastas.com"; password = "Admin123!" } | ConvertTo-Json
try {
    $loginResult = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method Post -Body $loginBody -Headers $Headers
    $token = $loginResult.data.token
    Write-Host "Login exitoso. Insertando vigencias..."
} catch {
    $err = $_.Exception.Message
    Write-Host "Fallo login: $err"
    exit
}

$AuthHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

for ($year = 2016; $year -le 2026; $year++) {
    $isActive = $year -eq 2026
    $vigBody = @{ ejercicio = $year; activoEjecucion = $isActive } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri "$baseUrl/Vigencia" -Method Post -Body $vigBody -Headers $AuthHeaders | Out-Null
        Write-Host "Vigencia $year creada."
    } catch {
        $err = $_.Exception.Message
        Write-Host "Error creando $year: $err"
    }
}

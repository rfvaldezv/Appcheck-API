<#
.SYNOPSIS
    Script de compilación y empaquetado para la API de Appcheck.
.DESCRIPTION
    Limpia binarios, compila en modo Release, asegura la copia de los modelos ONNX
    y genera un archivo ZIP con timestamp para el despliegue en el VPS.
#>

Set-Location "C:\Aplica\Appcheck_API\AppCheck.API"

Write-Host "`n🧹 1. Limpiando binarios anteriores..." -ForegroundColor Cyan
Remove-Item -Recurse -Force ".\bin", ".\obj" -ErrorAction SilentlyContinue

Write-Host "🔨 2. Compilando en Release (dotnet publish)..." -ForegroundColor Cyan
dotnet publish -c Release -o ./publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ ERROR: La compilación falló. Abortando para no generar un ZIP roto." -ForegroundColor Red
    exit 1
}

Write-Host "`n📦 3. Copiando modelos ONNX (siempre, por seguridad)..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path ".\publish\Models" -Force | Out-Null
if (Test-Path ".\Models\*.onnx") {
    Copy-Item ".\Models\*.onnx" -Destination ".\publish\Models\" -Force
    $modelos = Get-ChildItem ".\publish\Models\*.onnx"
    Write-Host "✅ $($modelos.Count) modelos copiados ($([math]::Round(($modelos | Measure-Object Length -Sum).Sum/1MB,2)) MB)" -ForegroundColor Green
} else {
    Write-Host "⚠️  ADVERTENCIA: No se encontraron archivos .onnx en la carpeta ./Models" -ForegroundColor Yellow
}

Write-Host "`n🗜️ 4. Comprimiendo con timestamp..." -ForegroundColor Cyan
$fecha = Get-Date -Format 'yyyyMMdd_HHmmss'
$zip = ".\publish_produccion_$fecha.zip"
Compress-Archive -Path ".\publish\*" -DestinationPath $zip -Force

Write-Host "`n📊 5. Resultado Final:" -ForegroundColor Cyan
Get-Item $zip | Select-Object Name, @{N='MB';E={[math]::Round($_.Length/1MB,2)}}, LastWriteTime | Format-Table -AutoSize

Write-Host "`n🚀 Listo para llevar al VPS:" -ForegroundColor Green
Write-Host "   Ruta: $zip" -ForegroundColor Yellow
Write-Host "   ---------------------------------------------------`n"

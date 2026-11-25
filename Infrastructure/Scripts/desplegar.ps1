# Sube el build y la infraestructura a Debian y ejecuta todo el despliegue.
# Uso (PowerShell, desde la carpeta del proyecto):
#   powershell -ExecutionPolicy Bypass -File Infrastructure\Scripts\desplegar.ps1 -IP 192.168.56.101
param(
    [Parameter(Mandatory = $true)][string]$IP,
    [string]$Usuario = "student"
)
$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$exe = Join-Path $root "Builds\NGO-LinuxServer\NetworkingLabNGOServer.x86_64"
if (-not (Test-Path $exe)) {
    Write-Host "ERROR: no existe $exe. Primero en Unity: Lab > Construir servidor Linux (NGO)." -ForegroundColor Red
    exit 1
}

Write-Host "1) Probando conexion con la VM..." -ForegroundColor Cyan
ping -n 2 $IP | Out-Host

Write-Host "2) Creando carpeta ~/lab en Debian (te pedira la contrasena)..." -ForegroundColor Cyan
ssh "$Usuario@$IP" "mkdir -p ~/lab"

Write-Host "3) Copiando Builds e Infrastructure..." -ForegroundColor Cyan
scp -r "$root\Builds" "${Usuario}@${IP}:/home/$Usuario/lab/"
scp -r "$root\Infrastructure" "${Usuario}@${IP}:/home/$Usuario/lab/"

Write-Host "4) Ejecutando el despliegue completo en Debian..." -ForegroundColor Cyan
ssh -t "$Usuario@$IP" "sed -i 's/\r$//' ~/lab/Infrastructure/Scripts/setup-servidor.sh && bash ~/lab/Infrastructure/Scripts/setup-servidor.sh"

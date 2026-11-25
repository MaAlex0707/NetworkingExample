#!/usr/bin/env bash
# Instala y despliega TODO el servidor del laboratorio dentro de Debian:
# servidor directo -> Docker -> Minikube -> Agones -> GameServer -> reenvío UDP.
# Uso (en Debian):  bash ~/lab/Infrastructure/Scripts/setup-servidor.sh
# Se puede volver a ejecutar las veces que quieras (por ejemplo tras cambiar el juego).
set -euo pipefail

SELF="$(readlink -f "$0")"
LAB="$(cd "$(dirname "$SELF")/../.." && pwd)"
EXE="NetworkingLabNGOServer.x86_64"
BUILD_DIR="$LAB/Builds/NGO-LinuxServer"
IMAGE="networking-lab-ngo:local"
GS_NAME="networking-lab-ngo"
PORT=7979
PROFILE="agones"
K="minikube kubectl -p $PROFILE --"
cd "$LAB"

paso() { echo; echo "==================== $* ===================="; }

# ---------------- FASE 1: sistema y paquetes ----------------
if [[ "${1:-}" != "--fase2" ]]; then
  paso "1/9 Sistema y red (checkpoints 03-04)"
  whoami; hostname; uname -m; ip -br addr; ip route
  [[ "$(uname -m)" == "x86_64" ]] || { echo "ERROR: necesitas Debian amd64 (x86_64)."; exit 1; }
  [[ -f "$BUILD_DIR/$EXE" ]] || { echo "ERROR: no encuentro $BUILD_DIR/$EXE. Genera el build en Unity y súbelo."; exit 1; }

  paso "2/9 Instalando paquetes (pide tu contraseña)"
  sudo apt-get update
  sudo apt-get install -y docker.io curl socat file libatomic1 libunwind8
  sudo systemctl enable --now docker
  sudo usermod -aG docker "$USER"
  # Continúa con el grupo docker activo sin cerrar sesión.
  exec sg docker -c "bash '$SELF' --fase2"
fi

# ---------------- FASE 2: despliegue ----------------
paso "3/9 Servidor directo en Linux (checkpoint 07)"
cd "$BUILD_DIR"
file "$EXE"
chmod +x "$EXE"
./"$EXE" -batchmode -nographics -port "$PORT" -logFile "$LAB/servidor-directo.log" &
SERVER_PID=$!
sleep 10
pgrep -af "$EXE" || true
sudo ss -lunp | grep ":$PORT" || echo "AVISO: el puerto $PORT no apareció; revisa $LAB/servidor-directo.log"
kill "$SERVER_PID" 2>/dev/null || true
wait "$SERVER_PID" 2>/dev/null || true
cd "$LAB"

paso "4/9 Imagen Docker (checkpoint 08)"
docker run --rm hello-world >/dev/null && echo "Docker funciona."
docker build -f Infrastructure/Containers/NGO/Dockerfile -t "$IMAGE" .
docker image inspect "$IMAGE" --format 'Arquitectura={{.Architecture}}  Entrypoint={{.Config.Entrypoint}}  Puertos={{.Config.ExposedPorts}}'
docker rm -f ngo-server-test >/dev/null 2>&1 || true
docker run -d --rm --name ngo-server-test -p "$PORT:$PORT/udp" "$IMAGE" >/dev/null
sleep 10
docker ps --filter name=ngo-server-test
docker logs ngo-server-test 2>&1 | grep -E "NGO_STATUS|AGONES" || true
sudo ss -lunp | grep ":$PORT" || true
docker stop ngo-server-test >/dev/null

paso "5/9 Kubernetes con Minikube (checkpoint 09)"
if ! command -v minikube >/dev/null; then
  curl -LO "https://github.com/kubernetes/minikube/releases/download/v1.39.0/minikube-linux-amd64"
  sudo install minikube-linux-amd64 /usr/local/bin/minikube
  rm minikube-linux-amd64
fi
minikube version
CPUS=$(nproc)
MEM_MB=$(free -m | awk '/Mem:/{print $2}')
if (( CPUS >= 4 && MEM_MB >= 7500 )); then MK_CPUS=4; MK_MEM=6144; else MK_CPUS=2; MK_MEM=$(( MEM_MB - 1024 )); fi
if ! minikube status -p "$PROFILE" >/dev/null 2>&1; then
  minikube start -p "$PROFILE" --driver=docker --kubernetes-version=v1.34.6 --cpus="$MK_CPUS" --memory="$MK_MEM"
fi
$K wait --for=condition=Ready node --all --timeout=300s
$K get nodes -o wide
grep -q 'alias kubectl=' ~/.bashrc || echo 'alias kubectl="minikube kubectl -p agones --"' >> ~/.bashrc

paso "6/9 Agones (checkpoint 10)"
$K create namespace agones-system --dry-run=client -o yaml | $K apply -f -
# 'create' en vez de 'apply --server-side': Kubernetes 1.34 rechaza el server-side apply de las CRD de Agones.
# Si algún recurso ya existe, 'create' avisa "AlreadyExists" y sigue con los demás (es normal).
# --validate=false: ignora campos que Kubernetes 1.34 ya no reconoce en las CRD (solo da avisos).
$K create --validate=false -f "https://raw.githubusercontent.com/agones-dev/agones/release-1.60.0/install/yaml/install.yaml" 2>&1 \
  | grep -v -E "AlreadyExists|unknown field" || true
$K get crd gameservers.agones.dev gameserversets.agones.dev fleets.agones.dev
# Versión liviana para un solo nodo: allocator y ping no se usan en el laboratorio.
$K scale deployment -n agones-system agones-allocator agones-ping --replicas=0
$K scale deployment -n agones-system agones-controller agones-extensions --replicas=1
# El disco de la VM es pequeño: se reduce el espacio temporal que reservan los pods.
for d in agones-controller agones-extensions; do
  $K set resources deployment "$d" -n agones-system --requests=ephemeral-storage=100Mi --limits=ephemeral-storage=1Gi
done
# Reinicia los controladores por si arrancaron antes de que existieran las CRD.
$K rollout restart deployment -n agones-system agones-controller agones-extensions
$K wait --for=condition=available deployment --all -n agones-system --timeout=600s
$K get pods -n agones-system
$K get crd gameservers.agones.dev

paso "7/9 Imagen en Minikube y GameServer"
$K delete gameserver "$GS_NAME" --ignore-not-found --wait=true
minikube image rm -p "$PROFILE" "$IMAGE" >/dev/null 2>&1 || true
minikube image load -p "$PROFILE" "$IMAGE"
minikube image ls -p "$PROFILE" | grep networking-lab-ngo
$K apply -f Infrastructure/Kubernetes/Agones/ngo-gameserver.yaml
STATE=""
for _ in $(seq 1 60); do
  STATE=$($K get gameserver "$GS_NAME" -o jsonpath='{.status.state}' 2>/dev/null || true)
  echo "  Estado del GameServer: ${STATE:-...}"
  [[ "$STATE" == "Ready" ]] && break
  sleep 3
done
$K get gameserver
$K get pods -o wide
POD=$($K get pods -l "agones.dev/gameserver=$GS_NAME" -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || true)
[[ -n "$POD" ]] && { $K logs "$POD" -c unity-server 2>/dev/null | grep -E "NGO_STATUS|AGONES" || true; }
if [[ "$STATE" != "Ready" ]]; then
  echo "ERROR: el GameServer no llegó a Ready."
  echo "Revisa: kubectl describe gameserver $GS_NAME   y   kubectl logs $POD -c unity-server"
  exit 1
fi

paso "8/9 Address y Port (checkpoint 11)"
GS_ADDR=$($K get gameserver "$GS_NAME" -o jsonpath='{.status.address}')
GS_PORT=$($K get gameserver "$GS_NAME" -o jsonpath='{.status.ports[0].port}')
echo "GameServer.status.address = $GS_ADDR"
echo "GameServer.status.port    = $GS_PORT"
echo "minikube ip               = $(minikube ip -p "$PROFILE")"
DEF_IF=$(ip route show default | awk '{print $5; exit}')
VM_IP=$(ip -4 -o addr show | awk -v d="$DEF_IF" '$2!="lo" && $2!=d && $2 !~ /^(docker|br-|veth)/ {split($4,a,"/"); print a[1]; exit}')
echo "IP Host-Only de Debian    = $VM_IP"

paso "9/9 Reenvío UDP $VM_IP:$GS_PORT -> $GS_ADDR:$GS_PORT"
echo
echo "  >>> EN UNITY (NGOGameplay):  Address = $VM_IP    Port = $GS_PORT    y pulsa Client <<<"
echo
echo "  Deja esta ventana ABIERTA mientras juegas. Ctrl+C para detener el reenvío."
sudo socat UDP4-LISTEN:"$GS_PORT",fork,reuseaddr UDP4:"$GS_ADDR":"$GS_PORT"

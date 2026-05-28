# URHYNIX TurtleBot3 helpers — single file for macOS + Linux (Ubuntu)
# Source from your shell rc:
#   echo 'source ~/URHYNIX/scripts/tb3.sh' >> ~/.zshrc    # macOS
#   echo 'source ~/URHYNIX/scripts/tb3.sh' >> ~/.bashrc   # Ubuntu
#
# Requires: ssh, nc, ping, expect, ipconfig/ip, arp/ip neigh
# Optional: scp (for tb3-up), xdg-open/open (for tb3-vnc), Unity Hub (for tb3-unity)

# ---- Tunables ----
export TB3_MAC_PATTERN='2c:cf:67:47:38:0?3'   # robot Wi-Fi MAC (leading-zero tolerant)
export TB3_USER='kim'
export TB3_ROBOT_IP_HINT='192.168.0.138'       # last known
export TB3_LAN_CIDR='192.168.0'                # /24 to sweep

# Passwords live OUTSIDE the repo. Put them in ~/.tb3rc (see scripts/tb3rc.example):
#   export TB3_PASSWORD='your-ssh-password'
#   export TB3_VNC_PASSWORD='your-vnc-password'
[ -f "$HOME/.tb3rc" ] && . "$HOME/.tb3rc"
: "${TB3_PASSWORD:=}"
: "${TB3_VNC_PASSWORD:=}"

# Unity smoke project — repo-relative (same path on macOS & Ubuntu)
_tb3_script_dir() {
  # works in both bash and zsh
  local s="${BASH_SOURCE[0]:-${(%):-%x}}"
  cd "$(dirname "$s")" && pwd
}
export TB3_REPO_ROOT="$(cd "$(_tb3_script_dir)/.." 2>/dev/null && pwd)"
export TB3_UNITY_PROJECT="${TB3_UNITY_PROJECT:-$TB3_REPO_ROOT/unity-smoke}"

# Unity Editor binary candidates — override by exporting TB3_UNITY_BIN
_tb3_unity_default() {
  case "$(uname -s)" in
    Darwin) echo "/Applications/Unity/Hub/Editor/6000.0.64f1/Unity.app/Contents/MacOS/Unity" ;;
    Linux)  for p in \
              "$HOME/Unity/Hub/Editor/6000.0.64f1/Editor/Unity" \
              "/opt/Unity/Editor/Unity" \
              "$(command -v unityhub 2>/dev/null)"; do
              [ -n "$p" ] && [ -x "$p" ] && echo "$p" && return; done; echo "" ;;
    *) echo "" ;;
  esac
}

# ---- OS-portable helpers ----
tb3-myip() {
  case "$(uname -s)" in
    Darwin) ipconfig getifaddr en0 2>/dev/null || ipconfig getifaddr en1 ;;
    Linux)  ip -4 -o addr show scope global 2>/dev/null \
            | awk '{print $4}' | cut -d/ -f1 | head -1 ;;
  esac
}

tb3-ip() {
  local hit
  # try last known first
  if ping -c1 -W1 "$TB3_ROBOT_IP_HINT" >/dev/null 2>&1; then
    if [ "$(uname -s)" = "Linux" ]; then
      hit=$(ip neigh show "$TB3_ROBOT_IP_HINT" 2>/dev/null \
            | grep -Ei "$TB3_MAC_PATTERN" | awk '{print "'"$TB3_ROBOT_IP_HINT"'"}' | head -1)
    else
      hit=$(arp -an 2>/dev/null | grep -F "($TB3_ROBOT_IP_HINT)" \
            | grep -Ei "$TB3_MAC_PATTERN" | sed -E 's/.*\(([0-9.]+)\).*/\1/' | head -1)
    fi
  fi
  if [ -z "$hit" ]; then
    for i in $(seq 1 254); do
      ping -c 1 -W 1 "${TB3_LAN_CIDR}.${i}" >/dev/null 2>&1 &
    done; wait
    if [ "$(uname -s)" = "Linux" ]; then
      hit=$(ip neigh 2>/dev/null | grep -Ei "$TB3_MAC_PATTERN" | awk '{print $1}' | head -1)
    else
      hit=$(arp -an 2>/dev/null | grep -Ei "$TB3_MAC_PATTERN" \
            | sed -E 's/.*\(([0-9.]+)\).*/\1/' | head -1)
    fi
  fi
  if [ -z "$hit" ]; then
    echo "TurtleBot3 not found on ${TB3_LAN_CIDR}.0/24 (looking for $TB3_MAC_PATTERN)" >&2
    return 1
  fi
  echo "$hit"
}

tb3-ssh() {
  local ip; ip=$(tb3-ip) || return 1
  ssh "$TB3_USER@$ip"
}

tb3-vnc() {
  local ip; ip=$(tb3-ip) || return 1
  echo "vnc://$ip:5902  (password: $TB3_VNC_PASSWORD)"
  case "$(uname -s)" in
    Darwin) open "vnc://$ip:5902" ;;
    Linux)  if command -v xdg-open >/dev/null; then xdg-open "vnc://$ip:5902";
            elif command -v vncviewer >/dev/null; then vncviewer "$ip:5902";
            else echo "Install vinagre/remmina/vncviewer to open VNC"; fi ;;
  esac
}

tb3-port() {
  local ip; ip=$(tb3-ip) || return 1
  nc -vz -G 3 "$ip" 10000 2>&1 || nc -vz -w 3 "$ip" 10000 2>&1
}

tb3-up() {
  # bringup + ros_tcp_endpoint tmux sessions on robot
  local ip mac
  ip=$(tb3-ip) || return 1
  mac=$(tb3-myip) || { echo "cannot detect Mac/Linux LAN IP"; return 1; }
  local script="$TB3_REPO_ROOT/scripts/urhynix_robot_up.sh"
  [ -f "$script" ] || { echo "missing $script"; return 1; }
  echo "→ scp $script to $ip"
  expect <<EXP
set timeout 25
spawn scp -o StrictHostKeyChecking=accept-new $script $TB3_USER@$ip:/tmp/urhynix_robot_up.sh
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
  echo "→ ssh + run robot_up.sh (Mac/Linux IP = $mac)"
  expect <<EXP
set timeout 25
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip bash /tmp/urhynix_robot_up.sh $mac
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } "OK_DONE" { exp_continue } eof }
EXP
}

tb3-down() {
  local ip; ip=$(tb3-ip) || return 1
  expect <<EXP
set timeout 12
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip {bash -lc "tmux kill-session -t bringup 2>/dev/null; tmux kill-session -t ros_tcp 2>/dev/null; tmux kill-session -t rviz 2>/dev/null; tmux kill-session -t arduino_bridge 2>/dev/null; sleep 1; tmux ls 2>/dev/null || echo NO_TMUX"}
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
}

tb3-bridge() {
  # start Arduino → ROS2 bridge node on robot
  local ip; ip=$(tb3-ip) || return 1
  local script="$TB3_REPO_ROOT/scripts/arduino_bridge.py"
  expect <<EXP
set timeout 15
spawn scp -o StrictHostKeyChecking=accept-new $script $TB3_USER@$ip:/tmp/arduino_bridge.py
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
  expect <<EXP
set timeout 15
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip {bash -lc "tmux kill-session -t arduino_bridge 2>/dev/null; tmux new-session -d -s arduino_bridge 'bash -lc \"source /opt/ros/jazzy/setup.bash && export ROS_DOMAIN_ID=56 RMW_IMPLEMENTATION=rmw_fastrtps_cpp ROS_AUTOMATIC_DISCOVERY_RANGE=SUBNET && python3 /tmp/arduino_bridge.py 2>&1 | tee /tmp/arduino_bridge.log\"'; sleep 1; tmux ls"}
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
}

tb3-arduino() {
  # 8-second raw serial capture (uses udev symlink)
  local ip; ip=$(tb3-ip) || return 1
  expect <<EXP
set timeout 18
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip {bash -lc "python3 - <<'PY'
import serial, time
s = serial.Serial('/dev/tb3_arduino', 9600, timeout=1); time.sleep(2)
deadline = time.time() + 8
while time.time() < deadline:
    line = s.readline().decode('utf-8', errors='replace').strip()
    if line: print('>', line)
s.close()
PY"}
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
}

tb3-poweroff() {
  local ip; ip=$(tb3-ip) || return 1
  printf "Shutdown robot %s ? (y/N) " "$ip"
  read -r ans
  case "$ans" in y|Y|yes) ;;
    *) echo "abort"; return 0 ;;
  esac
  expect <<EXP
set timeout 10
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip {bash -lc "echo $TB3_PASSWORD | sudo -S shutdown -h now"}
expect { "password:" { send "$TB3_PASSWORD\r"; exp_continue } eof }
EXP
  echo "shutdown issued — verify with: ping -c2 $ip"
}

tb3-unity() {
  local bin="${TB3_UNITY_BIN:-$(_tb3_unity_default)}"
  if [ -z "$bin" ] || [ ! -x "$bin" ]; then
    echo "Unity Editor not found. Install Unity Hub + 6000.0.64f1, or export TB3_UNITY_BIN=/path/to/Unity" >&2
    return 1
  fi
  case "$(uname -s)" in
    Darwin) open -na "/Applications/Unity/Hub/Editor/6000.0.64f1/Unity.app" --args \
              -projectPath "$TB3_UNITY_PROJECT" \
              -executeMethod RosSmokeConfigure.Play \
              -logFile /tmp/unity-tb3-smoke.log ;;
    Linux)  "$bin" -projectPath "$TB3_UNITY_PROJECT" \
              -executeMethod RosSmokeConfigure.Play \
              -logFile /tmp/unity-tb3-smoke.log & disown ;;
  esac
  echo "Unity launching → log: /tmp/unity-tb3-smoke.log"
}

tb3-key-setup() {
  # 한 번만 실행: Mac/Linux ed25519 공개키를 로봇에 등록 → 이후 비번 prompt 사라짐
  local ip; ip=$(tb3-ip) || return 1
  if [ ! -f "$HOME/.ssh/id_ed25519.pub" ]; then
    echo "→ generating new ed25519 key (no passphrase)"
    ssh-keygen -t ed25519 -N '' -f "$HOME/.ssh/id_ed25519" || return $?
  fi
  echo "→ ssh-copy-id to $TB3_USER@$ip (will prompt for robot password once)"
  expect <<EXP
set timeout 30
spawn ssh-copy-id -i $HOME/.ssh/id_ed25519.pub -o StrictHostKeyChecking=accept-new -o PreferredAuthentications=password -o PubkeyAuthentication=no $TB3_USER@$ip
expect {
  "password:" { send "$TB3_PASSWORD\r"; exp_continue }
  timeout { exit 2 }
  eof
}
EXP
  echo "→ verify passwordless login"
  ssh -o BatchMode=yes -o ConnectTimeout=6 "$TB3_USER@$ip" 'echo "OK from $(hostname)"' 2>&1 | head -3
}

tb3-go() {
  # 한 방 풀-기동: bringup + ros_tcp + arduino_bridge + 검증
  echo "▶ tb3-up (bringup + ros_tcp_endpoint)"
  tb3-up || return $?
  echo "▶ wait 12s for ros_tcp listen..."
  sleep 12
  echo "▶ tb3-bridge (arduino → ROS2 + Supabase)"
  tb3-bridge
  echo "▶ verify TCP 10000"
  tb3-port
}

tb3-restart() {
  echo "▶ tb3-down"
  tb3-down
  tb3-go
}

tb3-logs() {
  # 양쪽 tmux 로그 + 세션 한 화면
  local ip; ip=$(tb3-ip) || return 1
  expect <<EXP
set timeout 15
spawn ssh -o StrictHostKeyChecking=accept-new $TB3_USER@$ip {bash -lc "echo --TMUX--; tmux ls 2>/dev/null || echo NO_TMUX; echo; echo --BRINGUP--; tail -15 /tmp/bringup.log 2>/dev/null; echo; echo --ROS_TCP--; tail -15 /tmp/ros_tcp_endpoint.log 2>/dev/null; echo; echo --BRIDGE--; tail -15 /tmp/arduino_bridge.log 2>/dev/null"}
expect {
  "password:" { send "$TB3_PASSWORD\r"; exp_continue }
  eof
}
EXP
}

tb3-help() {
  cat <<EOF
URHYNIX TurtleBot helpers ($(uname -s))

  tb3-myip       Local LAN IP of this machine
  tb3-ip         Find robot IP by MAC pattern $TB3_MAC_PATTERN
  tb3-ssh        SSH into robot ($TB3_USER@<ip>)
  tb3-vnc        Open VNC viewer (RViz at :2)
  tb3-port       Check ROS-TCP port 10000

  tb3-up         Start bringup + ros_tcp_endpoint tmux on robot
  tb3-down       Kill all robot ROS tmux sessions
  tb3-bridge     Start Arduino → ROS2 bridge node on robot
  tb3-arduino    8-sec raw serial capture from /dev/tb3_arduino
  tb3-poweroff   sudo shutdown -h now (asks for confirmation)

  tb3-go         ★ up + wait + bridge + verify (one-shot full boot)
  tb3-restart    ★ down + go (clean restart)
  tb3-logs       Tail bringup/ros_tcp/arduino_bridge logs + tmux ls
  tb3-key-setup  ★ ssh-copy-id 한 번 (이후 비번 prompt 영구 사라짐)

  tb3-unity      Launch Unity Editor on $TB3_UNITY_PROJECT (auto-Play)

Env overrides: TB3_USER, TB3_PASSWORD, TB3_ROBOT_IP_HINT, TB3_LAN_CIDR,
               TB3_UNITY_PROJECT, TB3_UNITY_BIN

Additional URHYNIX aliases: run \`urhynix-help\`
EOF
}

# ─────── auto-load URHYNIX aliases (urhynix-*, sb-*) ───────
[ -f "$TB3_REPO_ROOT/scripts/aliases.sh" ] && . "$TB3_REPO_ROOT/scripts/aliases.sh"

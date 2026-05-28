#!/bin/bash
# URHYNIX robot one-shot reconnect — bringup + ros_tcp_endpoint
# Run on the robot via:  bash /tmp/urhynix_robot_up.sh <MAC_IP>
set -u
MAC_IP="${1:?MAC_IP required (your laptop's LAN IP for ROS_STATIC_PEERS)}"

# Kill old sessions (idempotent)
tmux kill-session -t bringup 2>/dev/null || true
tmux kill-session -t ros_tcp 2>/dev/null || true

# 1) TurtleBot bringup
tmux new-session -d -s bringup "bash -lc 'source /opt/ros/jazzy/setup.bash && \
  source \$HOME/turtlebot3_ws/install/setup.bash && \
  export ROS_DOMAIN_ID=56 TURTLEBOT3_MODEL=burger LDS_MODEL=LDS-03 \
         RMW_IMPLEMENTATION=rmw_fastrtps_cpp \
         ROS_AUTOMATIC_DISCOVERY_RANGE=SUBNET \
         ROS_STATIC_PEERS='"$MAC_IP"' && \
  ros2 launch turtlebot3_bringup robot.launch.py 2>&1 | tee /tmp/bringup.log'"

# 2) ROS-TCP-Endpoint
tmux new-session -d -s ros_tcp "bash -lc 'source /opt/ros/jazzy/setup.bash && \
  source \$HOME/turtlebot3_ws/install/setup.bash && \
  source \$HOME/unity_ros_ws/install/setup.bash && \
  export ROS_DOMAIN_ID=56 TURTLEBOT3_MODEL=burger LDS_MODEL=LDS-03 \
         RMW_IMPLEMENTATION=rmw_fastrtps_cpp \
         ROS_AUTOMATIC_DISCOVERY_RANGE=SUBNET && \
  ros2 run ros_tcp_endpoint default_server_endpoint \
    --ros-args -p ROS_IP:=$(hostname -I | awk "{print \$1}") -p ROS_TCP_PORT:=10000 \
  2>&1 | tee /tmp/ros_tcp_endpoint.log'"

sleep 1
tmux ls
echo OK_DONE

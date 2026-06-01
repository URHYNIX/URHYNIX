#!/bin/bash
# Pi Camera Module v2 (Sony IMX219) user-space build for Ubuntu 24.04 LTS
# Reason: Ubuntu 24.04 ports repo doesn't ship rpicam-apps / Pi-fork libcamera.
# Builds: github.com/raspberrypi/libcamera + github.com/raspberrypi/rpicam-apps
# Target time: 30~60 min on Pi 4 (4GB).
# Usage: tmux new -s picam-build "bash ~/build-picamera.sh"
#        tail -f ~/picam-build.log   # progress
set -euo pipefail
LOG=~/picam-build.log
exec > >(tee -a "$LOG") 2>&1

echo "=== [$(date +%T)] 1/4 deps install ==="
sudo apt update
sudo apt install -y \
  git python3-pip python3-jinja2 python3-yaml python3-ply \
  meson cmake ninja-build pkg-config \
  libboost-dev libgnutls28-dev openssl libtiff-dev pybind11-dev \
  libdrm-dev libexif-dev libjpeg-dev libpng-dev libgles2-mesa-dev \
  qtbase5-dev libqt5core5a libqt5gui5 libqt5widgets5 \
  libavcodec-dev libavdevice-dev libavformat-dev libswresample-dev \
  libudev-dev libyaml-dev libevent-dev \
  v4l-utils

echo "=== [$(date +%T)] 2/4 libcamera Pi fork build ==="
cd ~
if [ ! -d libcamera ]; then
  git clone --depth=1 https://github.com/raspberrypi/libcamera.git
fi
cd libcamera
if [ ! -d build ]; then
  meson setup build --buildtype=release \
    -Dpipelines=rpi/vc4 \
    -Dipas=rpi/vc4 \
    -Dv4l2=true -Dgstreamer=disabled -Dtest=false -Dlc-compliance=disabled \
    -Dcam=disabled -Dqcam=disabled -Ddocumentation=disabled
fi
ninja -C build -j2
sudo ninja -C build install
sudo ldconfig

echo "=== [$(date +%T)] 3/4 rpicam-apps build ==="
cd ~
if [ ! -d rpicam-apps ]; then
  git clone --depth=1 https://github.com/raspberrypi/rpicam-apps.git
fi
cd rpicam-apps
if [ ! -d build ]; then
  meson setup build --buildtype=release \
    -Denable_libav=enabled \
    -Denable_drm=enabled \
    -Denable_egl=enabled \
    -Denable_qt=enabled \
    -Denable_opencv=disabled \
    -Denable_tflite=disabled
fi
ninja -C build -j2
sudo ninja -C build install
sudo ldconfig

echo "=== [$(date +%T)] 4/4 verify ==="
rpicam-hello --version
rpicam-hello --list-cameras

echo ""
echo "=== BUILD COMPLETE [$(date +%T)] ==="
echo "Next: rpicam-still -n -t 2000 -o ~/cam_test.jpg"

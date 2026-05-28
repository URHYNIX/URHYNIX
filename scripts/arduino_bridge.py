#!/usr/bin/env python3
"""
URHYNIX Arduino → ROS2 bridge

Reads serial lines from /dev/tb3_arduino (Arduino UNO PIR + LDR sketch) and
publishes:
  - /sensors/pir   std_msgs/Bool   (motion detected: true on [MOTION], false on [CLEAR])
  - /sensors/ldr   std_msgs/Int32  (raw A0 value 0..1023, sent every LDR reading)

ROS_DOMAIN_ID and RMW settings must match the rest of the URHYNIX stack
(see scripts/urhynix_robot_up.sh).

Run on the robot (manual):
  source /opt/ros/jazzy/setup.bash
  python3 arduino_bridge.py

Run via helper:
  tb3-bridge        # from Mac/Ubuntu (uses tmux session arduino_bridge)
"""
import re
import sys
import time
import threading

import rclpy
from rclpy.node import Node
from std_msgs.msg import Bool, Int32

import serial

SERIAL_DEVICE = "/dev/tb3_arduino"
SERIAL_BAUD = 9600

RE_MOTION = re.compile(r"^\[MOTION\]")
RE_CLEAR = re.compile(r"^\[CLEAR")
RE_LDR = re.compile(r"^\[LDR\]\s+A0=(\d+)")


class ArduinoBridge(Node):
    def __init__(self):
        super().__init__("arduino_bridge")
        self.pub_pir = self.create_publisher(Bool, "/sensors/pir", 10)
        self.pub_ldr = self.create_publisher(Int32, "/sensors/ldr", 10)

        try:
            self.ser = serial.Serial(SERIAL_DEVICE, SERIAL_BAUD, timeout=1)
        except serial.SerialException as e:
            self.get_logger().error(f"open serial failed: {e}")
            raise

        # Arduino resets on DTR; give it a moment.
        time.sleep(2.0)
        self.get_logger().info(
            f"bridging {SERIAL_DEVICE} @ {SERIAL_BAUD} → /sensors/pir, /sensors/ldr"
        )

        self._stop = threading.Event()
        self._t = threading.Thread(target=self._read_loop, daemon=True)
        self._t.start()

    def _read_loop(self):
        while not self._stop.is_set():
            try:
                raw = self.ser.readline()
            except Exception as e:
                self.get_logger().warn(f"serial read error: {e}")
                time.sleep(0.5)
                continue
            if not raw:
                continue
            line = raw.decode("utf-8", errors="replace").strip()
            if not line:
                continue
            if RE_MOTION.match(line):
                self.pub_pir.publish(Bool(data=True))
                self.get_logger().info("PIR motion")
            elif RE_CLEAR.match(line):
                self.pub_pir.publish(Bool(data=False))
            else:
                m = RE_LDR.match(line)
                if m:
                    try:
                        v = int(m.group(1))
                        self.pub_ldr.publish(Int32(data=v))
                    except ValueError:
                        pass

    def destroy_node(self):
        self._stop.set()
        try:
            self.ser.close()
        except Exception:
            pass
        return super().destroy_node()


def main():
    rclpy.init()
    node = ArduinoBridge()
    try:
        rclpy.spin(node)
    except KeyboardInterrupt:
        pass
    finally:
        node.destroy_node()
        rclpy.shutdown()


if __name__ == "__main__":
    sys.exit(main() or 0)

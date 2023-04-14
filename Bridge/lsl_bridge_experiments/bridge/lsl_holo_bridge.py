from __future__ import print_function
from typing import Dict
import socket as soc
import sys
import logging
from bridge import bridge_data_classes as bc
from pylsl import StreamInlet, StreamOutlet
import signal
import pykka.debug
import platform
from bridge.bridge_inlet_thread import InletThread
from bridge.bridge_outlet_thread import OutletThread

# os dependent logging - logging should be enabled because it helps to determine issues

logging.basicConfig(level=logging.DEBUG)
if platform.system() in ['Darwin', 'Linux']:
    signal.signal(signal.SIGUSR1, pykka.debug.log_thread_tracebacks)
    pass
elif platform.system() is 'Windows':
    signal.signal(signal.SIGTERM, pykka.debug.log_thread_tracebacks)
    pass


class LslHoloBridge:

    def __init__(self, host: str, port: int,
                 inlets: Dict[bc.BridgeStreamInfo, StreamInlet], outlets: Dict[str, StreamOutlet]):
        self.host = host
        self.port = port
        self.inlets = inlets
        self.outlets = outlets

    def run_bridge(self):

        print(f"Starting server on port {self.port}")

        socket = soc.socket(soc.AF_INET, soc.SOCK_STREAM)
        socket.bind((self.host, self.port))

        socket.listen(1)
        conn, addr = socket.accept()
        print(f"New connection from {addr}")

        outlet_thread_ref = OutletThread.start(self.outlets, conn)
        inlet_thread_ref = InletThread.start(self.inlets.items(), conn)
        outlet_thread_ref.tell({'msg': 'Start'})
        inlet_thread_ref.tell({'msg': 'Start'})
        print("Started Inlet- and Outlet-Threads")

        while outlet_thread_ref.is_alive() & inlet_thread_ref.is_alive():
            pass

        conn.close()
        print(f"Connection to {addr} has been closed")
        socket.close()
        print('Server stopped successfully')
        sys.exit(0)

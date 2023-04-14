import pykka
import numpy as np
import struct
from bridge import bridge_data_classes as bc
import socket
from typing import List


class InletThread(pykka.ThreadingActor):
    """
    Thread handling all incoming samples from the lsl inlets.
    """

    def __init__(self, inlets: List, conn: socket.socket):
        super(InletThread, self).__init__()
        self.use_daemon_thread = True
        self.inlets = inlets
        self.conn = conn

    def on_receive(self, message):
        while not self.actor_stopped.is_set():
            # Handle inlets
            for bridge_stream_info, inlet in self.inlets:
                lsl_pkg, lsl_pkg_timestamp = inlet.pull_sample(timeout=0.0)

                if lsl_pkg is not None:
                    holo_pkg = self.convert_lsl_package(lsl_pkg, lsl_pkg_timestamp, bridge_stream_info)
                    self.conn.send(holo_pkg)

    @staticmethod
    def convert_lsl_package(lsl_pkg, lsl_pkg_timestamp: np.float64, stream_info: bc.BridgeStreamInfo):
        """
        Converts data from a lsl sample to a package in the format of the HoloLSLBridge.

        :param lsl_pkg: the lsl sample
        :param lsl_pkg_timestamp: a 64 bit float representing the lsl timestamp
        :param stream_info: an stream information object holding important information about the stream format
        :return:
        """

        pkg_name = stream_info.name.encode(bc.BRIDGE_ENCODING)
        pkg_name_size = len(pkg_name)

        pkg_type = bc.PKG_TYPES[stream_info.pkg_type_in_bytes]

        if pkg_type.lsl_arg == 'string':
            payload = (lsl_pkg[0].encode(bc.BRIDGE_ENCODING),)
            payload_size = pack_size = len(payload[0])
        else:
            payload = lsl_pkg
            pack_size = stream_info.channel_count
            payload_size = stream_info.channel_count * pkg_type.size_in_bytes_per_elem

        return struct.pack(f">dcI{pkg_name_size}sI{pack_size}{pkg_type.struct_arg}",
                           lsl_pkg_timestamp,
                           stream_info.pkg_type_in_bytes,
                           pkg_name_size,
                           pkg_name,
                           payload_size,
                           *payload)

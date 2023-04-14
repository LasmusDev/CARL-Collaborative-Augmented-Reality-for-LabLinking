import pykka
from pylsl import local_clock
import arrow
import numpy as np
import struct
from bridge import bridge_data_classes as bc
import socket
from typing import Dict
from datetime import datetime

class OutletThread(pykka.ThreadingActor):
    """
    Thread handling all incoming packages from the Hololens client
    """

    def __init__(self, outlets: Dict, conn: socket.socket):
        super(OutletThread, self).__init__()
        self.use_daemon_thread = True
        self.outlets = outlets
        self.conn = conn

    def on_receive(self, message):
        while True:
                pkg = self.receive_package(self.conn)
                # use default lsl timestamp
                # diff = self.get_time_difference(pkg.timestamp)           
                # assuming the time difference is positive - so the time stamp is smaller
                # self.outlets[pkg.name].push_sample(pkg.payload, local_clock() - diff)
                self.outlets[pkg.name].push_sample(pkg.payload)                

    @staticmethod
    def get_time_difference(pkg_timestamp):
        # On unix systems this is since January 1, 1970, 00:00:00 (UTC)
        return arrow.time() - pkg_timestamp

    @staticmethod
    def receive_package(conn: socket.socket):
        """
        A package:
        ( 64Bit Timestamp;  8Bit Type; 32Bit name-size; name-size Bit channelName;
        32Bit payload-size in byte; payload-size Bit rest Payload )
        """
        bridge_time_stamp = datetime.now()
        timestamp, type_int8, name_size = struct.unpack('!dcI', conn.recv(8 + 1 + 4))

        timestamp_datetime = datetime.utcfromtimestamp(timestamp).strftime('%Y-%m-%d %H:%M:%S')

        print("Received package. (Bridge_Time_Stamp vs. Timestamp) ", bridge_time_stamp, timestamp_datetime)

        if type_int8 not in bc.PKG_TYPES:
            raise bc.PackageError(f"The payload type of the package does not match any excepted type: {type_int8}")

        pkg_type = bc.PKG_TYPES[type_int8]

        if name_size == 0:
            raise bc.PackageError("The package name is not defined!")

        name_bytes = conn.recv(name_size)
        pkg_name = name_bytes.decode(bc.BRIDGE_ENCODING)

        payload_size_bytes = conn.recv(4)
        payload_size = struct.unpack('!I', payload_size_bytes)[0]
        if payload_size == 0:
            raise bc.PackageError("The package has no payload!")

        if payload_size % pkg_type.size_in_bytes_per_elem != 0:
            raise bc.PackageError("The package payload has not a valid size!")

        payload_bytes = conn.recv(payload_size)
        channel_count = np.uint32(payload_size / pkg_type.size_in_bytes_per_elem)

        if pkg_type.lsl_arg == "string":
            channel_count = 1
            payload = (payload_bytes.decode(bc.BRIDGE_ENCODING),)

        else:
            payload = struct.unpack(f"!{channel_count}{pkg_type.struct_arg}", payload_bytes)

        return bc.Package(timestamp, pkg_type, name_size, pkg_name, payload_size, payload, channel_count)


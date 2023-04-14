import typing
from dataclasses import dataclass

import numpy as np


@dataclass(frozen=True)
class PackageType:
    name: str
    struct_arg: str
    size_in_bytes_per_elem: int
    lsl_arg: str


@dataclass(frozen=True)
class BridgeStreamInfo:
    name: str
    pkg_type_in_bytes: bytes
    channel_count: int


BRIDGE_ENCODING = 'ascii'

BYTE_CODE_BYTE_TYPE = b'\x00'
BYTE_CODE_CHAR_TYPE = b'\x01'
BYTE_CODE_SHORT_TYPE = b'\x02'
BYTE_CODE_INT_TYPE = b'\x03'
BYTE_CODE_LONG_TYPE = b'\x04'
BYTE_CODE_FLOAT_TYPE = b'\x05'
BYTE_CODE_DOUBLE_TYPE = b'\x06'

# for unpacking bytes we have to use 'b' as struct arg because lsl only expects ints
BYTE_TYPE = PackageType(name='byte', struct_arg='b', size_in_bytes_per_elem=1, lsl_arg='int8')
CHAR_TYPE = PackageType(name='char', struct_arg='s', size_in_bytes_per_elem=1, lsl_arg='string')
SHORT_TYPE = PackageType(name='short', struct_arg='h', size_in_bytes_per_elem=2, lsl_arg='int16')
INT_TYPE = PackageType(name='int', struct_arg='i', size_in_bytes_per_elem=4, lsl_arg='int32')
# Long type is not supported for all os in lsl therefore we do not offer any support
LONG_TYPE = PackageType(name='long', struct_arg='q', size_in_bytes_per_elem=8, lsl_arg='int64')
FLOAT_TYPE = PackageType(name='float', struct_arg='f', size_in_bytes_per_elem=4, lsl_arg='float32')
DOUBLE_TYPE = PackageType(name='double', struct_arg='d', size_in_bytes_per_elem=8, lsl_arg='double64')

PKG_TYPES = {
    BYTE_CODE_BYTE_TYPE: BYTE_TYPE,
    BYTE_CODE_CHAR_TYPE: CHAR_TYPE,
    BYTE_CODE_SHORT_TYPE: SHORT_TYPE,
    BYTE_CODE_INT_TYPE: INT_TYPE,
    BYTE_CODE_LONG_TYPE: LONG_TYPE,
    BYTE_CODE_FLOAT_TYPE: FLOAT_TYPE,
    BYTE_CODE_DOUBLE_TYPE: DOUBLE_TYPE
}


@dataclass(frozen=True)
class Package:
    timestamp: np.float64
    package_type: PackageType
    name_size: np.uint32
    name: str
    payload_size: np.uint32
    payload: 'typing.Any'
    channel_count: np.uint32


class PackageError(Exception):
    """Exception raised if invalid packages are found.

    Attributes:
        message -- explanation of the error
    """

    def __init__(self, message):
        self.message = message

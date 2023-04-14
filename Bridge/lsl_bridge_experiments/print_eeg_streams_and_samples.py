"""
A standalone script that prints the name and number of samples of all streams in a xdf file
"""
import sys
from collections import namedtuple
from pathlib import Path
from typing import List, Dict

try:
    from pyxdf import load_xdf
except Exception:
    print("Cannot import pyxdf")
    print("Run the following command to install it: pip install pyxdf")
    sys.exit(1)

# pip install pyxdf
print_info_fields = "idx", "name", "type", "c_count", "c_fmt", "num_samples", "duration_s", "ts_min", "ts_max"
StreamPrintInfo = namedtuple("StreamPrintInfo", print_info_fields)


def print_table(table):
    # https://stackoverflow.com/a/52247284
    longest_cols = [
        (max([len(str(row[i])) for row in table]) + 3)
        for i in range(len(table[0]))
    ]
    row_format = "".join(["{:>" + str(longest_col) + "}" for longest_col in longest_cols])
    for row in table:
        print(row_format.format(*list(row)))


def print_streams_in_xdf_file(streams, xdf_file_path: Path):
    print(f"Stream info for {xdf_file_path}")
    print(f"Num streams: {len(streams)}")

    print_info_streams = [print_info_fields]
    for idx, stream_obj in enumerate(streams):
        if "info" in stream_obj:
            stream_info = stream_obj["info"]
            get_info = lambda field: stream_info.get(field, "")[0]
            fmt_float = lambda float_val: f"{float_val:.2f}"

            stream_ts = stream_obj["time_stamps"]
            if len(stream_ts) > 0:
                ts_min = stream_ts.min()
                ts_max = stream_ts.max()
                duration = ts_max - ts_min
            else:
                ts_min, ts_max, duration = float("nan"), float("nan"), float("nan")

            print_info_streams.append(StreamPrintInfo(
                idx=idx, name=get_info("name"), type=get_info("type"),
                c_count=get_info("channel_count"),
                c_fmt=get_info("channel_format"),
                num_samples=len(stream_obj["time_series"]),
                duration_s=fmt_float(duration),
                ts_min=fmt_float(ts_min), ts_max=fmt_float(ts_max),
            ))
        else:
            print(f"Could not find 'info' field for stream with idx {idx}: '{stream_obj}'..skipping")

    print_table(print_info_streams)


def try_finding_stream_by_info(streams: List[Dict], info_field: str, desired_info_val: str):
    """
    Finds the stream corresponding which stream info has fields which matched to a desired value (e.g. a "type" of
    "EEG") or "name" of "my-et-stream"
    :param streams: A list of streams
    :return The first found EEG stream, or None if no stream was found.
    """
    for stream_obj in streams:
        if "info" in stream_obj:
            if info_field in stream_obj["info"]:
                field = stream_obj["info"][info_field]
                if field == desired_info_val:
                    return stream_obj
                elif isinstance(field, (list, tuple)):
                    for val in field:
                        if val == desired_info_val:
                            return stream_obj
    return None


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--xdf_file_path", type=Path, default="TODO")
    args = parser.parse_args()
    xdf_path = Path(args.xdf_file_path)
    streams, file_info = load_xdf(xdf_path)
    print_streams_in_xdf_file(streams, xdf_path)


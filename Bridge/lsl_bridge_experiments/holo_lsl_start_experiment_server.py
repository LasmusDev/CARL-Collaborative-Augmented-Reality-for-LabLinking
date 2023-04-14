from pylsl import StreamInfo, StreamOutlet, resolve_stream, StreamInlet
from bridge.lsl_holo_bridge import LslHoloBridge
from bridge.bridge_data_classes import BridgeStreamInfo, BYTE_CODE_SHORT_TYPE, BYTE_CODE_CHAR_TYPE
import pylsl as lsl

def get_args():
    import argparse

    parser = argparse.ArgumentParser(description="Starts the LSL Bridge Server for interconnnecting the HL2 simulation "
                                                 "with LSL. ")
    parser.add_argument("--port", type=int, default=10_000,
                        help="The port which should be opened by the server. The HL2 application must send packets to "
                             "this port. Defaults to 10000")
    parser.add_argument("--host", type=str, default="0.0.0.0",
                        help="The internal IP adress of the server. The HL2 application must send packets to this IP "
                             "adress. Defaults to '0.0.0.0'")
    parser.add_argument("--outlet_prefix", type=str, default="",
                        help="A prefix to be attached to every outlets name, such that these outlets can be differentiated in the LabRecorder")                           
    args = parser.parse_args()

    return args

if __name__ == "__main__":
    args = get_args()
    # create stream info objects to create stream outlets to which the bridge will write
    HL_tracking_outlet = StreamInfo(name=f"HL_tracking_stream {args.outlet_prefix}",
                                    type='object_tracking_stream',
                                    channel_count=1,
                                    nominal_srate=lsl.IRREGULAR_RATE,
                                    channel_format=lsl.cf_string,
                                    source_id='HL_tracking_Data')
    #Comment back in if needed.
    """ 
    OT_tracking_outlet = StreamInfo(name='OT_tracking_stream',
                                    type='object_tracking_stream',
                                    channel_count=1,
                                    nominal_srate=lsl.IRREGULAR_RATE,
                                    channel_format=lsl.cf_string,
                                    source_id='OT_tracking_Data')
    """

    Event_outlet = StreamInfo(name=f"Event_stream {args.outlet_prefix}",
                                    type='Event_Stream',
                                    channel_count=1,
                                    nominal_srate=lsl.IRREGULAR_RATE,
                                    channel_format=lsl.cf_string,
                                    source_id='HL_Events')

    outlets = {
        "HL_tracking_stream": StreamOutlet(HL_tracking_outlet, chunk_size=1, max_buffered=3600),
        #"OT_tracking_stream": StreamOutlet(OT_tracking_outlet, chunk_size=1, max_buffered=3600),
        "event_stream": StreamOutlet(Event_outlet, chunk_size=1, max_buffered=3600),
    }
                                                
    inlets = { 
    }


    

    print("Configure server...")
    print(f"Host: {args.host}")
    print(f"Port: {args.port}")

    bridge = LslHoloBridge(port=args.port, host=args.host, inlets=inlets, outlets=outlets)

    print("Bridgeserver is starting...")
    #reopen bridge on crash
    while(True):
        try:
            bridge.run_bridge()
        except:
            print("Error occured")

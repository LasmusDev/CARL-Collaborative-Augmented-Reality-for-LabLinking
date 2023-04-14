using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Globalization;
using Microsoft.MixedReality.Toolkit;


using UnityEngine.UI;
using TMPro;

using System.IO;
using UnityEngine.SceneManagement;

/*/
 * This class represents the main data transfer for Eye-tracking and Audio data.
 * The main interfaces of this class is:
 *  - startTransfer: Starts the eye-trackign and audio transfer
 *  - stopTranfer: Stops the eye-trackign and audio transfer
 *  - switchTransfer: Convenience method for toggling the data transfer.
 *  
 *  Internally, data is sent as follows:
 *  - Eye-Tracking Data is sent in the Update method whenever the internal gaze input of the HL2 changes or gets invalid.
 *    The Data is sent via a DataUDPStream to the Eye-tracking UDP server.
 *    The host and port of this server must be set in the fields eyeTrackingServerHost and eyeTrackingServerPort.
 *  - Audio Data is sent via a class (MicStreamMinimal) which uses the UWP audioGraph API underneath.
 *    The data is also sent via DataUDPStream within a callback method of MicStreamMinimal.
 *    The audio UDP server must be specified in the fields audioServerHost and audioServerPort.
 *  
 *  The ET and audio UDP servers must be within the same internal network as the HL2.
 *  For more infos, see the README file.
 */

public class DataTransfer : MonoBehaviour
{   
    /**;
     * Whether the data transfer is currently running.
     */
    public bool isRunning;


    // Hard coded values for hosts and ports of the eye-tracking and audio servers.
    // Should be set by MQTT configurations later on.

    /**
     * Internal IP adress of the eye-tracking server
     */
    public String eyeTrackingServerHost = "192.168.0.142";

    /**
     * Port of the eye-tracking server;
     */
    public String eyeTrackingServerPort = "8082";

    /**
     * Internal IP adress of the audio server.
     */
    public String audioServerHost = "192.168.0.142";

    /**
     * Port of the audio server.
     */
    public String audioServerPort = "8081";

    /**
     * The last time stamp in which we received valid eye tracking data.
     * Is used to only send ET data when it was updated internally in the HL2.
     */
    private System.DateTime lastTimeStampEyeTracking = System.DateTime.MinValue;


    /**
     * A class which representa a UDP connection to stream audio;
     */
    private DataUDPStream audioUDPStream;

    /**
     * A UDP client which sends eyetracking data;
    */
    private DataUDPStream eyeTrackingUDPStream;

    /**
     * An NTPC client to calculate time offsets between the local clock and NTP clock;
     * The offset is used to fix local time stamps before sending ET data.
     */
    private NTPClient nTPClient = new NTPClient();

    /**
     * An action which sends eyeTracking data as bytes in the Update method.
     */
    private Action<Byte[]> eyeTrackingSendAction;


    // Use this for initialization
    void Start()
    {
        this.isRunning = false;
        // Get an initial estimate on the NTP offset.
        Debug.Log("DataTransfer.Start() Called");
        nTPClient.getNTPTimeAndCalcOffset();
    }

    /**
    * Starts the data transfer; Connects to the Audio and ET servers using the set host / ports and starts sending data.
    */
    public void startTransfer()
    {
        Debug.Log("Starting the data transfer.");
        startAudioTransfer();
        startEyeTrackingTransfer();
        this.isRunning = true;
        nTPClient.getNTPTimeAndCalcOffset();

    }


    /**
     * Stops the data transfer and tears down all relevant components for data (i.e. Audio and Eye-Tracking components);
     */
    public void stopTransfer()
    {
        Debug.Log("Stopping the data transfer.");
        stopAudioTransfer();
        stopEyeTrackingTransfer();
        this.isRunning = false;
    }

    /**
     * Convenience method for starting / stopping the data transfer.
     * Can for example be attached to an UI component to toggle the data transfer.
     */
    public void SwitchTransfer()
    {
        if (this.isRunning)
        {
            stopTransfer();
        }
        else
        {
            startTransfer();
        }
    }

   /**
   * Update method, in which the ET Data should be sent.
   * Checks if the Eyegaze has been updated in the EyeGazeProvider; If so, the EyeTracking data is sent via UDP
   */
    void Update()
    {
        if (isRunning)
        {
            // Sending Eyetracking Data - only send every Eye Tracker Update given by the timeStamp of the EyeGazeProvider
            // This timeStamp changes when the ET data has been updated internally;
            if (lastTimeStampEyeTracking != CoreServices.InputSystem.EyeGazeProvider.Timestamp)
            {
                sendEyeTrackingDataViaUDP();
            } else if (!CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingEnabledAndValid)
            {
                // Eye tracking data might not be enabled (privacy settings)
                // or might not be valid (e.g. the user closes his / her eyes):
                // We will send a custom message with NaNs to denote that we currently cannot get ET data,
                // but not because of a network disconnection between the mini pc and the HL2.
                // If we continiously receive NaNs etc., the user must calibrate and enable ET for the HL2 application.
                sendInvalidEyeTrackingdataViaUDP();
            }
            // Set the last seen ET timeStamp to check for new ET updates in the next iteration;
            lastTimeStampEyeTracking = CoreServices.InputSystem.EyeGazeProvider.Timestamp;
        }
    }

    /**
     * Formats a float s.t. it can be easily parsed later on; Replaces decimal commas with points
     */
    private static string fmtFloat(float val)
    {
        if (val == null)
        {
            return "";
        }
        return val.ToString().Replace(",", ".");
    }

    /**
     * Formats an EyeTracking vector to a string, s.t. it can be easily parsed later on.
     * Formats the X,Y and Z values as : "X,Y,Z", where each character represents the floating value of the vector.
     * If the vector is null, then "NaN,NaN,NaN" is returned.
     */
    public static string formatEyeTrackingVector(Vector3 vec)
    {
        if (vec == null)
        {
            return "NaN,NaN,NaN";
        }
        else
        {
            return string.Format("{0},{1},{2}", fmtFloat(vec.x), fmtFloat(vec.y), fmtFloat(vec.z));
        }
    }

    /**
     * Gets the current local time with an estimated NTP offset subtracted;
     * Formats the time stamp as a string which includes milliseconds.
     */
    private string getTimeStampStringWithOffset()
    {
        // Get the estimated NTP offset from the NTPClient;
        double ntpOffset;
        if (nTPClient.ntpError)
        {
            ntpOffset = 0.0;
        }
        else
        {
            ntpOffset = nTPClient.NtpTimeOffset;
        }

        // Get the current local time
        var timeStamp = DateTime.Now;
        // Subtract the NTP Offset form the time stamp to correct clock desyncs
        var timeStampWithOffset = DateTime.Now.AddSeconds(-ntpOffset);
        // Format the timestamps with milliseconds
        string timestampFmt = timeStampWithOffset.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return timestampFmt;
    }

    /**
     * Sends the current eye tracking data to the ET UDP server;
     * A message contains the following information encoded as string:
     * 1) The local time on the HL2, with an estimated NTP offset subtacted from the DateTime
     * 2) The Eye Gaze direction (X,Y and Z position)
     * 3) The Eye Gaze origin (X,Y and Z position)
     * The three fields are separated with a semicolon (';')
     */
    private void sendEyeTrackingDataViaUDP()
    {
        if (this.eyeTrackingSendAction == null)
        {
            Debug.Log("eyeTrackingSendAction is null - returning");
            return;
        }

        // Get the current Timestamp formatted as string;
        var timestamp = getTimeStampStringWithOffset();
        // Get the eye gaze direction and origin and format the vectors as strings
        var eyeDirectionFmt = formatEyeTrackingVector(CoreServices.InputSystem.EyeGazeProvider.GazeDirection);
        var eyeOriginFmt = formatEyeTrackingVector(CoreServices.InputSystem.EyeGazeProvider.GazeOrigin);

        sendEyeTrackingStringFormatViaUDP(timestamp, eyeDirectionFmt, eyeOriginFmt);
    }

    /**
     * Sends an UDP message that the ET data is currently not available.
     */
    private void sendInvalidEyeTrackingdataViaUDP()
    {
        if (this.eyeTrackingSendAction == null)
        {
            Debug.Log("eyeTrackingSendAction is null - returning");
            return;
        }
        // Get the current Timestamp formatted as string;
        var timestamp = getTimeStampStringWithOffset();
        var eyeDirectionFmt = "NaN,NaN,NaN";
        var eyeOriginFmt = "NaN,NaN,NaN";

        sendEyeTrackingStringFormatViaUDP(timestamp, eyeDirectionFmt, eyeOriginFmt);
    }

    /**
     * Formats the time stamp, the eye gaze direction and eye origin strings, encodes them as bytes and sends
     * the bytes to the eye-tracking UDP server.
     * If the eyeTrackingSendAction is null, then no data will be sent.
     */
    private void sendEyeTrackingStringFormatViaUDP(string timestamp, string eyeDirectioFmt, string eyeOriginFmt)
    {
        if (this.eyeTrackingSendAction != null)
        {
            // Concatenate the time stamps with with the vectors as string
            String message = string.Format("{0};{1};{2}", timestamp, eyeDirectioFmt, eyeOriginFmt);

            // Encode with UTF-8
            Byte[] messageBytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(message));
            this.eyeTrackingSendAction(messageBytes);
        }

    }

    /**
     * Starts the Audio transfer using the AudioGraph API (MicStream) and a UDP connection (DataUDPStream).
     */
    private void startAudioTransfer()
    {
        Debug.Log("startAudioTransfer called");
        if (this.audioUDPStream == null)
        {
            this.audioUDPStream = new DataUDPStream();
            this.audioUDPStream.ipString = audioServerHost;
            this.audioUDPStream.port = audioServerPort;
        }
        Debug.Log("Connecting to Audio UDP server: " + audioServerHost + ":" + audioServerPort);

        Action<Byte[]> frameProcessAction = audioUDPStream.createSendAction();

        //TODO Add Mic Stream back
    }

    /**
     * Connects to the ET server and creates an Action which sends byte to the server if called.
     * This action is used in the Update Method to send ET data to the server.
     */
    private void startEyeTrackingTransfer()
    {
        Debug.Log("startEyeTrackingTransfer called");
        if (this.eyeTrackingUDPStream == null)
        {
            this.eyeTrackingUDPStream = new DataUDPStream();
            this.eyeTrackingUDPStream.ipString = eyeTrackingServerHost;
            this.eyeTrackingUDPStream.port = eyeTrackingServerPort;
        }
        Debug.Log("Connecting to Eye-Tracking UDP server: " + eyeTrackingServerHost + ":" + eyeTrackingServerPort);

        Action<Byte[]> frameProcessAction = eyeTrackingUDPStream.createSendAction();
        this.eyeTrackingSendAction = frameProcessAction;
    }


    /**
      * Stops the AudioGraph and closes the socket to the audio UDP server;
      * This also stops the recording of the microphone of the HL2 - this might be appropiate if a driver gets near a customer.
     */
    private async Task stopAudioTransfer()
    {
        //TODO: Close MicStream

        if (this.audioUDPStream != null)
        {
            this.audioUDPStream.disconnect();
            this.audioUDPStream = null;
        }
    }

    /**
     * Deletes the eyeTracking send action and closes the socket the eye-tracking UDP Server.
     * This stops sending ET data to the server.
     */
    private async Task stopEyeTrackingTransfer()
    {
        this.eyeTrackingSendAction = null;
        if (this.eyeTrackingUDPStream != null)
        {
            this.eyeTrackingUDPStream.disconnect();
            this.eyeTrackingUDPStream = null;
        }
    }
}
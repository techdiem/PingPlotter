using System.Net.NetworkInformation;

namespace PingPlotter
{
    class PingUtil
    {
        public static PingReply PingHost(string host)
        {
            //set the ping options, TTL 128
            PingOptions pingOptions = new PingOptions(128, true);

            //create a new ping instance
            Ping ping = new Ping();

            //32 byte buffer (create empty)
            byte[] buffer = new byte[32];

            //first make sure we actually have an internet connection
            if (NetworkInterface.GetIsNetworkAvailable()) {
                try {
                    //send the ping 4 times to the host and record the returned data.
                    //The Send() method expects 4 items:
                    //1) The IPAddress we are pinging
                    //2) The timeout value
                    //3) A buffer (our byte array)
                    //4) PingOptions
                    PingReply pingReply = ping.Send(host, 1000, buffer, pingOptions);

                    return pingReply;
                }
                catch {
                    return null;
                }
            } else {
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PingPlotter
{
    class PingUtil
    {
        private static IPAddress GetIpFromHost(ref string host)
        {
            //variable to hold our error message (if something fails)
            string errMessage = string.Empty;

            //IPAddress instance for holding the returned host
            IPAddress address = null;

            //wrap the attempt in a try..catch to capture
            //any exceptions that may occur
            try
            {
                //get the host IP from the name provided
                address = Dns.GetHostEntry(host).AddressList[0];
            }
            catch (SocketException ex)
            {
                //some DNS error happened, return the message
                Console.WriteLine(string.Format("DNS Error: {0}", ex.Message));
            }
            return address;
        }


        public static PingReply PingHost(string host)
        {
            //IPAddress instance for holding the returned host
            IPAddress address = GetIpFromHost(ref host);

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
                    PingReply pingReply = ping.Send(address, 1000, buffer, pingOptions);

                    return pingReply;
                }
                catch (PingException ex) {
                    Console.WriteLine(string.Format("Connection Error: {0}", ex.Message));
                    return null;
                }
                catch (SocketException ex) {
                    Console.WriteLine(string.Format("Connection Error: {0}", ex.Message));
                    return null;
                }
            } else {
                return null;
            }
        }
    }
}

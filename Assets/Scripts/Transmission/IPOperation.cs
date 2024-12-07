using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class IPOperation
{
    public static IPAddress resolvedIp;

    public static bool IsLocal(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        byte b1 = bytes[0];
        byte b2 = bytes[1];

        if (b1 == 192 && b2 == 168)
        {
            //class C
            return true;
        }
        else if (b1 == 10)
        {
            //class A
            return true;
        }
        else if (b1 == 172 && (b2 >= 16 && b2 <= 31))
        {
            //class B
            return true;
        }
        else
        {
            byte b3 = bytes[2];
            byte b4 = bytes[3];

            if (b1 == 127 && b2 == 0 && b3 == 0 && (b4 >= 1 && b4 <= 8))
            {
                //loopback
                return true;
            }
        }

        return false;
    }

    public static IPAddress GlobalIPAddress()
    {
        try
        {
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri("http://checkip.dyndns.org");

            Task<HttpResponseMessage> resultTask = Task.Run<HttpResponseMessage>(async () => await client.GetAsync(""));
            HttpResponseMessage result = resultTask.Result;

            result.EnsureSuccessStatusCode();

            Task<string> responseTask = Task.Run<string>(async () => await result.Content.ReadAsStringAsync());
            string responseBody = responseTask.Result;

            //find the start of the IP address from the HTML response
            string ipHeader = "Current IP Address: ";
            int startIndex = responseBody.IndexOf(ipHeader);
            startIndex += ipHeader.Length;

            //find the end of the IP address
            int endIndex = responseBody.IndexOf("</body>", startIndex);

            string ipString = responseBody.Substring(startIndex, endIndex - startIndex);

            IPAddress ipAddress = IPAddress.Parse(ipString);

            return ipAddress;
        }
        catch (System.Exception)
        {
            return IPAddress.Any;
        }
    }

    public static IPAddress ResolveDomainName(string domain)
    {
        IPHostEntry hostEntry = Dns.GetHostEntry(domain);

        if (hostEntry.AddressList.Length > 0)
        {
            IPAddress firstEntry = hostEntry.AddressList[0];

            byte[] addressBytes = firstEntry.GetAddressBytes();

            int sum = addressBytes[0] + addressBytes[1] + addressBytes[2] + addressBytes[3];

            //fallback if the resolving fails
            if (sum > 0)
            {
                resolvedIp = firstEntry;
            }

            return resolvedIp;
        }

        return resolvedIp;
    }
}
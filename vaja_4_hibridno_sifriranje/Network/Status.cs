using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vaja_4_hibridno_sifriranje.Network
{
    public enum Status
    {
        Stopped = 0, Wait = 1, Success = 2, Error = -1
    }

    public static class StatusMethods
    {
        public static string GetStatusString(this Status status)
        {
            switch (status)
            {
                case Status.Wait:
                    return "Status: Wait";
                case Status.Success:
                    return "Status: Success";
                case Status.Error:
                    return "Status: Error";
                default:
                    return string.Empty;
            }
        }
    }

}

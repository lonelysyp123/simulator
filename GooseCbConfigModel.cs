using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator
{
    public class GooseCbConfigModel
    {
        public string GooseName { get; set; }
        public string GooseCbReference { get; set; }
        public byte[] DstMacAddress { get; set; }
        public string AppId { get; set; }
        public string Dsreference { get; set; }

        public GooseCbConfigModel(string name,string reference, byte[] mac, string appId, string dsRef)
        {
            GooseName = name;
            GooseCbReference = reference;
            DstMacAddress = mac;
            AppId = appId;
            Dsreference = dsRef;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Probe1
{
    class Claim
    {
        private string claimType;
        public string ClaimNumber;
        public string LineNumber;
        public string ProductCode;
        public string ProductName;
        public string BatchNumber;
        public string Amount;
        public string StackerNmber;
        public string StorageBin;
        public string Rack;
        public string Raw;
        public string Floor;
        public string FileName;
        public string OrginalString;

        public string ClaimType { get => claimType; set => claimType = value; }

        public Claim()
        {

        }
    }
}

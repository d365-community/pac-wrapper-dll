using System.Collections.Generic;
using System.Runtime.Serialization;

namespace D365.Community.Pac.Wrapper
{
    [DataContract]
    internal class PacResult
    {
        internal PacResult() { }

        [DataMember(Name = "Status")]
        internal string Status { get; set; }

        [DataMember(Name = "Errors")]
        internal List<string> Errors { get; set; }

        [DataMember(Name = "Warnings")]
        internal List<string> Warnings { get; set; }

        [DataMember(Name = "Information")]
        internal List<string> Information { get; set; }
    }
}

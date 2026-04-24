using System.Runtime.Serialization;

namespace OrderService
{
    /// <summary>
    /// WCF fault detail for validation errors.
    /// </summary>
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Field { get; set; }

        [DataMember]
        public string Reason { get; set; }
    }
}

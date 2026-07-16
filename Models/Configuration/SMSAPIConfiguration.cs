using MimeKit.Encodings;
using System.Web;

namespace HISWEBAPI.Models
{
  
    public class SMSAPIConfiguration
    {
        public int Id { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string SenderId { get; set; }
        public string NumberPlaceholder { get; set; }
        public string MessagePlaceholder { get; set; }
        public string Format { get; set; }
        public int Timeout { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }

        /// <summary>
        /// Builds the complete SMS API URL with contact number and message
        /// </summary>
        public string BuildSmsUrl(string contactNumber, string message)
        {
        //string url = $"{BaseUrl}?apikey={ApiKey}&senderid={SenderId}&format={Format}";
        //url += $"&number={contactNumber}";
        //url += $"&message={System.Web.HttpUtility.UrlEncode(message)}";
        //return url;


            // this is new SMS CONFIGARATION
            string url = $"https://msg.smsguruonline.com/fe/api/v1/send" +
                           $"?username=gravity.trans" +
                           $"&password=ROaJt" +
                           $"&unicode=false" +
                           $"&from=GRAVTT" +
                           $"&to={contactNumber}" +
                           $"&dltPrincipalEntityId=1701173156606119060" +
                           $"&dltContentId=1707176171295497152" +
                           $"&text={HttpUtility.UrlEncode(message)}";

            return url;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Helpers
{
    public class AppSettings
    {
        public string Issuer { get; set; }    // issuer token
        public string Audience { get; set; }  // audience token
        public int Lifetime { get; set; }     // life time token
        public string Secret { get; set; }    // key for crypt
    }
}

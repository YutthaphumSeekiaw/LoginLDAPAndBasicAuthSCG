using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TestLdap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        public LoginController() { 
        
        }

        public class loginModel { 
            public string userName { get; set; }  
            public string password { get; set; }
        }

        [HttpPost]
        [Route("LoginBasic")]

        public IActionResult LoginBasic(loginModel loginViewModel)
        {
            string username = loginViewModel.userName;
            string pass = loginViewModel.password;
            string userpass = username + ":" + pass;
            //string jsonstring = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(userpass));
            //string LoginJsonString = JsonConvert.SerializeObject(jsonstring);
            //UserADSCG userADSCG = new UserADSCG();
            try
            {
                 var data =  HttpActionToAPISCG("GET", "https://apgwd.scg.com/ADAuth", userpass);
                //userADSCG = JsonConvert.DeserializeObject<UserADSCG>(_accountAPIRepository.GetLoginAD(userpass, _token));
                //userADSCG.sAMAcountName = userADSCG.sAMAcountName;
            }
            catch (Exception ex)
            {
                throw;
            }
            return Ok("");
        }

        private static (bool IsSuccess, string ExceptionMessage, string Data) HttpActionToAPISCG(string method, string url, string jsonString)
        {
            bool isSuccess;
            string exceptionMessage = string.Empty;
            dynamic data = null;

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = method;
            httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(jsonString));
            if (!method.Equals("GET"))
            {
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonString);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream receiveStream = httpResponse.GetResponseStream();
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            data = readStream.ReadToEnd();
            isSuccess = true;
            return (isSuccess, exceptionMessage, data);
        }

        public class UserLdap
        {
            public string UserName { get; set; }
            public string DisplayName { get; set; }
            // other properties
        }


        private const string DisplayNameAttribute = "DisplayName";
        private const string SAMAccountNameAttribute = "SAMAccountName";


        [HttpGet]
        [Route("LoginLdap")]
        public IActionResult LoginLdap(string userName, string password)
        {
            try
            {
                UserLdap UserLdap = new UserLdap();
                using (DirectoryEntry entry = new DirectoryEntry("LDAP://scgadbs.cementhai.com:389", "cementhai" + "\\" + userName, password))
                //using (DirectoryEntry entry = new DirectoryEntry("LDAP://scgadbs.cementhai.com:636", "cementhai" + "\\" + userName, password))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = String.Format("({0}={1})", SAMAccountNameAttribute, userName);
                        searcher.PropertiesToLoad.Add(DisplayNameAttribute);
                        searcher.PropertiesToLoad.Add(SAMAccountNameAttribute);
                        var result = searcher.FindOne();
                        if (result != null)
                        {
                            var displayName = result.Properties[DisplayNameAttribute];
                            var samAccountName = result.Properties[SAMAccountNameAttribute];

                            UserLdap =  new UserLdap
                            {
                                DisplayName = displayName == null || displayName.Count <= 0 ? null : displayName[0].ToString(),
                                UserName = samAccountName == null || samAccountName.Count <= 0 ? null : samAccountName[0].ToString()
                            };
                        }
                    }
                }
                return Ok(UserLdap);
            }
            catch (Exception ex)
            {
                // if we get an error, it means we have a login failure.
                // Log specific exception
            }
            return null;
        }

    }


}

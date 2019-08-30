/*
 * This function is not intended to be invoked directly. Instead it will be
 * triggered by an orchestrator function.
 * 
 * Before running this sample, please:
 * - create a Durable orchestration function
 * - create a Durable HTTP starter function
 */
 
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Newtonsoft.Json"
#load "properties.csx"
 
using System;
using System.Net;
using System.Net.Http;
using System.Configuration;
using System.Security.Claims; 
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public static string GetApplicationGateway(string resource_id, ILogger log, string stoken, string endpoint)
{
   log.LogInformation("URL for API: "+endpoint);
    var httpCl = new HttpClient();
    httpCl.DefaultRequestHeaders.Add("Authorization", "Bearer " + stoken);

    var httpResponse = new HttpResponseMessage();
    string httpResponseBody = "";
    httpResponse = httpCl.GetAsync(endpoint).Result;
    httpResponseBody = httpResponse.Content.ReadAsStringAsync().Result.ToString();
    log.LogInformation("Get response for Application Gateway is: ");
    log.LogInformation(httpResponseBody);
    return httpResponseBody;
}
      
public static void setApplicationGateway(dynamic c,string stoken,ILogger log,string endpoint){

   var httpClient = new HttpClient();
   httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + stoken);
   var content = new StringContent(JsonConvert.SerializeObject(c), Encoding.UTF8, "application/json");
   var response = httpClient.PutAsync(endpoint, content).Result;
   string result = response.Content.ReadAsStringAsync().Result.ToString();
    log.LogInformation(content.ToString());
   log.LogInformation("SQL setting:" + result);
   var statuscode = response.StatusCode.ToString();

   log.LogInformation("Status code for API:" + statuscode);

}

public static void EnableWAF(string resource_id, ILogger log,string stoken)
{
   string endpoint = "https://management.azure.com/";
   endpoint = endpoint + resource_id + "?api-version=2019-06-01";
   string httpResponseBody = GetApplicationGateway(resource_id,log,stoken,endpoint);

   dynamic c = JsonConvert.DeserializeObject(httpResponseBody);
  // var obj = new JObject();
  // obj["enabled"] = "true";
   //obj["firewallMode"] = "Detection";
   //c.Insert(obj);
    c.properties.webApplicationFirewallConfiguration.enabled = true;
    c.properties.webApplicationFirewallConfiguration.firewallMode = "Detection";
    setApplicationGateway(c,stoken,log,endpoint);

}




public static void Run(Tuple<string, string, string> tuple1, ILogger log)
{
 
   log.LogInformation("Activity function started...");
   string module_id = tuple1.Item1;
   string resource_id = tuple1.Item2;
   string stoken = tuple1.Item3;
   log.LogInformation("Module ID: " + module_id);

   log.LogInformation("Token: " + stoken);
    
   log.LogInformation("Remediation started for resource with ID: ");
   log.LogInformation(resource_id);

   EnableWAF(resource_id,log,stoken);
    /*test*/
}

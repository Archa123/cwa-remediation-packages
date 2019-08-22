

//#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Microsoft.Azure.WebJobs.Extensions.DurableTask"
#r "Newtonsoft.Json"
#load "model.csx"

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class ClientDetail
{
    public string grant_type = "client_credentials";
    public string client_id =  Environment.GetEnvironmentVariable("CLIENT_ID", EnvironmentVariableTarget.Process);
    public string client_secret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
    public string resource = "https://management.azure.com/";
     public string tenant_id = Environment.GetEnvironmentVariable("DIRECTORY_ID");

}

class Result {
    public string token_type {get;set;}
    public int expires_in {get;set;}
    public int ext_expires_in {get;set;}
    public string expires_on{get;set;}
    public string not_before {get;set;}
    public string resource {get;set;}
    public string access_token {get;set;}
}

private static string GetAccessToken(ILogger log1)
{
    ClientDetail cd = new ClientDetail();    
    
    log1.LogInformation("Enter Get Access Token Function!!");
    var keyValues = new List<KeyValuePair<string, string>>();
    keyValues.Add(new KeyValuePair<string, string>("grant_type", cd.grant_type));

    log1.LogInformation("client_id :" +cd.client_id);
    keyValues.Add(new KeyValuePair<string, string>("client_id", cd.client_id));

    log1.LogInformation("client_secret :" +cd.client_secret);
    keyValues.Add(new KeyValuePair<string, string>("client_secret", cd.client_secret));
    keyValues.Add(new KeyValuePair<string, string>("resource", cd.resource));

    var httpClient = new HttpClient();
    var response = httpClient.PostAsync("https://login.microsoftonline.com/" + cd.tenant_id +"/oauth2/token", new FormUrlEncodedContent(keyValues)).Result;
    string result = response.Content.ReadAsStringAsync().Result.ToString();    
    Result rs = JsonConvert.DeserializeObject<Result>(result);
    return rs.access_token;    
}

private static Tuple<string, string> ReadCheckConfig(string check_id, string path, ILogger log)
{
  var result1 = Tuple.Create("", "");
  //Console.WriteLine("Test Console");
  
  FileStream fsSource = new FileStream(path, FileMode.Open, FileAccess.Read);
  using (StreamReader sr = new StreamReader(fsSource))
  {
      while(!sr.EndOfStream)
      {
       string line = sr.ReadLine();
       if (line.Contains(check_id))
        {
           log.LogInformation("Check "+ check_id +" found in config file");
           var fields = line.Split(':');
           string f0 = fields[0].Trim();
           //log.LogInformation("Check ID: "+ f0);
           string f1 = fields[1].Trim();
           //log.LogInformation("Function App: "+ f1);
           string f2 = fields[2].Trim();
           //log.LogInformation("Function Module: "+ f2);

           var result = Tuple.Create(f1, f2);
           return result;

        }else
        {
           log.LogInformation("Error: Could not find the check "+ check_id +" in config file");
           log.LogInformation("Resolve: Manually enter <check_id>:<Function_App>:<Function_Module> ");
           return result1;
        }

      }
      log.LogInformation("Error: The Check Config file is Empty!!");
      return result1;
    }  
}

public static bool HasValue( Tuple<string, string> tuple)
    {
        return !string.IsNullOrEmpty(tuple?.Item1) && !string.IsNullOrEmpty(tuple?.Item2);
    }

private static string GetResourceType(string resourceid)
{ 
    string resourcetype = "could not found";
    if(resourceid.Contains("Microsoft.Sql")){
        if (resourceid.Contains("/servers/"))
        {
            resourcetype = "SQL_Server";
            return resourcetype;
        }
        if (resourceid.Contains("/database/"))
        {
            resourcetype = "SQL_DB";
            return resourcetype;
        }

    }
    return resourcetype;
}

private static List<string> GetCheckIDs(List<Check> checks){
    var checkList = new List<string>();
    foreach (Check check in checks)
    {
      string checkid = check.id;
      checkList.Add(checkid);
    }
    return checkList;
}

public static void Run(DurableOrchestrationContext context, ILogger log, ExecutionContext context1)
{
  var event_data = context.GetInput<string>();
  var outputs = new List<string>();
   

  //Check for event_data
  Payload data = JsonConvert.DeserializeObject<Payload>(event_data);
  log.LogInformation("Payload ID : " + data.payload_id);
  log.LogInformation("Resource account : " + data.resource.account_id);
  log.LogInformation("Resource Name : " + data.resource.name);
  log.LogInformation("Resource region : " + data.resource.region);
  string resource_type = GetResourceType(data.resource.id);
  log.LogInformation("Resource Type : " + resource_type);

  string resource_id = data.resource.id; 
  List<Check> checks = data.checks;
  List<string> checklist = GetCheckIDs(checks);

  //get the path of check config file
  var path = System.IO.Path.Combine(context1.FunctionDirectory, "check_config.csv");  
  log.LogInformation("Check Config file path: "+ path); 

  log.LogInformation("Generating access token: ");
  string stoken = GetAccessToken(log);
  log.LogInformation("Token: "+ stoken);
  
  foreach (string check in checklist){
     Tuple<string,string> getFunctionApp = ReadCheckConfig(check, path, log);
     string functionapp = getFunctionApp.Item1;
     string moduleapp = getFunctionApp.Item2;
     bool b1 = HasValue(getFunctionApp);

     if(b1){

         log.LogInformation("Calling Function : "+ functionapp);
         var tuple1 = Tuple.Create(moduleapp, resource_id, stoken);
         context.CallSubOrchestratorAsync(functionapp, tuple1);
     }else{
         log.LogInformation("Error: Pre-requisite Function App or Module Name missing!!");
     }
     /*test*/
    }

     


}

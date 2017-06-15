<Query Kind="FSharpProgram">
  <NuGetReference>RestSharp</NuGetReference>
</Query>

open System
open RestSharp

module EventStoreClient =
    
    let baseUrl = "http://localhost:2113"
    let submit streamId data =
        let client  = RestClient(baseUrl)
        let url = printf "streams/%s" streamId
        
        let request = new RestRequest(url, Method.POST)
        request.RequestFormat <- DataFormat.Json
        request.AddHeader("Content-Type", "application/vnd.eventstore.events+json");
        
        let body = {
            eventId = Guid.NewGuid().ToString()
        }
        
        request.AddBody(body);
        
        let response = client.Execute(request);
        
        if response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.Found then
            let msg = printf "Event Store returned an error. Code: %s, Message: %s" response.StatusCode response.ErrorMessage
            raise ApplicationException msg
        
        ()


        try
            submit "jwe-test" { text = "aText", value = "aValue" }
        with ex -> ex.Dump()

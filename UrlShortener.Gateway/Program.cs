/// <summary>
/// The entry point for the API Gateway.
/// This minimal setup uses YARP (Yet Another Reverse Proxy) to route incoming HTTP requests 
/// to the internal microservices (WriteApi and ReadApi) based on the rules defined in appsettings.json.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// Registers the YARP services and dynamically loads the routing rules (Routes and Clusters) 
// from the "ReverseProxy" section in the configuration file.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Adds the YARP middleware to the request processing pipeline. 
// This intercepts incoming requests and seamlessly forwards them to the appropriate destination.
app.MapReverseProxy();

app.Run();
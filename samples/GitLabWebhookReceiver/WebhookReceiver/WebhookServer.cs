using System;
using System.Net;
using System.Text;
using GitLabWebhookReceiver.Dispatcher;
using GitLabWebhookReceiver.Config;

namespace GitLabWebhookReceiver.WebhookReceiver
{
    /// <summary>
    /// HTTP server that hosts the GitLab webhook receiver endpoint.
    /// Listens on /webhooks/gitlab/issues for incoming webhook POST requests.
    /// </summary>
    public class WebhookServer
    {
        private readonly HttpListener _listener;
        private readonly GitLabWebhookReceiver _receiver;
        private readonly string _webhookPath = "/webhooks/gitlab/issues";

        /// <summary>
        /// Initializes a new instance of the WebhookServer.
        /// </summary>
        /// <param name="host">Host address to listen on</param>
        /// <param name="port">Port to listen on</param>
        /// <param name="dispatcher">The dispatcher to forward validated events to</param>
        /// <param name="webhookSecret">The secret token to validate incoming requests</param>
        public WebhookServer(string host, int port, IIssueEventDispatcher dispatcher, string webhookSecret)
        {
            _listener = new HttpListener();
            var prefix = $"http://{host}:{port}{_webhookPath}/";
            _listener.Prefixes.Add(prefix);

            _receiver = new GitLabWebhookReceiver(dispatcher, webhookSecret);

            Console.WriteLine($"[WebhookServer] Configured to listen on: {prefix}");
        }

        /// <summary>
        /// Starts the HTTP server and begins processing requests.
        /// This method blocks until the server is stopped.
        /// </summary>
        public void Start()
        {
            _listener.Start();
            Console.WriteLine("[WebhookServer] Server started. Listening for webhook requests...");
            Console.WriteLine("[WebhookServer] Press Ctrl+C to stop.");

            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException ex)
                {
                    // Listener was stopped
                    if (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
                        break;

                    Console.Error.WriteLine($"[WebhookServer] HttpListenerException: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[WebhookServer] Unhandled exception: {ex.Message}");
                }
            }

            Console.WriteLine("[WebhookServer] Server stopped.");
        }

        /// <summary>
        /// Stops the HTTP server.
        /// </summary>
        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        /// <summary>
        /// Processes an incoming HTTP request.
        /// </summary>
        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Console.WriteLine($"[WebhookServer] Received {request.HttpMethod} request from {request.RemoteEndPoint}");

            // Handle the request using the webhook receiver
            var webhookResponse = _receiver.HandleRequest(request);

            // Send the HTTP response
            response.StatusCode = webhookResponse.StatusCode;
            response.ContentType = "text/plain";

            var responseBytes = Encoding.UTF8.GetBytes(webhookResponse.Message);
            response.ContentLength64 = responseBytes.Length;

            try
            {
                response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WebhookServer] Error writing response: {ex.Message}");
            }
        }
    }
}

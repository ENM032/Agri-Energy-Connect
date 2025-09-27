using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WebApplication2.Services;
using System.Net.NetworkInformation;
using System.Net;

namespace WebApplication2.HealthChecks
{
    public class EmailServiceHealthCheck : IHealthCheck
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailServiceHealthCheck> _logger;

        public EmailServiceHealthCheck(IOptions<EmailSettings> emailSettings, ILogger<EmailServiceHealthCheck> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["smtpServer"] = _emailSettings.SmtpServer ?? "Not configured",
                    ["smtpPort"] = _emailSettings.SmtpPort,
                    ["fromEmail"] = _emailSettings.FromEmail ?? "Not configured"
                };

                // Check if email settings are configured
                if (string.IsNullOrEmpty(_emailSettings.SmtpServer) ||
                    string.IsNullOrEmpty(_emailSettings.FromEmail) ||
                    string.IsNullOrEmpty(_emailSettings.SmtpUsername))
                {
                    _logger.LogWarning("Email service is not properly configured");
                    return HealthCheckResult.Degraded("Email service is not properly configured", data: data);
                }

                // Check if SMTP server is reachable
                var ping = new Ping();
                var reply = await ping.SendPingAsync(_emailSettings.SmtpServer, 5000);
                
                if (reply.Status != IPStatus.Success)
                {
                    _logger.LogWarning("SMTP server {SmtpServer} is not reachable: {Status}", 
                        _emailSettings.SmtpServer, reply.Status);
                    return HealthCheckResult.Degraded($"SMTP server is not reachable: {reply.Status}", data: data);
                }

                // Check if SMTP port is open
                using var tcpClient = new System.Net.Sockets.TcpClient();
                var connectTask = tcpClient.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                var timeoutTask = Task.Delay(5000, cancellationToken);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == timeoutTask || !tcpClient.Connected)
                {
                    _logger.LogWarning("Cannot connect to SMTP server {SmtpServer}:{SmtpPort}", 
                        _emailSettings.SmtpServer, _emailSettings.SmtpPort);
                    return HealthCheckResult.Degraded($"Cannot connect to SMTP server {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}", data: data);
                }

                data["connectionStatus"] = "Connected";
                data["pingTime"] = $"{reply.RoundtripTime}ms";
                
                _logger.LogDebug("Email service health check passed. SMTP server {SmtpServer} is reachable", 
                    _emailSettings.SmtpServer);
                
                return HealthCheckResult.Healthy("Email service is configured and SMTP server is reachable", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email service health check failed");
                return HealthCheckResult.Unhealthy("Email service health check failed", ex);
            }
        }
    }
}
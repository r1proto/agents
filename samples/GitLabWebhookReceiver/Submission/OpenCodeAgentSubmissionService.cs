using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using GitLabWebhookReceiver.Models;

namespace GitLabWebhookReceiver.Submission
{
    /// <summary>
    /// Implementation of IAgentSubmissionService that submits tasks to OpenCode AI agents.
    /// This service integrates with the OpenCode SDK via CLI invocation or HTTP API.
    /// </summary>
    public class OpenCodeAgentSubmissionService : IAgentSubmissionService
    {
        private readonly string _openCodeExecutable;
        private readonly string _agentName;
        private readonly HashSet<string> _submittedTasks;
        private readonly int _deduplicationWindowMinutes;

        /// <summary>
        /// Creates a new instance of OpenCodeAgentSubmissionService.
        /// </summary>
        /// <param name="openCodeExecutable">Path to the opencode CLI executable (defaults to "opencode" in PATH)</param>
        /// <param name="agentName">Name of the OpenCode agent to invoke (defaults to "dotnet-feature-coder")</param>
        /// <param name="deduplicationWindowMinutes">Time window for deduplication in minutes (defaults to 60)</param>
        public OpenCodeAgentSubmissionService(
            string openCodeExecutable = "opencode",
            string agentName = "dotnet-feature-coder",
            int deduplicationWindowMinutes = 60)
        {
            _openCodeExecutable = openCodeExecutable ?? "opencode";
            _agentName = agentName ?? "dotnet-feature-coder";
            _submittedTasks = new HashSet<string>();
            _deduplicationWindowMinutes = deduplicationWindowMinutes;
        }

        /// <summary>
        /// Submits a task to the OpenCode AI Agent.
        /// Performs idempotency checks to avoid duplicate submissions.
        /// </summary>
        /// <param name="task">The agent task to submit</param>
        /// <returns>A submission result indicating success, duplicate, or failure</returns>
        public SubmissionResult SubmitTask(AgentTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            // Validate required fields
            if (string.IsNullOrWhiteSpace(task.TargetRepoUrl))
                return SubmissionResult.Failure(string.Empty, "TargetRepoUrl is required");

            if (string.IsNullOrWhiteSpace(task.Title))
                return SubmissionResult.Failure(string.Empty, "Task title is required");

            // Generate deduplication key
            var dedupKey = task.GetDeduplicationKey();

            // Check for duplicate submission
            if (_submittedTasks.Contains(dedupKey))
            {
                Console.WriteLine($"[AgentSubmission] Duplicate task detected: {dedupKey}");
                return SubmissionResult.Duplicate(dedupKey);
            }

            try
            {
                // Build the agent prompt from the task
                var prompt = BuildAgentPrompt(task);

                // Submit to OpenCode agent
                var jobId = SubmitToOpenCode(prompt, task);

                // Mark as submitted
                _submittedTasks.Add(dedupKey);

                Console.WriteLine($"[AgentSubmission] Successfully submitted task: {dedupKey}");
                Console.WriteLine($"[AgentSubmission] Agent job ID: {jobId}");

                return SubmissionResult.Success(dedupKey, jobId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AgentSubmission] Failed to submit task: {ex.Message}");
                Console.Error.WriteLine($"[AgentSubmission] Stack trace: {ex.StackTrace}");
                return SubmissionResult.Failure(dedupKey, $"Failed to submit task: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Builds a prompt for the OpenCode agent from the task.
        /// </summary>
        private string BuildAgentPrompt(AgentTask task)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("# GitLab Issue Task");
            prompt.AppendLine();
            prompt.AppendLine($"**Issue**: {task.Title}");
            prompt.AppendLine($"**Issue URL**: {task.WebUrl}");
            prompt.AppendLine($"**Source Project**: {task.SourceProjectPath} (ID: {task.SourceProjectId})");
            prompt.AppendLine($"**Issue ID**: #{task.IssueIid}");
            prompt.AppendLine($"**Author**: {task.Author}");
            prompt.AppendLine($"**State**: {task.State}");
            prompt.AppendLine($"**Action**: {task.EventAction}");
            prompt.AppendLine();

            if (task.Labels != null && task.Labels.Count > 0)
            {
                prompt.AppendLine($"**Labels**: {string.Join(", ", task.Labels)}");
                prompt.AppendLine();
            }

            if (task.Assignees != null && task.Assignees.Count > 0)
            {
                prompt.AppendLine($"**Assignees**: {string.Join(", ", task.Assignees)}");
                prompt.AppendLine();
            }

            prompt.AppendLine("## Description");
            prompt.AppendLine();
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                prompt.AppendLine(task.Description);
            }
            else
            {
                prompt.AppendLine("(No description provided)");
            }
            prompt.AppendLine();

            prompt.AppendLine("## Target Repository");
            prompt.AppendLine();
            prompt.AppendLine($"**Repository URL**: {task.TargetRepoUrl}");
            if (!string.IsNullOrWhiteSpace(task.TargetRepoRef))
            {
                prompt.AppendLine($"**Branch/Ref**: {task.TargetRepoRef}");
            }
            prompt.AppendLine();

            prompt.AppendLine("## Instructions");
            prompt.AppendLine();
            prompt.AppendLine("Please implement the necessary changes to address this GitLab issue in the target repository.");
            prompt.AppendLine("Follow the existing code patterns and conventions in the repository.");
            prompt.AppendLine("Ensure all changes are tested and documented appropriately.");

            return prompt.ToString();
        }

        /// <summary>
        /// Submits the prompt to the OpenCode agent via CLI or HTTP API.
        /// This is a placeholder implementation that demonstrates the integration pattern.
        /// In a production environment, this would invoke the actual OpenCode CLI or API.
        /// </summary>
        private string SubmitToOpenCode(string prompt, AgentTask task)
        {
            // For now, we'll create a simple stub that logs the submission
            // In a real implementation, this would:
            // 1. Invoke the OpenCode CLI: opencode --agent <agent_name> --prompt "<prompt>"
            // 2. Or make an HTTP POST to the OpenCode API endpoint
            // 3. Parse the response to get the job/run ID
            // 4. Return the job ID

            Console.WriteLine($"[OpenCode] Submitting task to agent: {_agentName}");
            Console.WriteLine($"[OpenCode] Target repository: {task.TargetRepoUrl}");
            Console.WriteLine($"[OpenCode] Issue: {task.Title}");
            Console.WriteLine($"[OpenCode] Prompt length: {prompt.Length} characters");

            // Generate a mock job ID for tracking
            var jobId = $"job-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // In a real implementation, we would invoke the OpenCode CLI like this:
            // var result = InvokeOpenCodeCli(prompt, task);
            // return result.JobId;

            // For demonstration, write the prompt to a temporary file
            var tempDir = Path.Combine(Path.GetTempPath(), "opencode-submissions");
            Directory.CreateDirectory(tempDir);

            var filename = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{task.SourceProjectId}-{task.IssueIid}.txt";
            var filepath = Path.Combine(tempDir, filename);

            File.WriteAllText(filepath, prompt);
            Console.WriteLine($"[OpenCode] Prompt saved to: {filepath}");
            Console.WriteLine($"[OpenCode] Mock job ID: {jobId}");

            return jobId;
        }

        /// <summary>
        /// Example method showing how to invoke the OpenCode CLI (commented out for stub implementation).
        /// This would be used in a production environment.
        /// </summary>
        private string InvokeOpenCodeCli(string prompt, AgentTask task)
        {
            // Example implementation using Process.Start to invoke the CLI
            var processInfo = new ProcessStartInfo
            {
                FileName = _openCodeExecutable,
                Arguments = $"--agent {_agentName} --prompt \"{EscapeArgument(prompt)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Add environment variables for repository context
            processInfo.Environment["OPENCODE_TARGET_REPO"] = task.TargetRepoUrl;
            if (!string.IsNullOrWhiteSpace(task.TargetRepoRef))
            {
                processInfo.Environment["OPENCODE_TARGET_REF"] = task.TargetRepoRef;
            }

            using (var process = Process.Start(processInfo))
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"OpenCode CLI failed with exit code {process.ExitCode}: {error}");
                }

                // Parse output to extract job ID
                // The actual format depends on the OpenCode CLI output
                return ParseJobIdFromOutput(output);
            }
        }

        /// <summary>
        /// Escapes a command-line argument for safe passing to a process.
        /// </summary>
        private string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Parses the job ID from OpenCode CLI output.
        /// The actual implementation depends on the CLI output format.
        /// </summary>
        private string ParseJobIdFromOutput(string output)
        {
            // Placeholder: In reality, parse JSON or text output from OpenCode CLI
            // Example: {"jobId": "abc123", "status": "submitted"}
            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
                if (result.ContainsKey("jobId"))
                {
                    return result["jobId"].ToString();
                }
            }
            catch
            {
                // If JSON parsing fails, try extracting from text
            }

            return $"job-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
    }
}

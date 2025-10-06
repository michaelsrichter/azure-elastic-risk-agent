namespace ElasticOn.RiskAgent.Demo.M365.Services;

/// <summary>
/// Mode for jailbreak detection behavior
/// </summary>
public enum JailbreakDetectionMode
{
    /// <summary>
    /// Jailbreak detection is disabled - no analysis performed
    /// </summary>
    Disabled,

    /// <summary>
    /// Jailbreak detection runs but doesn't block - audit mode with reporting
    /// </summary>
    Audit,

    /// <summary>
    /// Jailbreak detection blocks requests when detected - enforcement mode
    /// </summary>
    Enforce
}

/// <summary>
/// Result of jailbreak detection analysis
/// </summary>
public class JailbreakDetectionResult
{
    /// <summary>
    /// Whether a jailbreak attempt was detected
    /// </summary>
    public bool IsJailbreakDetected { get; set; }

    /// <summary>
    /// The offending text that triggered the jailbreak detection (if any)
    /// </summary>
    public string? OffendingText { get; set; }

    /// <summary>
    /// The detection mode that was used
    /// </summary>
    public JailbreakDetectionMode Mode { get; set; }
}

/// <summary>
/// Interface for Azure AI Content Safety services
/// </summary>
public interface IContentSafetyService
{
    /// <summary>
    /// Gets the current jailbreak detection mode
    /// </summary>
    JailbreakDetectionMode DetectionMode { get; }

    /// <summary>
    /// Analyzes text for potential jailbreak attempts using Azure Content Safety Prompt Shield.
    /// Text longer than 1000 characters is automatically split into chunks and analyzed separately.
    /// </summary>
    /// <param name="text">The text to analyze (will be chunked internally if needed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection result including whether jailbreak was detected and offending text</returns>
    Task<JailbreakDetectionResult> DetectJailbreakAsync(string text, CancellationToken cancellationToken = default);
}

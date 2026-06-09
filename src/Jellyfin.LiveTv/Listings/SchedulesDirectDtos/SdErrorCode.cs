#pragma warning disable CA1008 // Enums should have zero value

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// Schedules Direct API error codes. See https://github.com/SchedulesDirect/JSON-Service/wiki/API-20141201#error-response for details.
/// </summary>
public enum SdErrorCode
{
    /// <summary>
    /// Schedules Direct unavailable/out of service.
    /// </summary>
    ServiceOffline = 3000,

    /// <summary>
    /// Server is busy processing other requests.
    /// </summary>
    ServerBusy = 3001,

    /// <summary>
    /// Account expired.
    /// </summary>
    AccountExpired = 4001,

    /// <summary>
    /// Invalid password hash.
    /// </summary>
    InvalidHash = 4002,

    /// <summary>
    /// Invalid user or password.
    /// </summary>
    InvalidUser = 4003,

    /// <summary>
    /// Account temporarily locked due to login failures.
    /// </summary>
    AccountTempLock = 4004,

    /// <summary>
    /// Access to the account via the JSON service has been disabled. Contact Schedules Direct support.
    /// </summary>
    JsonAccessDisabled = 4005,

    /// <summary>
    /// Token has expired. Request a new one.
    /// </summary>
    TokenExpired = 4006,

    /// <summary>
    /// Application is not authorized to use the data service.
    /// </summary>
    ApplicationDisabled = 4007,

    /// <summary>
    /// Account not active.
    /// </summary>
    AccountInactive = 4008,

    /// <summary>
    /// Maximum login attempts exceeded.
    /// </summary>
    MaxLoginAttempts = 4009,

    /// <summary>
    /// Maximum unique IP attempts reached.
    /// </summary>
    MaxIPAttempts = 4010,

    /// <summary>
    /// Maximum number of lineup changes for today reached.
    /// </summary>
    MaxLineupChanges = 4100,

    /// <summary>
    /// Requested image not found.
    /// </summary>
    ImageNotFound = 5000,

    /// <summary>
    /// Maximum image downloads reached for the day.
    /// </summary>
    MaxImageDownloads = 5002,

    /// <summary>
    /// Trial specific maximum image downloads reached for the day.
    /// </summary>
    MaxImageDownloadsTrial = 5003,

    /// <summary>
    /// Maximum number of invalid image URIs in 24 hours reached.
    /// </summary>
    MaxInvalidImages = 5004
}

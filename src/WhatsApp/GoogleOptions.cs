using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Devlooped.WhatsApp;

/// <summary>
/// Options for handling communication with Google.
/// </summary>
public class GoogleOptions
{
    [Required]
    public required string Endpoint { get; set; }

    [Required]
    public required string CallbackUri { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }
}
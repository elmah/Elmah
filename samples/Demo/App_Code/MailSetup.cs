using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Net.Mail;
using System.Web.Configuration;

public static class MailSetup
{
    private static bool? _validationResult;

    /// <summary>
    /// Determines whether error mails should be suppressed/filtered based
    /// on <c>system.net/mailSettings/smtp</c> configuration.
    /// </summary>
    
    public static bool SuppressesErrorMailing
    {
        get { return (_validationResult ?? (_validationResult = Validate())).Value; }
    }

    private static bool Validate()
    {
        var result =
            from SmtpSection smtp in new[] { WebConfigurationManager.GetSection("system.net/mailSettings/smtp") }
            where smtp != null && SmtpDeliveryMethod.SpecifiedPickupDirectory == smtp.DeliveryMethod
            select smtp.SpecifiedPickupDirectory into spd
            select spd != null ? spd.PickupDirectoryLocation : null into path 
            select (bool?) (string.IsNullOrEmpty(path) || !Directory.Exists(path));

        return result.SingleOrDefault() ?? false;
    }
}
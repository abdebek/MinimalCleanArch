#if (UseAuth)
namespace MCA.Application.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, string userId);
    Task SendEmailConfirmationAsync(string email, string token, string userId);
}
#endif

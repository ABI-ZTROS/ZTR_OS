namespace ZTR.Desktop.Features.UserAgreement.Services;

public interface IUserAgreementService
{
    bool IsAgreed { get; }
    DateTime? AgreedAt { get; }
    string? AgreedVersion { get; }
    string CurrentAgreementVersion { get; }
    bool RequiresReagreement { get; }
    void SetAgreed(string version);
    void Load();
    void Save();
}

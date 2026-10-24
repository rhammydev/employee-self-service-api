namespace EmployeeSelfServiceAPI.Domain.Enums;

public enum TrainingStatus
{
    Draft,
    PendingManagerApproval,
    PendingHRApproval,
    PendingFinanceApproval,
    ReturnedToHR,
    Approved,
    EvidenceSubmitted,
    Completed,
    Rejected,
    RejectedNoEvidence,
    Withdrawn
}
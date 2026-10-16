namespace EmployeeSelfServiceAPI.Domain.Enums;

public enum TrainingAction
{
    Created,
    Submitted,
    ManagerApproved,
    ManagerRejected,
    HRApproved,
    FinanceApproved,
    FinanceRejected,
    FinanceReturnedToHR,
    AllowanceCorrected,
    Withdrawn,
    ReviewerReassigned,
    EvidenceSubmitted,
    EvidenceAccepted,
    EvidenceReturned,
    ClosedNoEvidence
}